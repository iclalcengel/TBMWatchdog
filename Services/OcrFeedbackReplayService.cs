using System.Globalization;
using System.Text;
using System.Text.Json;
using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

/// <summary>Offline-only evaluator. It never captures the screen and never feeds results into live reliability.</summary>
public sealed class OcrFeedbackReplayService : IDisposable
{
    private readonly OcrService _ocr;
    private readonly OcrProfile _profile;
    private readonly IReadOnlyDictionary<string, NumericFieldDefinition> _fields;
    private readonly string _diagnostics;
    private readonly Func<Bitmap, NumericFieldDefinition, OcrFieldKind, FeedbackReplayMode, CancellationToken, Task<OcrDiagnosticResult>>? _runner;

    public OcrFeedbackReplayService(OcrProfile profile, IEnumerable<NumericFieldDefinition> fields,
        string? diagnosticsFolder = null,
        Func<Bitmap, NumericFieldDefinition, OcrFieldKind, FeedbackReplayMode, CancellationToken, Task<OcrDiagnosticResult>>? runner = null)
    {
        _profile = profile; _fields = fields.ToDictionary(x => x.FieldId, StringComparer.OrdinalIgnoreCase);
        _diagnostics = diagnosticsFolder ?? Path.Combine(AppContext.BaseDirectory, "Diagnostics");
        _ocr = new OcrService(); _runner = runner;
    }

