using TBMWatchdog.Models;
using System.Collections.Concurrent;

namespace TBMWatchdog.Services;

public sealed class OcrDataSource : IDataSource, IMonitoringLifecycleDataSource, IDisposable
{
    private readonly AppSettings _settings;
    private readonly ScreenCaptureService _captureService;
    private readonly OcrService _ocrService;
    private readonly CharacterOcrComparisonCsvService _comparisonCsv = new();
    private readonly TemporalValueValidationService _temporalValidation = new();
    private readonly SuspiciousValueDecisionCsvService _suspiciousDecisionCsv = new();
    private readonly MismatchSampleCaptureService _mismatchCapture;
    private readonly OcrImageCacheService _ocrCache;
    private readonly TextOcrService? _textOcrService;
    private readonly OperationModeStateService? _operationModeState;
    private readonly OperationModeTransitionCsvService _operationModeCsv = new();
    private readonly LiveOcrPerformanceCsvService _livePerformance = new();
    private readonly ProcessValueReliabilityService _processReliability;
    private MeasurementRecord? _previous;
    private decimal? _stableGateValue;
    private decimal? _pendingGateValue;
    private int _pendingGateCount;
    private int _forcedRealOcrReadings;
    private readonly ConcurrentDictionary<string, byte> _fieldCacheBypass = new(StringComparer.OrdinalIgnoreCase);
    private readonly OcrFeedbackSnapshotCache? _feedbackSnapshots;
    private readonly VerifiedFeedbackLearningService _feedbackLearning;
    private readonly MonitoringPipelineProfiler _pipelineProfiler = new();
    private readonly OcrWorkerScheduler _ocrScheduler;
    private readonly object _parallelMetricsSync = new();
    private readonly object _feedbackAuditSync = new();
    private OcrSchedulerCycleMetrics? _lastSchedulerMetrics;

    public OcrDataSource(AppSettings settings, ScreenCaptureService captureService, OcrService ocrService,
        OcrFeedbackSnapshotCache? feedbackSnapshots = null)
    {
        _settings = settings;
        OcrProfileMigrator.Migrate(_settings);
        _captureService = captureService;
        _ocrService = ocrService;
        _ocrScheduler = new OcrWorkerScheduler(settings.OcrMaxParallelWorkers, settings.OcrFieldTimeoutMs);
        _ = _ocrScheduler.WarmUpAsync().ContinueWith(task =>
        {
            if (task.Exception is not null) DiagnosticWarning?.Invoke($"OCR işçi havuzu ısınma uyarısı: {task.Exception.GetBaseException().Message}");
        }, TaskScheduler.Default);
        _feedbackSnapshots = feedbackSnapshots;
        _processReliability = new ProcessValueReliabilityService(settings.OcrProfile);
        _feedbackLearning = new VerifiedFeedbackLearningService(settings.OcrProfile);
        _mismatchCapture = new MismatchSampleCaptureService(new MismatchCaptureOptions(
            settings.EnableMismatchSampleCapture, settings.MismatchSampleCooldownSeconds,
            settings.MismatchSampleMaximumFilesPerField, settings.MismatchSampleMaximumTotalSizeMb));
        _mismatchCapture.Warning += message => DiagnosticWarning?.Invoke(message);
        _ocrCache = new OcrImageCacheService(new OcrCacheOptions(settings.EnableOcrImageCache,
            settings.OcrImageSimilarityThresholdPercent, settings.GateOcrCacheMaximumAgeSeconds,
            settings.NumericFieldOcrCacheMaximumAgeSeconds));
        _ocrCache.Warning += OnCacheWarning;
        OcrCacheControl.ClearRequested += OnCacheClearRequested;
        DigitTemplateMatcher.TemplatesReloaded += OnTemplatesReloaded;
        if (settings.OcrProfile.Name.Contains("1031", StringComparison.OrdinalIgnoreCase) && settings.OperationModeRegion.IsValid)
        {
            _textOcrService = new TextOcrService();
            _operationModeState = new OperationModeStateService("1031", Guid.NewGuid().ToString("N"), settings.OcrProfile);
            _operationModeState.OperationModeChanged += transition => { _operationModeCsv.Append(transition); OperationModeChanged?.Invoke(transition); };
            _operationModeState.ExcavationStarted += transition => ExcavationStarted?.Invoke(transition);
            _operationModeState.ExcavationEnded += transition => ExcavationEnded?.Invoke(transition);
        }
    }

    public string Name => "Ekran OCR";
    public event Action<Exception>? OcrFailed;
    public event Action<OcrPreviewFrame>? PreviewAvailable;
    public event Action<OcrDiagnosticsFrame>? DiagnosticsAvailable;
    public event Action<OcrPerformanceFrame>? PerformanceAvailable;
    public event Action<string>? ValidationDecisionLogged;
    public event Action<string>? DiagnosticWarning;
    public event Action<OperationModeDiagnostic>? OperationModeDiagnosticAvailable;
    public event Action<OperationModeTransition>? OperationModeChanged;
    public event Action<OperationModeTransition>? ExcavationStarted;
    public event Action<OperationModeTransition>? ExcavationEnded;
    public event Action<FieldMeasurementResult>? NumericFieldReadAvailable;
    public event Action<string>? LiveFieldTraceAvailable;
    public event Action<MonitoringCycleProfile>? PipelinePerformanceAvailable;

    public void SetFieldCacheBypass(string fieldId, bool bypass)
    {
        if (bypass) _fieldCacheBypass[fieldId] = 0; else _fieldCacheBypass.TryRemove(fieldId, out _);
    }

    public void PrepareForResume(int forcedRealOcrReadings)
    {
        _temporalValidation.ResetPending();
        _pendingGateValue = null; _pendingGateCount = 0;
        _operationModeState?.ResetForResume();
        _ocrCache.Clear();
        Interlocked.Exchange(ref _forcedRealOcrReadings, Math.Max(1, forcedRealOcrReadings));
        DiagnosticWarning?.Invoke("Devam etme: bekleyen adaylar ve OCR önbelleği temizlendi; gerçek OCR doğrulaması zorlandı.");
    }

    public void ResetAfterStop()
    {
        _temporalValidation.Reset(); _pendingGateValue = null; _pendingGateCount = 0; _stableGateValue = null;
        _operationModeState?.ResetForResume(); _ocrCache.Clear(); Interlocked.Exchange(ref _forcedRealOcrReadings, 0);
        DigitTemplateUsageTracker.Flush();
        _processReliability.Reset();
    }

