using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using TBMWatchdog.Models;
using TBMWatchdog.Utilities;
using Tesseract;

namespace TBMWatchdog.Services;

public sealed class OcrService : IDisposable
{
    private readonly SemaphoreSlim _engineLock = new(1, 1);
    private TesseractEngine? _engine;
    private bool _disposed;
    public OcrService(string? tessdataPath = null, string language = "eng")
    { TessdataPath = tessdataPath ?? Path.Combine(AppContext.BaseDirectory, "tessdata"); Language = language; }
    public string TessdataPath { get; }
    public string Language { get; }
    internal int EngineCreationCount { get; private set; }

    public Task<OcrReadResult> ReadNumberAsync(Bitmap source, decimal maximumValue, CancellationToken token = default) =>
        ReadNumberAsync(source, maximumValue, new OcrProfile(), OcrFieldKind.General, token);

    public async Task<OcrReadResult> ReadNumberAsync(Bitmap source, decimal maximumValue, OcrProfile profile,
        OcrFieldKind field, CancellationToken token = default)
    { using var diagnostic = await ReadNumberWithDiagnosticsAsync(source, maximumValue, profile, field, token).ConfigureAwait(false); return diagnostic.Result; }

    public Task<OcrDiagnosticResult> ReadNumberWithDiagnosticsAsync(Bitmap source, decimal maximumValue, CancellationToken token = default) =>
        ReadNumberWithDiagnosticsAsync(source, maximumValue, new OcrProfile(), OcrFieldKind.General, token);

    public Task<OcrDiagnosticResult> ReadNumberWithDiagnosticsAsync(Bitmap source, decimal maximumValue,
        OcrProfile profile, OcrFieldKind field, CancellationToken token = default) =>
        ReadNumberWithDiagnosticsAsync(source, maximumValue, profile, field, token, null);

    public Task<OcrDiagnosticResult> ReadNumberLiveFastWithDiagnosticsAsync(Bitmap source, decimal maximumValue,
        OcrProfile profile, OcrFieldKind field, CancellationToken token = default) =>
        ReadNumberLiveFastWithDiagnosticsCoreAsync(source, maximumValue, profile, field, token);