    public IReadOnlyList<OcrFeedbackReplaySample> LoadActiveDataset()
    {
        var root = Path.Combine(_diagnostics, "UserFeedback");
        if (!Directory.Exists(root)) return [];
        var all = new List<OcrUserFeedback>();
        foreach (var path in Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories))
        {
            try { var item = JsonSerializer.Deserialize<OcrUserFeedback>(File.ReadAllText(path)); if (item is not null) all.Add(item); }
            catch { /* A damaged audit entry must not stop the remaining dataset. */ }
        }
        return SelectLatestActive(all).Select(ToSample).Where(x => x is not null).Cast<OcrFeedbackReplaySample>().ToArray();
    }

    public static IReadOnlyList<OcrUserFeedback> SelectLatestActive(IEnumerable<OcrUserFeedback> source) => source
        .GroupBy(x => x.MeasurementId + "|" + x.FieldId, StringComparer.OrdinalIgnoreCase)
        .Select(g => g.OrderByDescending(x => x.FeedbackAt).ThenByDescending(x => x.FeedbackId).First())
        .Where(x => x.Verdict != OcrFeedbackVerdict.Removed).ToArray();

    private static OcrFeedbackReplaySample? ToSample(OcrUserFeedback feedback)
    {
        var expected = feedback.Verdict switch
        {
            OcrFeedbackVerdict.Correct => feedback.ObservedValue,
            OcrFeedbackVerdict.Corrected => feedback.CorrectedValue,
            _ => null
        };
        var negativeOnly = feedback.Verdict == OcrFeedbackVerdict.Incorrect && !feedback.CorrectedValue.HasValue;
        return File.Exists(feedback.RawImagePath) ? new(feedback, expected, negativeOnly, feedback.RawImagePath) : null;
    }

    public async Task<IReadOnlyList<OcrFeedbackReplayRow>> ReplayAsync(IProgress<(int Done,int Total,string Field)>? progress = null,
        CancellationToken token = default)
    {
        var samples = LoadActiveDataset(); var rows = new List<OcrFeedbackReplayRow>(samples.Count); var done = 0;
        foreach (var sample in samples)
        {
            token.ThrowIfCancellationRequested();
            if (!_fields.TryGetValue(sample.Feedback.FieldId, out var field)) { progress?.Report((++done,samples.Count,sample.Feedback.FieldId)); continue; }
            using var image = new Bitmap(sample.RawImagePath);
            Console.WriteLine($"Replay {done+1}/{samples.Count}: {sample.Feedback.FieldId} {image.Width}x{image.Height} LiveFast...");
            using var live = await Run(image, field, FeedbackReplayMode.LiveFast, token).ConfigureAwait(false);
            Console.WriteLine($"Replay {done+1}/{samples.Count}: {sample.Feedback.FieldId} Diagnostic...");
            using var diagnostic = await Run(image, field, FeedbackReplayMode.Diagnostic, token).ConfigureAwait(false);
            var liveOutcome = Analyze(live, sample, field); var diagnosticOutcome = Analyze(diagnostic, sample, field);
            var expected = sample.ExpectedValue; var observed = sample.Feedback.ObservedValue;
            decimal? abs = expected.HasValue && live.Result.Value.HasValue ? Math.Abs(expected.Value-live.Result.Value.Value) : null;
            decimal? rel = abs.HasValue && expected != 0 ? abs.Value/Math.Abs(expected!.Value) : abs == 0 ? 0 : null;
            rows.Add(new(sample, liveOutcome, diagnosticOutcome,
                ClassifyFailure(observed, expected, diagnosticOutcome),
                expected.HasValue && observed.HasValue && observed != expected && sample.Feedback.TrustedValueAtThatTime == observed,
                abs, rel));
            progress?.Report((++done,samples.Count,sample.Feedback.FieldId));
        }
        WriteReports(rows); return rows;
    }

    private Task<OcrDiagnosticResult> Run(Bitmap image, NumericFieldDefinition field, FeedbackReplayMode mode, CancellationToken token)
    {
        var kind = MapKind(field.FieldId); var maximum = field.MaximumValue ?? decimal.MaxValue;
        return _runner is not null ? _runner(image, field, kind, mode, token)
            : mode == FeedbackReplayMode.LiveFast
                ? _ocr.ReadNumberLiveFastWithDiagnosticsAsync(image, maximum, _profile, kind, token)
                : _ocr.ReadNumberWithDiagnosticsAsync(image, maximum, _profile, kind, token);
    }

    internal static OcrFeedbackReplayOutcome Analyze(OcrDiagnosticResult result, OcrFeedbackReplaySample sample, NumericFieldDefinition field)
    {
        var expected = sample.ExpectedValue;
        bool Match(decimal? value) => expected.HasValue && value.HasValue && value.Value == expected.Value;
        var correct = result.Candidates.Where(c => Match(c.ParsedValue)).ToArray();
        var best = correct.OrderByDescending(c => c.SystemConfidence).FirstOrDefault();
        var character = result.Candidates.FirstOrDefault(c => c.UsesCharacterBasedOcr);
        var segmentation = result.Candidates.Count == 0 ? 0 : result.Candidates.Max(c => c.SegmentCount);
        var expectedDigits = expected.HasValue ? Digits(FormatExpected(expected.Value, field.DecimalPlaces)) : 0;
        var candidates = BuildTemplateCandidates(character, sample, expectedDigits, field.DecimalPlaces);
        var eligible = candidates.Count == expectedDigits && expectedDigits > 0;
        var blocked = eligible ? "" : character is null || character.SegmentCount == 0 ? "SegmentationMissing"
            : character.SegmentCount != expectedDigits ? "SegmentationIncomplete" : "QualityRejected";
        return new(result.Result.Value, result.Result.RawText, result.Result.SelectionReason ?? "", best?.ProcessingMethod ?? "",
            correct.Select(c => c.ProcessingMethod).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            correct.Any(c => !c.UsesCharacterBasedOcr), Match(character?.ParsedValue),
            character?.CharacterDiagnostics.Any(x => !string.IsNullOrWhiteSpace(x.TemplateResult)) == true && Match(character.ParsedValue),
            segmentation, expectedDigits, result.Result.Error ?? "", eligible, blocked, candidates);
    }

    private static IReadOnlyList<FeedbackTemplateCandidate> BuildTemplateCandidates(OcrCandidateResult? candidate,
        OcrFeedbackReplaySample sample, int expectedDigits, int decimalPlaces)
    {
        if (candidate is null || sample.ExpectedValue is null || candidate.AcceptedSegmentBounds.Count != expectedDigits) return [];
        var digits = new string(FormatExpected(sample.ExpectedValue.Value, decimalPlaces).Where(char.IsDigit).ToArray());
        if (digits.Length != expectedDigits) return [];
        var list = new List<FeedbackTemplateCandidate>();
        for (var i=0;i<expectedDigits;i++)
        {
            var bounds=candidate.AcceptedSegmentBounds[i];
            if (bounds.Width<=0||bounds.Height<=0||bounds.Right>candidate.BinaryImage.Width||bounds.Bottom>candidate.BinaryImage.Height) return [];
            using var crop=candidate.BinaryImage.Clone(bounds,candidate.BinaryImage.PixelFormat);
            var quality=DigitTemplateQualityEvaluator.Evaluate(crop);
            if (quality.Status is DigitTemplateQualityStatus.Rejected or DigitTemplateQualityStatus.Poor) return [];
            list.Add(new(digits[i]-'0',bounds,quality.Status.ToString(),quality.QualityScore));
        }
        return list;
    }

    public static FeedbackFailureClass ClassifyFailure(decimal? observed, decimal? expected, OcrFeedbackReplayOutcome diagnostic)
    {
        if (!expected.HasValue) return observed.HasValue ? FeedbackFailureClass.Unknown : FeedbackFailureClass.Unreadable;
        if (!observed.HasValue) return diagnostic.SegmentationCount == 0 ? FeedbackFailureClass.Unreadable : FeedbackFailureClass.FullLineFailure;
        if (observed == expected) return FeedbackFailureClass.None;
        var o=Canonical(observed.Value); var e=Canonical(expected.Value);
        var od=new string(o.Where(char.IsDigit).ToArray()); var ed=new string(e.Where(char.IsDigit).ToArray());
        if (o.Contains('.') != e.Contains('.') || DecimalCount(o)!=DecimalCount(e)) return FeedbackFailureClass.DecimalDigitCountError;
        if (ed.EndsWith(od,StringComparison.Ordinal) && ed.Length>od.Length) return FeedbackFailureClass.MissingLeadingDigit;
        if (ed.StartsWith(od,StringComparison.Ordinal) && ed.Length>od.Length) return FeedbackFailureClass.MissingTrailingDigit;
        if (IsSubsequence(od,ed) && ed.Length>od.Length) return FeedbackFailureClass.MissingMiddleDigit;
        if (od.EndsWith(ed,StringComparison.Ordinal) && od.Length>ed.Length) return FeedbackFailureClass.ExtraLeadingDigit;
        if (od.StartsWith(ed,StringComparison.Ordinal) && od.Length>ed.Length) return FeedbackFailureClass.ExtraTrailingDigit;
        if (od.Length==ed.Length) return FeedbackFailureClass.WrongDigit;
        return diagnostic.SegmentationCount>0 && diagnostic.SegmentationCount!=diagnostic.ExpectedDigitCount
            ? FeedbackFailureClass.CharacterSegmentationFailure : FeedbackFailureClass.Unknown;
    }

    internal static string DetermineTrainingBlockedReason(int segmentCount, int expectedDigitCount,
        IEnumerable<DigitTemplateQualityStatus> qualities)
    {
        if (segmentCount == 0) return "SegmentationMissing";
        if (segmentCount != expectedDigitCount) return "SegmentationIncomplete";
        return qualities.Any(q => q is DigitTemplateQualityStatus.Poor or DigitTemplateQualityStatus.Rejected)
            ? "QualityRejected" : "";
    }

    internal static bool WasWrongValueTrusted(decimal? observed, decimal? trusted, decimal? expected) =>
        observed.HasValue && expected.HasValue && observed.Value != expected.Value && trusted == observed;

    private void WriteReports(IReadOnlyList<OcrFeedbackReplayRow> rows)
    {
        Directory.CreateDirectory(_diagnostics);
        using(var w=new StreamWriter(Path.Combine(_diagnostics,"FeedbackReplayReport.csv"),false,new UTF8Encoding(true)))
        { w.WriteLine("FeedbackId,MeasurementId,Field,CapturedAt,Verdict,ExpectedValue,OriginalObservedValue,OriginalTrustedValue,ReplayLiveFastValue,ReplayDiagnosticFinalValue,LiveFastExactMatch,DiagnosticExactMatch,AbsoluteError,RelativeError,BestCorrectPipeline,CorrectPipelineCount,FullLineCorrect,CharacterCorrect,TemplateCorrect,SegmentationCount,ExpectedDigitCount,FailureClass,DecisionSource,TrainingBlockedReason,Notes");
          foreach(var r in rows) w.WriteLine(string.Join(',',Q(r.Sample.Feedback.FeedbackId),Q(r.Sample.Feedback.MeasurementId),Q(r.Sample.Feedback.FieldId),Q(r.Sample.Feedback.CapturedAt.ToString("O")),r.Sample.Feedback.Verdict,N(r.Sample.ExpectedValue),N(r.Sample.Feedback.ObservedValue),N(r.Sample.Feedback.TrustedValueAtThatTime),N(r.LiveFast.Value),N(r.Diagnostic.Value),B(Eq(r.LiveFast.Value,r.Sample.ExpectedValue)),B(Eq(r.Diagnostic.Value,r.Sample.ExpectedValue)),N(r.AbsoluteError),N(r.RelativeError),Q(r.Diagnostic.BestCorrectPipeline),r.Diagnostic.CorrectPipelineCount,B(r.Diagnostic.FullLineCorrect),B(r.Diagnostic.CharacterCorrect),B(r.Diagnostic.TemplateCorrect),r.Diagnostic.SegmentationCount,r.Diagnostic.ExpectedDigitCount,r.FailureClass,Q(r.Diagnostic.DecisionSource),Q(r.Diagnostic.TrainingBlockedReason),Q(r.Diagnostic.Notes))); }
        var summaries=Summarize(rows);
        using(var w=new StreamWriter(Path.Combine(_diagnostics,"FeedbackAccuracySummary.csv"),false,new UTF8Encoding(true)))
        { w.WriteLine("Field,TotalLabeled,CorrectSamples,CorrectedSamples,OriginalExactAccuracy,ReplayLiveFastExactAccuracy,ReplayDiagnosticExactAccuracy,UnreadableRate,MissingDigitRate,WrongDigitRate,DecimalErrorRate"); foreach(var s in summaries)w.WriteLine(string.Join(',',Q(s.Field),s.TotalLabeled,s.CorrectSamples,s.CorrectedSamples,F(s.OriginalExactAccuracy),F(s.ReplayLiveFastExactAccuracy),F(s.ReplayDiagnosticExactAccuracy),F(s.UnreadableRate),F(s.MissingDigitRate),F(s.WrongDigitRate),F(s.DecimalErrorRate))); }
        using(var w=new StreamWriter(Path.Combine(_diagnostics,"FeedbackReliabilityAudit.csv"),false,new UTF8Encoding(true)))
        { w.WriteLine("Field,Observed,TrustedAtFeedback,Expected,WrongValueWasTrusted,ReliabilityReason,StructureStatus,TemporalStatus"); foreach(var r in rows.Where(x=>x.Sample.ExpectedValue.HasValue))w.WriteLine(string.Join(',',Q(r.Sample.Feedback.FieldId),N(r.Sample.Feedback.ObservedValue),N(r.Sample.Feedback.TrustedValueAtThatTime),N(r.Sample.ExpectedValue),B(r.WrongValueWasTrusted),Q(r.Sample.Feedback.DecisionSource),Q(r.Sample.Feedback.QualityStatus),Q(r.Sample.Feedback.FailureReason))); }
    }

    public static IReadOnlyList<FeedbackAccuracySummary> Summarize(IEnumerable<OcrFeedbackReplayRow> source) => source
        .Where(r=>r.Sample.ExpectedValue.HasValue).GroupBy(r=>r.Sample.Feedback.FieldId,StringComparer.OrdinalIgnoreCase).Select(g=>
        { var a=g.ToArray(); double Rate(Func<OcrFeedbackReplayRow,bool> p)=>a.Length==0?0:a.Count(p)*100d/a.Length; return new FeedbackAccuracySummary(g.Key,a.Length,a.Count(x=>x.Sample.Feedback.Verdict==OcrFeedbackVerdict.Correct),a.Count(x=>x.Sample.Feedback.Verdict==OcrFeedbackVerdict.Corrected),Rate(x=>Eq(x.Sample.Feedback.ObservedValue,x.Sample.ExpectedValue)),Rate(x=>Eq(x.LiveFast.Value,x.Sample.ExpectedValue)),Rate(x=>Eq(x.Diagnostic.Value,x.Sample.ExpectedValue)),Rate(x=>x.FailureClass==FeedbackFailureClass.Unreadable),Rate(x=>x.FailureClass is FeedbackFailureClass.MissingLeadingDigit or FeedbackFailureClass.MissingMiddleDigit or FeedbackFailureClass.MissingTrailingDigit),Rate(x=>x.FailureClass==FeedbackFailureClass.WrongDigit),Rate(x=>x.FailureClass is FeedbackFailureClass.DecimalShift or FeedbackFailureClass.DecimalDigitCountError)); }).ToArray();

    internal static OcrFieldKind MapKind(string id)=>id switch { BuiltInFieldIds.StableRpm=>OcrFieldKind.StableRpm,BuiltInFieldIds.StableTorque=>OcrFieldKind.StableTorque,BuiltInFieldIds.StableThrustForce=>OcrFieldKind.StableThrustForce,BuiltInFieldIds.StableAdvanceSpeed=>OcrFieldKind.StableAdvanceSpeed,BuiltInFieldIds.StablePenetration=>OcrFieldKind.StablePenetration,BuiltInFieldIds.NominalInjection=>OcrFieldKind.NominalInjection,BuiltInFieldIds.RealInjection=>OcrFieldKind.RealInjection,BuiltInFieldIds.Epb1=>OcrFieldKind.Epb1,BuiltInFieldIds.Epb2=>OcrFieldKind.Epb2,BuiltInFieldIds.Epb3=>OcrFieldKind.Epb3,BuiltInFieldIds.Epb4=>OcrFieldKind.Epb4,BuiltInFieldIds.Epb5=>OcrFieldKind.Epb5,BuiltInFieldIds.Gate=>OcrFieldKind.Gate,BuiltInFieldIds.Scale1=>OcrFieldKind.Scale1,BuiltInFieldIds.Scale2=>OcrFieldKind.Scale2,BuiltInFieldIds.Nominal=>OcrFieldKind.Nominal,_=>OcrFieldKind.General};
    private static int Digits(string s)=>s.Count(char.IsDigit); private static int DecimalCount(string s){var i=s.IndexOf('.');return i<0?0:s.Length-i-1;}
    private static string Canonical(decimal d)=>d.ToString("0.############################",CultureInfo.InvariantCulture);
    private static string FormatExpected(decimal d,int places)=>places>0?d.ToString("F"+places,CultureInfo.InvariantCulture):Canonical(d);
    private static bool IsSubsequence(string small,string large){var i=0;foreach(var c in large)if(i<small.Length&&small[i]==c)i++;return i==small.Length;}
    private static bool Eq(decimal? a,decimal? b)=>a.HasValue&&b.HasValue&&a.Value==b.Value;
    private static string N(decimal? v)=>v?.ToString(CultureInfo.InvariantCulture)??""; private static string F(double v)=>v.ToString("0.00",CultureInfo.InvariantCulture); private static string B(bool v)=>v?"true":"false"; private static string Q(string? s)=>'"'+(s??"").Replace("\"","\"\"")+'"';
    public void Dispose()=>_ocr.Dispose();
}