    public async Task<MeasurementRecord> ReadAsync(CancellationToken cancellationToken)
    {
        var measurementId = Guid.NewGuid().ToString("N");
        var cycleWatch = System.Diagnostics.Stopwatch.StartNew();
        var captureTotalMs = 0d; var numericOcrTotalMs = 0d; var operationModeOcrMs = 0d; var loggingMs=0d;
        var liveFieldCount = 0; var liveCacheHits = 0; var liveFullExecutions = 0;
        try
        {
            TraceStableParameterConfiguration();
            var captureWatch = System.Diagnostics.Stopwatch.StartNew();
            using var images = await _captureService.CaptureConfiguredRegionsAsync(_settings, cancellationToken).ConfigureAwait(false);
            captureWatch.Stop(); captureTotalMs += captureWatch.Elapsed.TotalMilliseconds;
            if (_settings.ShowOcrPreview && PreviewAvailable is not null)
            {
                PreviewAvailable.Invoke(OcrPreviewFrame.From(images));
            }
            if (_settings.SaveScreenshots)
            {
                SaveImages(images);
            }

            OcrDiagnosticResult? nominalDiagnostic = null;
            OcrDiagnosticResult? scale1Diagnostic = null;
            OcrDiagnosticResult? scale2Diagnostic = null;
            OcrDiagnosticResult? gateDiagnostic = null;
            var ownershipTransferred = false;
            OcrReadResult nominal;
            OcrReadResult scale1;
            OcrReadResult scale2;
            OcrReadResult gate;
            var feedbackMatches = new ConcurrentDictionary<string, VerifiedFeedbackMatch>(StringComparer.OrdinalIgnoreCase);
            try
            {
                async Task<(OcrDiagnosticResult Diagnostic, OcrReadResult Result, CharacterOcrComparison Comparison, OcrCacheLookup Cache)> ReadField(
                    string key, string label, Bitmap image, DateTime capturedAt, decimal maximum, OcrFieldKind kind, int maximumSegments, bool bypass,
                    double captureMs = 0, OcrService? workerOcr = null, CancellationToken? jobToken = null)
                {
                    Interlocked.Increment(ref liveFieldCount);
                    var cacheWatch = System.Diagnostics.Stopwatch.StartNew();
                    var lookup = _ocrCache.TryGet(key, image, DateTime.Now,
                        bypass || Volatile.Read(ref _forcedRealOcrReadings) > 0);
                    cacheWatch.Stop();
                    if (lookup.Hit && lookup.OfficialResult is { } cached && lookup.Comparison is { } old)
                    {
                        Interlocked.Increment(ref liveCacheHits);
                        var shown = DateTime.Now;
                        var comparison = old with { CapturedAt = capturedAt, DisplayedAt = shown,
                            TotalLatencyMilliseconds = Math.Max(0, (shown - capturedAt).TotalMilliseconds), CacheUsed = true,
                            CacheAgeMs = lookup.AgeMilliseconds, ImageSimilarityPercent = lookup.SimilarityPercent,
                            RealOcrExecuted = false, MismatchSampleSaved = false, MismatchSampleId = null, MismatchSamplePath = null,
                            CaptureMs = captureMs, CacheMs = cacheWatch.Elapsed.TotalMilliseconds, ExecutionPath = "Cached" };
                        var cachedDiagnostic = new OcrDiagnosticResult(new Bitmap(image), new Bitmap(image), cached, shown)
                        { ExecutionMode = OcrExecutionMode.LiveFast, PrimaryPipeline = "Cache" };
                        _livePerformance.AppendField(shown, label, cachedDiagnostic, true, captureMs, 0,
                            cached.Value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null", cacheWatch.Elapsed.TotalMilliseconds);
                        return (cachedDiagnostic, cached, comparison, lookup);
                    }
                    var diagnostic = await (workerOcr ?? _ocrService).ReadNumberLiveFastWithDiagnosticsAsync(image, maximum, _settings.OcrProfile, kind, jobToken ?? cancellationToken).ConfigureAwait(false);
                    Interlocked.Increment(ref liveFullExecutions);
                    lock (_parallelMetricsSync) numericOcrTotalMs += diagnostic.TotalElapsed.TotalMilliseconds;
                    DetailedTrace($"Field={label} | ExecutionPath={diagnostic.ExecutionPath} | PipelineCountExecuted={diagnostic.PipelineCountExecuted} | PrimaryPipeline={diagnostic.PrimaryPipeline} | FallbackUsed={diagnostic.FallbackUsed} | HeavyFallbackUsed={diagnostic.HeavyFallbackUsed} | CacheUsed=false | CacheMs={cacheWatch.Elapsed.TotalMilliseconds:0.0} | FastFullLineOcrMs={diagnostic.FullLineOcrMs:0.0} | CharacterSegmentationMs={diagnostic.CharacterSegmentationMs:0.0} | TemplateMatchMs={diagnostic.TemplateMatchMs:0.0} | TargetedFallbackMs={diagnostic.TargetedFallbackMs:0.0} | DecisionMs={diagnostic.DecisionMs:0.0} | TotalMs={diagnostic.TotalElapsed.TotalMilliseconds:0.0} | Result={diagnostic.Result.Value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null"}");
                    _livePerformance.AppendField(DateTime.Now, label, diagnostic, false, captureMs, 0,
                        diagnostic.Result.Value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null", cacheWatch.Elapsed.TotalMilliseconds);
                    var shownAt = DateTime.Now;
                    var result = kind is OcrFieldKind.Gate or OcrFieldKind.Advance
                        or OcrFieldKind.StableRpm or OcrFieldKind.StableTorque or OcrFieldKind.StableThrustForce
                        or OcrFieldKind.StableAdvanceSpeed or OcrFieldKind.StablePenetration
                        or OcrFieldKind.NominalInjection or OcrFieldKind.RealInjection
                        or OcrFieldKind.Epb1 or OcrFieldKind.Epb2 or OcrFieldKind.Epb3 or OcrFieldKind.Epb4 or OcrFieldKind.Epb5
                        ? diagnostic.Result
                        : OcrService.SelectFullLineTesseractResult(diagnostic.Candidates);
                    var comparisonResult = CharacterOcrComparisonCsvService.Create(label, diagnostic, capturedAt, shownAt, maximumSegments) with
                    { CacheAgeMs = lookup.AgeMilliseconds == double.MaxValue ? 0 : lookup.AgeMilliseconds,
                        ImageSimilarityPercent = lookup.SimilarityPercent, RealOcrExecuted = true,
                        CaptureMs = captureMs, FastFullLineOcrMs = diagnostic.FullLineOcrMs,
                        CharacterSegmentationMs = diagnostic.CharacterSegmentationMs,
                        TemplateMatchMs = diagnostic.TemplateMatchMs, TargetedFallbackMs = diagnostic.TargetedFallbackMs,
                        CacheMs = cacheWatch.Elapsed.TotalMilliseconds, DecisionMs = diagnostic.DecisionMs,
                        ExecutionPath = diagnostic.ExecutionPath };
                    var definition = _settings.OcrProfile.FindField(key);
                    if (definition is not null)
                    {
                        var machineId = _settings.OcrProfile.Name.Contains("1031", StringComparison.OrdinalIgnoreCase) ? "1031" : _settings.OcrProfile.Name;
                        var feedbackWatch=System.Diagnostics.Stopwatch.StartNew();
                        var feedback = _feedbackLearning.Match(machineId, key, image); feedbackWatch.Stop(); feedbackMatches[key] = feedback;
                        comparisonResult=comparisonResult with{FeedbackMatchingMs=feedbackWatch.Elapsed.TotalMilliseconds,FeedbackCandidateCount=feedback.SupportingExamples+feedback.ConflictCount};
                        var original = result.Value;
                        if (_feedbackLearning.CanOverride(feedback, definition, null, out var feedbackReason))
                        {
                            result = result with { Value=feedback.CandidateValue,
                                CleanedText=feedback.CandidateValue!.Value.ToString($"F{Math.Max(0,definition.DecimalPlaces)}",System.Globalization.CultureInfo.InvariantCulture),
                                Error=null,Warning=null,DecisionSource="AuthoritativeUserFeedback",SelectionReason="AuthoritativeUserFeedback" };
                            comparisonResult = comparisonResult with { ParsedNumericValue=feedback.CandidateValue, FailureReason="Success" };
                        }
                        else feedbackReason=feedback.Reason;
                        lock (_feedbackAuditSync) _feedbackLearning.Audit(DateTime.Now,measurementId,machineId,key,original,feedback,result.Value,feedbackReason);
                    }
                    return (diagnostic, result, comparisonResult, lookup);
                }

                var profile = _settings.OcrProfile;
                var coreJobs = new[]
                {
                    new OcrFieldJob(measurementId, measurementId, images.CapturedAt, "Nominal", profile.FindField("Nominal")!, _settings.NominalRegion, OcrJobPriority.Medium, OcrExecutionMode.LiveFast, new Bitmap(images.Nominal)),
                    new OcrFieldJob(measurementId, measurementId, images.CapturedAt, "Scale1", profile.FindField("Scale1")!, _settings.Scale1Region, OcrJobPriority.Medium, OcrExecutionMode.LiveFast, new Bitmap(images.Scale1)),
                    new OcrFieldJob(measurementId, measurementId, images.CapturedAt, "Scale2", profile.FindField("Scale2")!, _settings.Scale2Region, OcrJobPriority.Medium, OcrExecutionMode.LiveFast, new Bitmap(images.Scale2)),
                    new OcrFieldJob(measurementId, measurementId, images.CapturedAt, "Gate", profile.FindField("Gate")!, _settings.GateRegion, OcrJobPriority.Medium, OcrExecutionMode.LiveFast, new Bitmap(images.Gate))
                };
                var additionalDefinitions = GetEnabledAdditionalFieldDefinitions(profile);
                var additionalScreen = images.ScreenImage;
                var extraJobs = new List<OcrFieldJob>(additionalDefinitions.Count);
                foreach (var definition in additionalDefinitions)
                {
                    var fieldKind = GetFieldKind(definition.FieldId);
                    var cropWatch = System.Diagnostics.Stopwatch.StartNew();
                    var crop = ScreenCaptureService.CropRegionWithPadding(additionalScreen!, definition.RegionCoordinates, profile, fieldKind);
                    cropWatch.Stop(); captureTotalMs += cropWatch.Elapsed.TotalMilliseconds;
                    extraJobs.Add(new OcrFieldJob(measurementId, measurementId, images.CapturedAt, definition.FieldId,
                        definition, definition.RegionCoordinates, GetJobPriority(definition.FieldId), OcrExecutionMode.LiveFast, crop));
                }
                IReadOnlyList<OcrScheduledResult<(OcrDiagnosticResult Diagnostic, OcrReadResult Result, CharacterOcrComparison Comparison, OcrCacheLookup Cache)>> coreScheduled;
                IReadOnlyDictionary<string, OcrScheduledResult<(OcrDiagnosticResult Diagnostic, OcrReadResult Result, CharacterOcrComparison Comparison, OcrCacheLookup Cache)>> extraScheduledByField;
                try
                {
                    var allJobs = coreJobs.Concat(extraJobs).ToArray();
                    var scheduledBatch = await _ocrScheduler.ExecuteAsync(allJobs,
                        (worker, job, token) => ReadField(job.FieldId, job.Definition.DisplayName, job.Crop, job.CapturedAt,
                            job.Definition.MaximumValue ?? 1_000_000m, GetFieldKind(job.FieldId), job.Definition.MaximumExpectedDigits,
                            job.FieldId switch { "Nominal" => _temporalValidation.HasPending("Nominal"), "Scale1" => _temporalValidation.HasPending("Kantar 1"), "Scale2" => _temporalValidation.HasPending("Kantar 2"), "Gate" => _pendingGateValue.HasValue,
                                _ => job.Definition.UsesSuspiciousValueFilter && _temporalValidation.HasPending(job.Definition.FieldId)
                                    || _fieldCacheBypass.ContainsKey(job.Definition.FieldId) }, 0, worker, token),
                        cancellationToken).ConfigureAwait(false);
                    _lastSchedulerMetrics = scheduledBatch.Metrics;
                    coreScheduled = scheduledBatch.Results.Where(item => coreJobs.Any(job => job.FieldId.Equals(item.FieldId, StringComparison.OrdinalIgnoreCase))).ToArray();
                    extraScheduledByField = scheduledBatch.Results.Where(item => extraJobs.Any(job => job.FieldId.Equals(item.FieldId, StringComparison.OrdinalIgnoreCase)))
                        .ToDictionary(item => item.FieldId, StringComparer.OrdinalIgnoreCase);
                }
                catch
                {
                    foreach (var job in extraJobs) job.Dispose();
                    throw;
                }
                finally { foreach (var job in coreJobs) job.Dispose(); }
                var scheduledByField = coreScheduled.ToDictionary(item => item.FieldId, StringComparer.OrdinalIgnoreCase);
                (OcrDiagnosticResult Diagnostic, OcrReadResult Result, CharacterOcrComparison Comparison, OcrCacheLookup Cache) RequireCore(string field)
                {
                    var scheduled = scheduledByField[field];
                    if (scheduled.Value is { } value) return value;
                    throw new TimeoutException($"{field} OCR işi tamamlanamadı: {scheduled.FailureReason}");
                }
                var nominalRead = RequireCore("Nominal");
                nominalDiagnostic = nominalRead.Diagnostic;
                var scale1Read = RequireCore("Scale1");
                scale1Diagnostic = scale1Read.Diagnostic;
                var scale2Read = RequireCore("Scale2");
                scale2Diagnostic = scale2Read.Diagnostic;
                var gateRead = RequireCore("Gate");
                gateDiagnostic = gateRead.Diagnostic;

                // Karakter karşılaştırması resmi Nominal/Kantar ölçümünü değiştirmez.
                nominal = nominalRead.Result;
                scale1 = scale1Read.Result;
                scale2 = scale2Read.Result;
                gate = gateRead.Result;
                var displayedAt = gateRead.Comparison.DisplayedAt;
                var comparisons = new List<CharacterOcrComparison> { nominalRead.Comparison, gateRead.Comparison, scale1Read.Comparison, scale2Read.Comparison };
                var extraResults = new List<(NumericFieldDefinition Definition, OcrReadResult Result, CharacterOcrComparison Comparison)>();
                var reliabilityResults = new Dictionary<string, ProcessValueReliabilityResult>(StringComparer.OrdinalIgnoreCase);
                var extraDecisions = new List<SuspiciousValueDecision>();
                var nominalValidation = nominalRead.Cache.Hit ? _temporalValidation.Snapshot("Nominal", profile.SuspiciousValueConfirmationReadings)
                    : _temporalValidation.Validate(profile.Name, "Nominal", nominal.Value,
                    profile.NominalMaximumChangePerReading, profile.SuspiciousValueConfirmationReadings,
                    profile.SuspiciousValueConfirmationTolerance, images.NominalCapturedAt);
                var scale1Validation = scale1Read.Cache.Hit ? _temporalValidation.Snapshot("Kantar 1", profile.SuspiciousValueConfirmationReadings)
                    : _temporalValidation.Validate(profile.Name, "Kantar 1", scale1.Value,
                    profile.Scale1MaximumChangePerReading, profile.SuspiciousValueConfirmationReadings,
                    profile.SuspiciousValueConfirmationTolerance, images.Scale1CapturedAt);
                var scale2Validation = scale2Read.Cache.Hit ? _temporalValidation.Snapshot("Kantar 2", profile.SuspiciousValueConfirmationReadings)
                    : _temporalValidation.Validate(profile.Name, "Kantar 2", scale2.Value,
                    profile.Scale2MaximumChangePerReading, profile.SuspiciousValueConfirmationReadings,
                    profile.SuspiciousValueConfirmationTolerance, images.Scale2CapturedAt);
                nominal = ApplyTemporalValidation(nominal, nominalValidation);
                scale1 = ApplyTemporalValidation(scale1, scale1Validation);
                scale2 = ApplyTemporalValidation(scale2, scale2Validation);
                if (!gateRead.Cache.Hit) gate = StabilizeGate(gate);

                void StoreIfReal(string key, Bitmap image, OcrReadResult original, OcrReadResult official, CharacterOcrComparison comparison)
                {
                    if (!comparison.RealOcrExecuted) return;
                    _ocrCache.Store(key, image, original, comparison.CharacterResult, official,
                        official.SelectionReason ?? "OCR", comparison.FailureReason, DateTime.Now, comparison,
                        !official.IsSuccessful || comparison.FailureReason == "Timeout");
                }
                StoreIfReal("Nominal", images.Nominal, nominalRead.Result, nominal, comparisons[0]);
                StoreIfReal("Gate", images.Gate, OcrService.SelectFullLineTesseractResult(gateDiagnostic.Candidates), gate, comparisons[1]);
                StoreIfReal("Scale1", images.Scale1, scale1Read.Result, scale1, comparisons[2]);
                StoreIfReal("Scale2", images.Scale2, scale2Read.Result, scale2, comparisons[3]);

                void CaptureMismatch(int index, string field, OcrDiagnosticResult diagnostic,
                    decimal? official, ScreenRegion region)
                {
                    if (!comparisons[index].RealOcrExecuted) return;
                    var capture = _mismatchCapture.TryEnqueue(profile.Name, field, comparisons[index],
                        diagnostic, official, region, profile.Name);
                    comparisons[index] = comparisons[index] with
                    {
                        MismatchSampleSaved = capture.Saved,
                        MismatchSampleId = capture.SampleId,
                        MismatchSamplePath = capture.SamplePath
                    };
                }
                CaptureMismatch(0, "Nominal", nominalDiagnostic, nominal.Value, _settings.NominalRegion);
                CaptureMismatch(1, "Gate", gateDiagnostic, gate.Value, _settings.GateRegion);
                CaptureMismatch(2, "Scale1", scale1Diagnostic, scale1.Value, _settings.Scale1Region);
                CaptureMismatch(3, "Scale2", scale2Diagnostic, scale2.Value, _settings.Scale2Region);
                foreach (var definition in additionalDefinitions)
                {
                    try
                    {
                        DetailedTrace($"Field={definition.FieldId} | RegionConfigured={definition.RegionCoordinates.IsValid} | CaptureAttempted=true | DetailedDiagnostics={(_settings.EnableStableParameterDiagnostics ? "Enabled" : "Disabled")}");
                        var fieldKind = GetFieldKind(definition.FieldId);
                        var job = extraJobs.First(item => item.FieldId.Equals(definition.FieldId, StringComparison.OrdinalIgnoreCase));
                        var image = job.Crop;
                        var capturedAt = job.CapturedAt;
                        var scheduled = extraScheduledByField[definition.FieldId];
                        if (scheduled.FailureReason is not null) throw new TimeoutException($"{definition.DisplayName}: {scheduled.FailureReason}");
                        var read = scheduled.Value;
                        using var diagnostic = read.Diagnostic;
                        var result = read.Result;
                        if (definition.UsesSuspiciousValueFilter)
                        {
                            var validation = read.Cache.Hit
                                ? _temporalValidation.Snapshot(definition.FieldId, definition.ConfirmationReadings)
                                : _temporalValidation.Validate(profile.Name, definition.FieldId, result.Value,
                                    definition.MaximumChangePerReading, definition.ConfirmationReadings,
                                    definition.ConfirmationTolerance, capturedAt);
                            result = ApplyTemporalValidation(result, validation); extraDecisions.AddRange(validation.Decisions);
                        }
                        StoreIfReal(definition.FieldId, image, read.Result, result, read.Comparison);
                        var displayCandidate = diagnostic.Candidates.Where(candidate => candidate.ParsedValue.HasValue)
                            .OrderByDescending(candidate => ReferenceEquals(candidate, diagnostic.SelectedCandidate))
                            .ThenByDescending(candidate => candidate.SystemConfidence)
                            .ThenByDescending(candidate => candidate.Confidence)
                            .FirstOrDefault();
                        var extraComparison = read.Comparison with
                        {
                            RawOcrText = !string.IsNullOrWhiteSpace(result.RawText) ? result.RawText : displayCandidate?.RawText ?? read.Comparison.RawOcrText,
                            ParsedNumericValue = result.Value ?? displayCandidate?.ParsedValue ?? read.Comparison.FullLineResult ?? read.Comparison.CharacterResult,
                            NumericConfidencePercent = displayCandidate is null ? read.Comparison.NumericConfidencePercent : displayCandidate.Confidence * 100d,
                            Unit = definition.Unit,
                            RegionValid = definition.RegionCoordinates.IsValid
                        };
                        if (fieldKind == OcrFieldKind.StableTorque && !result.Value.HasValue)
                            extraComparison = extraComparison with
                            {
                                ParsedNumericValue = null,
                                FullLineResult = null,
                                FailureReason = "DecimalStructureIncomplete"
                            };
                        if (fieldKind == OcrFieldKind.StableThrustForce)
                        {
                            var estimatedDigits = OcrService.EstimateThrustDigitCount(diagnostic.Candidates);
                            var observedDigits = extraComparison.ParsedNumericValue?.ToString("0",
                                System.Globalization.CultureInfo.InvariantCulture).Count(char.IsDigit) ?? 0;
                            var implausible = extraComparison.ParsedNumericValue.HasValue
                                && !OcrService.IsPlausibleThrustValue(extraComparison.ParsedNumericValue, observedDigits, estimatedDigits);
                            extraComparison = extraComparison with
                            {
                                ParsedNumericValue = implausible ? null : result.Value ?? extraComparison.ParsedNumericValue,
                                FailureReason = implausible ? "DigitCountMismatch"
                                    : result.Warning is not null ? "CharacterMismatch" : extraComparison.FailureReason
                            };
                        }
                        if (fieldKind is OcrFieldKind.StableTorque or OcrFieldKind.StableThrustForce)
                        {
                            // Observed is what this frame actually read. In particular, never turn a
                            // full-line 1725 into a reconstructed 18725 merely because the old trusted
                            // process level was 18xxx.
                            var observed = fieldKind == OcrFieldKind.StableThrustForce
                                ? extraComparison.FullLineResult ?? extraComparison.ParsedNumericValue
                                : extraComparison.ParsedNumericValue ?? extraComparison.FullLineResult;
                            var observedRaw = fieldKind == OcrFieldKind.StableThrustForce && extraComparison.FullLineResult.HasValue
                                ? extraComparison.FullLineResult.Value.ToString("0", System.Globalization.CultureInfo.InvariantCulture)
                                : extraComparison.RawOcrText;
                            var reliability = _processReliability.Evaluate(new(profile.Name, definition.FieldId,
                                observed, observedRaw ?? "", capturedAt, read.Cache.Hit,
                                !observed.HasValue ? OcrConfidenceStatus.Unreadable
                                    : extraComparison.FailureReason == "Success" ? OcrConfidenceStatus.Reliable : OcrConfidenceStatus.Partial));
                            reliabilityResults[definition.FieldId] = reliability;
                            DetailedTrace($"Field={definition.FieldId} | ObservedValue={reliability.ObservedValue?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null"} | ObservedQuality={reliability.ObservedQuality} | TrustedValue={reliability.TrustedValue?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null"} | TrustedAccepted={reliability.TrustedAccepted} | StructureStatus={reliability.StructureStatus} | TemporalStatus={reliability.TemporalStatus} | Reason={reliability.Reason}");
                            extraComparison = extraComparison with
                            {
                                RawOcrText = observedRaw ?? "",
                                ParsedNumericValue = observed,
                                FailureReason = reliability.ObservedQuality switch
                                {
                                    ProcessValueQuality.TemporalSuspicious => "TemporalSuspicious",
                                    ProcessValueQuality.StructureSuspicious => reliability.StructureStatus,
                                    ProcessValueQuality.Unreadable => extraComparison.FailureReason,
                                    _ => extraComparison.FailureReason
                                }
                            };
                        }
                        if (extraComparison.RealOcrExecuted)
                        {
                            var capture = _mismatchCapture.TryEnqueue(profile.Name, definition.FieldId, extraComparison,
                                diagnostic, result.Value, definition.RegionCoordinates, profile.Name);
                            extraComparison = extraComparison with { MismatchSampleSaved = capture.Saved,
                                MismatchSampleId = capture.SampleId, MismatchSamplePath = capture.SamplePath };
                        }
                        StoreFeedbackSnapshot(measurementId, definition, diagnostic, extraComparison,
                            reliabilityResults.GetValueOrDefault(definition.FieldId));
                        comparisons.Add(extraComparison); extraResults.Add((definition, result, extraComparison));
                        DetailedTrace($"Field={definition.FieldId} | RegionConfigured={definition.RegionCoordinates.IsValid} | CaptureAttempted=true | CapturedAt={capturedAt:O} | OcrExecuted={extraComparison.RealOcrExecuted} | RawOcrText=\"{extraComparison.RawOcrText}\" | FullLineResult={extraComparison.FullLineResult?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null"} | ParsedValue={extraComparison.ParsedNumericValue?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null"} | OfficialValue=null | FailureReason={extraComparison.FailureReason} | ResultPublished=pending");
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        var failedAt = DateTime.Now;
                        var failure = OcrReadResult.Failed("", "", 0, $"{definition.DisplayName} okunamadı: {ex.Message}");
                        var comparison = new CharacterOcrComparison(definition.FieldId, null, null, null, 0, 0,
                            failedAt, failedAt, 0, "OcrError", RawOcrText: failure.RawText,
                            ParsedNumericValue: null, Unit: definition.Unit, RegionValid: definition.RegionCoordinates.IsValid);
                        comparisons.Add(comparison); extraResults.Add((definition, failure, comparison));
                        StoreFailedFeedbackSnapshot(measurementId, definition,
                            extraJobs.FirstOrDefault(item => item.FieldId.Equals(definition.FieldId, StringComparison.OrdinalIgnoreCase)),
                            comparison, ex.Message);
                        DiagnosticWarning?.Invoke($"{definition.DisplayName} tanılama OCR hatası: {ex.Message}");
                    }
                }
                foreach (var job in extraJobs) job.Dispose();
                var loggingWatch=System.Diagnostics.Stopwatch.StartNew();
                _comparisonCsv.Append(_settings.EnableStableParameterDiagnostics
                    ? comparisons
                    : comparisons.Where(item => !IsStableParameter(item.Field)).ToArray());

                var decisions = nominalValidation.Decisions.Concat(scale1Validation.Decisions).Concat(scale2Validation.Decisions).Concat(extraDecisions).ToArray();
                var cacheStatistics = profile.Fields.Where(field => field.IsEnabled).Select(field => _ocrCache.Statistics(field.FieldId)).ToArray();
                _suspiciousDecisionCsv.Append(decisions);
                loggingWatch.Stop();loggingMs+=loggingWatch.Elapsed.TotalMilliseconds;
                PerformanceAvailable?.Invoke(new OcrPerformanceFrame(DateTime.Now, comparisons,
                    cacheStatistics,
                    comparisons.Count(item => item.FailureReason != "Success"),
                    decisions.Count(item => item.Decision == "Pending"),
                    decisions.Count(item => item.Reason.Contains("sıçraması filtrelendi", StringComparison.OrdinalIgnoreCase)),
                    _mismatchCapture.SavedCount, _mismatchCapture.PendingCount) { FieldDefinitions = profile.Fields });
                foreach (var decision in decisions.Where(item => item.Reason is "Ardışık doğrulama ile kabul edildi" or "Tek karelik OCR sıçraması filtrelendi"))
                    ValidationDecisionLogged?.Invoke($"{decision.Field}: {decision.Reason} (aday={decision.CandidateValue:0.##}, son={decision.LastAcceptedValue:0.##})");
                ReportFieldErrors(nominal, scale1, scale2, gate);
                var record = CreateRecord(images.CapturedAt, nominal, scale1, scale2, gate,
                    nominalValidation, scale1Validation, scale2Validation, comparisons, extraResults, reliabilityResults, measurementId, feedbackMatches);
                // The preview subscriber marshals the frame to the UI thread and owns/disposes the
                // diagnostic bitmaps.  Finish every background consumer before transferring that
                // ownership; otherwise preview cloning can race snapshot cloning/disposal and GDI+
                // reports "Object is currently in use elsewhere".
                StoreFeedbackSnapshot(measurementId, profile.FindField(BuiltInFieldIds.Nominal), nominalDiagnostic, comparisons[0], null);
                StoreFeedbackSnapshot(measurementId, profile.FindField(BuiltInFieldIds.Gate), gateDiagnostic, comparisons[1], null);
                StoreFeedbackSnapshot(measurementId, profile.FindField(BuiltInFieldIds.Scale1), scale1Diagnostic, comparisons[2], null);
                StoreFeedbackSnapshot(measurementId, profile.FindField(BuiltInFieldIds.Scale2), scale2Diagnostic, comparisons[3], null);
                foreach (var extra in extraResults)
                    if (record.FieldResults.TryGetValue(extra.Definition.FieldId, out var fieldResult))
                    {
                        NumericFieldReadAvailable?.Invoke(fieldResult);
                        DetailedTrace($"Field={fieldResult.FieldId} | CapturedAt={fieldResult.CapturedAt:O} | RawOcrText=\"{fieldResult.RawValue}\" | FullLineResult={fieldResult.FullLineResult?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null"} | ParsedValue={fieldResult.ParsedValue?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null"} | OfficialValue={fieldResult.OfficialValue?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null"} | FailureReason={fieldResult.FailureReason} | ResultPublished=true");
                    }
                operationModeOcrMs = await ProcessOperationModeAsync(cancellationToken,images.ScreenImage).ConfigureAwait(false);
                if (Volatile.Read(ref _forcedRealOcrReadings) > 0) Interlocked.Decrement(ref _forcedRealOcrReadings);
                _previous = record; cycleWatch.Stop();
                _livePerformance.AppendCycle(DateTime.Now, captureTotalMs, numericOcrTotalMs, operationModeOcrMs,
                    liveFieldCount, liveCacheHits, liveFullExecutions, cycleWatch.Elapsed.TotalMilliseconds);
                var fieldProfiles=comparisons.Select(item=>new OcrFieldProfile(item.Field,item.CaptureMs,0,item.FastFullLineOcrMs,
                    item.CharacterSegmentationMs,item.TemplateMatchMs,item.FastFullLineOcrMs,
                    Math.Max(0,item.TotalOcrDurationMilliseconds-item.FastFullLineOcrMs),item.FeedbackMatchingMs,0,0,
                    item.TotalOcrDurationMilliseconds,item.ExecutionPath is "Fast" or "Cached",item.FailureReason,
                    item.ExecutionPath.Contains("Heavy",StringComparison.OrdinalIgnoreCase),item.ExecutionPath.Contains("Heavy",StringComparison.OrdinalIgnoreCase)?item.TotalOcrDurationMilliseconds:0)).ToArray();
                var materialMs=fieldProfiles.Where(x=>x.Field is "Nominal" or "Kapak mm" or "Kantar 1" or "Kantar 2").Sum(x=>x.TotalFieldMs);
                var injectionMs=fieldProfiles.Where(x=>x.Field.Contains("Enjeksiyon",StringComparison.OrdinalIgnoreCase)).Sum(x=>x.TotalFieldMs);
                var epbMs=fieldProfiles.Where(x=>x.Field.StartsWith("EPB",StringComparison.OrdinalIgnoreCase)).Sum(x=>x.TotalFieldMs);
                var feedbackMs=fieldProfiles.Sum(x=>x.FeedbackMatchingMs);
                var stages=new Dictionary<string,double>{{"ScreenCapture",captureTotalMs},{"NumericFields",numericOcrTotalMs},{"OperationMode",operationModeOcrMs},{"CsvLogging",loggingMs},{"Feedback",feedbackMs}};
                foreach(var f in fieldProfiles)stages[$"{f.Field} OCR"]=f.TotalFieldMs;
                var slowest=stages.OrderByDescending(x=>x.Value).First();
                var accounted=captureTotalMs+numericOcrTotalMs+operationModeOcrMs+loggingMs;
                var cycleProfile=new MonitoringCycleProfile(DateTime.Now,profile.Name,measurementId,cycleWatch.Elapsed.TotalMilliseconds,
                    captureTotalMs,1,0,numericOcrTotalMs,materialMs,injectionMs,epbMs,operationModeOcrMs,0,0,0,loggingMs,0,feedbackMs,
                    Math.Max(0,cycleWatch.Elapsed.TotalMilliseconds-accounted),slowest.Key,slowest.Value,fieldProfiles)
                {
                    WorkerCount = _lastSchedulerMetrics?.WorkerCount ?? 1,
                    ParallelOcrWallClockMs = _lastSchedulerMetrics?.ParallelOcrWallClockMs ?? numericOcrTotalMs,
                    SumOfFieldOcrMs = _lastSchedulerMetrics?.SumOfFieldOcrMs ?? numericOcrTotalMs,
                    AverageQueueWaitMs = _lastSchedulerMetrics?.AverageQueueWaitMs ?? 0,
                    OcrTimeoutCount = _lastSchedulerMetrics?.TimeoutCount ?? 0
                    ,QueueWaitTotalMs = _lastSchedulerMetrics?.QueueWaitTotalMs ?? 0
                    ,EngineAcquireWaitMs = _lastSchedulerMetrics?.EngineAcquireWaitMs ?? 0
                    ,CoreDashboardReadyMs = _lastSchedulerMetrics?.CoreDashboardReadyMs ?? 0
                    ,AdvanceDecisionReadyMs = _lastSchedulerMetrics?.AdvanceDecisionReadyMs ?? 0
                    ,FullMeasurementReadyMs = _lastSchedulerMetrics?.FullMeasurementReadyMs ?? numericOcrTotalMs
                };
                _pipelineProfiler.Record(cycleProfile);PipelinePerformanceAvailable?.Invoke(cycleProfile);
                if (_settings.ShowOcrPreview && DiagnosticsAvailable is not null)
                {
                    var frame = new OcrDiagnosticsFrame(
                        images.CapturedAt,
                        nominalDiagnostic,
                        scale1Diagnostic,
                        scale2Diagnostic,
                        gateDiagnostic,
                        displayedAt,
                        comparisons,
                        cacheStatistics);
                    ownershipTransferred = true;
                    DiagnosticsAvailable.Invoke(frame);
                }
                return record;
            }
            finally
            {
                if (!ownershipTransferred)
                {
                    nominalDiagnostic?.Dispose();
                    scale1Diagnostic?.Dispose();
                    scale2Diagnostic?.Dispose();
                    gateDiagnostic?.Dispose();
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            OcrFailed?.Invoke(exception);
            return UnreadableRecord(exception.Message);
        }
    }

    private void TraceStableParameterConfiguration()
    {
        var ids = new[] { BuiltInFieldIds.StableRpm, BuiltInFieldIds.StableTorque, BuiltInFieldIds.StableThrustForce,
            BuiltInFieldIds.StableAdvanceSpeed, BuiltInFieldIds.StablePenetration };
        foreach (var id in ids)
        {
            var definition = _settings.OcrProfile.FindField(id);
            var region = definition?.RegionCoordinates;
            DetailedTrace($"Field={id} | RegionConfigured={region?.IsValid == true} | CaptureAttempted=false | OcrExecuted=false | DetailedDiagnostics={(_settings.EnableStableParameterDiagnostics ? "Enabled" : "Disabled")} | Profile={_settings.OcrProfile.Name} | FieldEnabled={definition?.IsEnabled == true} | FailureReason={(definition?.IsEnabled == true ? "AwaitingCapture" : "RegionOrProfileDisabled")} | ResultPublished=false");
        }
    }

    internal static IReadOnlyList<NumericFieldDefinition> GetEnabledAdditionalFieldDefinitions(OcrProfile profile)
        => profile.Fields.Where(field => field.IsEnabled
            && field.Category == MeasurementCategory.OcrMeasurement
            && field.ValueType is FieldValueType.Integer or FieldValueType.Decimal
            && field.FieldId is not BuiltInFieldIds.Nominal and not BuiltInFieldIds.Gate and not BuiltInFieldIds.Scale1 and not BuiltInFieldIds.Scale2
            && field.RegionCoordinates.Width > 0 && field.RegionCoordinates.Height > 0)
            .OrderBy(field => field.DisplayOrder).ToArray();

    private void DetailedTrace(string message)
    {
        if (_settings.EnableStableParameterDiagnostics) LiveFieldTraceAvailable?.Invoke(message);
    }

    private static bool IsStableParameter(string fieldId) => fieldId is BuiltInFieldIds.StableRpm or BuiltInFieldIds.StableTorque
        or BuiltInFieldIds.StableThrustForce or BuiltInFieldIds.StableAdvanceSpeed or BuiltInFieldIds.StablePenetration;

    internal static OcrFieldKind GetFieldKind(string fieldId) => fieldId switch
    {
        BuiltInFieldIds.StableRpm => OcrFieldKind.StableRpm,
        BuiltInFieldIds.StableTorque => OcrFieldKind.StableTorque,
        BuiltInFieldIds.StableThrustForce => OcrFieldKind.StableThrustForce,
        BuiltInFieldIds.StableAdvanceSpeed => OcrFieldKind.StableAdvanceSpeed,
        BuiltInFieldIds.StablePenetration => OcrFieldKind.StablePenetration,
        BuiltInFieldIds.NominalInjection => OcrFieldKind.NominalInjection,
        BuiltInFieldIds.RealInjection => OcrFieldKind.RealInjection,
        BuiltInFieldIds.Epb1 => OcrFieldKind.Epb1, BuiltInFieldIds.Epb2 => OcrFieldKind.Epb2,
        BuiltInFieldIds.Epb3 => OcrFieldKind.Epb3, BuiltInFieldIds.Epb4 => OcrFieldKind.Epb4,
        BuiltInFieldIds.Epb5 => OcrFieldKind.Epb5,
        _ when fieldId.StartsWith("Advance", StringComparison.OrdinalIgnoreCase) => OcrFieldKind.Advance,
        _ => OcrFieldKind.General
    };

    internal static OcrJobPriority GetJobPriority(string fieldId)
        => fieldId.StartsWith("Advance", StringComparison.OrdinalIgnoreCase)
            || IsStableParameter(fieldId) ? OcrJobPriority.High
            : fieldId.StartsWith("EPB", StringComparison.OrdinalIgnoreCase) ? OcrJobPriority.Low
            : OcrJobPriority.Medium;

    private static OcrSchedulerCycleMetrics MergeSchedulerMetrics(OcrSchedulerCycleMetrics? first, OcrSchedulerCycleMetrics second)
    {
        if (first is null) return second;
        var count = first.JobCount + second.JobCount;
        return new(second.WorkerCount, count, first.ParallelOcrWallClockMs + second.ParallelOcrWallClockMs,
            first.SumOfFieldOcrMs + second.SumOfFieldOcrMs,
            count == 0 ? 0 : (first.AverageQueueWaitMs * first.JobCount + second.AverageQueueWaitMs * second.JobCount) / count,
            first.TimeoutCount + second.TimeoutCount, first.CancelledCount + second.CancelledCount);
    }

    public void Dispose()
    {
        OcrCacheControl.ClearRequested -= OnCacheClearRequested;
        DigitTemplateMatcher.TemplatesReloaded -= OnTemplatesReloaded;
        _ocrCache.Warning -= OnCacheWarning;
        _ocrCache.Dispose();
        _textOcrService?.Dispose();
        _mismatchCapture.Dispose();
        _pipelineProfiler.Dispose();
        _ocrScheduler.Dispose();
        _ocrService.Dispose();
        _feedbackLearning.Dispose();
        DigitTemplateUsageTracker.Flush();
    }

    private void OnCacheWarning(string message) => DiagnosticWarning?.Invoke(message);
    private void OnCacheClearRequested() { _ocrCache.Clear(); DiagnosticWarning?.Invoke("OCR önbelleği temizlendi."); }
    private void OnTemplatesReloaded(string _) => _ocrCache.Clear();

    private async Task<double> ProcessOperationModeAsync(CancellationToken cancellationToken, Bitmap? capturedScreen)
    {
        if (_textOcrService is null || _operationModeState is null || !_settings.OperationModeRegion.IsValid) return 0;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var image = capturedScreen is null
                ? _captureService.CaptureRegionWithPadding(_settings.ScreenNumber, _settings.OperationModeRegion, _settings.OcrProfile, OcrFieldKind.General)
                : ScreenCaptureService.CropRegionWithPadding(capturedScreen,_settings.OperationModeRegion,_settings.OcrProfile,OcrFieldKind.General);
            var text = await _textOcrService.ReadAsync(image, cancellationToken).ConfigureAwait(false);
            var processed = _operationModeState.Process(text.RawText, text.Confidence, DateTime.Now);
            OperationModeDiagnosticAvailable?.Invoke(processed.Diagnostic);
            if (text.Error is not null) DiagnosticWarning?.Invoke($"Çalışma modu OCR okunamadı: {text.Error}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) { DiagnosticWarning?.Invoke($"Çalışma modu tanılaması başarısız; resmî ölçüm etkilenmedi: {exception.Message}"); }
        finally { watch.Stop(); }
        return watch.Elapsed.TotalMilliseconds;
    }

    private static OcrReadResult ApplyTemporalValidation(OcrReadResult source, TemporalValidationResult validation)
    {
        if (!validation.Value.HasValue) return source;
        return source with
        {
            Value = validation.Value,
            CleanedText = validation.Value.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
            Error = null,
            Warning = validation.Status
        };
    }

    private OcrReadResult StabilizeGate(OcrReadResult result)
    {
        if (!result.IsSuccessful || result.Value is null) return result;
        var current = result.Value.Value;
        if (_stableGateValue is null)
        {
            _stableGateValue = current;
            return result;
        }

        var maximumChange = _settings.OcrProfile.GateMaximumChangePerReading;
        if (Math.Abs(current - _stableGateValue.Value) <= maximumChange)
        {
            _stableGateValue = current;
            _pendingGateValue = null;
            _pendingGateCount = 0;
            return result;
        }

        var confirmationTolerance = Math.Max(1m, Math.Min(
            _settings.OcrProfile.GateChangeTolerance,
            maximumChange));
        if (_pendingGateValue.HasValue && Math.Abs(current - _pendingGateValue.Value) <= confirmationTolerance)
            _pendingGateCount++;
        else
        {
            _pendingGateValue = current;
            _pendingGateCount = 1;
        }

        if (_pendingGateCount >= _settings.OcrProfile.GateConfirmationReadings)
        {
            _stableGateValue = current;
            _pendingGateValue = null;
            _pendingGateCount = 0;
            return result;
        }

        return result with
        {
            Value = _stableGateValue,
            CleanedText = _stableGateValue.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
            Confidence = result.Confidence * 0.75f,
            Error = null,
            Warning = $"Şüpheli kapak değeri {current:0.##} mm bekletiliyor; son kararlı değer {_stableGateValue:0.##} mm yayınlandı."
        };
    }

    private MeasurementRecord CreateRecord(
        DateTime timestamp,
        OcrReadResult nominal,
        OcrReadResult scale1,
        OcrReadResult scale2,
        OcrReadResult gate,
        TemporalValidationResult nominalValidation,
        TemporalValidationResult scale1Validation,
        TemporalValidationResult scale2Validation,
        IReadOnlyList<CharacterOcrComparison> comparisons,
        IReadOnlyList<(NumericFieldDefinition Definition, OcrReadResult Result, CharacterOcrComparison Comparison)> extraResults,
        IReadOnlyDictionary<string, ProcessValueReliabilityResult>? reliabilityResults = null,
        string? measurementId = null,
        IReadOnlyDictionary<string, VerifiedFeedbackMatch>? feedbackMatches = null)
    {
        var results = new[] { nominal, scale1, scale2, gate };
        var readableCount = results.Count(result => result.IsSuccessful);
        var confidence = OfficialOcrConfidenceEvaluator.FromResults(results);
        var gateStatus = gate.Value switch
        {
            null => "Okunamadı",
            0m => "Tam Kapalı",
            <= 100m => "Kısmen Açık - Malzeme Akışı Yok",
            _ => "Açık - Malzeme Akışı Var"
        };
        if (!string.IsNullOrWhiteSpace(gate.Warning))
            gateStatus += " • Şüpheli değer bekletiliyor";
        var excavationStatus = EvaluateStatus(nominal.Value, scale1.Value, scale2.Value, gate.Value, readableCount);
        FieldMeasurementResult Field(string id, OcrReadResult result, CharacterOcrComparison comparison) => new()
        {
            FieldId = id, RawValue = result.RawText,
                ParsedValue = result.Value ?? comparison.ParsedNumericValue ?? comparison.FullLineResult ?? comparison.CharacterResult,
            OfficialValue = result.Value,
            FullLineResult = comparison.FullLineResult, CharacterResult = comparison.CharacterResult,
            DecisionSource = comparison.CacheUsed ? "Cache" : result.SelectionReason ?? "OCR",
            ConfidenceStatus = result.IsSuccessful ? OcrConfidenceStatus.Reliable : OcrConfidenceStatus.Unreadable,
            FailureReason = result.Warning?.Contains("Şüpheli", StringComparison.OrdinalIgnoreCase) == true
                ? "SuspiciousPending" : comparison.FailureReason, CapturedAt = comparison.CapturedAt,
            CompletedAt = comparison.DisplayedAt, ProcessingDurationMs = comparison.TotalOcrDurationMilliseconds,
            TotalLatencyMs = comparison.TotalLatencyMilliseconds, CacheUsed = comparison.CacheUsed,
            OcrConfidencePercent = comparison.NumericConfidencePercent > 0 ? comparison.NumericConfidencePercent : result.Confidence * 100d,
                ObservedValue = result.Value ?? comparison.ParsedNumericValue ?? comparison.FullLineResult ?? comparison.CharacterResult,
            AcceptedValue = result.Value
        };
        var fieldResults = new Dictionary<string, FieldMeasurementResult>(StringComparer.OrdinalIgnoreCase)
        {
            [BuiltInFieldIds.Nominal] = Field(BuiltInFieldIds.Nominal, nominal, comparisons[0]),
            [BuiltInFieldIds.Gate] = Field(BuiltInFieldIds.Gate, gate, comparisons[1]),
            [BuiltInFieldIds.Scale1] = Field(BuiltInFieldIds.Scale1, scale1, comparisons[2]),
            [BuiltInFieldIds.Scale2] = Field(BuiltInFieldIds.Scale2, scale2, comparisons[3])
        };
        fieldResults[BuiltInFieldIds.Nominal] = WithValidation(fieldResults[BuiltInFieldIds.Nominal], nominalValidation);
        fieldResults[BuiltInFieldIds.Scale1] = WithValidation(fieldResults[BuiltInFieldIds.Scale1], scale1Validation);
        fieldResults[BuiltInFieldIds.Scale2] = WithValidation(fieldResults[BuiltInFieldIds.Scale2], scale2Validation);
        foreach (var extra in extraResults)
        {
            var field = Field(extra.Definition.FieldId, extra.Result, extra.Comparison);
            if (string.Equals(extra.Definition.ReportGroup, "StableDiagnostics", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extra.Definition.ReportGroup, "InjectionDiagnostics", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extra.Definition.ReportGroup, "EpbDiagnostics", StringComparison.OrdinalIgnoreCase))
                field = field with
                {
                    RawValue = string.IsNullOrWhiteSpace(field.RawValue) ? extra.Comparison.RawOcrText : field.RawValue,
                    ParsedValue = field.ParsedValue ?? extra.Comparison.ParsedNumericValue ?? extra.Comparison.FullLineResult ?? extra.Comparison.CharacterResult,
                    OfficialValue = null,
                    OcrConfidencePercent = extra.Comparison.NumericConfidencePercent,
                    DecisionSource = !string.IsNullOrWhiteSpace(extra.Result.DecisionSource)
                        ? $"DiagnosticOnly/{extra.Result.DecisionSource}"
                        : extra.Comparison.FailureReason == "CharacterMismatch"
                            ? "DiagnosticOnly/TesseractFallback" : "DiagnosticOnly",
                    ConfidenceStatus = field.ParsedValue.HasValue
                        ? extra.Comparison.FailureReason == "Success" ? OcrConfidenceStatus.Reliable : OcrConfidenceStatus.Partial
                        : OcrConfidenceStatus.Unreadable,
                    FailureReason = field.ParsedValue.HasValue && extra.Comparison.FailureReason == "CharacterMismatch"
                        ? "CharacterMismatch" : field.FailureReason
                };
            if (reliabilityResults?.TryGetValue(extra.Definition.FieldId, out var reliability) == true)
                field = field with
                {
                    // Diagnostic kart/history bu turda gerçekten gözlenen OCR değerini gösterir.
                    // TrustedValue ayrı tutulur; reliability filtresi ve resmî karar değiştirilmez.
                    // ParsedValue/ObservedValue this capture'a aittir.  Önceki güvenilir
                    // proses değeri yalnız TrustedValue'da tutulur; aksi halde history satırı
                    // ve aynı satırın feedback görüntüsü farklı değerler gösterir.
                    ParsedValue = reliability.ObservedValue ?? field.ParsedValue,
                    ObservedValue = reliability.ObservedValue,
                    ObservedAt = reliability.ObservedAt,
                    ObservedQuality = MapQuality(reliability.ObservedQuality),
                    TrustedValue = reliability.TrustedValue,
                    TrustedAt = reliability.TrustedAt,
                    TrustedQuality = MapQuality(reliability.TrustedQuality),
                    AcceptedValue = reliability.TrustedValue,
                    ReliabilityReason = reliability.Reason,
                    ValidationState = reliability.TemporalStatus
                };
            if (feedbackMatches?.TryGetValue(extra.Definition.FieldId, out var feedback) == true)
                field = field with
                {
                    FeedbackCandidateValue = feedback.CandidateValue,
                    FeedbackSimilarityPercent = feedback.Similarity * 100,
                    FeedbackConfidencePercent = feedback.Confidence * 100,
                    FeedbackSupportingExamples = feedback.SupportingExamples,
                    FeedbackConflictCount = feedback.ConflictCount,
                    FeedbackDecisionLevel = feedback.Level.ToString(),
                    FeedbackDecisionReason = feedback.Reason
                };
            fieldResults[extra.Definition.FieldId] = field;
        }
        return new MeasurementRecord
        {
            MeasurementId = measurementId ?? Guid.NewGuid().ToString("N"),
            Timestamp = timestamp,
            Nominal = nominal.Value,
            Scale1 = scale1.Value,
            Scale2 = scale2.Value,
            GateMillimeters = gate.Value,
            FieldResults = fieldResults,
            GateStatus = gateStatus,
            ExcavationStatus = excavationStatus,
            OcrConfidence = confidence,
            NominalValidationStatus = nominalValidation.Status,
            Scale1ValidationStatus = scale1Validation.Status,
            Scale2ValidationStatus = scale2Validation.Status,
            HasHeldSuspiciousValue = nominalValidation.IsHeld || scale1Validation.IsHeld || scale2Validation.IsHeld
        };

        static OcrConfidenceStatus MapQuality(ProcessValueQuality quality) => quality switch
        { ProcessValueQuality.Reliable => OcrConfidenceStatus.Reliable, ProcessValueQuality.Unreadable => OcrConfidenceStatus.Unreadable, _ => OcrConfidenceStatus.Partial };

        static FieldMeasurementResult WithValidation(FieldMeasurementResult field, TemporalValidationResult validation) => field with
        {
            ObservedValue = field.ObservedValue ?? field.ParsedValue ?? field.FullLineResult ?? field.CharacterResult,
            AcceptedValue = validation.Value,
            PendingValue = validation.PendingValue,
            ValidationState = validation.ValidationState
        };
    }

    private void StoreFeedbackSnapshot(string measurementId, NumericFieldDefinition? definition,
        OcrDiagnosticResult diagnostic, CharacterOcrComparison comparison, ProcessValueReliabilityResult? reliability)
    {
        if (_feedbackSnapshots is null || definition is null) return;
        var observed = reliability?.ObservedValue ?? comparison.ParsedNumericValue ?? comparison.FullLineResult ?? comparison.CharacterResult;
        var warning = comparison.FailureReason != "Success" || reliability?.ObservedQuality is
            ProcessValueQuality.TemporalSuspicious or ProcessValueQuality.StructureSuspicious or ProcessValueQuality.Partial;
        if (!warning) return;
        _feedbackSnapshots.Store(new OcrFeedbackSnapshot
        {
            MeasurementId=measurementId,FieldId=definition.FieldId,CapturedAt=comparison.CapturedAt,
            RawCrop=diagnostic.RawImage,ProcessedCrop=diagnostic.ProcessedImage,RawOcrText=comparison.RawOcrText,
            FullLineResult=comparison.FullLineResult,CharacterResult=comparison.CharacterResult,
            TemplateResult=diagnostic.SelectedCandidate?.TemplateResult ?? "",
            ObservedValue=observed,TrustedValue=reliability?.TrustedValue,QualityStatus=(reliability?.ObservedQuality.ToString() ?? comparison.FailureReason),
            FailureReason=reliability?.Reason ?? comparison.FailureReason,DecisionSource=reliability?.TemporalStatus ?? diagnostic.Result.DecisionSource ?? "OCR",
            Pipeline=diagnostic.PrimaryPipeline,Region=definition.RegionCoordinates
        });
    }

    private void StoreFailedFeedbackSnapshot(string measurementId, NumericFieldDefinition definition,
        OcrFieldJob? job, CharacterOcrComparison comparison, string failureReason)
    {
        if (_feedbackSnapshots is null || job is null) return;
        _feedbackSnapshots.Store(new OcrFeedbackSnapshot
        {
            MeasurementId = measurementId, FieldId = definition.FieldId, CapturedAt = job.CapturedAt,
            RawCrop = job.Crop, ProcessedCrop = job.Crop, RawOcrText = comparison.RawOcrText,
            QualityStatus = "Unreadable", FailureReason = failureReason,
            DecisionSource = "SchedulerFailure", Pipeline = "Live OCR",
            Region = definition.RegionCoordinates
        });
    }

    private string EvaluateStatus(decimal? nominal, decimal? scale1, decimal? scale2, decimal? gate, int readableCount)
    {
        if (readableCount < 4 || gate is null) return "Ekran Değerleri Okunamadı";
        if (gate == 0) return "Kapak Tam Kapalı";
        if (gate <= 100) return "Kapak Kısmen Açık - Malzeme Akışı Beklenmiyor";
        if (_previous is not null
            && (nominal > _previous.Nominal || scale1 > _previous.Scale1 || scale2 > _previous.Scale2))
        {
            return "Kazı / Taşıma Devam Ediyor";
        }

        return "Kapak Açık - İlerleme Bekleniyor";
    }

    private void ReportFieldErrors(params OcrReadResult[] results)
    {
        var names = new[] { "Nominal", "Kantar 1", "Kantar 2", "Kapak" };
        for (var index = 0; index < results.Length; index++)
        {
            if (!results[index].IsSuccessful)
            {
                OcrFailed?.Invoke(new OcrReadException($"{names[index]} okunamadı: {results[index].Error}"));
            }
        }
    }

    private void SaveImages(CapturedScreenRegions images)
    {
        _captureService.SaveRegion(images.Nominal, "Nominal", images.CapturedAt);
        _captureService.SaveRegion(images.Scale1, "Kantar1", images.CapturedAt);
        _captureService.SaveRegion(images.Scale2, "Kantar2", images.CapturedAt);
        _captureService.SaveRegion(images.Gate, "Kapak", images.CapturedAt);
    }

    private static MeasurementRecord UnreadableRecord(string error) => new()
    {
        Timestamp = DateTime.Now,
        GateStatus = "Okunamadı",
        ExcavationStatus = "Ekran Değerleri Okunamadı",
        OcrConfidence = OcrConfidenceStatus.Unreadable
    };
}

public sealed class OcrReadException : Exception
{
    public OcrReadException(string message) : base(message) { }
}

public sealed class OcrPreviewFrame : IDisposable
{
    private OcrPreviewFrame(Bitmap nominal, Bitmap scale1, Bitmap scale2, Bitmap gate, DateTime capturedAt)
    {
        Nominal = nominal;
        Scale1 = scale1;
        Scale2 = scale2;
        Gate = gate;
        CapturedAt = capturedAt;
    }

    public Bitmap Nominal { get; }
    public Bitmap Scale1 { get; }
    public Bitmap Scale2 { get; }
    public Bitmap Gate { get; }
    public DateTime CapturedAt { get; }

    public static OcrPreviewFrame From(CapturedScreenRegions source) => new(
        new Bitmap(source.Nominal),
        new Bitmap(source.Scale1),
        new Bitmap(source.Scale2),
        new Bitmap(source.Gate),
        source.CapturedAt);

    public void Dispose()
    {
        Nominal.Dispose();
        Scale1.Dispose();
        Scale2.Dispose();
        Gate.Dispose();
    }
}

public sealed class OcrDiagnosticsFrame : IDisposable
{
    public OcrDiagnosticsFrame(
        DateTime capturedAt,
        OcrDiagnosticResult nominal,
        OcrDiagnosticResult scale1,
        OcrDiagnosticResult scale2,
        OcrDiagnosticResult gate,
        DateTime? displayedAt = null,
        IReadOnlyList<CharacterOcrComparison>? comparisons = null,
        IReadOnlyList<OcrCacheStatistics>? cacheStatistics = null)
    {
        CapturedAt = capturedAt;
        Nominal = nominal;
        Scale1 = scale1;
        Scale2 = scale2;
        Gate = gate;
        DisplayedAt = displayedAt ?? DateTime.Now;
        Comparisons = comparisons ?? Array.Empty<CharacterOcrComparison>();
        CacheStatistics = cacheStatistics ?? Array.Empty<OcrCacheStatistics>();
    }

    public DateTime CapturedAt { get; }
    public DateTime DisplayedAt { get; }
    public IReadOnlyList<CharacterOcrComparison> Comparisons { get; }
    public IReadOnlyList<OcrCacheStatistics> CacheStatistics { get; }
    public OcrDiagnosticResult Nominal { get; }
    public OcrDiagnosticResult Scale1 { get; }
    public OcrDiagnosticResult Scale2 { get; }
    public OcrDiagnosticResult Gate { get; }

    public void Dispose()
    {
        Nominal.Dispose();
        Scale1.Dispose();
        Scale2.Dispose();
        Gate.Dispose();
    }
}