    private async Task<OcrDiagnosticResult> ReadNumberLiveFastWithDiagnosticsCoreAsync(Bitmap source, decimal maximumValue,
        OcrProfile profile, OcrFieldKind field, CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(_disposed, this); ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(profile);
        await _engineLock.WaitAsync(token).ConfigureAwait(false);
        Bitmap? raw = null; List<OcrCandidateResult>? candidates = null;
        var totalWatch = System.Diagnostics.Stopwatch.StartNew();
        var pipelineCount = 0; var fallbackUsed = false; var heavyFallbackUsed = false; var acceptedFastCandidate = false;
        var targetedFallbackUsed = false; var targetedFallbackMs = 0d;
        const string primaryPipeline = "Gaussian + Median + Otsu/SingleLine";
        try
        {
            raw = new Bitmap(source); EnsureEngine(); candidates = [];
            var preprocessingWatch = System.Diagnostics.Stopwatch.StartNew();
            var created = OcrImageVariantFactory.CreateLiveFast(source, profile, field);
            preprocessingWatch.Stop();
            using var variants = new DisposableVariantCollection(created);
            CharacterSegmentationResult? bestSegmentation = null;
            try
            {
                for (var index = 0; index < variants.Items.Count; index++)
                {
                    token.ThrowIfCancellationRequested();
                    var variant = variants.Items[index];
                    var segmentationWatch = System.Diagnostics.Stopwatch.StartNew();
                    using var segmentation = CharacterSegmenter.Segment(variant.Image, field, CharacterSegmentationOptions.For(profile, field));
                    segmentationWatch.Stop();
                    if (NumericFieldCharacterOcrService.Supports(field)
                        && NumericFieldCharacterOcrService.GetOptions(profile, field).Enabled
                        && IsAcceptedCharacterSegmentation(segmentation, profile, field)
                        && segmentation.SegmentCrop is not null
                        && IsBetterCharacterSegmentation(segmentation, bestSegmentation, profile, field))
                    {
                        bestSegmentation?.Dispose(); bestSegmentation = CloneSegmentation(segmentation);
                    }
                    var psm = variant.Method == "Nearest + Otsu" ? PageSegMode.SingleWord : PageSegMode.SingleLine;
                    var candidate = ReadCandidate(variant, segmentation, psm, maximumValue, profile, field,
                        preprocessingWatch.Elapsed / Math.Max(1, variants.Items.Count), segmentationWatch.Elapsed, token);
                    candidates.Add(candidate); pipelineCount++;
                    if (pipelineCount > 1) fallbackUsed = true;
                    if (IsLiveFastEarlyExitCandidate(candidate, profile, field)) { acceptedFastCandidate = true; break; }
                }

                if (field == OcrFieldKind.StableThrustForce && NeedsThrustTargetedFallback(candidates))
                {
                    var targetedWatch = System.Diagnostics.Stopwatch.StartNew();
                    using var targeted = OcrImageVariantFactory.CreateThrustTargetedFallback(source);
                    using var targetedSegmentation = CharacterSegmenter.Segment(targeted.Image, field,
                        CharacterSegmentationOptions.For(profile, field));
                    candidates.Add(ReadCandidate(targeted, targetedSegmentation, PageSegMode.SingleWord,
                        maximumValue, profile, field, TimeSpan.Zero, targetedSegmentation.ProcessingElapsed, token,
                        "Targeted fallback: Bicubic + Otsu"));
                    targetedWatch.Stop(); targetedFallbackMs = targetedWatch.Elapsed.TotalMilliseconds;
                    targetedFallbackUsed = true; fallbackUsed = true; pipelineCount++;
                    acceptedFastCandidate = HasPlausibleThrustFullLine(candidates);
                }

                // The rounded 1031 HMI value boxes can lose the leading digit in the denoised
                // live variants. Diagnostics consistently recover it with Bicubic/Otsu +
                // SingleLine, so run exactly that bounded fallback before retaining an old
                // official advance value.
                if (field == OcrFieldKind.Advance && NeedsAdvanceTargetedFallback(candidates, maximumValue, profile))
                {
                    var targetedWatch = System.Diagnostics.Stopwatch.StartNew();
                    using var targeted = OcrImageVariantFactory.CreateAdvanceTargetedFallback(source);
                    using var targetedSegmentation = CharacterSegmenter.Segment(targeted.Image, field,
                        CharacterSegmentationOptions.For(profile, field));
                    candidates.Add(ReadCandidate(targeted, targetedSegmentation, PageSegMode.SingleLine,
                        maximumValue, profile, field, TimeSpan.Zero, targetedSegmentation.ProcessingElapsed, token,
                        "Advance targeted fallback: Bicubic + Otsu / SingleLine"));
                    targetedWatch.Stop(); targetedFallbackMs += targetedWatch.Elapsed.TotalMilliseconds;
                    targetedFallbackUsed = true; fallbackUsed = true; pipelineCount++;
                    acceptedFastCandidate = !NeedsAdvanceTargetedFallback(candidates, maximumValue, profile);
                }

                // OCR Test uses the broader diagnostic set and can read the small three-decimal
                // torque label even when all three live-fast inputs return empty. Keep the normal
                // live path cheap; only on failure retry the diagnostic set's most useful bicubic
                // input with the two appropriate line modes.
                if (field == OcrFieldKind.StableTorque && NeedsTorqueTargetedFallback(candidates, maximumValue))
                {
                    var targetedWatch = System.Diagnostics.Stopwatch.StartNew();
                    using var targeted = OcrImageVariantFactory.CreateAdvanceTargetedFallback(source);
                    using var targetedSegmentation = CharacterSegmenter.Segment(targeted.Image, field,
                        CharacterSegmentationOptions.For(profile, field));
                    candidates.Add(ReadCandidate(targeted, targetedSegmentation, PageSegMode.SingleWord,
                        maximumValue, profile, field, TimeSpan.Zero, targetedSegmentation.ProcessingElapsed, token,
                        "Torque targeted fallback: Bicubic + Otsu / SingleWord"));
                    candidates.Add(ReadCandidate(targeted, targetedSegmentation, PageSegMode.SingleLine,
                        maximumValue, profile, field, TimeSpan.Zero, targetedSegmentation.ProcessingElapsed, token,
                        "Torque targeted fallback: Bicubic + Otsu / SingleLine"));
                    targetedWatch.Stop(); targetedFallbackMs += targetedWatch.Elapsed.TotalMilliseconds;
                    targetedFallbackUsed = true; fallbackUsed = true; pipelineCount += 2;
                    acceptedFastCandidate = !NeedsTorqueTargetedFallback(candidates, maximumValue);
                }

                // The torque label uses warm brown digits on a neutral rounded HMI box. Grayscale
                // preprocessing can erase its very thin leading 1 (1.708 -> 0.708). Keep one
                // bounded colour-preserving mask and require both word/line readings to agree.
                if (field == OcrFieldKind.StableTorque)
                {
                    var targetedWatch = System.Diagnostics.Stopwatch.StartNew();
                    using var targeted = OcrImageVariantFactory.CreateTorqueColorPreservingFallback(source);
                    using var targetedSegmentation = CharacterSegmenter.Segment(targeted.Image, field,
                        CharacterSegmentationOptions.For(profile, field));
                    candidates.Add(ReadCandidate(targeted, targetedSegmentation, PageSegMode.SingleWord,
                        maximumValue, profile, field, TimeSpan.Zero, targetedSegmentation.ProcessingElapsed, token,
                        "Torque colour-preserving fallback / SingleWord"));
                    candidates.Add(ReadCandidate(targeted, targetedSegmentation, PageSegMode.SingleLine,
                        maximumValue, profile, field, TimeSpan.Zero, targetedSegmentation.ProcessingElapsed, token,
                        "Torque colour-preserving fallback / SingleLine"));
                    targetedWatch.Stop(); targetedFallbackMs += targetedWatch.Elapsed.TotalMilliseconds;
                    targetedFallbackUsed = true; fallbackUsed = true; pipelineCount += 2;

                    using var tight = OcrImageVariantFactory.CreateTorqueTightColorPreservingFallback(source);
                    using var tightSegmentation = CharacterSegmenter.Segment(tight.Image, field,
                        CharacterSegmentationOptions.For(profile, field));
                    candidates.Add(ReadCandidate(tight, tightSegmentation, PageSegMode.SingleWord,
                        maximumValue, profile, field, TimeSpan.Zero, tightSegmentation.ProcessingElapsed, token,
                        "Torque tight colour-preserving fallback / SingleWord"));
                    candidates.Add(ReadCandidate(tight, tightSegmentation, PageSegMode.SingleLine,
                        maximumValue, profile, field, TimeSpan.Zero, tightSegmentation.ProcessingElapsed, token,
                        "Torque tight colour-preserving fallback / SingleLine"));
                    pipelineCount += 2;
                }

                if (!acceptedFastCandidate && profile.LiveFastHeavyFallbackAllowed)
                {
                    heavyFallbackUsed = true; fallbackUsed = true;
                    using var diagnosticVariants = new DisposableVariantCollection(OcrImageVariantFactory.Create(source, profile, field));
                    foreach (var variant in diagnosticVariants.Items)
                    foreach (var psm in GetPageSegModes(profile, field))
                    {
                        token.ThrowIfCancellationRequested();
                        if (candidates.Any(c => c.ProcessingMethod == variant.Method && c.PageSegMode == psm)) continue;
                        using var segmentation = CharacterSegmenter.Segment(variant.Image, field, CharacterSegmentationOptions.For(profile, field));
                        candidates.Add(ReadCandidate(variant, segmentation, psm, maximumValue, profile, field,
                            TimeSpan.Zero, TimeSpan.Zero, token, "Heavy fallback: " + variant.Method));
                    }
                }

                // Stable diagnostic fields use the character/template chain as a fallback/validator. Once the
                // fast full-line path is already plausible, three more OCR calls and template matching add latency
                // without changing the displayed diagnostic value.
                if (ShouldRunPostFastCharacterPipeline(field, acceptedFastCandidate, bestSegmentation?.SegmentCrop is not null))
                {
                    using var crop = new OcrImageVariant("Segment Crop", new Bitmap(bestSegmentation!.SegmentCrop!), new Bitmap(bestSegmentation.SegmentCrop!));
                    candidates.Add(ReadCandidate(crop, bestSegmentation, PageSegMode.SingleWord, maximumValue, profile, field,
                        TimeSpan.Zero, TimeSpan.Zero, token, "Segment Crop + SingleWord"));
                    candidates.Add(ReadCandidate(crop, bestSegmentation, PageSegMode.SingleLine, maximumValue, profile, field,
                        TimeSpan.Zero, TimeSpan.Zero, token, "Segment Crop + SingleLine"));
                    candidates.Add(ReadSegmentedCharacters(crop, bestSegmentation, maximumValue, profile, field, TimeSpan.Zero, token));
                }
            }
            finally { bestSegmentation?.Dispose(); }

            var decisionWatch = System.Diagnostics.Stopwatch.StartNew();
            var (result, selected) = SelectResult(candidates, profile, field);
            decisionWatch.Stop();
            totalWatch.Stop();
            var output = new OcrDiagnosticResult(raw, candidates, selected, result, DateTime.Now, totalWatch.Elapsed)
            {
                ExecutionMode = OcrExecutionMode.LiveFast, PipelineCountExecuted = pipelineCount,
                PrimaryPipeline = primaryPipeline, FallbackUsed = fallbackUsed, HeavyFallbackUsed = heavyFallbackUsed,
                TargetedFallbackUsed = targetedFallbackUsed,
                TargetedFallbackPipeline = targetedFallbackUsed
                    ? field == OcrFieldKind.Advance ? "Bicubic + Otsu / SingleLine"
                    : field == OcrFieldKind.StableTorque ? "Bicubic + Otsu / SingleWord + SingleLine"
                    : "Bicubic + Otsu / SingleWord"
                    : "",
                TargetedFallbackMs = targetedFallbackMs,
                DecisionMs = decisionWatch.Elapsed.TotalMilliseconds
            };
            raw = null; candidates = null; return output;
        }
        catch (OcrConfigurationException) { throw; }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            raw ??= new Bitmap(source); totalWatch.Stop();
            var failed = OcrReadResult.Failed("", "", 0, $"LiveFast OCR başarısız: {exception.Message}");
            var output = new OcrDiagnosticResult(raw, candidates ?? [], null, failed, DateTime.Now, totalWatch.Elapsed)
            {
                ExecutionMode = OcrExecutionMode.LiveFast, PipelineCountExecuted = pipelineCount,
                PrimaryPipeline = primaryPipeline, FallbackUsed = fallbackUsed, HeavyFallbackUsed = heavyFallbackUsed
            };
            raw = null; candidates = null; return output;
        }
        finally
        {
            raw?.Dispose(); if (candidates is not null) foreach (var candidate in candidates) candidate.Dispose(); _engineLock.Release();
        }
    }

    internal static bool IsLiveFastEarlyExitCandidate(OcrCandidateResult candidate, OcrProfile profile, OcrFieldKind field)
    {
        // Torque's small 0.000 label can be read as the upper-bound value 20.000 by the
        // denoised first pipeline. Never let one pipeline end the live cycle early;
        // bounded multi-pipeline arbitration must see the preserved-stroke variants.
        if (field == OcrFieldKind.StableTorque) return false;
        var expectedDecimals = ExpectedDecimalPlaces(field);
        return candidate.IsValid && candidate.Confidence >= profile.LiveFastMinimumConfidence
            && profile.GetLengthRange(field).Contains(candidate.CharacterCount)
            // A decimal field may not early-exit on a unit/edge glyph interpreted as an extra
            // decimal digit (real RPM feedback: "2.93 r" -> 2.936). Continue to the bounded
            // fallback candidates; do not mutate or truncate the OCR value.
            && (expectedDecimals == 0 || HasExpectedDecimalStructure(candidate.CleanedText, expectedDecimals));
    }

    internal static int PlannedPipelineExecutionCountForTest(params bool[] accepted) =>
        accepted.Length == 0 ? 0 : Array.FindIndex(accepted, value => value) is var index && index >= 0 ? index + 1 : accepted.Length;

    internal static bool NeedsAdvanceTargetedFallback(IReadOnlyList<OcrCandidateResult> candidates,
        decimal maximumValue, OcrProfile profile) => !candidates.Any(candidate =>
            candidate.IsValid && candidate.ParsedValue is { } value
            && value >= profile.AdvanceMinimumValidValue && value <= maximumValue
            && profile.GetLengthRange(OcrFieldKind.Advance).Contains(candidate.CharacterCount)
            && !candidate.CleanedText.Contains('.') && !candidate.CleanedText.Contains(','));

    internal static bool NeedsDecimalTargetedFallback(IReadOnlyList<OcrCandidateResult> candidates,
        OcrFieldKind field)
    {
        var decimalPlaces = ExpectedDecimalPlaces(field);
        return decimalPlaces <= 0 || !candidates.Any(candidate => candidate.IsValid
            && candidate.ParsedValue.HasValue
            && HasExpectedDecimalStructure(candidate.CleanedText, decimalPlaces));
    }

    internal static bool NeedsTorqueTargetedFallback(IReadOnlyList<OcrCandidateResult> candidates, decimal maximumValue)
    {
        var structured = candidates.Where(candidate => candidate.IsValid && candidate.ParsedValue.HasValue
            && HasExpectedDecimalStructure(candidate.CleanedText, ExpectedDecimalPlaces(OcrFieldKind.StableTorque))).ToArray();
        if (structured.Length == 0) return true;
        var distinct = structured.Select(candidate => candidate.ParsedValue!.Value).Distinct().ToArray();
        // A value exactly at the physical maximum is a known 0.000 -> 20.000 HMI glyph
        // confusion. It needs independent preserved-stroke evidence before acceptance.
        var boundaryOnlyOrConflict = distinct.Length > 1 || distinct.Contains(maximumValue);
        if (!boundaryOnlyOrConflict) return false;
        var groups = structured.GroupBy(candidate => candidate.ParsedValue!.Value)
            .Select(group => new { Value = group.Key, Pipelines = group.Select(candidate => candidate.ProcessingMethod).Distinct().Count() })
            .OrderByDescending(group => group.Pipelines).ToArray();
        return groups.Length == 0 || groups[0].Pipelines < 2 || groups.Count(group => group.Pipelines == groups[0].Pipelines) > 1;
    }

    internal static bool ShouldRunPostFastCharacterPipeline(OcrFieldKind field, bool acceptedFastCandidate, bool hasSegmentCrop)
    {
        if (!hasSegmentCrop) return false;
        var diagnosticNumeric = field is OcrFieldKind.StableRpm or OcrFieldKind.StableTorque or OcrFieldKind.StableThrustForce
            or OcrFieldKind.StableAdvanceSpeed or OcrFieldKind.StablePenetration
            or OcrFieldKind.NominalInjection or OcrFieldKind.RealInjection
            or OcrFieldKind.Epb1 or OcrFieldKind.Epb2 or OcrFieldKind.Epb3 or OcrFieldKind.Epb4 or OcrFieldKind.Epb5;
        return !diagnosticNumeric || !acceptedFastCandidate;
    }

    public async Task<OcrDiagnosticResult> ReadNumberWithDiagnosticsAsync(Bitmap source, decimal maximumValue,
        OcrProfile profile, OcrFieldKind field, CancellationToken token, IProgress<OcrTestProgress>? progress)
    {
        ObjectDisposedException.ThrowIf(_disposed, this); ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(profile);
        await _engineLock.WaitAsync(token).ConfigureAwait(false);
        Bitmap? raw = null; List<OcrCandidateResult>? candidates = null;
        var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            raw = new Bitmap(source); EnsureEngine(); candidates = [];
            var preprocessingWatch = System.Diagnostics.Stopwatch.StartNew();
            var createdVariants = OcrImageVariantFactory.Create(source, profile, field);
            preprocessingWatch.Stop();
            using var variants = new DisposableVariantCollection(createdVariants);
            var modes = GetPageSegModes(profile, field);
            var characterOcrEnabled = NumericFieldCharacterOcrService.Supports(field)
                && NumericFieldCharacterOcrService.GetOptions(profile, field).Enabled;
            var totalCandidates = variants.Items.Count * modes.Count + (characterOcrEnabled ? 3 : 0);
            var completed = 0;
            var preprocessingShare = TimeSpan.FromTicks(preprocessingWatch.Elapsed.Ticks / Math.Max(1, totalCandidates));
            progress?.Report(new OcrTestProgress(0, totalCandidates, "Görüntüler hazırlandı", totalStopwatch.Elapsed));
            CharacterSegmentationResult? bestGateSegmentation = null;
            try
            {
                foreach (var variant in variants.Items)
                {
                    token.ThrowIfCancellationRequested();
                    var segmentationWatch = System.Diagnostics.Stopwatch.StartNew();
                    using var segmentation = CharacterSegmenter.Segment(variant.Image, field, CharacterSegmentationOptions.For(profile, field));
                    segmentationWatch.Stop();
                    if (characterOcrEnabled && IsAcceptedCharacterSegmentation(segmentation, profile, field) && segmentation.SegmentCrop is not null
                        && IsBetterCharacterSegmentation(segmentation, bestGateSegmentation, profile, field))
                    {
                        bestGateSegmentation?.Dispose();
                        bestGateSegmentation = CloneSegmentation(segmentation);
                    }
                    foreach (var mode in modes)
                    {
                        token.ThrowIfCancellationRequested();
                        candidates.Add(ReadCandidate(variant, segmentation, mode, maximumValue, profile, field,
                            preprocessingShare, segmentationWatch.Elapsed, token));
                        ReportCandidateProgress(progress, ++completed, totalCandidates, totalStopwatch.Elapsed);
                    }
                }

                if (characterOcrEnabled && bestGateSegmentation?.SegmentCrop is not null)
                {
                    using var cropVariant = new OcrImageVariant("Segment Crop",
                        new Bitmap(bestGateSegmentation.SegmentCrop), new Bitmap(bestGateSegmentation.SegmentCrop));
                    candidates.Add(ReadCandidate(cropVariant, bestGateSegmentation, PageSegMode.SingleWord,
                        maximumValue, profile, field, preprocessingShare, TimeSpan.Zero, token, "Segment Crop + SingleWord"));
                    ReportCandidateProgress(progress, ++completed, totalCandidates, totalStopwatch.Elapsed);
                    candidates.Add(ReadCandidate(cropVariant, bestGateSegmentation, PageSegMode.SingleLine,
                        maximumValue, profile, field, preprocessingShare, TimeSpan.Zero, token, "Segment Crop + SingleLine"));
                    ReportCandidateProgress(progress, ++completed, totalCandidates, totalStopwatch.Elapsed);
                    candidates.Add(ReadSegmentedCharacters(cropVariant, bestGateSegmentation, maximumValue,
                        profile, field, preprocessingShare, token));
                    ReportCandidateProgress(progress, ++completed, totalCandidates, totalStopwatch.Elapsed);
                }
            }
            finally
            {
                bestGateSegmentation?.Dispose();
            }
            var (result, selected) = SelectResult(candidates, profile, field);
            totalStopwatch.Stop();
            var diagnostic = new OcrDiagnosticResult(raw, candidates, selected, result, DateTime.Now, totalStopwatch.Elapsed)
            {
                ExecutionMode = OcrExecutionMode.Diagnostic,
                PipelineCountExecuted = variants.Items.Count * modes.Count,
                PrimaryPipeline = variants.Items.Count == 0 ? "" : $"{variants.Items[0].Method}/{modes[0]}"
            };
            raw = null; candidates = null; return diagnostic;
        }
        catch (OcrConfigurationException) { throw; }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            raw ??= new Bitmap(source); var result = OcrReadResult.Failed("", "", 0, $"OCR işlemi başarısız: {exception.Message}");
            var diagnostic = new OcrDiagnosticResult(raw, candidates ?? [], null, result, DateTime.Now); raw = null; candidates = null; return diagnostic;
        }
        finally
        {
            raw?.Dispose(); if (candidates is not null) foreach (var candidate in candidates) candidate.Dispose(); _engineLock.Release();
        }
    }

    private static void ReportCandidateProgress(IProgress<OcrTestProgress>? progress, int completed, int total, TimeSpan elapsed) =>
        progress?.Report(new OcrTestProgress(completed, total, $"{completed} / {total} aday tamamlandı", elapsed));

    private static bool IsBetterCharacterSegmentation(CharacterSegmentationResult candidate,
        CharacterSegmentationResult? current, OcrProfile profile, OcrFieldKind field)
    {
        if (current is null) return true;
        if (field is OcrFieldKind.Nominal or OcrFieldKind.Scale1 or OcrFieldKind.Scale2 or OcrFieldKind.Advance
            && candidate.Characters.Count != current.Characters.Count)
            return candidate.Characters.Count > current.Characters.Count;
        var segmentationOptions = CharacterSegmentationOptions.For(profile, field);
        double Score(CharacterSegmentationResult value) => value.Integrity
            + (profile.GetLengthRange(field).Contains(value.Characters.Count) ? 2 : 0)
            + (field == OcrFieldKind.StableThrustForce
                && value.Characters.Count >= segmentationOptions.ExpectedDigitCountMin
                && value.Characters.Count <= segmentationOptions.ExpectedDigitCountMax ? 3 : 0)
            - (value.HasUnresolvedMergedComponents ? 10 : 0);
        return Score(candidate) > Score(current);
    }

    private static bool IsAcceptedCharacterSegmentation(CharacterSegmentationResult segmentation,
        OcrProfile profile, OcrFieldKind field)
    {
        if (!segmentation.IsSuccessful || segmentation.HasUnresolvedMergedComponents
            || !profile.GetLengthRange(field).Contains(segmentation.Characters.Count)) return false;
        return field == OcrFieldKind.Gate ? segmentation.IsReliable : segmentation.Integrity >= 0.35;
    }

    private static CharacterSegmentationResult CloneSegmentation(CharacterSegmentationResult source) => new()
    {
        Characters = source.Characters.Select(image => new Bitmap(image)).ToArray(),
        RawSegmentCount = source.RawSegmentCount,
        RejectedBounds = source.RejectedBounds.ToArray(),
        AcceptedBounds = source.AcceptedBounds.ToArray(),
        DecimalSeparatorIndexes = source.DecimalSeparatorIndexes.ToHashSet(),
        Visualization = new Bitmap(source.Visualization),
        SegmentCrop = source.SegmentCrop is null ? null : new Bitmap(source.SegmentCrop),
        UnionBounds = source.UnionBounds,
        Integrity = source.Integrity
        ,DetectedComponentCount = source.DetectedComponentCount
        ,MergedComponentCount = source.MergedComponentCount
        ,SplitAttemptCount = source.SplitAttemptCount
        ,ProjectionSplitCount = source.ProjectionSplitCount
        ,ProjectionValleys = source.ProjectionValleys.ToArray()
        ,SegmentSources = source.SegmentSources.ToArray()
        ,HasUnresolvedMergedComponents = source.HasUnresolvedMergedComponents
        ,SegmentationFallbackUsed = source.SegmentationFallbackUsed
        ,ProcessingElapsed = source.ProcessingElapsed
    };

    private static IReadOnlyList<PageSegMode> GetPageSegModes(OcrProfile profile, OcrFieldKind field) =>
        profile.GetLengthRange(field).Maximum == 1
            ? [PageSegMode.SingleChar]
            : [PageSegMode.SingleWord, PageSegMode.SingleLine];

    private OcrCandidateResult ReadCandidate(OcrImageVariant variant, CharacterSegmentationResult segmentation,
        PageSegMode mode, decimal maximum, OcrProfile profile, OcrFieldKind field,
        TimeSpan preprocessingElapsed, TimeSpan segmentationElapsed, CancellationToken token,
        string? processingMethod = null)
    {
        var totalWatch = System.Diagnostics.Stopwatch.StartNew();
        var ocrWatch = new System.Diagnostics.Stopwatch();
        try
        {
            token.ThrowIfCancellationRequested();
            ocrWatch.Start();
            using var pix = PixConverter.ToPix(variant.Image); using var page = _engine!.Process(pix, mode);
            var parsed = Normalize(page.GetText(), page.GetMeanConfidence(), maximum, profile, field);
            ocrWatch.Stop(); token.ThrowIfCancellationRequested(); totalWatch.Stop();
            return NewCandidate(variant, segmentation, mode, parsed, field, false,
                preprocessingElapsed, ocrWatch.Elapsed, segmentationElapsed,
                totalWatch.Elapsed + preprocessingElapsed + segmentationElapsed, processingMethod);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            ocrWatch.Stop(); totalWatch.Stop();
            return NewCandidate(variant, segmentation, mode, OcrReadResult.Failed("", "", 0, exception.Message), field, false,
                preprocessingElapsed, ocrWatch.Elapsed, segmentationElapsed,
                totalWatch.Elapsed + preprocessingElapsed + segmentationElapsed, processingMethod);
        }
    }

    private OcrCandidateResult ReadSegmentedCharacters(OcrImageVariant variant,
        CharacterSegmentationResult segmentation, decimal maximum, OcrProfile profile, OcrFieldKind field,
        TimeSpan preprocessingElapsed, CancellationToken token)
    {
        var totalWatch = System.Diagnostics.Stopwatch.StartNew();
        var ocrWatch = System.Diagnostics.Stopwatch.StartNew();
        var text = new System.Text.StringBuilder();
        var confidences = new List<float>();
        var diagnostics = new List<CharacterOcrDiagnostic>();
        var templateElapsed = TimeSpan.Zero;
        try
        {
            var options = NumericFieldCharacterOcrService.GetOptions(profile, field);
            _engine!.SetVariable("tessedit_char_whitelist", "0123456789");
            for (var index = 0; index < segmentation.Characters.Count; index++)
            {
                token.ThrowIfCancellationRequested();
                var character = segmentation.Characters[index];
                if (segmentation.DecimalSeparatorIndexes.Contains(index))
                {
                    text.Append('.'); confidences.Add(1f);
                    diagnostics.Add(new CharacterOcrDiagnostic
                    {
                        CharacterIndex = index + 1, OriginalSegmentImage = new Bitmap(character),
                        CleanedSegmentImage = new Bitmap(character), TightCroppedImage = new Bitmap(character),
                        NormalizedOcrInputImage = new Bitmap(character), RawOcrText = ".", CleanedDigit = ".",
                        OcrConfidence = 1f, GeometryResult = "Ondalık ayıracı", GeometryConfidence = 1,
                        FinalDigit = ".", DecisionReason = "Küçük alt bileşen ondalık ayıracı geometrisiyle eşleşti.",
                        DecisionSource = "Ondalık ayıracı geometrisi", GeometryContributed = true
                    });
                    continue;
                }
                using var normalized = CharacterOcrNormalizer.NormalizeCharacterForOcr(character,
                    maximumAllowedInkLoss: options.MaximumAllowedInkLoss);
                if (!normalized.IsValid)
                {
                    diagnostics.Add(new CharacterOcrDiagnostic
                    {
                        CharacterIndex = index + 1, OriginalSegmentImage = new Bitmap(character),
                        CleanedSegmentImage = new Bitmap(normalized.Cleaned), TightCroppedImage = new Bitmap(normalized.TightCropped),
                        NormalizedOcrInputImage = new Bitmap(normalized.Normalized), RawOcrText = "", CleanedDigit = "",
                        OcrConfidence = 0, GeometryResult = "Geçersiz temizlenmiş segment", FinalDigit = "",
                        DecisionReason = "Temizleme sonrasında geçerli rakam bileşeni kalmadı."
                    });
                    continue;
                }
                using var pix = PixConverter.ToPix(normalized.Normalized);
                using var page = _engine.Process(pix, PageSegMode.SingleChar);
                var rawOcrText = page.GetText() ?? string.Empty;
                var digit = rawOcrText.FirstOrDefault(char.IsDigit);
                var tesseractConfidence = page.GetMeanConfidence();
                var geometry = options.EnableGeometry
                    ? GateDigitGeometryAnalyzer.Analyze(normalized.Normalized)
                    : new GateDigitGeometryResult(null, 0, false, false, false, "Karakter geometri kontrolü kapalı.");
                var templateFolder = options.TemplateFolder;
                var selectedImageSource = normalized.UsedOriginalFallback
                    ? "Orijinal karakterden normalize OCR girdisi"
                    : "Temizlenmiş karakterden normalize OCR girdisi";
                var templateWatch = System.Diagnostics.Stopwatch.StartNew();
                var template = options.Enabled
                    ? DigitTemplateMatcher.Match(normalized.Normalized, templateFolder, options.MaximumAllowedInkLoss)
                    : null;
                templateWatch.Stop(); templateElapsed += templateWatch.Elapsed;
                if (options.Enabled && normalized.RemovedInkRatio > options.MaximumAllowedInkLoss)
                {
                    templateWatch.Restart();
                    var tightTemplate = DigitTemplateMatcher.Match(normalized.TightCropped, templateFolder, options.MaximumAllowedInkLoss);
                    templateWatch.Stop();
                    templateElapsed += templateWatch.Elapsed;
                    if (tightTemplate is not null && (template is null || tightTemplate.Score > template.Score))
                    {
                        template = tightTemplate;
                        selectedImageSource = "Sıkı kırpılmış karakter şablon girdisi";
                    }
                }
                var decision = ResolveCharacterDecision(digit, tesseractConfidence, template, geometry, options);
                DigitTemplateUsageTracker.RecordOutcome(template?.TemplatePath,
                    decision.TemplateContributed && decision.FinalDigit == template?.Digit,
                    template is not null && !decision.TemplateContributed && digit != default && digit != template.Digit);
                var templateAuthoritative = decision.TemplateAuthoritative;
                var templateHighConfidence = decision.TemplateHighConfidence;
                var templateGeometryAgreement = decision.TemplateGeometryAgreement;
                var templateAccepted = decision.TemplateContributed;
                var combined = decision.FinalDigit;
                if (combined != default) text.Append(combined);
                var combinedConfidence = decision.Confidence;
                confidences.Add((float)combinedConfidence);
                diagnostics.Add(new CharacterOcrDiagnostic
                {
                    CharacterIndex = index + 1, OriginalSegmentImage = new Bitmap(character),
                    CleanedSegmentImage = new Bitmap(normalized.Cleaned), TightCroppedImage = new Bitmap(normalized.TightCropped),
                    NormalizedOcrInputImage = new Bitmap(normalized.Normalized), RawOcrText = rawOcrText,
                    CleanedDigit = digit == default ? "" : digit.ToString(), OcrConfidence = tesseractConfidence,
                    TemplateResult = template?.Digit.ToString(), TemplateScore = template?.Score ?? 0,
                    SecondBestTemplateResult = template?.SecondBestDigit?.ToString(),
                    SecondBestTemplateScore = template?.SecondBestScore ?? 0,
                    TemplateScoreMargin = template?.ScoreMargin ?? 0,
                    IsTemplateConfident = templateAccepted,
                    InkLossRatio = normalized.RemovedInkRatio, SelectedImageSource = selectedImageSource,
                    TesseractInputSource = normalized.UsedOriginalFallback ? "Orijinal karakterden normalize Tesseract girdisi" : "Temizlenmiş karakterden normalize Tesseract girdisi",
                    TemplateInputSource = selectedImageSource,
                    GeometryInputSource = normalized.UsedOriginalFallback ? "Orijinal karakterden normalize geometri girdisi" : "Temizlenmiş karakterden normalize geometri girdisi",
                    IsTemplateAuthoritative = templateAuthoritative,
                    TemplateContributed = decision.TemplateContributed, TesseractContributed = decision.TesseractContributed,
                    GeometryContributed = decision.GeometryContributed, DecisionSource = decision.Source,
                    GeometryResult = geometry.SuggestedDigit?.ToString() ?? "—",
                    GeometryConfidence = geometry.Confidence,
                    SegmentBounds = index < segmentation.AcceptedBounds.Count ? segmentation.AcceptedBounds[index] : Rectangle.Empty,
                    SplitSource = index < segmentation.SegmentSources.Count ? segmentation.SegmentSources[index] : "ConnectedComponent",
                    FinalDigit = combined == default ? "" : combined.ToString(),
                    DecisionReason = $"{geometry.Description} Temizlikte mürekkep kaybı=%{normalized.RemovedInkRatio * 100:0.0}"
                        + (normalized.UsedOriginalFallback ? $" ve %{options.MaximumAllowedInkLoss * 100:0} sınırı aşıldığı için orijinal sıkı kırpmaya dönüldü." : ".")
                        + $" Şablon={(template is null ? "yok" : $"{template.Digit} (%{template.Score * 100:0.0}), ikinci={template.SecondBestDigit?.ToString() ?? "—"} (%{template.SecondBestScore * 100:0.0}), fark=%{template.ScoreMargin * 100:0.0}")};"
                        + (templateAuthoritative ? $" %{options.AuthoritativeScore * 100:0} ve üzeri şablon otoriter seçildi; Tesseract yalnızca bilgi amaçlıdır." : templateHighConfidence ? " Yüksek güvenli şablon seçildi." : templateGeometryAgreement ? " Şablon ve geometri uzlaşısı seçildi." : decision.Source == "Geometri düzeltmesi" ? " Çok yüksek güvenli geometri Tesseract sonucunu düzeltti." : template is not null ? " Şablon düşük güvenli/belirsiz; Tesseract geri dönüşü kullanıldı." : "Alan rakam şablonları eksik; Tesseract geri dönüşü kullanıldı.")
                        + $" Kullanılan görüntü={selectedImageSource}. Karar kaynağı={decision.Source}."
                        + $" Tesseract={(digit == default ? "okunamadı" : digit)} (%{tesseractConfidence * 100:0.0}); birleşik={(combined == default ? "okunamadı" : combined)}."
                });
            }
            ocrWatch.Stop(); totalWatch.Stop();
            var confidence = confidences.Count == 0 ? 0 : confidences.Average();
            var parsed = Normalize(text.ToString(), confidence, maximum, profile, field);
            return NewCandidate(variant, segmentation, PageSegMode.SingleChar, parsed, field, true,
                preprocessingElapsed, ocrWatch.Elapsed, segmentation.ProcessingElapsed,
                totalWatch.Elapsed + preprocessingElapsed + segmentation.ProcessingElapsed, "Segmented Characters", diagnostics, templateElapsed);
        }
        catch (OperationCanceledException)
        {
            foreach (var diagnostic in diagnostics) diagnostic.Dispose();
            throw;
        }
        catch (Exception exception)
        {
            ocrWatch.Stop(); totalWatch.Stop();
            return NewCandidate(variant, segmentation, PageSegMode.SingleChar,
                OcrReadResult.Failed(text.ToString(), text.ToString(), 0, exception.Message), field, true,
                preprocessingElapsed, ocrWatch.Elapsed, TimeSpan.Zero,
                totalWatch.Elapsed + preprocessingElapsed + segmentation.ProcessingElapsed, "Segmented Characters", diagnostics);
        }
        finally
        {
            _engine!.SetVariable("tessedit_char_whitelist", "0123456789,.-");
        }
    }

    private static OcrReadResult Normalize(string? raw, float confidence, decimal maximum, OcrProfile profile, OcrFieldKind field)
    {
        if (field == OcrFieldKind.Gate && raw is not null)
            raw = raw.Replace("-", string.Empty, StringComparison.Ordinal);
        var parsed = MeasurementParser.Parse(raw, confidence, maximum);
        if (profile.PreserveLeadingZeros || string.IsNullOrEmpty(parsed.CleanedText)) return parsed;
        var cleaned = parsed.CleanedText;
        var sign = cleaned.StartsWith('-') ? "-" : ""; var unsigned = cleaned.TrimStart('-');
        var normalized = unsigned.TrimStart('0'); if (normalized.Length == 0 || normalized[0] is '.' or ',') normalized = "0" + normalized;
        return parsed with { CleanedText = sign + normalized };
    }

    internal static GateCharacterDecision ResolveGateCharacterDecision(char tesseractDigit, float tesseractConfidence,
        DigitTemplateMatch? template, GateDigitGeometryResult geometry, OcrProfile profile)
        => ResolveCharacterDecision(tesseractDigit, tesseractConfidence, template, geometry,
            NumericFieldCharacterOcrService.GetOptions(profile, OcrFieldKind.Gate));

    internal static GateCharacterDecision ResolveCharacterDecision(char tesseractDigit, float tesseractConfidence,
        DigitTemplateMatch? template, GateDigitGeometryResult geometry, NumericCharacterOcrOptions options)
    {
        var authoritative = template is not null
            && template.Score >= options.AuthoritativeScore
            && template.ScoreMargin >= options.MinimumMargin;
        var highConfidence = template is not null
            && template.Score >= options.MinimumScore
            && template.ScoreMargin >= options.MinimumMargin;
        var geometryAgreement = template is not null
            && geometry.SuggestedDigit == template.Digit
            && template.Score >= Math.Max(0, options.MinimumScore - 0.10)
            && template.ScoreMargin >= options.MinimumMargin / 2;
        if (authoritative)
            return new(template!.Digit, template.Score, true, false, geometry.SuggestedDigit == template.Digit,
                true, highConfidence, geometryAgreement, "Şablon (otoriter)");
        if (highConfidence || geometryAgreement)
            return new(template!.Digit, template.Score, true, false, geometryAgreement,
                false, highConfidence, geometryAgreement, geometryAgreement ? "Şablon + Geometri" : "Şablon");
        if (geometry.SuggestedDigit is { } geometryDigit && geometry.Confidence >= 0.90
            && geometryDigit != tesseractDigit)
            return new(geometryDigit, geometry.Confidence, false, false, true,
                false, false, false, "Geometri düzeltmesi");
        return new(tesseractDigit, tesseractConfidence, false, true, false,
            false, false, false, "Tesseract geri dönüşü");
    }

    private static OcrCandidateResult NewCandidate(OcrImageVariant variant, CharacterSegmentationResult segmentation,
        PageSegMode mode, OcrReadResult result, OcrFieldKind field, bool segmented,
        TimeSpan preprocessingElapsed = default, TimeSpan ocrElapsed = default,
        TimeSpan segmentationElapsed = default, TimeSpan totalElapsed = default,
        string? processingMethod = null,
        IReadOnlyList<CharacterOcrDiagnostic>? characterDiagnostics = null,
        TimeSpan templateMatchElapsed = default)
    {
        var metrics = BinaryImageBuilder.Analyze(variant.Image);
        var digitCount = result.CleanedText.Count(char.IsDigit);
        var characterField = NumericFieldCharacterOcrService.Supports(field);
        var acceptedSegmentation = characterField && segmentation.IsSuccessful
            && (field == OcrFieldKind.Gate ? segmentation.IsReliable : segmentation.Integrity >= 0.35);
        var expectedDigitSegments = segmentation.Characters.Count - segmentation.DecimalSeparatorIndexes.Count;
        var segmentMatch = acceptedSegmentation
            ? digitCount == expectedDigitSegments
            : (bool?)null;
        var parsedValue = result.Value;
        var error = result.Error;
        var diagnostics = characterDiagnostics ?? Array.Empty<CharacterOcrDiagnostic>();
        var characterText = string.Concat(diagnostics.Select(c => c.CleanedDigit));
        var combinedText = string.Concat(diagnostics.Select(c => c.FinalDigit));
        var conflictingCharacters = diagnostics.Count(c => c.GeometryResult != "—"
            && c.GeometryResult != c.FinalDigit && !string.IsNullOrEmpty(c.FinalDigit));
        var digitDiagnostics = diagnostics.Where(c => c.FinalDigit != ".").ToArray();
        var separatorsValid = diagnostics.Where(c => c.FinalDigit == ".").All(c => c.GeometryContributed);
        var reliableCharacters = digitDiagnostics.Length > 0 && separatorsValid && digitDiagnostics.All(c =>
            !string.IsNullOrEmpty(c.FinalDigit)
            && (c.IsTemplateConfident
                || c.TemplateResult is null && c.OcrConfidence >= 0.60f
                || c.TemplateResult is null && c.GeometryResult == c.FinalDigit && c.OcrConfidence >= 0.45f));
        var characterDecisionReliable = reliableCharacters && conflictingCharacters == 0;
        var allCharactersTemplateAuthoritative = digitDiagnostics.Length > 0 && separatorsValid && digitDiagnostics.All(c =>
            c.IsTemplateAuthoritative && !string.IsNullOrEmpty(c.FinalDigit));
        var expectedDecimals = ExpectedDecimalPlaces(field);
        if (segmented && parsedValue.HasValue && expectedDecimals > 0
            && !HasExpectedDecimalStructure(result.CleanedText, expectedDecimals))
        {
            error = $"Karakter sonucu beklenen {expectedDecimals} ondalık basamak yapısıyla uyuşmuyor.";
            parsedValue = null;
        }
        var segmentationValidation = ApplyCharacterSegmentationValidation(parsedValue, error, segmentMatch,
            digitCount, expectedDigitSegments, field, segmented);
        parsedValue = segmentationValidation.ParsedValue; error = segmentationValidation.Error;
        return new OcrCandidateResult
        {
            ProcessingMethod = processingMethod ?? (segmented ? "Segmented Characters" : variant.Method),
            PageSegMode = mode, RawText = result.RawText, CleanedText = result.CleanedText, ParsedValue = parsedValue,
            Confidence = result.Confidence, OcrConfidence = result.Confidence, SystemConfidence = 0,
            CharacterCount = digitCount, RawSegmentCount = segmentation.RawSegmentCount,
            SegmentCount = segmentation.Characters.Count, SegmentMatch = segmentMatch,
            LeadingNarrowDigitGeometry = variant.LeadingNarrowDigit,
            TorqueGlyphs = variant.TorqueGlyphs,
            AcceptedSegmentBounds = segmentation.AcceptedBounds.ToArray(),
            RejectedSegmentBounds = segmentation.RejectedBounds.ToArray(),
            CharacterDiagnostics = diagnostics, UsesCharacterBasedOcr = segmented && diagnostics.Count > 0,
            CharacterOcrText = characterText, CombinedText = combinedText,
            CharacterDecisionReliable = characterDecisionReliable,
            AllCharactersTemplateAuthoritative = allCharactersTemplateAuthoritative,
            CharacterDecisionSummary = diagnostics.Count == 0 ? "Karakter tanılaması yok."
                : characterDecisionReliable ? "Tüm karakterler güvenilir ve birbiriyle uyumlu."
                : $"Karakter adayı düşük güvenli veya çelişkili ({conflictingCharacters} geometri çelişkisi).",
            TemplateWarning = segmented && diagnostics.Count > 0 && diagnostics.All(c => c.TemplateResult is null)
                ? "Alan rakam şablonları eksik"
                : null,
            SegmentationSuccessful = acceptedSegmentation,
            DetectedComponentCount = segmentation.DetectedComponentCount,
            MergedComponentCount = segmentation.MergedComponentCount,
            SplitAttemptCount = segmentation.SplitAttemptCount,
            ProjectionSplitCount = segmentation.ProjectionSplitCount,
            ProjectionValleys = segmentation.ProjectionValleys.ToArray(),
            HasUnresolvedMergedComponents = segmentation.HasUnresolvedMergedComponents,
            SegmentationFallbackUsed = segmentation.SegmentationFallbackUsed,
            CharacterValidationStatus = segmentationValidation.Status,
            CharacterValidationWarning = segmentationValidation.Warning,
            BinaryQuality = metrics.Quality, NoiseRatio = metrics.NoiseRatio, CharacterIntegrity = segmentation.Integrity,
            PreprocessingElapsed = preprocessingElapsed, OcrElapsed = ocrElapsed,
            SegmentationElapsed = segmentationElapsed, TotalElapsed = totalElapsed,
            TemplateMatchElapsed = templateMatchElapsed,
            ImageSize = variant.Image.Size, ProcessedImage = CloneForDiagnostics(variant.ProcessedImage),
            BinaryImage = CloneForDiagnostics(variant.Image), SegmentationImage = CloneForDiagnostics(segmentation.Visualization), Error = error
        };
    }

    internal static int ExpectedDecimalPlaces(OcrFieldKind field) => field switch
    {
        OcrFieldKind.StableRpm => 2,
        OcrFieldKind.StableTorque => 3,
        OcrFieldKind.StableAdvanceSpeed or OcrFieldKind.StablePenetration => 1,
        OcrFieldKind.NominalInjection or OcrFieldKind.RealInjection => 1,
        OcrFieldKind.Epb1 or OcrFieldKind.Epb2 or OcrFieldKind.Epb3 or OcrFieldKind.Epb4 or OcrFieldKind.Epb5 => 2,
        _ => 0
    };

    internal static (decimal? ParsedValue, string? Error, string Status, string? Warning) ApplyCharacterSegmentationValidation(
        decimal? parsedValue, string? error, bool? segmentMatch, int ocrDigits, int segmentDigits,
        OcrFieldKind field, bool characterCandidate)
    {
        if (!parsedValue.HasValue || segmentMatch != false)
            return (parsedValue, error, segmentMatch == true ? "Matched" : "NotAvailable", null);
        var message = $"OCR rakam sayısı ({ocrDigits}), segmentasyon rakam sayısıyla ({segmentDigits}) uyuşmuyor.";
        var diagnosticStableField = field is OcrFieldKind.StableRpm or OcrFieldKind.StableTorque
            or OcrFieldKind.StableThrustForce or OcrFieldKind.StableAdvanceSpeed or OcrFieldKind.StablePenetration
            or OcrFieldKind.NominalInjection or OcrFieldKind.RealInjection
            or OcrFieldKind.Epb1 or OcrFieldKind.Epb2 or OcrFieldKind.Epb3 or OcrFieldKind.Epb4 or OcrFieldKind.Epb5;
        if (diagnosticStableField && !characterCandidate)
            return (parsedValue, error, "Mismatch", "Full-line OCR geçerli; karakter segmentasyonu doğrulayamadı. " + message);
        return (null, message, "Mismatch", message);
    }

    internal static bool HasExpectedDecimalStructure(string text, int decimalPlaces)
    {
        if (decimalPlaces <= 0) return !text.Contains('.') && !text.Contains(',');
        var separator = Math.Max(text.LastIndexOf('.'), text.LastIndexOf(','));
        return separator > 0 && text.Length - separator - 1 == decimalPlaces
            && text[(separator + 1)..].All(char.IsDigit);
    }

    private static Bitmap CloneForDiagnostics(Bitmap source)
    {
        const int maximumWidth = 1000; const int maximumHeight = 500;
        var scale = Math.Min(1d, Math.Min(maximumWidth / (double)source.Width, maximumHeight / (double)source.Height));
        return scale >= 1d ? new Bitmap(source) : new Bitmap(source,
            Math.Max(1, (int)(source.Width * scale)), Math.Max(1, (int)(source.Height * scale)));
    }

    internal static (OcrReadResult Result, OcrCandidateResult? SelectedCandidate) SelectResult(
        IReadOnlyList<OcrCandidateResult> candidates, OcrProfile? profile = null, OcrFieldKind field = OcrFieldKind.General)
    {
        profile ??= new OcrProfile(); var range = profile.GetLengthRange(field); var valid = candidates.Where(c => c.IsValid).ToList();
        if (field == OcrFieldKind.Advance && TrySelectAdvanceWithoutRightEdgeNoise(candidates, out var corrected, out var correctedCandidate))
            return (corrected, correctedCandidate);
        var totalMethods = candidates.Select(c => BaseMethod(c.ProcessingMethod)).Distinct().Count();
        foreach (var group in valid.GroupBy(c => c.ParsedValue!.Value))
        {
            var methods = group.Select(c => BaseMethod(c.ProcessingMethod)).Distinct().Count();
            foreach (var candidate in group)
            {
                var lengthValid = field == OcrFieldKind.General || range.Contains(candidate.CharacterCount);
                var independentMethodEvidence = NumericFieldCharacterOcrService.Supports(field) && !candidate.UsesCharacterBasedOcr ? 1 : methods;
                candidate.SystemConfidence = OcrConfidenceCalculator.Calculate(candidate.OcrConfidence, independentMethodEvidence, totalMethods,
                    lengthValid, candidate.SegmentationSuccessful, candidate.BinaryQuality, candidate.NoiseRatio, candidate.CharacterIntegrity);
                candidate.Confidence = candidate.SystemConfidence;
            }
        }
        if (ExpectedDecimalPlaces(field) > 0
            && DecimalNumericCandidateDecisionService.TrySelect(candidates, field, out var decimalDecision))
            return (decimalDecision!.Result, decimalDecision.SelectedCandidate);
        if (field == OcrFieldKind.StableTorque)
        {
            var candidate = candidates.Where(item => !item.UsesCharacterBasedOcr && item.ParsedValue.HasValue)
                .OrderByDescending(item => item.OcrConfidence).FirstOrDefault();
            return (OcrReadResult.Failed(candidate?.RawText ?? "", candidate?.CleanedText ?? "",
                candidate?.OcrConfidence ?? 0, "Tork sonucu beklenen #.### ondalık yapısında değil."), candidate);
        }
        if (IsStableNumericField(field)
            && TrySelectStableZeroValue(candidates, field, out var zeroResult, out var zeroCandidate))
            return (zeroResult, zeroCandidate);
        if (field == OcrFieldKind.StableThrustForce
            && TrySelectStableThrustDecision(candidates, valid, out var thrustResult, out var thrustCandidate))
            return (thrustResult, thrustCandidate);
        if (field == OcrFieldKind.StableThrustForce)
            valid = candidates.Where(candidate => candidate.IsValid).ToList();
        if (valid.Count == 0)
        {
            var reasons = candidates.Select(c => c.Error).Where(e => !string.IsNullOrWhiteSpace(e)).Distinct().Take(3);
            return (OcrReadResult.Failed("", "", 0, $"Hiçbir OCR adayı kabul edilmedi: {string.Join(" | ", reasons)}"), null);
        }
        var groups = valid.GroupBy(c => c.ParsedValue!.Value).Select(group => new
        {
            Value = group.Key, Candidates = group.ToList(),
            Votes = group.Select(c => BaseMethod(c.ProcessingMethod)).Distinct().Count(),
            Confidence = group.Average(c => c.SystemConfidence),
            OcrConfidence = group.Average(c => c.OcrConfidence),
            SegmentRank = group.Max(c => c.SegmentMatch == true ? 2 : c.SegmentMatch is null ? 1 : 0),
            HasAuthoritativeCharacterCandidate = group.Any(c => c.UsesCharacterBasedOcr && c.AllCharactersTemplateAuthoritative),
            HasReliableCharacterCandidate = group.Any(c => c.UsesCharacterBasedOcr && c.CharacterDecisionReliable)
        }).ToList();
        var characterField = NumericFieldCharacterOcrService.Supports(field);
        var winner = characterField
            ? groups.OrderByDescending(g => g.SegmentRank)
                .ThenByDescending(g => g.HasAuthoritativeCharacterCandidate)
                .ThenByDescending(g => g.HasReliableCharacterCandidate)
                .ThenByDescending(g => g.Votes)
                .ThenByDescending(g => g.Confidence)
                .ThenByDescending(g => g.OcrConfidence).First()
            : groups.OrderByDescending(g => g.Votes).ThenByDescending(g => g.Confidence).First();
        var selected = winner.Candidates
            .OrderByDescending(c => characterField && c.UsesCharacterBasedOcr && c.AllCharactersTemplateAuthoritative)
            .ThenByDescending(c => characterField && c.UsesCharacterBasedOcr && c.CharacterDecisionReliable)
            .ThenByDescending(c => c.SystemConfidence)
            .ThenByDescending(c => c.OcrConfidence).First();
        var rejectedCharacter = characterField
            ? valid.FirstOrDefault(c => c.UsesCharacterBasedOcr && !c.CharacterDecisionReliable && c.ParsedValue != winner.Value)
            : null;
        var templateGeometryConsensus = selected.UsesCharacterBasedOcr
            && selected.CharacterDiagnostics.Count > 0
            && selected.CharacterDiagnostics.All(character => character.IsTemplateConfident)
            && selected.CharacterDiagnostics.Any(character => character.GeometryResult == character.FinalDigit);
        var reason = winner.HasAuthoritativeCharacterCandidate
            ? $"Nihai sonuç {winner.Value:0.##}: segmentasyondaki tüm karakterler otoriter şablon eşiğini geçti; tam satır Tesseract çoğunluğu kararı değiştiremez."
            : winner.HasReliableCharacterCandidate
            ? templateGeometryConsensus
                ? $"Nihai sonuç {winner.Value:0.##}: şablon eşleştirme ve geometri uzlaşısıyla seçildi."
                : $"Nihai sonuç {winner.Value:0.##}: yüksek güvenli şablon eşleştirmeyle seçildi."
            : winner.Votes > 1
                ? $"Kabul: {winner.Value:0.##}, {winner.Votes} Tesseract ön işleme tekrarındaki çoğunluk nedeniyle seçildi; bunlar bağımsız tanıma kanıtı sayılmadı."
                    + (rejectedCharacter is null ? "" : $" Karakter bazlı {rejectedCharacter.ParsedValue:0.##} sonucu düşük güvenli/çelişkili olduğu için öncelik almadı.")
                : $"Kabul: {winner.Value:0.##}, en yüksek sistem ve OCR güveniyle seçildi.";
        var characterWarning = winner.Candidates.Select(candidate => candidate.CharacterValidationWarning)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        if (!string.IsNullOrWhiteSpace(characterWarning) && !winner.HasReliableCharacterCandidate)
            reason = $"Nihai sonuç {winner.Value:0.##}: geçerli full-line OCR kullanıldı. Uyarı: {characterWarning}";
        return (new OcrReadResult(winner.Value, selected.RawText, selected.CleanedText, winner.Confidence, null)
            { SelectionReason = reason, Warning = characterWarning }, selected);
    }

    internal static bool TrySelectStableZeroValue(IReadOnlyList<OcrCandidateResult> candidates, OcrFieldKind field,
        out OcrReadResult result, out OcrCandidateResult? selected)
    {
        result = OcrReadResult.Failed("", "", 0, "Geçerli sıfır adayı bulunamadı."); selected = null;
        if (!IsStableNumericField(field)) return false;
        var expectedDecimals = ExpectedDecimalPlaces(field);
        // Decimal zero is a normal candidate. It must never bypass decimal structure and consensus arbitration.
        if (expectedDecimals > 0) return false;
        var zeroCandidates = candidates.Where(candidate => !candidate.UsesCharacterBasedOcr)
            .Select(candidate => new { Candidate = candidate, Parsed = ParseStableNumericText(candidate.CleanedText) })
            .Where(item => item.Parsed.HasValue && item.Parsed.Value == 0
                && (expectedDecimals == 0 || HasExpectedDecimalStructure(item.Candidate.CleanedText, expectedDecimals)))
            .OrderByDescending(item => item.Candidate.OcrConfidence).ToArray();
        if (zeroCandidates.Length == 0 && expectedDecimals > 0)
            zeroCandidates = candidates.Where(candidate => !candidate.UsesCharacterBasedOcr)
                .Select(candidate => new { Candidate = candidate, Parsed = ParseStableNumericText(candidate.CleanedText) })
                .Where(item => item.Parsed.HasValue && item.Parsed.Value == 0)
                .OrderByDescending(item => item.Candidate.OcrConfidence).ToArray();
        if (zeroCandidates.Length == 0) return false;
        if (field == OcrFieldKind.StableThrustForce && EstimateThrustDigitCount(candidates) > 1) return false;
        selected = zeroCandidates[0].Candidate; selected.ParsedValue = 0; selected.Error = null;
        var warning = selected.SegmentMatch == false
            ? "Karakter segmentasyonu sonucu doğrulayamadı; geçerli full-line sıfır değeri kullanıldı."
            : null;
        result = new OcrReadResult(0m, selected.RawText, selected.CleanedText, selected.OcrConfidence, null)
        {
            Warning = warning, DecisionSource = "ValidFullLineZero",
            SelectionReason = "Geçerli full-line numeric sıfır değeri seçildi."
                + (warning is null ? "" : $" Uyarı: {warning}"),
            SelectedCandidateDigitCount = selected.CharacterCount,
            DigitPlausibility = "ValidZero"
        };
        return true;
    }

    private static decimal? ParseStableNumericText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var normalized = text.Replace(',', '.');
        return decimal.TryParse(normalized, System.Globalization.NumberStyles.AllowDecimalPoint,
            System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    private static bool IsStableNumericField(OcrFieldKind field) => field is OcrFieldKind.StableRpm
        or OcrFieldKind.StableTorque or OcrFieldKind.StableThrustForce
        or OcrFieldKind.StableAdvanceSpeed or OcrFieldKind.StablePenetration
        or OcrFieldKind.NominalInjection or OcrFieldKind.RealInjection
        or OcrFieldKind.Epb1 or OcrFieldKind.Epb2 or OcrFieldKind.Epb3 or OcrFieldKind.Epb4 or OcrFieldKind.Epb5;

    internal static bool TrySelectStableThrustDecision(IReadOnlyList<OcrCandidateResult> candidates,
        IReadOnlyList<OcrCandidateResult> validCandidates,
        out OcrReadResult result, out OcrCandidateResult? selected)
    {
        result = OcrReadResult.Failed("", "", 0, "Yapısal olarak geçerli thrust adayı bulunamadı."); selected = null;
        var estimatedDigits = EstimateThrustDigitCount(candidates);
        var authoritativeCharacter = validCandidates.FirstOrDefault(candidate => candidate.UsesCharacterBasedOcr
            && candidate.AllCharactersTemplateAuthoritative && candidate.CharacterDecisionReliable
            && candidate.SegmentMatch == true && IsPlausibleThrustValue(candidate.ParsedValue, candidate.CharacterCount, estimatedDigits));
        if (authoritativeCharacter is not null) return false;

        var fullLines = candidates.Where(candidate => !candidate.UsesCharacterBasedOcr)
            .Select(candidate => new { Candidate = candidate, Parsed = ParseThrustCandidate(candidate) })
            .Where(item => item.Parsed.HasValue && item.Parsed is >= 0 and <= 100000)
            .ToArray();
        foreach (var item in fullLines)
        {
            item.Candidate.EstimatedDigitCount = estimatedDigits;
            var plausible = IsPlausibleThrustValue(item.Parsed, item.Candidate.CharacterCount, estimatedDigits);
            item.Candidate.DigitPlausibility = plausible ? "Plausible" : "DigitCountMismatch";
            item.Candidate.PlausibilityScore = ThrustPlausibilityScore(item.Candidate, plausible, estimatedDigits);
            if (!plausible)
            {
                item.Candidate.Error = "DigitCountMismatch: aday hane sayısı görüntü geometrisiyle uyuşmuyor.";
                item.Candidate.ParsedValue = null;
            }
        }
        var plausibleLines = fullLines.Where(item => item.Candidate.DigitPlausibility == "Plausible"
                && (item.Candidate.OcrConfidence >= .10f || item.Candidate.ParsedValue.HasValue))
            .ToArray();
        var fullLineGroups = plausibleLines
            .GroupBy(item => item.Parsed!.Value)
            .Select(group => new
            {
                Value = group.Key, Candidates = group.Select(item => item.Candidate).ToArray(),
                IndependentPipelines = group.Select(item => BaseMethod(item.Candidate.ProcessingMethod)).Distinct().Count(),
                AverageConfidence = group.Average(item => item.Candidate.OcrConfidence),
                Score = group.Max(item => item.Candidate.PlausibilityScore)
            })
            .OrderByDescending(group => group.IndependentPipelines).ThenByDescending(group => group.Score)
            .ThenByDescending(group => group.AverageConfidence).ToArray();
        var consensus = fullLineGroups.FirstOrDefault();
        if (consensus is null) return false;

        var availablePipelineCount = candidates.Where(candidate => !candidate.UsesCharacterBasedOcr)
            .Select(candidate => BaseMethod(candidate.ProcessingMethod)).Distinct().Count();
        var strongThreshold = availablePipelineCount <= 3 ? 2 : 3;
        var level = consensus.IndependentPipelines >= strongThreshold ? "StrongConsensus"
            : consensus.IndependentPipelines == 2 ? "MediumConsensus" : "SingleCandidate";
        var expectedCharacter = validCandidates.Where(candidate => candidate.UsesCharacterBasedOcr)
            .OrderByDescending(candidate => candidate.AllCharactersTemplateAuthoritative)
            .ThenByDescending(candidate => candidate.CharacterDecisionReliable).ThenByDescending(candidate => candidate.SystemConfidence)
            .FirstOrDefault();
        var anyCharacter = candidates.Where(candidate => candidate.UsesCharacterBasedOcr)
            .OrderByDescending(candidate => candidate.AllCharactersTemplateAuthoritative)
            .ThenByDescending(candidate => candidate.CharacterDecisionReliable).FirstOrDefault();
        var character = expectedCharacter ?? anyCharacter;
        var characterValue = character?.ParsedValue;
        var characterComplete = character is not null && character.SegmentMatch == true
            && IsPlausibleThrustValue(character.ParsedValue, character.CharacterCount, estimatedDigits)
            && character.CharacterDecisionReliable;
        var characterAuthoritative = characterComplete && character!.AllCharactersTemplateAuthoritative;

        if (characterAuthoritative && characterValue != consensus.Value) return false;
        if (characterComplete && characterValue != consensus.Value && level != "StrongConsensus") return false;

        selected = consensus.Candidates.OrderByDescending(candidate => candidate.OcrConfidence).First();
        selected.ParsedValue = consensus.Value; selected.Error = null;
        var agrees = characterValue == consensus.Value && characterComplete;
        var strong = level == "StrongConsensus";
        var warning = agrees ? null : strong
            ? "Karakter segmentasyonu full-line çoğunluğu ile uyuşmadı."
            : "Karakter segmentasyonu doğrulayamadı; yapısal olarak geçerli full-line OCR kullanıldı.";
        var characterStatus = agrees ? "Matched" : character is null ? "Unavailable" : "Incomplete / Conflict";
        var confidence = Math.Clamp((float)(consensus.AverageConfidence + (agrees ? .10 : strong ? .03 : 0)), 0, 1);
        var source = strong ? "FullLineConsensus"
            : selected.ProcessingMethod.StartsWith("Targeted fallback:", StringComparison.Ordinal)
                ? "TargetedFullLineFallback" : "PlausibleFullLineFallback";
        result = new OcrReadResult(consensus.Value, selected.RawText, selected.CleanedText, confidence, null)
        {
            Warning = warning,
            SelectionReason = strong
                ? $"Güçlü tam-satır uzlaşısı: {consensus.Value:0} ({consensus.IndependentPipelines} bağımsız pipeline)."
                    + (warning is null ? " Karakter sonucu aynı değeri doğruladı." : $" {warning}")
                : $"Yapısal olarak geçerli tek full-line OCR seçildi: {consensus.Value:0}. {warning}",
            FullLineConsensusValue = consensus.Value, FullLineConsensusCount = consensus.IndependentPipelines,
            FullLineConsensusLevel = level, CharacterResult = characterValue,
            CharacterStatus = characterStatus, DecisionSource = source,
            EstimatedDigitCount = estimatedDigits, SelectedCandidateDigitCount = selected.CharacterCount,
            DigitPlausibility = "Plausible"
        };
        return true;
    }

    internal static int EstimateThrustDigitCount(IReadOnlyList<OcrCandidateResult> candidates)
    {
        var direct = candidates.Select(candidate => candidate.SegmentCount).Where(count => count is >= 4 and <= 5)
            .DefaultIfEmpty(0).Max();
        if (direct > 0) return direct;
        var geometric = 0;
        foreach (var bound in candidates.SelectMany(candidate => candidate.AcceptedSegmentBounds))
        {
            if (bound.Width <= 0 || bound.Height <= 0) continue;
            var estimate = Math.Clamp((int)Math.Round(bound.Width / Math.Max(1d, bound.Height * .55)), 1, 5);
            geometric = Math.Max(geometric, estimate);
        }
        if (geometric > 1) return geometric;
        return candidates.Where(candidate => !candidate.UsesCharacterBasedOcr)
            .Select(candidate => candidate.CleanedText.Count(char.IsDigit)).Where(count => count > 0)
            .DefaultIfEmpty(geometric).Max();
    }

    internal static bool IsPlausibleThrustValue(decimal? value, int candidateDigits, int estimatedDigits)
    {
        if (!value.HasValue || value is < 0 or > 100000) return false;
        if (value == 0) return estimatedDigits <= 1 && candidateDigits == 1;
        if (candidateDigits is < 4 or > 5) return false;
        return estimatedDigits <= 0 || Math.Abs(candidateDigits - estimatedDigits) <= 1;
    }

    private static decimal? ParseThrustCandidate(OcrCandidateResult candidate)
    {
        if (candidate.ParsedValue is >= 0 and <= 100000) return candidate.ParsedValue;
        var digits = new string(candidate.CleanedText.Where(char.IsDigit).ToArray());
        return decimal.TryParse(digits, System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    private static double ThrustPlausibilityScore(OcrCandidateResult candidate, bool plausible, int estimatedDigits)
    {
        if (!plausible) return 0;
        var digitScore = estimatedDigits <= 0 ? .20 : Math.Abs(candidate.CharacterCount - estimatedDigits) == 0 ? .35 : .22;
        return Math.Clamp(candidate.OcrConfidence * .40 + .25 + digitScore, 0, 1);
    }

    internal static bool HasPlausibleThrustFullLine(IReadOnlyList<OcrCandidateResult> candidates)
    {
        var estimated = EstimateThrustDigitCount(candidates);
        return candidates.Where(candidate => !candidate.UsesCharacterBasedOcr)
            .Any(candidate => IsPlausibleThrustValue(ParseThrustCandidate(candidate),
                candidate.CleanedText.Count(char.IsDigit), estimated) && candidate.OcrConfidence >= .10f);
    }

    internal static bool NeedsThrustTargetedFallback(IReadOnlyList<OcrCandidateResult> candidates) =>
        !HasPlausibleThrustFullLine(candidates);

    private static bool TrySelectAdvanceWithoutRightEdgeNoise(IReadOnlyList<OcrCandidateResult> candidates,
        out OcrReadResult result, out OcrCandidateResult? selected)
    {
        result = OcrReadResult.Failed("", "", 0, "Sağ kenar gürültüsü düzeltmesi uygulanmadı."); selected = null;
        var fullLine = candidates.Where(candidate => !candidate.UsesCharacterBasedOcr && candidate.IsValid)
            .GroupBy(candidate => candidate.CleanedText)
            .OrderByDescending(group => group.Select(candidate => BaseMethod(candidate.ProcessingMethod)).Distinct().Count())
            .ThenByDescending(group => group.Average(candidate => candidate.OcrConfidence)).FirstOrDefault();
        var character = candidates.Where(candidate => candidate.UsesCharacterBasedOcr)
            .Select(candidate => new { Candidate = candidate, Text = Digits(string.IsNullOrWhiteSpace(candidate.CombinedText) ? candidate.CharacterOcrText : candidate.CombinedText) })
            .Where(value => value.Text.Length is >= 1 and <= 4).OrderByDescending(value => value.Candidate.AllCharactersTemplateAuthoritative)
            .ThenByDescending(value => value.Candidate.CharacterDecisionReliable).ThenByDescending(value => value.Text.Length).FirstOrDefault();
        if (fullLine is null || character is null) return false;
        var fullText = Digits(fullLine.Key); var geometry = candidates
            .Where(candidate => candidate.AcceptedSegmentBounds.Count + candidate.RejectedSegmentBounds.Count > character.Text.Length)
            .OrderByDescending(candidate => candidate.AcceptedSegmentBounds.Count + candidate.RejectedSegmentBounds.Count).FirstOrDefault() ?? character.Candidate;
        var acceptedBounds = geometry.AcceptedSegmentBounds.OrderBy(bound => bound.X).ToList();
        if (acceptedBounds.Count == character.Text.Length)
        {
            var rejectedRightEdge = geometry.RejectedSegmentBounds.Where(bound => bound.X >= acceptedBounds[^1].Right)
                .OrderByDescending(bound => bound.Right).FirstOrDefault();
            if (!rejectedRightEdge.IsEmpty) acceptedBounds.Add(rejectedRightEdge);
        }
        var geometryBounds = acceptedBounds.ToArray();
        var decision = EvaluateAdvanceRightEdgeNoise(fullText, character.Text, character.Text.Length,
            geometryBounds, geometry.ImageSize);
        if (!decision.ShouldRemove || !decimal.TryParse(character.Text, out var value)) return false;
        character.Candidate.ParsedValue = value; character.Candidate.Error = null;
        character.Candidate.ExtraCharacterDetected = true;
        character.Candidate.RemovedNoiseCharacter = decision.RemovedCharacter;
        character.Candidate.ExtraCharacterEliminationReason = decision.Reason;
        character.Candidate.CorrectedFullLineResult = value;
        character.Candidate.SystemConfidence = Math.Max(character.Candidate.SystemConfidence, 0.90f);
        character.Candidate.Confidence = character.Candidate.SystemConfidence;
        selected = character.Candidate;
        result = new OcrReadResult(value, fullLine.First().RawText, character.Text, character.Candidate.Confidence, null)
        { SelectionReason = $"Sağ kenar gürültüsü elendi: tam satır {fullText}, karakter bazlı {character.Text}. {decision.Reason}" };
        return true;

        static string Digits(string text) => new(text.Where(char.IsDigit).ToArray());
    }

    internal static AdvanceRightEdgeNoiseDecision EvaluateAdvanceRightEdgeNoise(string fullLineText, string characterText,
        int validSegmentCount, IReadOnlyList<Rectangle> segmentBounds, Size imageSize)
    {
        if (characterText.Length != validSegmentCount || fullLineText.Length != characterText.Length + 1
            || !fullLineText.StartsWith(characterText, StringComparison.Ordinal) || characterText.Length >= 4)
            return new(false, "", "Tam satır ve karakter sonucu fazladan tek son karakter deseninde değil.");
        if (segmentBounds.Count <= validSegmentCount || imageSize.Width <= 0 || imageSize.Height <= 0)
            return new(false, "", "Fazladan karakter için sağ kenar geometrisi bulunamadı.");
        var ordered = segmentBounds.OrderBy(bound => bound.X).ToArray(); var last = ordered[^1];
        var real = ordered.Take(validSegmentCount).ToArray();
        var medianWidth = real.Select(bound => (double)bound.Width).OrderBy(value => value).ElementAt(real.Length / 2);
        var medianArea = real.Select(bound => (double)(bound.Width * bound.Height)).OrderBy(value => value).ElementAt(real.Length / 2);
        var gaps = real.Zip(real.Skip(1), (left, right) => (double)Math.Max(0, right.X - left.Right)).ToArray();
        var normalGap = gaps.Length == 0 ? medianWidth * 0.35 : Math.Max(1, gaps.OrderBy(value => value).ElementAt(gaps.Length / 2));
        var aspect = last.Width / (double)Math.Max(1, last.Height);
        var narrow = aspect <= 0.35 || last.Width <= medianWidth * 0.50;
        var edge = last.Right >= imageSize.Width * 0.88;
        var distant = last.X - real[^1].Right >= Math.Max(normalGap * 1.5, medianWidth * 0.55);
        var lowArea = last.Width * last.Height <= medianArea * 0.50;
        var verticalEdge = last.Height >= imageSize.Height * 0.35 && aspect <= 0.30;
        var score = new[] { narrow, edge, distant, lowArea, verticalEdge }.Count(value => value);
        var remove = score >= 3 && narrow && edge && verticalEdge;
        var reason = remove
            ? $"Son '{fullLineText[^1]}' karakteri sağ kenara yapışık ince dikey çizgi/kutu kenarıdır (dar={narrow}, uzak={distant}, düşük alan={lowArea}, skor={score}/5)."
            : $"Son karakter gerçek rakam olabilir; sağ kenar gürültü koşulları yetersiz (skor={score}/5).";
        return new(remove, remove ? fullLineText[^1].ToString() : "", reason);
    }

    internal static OcrReadResult SelectFullLineTesseractResult(IReadOnlyList<OcrCandidateResult> candidates)
    {
        var winner = candidates.Where(candidate => !candidate.UsesCharacterBasedOcr && candidate.IsValid)
            .GroupBy(candidate => candidate.ParsedValue!.Value)
            .Select(group => new
            {
                Value = group.Key,
                Candidates = group.ToArray(),
                Votes = group.Select(candidate => BaseMethod(candidate.ProcessingMethod)).Distinct().Count(),
                Confidence = group.Average(candidate => candidate.OcrConfidence)
            })
            .OrderByDescending(group => group.Votes)
            .ThenByDescending(group => group.Confidence)
            .FirstOrDefault();
        if (winner is null)
            return OcrReadResult.Failed("", "", 0, "Geçerli tam satır Tesseract adayı yok.");
        var selected = winner.Candidates.OrderByDescending(candidate => candidate.OcrConfidence).First();
        return new OcrReadResult(winner.Value, selected.RawText, selected.CleanedText,
            (float)winner.Confidence, null)
        { SelectionReason = $"Tam satır Tesseract çoğunluğu: {winner.Value:0.##} ({winner.Votes} yöntem)." };
    }

    private static string BaseMethod(string method) => method.StartsWith("Segment Crop", StringComparison.Ordinal)
        ? "Segment Crop"
        : method.Replace(" + Segmentasyon", "", StringComparison.Ordinal);

    public static Bitmap Preprocess(Bitmap source)
    {
        var scaled = new Bitmap(Math.Max(1, source.Width * 3), Math.Max(1, source.Height * 3), PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(scaled); graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.DrawImage(source, new Rectangle(0, 0, scaled.Width, scaled.Height)); return scaled;
    }

    private void EnsureEngine()
    {
        if (_engine is not null) return; var trained = Path.Combine(TessdataPath, $"{Language}.traineddata");
        if (!File.Exists(trained)) throw new OcrConfigurationException($"OCR eğitim dosyası bulunamadı: {trained}");
        try { _engine = new TesseractEngine(TessdataPath, Language, EngineMode.LstmOnly); EngineCreationCount++; _engine.SetVariable("tessedit_char_whitelist", "0123456789,.-"); _engine.SetVariable("classify_bln_numeric_mode", "1"); }
        catch (Exception exception) { throw new OcrConfigurationException("Tesseract OCR motoru başlatılamadı.", exception); }
    }

    public async Task WarmUpAsync(CancellationToken cancellationToken = default)
    {
        await _engineLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { EnsureEngine(); }
        finally { _engineLock.Release(); }
    }

    public void Dispose() { if (_disposed) return; _disposed = true; _engine?.Dispose(); _engineLock.Dispose(); }
}

internal sealed class DisposableVariantCollection(IReadOnlyList<OcrImageVariant> items) : IDisposable
{ public IReadOnlyList<OcrImageVariant> Items { get; } = items; public void Dispose() { foreach (var item in Items) item.Dispose(); } }
public sealed class OcrConfigurationException : Exception
{ public OcrConfigurationException(string message) : base(message) { } public OcrConfigurationException(string message, Exception inner) : base(message, inner) { } }

internal sealed record GateCharacterDecision(char FinalDigit, double Confidence,
    bool TemplateContributed, bool TesseractContributed, bool GeometryContributed,
    bool TemplateAuthoritative, bool TemplateHighConfidence, bool TemplateGeometryAgreement, string Source);

internal sealed record AdvanceRightEdgeNoiseDecision(bool ShouldRemove, string RemovedCharacter, string Reason);
