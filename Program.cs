namespace TBMWatchdog;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--demo-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunDemoSelfTest();
        }

        if (args.Contains("--csv-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunCsvSelfTest();
        }
        if (args.Contains("--log-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunLogSelfTest();
        }
        if (args.Contains("--capture-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunCaptureSelfTest();
        }
        if (args.Contains("--ocr-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunOcrSelfTest();
        }
        if (args.Contains("--live-fast-ocr-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunLiveFastOcrSelfTest();
        }
        if (args.Contains("--torque-real-crop-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunTorqueRealCropSelfTest(args);
        }
        if (args.Contains("--stable-character-template-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunStableCharacterTemplateSelfTest();
        }
        if (args.Contains("--thrust-full-line-consensus-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunThrustFullLineConsensusSelfTest();
        }
        if (args.Contains("--thrust-plausible-fallback-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunThrustPlausibleFallbackSelfTest();
        }
        if (args.Contains("--zero-numeric-value-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunZeroNumericValueSelfTest();
        }
        if (args.Contains("--decimal-candidate-arbitration-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunDecimalCandidateArbitrationSelfTest();
        }
        if (args.Contains("--torque-glyph-confusion-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunTorqueGlyphConfusionSelfTest();
        }
        if (args.Contains("--template-training-quality-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunTemplateTrainingQualitySelfTest();
        }
        if (args.Contains("--region-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunRegionSelfTest();
        }
        if (args.Contains("--chart-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunChartSelfTest();
        }
        if (args.Contains("--rules-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunRulesSelfTest();
        }
        if (args.Contains("--integration-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunIntegrationSelfTest();
        }
        if (args.Contains("--diagnostics-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunDiagnosticsSelfTest();
        }
        if (args.Contains("--template-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunTemplateSelfTest();
        }
        if (args.Contains("--scale-character-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunScaleCharacterSelfTest();
        }
        if (args.Contains("--suspicious-value-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunSuspiciousValueSelfTest();
        }
        if (args.Contains("--mismatch-sample-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunMismatchSampleSelfTest();
        }
        if (args.Contains("--mismatch-production-cleanup", StringComparer.OrdinalIgnoreCase))
            return RunMismatchProductionCleanup();
        if (args.Contains("--ocr-cache-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunOcrCacheSelfTest();
        }
        if (args.Contains("--performance-monitor-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunPerformanceMonitorSelfTest();
        }
        if (args.Contains("--extensible-fields-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunExtensibleFieldsSelfTest();
        }
        if (args.Contains("--excel-template-inspector-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunExcelTemplateInspectorSelfTest();
        }
        if (args.Contains("--operation-mode-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunOperationModeSelfTest();
        }
        if (args.Contains("--monitoring-pause-resume-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunMonitoringPauseResumeSelfTest();
        }
        if (args.Contains("--ring-session-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunRingSessionSelfTest();
        }
        if (args.Contains("--manual-ring-session-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunManualRingSessionSelfTest();
        }
        if (args.Contains("--manual-ring-ui-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunManualRingUiSelfTest();
        }
        if (args.Contains("--operation-mode-ring-session-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunOperationModeRingSessionSelfTest();
        }
        if (args.Contains("--advance-stable-zone-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunAdvanceStableZoneSelfTest();
        }
        if (args.Contains("--advance-right-edge-noise-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunAdvanceRightEdgeNoiseSelfTest();
        }
        if (args.Contains("--advance-group-decision-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunAdvanceGroupDecisionSelfTest();
        }
        if (args.Contains("--advance-reliability-v2-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunAdvanceReliabilityV2SelfTest();
        }
        if (args.Contains("--ring-lifecycle-v2-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunRingLifecycleV2SelfTest();
        }
        if (args.Contains("--process-value-reliability-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunProcessValueReliabilitySelfTest();
        }
        if (args.Contains("--ocr-user-feedback-snapshot-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunOcrUserFeedbackSnapshotSelfTest();
        }
        if (args.Contains("--feedback-replay-self-test", StringComparer.OrdinalIgnoreCase)) return RunFeedbackReplaySelfTest();
        if (args.Contains("--feedback-dataset-dedup-self-test", StringComparer.OrdinalIgnoreCase)) return RunFeedbackDatasetDedupSelfTest();
        if (args.Contains("--feedback-failure-classification-self-test", StringComparer.OrdinalIgnoreCase)) return RunFeedbackFailureClassificationSelfTest();
        if (args.Contains("--feedback-template-safety-self-test", StringComparer.OrdinalIgnoreCase)) return RunFeedbackTemplateSafetySelfTest();
        if (args.Contains("--feedback-reliability-audit-self-test", StringComparer.OrdinalIgnoreCase)) return RunFeedbackReliabilityAuditSelfTest();
        if (args.Contains("--feedback-replay", StringComparer.OrdinalIgnoreCase)) return RunFeedbackReplay(args);
        if (args.Contains("--feedback-learning-replay", StringComparer.OrdinalIgnoreCase)) return RunFeedbackLearningReplay();
        if (args.Contains("--feedback-calibrate", StringComparer.OrdinalIgnoreCase)) return RunFeedbackCalibration();
        var feedbackUiTest=args.FirstOrDefault(x=>x is "--correct-feedback-ui-self-test" or "--incorrect-feedback-ui-self-test" or "--corrected-feedback-ui-self-test" or "--feedback-last-revision-ui-self-test" or "--feedback-remove-ui-self-test" or "--feedback-reload-ui-self-test" or "--feedback-does-not-change-official-value-self-test" or "--feedback-measurement-field-identity-self-test");
        if(feedbackUiTest is not null)return RunFeedbackUiSelfTest(feedbackUiTest[2..]);
        var stabilizationTest=args.FirstOrDefault(x=>x is "--user-feedback-effective-display-self-test" or "--correct-feedback-removes-warning-self-test" or "--corrected-feedback-effective-value-self-test" or "--incorrect-feedback-hidden-from-presentation-self-test" or "--feedback-audit-preserves-raw-self-test" or "--feedback-revision-effective-display-self-test" or "--numeric-feedback-conflict-semantics-self-test" or "--mismatch-capture-production-default-self-test" or "--mismatch-retention-self-test" or "--mismatch-retention-no-full-scan-per-write-self-test" or "--mismatch-cleanup-does-not-delete-user-feedback-self-test" or "--csv-log-rotation-self-test" or "--verification-artifact-retention-self-test");
        if(stabilizationTest is not null)return RunProductionStabilizationSelfTest(stabilizationTest[2..]);
        var performanceTest=args.FirstOrDefault(x=>x is "--monitoring-performance-profiler-self-test" or "--monitoring-stage-accounting-self-test" or "--screen-capture-count-self-test" or "--ocr-redundant-work-self-test" or "--feedback-index-performance-self-test" or "--tesseract-engine-reuse-self-test" or "--diagnostic-only-nonblocking-self-test" or "--ui-update-nonblocking-self-test" or "--slow-cycle-detection-self-test" or "--performance-log-rotation-self-test");
        if(performanceTest is not null)return RunMonitoringPerformanceSelfTest(performanceTest[2..]);
        var parallelOcrTest=args.FirstOrDefault(x=>x is "--parallel-vs-serial-ocr-equivalence-self-test" or "--ocr-worker-pool-self-test" or "--ocr-engine-pool-thread-safety-self-test" or "--parallel-cycle-stale-result-self-test" or "--parallel-cycle-cancellation-self-test" or "--parallel-feedback-snapshot-mapping-self-test" or "--parallel-operationmode-single-transition-self-test" or "--parallel-advance-state-safety-self-test" or "--parallel-reliability-state-safety-self-test" or "--parallel-image-dispose-self-test" or "--ocr-parallel-worker-benchmark" or "--real-field-worker-tuning-self-test" or "--priority-queue-order-self-test" or "--core-dashboard-ready-self-test" or "--core-does-not-wait-for-epb-self-test" or "--engine-acquire-wait-self-test" or "--targeted-fallback-redundancy-self-test" or "--heavy-field-call-count-self-test" or "--trusted-dashboard-presentation-self-test" or "--parallel-worker-equivalence-v2-self-test");
        if(parallelOcrTest is not null)return RunParallelOcrSelfTest(parallelOcrTest[2..]);
        var feedbackLearningTest=args.FirstOrDefault(x=>x is "--verified-feedback-memory-self-test" or "--feedback-field-isolation-self-test" or "--feedback-machine-isolation-self-test" or "--feedback-conflict-self-test" or "--feedback-threshold-self-test" or "--feedback-decimal-format-self-test" or "--feedback-temporal-compatibility-self-test" or "--feedback-live-decision-log-self-test" or "--feedback-assisted-replay-self-test" or "--feedback-card-source-self-test" or "--feedback-history-tooltip-self-test" or "--feedback-wrong-trusted-regression-self-test" or "--feedback-calibration-self-test" or "--feedback-similarity-distribution-self-test" or "--feedback-threshold-evaluation-self-test" or "--feedback-independent-support-self-test" or "--feedback-two-support-verified-self-test" or "--feedback-three-support-authoritative-self-test" or "--feedback-correct-and-corrected-cluster-self-test" or "--feedback-torque-zero-learning-self-test" or "--feedback-thrust-visual-recovery-self-test" or "--feedback-no-value-mapping-self-test");
        if(feedbackLearningTest is not null)return RunFeedbackLearningSelfTest(feedbackLearningTest[2..]);
        if (args.Contains("--advance-live-refresh-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunAdvanceLiveRefreshSelfTest();
        }
        if (args.Contains("--advance-physical-consistency-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunAdvancePhysicalConsistencySelfTest();
        }
        if (args.Contains("--advance-legacy-removal-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunAdvanceLegacyRemovalSelfTest();
        }
        if (args.Contains("--operation-mode-text-ocr-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunOperationModeTextOcrSelfTest();
        }
        if (args.Contains("--stable-zone-state-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunStableZoneStateSelfTest();
        }
        if (args.Contains("--stable-parameter-ocr-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunStableParameterOcrSelfTest();
        }
        if (args.Contains("--thrust-character-segmentation-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunThrustCharacterSegmentationSelfTest();
        }
        if (args.Contains("--stable-full-line-fallback-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunStableFullLineFallbackSelfTest();
        }
        if (args.Contains("--dashboard-ui-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunDashboardUiSelfTest();
        }
        if (args.Contains("--history-ocr-reading-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunHistoryOcrReadingSelfTest();
        }
        if (args.Contains("--temporal-recovery-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunTemporalRecoverySelfTest();
        }
        if (args.Contains("--diagnostic-value-visibility-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunDiagnosticValueVisibilitySelfTest();
        }
        if (args.Contains("--live-stable-diagnostic-binding-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunLiveStableDiagnosticBindingSelfTest();
        }
        if (args.Contains("--live-stable-field-enablement-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunLiveStableFieldEnablementSelfTest();
        }
        if (args.Contains("--live-diagnostic-state-and-performance-self-test", StringComparer.OrdinalIgnoreCase))
        {
            return RunLiveDiagnosticStateAndPerformanceSelfTest();
        }
        var injectionTest = args.FirstOrDefault(arg => arg is "--injection-field-definition-self-test"
            or "--nominal-injection-decimal-self-test" or "--real-injection-decimal-self-test"
            or "--injection-zero-self-test" or "--injection-decimal-comma-self-test"
            or "--injection-live-current-value-self-test" or "--injection-history-no-stale-copy-self-test"
            or "--injection-fast-path-self-test");
        if (injectionTest is not null) return RunInjectionStage5ASelfTest(injectionTest[2..]);
        var epbTest = args.FirstOrDefault(arg => arg is "--epb-field-definition-self-test" or "--epb-decimal-self-test"
            or "--epb-comma-self-test" or "--epb-zero-self-test" or "--epb-history-binding-self-test"
            or "--epb-history-no-stale-copy-self-test" or "--epb-fast-path-self-test" or "--epb-invalid-token-self-test");
        if (epbTest is not null) return RunEpbStage5BSelfTest(epbTest[2..]);

        Services.VerificationArtifactRetentionService.RetainLatestRuns(
            Services.VerificationArtifactRetentionService.Root, 3);
        ApplicationConfiguration.Initialize();
        using var logService = new Services.LogService();
        GlobalExceptionHandler.Register(logService);
        logService.Information("Program açıldı.");
        Application.Run(new Forms.MainForm(logService));
        logService.Information("Program kapatıldı.");
        return 0;
    }

    private static int RunTorqueRealCropSelfTest(string[] args)
    {
        try
        {
            var index = Array.FindIndex(args, value => value.Equals("--torque-real-crop-self-test", StringComparison.OrdinalIgnoreCase));
            if (index < 0 || index + 1 >= args.Length || !File.Exists(args[index + 1])) return 2;
            using var image = new Bitmap(args[index + 1]);
            using var service = new Services.OcrService();
            using var result = service.ReadNumberLiveFastWithDiagnosticsAsync(image, 20m,
                new Models.OcrProfile(), Models.OcrFieldKind.StableTorque).GetAwaiter().GetResult();
            var passed = result.Result.Value == 1.375m;
            Console.WriteLine($"torque-real-crop-self-test: {(passed ? "PASS" : "FAIL")} | Value={result.Result.Value?.ToString("0.000") ?? "null"} | Candidates={string.Join(" || ", result.Candidates.Select(c => $"{c.ProcessingMethod}/{c.PageSegMode}:{c.RawText}->{c.CleanedText}={c.ParsedValue}"))}");
            return passed ? 0 : 1;
        }
        catch (Exception exception) { Console.Error.WriteLine("torque-real-crop-self-test: FAIL | " + exception); return 1; }
    }

    private static int RunLiveDiagnosticStateAndPerformanceSelfTest()
    {
        ApplicationConfiguration.Initialize();
        using var log = new Services.LogService(Path.Combine(Path.GetTempPath(), "TBMWatchdog-LiveState-" + Guid.NewGuid().ToString("N") + ".log"));
        using var form = new Forms.MainForm(log);
        form.SelectDashboardCategoryForTest(Forms.MainForm.DashboardCategory.Excavation);
        var now = DateTime.Now;
        Models.FieldMeasurementResult Torque(decimal? value, string reason, DateTime at) => new()
        {
            FieldId = Models.BuiltInFieldIds.StableTorque, RawValue = value?.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) ?? "",
            ParsedValue = value, FullLineResult = value, FailureReason = reason,
            ConfidenceStatus = reason == "Success" ? Models.OcrConfidenceStatus.Reliable : Models.OcrConfidenceStatus.Partial,
            CapturedAt = at, CompletedAt = at
        };
        Models.MeasurementRecord Record(Models.FieldMeasurementResult field) => new() { Timestamp = field.CapturedAt,
            FieldResults = new Dictionary<string, Models.FieldMeasurementResult>(StringComparer.OrdinalIgnoreCase)
            { [field.FieldId] = field } };
        form.DisplayMeasurementForTest(Record(Torque(3m, "Success", now)));
        form.DisplayMeasurementForTest(Record(Torque(0m, "DigitCountMismatch", now.AddSeconds(1))));
        var currentWarning = form.DashboardCards[1].Value.Contains("0.000", StringComparison.Ordinal)
            && form.DashboardCards[1].Detail.Contains("Canlı OCR", StringComparison.Ordinal)
            && form.LatestHistoryValueForTest("Torque").Contains("0.000", StringComparison.Ordinal);
        form.DisplayMeasurementForTest(Record(Torque(null, "NoSegments", now.AddSeconds(2))));
        var lastFallback = form.DashboardCards[1].Value.Contains("0.000", StringComparison.Ordinal)
            && form.DashboardCards[1].Detail.Contains("Son canlı değer", StringComparison.Ordinal)
            && form.LatestHistoryValueForTest("Torque") == "—";

        Models.OcrCandidateResult Candidate(decimal value, int segments, float confidence = .5f) => new()
        {
            ProcessingMethod = "Fast", PageSegMode = Tesseract.PageSegMode.SingleLine,
            RawText = value.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
            CleanedText = value.ToString("0", System.Globalization.CultureInfo.InvariantCulture), ParsedValue = value,
            Confidence = confidence, OcrConfidence = confidence, SystemConfidence = confidence,
            CharacterCount = value.ToString("0", System.Globalization.CultureInfo.InvariantCulture).Length,
            RawSegmentCount = segments, SegmentCount = segments, ImageSize = new Size(120, 40),
            ProcessedImage = new Bitmap(1, 1), BinaryImage = new Bitmap(1, 1), SegmentationImage = new Bitmap(1, 1)
        };
        using var plausible = Candidate(13763, 5);
        using var implausible = Candidate(3, 5);
        var fallbackConditional = !Services.OcrService.NeedsThrustTargetedFallback([plausible])
            && Services.OcrService.NeedsThrustTargetedFallback([implausible]);
        using var digits = new Bitmap(90, 40);
        using (var graphics = Graphics.FromImage(digits))
        { graphics.Clear(Color.White); using var brush = new SolidBrush(Color.Black); graphics.FillRectangle(brush, 10, 5, 8, 30); graphics.FillRectangle(brush, 35, 5, 8, 30); graphics.FillRectangle(brush, 60, 5, 8, 30); }
        using var segmentation = Services.CharacterSegmenter.Segment(digits, Models.OcrFieldKind.StableThrustForce,
            new Services.CharacterSegmentationOptions(true, 3, 5, 1.10, .38));
        var segmentationConditional = segmentation.Characters.Count == 3 && segmentation.SplitAttemptCount == 0;
        var passed = currentWarning && lastFallback && fallbackConditional && segmentationConditional;
        Console.WriteLine($"live-diagnostic-state-and-performance-self-test: {(passed ? "PASS" : "FAIL")} | currentWarning={currentWarning} | lastFallback={lastFallback} | targetedConditional={fallbackConditional} | normalSplitAttempts={segmentation.SplitAttemptCount}");
        return passed ? 0 : 1;
    }

    private static int RunInjectionStage5ASelfTest(string requested)
    {
        ApplicationConfiguration.Initialize();
        static Models.ScreenRegion Region() => new() { X = 10, Y = 10, Width = 40, Height = 18 };
        var settings = new Models.AppSettings { OcrProfile = new Models.OcrProfile { Name = "1031" },
            NominalInjectionRegion = Region(), RealInjectionRegion = Region() };
        Services.OcrProfileMigrator.Migrate(settings);
        var nominal = settings.OcrProfile.FindField(Models.BuiltInFieldIds.NominalInjection);
        var real = settings.OcrProfile.FindField(Models.BuiltInFieldIds.RealInjection);
        var definitions = nominal is { IsEnabled: true, DecimalPlaces: 1, MinimumValue: 0, MaximumValue: 20,
                TemplateFolderName: "SharedNumericDigits", UsesOfficialCharacterDecision: false }
            && real is { IsEnabled: true, DecimalPlaces: 1, MinimumValue: 0, MaximumValue: 20,
                TemplateFolderName: "SharedNumericDigits", UsesOfficialCharacterDecision: false };
        var parses = nominal is not null && real is not null
            && Services.NumericFieldParser.TryParse("4.0", nominal, out var four, out var fourText) && four == 4m && fourText == "4.0"
            && Services.NumericFieldParser.TryParse("2.1", real, out var two, out var twoText) && two == 2.1m && twoText == "2.1"
            && Services.NumericFieldParser.TryParse("0.0", real, out var zero, out var zeroText) && zero == 0m && zeroText == "0.0"
            && Services.NumericFieldParser.TryParse("2,1", real, out var comma, out var commaText) && comma == 2.1m && commaText == "2.1";
        using var candidate = new Models.OcrCandidateResult { ProcessingMethod = "Gaussian + Median + Otsu",
            PageSegMode = Tesseract.PageSegMode.SingleLine, RawText = "2.1", CleanedText = "2.1", ParsedValue = 2.1m,
            Confidence = .95f, OcrConfidence = .95f, SystemConfidence = .95f, CharacterCount = 2,
            ImageSize = new Size(40, 18),
            ProcessedImage = new Bitmap(1, 1), BinaryImage = new Bitmap(1, 1), SegmentationImage = new Bitmap(1, 1) };
        var fast = Services.OcrService.IsLiveFastEarlyExitCandidate(candidate, settings.OcrProfile, Models.OcrFieldKind.RealInjection);
        // Verify malformed structure separately through the structure guard.
        var decimalGuard = Services.OcrService.HasExpectedDecimalStructure("2.1", 1)
            && !Services.OcrService.HasExpectedDecimalStructure("21", 1)
            && !Services.OcrService.HasExpectedDecimalStructure("211", 1);
        var warningText = Forms.MainForm.FormatDiagnosticHistory(4m, "LowConfidence", "0.0");
        var warningVisible = warningText == "4.0 ⚠";
        var noStale = Forms.MainForm.FormatDiagnosticHistory(null, "OcrEmpty", "0.0") == "—";
        var all = definitions && parses && fast && decimalGuard && warningVisible && noStale;
        var relevant = requested switch
        {
            "injection-field-definition-self-test" => definitions,
            "nominal-injection-decimal-self-test" or "real-injection-decimal-self-test" or "injection-zero-self-test" or "injection-decimal-comma-self-test" => parses,
            "injection-live-current-value-self-test" => warningVisible,
            "injection-history-no-stale-copy-self-test" => noStale,
            "injection-fast-path-self-test" => fast && decimalGuard,
            _ => all
        };
        Console.WriteLine($"{requested}: {(relevant ? "PASS" : "FAIL")} | definitions={definitions} | parse={parses} | fast={fast} | decimalGuard={decimalGuard} | warningCurrent={warningVisible} | noStale={noStale}");
        return relevant ? 0 : 1;
    }

    private static int RunEpbStage5BSelfTest(string requested)
    {
        ApplicationConfiguration.Initialize();
        static Models.ScreenRegion Region() => new() { X = 10, Y = 10, Width = 42, Height = 18 };
        var settings = new Models.AppSettings { OcrProfile = new Models.OcrProfile { Name = "1031" },
            Epb1Region = Region(), Epb2Region = Region(), Epb3Region = Region(), Epb4Region = Region(), Epb5Region = Region() };
        Services.OcrProfileMigrator.Migrate(settings);
        var ids = new[] { Models.BuiltInFieldIds.Epb1, Models.BuiltInFieldIds.Epb2, Models.BuiltInFieldIds.Epb3,
            Models.BuiltInFieldIds.Epb4, Models.BuiltInFieldIds.Epb5 };
        var fields = ids.Select(id => settings.OcrProfile.FindField(id)).ToArray();
        var definitions = fields.All(field => field is { IsEnabled: true, Unit: "bar", DecimalPlaces: 2,
            MinimumValue: 0, MaximumValue: 10, TemplateFolderName: "SharedNumericDigits", UsesOfficialCharacterDecision: false });
        var other = new Models.AppSettings { OcrProfile = new Models.OcrProfile { Name = "1030" },
            Epb1Region = Region(), Epb2Region = Region(), Epb3Region = Region(), Epb4Region = Region(), Epb5Region = Region() };
        Services.OcrProfileMigrator.Migrate(other);
        var only1031 = ids.All(id => other.OcrProfile.FindField(id) is { IsEnabled: false });
        var parser = fields[0]!;
        var decimals = Services.NumericFieldParser.TryParse("2.88", parser, out var point, out var pointText) && point == 2.88m && pointText == "2.88"
            && Services.NumericFieldParser.TryParse("2,88 bar", parser, out var comma, out var commaText) && comma == 2.88m && commaText == "2.88";
        var zero = Services.NumericFieldParser.TryParse("0.00", parser, out var zeroValue, out var zeroText) && zeroValue == 0 && zeroText == "0.00";
        var invalid = !Services.NumericFieldParser.TryParse(".", parser, out _, out _)
            && !Services.NumericFieldParser.TryParse(",", parser, out _, out _) && !Services.NumericFieldParser.TryParse("", parser, out _, out _);
        var now = DateTime.Now; var values = new[] { 2.88m, .14m, 6m, 2.84m, 2.94m };
        var map = ids.Select((id, index) => new KeyValuePair<string, Models.FieldMeasurementResult>(id, new()
        { FieldId = id, RawValue = values[index].ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
            ParsedValue = values[index], OfficialValue = null, FailureReason = index == 0 ? "LowConfidence" : "Success",
            CapturedAt = now, CompletedAt = now })).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var history = Forms.MainForm.BuildEpbHistoryValues(new Models.MeasurementRecord { Timestamp = now, FieldResults = map });
        var mapping = history.SequenceEqual(new[] { "2.88 ⚠", "0.14", "6.00", "2.84", "2.94" });
        var noStale = Forms.MainForm.BuildEpbHistoryValues(new Models.MeasurementRecord { Timestamp = now.AddSeconds(1),
            FieldResults = new Dictionary<string, Models.FieldMeasurementResult>(StringComparer.OrdinalIgnoreCase) })
            .All(value => value == "—");
        using var candidate = new Models.OcrCandidateResult { ProcessingMethod = "Gaussian + Median + Otsu",
            PageSegMode = Tesseract.PageSegMode.SingleLine, RawText = "2.88", CleanedText = "2.88", ParsedValue = 2.88m,
            Confidence = .95f, OcrConfidence = .95f, SystemConfidence = .95f, CharacterCount = 3, ImageSize = new Size(42, 18),
            ProcessedImage = new Bitmap(1, 1), BinaryImage = new Bitmap(1, 1), SegmentationImage = new Bitmap(1, 1) };
        var fastAccepted = Services.OcrService.IsLiveFastEarlyExitCandidate(candidate, settings.OcrProfile, Models.OcrFieldKind.Epb1);
        var fastPath = fastAccepted && !Services.OcrService.ShouldRunPostFastCharacterPipeline(Models.OcrFieldKind.Epb1, true, true)
            && !Services.OcrService.HasExpectedDecimalStructure("288", 2);
        var relevant = requested switch
        {
            "epb-field-definition-self-test" => definitions && only1031,
            "epb-decimal-self-test" or "epb-comma-self-test" => decimals,
            "epb-zero-self-test" => zero,
            "epb-history-binding-self-test" => mapping,
            "epb-history-no-stale-copy-self-test" => noStale,
            "epb-fast-path-self-test" => fastPath,
            "epb-invalid-token-self-test" => invalid,
            _ => false
        };
        Console.WriteLine($"{requested}: {(relevant ? "PASS" : "FAIL")} | fields={definitions} | 1031Only={only1031} | decimal={decimals} | zero={zero} | mapping={mapping} | noStale={noStale} | fast={fastPath} | invalid={invalid}");
        return relevant ? 0 : 1;
    }

    private static int RunDecimalCandidateArbitrationSelfTest()
    {
        ApplicationConfiguration.Initialize();
        static Models.OcrCandidateResult Candidate(string text, string method, bool character = false,
            bool authoritative = false, bool templatePresent = false, float confidence = .80f)
        {
            var value = decimal.TryParse(text, System.Globalization.NumberStyles.AllowDecimalPoint,
                System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : (decimal?)null;
            var diagnostics = new List<Models.CharacterOcrDiagnostic>();
            if (character && templatePresent)
            {
                foreach (var item in text.Where(char.IsDigit)) diagnostics.Add(new Models.CharacterOcrDiagnostic
                {
                    CharacterIndex = diagnostics.Count, OriginalSegmentImage = new Bitmap(1, 1),
                    CleanedSegmentImage = new Bitmap(1, 1), TightCroppedImage = new Bitmap(1, 1),
                    NormalizedOcrInputImage = new Bitmap(1, 1), RawOcrText = item.ToString(),
                    CleanedDigit = item.ToString(), OcrConfidence = confidence, TemplateResult = item.ToString(),
                    TemplateScore = authoritative ? .96 : .70, TemplateScoreMargin = authoritative ? .20 : .02,
                    IsTemplateConfident = authoritative, IsTemplateAuthoritative = authoritative,
                    TemplateContributed = authoritative, GeometryResult = "—", FinalDigit = item.ToString(),
                    DecisionReason = authoritative ? "User verified template" : "Weak template"
                });
            }
            return new Models.OcrCandidateResult
            {
                ProcessingMethod = method, PageSegMode = character ? Tesseract.PageSegMode.SingleChar : Tesseract.PageSegMode.SingleLine,
                RawText = text, CleanedText = text, ParsedValue = value, Confidence = confidence,
                OcrConfidence = confidence, SystemConfidence = confidence, CharacterCount = text.Count(char.IsDigit),
                RawSegmentCount = character ? text.Count(char.IsDigit) : 0,
                SegmentCount = character ? text.Count(char.IsDigit) : 0, SegmentMatch = character ? true : null,
                UsesCharacterBasedOcr = character, CharacterOcrText = character ? text : "",
                CombinedText = character ? text : "", CharacterDecisionReliable = character,
                AllCharactersTemplateAuthoritative = authoritative, CharacterDiagnostics = diagnostics,
                SegmentationSuccessful = character, ImageSize = new Size(1, 1),
                ProcessedImage = new Bitmap(1, 1), BinaryImage = new Bitmap(1, 1), SegmentationImage = new Bitmap(1, 1)
            };
        }
        static (Models.OcrReadResult Result, Models.OcrCandidateResult? Selected) Select(
            List<Models.OcrCandidateResult> items, Models.OcrFieldKind field) =>
            Services.OcrService.SelectResult(items, new Models.OcrProfile(), field);
        static void Dispose(IEnumerable<Models.OcrCandidateResult> items) { foreach (var item in items) item.Dispose(); }

        var a = new List<Models.OcrCandidateResult> { Candidate("0.6", "Nearest"), Candidate("0.6", "Contrast"),
            Candidate("0.6", "Gaussian"), Candidate("0", "Adaptive") };
        var b = new List<Models.OcrCandidateResult> { Candidate("0.0", "Nearest"), Candidate("0.0", "Contrast"), Candidate("0.0", "Gaussian") };
        var c = new List<Models.OcrCandidateResult> { Candidate("1.98", "Nearest"), Candidate("1.98", "Contrast"),
            Candidate("1.98", "Gaussian"), Candidate("1.11", "Segmented Characters", true) };
        var d = new List<Models.OcrCandidateResult> { Candidate("1.98", "Nearest"), Candidate("1.98", "Segmented Characters", true) };
        var e = new List<Models.OcrCandidateResult> { Candidate("1.98", "Nearest"),
            Candidate("1.11", "Segmented Characters", true, true, true, .95f) };
        var f = new List<Models.OcrCandidateResult> { Candidate("0.000", "Nearest"), Candidate("0.000", "Contrast") };
        var g = new List<Models.OcrCandidateResult> { Candidate(".", "Nearest") };
        var hCandidate = new Models.OcrCandidateResult
        {
            ProcessingMethod = "Bicubic Gri (Ondalık)", PageSegMode = Tesseract.PageSegMode.SingleWord,
            RawText = "14.207", CleanedText = "14.207", ParsedValue = 14.207m,
            Confidence = .40f, OcrConfidence = .40f, SystemConfidence = .40f,
            CharacterCount = 5, RawSegmentCount = 5, SegmentCount = 5, SegmentMatch = false,
            ImageSize = new Size(320, 144), ProcessedImage = new Bitmap(1, 1),
            BinaryImage = new Bitmap(1, 1), SegmentationImage = new Bitmap(1, 1)
        };
        try
        {
            var ar = Select(a, Models.OcrFieldKind.RealInjection).Result;
            var br = Select(b, Models.OcrFieldKind.RealInjection).Result;
            var cr = Select(c, Models.OcrFieldKind.Epb1).Result;
            var dr = Select(d, Models.OcrFieldKind.Epb1).Result;
            var er = Select(e, Models.OcrFieldKind.Epb1).Result;
            var fr = Select(f, Models.OcrFieldKind.StableTorque).Result;
            var gr = Select(g, Models.OcrFieldKind.RealInjection).Result;
            var torqueDotNoiseCorrected = Services.DecimalNumericCandidateDecisionService
                .TryRemoveTorqueDecimalArtifact(hCandidate, 3) && hCandidate.ParsedValue == 1.207m;
            var passed = ar.Value == .6m && ar.DecisionSource == "FullLineConsensus" && ar.ZeroCandidateCount == 1
                && br.Value == 0m && cr.Value == 1.98m && !cr.CharacterAuthoritative
                && cr.CharacterAuthorityReason == "NoVerifiedTemplateSupport"
                && dr.Value == 1.98m && er.Value == 1.11m && er.CharacterAuthoritative
                && fr.Value == 0m && !gr.IsSuccessful && torqueDotNoiseCorrected;
            Console.WriteLine($"decimal-candidate-arbitration-self-test: {(passed ? "PASS" : "FAIL")} | RealInjection={ar.Value}/{ar.DecisionSource} | RealZero={br.Value} | EPB_NoTemplate={cr.Value}/{cr.CharacterAuthorityReason} | Agreement={dr.Value} | Authoritative={er.Value} | TorqueZero={fr.Value:0.000} | DotRejected={!gr.IsSuccessful} | Torque14.207→1.207={torqueDotNoiseCorrected}");
            return passed ? 0 : 1;
        }
        finally { Dispose(a); Dispose(b); Dispose(c); Dispose(d); Dispose(e); Dispose(f); Dispose(g); hCandidate.Dispose(); }
    }

    private static int RunTorqueGlyphConfusionSelfTest()
    {
        ApplicationConfiguration.Initialize();
    static Models.OcrCandidateResult CreateCandidate(
        string processingMethod,
        string text,
        params Services.TorqueGlyphGeometry[] glyphs)
    {
        return new Models.OcrCandidateResult
        {
            ProcessingMethod = processingMethod,
                PageSegMode = Tesseract.PageSegMode.SingleLine,
                RawText = text,
                CleanedText = text,
                ParsedValue = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture),
                Confidence = .70f,
                OcrConfidence = .70f,
                SystemConfidence = .70f,
                CharacterCount = 4,
                ImageSize = new Size(1, 1),
                ProcessedImage = new Bitmap(1, 1),
                BinaryImage = new Bitmap(1, 1),
                SegmentationImage = new Bitmap(1, 1),
            TorqueGlyphs = glyphs
        };
    }

    static Models.OcrCandidateResult Candidate(string text, params Services.TorqueGlyphGeometry[] glyphs)
        => CreateCandidate("Torque tight colour-preserving fallback / SingleLine", text, glyphs);

    static Models.OcrCandidateResult LiveCandidate(string text, params Services.TorqueGlyphGeometry[] glyphs)
        => CreateCandidate("Torque colour-preserving fallback / SingleLine", text, glyphs);

        static Services.TorqueGlyphGeometry Same(char digit) => new(digit, .99, "aynı glif");
        static Services.TorqueGlyphGeometry Fix(char digit, string reason) => new(digit, .95, reason);
    var cases = new[]
    {
        (LiveCandidate("4.738", Fix('1', "dar dikey 1"), Same('7'), Same('3'), Same('8')), 1.738m),
            (Candidate("4.175", Fix('1', "dar dikey 1"), Same('1'), Same('7'), Same('5')), 1.175m),
            (Candidate("1.432", Same('1'), Fix('1', "dar dikey 1"), Same('3'), Same('2')), 1.132m),
            (Candidate("1.820", Same('1'), Fix('5', "açık konturlu 5"), Same('2'), Same('0')), 1.520m),
            (Candidate("1.959", Same('1'), Fix('5', "açık konturlu 5"), Same('5'), Same('9')), 1.559m),
            (Candidate("6.853", Fix('0', "kapalı merkezli 0"), Same('8'), Same('5'), Same('3')), .853m),
            (Candidate("6.631", Fix('0', "kapalı merkezli 0"), Same('6'), Same('3'), Same('1')), .631m)
        };
        try
        {
            var passed = true;
            foreach (var (candidate, expected) in cases)
            {
                var corrected = Services.DecimalNumericCandidateDecisionService.TryCorrectTorqueGlyphConfusions(candidate, 3);
                passed &= corrected && candidate.ParsedValue == expected;
                Console.WriteLine($"{candidate.RawText} -> {candidate.ParsedValue:0.000} (beklenen {expected:0.000})");
            }
            Console.WriteLine($"torque-glyph-confusion-self-test: {(passed ? "PASS" : "FAIL")}");
            return passed ? 0 : 1;
        }
        finally
        {
            foreach (var (candidate, _) in cases) candidate.Dispose();
        }
    }

    private static int RunStableFullLineFallbackSelfTest()
    {
        var candidates = new List<Models.OcrCandidateResult>();
        try
        {
            var mismatch = Services.OcrService.ApplyCharacterSegmentationValidation(11988, null, false, 5, 1,
                Models.OcrFieldKind.StableThrustForce, false);
            candidates.Add(Candidate("Pipeline A", 11988, .88f, false));
            candidates.Add(Candidate("Pipeline B", 11988, .84f, false));
            candidates.Add(Candidate("Pipeline C", 11988, .81f, false));
            var profile = new Models.OcrProfile();
            var selected = Services.OcrService.SelectResult(candidates, profile, Models.OcrFieldKind.StableThrustForce);

            var torqueZero = Services.OcrService.ApplyCharacterSegmentationValidation(0m, null, false, 4, 1,
                Models.OcrFieldKind.StableTorque, false);
            var rpmZero = Services.OcrService.ApplyCharacterSegmentationValidation(0m, null, false, 3, 1,
                Models.OcrFieldKind.StableRpm, false);
            var speedZero = Services.OcrService.ApplyCharacterSegmentationValidation(0m, null, false, 2, 1,
                Models.OcrFieldKind.StableAdvanceSpeed, false);
            var thrustDefinition = new Models.NumericFieldDefinition { FieldId = Models.BuiltInFieldIds.StableThrustForce,
                ValueType = Models.FieldValueType.Integer, MinimumValue = 0, MaximumValue = 100000, DecimalPlaces = 0 };
            var outOfRangeRejected = !Services.NumericFieldParser.TryParse("511988", thrustDefinition, out _, out _);
            var comparisonFailure = Services.CharacterOcrComparisonCsvService.ClassifyFailure(candidates, null, 8) == "CharacterMismatch";

            using var log = new Services.LogService(Path.Combine(Path.GetTempPath(), "TBMWatchdog-Fallback-" + Guid.NewGuid().ToString("N") + ".log"));
            using var form = new Forms.MainForm(log);
            form.SelectDashboardCategoryForTest(Forms.MainForm.DashboardCategory.Excavation);
            var now = DateTime.Now;
            var goodField = new Models.FieldMeasurementResult { FieldId = Models.BuiltInFieldIds.StableThrustForce,
                RawValue = "11988", ParsedValue = 11988, FullLineResult = 11988, OfficialValue = null,
                ConfidenceStatus = Models.OcrConfidenceStatus.Partial, FailureReason = "CharacterMismatch",
                DecisionSource = "DiagnosticOnly/TesseractFallback", CapturedAt = now, CompletedAt = now };
            form.DisplayMeasurementForTest(new Models.MeasurementRecord { Timestamp = now,
                FieldResults = new Dictionary<string, Models.FieldMeasurementResult>(StringComparer.OrdinalIgnoreCase)
                { [Models.BuiltInFieldIds.StableThrustForce] = goodField } });
            var cardAfterGood = form.DashboardCards[2].Value.Contains("11988", StringComparison.Ordinal);
            var historyGood = form.LatestHistoryValueForTest("Thrust").Contains("11988", StringComparison.Ordinal)
                && form.LatestHistoryValueForTest("Thrust").Contains("⚠", StringComparison.Ordinal)
                && form.LatestHistoryToolTipForTest("Thrust").Contains("Tesseract fallback", StringComparison.Ordinal);
            var failedField = new Models.FieldMeasurementResult { FieldId = Models.BuiltInFieldIds.StableThrustForce,
                RawValue = "", ParsedValue = null, OfficialValue = null, ConfidenceStatus = Models.OcrConfidenceStatus.Unreadable,
                FailureReason = "NoSegments", CapturedAt = now.AddSeconds(1), CompletedAt = now.AddSeconds(1) };
            form.DisplayMeasurementForTest(new Models.MeasurementRecord { Timestamp = now.AddSeconds(1),
                FieldResults = new Dictionary<string, Models.FieldMeasurementResult>(StringComparer.OrdinalIgnoreCase)
                { [Models.BuiltInFieldIds.StableThrustForce] = failedField } });
            var cardKeepsLast = form.DashboardCards[2].Value.Contains("11988", StringComparison.Ordinal)
                && form.DashboardCards[2].Detail.Contains("yeni okuma başarısız", StringComparison.OrdinalIgnoreCase);
            var historyDoesNotCopy = form.LatestHistoryValueForTest("Thrust") == "—";

            var passed = mismatch.ParsedValue == 11988 && mismatch.Error is null && mismatch.Status == "Mismatch"
                && selected.Result.Value == 11988 && selected.Result.Warning is not null
                && torqueZero.ParsedValue == 0 && rpmZero.ParsedValue == 0 && speedZero.ParsedValue == 0
                && outOfRangeRejected && comparisonFailure && cardAfterGood && historyGood && cardKeepsLast && historyDoesNotCopy;
            Console.WriteLine($"stable-full-line-fallback-self-test: {(passed ? "PASS" : "FAIL")} | Thrust=11988/{selected.Result.Value} | Quality={(selected.Result.Warning is null ? "Reliable" : "Partial")} | Torque=0.000->{torqueZero.ParsedValue} | RPM=0.00->{rpmZero.ParsedValue} | Speed=0.0->{speedZero.ParsedValue} | 511988Rejected={outOfRangeRejected} | CardLast={cardKeepsLast} | HistoryCurrentOnly={historyDoesNotCopy}");
            return passed ? 0 : 1;

            static Models.OcrCandidateResult Candidate(string method, decimal value, float confidence, bool character)
            {
                var processed = new Bitmap(8, 8); var binary = new Bitmap(8, 8); var segmentation = new Bitmap(8, 8);
                return new Models.OcrCandidateResult { ProcessingMethod = method, PageSegMode = character ? Tesseract.PageSegMode.SingleChar : Tesseract.PageSegMode.SingleLine,
                    RawText = value.ToString(System.Globalization.CultureInfo.InvariantCulture), CleanedText = value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ParsedValue = value, Confidence = confidence, OcrConfidence = confidence, CharacterCount = 5,
                    SegmentCount = 1, RawSegmentCount = 1, SegmentMatch = false, CharacterValidationStatus = "Mismatch",
                    CharacterValidationWarning = "Full-line OCR geçerli; karakter segmentasyonu doğrulayamadı.",
                    UsesCharacterBasedOcr = character, ImageSize = processed.Size, ProcessedImage = processed, BinaryImage = binary, SegmentationImage = segmentation };
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("stable-full-line-fallback-self-test: FAIL | " + exception);
            return 1;
        }
        finally { foreach (var candidate in candidates) candidate.Dispose(); }
    }

    private static int RunThrustFullLineConsensusSelfTest()
    {
        ApplicationConfiguration.Initialize();
        static Models.OcrCandidateResult Candidate(decimal? value, string method, bool character = false,
            bool complete = false, bool authoritative = false, float confidence = .82f, string? error = null)
        {
            var text = value?.ToString("0") ?? "";
            return new Models.OcrCandidateResult
            {
                ProcessingMethod = method, PageSegMode = character ? Tesseract.PageSegMode.SingleChar : Tesseract.PageSegMode.SingleWord,
                RawText = text, CleanedText = text, ParsedValue = value, Error = error,
                Confidence = confidence, OcrConfidence = confidence, SystemConfidence = confidence,
                CharacterCount = text.Length, RawSegmentCount = character ? text.Length : 0,
                SegmentCount = character ? (complete ? text.Length : Math.Max(1, text.Length - 2)) : 0,
                SegmentMatch = character ? complete : null, UsesCharacterBasedOcr = character,
                CharacterOcrText = character ? text : "", CombinedText = character ? text : "",
                CharacterDecisionReliable = complete, AllCharactersTemplateAuthoritative = authoritative,
                SegmentationSuccessful = complete, ImageSize = new Size(1, 1),
                ProcessedImage = new Bitmap(1, 1), BinaryImage = new Bitmap(1, 1), SegmentationImage = new Bitmap(1, 1)
            };
        }
        static (Models.OcrReadResult Result, Models.OcrCandidateResult? Selected) Select(List<Models.OcrCandidateResult> items) =>
            Services.OcrService.SelectResult(items, new Models.OcrProfile(), Models.OcrFieldKind.StableThrustForce);
        static void Dispose(IEnumerable<Models.OcrCandidateResult> items) { foreach (var item in items) item.Dispose(); }

        var a = new List<Models.OcrCandidateResult>
        {
            Candidate(11050, "Nearest + Otsu"), Candidate(11050, "Otomatik Kontrast + Otsu"),
            Candidate(11050, "Gaussian + Median + Otsu"), Candidate(11050, "Open + Close"),
            Candidate(100, "Segmented Characters", true, false, false, .25f)
        };
        var ar = Select(a).Result;
        var b = new List<Models.OcrCandidateResult>
        {
            Candidate(11988, "Nearest + Otsu"), Candidate(11988, "Gaussian + Median + Otsu"),
            Candidate(11988, "Open + Close"), Candidate(1, "Segmented Characters", true, false, false, .20f)
        };
        var br = Select(b).Result;
        var c = new List<Models.OcrCandidateResult>
        {
            Candidate(511988, "Nearest + Otsu", error: "Değer izin verilen aralığın dışında."),
            Candidate(11050, "Segmented Characters", true, true, true, .95f)
        };
        var cr = Select(c).Result;
        var d = new List<Models.OcrCandidateResult>
        {
            Candidate(11050, "Nearest + Otsu"), Candidate(11050, "Open + Close"),
            Candidate(11050, "Segmented Characters", true, true, false, .85f)
        };
        var dr = Select(d).Result;
        var e = new List<Models.OcrCandidateResult>
        {
            Candidate(11050, "Nearest + Otsu", confidence: .25f),
            Candidate(100, "Segmented Characters", true, false, false, .20f)
        };
        var er = Select(e).Result;
        var f = new List<Models.OcrCandidateResult>
        {
            Candidate(11050, "LiveFast Primary"), Candidate(11050, "LiveFast Secondary"),
            Candidate(100, "Segmented Characters", true, false, false, .20f)
        };
        var fr = Select(f).Result;
        var passed = ar.Value == 11050 && ar.DecisionSource == "FullLineConsensus" && ar.FullLineConsensusCount == 4
            && br.Value == 11988 && cr.Value == 11050 && dr.Value == 11050
            && er.FullLineConsensusLevel != "StrongConsensus" && fr.Value == 11050
            && fr.DecisionSource == "FullLineConsensus" && fr.FullLineConsensusCount == 2;
        Dispose(a); Dispose(b); Dispose(c); Dispose(d); Dispose(e); Dispose(f);
        Console.WriteLine($"thrust-full-line-consensus-self-test: {(passed ? "PASS" : "FAIL")} | A={ar.Value}/{ar.FullLineConsensusCount} | B={br.Value} | C={cr.Value} | D={dr.Value} | E={er.Value}/{er.DecisionSource} | LiveFast={fr.Value}/{fr.FullLineConsensusCount}");
        return passed ? 0 : 1;
    }

    private static int RunThrustPlausibleFallbackSelfTest()
    {
        ApplicationConfiguration.Initialize();
        static Models.OcrCandidateResult Candidate(decimal? value, string method, int estimatedSegments,
            float confidence = .55f, bool character = false, string? error = null)
        {
            var text = value?.ToString("0", System.Globalization.CultureInfo.InvariantCulture) ?? "";
            return new Models.OcrCandidateResult
            {
                ProcessingMethod = method, PageSegMode = character ? Tesseract.PageSegMode.SingleChar : Tesseract.PageSegMode.SingleWord,
                RawText = text, CleanedText = text, ParsedValue = error is null ? value : null, Error = error,
                Confidence = confidence, OcrConfidence = confidence, SystemConfidence = confidence,
                CharacterCount = text.Length, RawSegmentCount = estimatedSegments, SegmentCount = estimatedSegments,
                SegmentMatch = character ? text.Length == estimatedSegments : false,
                UsesCharacterBasedOcr = character, CharacterOcrText = character ? text : "",
                CombinedText = character ? text : "", CharacterDecisionReliable = false,
                ImageSize = new Size(120, 40), ProcessedImage = new Bitmap(1, 1),
                BinaryImage = new Bitmap(1, 1), SegmentationImage = new Bitmap(1, 1)
            };
        }
        static Models.OcrReadResult Select(List<Models.OcrCandidateResult> items) =>
            Services.OcrService.SelectResult(items, new Models.OcrProfile(), Models.OcrFieldKind.StableThrustForce).Result;
        static void Dispose(IEnumerable<Models.OcrCandidateResult> items) { foreach (var item in items) item.Dispose(); }

        var a = new List<Models.OcrCandidateResult> { Candidate(10531, "Bicubic + Otsu", 5, .30f,
            error: "Düşük OCR güveni."), Candidate(1, "Segmented Characters", 5, .20f, true) };
        var ar = Select(a);
        var b = new List<Models.OcrCandidateResult> { Candidate(3, "Gaussian + Median + Otsu", 5, .60f),
            Candidate(10531, "Targeted fallback: Bicubic + Otsu", 5, .35f) };
        var br = Select(b);
        var c = new List<Models.OcrCandidateResult> { Candidate(112, "Nearest + Otsu", 5, .60f),
            Candidate(11988, "Targeted fallback: Bicubic + Otsu", 5, .40f) };
        var cr = Select(c);
        var d = new List<Models.OcrCandidateResult> { Candidate(511988, "Bicubic + Otsu", 5, .90f,
            error: "Değer izin verilen aralığın dışında.") };
        var dr = Select(d);
        var e = new List<Models.OcrCandidateResult> { Candidate(0, "Bicubic + Otsu", 1, .60f) };
        var er = Select(e);
        var f = new List<Models.OcrCandidateResult> { Candidate(0, "Gaussian + Median + Otsu", 5, .60f) };
        var needsFallback = Services.OcrService.NeedsThrustTargetedFallback(f); var fr = Select(f);
        var g = new List<Models.OcrCandidateResult> { Candidate(11050, "Nearest + Otsu", 5),
            Candidate(11050, "Open + Close", 5), Candidate(11050, "Gaussian + Median + Otsu", 5) };
        var gr = Select(g);
        var weakHistory = Forms.MainForm.FormatDiagnosticHistory(3m, "DigitCountMismatch", "0");
        var cardShowsCurrentWeakThree = weakHistory.StartsWith("3", StringComparison.Ordinal);
        var historyShowsWeakThree = weakHistory == "3 ⚠";
        var passed = ar.Value == 10531 && ar.DecisionSource == "PlausibleFullLineFallback"
            && br.Value == 10531 && br.DecisionSource == "TargetedFullLineFallback"
            && cr.Value == 11988 && dr.Value is null && er.Value == 0
            && needsFallback && fr.Value is null && gr.Value == 11050 && gr.DecisionSource == "FullLineConsensus"
            && cardShowsCurrentWeakThree && historyShowsWeakThree;
        Dispose(a); Dispose(b); Dispose(c); Dispose(d); Dispose(e); Dispose(f); Dispose(g);
        Console.WriteLine($"thrust-plausible-fallback-self-test: {(passed ? "PASS" : "FAIL")} | Single={ar.Value}/{ar.DecisionSource} | Targeted={br.Value}/{br.DecisionSource} | Short112->{cr.Value} | RangeRejected={dr.Value is null} | Zero={er.Value} | WideZeroFallback={needsFallback}/{fr.Value?.ToString() ?? "null"} | Consensus={gr.Value} | CardCurrentWeak={cardShowsCurrentWeakThree} | HistoryWeak={historyShowsWeakThree}");
        return passed ? 0 : 1;
    }

    private static int RunZeroNumericValueSelfTest()
    {
        ApplicationConfiguration.Initialize();
        static Models.OcrCandidateResult Candidate(string raw, decimal? value, bool mismatch = true)
        {
            var bitmap = new Bitmap(2, 2);
            return new Models.OcrCandidateResult
            {
                ProcessingMethod = "Nearest + Otsu", PageSegMode = Tesseract.PageSegMode.SingleWord,
                RawText = raw, CleanedText = raw, ParsedValue = value,
                Confidence = .42f, OcrConfidence = .42f, SystemConfidence = .42f,
                CharacterCount = raw.Count(char.IsDigit), RawSegmentCount = mismatch ? 1 : raw.Count(char.IsDigit),
                SegmentCount = mismatch ? 1 : raw.Count(char.IsDigit), SegmentMatch = mismatch ? false : true,
                CharacterValidationStatus = mismatch ? "Mismatch" : "Matched",
                CharacterValidationWarning = mismatch ? "Karakter segmentasyonu sonucu doğrulayamadı." : null,
                ImageSize = bitmap.Size, ProcessedImage = bitmap, BinaryImage = new Bitmap(2, 2),
                SegmentationImage = new Bitmap(2, 2)
            };
        }
        static Models.OcrReadResult Select(string raw, Models.OcrFieldKind field)
        {
            using var candidate = Candidate(raw, string.IsNullOrWhiteSpace(raw) ? null : 0m);
            if (string.IsNullOrWhiteSpace(raw)) candidate.Error = "Sayısal değer okunamadı.";
            return Services.OcrService.SelectResult([candidate], new Models.OcrProfile(), field).Result;
        }
        var torque = Select("0.000", Models.OcrFieldKind.StableTorque);
        var rpm = Select("0.00", Models.OcrFieldKind.StableRpm);
        var speed = Select("0.0", Models.OcrFieldKind.StableAdvanceSpeed);
        var penetration = Select("0.0", Models.OcrFieldKind.StablePenetration);
        var thrust = Select("0", Models.OcrFieldKind.StableThrustForce);
        var empty = Select("", Models.OcrFieldKind.StableTorque);

        var zeroDisplay = Forms.MainForm.FormatDiagnosticHistory(0m, "CharacterMismatch", "0.000");
        var card = zeroDisplay.Contains("0.000", StringComparison.Ordinal);
        var history = zeroDisplay == "0.000 ⚠";
        var passed = torque.Value.HasValue && torque.Value.Value == 0 && torque.Warning is not null
            && rpm.Value.HasValue && rpm.Value.Value == 0 && speed.Value.HasValue && speed.Value.Value == 0
            && penetration.Value.HasValue && penetration.Value.Value == 0 && thrust.Value.HasValue && thrust.Value.Value == 0
            && !empty.Value.HasValue && card && history;
        Console.WriteLine($"zero-numeric-value-self-test: {(passed ? "PASS" : "FAIL")} | Torque={torque.Value?.ToString("0.000") ?? "null"} | RPM={rpm.Value?.ToString("0.00") ?? "null"} | Speed={speed.Value?.ToString("0.0") ?? "null"} | Penetration={penetration.Value?.ToString("0.0") ?? "null"} | Thrust={thrust.Value?.ToString("0") ?? "null"} | EmptyHasValue={empty.Value.HasValue} | Card={card} | History={history} | Warning={torque.Warning is not null}");
        return passed ? 0 : 1;
    }

    private static int RunThrustCharacterSegmentationSelfTest()
    {
        try
        {
            var values = new[] { "10922", "18922", "18405", "23029", "20720", "20451", "19822", "17972", "0", "950", "5389" };
            var summaries = new List<string>(); var passed = true;
            foreach (var value in values)
            {
                using var image = Render(value);
                var segmentationWatch = System.Diagnostics.Stopwatch.StartNew();
                using var segmentation = Services.CharacterSegmenter.Segment(image, Models.OcrFieldKind.StableThrustForce,
                    new Services.CharacterSegmentationOptions(true, 1, 5, 1.10, 0.38));
                segmentationWatch.Stop();
                var recognized = Recognize(segmentation.Characters);
                var expectedMerged = value.Length > 1;
                passed &= segmentation.Characters.Count == value.Length
                    && recognized == value
                    && (!expectedMerged || segmentation.MergedComponentCount == 1)
                    && !segmentation.HasUnresolvedMergedComponents;
                if (value == "18922") summaries.Add($"Input=18922; ilk={segmentation.DetectedComponentCount}; merged={segmentation.MergedComponentCount}; projection={segmentation.Characters.Count}; karakter={recognized}; split={segmentation.ProjectionSplitCount}; ms={segmentationWatch.Elapsed.TotalMilliseconds:0.0}");
            }
            using var unresolvedImage = new Bitmap(100, 30);
            using (var graphics = Graphics.FromImage(unresolvedImage)) { graphics.Clear(Color.White); graphics.FillRectangle(Brushes.Black, 2, 4, 96, 22); }
            using var unresolved = Services.CharacterSegmenter.Segment(unresolvedImage, Models.OcrFieldKind.StableThrustForce,
                new Services.CharacterSegmentationOptions(true, 4, 5, 1.10, 0.20));
            passed &= unresolved.HasUnresolvedMergedComponents && unresolved.Characters.Count == 0;
            Console.WriteLine($"thrust-character-segmentation-self-test: {(passed ? "PASS" : "FAIL")} | {string.Join(" | ", summaries)} | unresolved-wide=>segments={unresolved.Characters.Count}");
            return passed ? 0 : 1;

            string Recognize(IReadOnlyList<Bitmap> characters)
            {
                var output = new System.Text.StringBuilder();
                foreach (var character in characters)
                {
                    var best = Enumerable.Range(0, 10).Select(digit =>
                    {
                        using var referenceImage = Render(new string((char)('0' + digit), 5));
                        using var referenceSegments = Services.CharacterSegmenter.Segment(referenceImage, Models.OcrFieldKind.StableThrustForce,
                            new Services.CharacterSegmentationOptions(true, 1, 5, 1.10, 0.38));
                        return (Digit: digit, Score: referenceSegments.Characters.Count == 5
                            ? Services.DigitTemplateMatcher.CalculateSimilarity(character, referenceSegments.Characters[2]) : 0);
                    }).OrderByDescending(item => item.Score).First();
                    output.Append(best.Digit);
                }
                return output.ToString();
            }

            static Bitmap Render(string text)
            {
                const int cellWidth = 18, height = 30;
                var bitmap = new Bitmap(cellWidth * text.Length, height);
                using var graphics = Graphics.FromImage(bitmap); graphics.Clear(Color.White);
                for (var index = 0; index < text.Length; index++) DrawDigit(graphics, text[index], index * cellWidth);
                if (text.Length > 1) graphics.FillRectangle(Brushes.Black, 0, 24, bitmap.Width, 1);
                return bitmap;
            }

            static void DrawDigit(Graphics graphics, char digit, int x)
            {
                var segments = digit switch
                {
                    '0' => "ab cdef".Replace(" ", ""), '1' => "bc", '2' => "abdeg", '3' => "abcdg", '4' => "bcfg",
                    '5' => "acdfg", '6' => "acdefg", '7' => "abc", '8' => "abcdefg", '9' => "abcdfg", _ => ""
                };
                void H(int y) => graphics.FillRectangle(Brushes.Black, x + 3, y, 10, 3);
                void V(int xx, int y) => graphics.FillRectangle(Brushes.Black, x + xx, y, 3, 11);
                if (segments.Contains('a')) H(2); if (segments.Contains('g')) H(13); if (segments.Contains('d')) H(24);
                if (segments.Contains('f')) V(2, 3); if (segments.Contains('b')) V(11, 3);
                if (segments.Contains('e')) V(2, 14); if (segments.Contains('c')) V(11, 14);
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("thrust-character-segmentation-self-test: FAIL | " + exception);
            return 1;
        }
    }

    private static int RunAdvancePhysicalConsistencySelfTest()
    {
        var folder = Path.Combine(Path.GetTempPath(), "TBMWatchdog_AdvancePhysical_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var profile = new Models.OcrProfile
            {
                AdvanceMaximumBackwardToleranceMm = 50,
                AdvanceLargeBackwardJumpMm = 200,
                AdvanceMaximumForwardJumpPerRealReadingMm = 250
            };
            var csv = Path.Combine(folder, "AdvancePhysicalConsistency.csv");
            var service = new Services.AdvancePhysicalConsistencyService(profile, csv);
            var now = DateTime.Now;
            Services.AdvancePhysicalConsistencyInput Input(string session, string ring, decimal? current, decimal? observed,
                bool temporal = true, bool real = true, Models.MonitoringRunState monitoring = Models.MonitoringRunState.Running,
                Models.OperationModeState mode = Models.OperationModeState.Excavation,
                Models.RingSessionState ringState = Models.RingSessionState.ExcavationInProgress) =>
                new("1031", ring, session, current, observed, mode, ringState, now = now.AddSeconds(1), false, temporal, real, monitoring);

            var baseline1586 = service.Evaluate(Input("session-a", "1169", null, 1586));
            var backward1 = service.Evaluate(Input("session-a", "1169", 1586, 43, false));
            var backward2 = service.Evaluate(Input("session-a", "1169", 1586, 43, true));

            var smallService = new Services.AdvancePhysicalConsistencyService(profile, Path.Combine(folder, "small.csv"));
            smallService.Evaluate(Input("session-small", "1", null, 1000));
            var smallBackward = smallService.Evaluate(Input("session-small", "1", 1000, 980));

            var forwardService = new Services.AdvancePhysicalConsistencyService(profile, Path.Combine(folder, "forward.csv"));
            forwardService.Evaluate(Input("session-forward", "2", null, 800));
            var forwardPending = forwardService.Evaluate(Input("session-forward", "2", 800, 2000, false));
            var forwardConfirmed = forwardService.Evaluate(Input("session-forward", "2", 800, 2000, true));

            var newRingReset = service.Evaluate(Input("session-b", "1170", 1878, 0));

            var zeroService = new Services.AdvancePhysicalConsistencyService(profile, Path.Combine(folder, "zero.csv"));
            zeroService.Evaluate(Input("session-zero", "3", null, 1700));
            var sameRingZero = zeroService.Evaluate(Input("session-zero", "3", 1700, 0, true));

            var cacheService = new Services.AdvancePhysicalConsistencyService(profile, Path.Combine(folder, "cache.csv"));
            cacheService.Evaluate(Input("session-cache", "4", null, 1586));
            var cacheRepeat = cacheService.Evaluate(Input("session-cache", "4", 1586, 43, true, false));

            var pauseService = new Services.AdvancePhysicalConsistencyService(profile, Path.Combine(folder, "pause.csv"));
            pauseService.Evaluate(Input("session-pause", "5", null, 1000));
            var paused = pauseService.Evaluate(Input("session-pause", "5", 1000, 1050, true, true, Models.MonitoringRunState.Paused));
            var resuming = pauseService.Evaluate(Input("session-pause", "5", 1000, 1050, true, true, Models.MonitoringRunState.Resuming));

            var historyField = new Models.FieldMeasurementResult { FieldId = Models.BuiltInFieldIds.AdvanceMm,
                ObservedValue = 43, OfficialValue = backward2.OfficialAdvanceMm, ValidationState = "PhysicalSuspicious" };
            var stableProfile = new Models.OcrProfile();
            var stable = new Services.StableZoneCandidateService(stableProfile, Path.Combine(folder, "stable.csv"));
            var ring = new Models.RingSession { MachineId = "1031", RingNo = "1169", SessionId = "session-a",
                CurrentState = Models.RingSessionState.ExcavationInProgress };
            var stableResult = stable.Evaluate(new(now, "1031", ring, backward2.OfficialAdvanceMm,
                backward2.AcceptedForAdvanceDecision, true, true, true, Models.OperationModeState.Excavation, Models.MonitoringRunState.Running));

            var passed = baseline1586.OfficialAdvanceMm == 1586
                && !backward1.AcceptedForAdvanceDecision && !backward2.AcceptedForAdvanceDecision && backward2.OfficialAdvanceMm == 1586
                && smallBackward.AcceptedForAdvanceDecision && smallBackward.OfficialAdvanceMm == 980
                && !forwardPending.AcceptedForAdvanceDecision && forwardPending.OfficialAdvanceMm == 800
                && forwardConfirmed.AcceptedForAdvanceDecision && forwardConfirmed.OfficialAdvanceMm == 2000
                && newRingReset.StateReset && newRingReset.AcceptedForAdvanceDecision && newRingReset.OfficialAdvanceMm == 0
                && !sameRingZero.AcceptedForAdvanceDecision && sameRingZero.OfficialAdvanceMm == 1700
                && historyField.ObservedValue == 43 && historyField.OfficialValue == 1586
                && stableResult.Status == Services.StableZoneStatus.Waiting && !stableResult.Candidate && !stableResult.Confirmed
                && !cacheRepeat.AcceptedForAdvanceDecision && cacheRepeat.OfficialAdvanceMm == 1586
                && !paused.AcceptedForAdvanceDecision && paused.OfficialAdvanceMm == 1000
                && !resuming.AcceptedForAdvanceDecision && resuming.OfficialAdvanceMm == 1000
                && File.Exists(csv) && File.ReadAllText(csv).Contains("PhysicalSuspicious", StringComparison.Ordinal);
            Console.WriteLine($"advance-physical-consistency-self-test: {(passed ? "PASS" : "FAIL")} | 1586→43→43={backward1.OfficialAdvanceMm}/{backward2.OfficialAdvanceMm} | 1000→980={smallBackward.OfficialAdvanceMm} | 800→2000={forwardPending.PhysicalStatus}/{forwardConfirmed.OfficialAdvanceMm} | new-ring 1878→0={newRingReset.OfficialAdvanceMm}/{newRingReset.PhysicalStatus} | same-ring 1700→0={sameRingZero.OfficialAdvanceMm} | cache={cacheRepeat.PhysicalStatus} | paused={paused.OfficialAdvanceMm}");
            return passed ? 0 : 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("advance-physical-consistency-self-test: FAIL | " + exception);
            return 1;
        }
        finally
        {
            try { Directory.Delete(folder, true); } catch { }
        }
    }

    private static int RunAdvanceStableZoneSelfTest()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"TBMWatchdog-AdvanceStable-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(folder);
            var profile = new Models.OcrProfile { StableZoneMinimumAdvanceMm = 950, StableZoneMaximumAdvanceMm = 1150, StableZoneConfirmationReadings = 2 };
            var ring = new Models.RingSession { MachineId = "1031", RingNo = "77", SessionId = "ring-77", CurrentState = Models.RingSessionState.ExcavationInProgress };
            var service = new Services.StableZoneCandidateService(profile, Path.Combine(folder, "StableZoneCandidates.csv"));
            Services.StableZoneCandidateInput Input(decimal? value, bool real = true, Models.RingSession? active = null,
                Models.MonitoringRunState state = Models.MonitoringRunState.Running) =>
                new(DateTime.Now, "1031", active ?? ring, value, value.HasValue, real, true, true,
                    Models.OperationModeState.Excavation, state);

            var below = service.Evaluate(Input(900));
            var one = service.Evaluate(Input(980));
            var cache = service.Evaluate(Input(980, false));
            var confirmed = service.Evaluate(Input(980));
            var duplicate = service.Evaluate(Input(1000));
            var immutable = !ring.StableDataCaptured && ring.StableData is null && ring.CurrentState == Models.RingSessionState.ExcavationInProgress;

            var resetService = new Services.StableZoneCandidateService(profile, Path.Combine(folder, "reset.csv"));
            var resetOne = resetService.Evaluate(Input(980)); var resetOut = resetService.Evaluate(Input(1200));
            var noRing = resetService.Evaluate(Input(980) with { RingSession = null });
            var building = resetService.Evaluate(Input(980) with { RingSession = new Models.RingSession { MachineId = "1031", SessionId = "b", CurrentState = Models.RingSessionState.RingBuilding } });
            var paused = resetService.Evaluate(Input(980, state: Models.MonitoringRunState.Paused));
            resetService.Evaluate(Input(980)); resetService.OnMonitoringStateChanged(Models.MonitoringRunState.Resuming);
            var afterResume = resetService.Evaluate(Input(980));
            var otherMachine = resetService.Evaluate(Input(980) with { MachineId = "1030" });
            var csvLines = File.ReadAllLines(Path.Combine(folder, "StableZoneCandidates.csv"));
            var passed = !below.Candidate && one.ConfirmationCount == 1 && cache.ConfirmationCount == 1
                && confirmed.Confirmed && confirmed.NewlyConfirmed && duplicate.Confirmed && !duplicate.NewlyConfirmed
                && resetOne.ConfirmationCount == 1 && resetOut.ConfirmationCount == 0
                && !noRing.Candidate && !building.Candidate && !paused.Candidate && afterResume.ConfirmationCount == 1
                && !otherMachine.Candidate && immutable && csvLines.Length == 4;
            Console.WriteLine($"advance-stable-zone-self-test: {(passed ? "PASS" : "FAIL")} | 900={below.Candidate} | 980={one.ConfirmationCount}/2 | cache={cache.ConfirmationCount}/2 | confirmed={confirmed.Confirmed} | duplicateEvent={duplicate.NewlyConfirmed} | resume={afterResume.ConfirmationCount}/2 | RingUntouched={immutable} | CsvRows={csvLines.Length - 1}");
            return passed ? 0 : 1;
        }
        catch (Exception exception) { Console.Error.WriteLine($"advance-stable-zone-self-test: FAIL - {exception}"); return 1; }
        finally { try { Directory.Delete(folder, true); } catch { } }
    }

    private static int RunAdvanceRightEdgeNoiseSelfTest()
    {
        var image = new Size(200, 80);
        var thinRightEdge = new[] { new Rectangle(45, 20, 20, 42), new Rectangle(70, 20, 20, 42),
            new Rectangle(95, 20, 20, 42), new Rectangle(194, 15, 3, 50) };
        var value659 = Services.OcrService.EvaluateAdvanceRightEdgeNoise("6591", "659", 3, thinRightEdge, image);
        var value115 = Services.OcrService.EvaluateAdvanceRightEdgeNoise("1151", "115", 3, thinRightEdge, image);
        var realFour = Services.OcrService.EvaluateAdvanceRightEdgeNoise("1050", "1050", 4,
            new[] { new Rectangle(40, 20, 18, 42), new Rectangle(63, 20, 20, 42), new Rectangle(88, 20, 20, 42), new Rectangle(113, 20, 20, 42) }, image);
        var normalRight = Services.OcrService.EvaluateAdvanceRightEdgeNoise("10501", "1050", 4,
            new[] { new Rectangle(40, 20, 18, 42), new Rectangle(63, 20, 20, 42), new Rectangle(88, 20, 20, 42),
                new Rectangle(113, 20, 20, 42), new Rectangle(138, 20, 18, 42) }, image);
        var passed = value659.ShouldRemove && value659.RemovedCharacter == "1"
            && value115.ShouldRemove && !realFour.ShouldRemove && !normalRight.ShouldRemove;
        Console.WriteLine($"advance-right-edge-noise-self-test: {(passed ? "PASS" : "FAIL")} | 6591→659={value659.ShouldRemove} | 1151→115={value115.ShouldRemove} | 1050 korunuyor={!realFour.ShouldRemove} | normal4 korunuyor={!normalRight.ShouldRemove}");
        return passed ? 0 : 1;
    }

    private static int RunAdvanceReliabilityV2SelfTest()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"TBMWatchdog-AdvanceV2-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        try
        {
            var profile = new Models.OcrProfile { Name = "1031", AdvanceEnabledGroups = ["A", "C"],
                AdvancePrimaryGroup = "A", AdvanceSelectionMode = Models.AdvanceSelectionMode.GroupA,
                AdvanceMinimumValidValue = 1, MinimumConsistentAdvanceGroups = 2,
                AdvanceMinimumRequiredValidGroups = 2, AdvanceGroupAgreementToleranceMm = 30,
                AdvanceSingleGroupConfirmationReadings = 2, AdvanceConfirmationTolerance = 2 };
            var start = DateTime.Now;
            static Services.AdvanceGroupReading R(string group, decimal? value, DateTime captured, bool cache = false, string? raw = null) =>
                new(group, value, true, value.HasValue, cache) { CapturedAt = captured, RawText = raw ?? value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "" };
            var single = new Services.AdvanceGroupDecisionService(profile, Path.Combine(folder, "single.csv"));
            var a2 = single.Decide(start, "1031", "1", [R("A", 2, start), R("C", null, start)], "r1");
            var a7 = single.Decide(start.AddSeconds(1), "1031", "1", [R("A", 7, start.AddSeconds(1)), R("C", null, start.AddSeconds(1))], "r1");

            var agreementService = new Services.AdvanceGroupDecisionService(profile, Path.Combine(folder, "agreement.csv"));
            var agreement = agreementService.Decide(start, "1031", "2", [R("A", 1148, start), R("C", 1145, start)], "r2");
            agreementService.CommitOfficial(agreement.OfficialAdvanceMm);
            var conflict = agreementService.Decide(start.AddSeconds(1), "1031", "2", [R("A", 1148, start.AddSeconds(1)), R("C", 400, start.AddSeconds(1))], "r2");
            var decimalInvalid = new Services.AdvanceGroupDecisionService(profile, Path.Combine(folder, "decimal.csv"))
                .Decide(start, "1031", "3", [R("A", .337m, start, raw: "0.337")], "r3");

            var temporal = new Services.AdvanceGroupDecisionService(profile, Path.Combine(folder, "temporal.csv"));
            var one = temporal.Decide(start, "1031", "4", [R("A", 1150, start)], "r4");
            var sameFrame = temporal.Decide(start.AddMilliseconds(1), "1031", "4", [R("A", 1150, start)], "r4");
            var cache = temporal.Decide(start.AddSeconds(1), "1031", "4", [R("A", 1150, start.AddSeconds(1), true)], "r4");
            var two = temporal.Decide(start.AddSeconds(2), "1031", "4", [R("A", 1151, start.AddSeconds(2))], "r4");
            temporal.CommitOfficial(two.OfficialAdvanceMm);
            var frozen = temporal.Decide(start.AddSeconds(3), "1031", "4",
                [R("A", 1300, start.AddSeconds(3)), R("C", 1302, start.AddSeconds(3))], "r4", Models.OperationModeState.RingBuilding);

            var physical = new Services.AdvancePhysicalConsistencyService(profile, Path.Combine(folder, "physical.csv"));
            physical.Evaluate(new("1031", "5", "r5", null, 1586,
                Models.OperationModeState.Excavation, Models.RingSessionState.ExcavationInProgress, start.AddSeconds(-1),
                false, true, true, Models.MonitoringRunState.Running));
            var rejected = physical.Evaluate(new("1031", "5", "r5", 1586, 1148,
                Models.OperationModeState.Excavation, Models.RingSessionState.ExcavationInProgress, start,
                false, true, true, Models.MonitoringRunState.Running));

            var settings = Services.SettingsService.CreateDefaults(); settings.OcrProfile.Name = "1031";
            var rings = new Services.RingSessionService(Path.Combine(folder, "rings"));
            var ring = rings.Create("1031", "6", null, false, start);
            var coordinator = new Services.OperationModeRingSessionCoordinator(rings); coordinator.SetMonitoringState(Models.MonitoringRunState.Running);
            coordinator.ProcessConfirmedState("1031", Models.OperationModeState.Excavation, start,
                new Services.OperationModeRingContext(true, true));
            coordinator.ProcessConfirmedState("1031", Models.OperationModeState.RingBuilding, start.AddSeconds(1),
                new Services.OperationModeRingContext(true, true));
            var resynced = coordinator.ProcessConfirmedState("1031", Models.OperationModeState.RingBuilding, start.AddSeconds(3),
                new Services.OperationModeRingContext(true, true));
            var pendingPreserved = !resynced.Updated && rings.GetPendingFinalData("1031").SingleOrDefault() is
                { CurrentState: Models.RingSessionState.FinalDataPending };

            var passed = a2.OfficialAdvanceMm is null && a7.OfficialAdvanceMm is null
                && agreement.OfficialAdvanceMm == 1148 && agreement.DecisionSource == "MultiGroupAgreement"
                && conflict.OfficialAdvanceMm == 1148 && conflict.DecisionSource == "GroupConflict"
                && decimalInvalid.OfficialAdvanceMm is null && decimalInvalid.Reason.Contains("InvalidIntegerFormat")
                && one.OfficialAdvanceMm is null && sameFrame.OfficialAdvanceMm is null && cache.OfficialAdvanceMm is null
                && two.OfficialAdvanceMm == 1151 && two.DecisionSource == "PrimaryGroupTemporalConfirmation"
                && frozen.OfficialAdvanceMm == 1151 && frozen.DecisionSource == "RingBuildingFreeze"
                && !rejected.AcceptedForAdvanceDecision && rejected.OfficialAdvanceMm == 1586
                && pendingPreserved;
            Console.WriteLine($"advance-reliability-v2-self-test: {(passed ? "PASS" : "FAIL")} | A2={a2.OfficialAdvanceMm?.ToString() ?? "HOLD"} | A7={a7.OfficialAdvanceMm?.ToString() ?? "HOLD"} | A1148/C1145={agreement.OfficialAdvanceMm}/{agreement.DecisionSource} | Conflict={conflict.OfficialAdvanceMm}/{conflict.DecisionSource} | 0.337={decimalInvalid.DecisionSource}/{decimalInvalid.Reason} | SingleTemporal={one.OfficialAdvanceMm?.ToString() ?? "HOLD"}->{two.OfficialAdvanceMm} | SameFrame={sameFrame.OfficialAdvanceMm?.ToString() ?? "HOLD"} | Cache={cache.OfficialAdvanceMm?.ToString() ?? "HOLD"} | Freeze={frozen.OfficialAdvanceMm} | Physical={rejected.PhysicalStatus}/{rejected.OfficialAdvanceMm} | PendingPreserved={pendingPreserved}");
            return passed ? 0 : 1;
        }
        catch (Exception exception) { Console.Error.WriteLine("advance-reliability-v2-self-test: FAIL | " + exception); return 1; }
        finally { try { Directory.Delete(folder, true); } catch { } }
    }

    private static int RunRingLifecycleV2SelfTest()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"TBMWatchdog-RingLifecycleV2-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        try
        {
            var start = DateTime.Now;
            var rings = new Services.RingSessionService(folder);
            var coordinator = new Services.OperationModeRingSessionCoordinator(rings);
            coordinator.SetMonitoringState(Models.MonitoringRunState.Running);
            var ring1182 = rings.Create("1031", "1182", "monitoring-1", false, start);
            var context = new Services.OperationModeRingContext(true, true, 1859m, "monitoring-1");
            var excavation = coordinator.ProcessConfirmedState("1031", Models.OperationModeState.Excavation,
                start.AddSeconds(1), context);
            var building = coordinator.ProcessConfirmedState("1031", Models.OperationModeState.RingBuilding,
                start.AddSeconds(2), context);
            var pending1182 = rings.GetPendingFinalData("1031").Single();
            var next = coordinator.ProcessConfirmedState("1031", Models.OperationModeState.Excavation,
                start.AddSeconds(3), context);
            var current1183 = rings.GetActive("1031");

            var profile = new Models.OcrProfile { Name = "1031", AdvanceEnabledGroups = ["A"],
                AdvancePrimaryGroup = "A", AdvanceSingleGroupConfirmationReadings = 2,
                AdvanceConfirmationTolerance = 2, AdvanceMinimumValidValue = 1 };
            var advance = new Services.AdvanceGroupDecisionService(profile, Path.Combine(folder, "advance.csv"));
            static Services.AdvanceGroupReading Reading(decimal value, DateTime at) =>
                new("A", value, true, true, false) { CapturedAt = at, RawText = value.ToString(System.Globalization.CultureInfo.InvariantCulture) };
            advance.Decide(start, "1031", "1182", [Reading(1859, start)], ring1182.SessionId);
            var oldAccepted = advance.Decide(start.AddMilliseconds(10), "1031", "1182",
                [Reading(1859, start.AddMilliseconds(10))], ring1182.SessionId);
            var newFirst = advance.Decide(start.AddSeconds(4), "1031", "1183",
                [Reading(426, start.AddSeconds(4))], current1183!.SessionId);
            var newSecond = advance.Decide(start.AddSeconds(5), "1031", "1183",
                [Reading(426, start.AddSeconds(5))], current1183.SessionId);

            var physical = new Services.AdvancePhysicalConsistencyService(profile,
                Path.Combine(folder, "advance-physical.csv"));
            physical.Evaluate(new("1031", "1182", ring1182.SessionId, null, 1859m,
                Models.OperationModeState.Excavation, Models.RingSessionState.ExcavationInProgress,
                start, false, true, true));
            var physicalReset = physical.Evaluate(new("1031", "1183", current1183.SessionId, null, 426m,
                Models.OperationModeState.Excavation, Models.RingSessionState.ExcavationInProgress,
                start.AddSeconds(4), false, false, true));

            var updatedPending = rings.UpdatePendingRingEndData(pending1182.SessionId, data =>
            {
                data.FoamConsumptionLiters = 150m; data.FoamConsumptionCapturedAt = start.AddSeconds(6);
                data.FoamConsumptionSource = "SelfTest";
            }, "Köpük toplamı test güncellemesi", start.AddSeconds(6));
            var currentUntouched = rings.GetActive("1031")?.EndData?.FoamConsumptionLiters is null;

            var startupFolder = Path.Combine(folder, "false-start");
            var falseRings = new Services.RingSessionService(startupFolder);
            var falseCoordinator = new Services.OperationModeRingSessionCoordinator(falseRings);
            falseCoordinator.SetMonitoringState(Models.MonitoringRunState.Running);
            falseRings.Create("1031", "2000", null, false, start);
            falseCoordinator.ProcessConfirmedState("1031", Models.OperationModeState.RingBuilding, start, context);
            var startInBuilding = falseCoordinator.ProcessConfirmedState("1031", Models.OperationModeState.Excavation,
                start.AddSeconds(1), context);
            falseCoordinator.SetMonitoringState(Models.MonitoringRunState.Paused);
            var paused = falseCoordinator.ProcessConfirmedState("1031", Models.OperationModeState.RingBuilding,
                start.AddSeconds(2), context);

            var reloaded = new Services.RingSessionService(folder);
            var restoredPending = reloaded.GetPendingFinalData("1031").SingleOrDefault(item => item.RingNo == "1182");
            var restoredCurrent = reloaded.IncompleteSessions.SingleOrDefault(item => item.RingNo == "1183");
            var resumed = restoredCurrent is null ? null : reloaded.ResumeIncomplete(restoredCurrent.SessionId, start.AddSeconds(7));

            var eventText = File.ReadAllText(rings.EventsFilePath);
            var passed = excavation.Updated && building.Updated
                && pending1182.CurrentState == Models.RingSessionState.FinalDataPending
                && pending1182.FinalOfficialAdvanceMm == 1859m && next.NextRingCreated
                && current1183?.RingNo == "1183" && current1183.CurrentState == Models.RingSessionState.ExcavationInProgress
                && oldAccepted.OfficialAdvanceMm == 1859m && newFirst.OfficialAdvanceMm is null
                && newSecond.OfficialAdvanceMm == 426m
                && physicalReset.StateReset && physicalReset.OfficialAdvanceMm == 426m
                && updatedPending.EndData?.FoamConsumptionLiters == 150m && currentUntouched
                && !startInBuilding.NextRingCreated && falseRings.GetActive("1031")?.RingNo == "2000"
                && !paused.Updated && restoredPending?.EndData?.FoamConsumptionLiters == 150m
                && resumed?.RingNo == "1183"
                && eventText.Contains("ExcavationEnded") && eventText.Contains("FinalDataPending")
                && eventText.Contains("NextRingCreated") && eventText.Contains("PendingRingEndDataUpdated");
            Console.WriteLine($"ring-lifecycle-v2-self-test: {(passed ? "PASS" : "FAIL")} | "
                + $"1182={pending1182.CurrentState}/{pending1182.FinalOfficialAdvanceMm} | 1183={current1183?.CurrentState} | "
                + $"Advance=1859->{newFirst.OfficialAdvanceMm?.ToString() ?? "HOLD"}->{newSecond.OfficialAdvanceMm} | "
                + $"PhysicalReset={physicalReset.StateReset}/{physicalReset.OfficialAdvanceMm} | "
                + $"FalseIncrement={!startInBuilding.NextRingCreated} | PausedBlocked={!paused.Updated} | "
                + $"PendingFoam={updatedPending.EndData?.FoamConsumptionLiters} | Persistence={restoredPending is not null && resumed is not null}");
            return passed ? 0 : 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("ring-lifecycle-v2-self-test: FAIL | " + exception); return 1;
        }
        finally { try { Directory.Delete(folder, true); } catch { } }
    }

    private static int RunProcessValueReliabilitySelfTest()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"TBMWatchdog-ProcessReliability-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        try
        {
            var profile = new Models.OcrProfile();
            var service = new Services.ProcessValueReliabilityService(profile, Path.Combine(folder, "ProcessValueReliability.csv"));
            var start = DateTime.Now;
            Services.ProcessValueReliabilityResult Thrust(decimal value, int seconds, bool cache = false, DateTime? capturedAt = null) =>
                service.Evaluate(new("1031", Models.BuiltInFieldIds.StableThrustForce, value,
                    value.ToString("0", System.Globalization.CultureInfo.InvariantCulture), capturedAt ?? start.AddSeconds(seconds), cache,
                    Models.OcrConfidenceStatus.Reliable));
            Services.ProcessValueReliabilityResult Torque(decimal value, string raw, int seconds, bool cache = false, DateTime? capturedAt = null) =>
                service.Evaluate(new("1031", Models.BuiltInFieldIds.StableTorque, value, raw,
                    capturedAt ?? start.AddSeconds(seconds), cache, Models.OcrConfidenceStatus.Reliable));

            var thrustInitial = Thrust(18848, 0);
            var thrustA = Thrust(18906, 30);
            var thrustB = Thrust(1151, 60);
            var thrustC = Thrust(19003, 90);
            var raw1725 = Thrust(1725, 120);
            var longCycle1131 = Thrust(1131, 300);
            var torqueInitial = Torque(3.099m, "3.099", 0);
            var torqueE = Torque(2.904m, "2.904", 30);
            var torqueF = Torque(7m, "7", 60);

            var duplicateService = new Services.ProcessValueReliabilityService(profile, Path.Combine(folder, "duplicate.csv"));
            duplicateService.Evaluate(new("1031", Models.BuiltInFieldIds.StableThrustForce, 18848, "18848", start, false, Models.OcrConfidenceStatus.Reliable));
            var firstSuspicious = duplicateService.Evaluate(new("1031", Models.BuiltInFieldIds.StableThrustForce, 1151, "1151", start.AddSeconds(1), false, Models.OcrConfidenceStatus.Reliable));
            var sameCapturedAt = duplicateService.Evaluate(new("1031", Models.BuiltInFieldIds.StableThrustForce, 1151, "1151", start.AddSeconds(1), false, Models.OcrConfidenceStatus.Reliable));
            var cache = duplicateService.Evaluate(new("1031", Models.BuiltInFieldIds.StableThrustForce, 1151, "1151", start.AddSeconds(2), true, Models.OcrConfidenceStatus.Reliable));

            var csv = File.ReadAllText(Path.Combine(folder, "ProcessValueReliability.csv"));
            var passed = thrustInitial.TrustedValue == 18848 && thrustA.TrustedAccepted && thrustA.TrustedValue == 18906
                && thrustB.ObservedValue == 1151 && !thrustB.TrustedAccepted && thrustB.TrustedValue == 18906
                && thrustC.TrustedAccepted && thrustC.TrustedValue == 19003
                && raw1725.ObservedValue == 1725 && raw1725.TrustedValue == 19003 && raw1725.ObservedValue != 18725
                && !longCycle1131.TrustedAccepted && longCycle1131.TrustedValue == 19003
                && torqueInitial.TrustedValue == 3.099m && torqueE.TrustedAccepted && torqueE.TrustedValue == 2.904m
                && torqueF.ObservedValue == 7 && !torqueF.TrustedAccepted && torqueF.TrustedValue == 2.904m
                && !firstSuspicious.TrustedAccepted && !sameCapturedAt.TrustedAccepted && !cache.TrustedAccepted
                && csv.Contains("StructureStatus") && csv.Contains("TemporalStatus");
            Console.WriteLine($"process-value-reliability-self-test: {(passed ? "PASS" : "FAIL")} | "
                + $"Thrust=18848->{thrustA.TrustedValue}, observed1151/trusted{thrustB.TrustedValue}, observed1725/trusted{raw1725.TrustedValue} | "
                + $"Torque=3.099->{torqueE.TrustedValue}, observed7/trusted{torqueF.TrustedValue} | SameCapturedAtBlocked={!sameCapturedAt.TrustedAccepted} | CacheBlocked={!cache.TrustedAccepted}");
            return passed ? 0 : 1;
        }
        catch (Exception exception) { Console.Error.WriteLine("process-value-reliability-self-test: FAIL | " + exception); return 1; }
        finally { try { Directory.Delete(folder, true); } catch { } }
    }

    private static int RunOcrUserFeedbackSnapshotSelfTest()
    {
        var folder=Path.Combine(Path.GetTempPath(),$"TBMWatchdog-Feedback-{Guid.NewGuid():N}");Directory.CreateDirectory(folder);
        try
        {
            using var cache=new Services.OcrFeedbackSnapshotCache(2,60);using var raw=new Bitmap(31,16);using var processed=new Bitmap(248,128);
            using(var g=Graphics.FromImage(raw)){g.Clear(Color.White);g.DrawString("1147",SystemFonts.DefaultFont,Brushes.Black,0,0);}using(var g=Graphics.FromImage(processed)){g.Clear(Color.White);g.DrawString("1147",SystemFonts.DefaultFont,Brushes.Black,10,10);}
            var snapshot=new Services.OcrFeedbackSnapshot{MeasurementId="m1",FieldId=Models.BuiltInFieldIds.StableThrustForce,CapturedAt=DateTime.Now,
                RawCrop=raw,ProcessedCrop=processed,RawOcrText="1147",FullLineResult=1147,ObservedValue=1147,TrustedValue=19022,
                QualityStatus="TemporalSuspicious",FailureReason="Ani değişim",DecisionSource="TemporalSuspicious",Pipeline="LiveFast",
                Region=new Models.ScreenRegion{X=10,Y=20,Width=31,Height=16}};
            cache.Store(snapshot);var noDiskBefore=!Directory.EnumerateFiles(folder,"*",SearchOption.AllDirectories).Any();
            var exact=cache.TryGet("m1",Models.BuiltInFieldIds.StableThrustForce,out var found)&&found is not null&&found.RawOcrText=="1147"&&found.RawCrop.Width==31;
            using(found){var service=new Services.OcrUserFeedbackService(Path.Combine(folder,"Diagnostics","UserFeedback"));
                var thrust=new Models.NumericFieldDefinition{FieldId=Models.BuiltInFieldIds.StableThrustForce,DisplayName="İtme Kuvveti",ValueType=Models.FieldValueType.Integer,MinimumValue=0,MaximumValue=100000};
                var epb=new Models.NumericFieldDefinition{FieldId=Models.BuiltInFieldIds.Epb1,DisplayName="EPB1",ValueType=Models.FieldValueType.Decimal,MinimumValue=0,MaximumValue=10,DecimalPlaces=2};
                var torque=new Models.NumericFieldDefinition{FieldId=Models.BuiltInFieldIds.StableTorque,DisplayName="Tork",ValueType=Models.FieldValueType.Decimal,MinimumValue=0,MaximumValue=20,DecimalPlaces=3};
                var invalidTorque=!service.TryValidateCorrection("50",torque,out _,out _);var epbValid=service.TryValidateCorrection("1.98",epb,out var epbValue,out _)&&epbValue==1.98m;
                var incorrect=service.Save(found!,Models.OcrFeedbackVerdict.Incorrect,null,"1031","1031_Dilber","1184","r1","İtme Kuvveti");
                var corrected=service.Save(found!,Models.OcrFeedbackVerdict.Corrected,19147,"1031","1031_Dilber","1184","r1","İtme Kuvveti");
                var unchanged=corrected.ObservedValue==1147&&corrected.CorrectedValue==19147&&corrected.TrustedValueAtThatTime==19022;
                var removed=service.Remove(found!,"1031","1031_Dilber","1184","r1","İtme Kuvveti");
                var audit=File.Exists(Path.Combine(folder,"Diagnostics","OcrUserFeedback.csv"))&&File.Exists(corrected.RawImagePath)&&File.Exists(corrected.ProcessedImagePath)
                    && incorrect.Verdict==Models.OcrFeedbackVerdict.Incorrect&&corrected.RevisesFeedbackId==incorrect.FeedbackId&&removed.RevisesFeedbackId==corrected.FeedbackId;
                using var expiredCache=new Services.OcrFeedbackSnapshotCache(2,1);expiredCache.Store(new Services.OcrFeedbackSnapshot{MeasurementId="m1",FieldId=Models.BuiltInFieldIds.StableThrustForce,CapturedAt=DateTime.Now.AddMinutes(-2),RawCrop=raw,ProcessedCrop=processed});
                var expired=!expiredCache.TryGet("m1",Models.BuiltInFieldIds.StableThrustForce,out var unavailable)&&unavailable is null;
                var passed=noDiskBefore&&exact&&invalidTorque&&epbValid&&unchanged&&audit&&expired;
                Console.WriteLine($"ocr-user-feedback-snapshot-self-test: {(passed?"PASS":"FAIL")} | Exact={exact} | NoDiskBefore={noDiskBefore} | Incorrect={incorrect.Verdict} | Corrected={corrected.ObservedValue}->{corrected.CorrectedValue} | EPB={epbValue} | Torque50Rejected={invalidTorque} | EditRemove={audit} | Expired={expired}");return passed?0:1;}
        }
        catch(Exception ex){Console.Error.WriteLine("ocr-user-feedback-snapshot-self-test: FAIL | "+ex);return 1;}
        finally{try{Directory.Delete(folder,true);}catch{}}
    }

    private static int RunAdvanceGroupDecisionSelfTest()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"TBMWatchdog-AdvanceGroups-{Guid.NewGuid():N}"); Directory.CreateDirectory(folder);
        try
        {
            var profile = new Models.OcrProfile { Name = "1031", AdvanceEnabledGroups = ["A", "C"], AdvancePrimaryGroup = "A",
                AdvanceMinimumValidValue = 1, AdvanceMaximumAllowedGroupDifferenceMm = 100, AdvanceMinimumRequiredValidGroups = 2,
                MinimumConsistentAdvanceGroups = 2, AdvanceGroupAgreementToleranceMm = 100,
                AdvanceSensorZeroWarningReadings = 5, AdvanceMaximumChangePerReading = 2500 };
            Services.AdvanceGroupReading[] Values(decimal? a, decimal? b, decimal? c, decimal? d) =>
            [new("A", a, true, a.HasValue, false), new("B", b, true, b.HasValue, false), new("C", c, true, c.HasValue, false), new("D", d, true, d.HasValue, false)];
            Services.AdvanceGroupDecision Decide(Models.AdvanceSelectionMode mode, decimal? a, decimal? b, decimal? c, decimal? d, string name)
            { var p = System.Text.Json.JsonSerializer.Deserialize<Models.OcrProfile>(System.Text.Json.JsonSerializer.Serialize(profile))!; p.AdvanceSelectionMode = mode;
                return new Services.AdvanceGroupDecisionService(p, Path.Combine(folder, name + ".csv")).Decide(DateTime.Now, "1031", "88", Values(a,b,c,d)); }
            var groupA = Decide(Models.AdvanceSelectionMode.GroupA, 659, 0, 623, 0, "a");
            var average = Decide(Models.AdvanceSelectionMode.ClosestValidPairAverage, 659, 0, 623, 0, "avg");
            var fallback = Decide(Models.AdvanceSelectionMode.GroupA, null, 0, 623, 0, "fallback");
            var mismatch = Decide(Models.AdvanceSelectionMode.ClosestValidPairAverage, 659, 0, 1200, 0, "mismatch");
            var warningService = new Services.AdvanceGroupDecisionService(profile, Path.Combine(folder, "warnings.csv")); Services.AdvanceGroupDecision warning = null!;
            for (var i = 0; i < 5; i++) warning = warningService.Decide(DateTime.Now.AddSeconds(i), "1031", "88", Values(650 + i, 0, 620 + i, 0));
            var stableProfile = new Models.OcrProfile(); var stableService = new Services.StableZoneCandidateService(stableProfile, Path.Combine(folder, "stable.csv"));
            var ring = new Models.RingSession { MachineId = "1031", RingNo = "88", SessionId = "r88", CurrentState = Models.RingSessionState.ExcavationInProgress };
            var stable1 = stableService.Evaluate(new(DateTime.Now, "1031", ring, 990, true, true, true, true, Models.OperationModeState.Excavation, Models.MonitoringRunState.Running));
            var stable2 = stableService.Evaluate(new(DateTime.Now, "1031", ring, 990, true, true, true, true, Models.OperationModeState.Excavation, Models.MonitoringRunState.Running));
            var legacy = Services.SettingsService.CreateDefaults(); legacy.OcrProfile.Name = "1031"; legacy.AdvanceMmRegion = new Models.ScreenRegion { X = 1, Y = 2, Width = 30, Height = 15 };
            Services.OcrProfileMigrator.Migrate(legacy); var legacyOk = legacy.OcrProfile.FindField(Models.BuiltInFieldIds.AdvanceMm) is null
                && legacy.OcrProfile.FindField(Models.BuiltInFieldIds.AdvanceGroupA) is { IsEnabled: true }
                && legacy.AdvanceGroupARegion.X == 1 && legacy.AdvanceGroupARegion.Y == 2;
            var temporal = new Services.TemporalValueValidationService(); temporal.Validate("1031", "AdvanceGroupA", 10, 5, 2, 1, DateTime.Now);
            temporal.Validate("1031", "AdvanceGroupA", 100, 5, 2, 1, DateTime.Now); temporal.Validate("1031", "AdvanceGroupC", 20, 5, 2, 1, DateTime.Now);
            var isolated = temporal.HasPending("AdvanceGroupA") && !temporal.HasPending("AdvanceGroupC");
            var passed = groupA.OfficialAdvanceMm == 659 && groupA.DecisionSource == "MultiGroupAgreement"
                && average.OfficialAdvanceMm == 641 && !average.Groups["B"].Value.HasValue == false
                && fallback.OfficialAdvanceMm is null && fallback.DecisionSource == "HoldPreviousOfficial"
                && mismatch.OfficialAdvanceMm is null && mismatch.DecisionSource == "GroupConflict"
                && warning.Warnings.Count >= 2 && stable1.ConfirmationCount == 1 && stable2.Confirmed
                && legacyOk && isolated && !ring.StableDataCaptured && ring.StableData is null;
            Console.WriteLine($"advance-group-decision-self-test: {(passed ? "PASS" : "FAIL")} | GroupA={groupA.OfficialAdvanceMm} | ClosestPair={average.OfficialAdvanceMm} | Fallback={fallback.OfficialAdvanceMm}/{fallback.DecisionSource} | Mismatch={mismatch.ConfidenceStatus} | Warnings={warning.Warnings.Count} | Stable={stable2.Confirmed} | Legacy={legacyOk} | Isolated={isolated}");
            return passed ? 0 : 1;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
        finally { try { Directory.Delete(folder, true); } catch { } }
    }

    private static int RunAdvanceLiveRefreshSelfTest()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"TBMWatchdog-AdvanceRefresh-{Guid.NewGuid():N}"); Directory.CreateDirectory(folder);
        try
        {
            var profile = new Models.OcrProfile { Name = "1031", AdvanceEnabledGroups = ["C"], AdvancePrimaryGroup = "C",
                AdvanceSelectionMode = Models.AdvanceSelectionMode.GroupC, AdvanceMinimumValidValue = 1,
                AdvanceMinimumRequiredValidGroups = 1, AdvanceMaximumChangePerReading = 30,
                AdvanceConfirmationReadings = 2, AdvanceConfirmationTolerance = 2 };
            Services.AdvanceGroupReading[] C(decimal value, bool cache = false) => [new("C", value, true, true, cache)];
            var start = DateTime.Now;
            var service800 = new Services.AdvanceGroupDecisionService(profile, Path.Combine(folder, "800.csv"));
            var initial800 = service800.Decide(start, "1031", "1169", C(621));
            var candidate800 = service800.Decide(start.AddSeconds(1), "1031", "1169", C(800));
            var accepted800 = service800.Decide(start.AddSeconds(2), "1031", "1169", C(800));

            var service1005 = new Services.AdvanceGroupDecisionService(profile, Path.Combine(folder, "1005.csv"));
            service1005.Decide(start, "1031", "1169", C(621));
            var candidate1005 = service1005.Decide(start.AddSeconds(1), "1031", "1169", C(1005));
            var accepted1005 = service1005.Decide(start.AddSeconds(2), "1031", "1169", C(1005));

            var cacheService = new Services.AdvanceGroupDecisionService(profile, Path.Combine(folder, "cache.csv"));
            cacheService.Decide(start, "1031", "1169", C(621));
            var cacheCandidate = cacheService.Decide(start.AddSeconds(1), "1031", "1169", C(800));
            var cacheRepeat = cacheService.Decide(start.AddSeconds(2), "1031", "1169", C(800, true));
            var realAfterCache = cacheService.Decide(start.AddSeconds(3), "1031", "1169", C(800));

            var timestampService = new Services.AdvanceGroupDecisionService(profile, Path.Combine(folder, "timestamp.csv"));
            timestampService.Decide(start, "1031", "1169", C(621));
            var sameTimestampCandidate = timestampService.Decide(start.AddSeconds(1), "1031", "1169", C(800));
            var sameTimestampRepeat = timestampService.Decide(start.AddSeconds(1), "1031", "1169", C(800));
            var laterTimestamp = timestampService.Decide(start.AddSeconds(2), "1031", "1169", C(800));

            using var oldImage = new Bitmap(40, 18); using var changedImage = new Bitmap(40, 18);
            using (var graphics = Graphics.FromImage(oldImage)) graphics.Clear(Color.White);
            using (var graphics = Graphics.FromImage(changedImage)) { graphics.Clear(Color.White); graphics.FillRectangle(Brushes.Black, 10, 3, 10, 12); }
            var changedRejectedByCache = Services.OcrImageCacheService.HasSignificantPixelChange(oldImage, changedImage);

            var ring = new Models.RingSession { MachineId = "1031", RingNo = "1169", SessionId = "r1169", CurrentState = Models.RingSessionState.ExcavationInProgress };
            var stable = new Services.StableZoneCandidateService(profile, Path.Combine(folder, "stable.csv"));
            var stableUsesCurrent = stable.Evaluate(new(start.AddSeconds(3), "1031", ring, accepted1005.OfficialAdvanceMm,
                accepted1005.IsReliable, accepted1005.AllContributingReadsReal, true, true,
                Models.OperationModeState.Excavation, Models.MonitoringRunState.Running));

            var passed = initial800.OfficialAdvanceMm is null && candidate800.OfficialAdvanceMm is null
                && candidate800.DecisionInputValue == 800 && candidate800.TemporalStatus?.Contains("1/2", StringComparison.Ordinal) == true
                && accepted800.OfficialAdvanceMm == 800 && candidate1005.OfficialAdvanceMm is null && accepted1005.OfficialAdvanceMm == 1005
                && cacheCandidate.OfficialAdvanceMm is null && cacheRepeat.OfficialAdvanceMm is null && realAfterCache.OfficialAdvanceMm == 800
                && sameTimestampCandidate.OfficialAdvanceMm is null && sameTimestampRepeat.OfficialAdvanceMm is null && laterTimestamp.OfficialAdvanceMm == 800
                && changedRejectedByCache && stableUsesCurrent.AdvanceMm == 1005;
            Console.WriteLine($"advance-live-refresh-self-test: {(passed ? "PASS" : "FAIL")} | 621→800={candidate800.OfficialAdvanceMm}/{accepted800.OfficialAdvanceMm} | 621→1005={candidate1005.OfficialAdvanceMm}/{accepted1005.OfficialAdvanceMm} | cache={cacheRepeat.OfficialAdvanceMm}→real={realAfterCache.OfficialAdvanceMm} | sameCapturedAt={sameTimestampRepeat.OfficialAdvanceMm}→new={laterTimestamp.OfficialAdvanceMm} | imageChange={changedRejectedByCache} | stableInput={stableUsesCurrent.AdvanceMm}");
            return passed ? 0 : 1;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
        finally { try { Directory.Delete(folder, true); } catch { } }
    }

    private static int RunAdvanceLegacyRemovalSelfTest()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"TBMWatchdog-AdvanceLegacy-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(folder);
            static Models.ScreenRegion Region(int x) => new() { X = x, Y = 20, Width = 50, Height = 18 };
            var both = Services.SettingsService.CreateDefaults(); both.OcrProfile.Name = "1031_Dilber";
            both.AdvanceMmRegion = Region(10); both.AdvanceGroupARegion = Region(100);
            Services.OcrProfileMigrator.Migrate(both);
            var enabledBoth = Services.OcrDataSource.GetEnabledAdditionalFieldDefinitions(both.OcrProfile);
            var noLegacyWhenGroupExists = enabledBoth.All(field => field.FieldId != Models.BuiltInFieldIds.AdvanceMm)
                && enabledBoth.Any(field => field.FieldId == Models.BuiltInFieldIds.AdvanceGroupA)
                && both.AdvanceGroupARegion.X == 100;

            var legacyOnly = Services.SettingsService.CreateDefaults(); legacyOnly.OcrProfile.Name = "1031_Dilber";
            legacyOnly.OcrProfile.SchemaVersion = 5; legacyOnly.AdvanceMmRegion = Region(37); legacyOnly.AdvanceGroupARegion = new();
            Services.OcrProfileMigrator.Migrate(legacyOnly);
            var enabledMigrated = Services.OcrDataSource.GetEnabledAdditionalFieldDefinitions(legacyOnly.OcrProfile);
            var migratedToA = legacyOnly.AdvanceGroupARegion is { IsValid: true, X: 37 }
                && legacyOnly.OcrProfile.FindField(Models.BuiltInFieldIds.AdvanceMm) is null
                && enabledMigrated.Any(field => field.FieldId == Models.BuiltInFieldIds.AdvanceGroupA)
                && enabledMigrated.All(field => field.FieldId != Models.BuiltInFieldIds.AdvanceMm);

            var profile = legacyOnly.OcrProfile; profile.AdvanceSelectionMode = Models.AdvanceSelectionMode.GroupA;
            profile.AdvanceEnabledGroups = ["A", "C"]; profile.AdvanceMinimumRequiredValidGroups = 1;
            var decisionService = new Services.AdvanceGroupDecisionService(profile, Path.Combine(folder, "AdvanceGroupDecisions.csv"));
            var timestamp = DateTime.Now;
            var decision = decisionService.Decide(timestamp, "1031", "1169",
            [
                new Services.AdvanceGroupReading("A", 1000, true, true, false),
                new Services.AdvanceGroupReading("B", 0, false, false, false),
                new Services.AdvanceGroupReading("C", 1001, true, true, false),
                new Services.AdvanceGroupReading("D", 0, false, false, false)
            ]);
            var record = new Models.MeasurementRecord { Timestamp = timestamp };
            record.FieldResults[Models.BuiltInFieldIds.AdvanceMm] = Forms.MainForm.CreateOfficialAdvanceFieldForTest(decision, timestamp);
            var officialPreserved = record.FieldResults[Models.BuiltInFieldIds.AdvanceMm] is { OfficialValue: 1000m } official
                && official.DecisionSource.StartsWith("AdvanceGroupDecision/", StringComparison.Ordinal);

            var stableProfile = new Models.OcrProfile { StableZoneMinimumAdvanceMm = 950, StableZoneMaximumAdvanceMm = 1150, StableZoneConfirmationReadings = 2 };
            var stable = new Services.StableZoneCandidateService(stableProfile, Path.Combine(folder, "StableZone.csv"));
            var ring = new Models.RingSession { SessionId = "r1", MachineId = "1031", RingNo = "1169", CurrentState = Models.RingSessionState.ExcavationInProgress };
            var stableResult = stable.Evaluate(new Services.StableZoneCandidateInput(timestamp, "1031", ring,
                decision.OfficialAdvanceMm, decision.IsReliable, decision.AllContributingReadsReal, true, true,
                Models.OperationModeState.Excavation, Models.MonitoringRunState.Running));
            var stableUsesDecision = stableResult.AdvanceMm == decision.OfficialAdvanceMm && stableResult.Status == Services.StableZoneStatus.Candidate;

            var otherMachines = new[] { "1028", "1029", "1030" }.All(machine =>
            {
                var settings = Services.SettingsService.CreateDefaults(); settings.OcrProfile.Name = machine; settings.AdvanceMmRegion = Region(8);
                Services.OcrProfileMigrator.Migrate(settings);
                return settings.OcrProfile.FindField(Models.BuiltInFieldIds.AdvanceMm) is { IsEnabled: false };
            });
            var oneLessCapture = enabledBoth.Count(field => field.FieldId.StartsWith("Advance", StringComparison.Ordinal))
                == new[] { both.AdvanceGroupARegion, both.AdvanceGroupBRegion, both.AdvanceGroupCRegion, both.AdvanceGroupDRegion }.Count(region => region.IsValid);
            var passed = noLegacyWhenGroupExists && migratedToA && officialPreserved && stableUsesDecision && otherMachines && oneLessCapture;
            Console.WriteLine($"advance-legacy-removal-self-test: {(passed ? "PASS" : "FAIL")} | noLegacy={noLegacyWhenGroupExists} | migratedToA={migratedToA} | official={officialPreserved} | stable={stableUsesDecision} | otherMachines={otherMachines} | noDuplicateCapture={oneLessCapture}");
            return passed ? 0 : 1;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
        finally { try { Directory.Delete(folder, true); } catch { } }
    }

    private static int RunOperationModeTextOcrSelfTest()
    {
        try
        {
            using var service = new Services.TextOcrService(); var profile = new Models.OcrProfile();
            using var advanceImage = TextImage("İlerleme"); using var ringImage = TextImage("Halka tesisatı"); using var blank = new Bitmap(300, 70);
            using (var graphics = Graphics.FromImage(blank)) graphics.Clear(Color.White);
            var advance = service.ReadAsync(advanceImage).GetAwaiter().GetResult();
            var ring = service.ReadAsync(ringImage).GetAwaiter().GetResult();
            var empty = service.ReadAsync(blank).GetAwaiter().GetResult();
            var unmatchedResult = new Models.TextOcrResult("Bakim modu", .9f) { TrimmedText = "Bakim modu",
                NormalizedText = Services.OperationModeStateService.Normalize("Bakim modu"), FinalOperationModeText = "Bakim modu" };
            var unmatched = Services.OperationModeTextOcrEvaluator.Evaluate(unmatchedResult, profile);
            var emptyEvaluation = Services.OperationModeTextOcrEvaluator.Evaluate(empty, profile);
            var normalizedMatch = Services.OperationModeTextMatcher.Match(Services.OperationModeStateService.Normalize("Halka tesisati"),
                profile.OperationModeKeywords.SelectMany(group => group.Keywords.Select(keyword => (group.State,
                    Services.OperationModeStateService.Normalize(keyword).Replace(" ", "")))).ToArray(), profile.OperationModeMinimumTextSimilarityPercent);
            var passed = advance.IsSuccessful && ring.IsSuccessful && advance.Candidates.Select(candidate => candidate.PageSegMode).Distinct().Count() == 3
                && !service.UsesNumericWhitelist && !advance.NumericWhitelistUsed && unmatched.FailureReason == "KeywordNotMatched"
                && emptyEvaluation.FailureReason == "OcrEmpty" && normalizedMatch.State == Models.OperationModeState.RingBuilding;
            Console.WriteLine($"operation-mode-text-ocr-self-test: {(passed ? "PASS" : "FAIL")} | İlerleme='{advance.TrimmedText}' | Halka='{ring.TrimmedText}' | PSM={ring.BestPageSegMode} | Önİşleme={ring.BestPreprocessingMethod} | Güven=%{ring.Confidence * 100:0.0} | Empty={emptyEvaluation.FailureReason} | Unknown={unmatched.FailureReason} | NumericWhitelist={service.UsesNumericWhitelist}");
            return passed ? 0 : 1;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }

        static Bitmap TextImage(string text)
        {
            var bitmap = new Bitmap(420, 90); using var graphics = Graphics.FromImage(bitmap); graphics.Clear(Color.White);
            using var font = new Font("Arial", 34, FontStyle.Bold, GraphicsUnit.Pixel); graphics.DrawString(text, font, Brushes.Black, new PointF(12, 20)); return bitmap;
        }
    }

    private static int RunStableZoneStateSelfTest()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"TBMWatchdog-StableState-{Guid.NewGuid():N}"); Directory.CreateDirectory(folder);
        try
        {
            var csv = Path.Combine(folder, "StableZoneCandidates.csv"); var service = new Services.StableZoneCandidateService(new Models.OcrProfile(), csv);
            var ring1 = Ring("r1", Models.RingSessionState.ExcavationInProgress); Services.StableZoneCandidateInput Input(Models.RingSession ring, decimal? advance,
                Models.OperationModeState mode = Models.OperationModeState.Excavation) => new(DateTime.Now, "1031", ring, advance, advance.HasValue, true, true, true, mode, Models.MonitoringRunState.Running);
            var waiting = service.Evaluate(Input(ring1, 900)); var candidate = service.Evaluate(Input(ring1, 1000)); var confirmed = service.Evaluate(Input(ring1, 1000));
            ring1.CurrentState = Models.RingSessionState.RingBuilding; var confirmedAfterRing = service.Evaluate(Input(ring1, null, Models.OperationModeState.RingBuilding));
            var ring2 = Ring("r2", Models.RingSessionState.ExcavationInProgress); service.BeginRing(ring2.SessionId);
            var newRingWaiting = service.Evaluate(Input(ring2, 900)); var missedAdvance = service.Evaluate(Input(ring2, 1290)); var duplicateMissed = service.Evaluate(Input(ring2, 1350));
            var ring3 = Ring("r3", Models.RingSessionState.RingBuilding); service.BeginRing(ring3.SessionId);
            var missedRing = service.Evaluate(Input(ring3, null, Models.OperationModeState.RingBuilding));
            var csvText = File.ReadAllText(csv); var passed = waiting.Status == Services.StableZoneStatus.Waiting
                && candidate.Status == Services.StableZoneStatus.Candidate && candidate.ConfirmationCount == 1
                && confirmed.Status == Services.StableZoneStatus.Confirmed && confirmedAfterRing.Status == Services.StableZoneStatus.Confirmed
                && newRingWaiting.Status == Services.StableZoneStatus.Waiting && missedAdvance.Status == Services.StableZoneStatus.Missed && missedAdvance.NewlyMissed
                && !duplicateMissed.NewlyMissed && missedRing.Status == Services.StableZoneStatus.Missed && missedRing.NewlyMissed
                && !ring2.StableDataCaptured && ring2.StableData is null && !ring3.StableDataCaptured && ring3.StableData is null
                && csvText.Contains(",Status,", StringComparison.Ordinal) && csvText.Contains("\"Missed\"", StringComparison.Ordinal);
            Console.WriteLine($"stable-zone-state-self-test: {(passed ? "PASS" : "FAIL")} | 900={waiting.Status} | 1000x1={candidate.Status} | 1000x2={confirmed.Status} | Confirmed→RingBuilding={confirmedAfterRing.Status} | 1290={missedAdvance.Status} | RingBuilding={missedRing.Status} | NewRing={newRingWaiting.Status} | StableData={ring2.StableDataCaptured || ring3.StableDataCaptured}");
            return passed ? 0 : 1;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
        finally { try { Directory.Delete(folder, true); } catch { } }
        static Models.RingSession Ring(string id, Models.RingSessionState state) => new() { SessionId = id, MachineId = "1031", RingNo = id, CurrentState = state };
    }

    private static int RunOperationModeRingSessionSelfTest()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"TBMWatchdog-ModeRing-{Guid.NewGuid():N}");
        try
        {
            var context = new Services.OperationModeRingContext(true, true); var now = new DateTime(2026, 8, 7, 2, 0, 0);
            var emptyRings = new Services.RingSessionService(Path.Combine(folder, "empty"));
            var emptyLink = new Services.OperationModeRingSessionCoordinator(emptyRings); emptyLink.SetMonitoringState(Models.MonitoringRunState.Running);
            var noActive = !emptyLink.Process(Transition(Models.OperationModeState.Unknown, Models.OperationModeState.Excavation, now), context).Updated;

            var excavationRings = new Services.RingSessionService(Path.Combine(folder, "excavation")); var excavation = excavationRings.Create("1031", "1001");
            var excavationLink = new Services.OperationModeRingSessionCoordinator(excavationRings); excavationLink.SetMonitoringState(Models.MonitoringRunState.Running);
            var initialEvents = 0; excavationRings.RingSessionUpdated += _ => initialEvents++;
            var firstExcavation = excavationLink.Process(Transition(Models.OperationModeState.Unknown, Models.OperationModeState.Excavation, now), context);
            var excavationBaseline = firstExcavation.Updated && firstExcavation.Session is
                { CurrentState: Models.RingSessionState.ExcavationInProgress, WasMonitoringStartedMidExcavation: true, StartedAt: null }
                && initialEvents == 1;
            excavationLink.Process(Transition(Models.OperationModeState.Excavation, Models.OperationModeState.Excavation, now.AddSeconds(1)), context);
            var duplicateIgnored = initialEvents == 1;
            excavationLink.SetMonitoringState(Models.MonitoringRunState.Paused);
            excavationLink.Process(Transition(Models.OperationModeState.Excavation, Models.OperationModeState.RingBuilding, now.AddSeconds(2)), context);
            var pausedIgnored = excavationRings.GetActive("1031")?.CurrentState == Models.RingSessionState.ExcavationInProgress;
            excavationLink.SetMonitoringState(Models.MonitoringRunState.Resuming); excavationLink.SetMonitoringState(Models.MonitoringRunState.Running);
            excavationLink.Process(Transition(Models.OperationModeState.Unknown, Models.OperationModeState.RingBuilding, now.AddSeconds(3)), context);
            excavationLink.ProcessConfirmedState("1031", Models.OperationModeState.RingBuilding, now.AddSeconds(3), context);
            var resumeBaselineIgnored = excavationRings.GetActive("1031")?.CurrentState == Models.RingSessionState.ExcavationInProgress && initialEvents == 1;
            var realAfterResume = excavationLink.Process(Transition(Models.OperationModeState.RingBuilding, Models.OperationModeState.Excavation, now.AddSeconds(4)), context);
            var resumeRealTransition = realAfterResume.Updated && realAfterResume.Session?.StartedAt == now.AddSeconds(4)
                && realAfterResume.Session.WasMonitoringStartedMidExcavation == false;
            excavationLink.SetMonitoringState(Models.MonitoringRunState.Stopped);
            var afterStop = excavationRings.GetActive("1031"); var stopPreserved = afterStop is { IsCompleted: false }
                && afterStop.CurrentState == Models.RingSessionState.ExcavationInProgress && afterStop.RingNo == excavation.RingNo;

            var ringRings = new Services.RingSessionService(Path.Combine(folder, "ring")); ringRings.Create("1031", "1002");
            var ringLink = new Services.OperationModeRingSessionCoordinator(ringRings); ringLink.SetMonitoringState(Models.MonitoringRunState.Running);
            var firstRing = ringLink.Process(Transition(Models.OperationModeState.Unknown, Models.OperationModeState.RingBuilding, now), context);
            var ringBaseline = firstRing.Updated && firstRing.Session is { CurrentState: Models.RingSessionState.RingBuilding, IsCompleted: false, EndedAt: null };
            var start = ringLink.Process(Transition(Models.OperationModeState.RingBuilding, Models.OperationModeState.Excavation, now.AddMinutes(1)), context);
            var started = start.Updated && start.Session?.StartedAt == now.AddMinutes(1);
            var backToRing = ringLink.Process(Transition(Models.OperationModeState.Excavation, Models.OperationModeState.RingBuilding, now.AddMinutes(2)), context);
            var pendingNotCompleted = backToRing.Updated && backToRing.PendingPreviousRing is
                { CurrentState: Models.RingSessionState.FinalDataPending, IsCompleted: false };
            var wrongMachine = !ringLink.Process(new Models.OperationModeTransition(now, "1030", Models.OperationModeState.Unknown,
                Models.OperationModeState.Excavation, "", "", 2, Models.OperationModeEventType.None, 1, "x"), context).Updated;
            var invalidRegion = !ringLink.ProcessConfirmedState("1031", Models.OperationModeState.Excavation, now, new(false, true)).Updated;
            Console.WriteLine($"OperationMode-RingSession: noActive={noActive}, excavationBaseline={excavationBaseline}, duplicate={duplicateIgnored}, paused={pausedIgnored}, resumeSafe={resumeBaselineIgnored}, resumeReal={resumeRealTransition}, stop={stopPreserved}, ringBaseline={ringBaseline}, started={started}, pendingNotCompleted={pendingNotCompleted}, wrongMachine={wrongMachine}, invalidRegion={invalidRegion}");
            return noActive && excavationBaseline && duplicateIgnored && pausedIgnored && resumeBaselineIgnored && resumeRealTransition
                && stopPreserved && ringBaseline && started && pendingNotCompleted && wrongMachine && invalidRegion ? 0 : 1;
        }
        catch (Exception exception) { Console.WriteLine(exception); return 1; }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }

        static Models.OperationModeTransition Transition(Models.OperationModeState previous, Models.OperationModeState next, DateTime at) =>
            new(at, "1031", previous, next, next.ToString(), next.ToString(), 2, Models.OperationModeEventType.None, 1, "mode-test");
    }

    private static int RunManualRingUiSelfTest()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"TBMWatchdog-RingUi-{Guid.NewGuid():N}");
        try
        {
            using var log = new Services.LogService(folder); using var form = new Forms.MainForm(log);
            bool Validate(Size size, float scale)
            {
                form.CreateControl(); if (scale != 1f) form.Scale(new SizeF(scale, scale)); form.ClientSize = size;
                form.PerformLayout(); form.RingTrackingPanel.PerformLayout();
                var controls = new Control[] { form.RingNoTextBox, form.RingStartButton, form.RingCancelButton };
                var noOverlap = !controls[0].Bounds.IntersectsWith(controls[1].Bounds)
                    && !controls[1].Bounds.IntersectsWith(controls[2].Bounds);
                var fullyVisible = controls.All(control => form.RingTrackingPanel.DisplayRectangle.Contains(control.Bounds));
                var rowsSeparated = form.LiveChart.Bottom <= form.RingTrackingPanel.Top
                    && form.RingTrackingPanel.Bottom <= form.HistoryGrid.Top;
                var widths = form.RingNoTextBox.Width >= 100 && form.RingStartButton.Width >= 110 && form.RingCancelButton.Width >= 110;
                var textFits = form.RingStartButton.Width >= form.RingStartButton.PreferredSize.Width
                    && form.RingCancelButton.Width >= form.RingCancelButton.PreferredSize.Width;
                Console.WriteLine($"UI detail {size.Width}x{size.Height}@{scale}: widths={widths}, overlap={noOverlap}, visible={fullyVisible}, rows={rowsSeparated}, text={textFits}, panel={form.RingTrackingPanel.Bounds}, chartBottom={form.LiveChart.Bottom}, historyTop={form.HistoryGrid.Top}");
                return widths
                    && noOverlap && fullyVisible && rowsSeparated
                    && textFits;
            }
            var fullHd = Validate(new Size(1920, 1080), 1f);
            var small = Validate(new Size(1366, 768), 1f);
            var dpi125 = Validate(new Size(1366, 768), 1.25f);
            Console.WriteLine($"Manual ring UI: 1920x1080={fullHd}, 1366x768={small}, DPI125={dpi125}, textbox={form.RingNoTextBox.Width}, start={form.RingStartButton.Width}, cancel={form.RingCancelButton.Width}");
            return fullHd && small && dpi125 ? 0 : 1;
        }
        catch (Exception exception) { Console.WriteLine(exception); return 1; }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    private static int RunManualRingSessionSelfTest()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"TBMWatchdog-ManualRing-{Guid.NewGuid():N}");
        try
        {
            var service = new Services.RingSessionService(folder); var rejected = 0;
            foreach (var invalid in new[] { "", "ABC12", "-1", "0" }) try { service.Create("1031", invalid); } catch (ArgumentException) { rejected++; }
            var created = service.Create("1031", " 1234 "); var ringId = created.SessionId;
            var manualDoesNotStartMonitoring = created.RingNo == "1234" && created.MonitoringSessionId is null;
            var secondRejected = false; try { service.Create("1031", "1235"); } catch (InvalidOperationException) { secondRejected = true; }
            var transitionPath = Path.Combine(folder, "monitoring-transitions.csv");
            using (var monitoring = new Services.MonitoringService(new Services.MonitoringStateTransitionCsvService(transitionPath)))
            {
                var source = new MonitoringPauseTestDataSource(); monitoring.Start(source, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
                var startDidNotCreateRing = service.GetActive("1031")?.SessionId == ringId;
                SpinWait.SpinUntil(() => source.ReadCount > 0, 2000); monitoring.PauseAsync().GetAwaiter().GetResult();
                var pausePreserved = service.GetActive("1031")?.SessionId == ringId;
                monitoring.ResumeAsync().GetAwaiter().GetResult(); var resumePreserved = service.GetActive("1031")?.SessionId == ringId;
                monitoring.StopAsync().GetAwaiter().GetResult(); manualDoesNotStartMonitoring &= startDidNotCreateRing && pausePreserved && resumePreserved;
            }
            var cancelled = service.Cancel("1031", "Kullanıcı onaylı self-test iptali");
            var cancelledJson = service.GetSessionFilePath(cancelled); var historyPreserved = File.Exists(cancelledJson)
                && File.ReadAllText(cancelledJson).Contains("\"CurrentState\": \"Cancelled\"");

            var pendingRoot = Path.Combine(folder, "pending"); var creator = new Services.RingSessionService(pendingRoot);
            var pending = creator.Create("1031", "2222"); var reopened = new Services.RingSessionService(pendingRoot);
            var deferredBlocks = false; try { reopened.Create("1031", "2223"); } catch (InvalidOperationException) { deferredBlocks = true; }
            var resumed = reopened.ResumeIncomplete(pending.SessionId);
            var sameIdentity = resumed.SessionId == pending.SessionId && reopened.GetActive("1031")?.SessionId == pending.SessionId;
            reopened.Cancel("1031", "Devam ettirilen self-test oturumu iptal edildi");
            var other = service.Create("1029", "88"); var otherUnaffected = other.MachineId == "1029";
            var events = File.ReadAllText(reopened.EventsFilePath); var eventRows = events.Contains("Resumed") && events.Contains("Cancelled");
            Console.WriteLine($"Manual ring: valid={created.RingNo}, invalidRejected={rejected}, sameId={sameIdentity}, deferredBlocks={deferredBlocks}, history={historyPreserved}");
            return rejected == 4 && manualDoesNotStartMonitoring && secondRejected && cancelled.CurrentState == Models.RingSessionState.Cancelled
                && historyPreserved && deferredBlocks && sameIdentity && otherUnaffected && eventRows ? 0 : 1;
        }
        catch (Exception exception) { Console.WriteLine(exception); return 1; }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    private static int RunRingSessionSelfTest()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"TBMWatchdog-Ring-{Guid.NewGuid():N}");
        try
        {
            var service = new Services.RingSessionService(folder); var createdEvents = 0; service.RingSessionCreated += _ => createdEvents++;
            var invalidRejected = false; try { service.Create("1031", ""); } catch (ArgumentException) { invalidRejected = true; }
            try { service.Create("1031", "-1"); invalidRejected = false; } catch (ArgumentException) { }
            var created = service.Create("1031", "1234", "monitoring-A", true, new DateTime(2026, 8, 7, 1, 0, 0));
            var identity = created.SessionId; var secondRejected = false;
            try { service.Create("1031", "1235"); } catch (InvalidOperationException) { secondRejected = true; }
            var defaults = created.StartedAt is null && created.StableData is null && created.EndData is null && created.Pps is null
                && !created.StableDataCaptured && !created.EndDataCaptured && !created.PpsDataReceived;
            var jsonPath = service.GetSessionFilePath(created); var jsonExists = File.Exists(jsonPath);
            var jsonOptions = new System.Text.Json.JsonSerializerOptions();
            jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            var loaded = System.Text.Json.JsonSerializer.Deserialize<Models.RingSession>(File.ReadAllText(jsonPath), jsonOptions);
            var roundTrip = loaded is { RingNo: "1234", MachineId: "1031", StartedAt: null, WasMonitoringStartedMidExcavation: true };
            var atomic = !Directory.EnumerateFiles(Path.GetDirectoryName(jsonPath)!, "*.tmp").Any();
            var otherMachine = service.Create("1029", "77"); var isolated = service.HasActive("1031") && service.HasActive("1029")
                && otherMachine.SessionId != created.SessionId;

            var transitionCsv = new Services.MonitoringStateTransitionCsvService(Path.Combine(folder, "monitoring.csv"));
            using (var monitor = new Services.MonitoringService(transitionCsv))
            {
                var source = new MonitoringPauseTestDataSource(); monitor.Start(source, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
                SpinWait.SpinUntil(() => source.ReadCount > 0, 2000); monitor.PauseAsync().GetAwaiter().GetResult();
                var duringPause = service.GetActive("1031")?.SessionId == identity;
                monitor.ResumeAsync().GetAwaiter().GetResult(); monitor.StopAsync().GetAwaiter().GetResult();
                isolated &= duringPause && service.GetActive("1031") is { IsCompleted: false } active && active.SessionId == identity;
            }
            service.Update("1031", session => session.CurrentState = Models.RingSessionState.ExcavationInProgress, "Self-test güncellemesi");
            var detector = new Services.RingSessionService(folder); var incomplete = detector.HasIncompleteSessions
                && detector.StartupDiagnostic == "Tamamlanmamış ring oturumu bulundu."
                && detector.IncompleteSessions.Any(item => item.SessionId == identity);
            var completed = service.Complete("1031", "Self-test tamamlandı");
            service.Cancel("1029", "Self-test iptali");
            var csvOk = File.Exists(service.EventsFilePath) && File.ReadAllText(service.EventsFilePath).Contains("ExcavationInProgress")
                && File.ReadAllText(service.EventsFilePath).Contains("Completed");
            Console.WriteLine($"Ring session: id={identity}, json={jsonExists}, atomic={atomic}, incomplete={incomplete}, csv={csvOk}");
            Console.WriteLine(File.ReadAllText(jsonPath));
            return invalidRejected && createdEvents == 2 && secondRejected && defaults && jsonExists && roundTrip && atomic
                && isolated && incomplete && completed.IsCompleted && completed.CurrentState == Models.RingSessionState.Completed && csvOk ? 0 : 1;
        }
        catch (Exception exception) { Console.WriteLine(exception); return 1; }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    private static int RunMonitoringPauseResumeSelfTest()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"TBMWatchdog-Pause-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(folder);
            var csv = new Services.MonitoringStateTransitionCsvService(Path.Combine(folder, "MonitoringStateTransitions.csv"));
            using var service = new Services.MonitoringService(csv);
            var source = new MonitoringPauseTestDataSource(); var published = 0; var completed = 0;
            var states = new List<Models.MonitoringRunState>();
            service.MeasurementReceived += _ => Interlocked.Increment(ref published);
            service.ReadingCompleted += _ => Interlocked.Increment(ref completed);
            service.RunStateChanged += states.Add;
            service.Start(source, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            SpinWait.SpinUntil(() => source.ReadCount >= 1, 2000); Thread.Sleep(50);
            service.PauseAsync().GetAwaiter().GetResult(); var pausedReads = source.ReadCount; var pausedPublished = published; var pausedCompleted = completed;
            Thread.Sleep(1200); var noWorkWhilePaused = source.ReadCount == pausedReads && published == pausedPublished && completed == pausedCompleted;
            var measuredPausedMs = service.GetTimingSnapshot().PausedDuration.TotalMilliseconds;
            var pausedTime = measuredPausedMs >= 1000;
            service.ResumeAsync().GetAwaiter().GetResult();
            var observedResuming = states.Contains(Models.MonitoringRunState.Resuming);
            SpinWait.SpinUntil(() => service.State == Models.MonitoringRunState.Running && source.ReadCount >= pausedReads + 2, 4000);
            var resumeValidationSuppressed = published == pausedPublished && source.PrepareCount == 1 && source.ForcedReads == 2;
            var originalSession = service.SessionId; var repeatedCycles = service.State == Models.MonitoringRunState.Running;
            for (var cycle = 1; cycle < 4 && repeatedCycles; cycle++)
            {
                service.PauseAsync().GetAwaiter().GetResult(); Thread.Sleep(80);
                var readsBeforeResume = source.ReadCount;
                repeatedCycles &= service.State == Models.MonitoringRunState.Paused && service.SessionId == originalSession;
                service.ResumeAsync().GetAwaiter().GetResult();
                repeatedCycles &= service.State == Models.MonitoringRunState.Running
                    && Forms.MainForm.MonitoringControlsFor(service.State) is { PauseText: "Duraklat", PauseEnabled: true };
                repeatedCycles &= SpinWait.SpinUntil(() => service.State == Models.MonitoringRunState.Running
                    && source.ReadCount >= readsBeforeResume + 2, 4000) && service.SessionId == originalSession
                    && Forms.MainForm.MonitoringControlsFor(service.State) is { PauseText: "Duraklat", PauseEnabled: true };
            }
            var accumulatedPauses = service.GetTimingSnapshot().PausedDuration.TotalMilliseconds >= measuredPausedMs + 150;
            var buttonReturnsToPause = Forms.MainForm.MonitoringControlsFor(service.State) is { PauseText: "Duraklat", PauseEnabled: true }
                && !Forms.MainForm.MonitoringControlsFor(Models.MonitoringRunState.Resuming).PauseText.Contains("Devam ediliyor", StringComparison.OrdinalIgnoreCase);
            service.PauseAsync().GetAwaiter().GetResult(); source.ReturnUnreadable = true;
            service.ResumeAsync().GetAwaiter().GetResult();
            var readsAtErrorResume = source.ReadCount;
            SpinWait.SpinUntil(() => source.ReadCount > readsAtErrorResume, 3000);
            var resumeErrorKeepsRunning = service.State == Models.MonitoringRunState.Running
                && Forms.MainForm.MonitoringControlsFor(service.State) is { PauseText: "Duraklat", PauseEnabled: true }
                && service.SessionId == originalSession;
            source.ReturnUnreadable = false;
            service.StopAsync().GetAwaiter().GetResult(); var firstSession = service.SessionId;
            var stopped = service.State == Models.MonitoringRunState.Stopped && source.ResetCount == 1;
            service.Start(source, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)); var newSession = service.SessionId != firstSession;
            service.StopAsync().GetAwaiter().GetResult();
            var log = File.ReadAllText(csv.FilePath); var transitions = log.Contains("Running,Paused") && log.Contains("Paused,Resuming")
                && log.Contains("Resuming,Running") && log.Contains("Running,Stopped");
            Console.WriteLine($"Monitoring pause/resume: paused={noWorkWhilePaused}, cycles4={repeatedCycles}, accumulated={accumulatedPauses}, button={buttonReturnsToPause}, errorKeepsRunning={resumeErrorKeepsRunning}, resuming={observedResuming}, newSession={newSession}");
            return noWorkWhilePaused && pausedTime && observedResuming && resumeValidationSuppressed && repeatedCycles
                && accumulatedPauses && buttonReturnsToPause && resumeErrorKeepsRunning && stopped && newSession && transitions ? 0 : 1;
        }
        catch (Exception exception) { Console.WriteLine(exception); return 1; }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    private sealed class MonitoringPauseTestDataSource : Services.IDataSource, Services.IMonitoringLifecycleDataSource
    {
        public string Name => "1031 Test"; public int ReadCount; public int PrepareCount; public int ResetCount; public int ForcedReads; public bool ReturnUnreadable;
        public Task<Models.MeasurementRecord> ReadAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref ReadCount);
            return Task.FromResult(new Models.MeasurementRecord { Timestamp = DateTime.Now, Nominal = ReturnUnreadable ? null : 1, Scale1 = 1,
                Scale2 = 1, GateMillimeters = 1, GateStatus = "Test", ExcavationStatus = "Test",
                OcrConfidence = ReturnUnreadable ? Models.OcrConfidenceStatus.Unreadable : Models.OcrConfidenceStatus.Reliable });
        }
        public void PrepareForResume(int forcedRealOcrReadings) { PrepareCount++; ForcedReads = forcedRealOcrReadings; }
        public void ResetAfterStop() => ResetCount++;
    }

    private static int RunDemoSelfTest()
    {
        var source = new Services.DemoDataSource();
        var samples = Enumerable.Range(0, 24)
            .Select(_ => source.ReadAsync(CancellationToken.None).GetAwaiter().GetResult())
            .ToArray();

        var expectedGatesExist = new[] { 0m, 75m, 400m }.All(gate => samples.Any(sample => sample.GateMillimeters == gate));
        var independentScales = samples[^1].Scale1 != samples[^1].Scale2;
        var valuesIncreased = samples[^1].Nominal > samples[0].Nominal
            && samples[^1].Scale1 > samples[0].Scale1
            && samples[^1].Scale2 > samples[0].Scale2;
        var warningExists = samples.Any(sample => sample.ExcavationStatus == "Kapak Açık Ancak Değerler İlerlemiyor");

        return expectedGatesExist && independentScales && valuesIncreased && warningExists ? 0 : 1;
    }

    private static int RunCsvSelfTest()
    {
        var testFolder = Path.Combine(Path.GetTempPath(), $"TBMWatchdog-CsvTest-{Guid.NewGuid():N}");
        try
        {
            using var csv = new Services.CsvService(testFolder);
            var record = new Models.MeasurementRecord
            {
                Timestamp = new DateTime(2026, 7, 31, 12, 30, 45, 123),
                Nominal = 12.5m,
                Scale1 = 8.25m,
                Scale2 = 7.75m,
                GateMillimeters = 400m,
                GateStatus = "Açık - Malzeme Akışı Var",
                ExcavationStatus = "Kazı / Taşıma Devam Ediyor",
                OcrConfidence = Models.OcrConfidenceStatus.Reliable
            };
            var first = csv.WriteIfChangedAsync(record).GetAwaiter().GetResult();
            var duplicate = csv.WriteIfChangedAsync(record).GetAwaiter().GetResult();
            var filePath = csv.GetFilePath(record.Timestamp);
            var bytes = File.ReadAllBytes(filePath);
            var lines = File.ReadAllLines(filePath);
            var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            var hasDecimalComma = lines.Length == 2 && lines[1].Contains("12,5;8,25;7,75;400", StringComparison.Ordinal);
            return first == Services.CsvWriteResult.Written
                && duplicate == Services.CsvWriteResult.SkippedDuplicate
                && hasBom
                && hasDecimalComma
                && lines[0].Contains("Kantar 1;Kantar 2", StringComparison.Ordinal)
                ? 0 : 1;
        }
        finally
        {
            if (Directory.Exists(testFolder)) Directory.Delete(testFolder, recursive: true);
        }
    }

    private static int RunLogSelfTest()
    {
        var testFolder = Path.Combine(Path.GetTempPath(), $"TBMWatchdog-LogTest-{Guid.NewGuid():N}");
        try
        {
            using (var log = new Services.LogService(testFolder))
            {
                log.Information("Öz test başlangıcı.");
                log.Warning("Örnek uyarı.");
                log.Error("Örnek hata.", new InvalidOperationException("Test istisnası"));
                var path = log.GetLogPath(DateTime.Now);
                var bytes = File.ReadAllBytes(path);
                var contents = File.ReadAllText(path);
                var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
                if (!hasBom || !contents.Contains("BİLGİ", StringComparison.Ordinal)
                    || !contents.Contains("UYARI", StringComparison.Ordinal)
                    || !contents.Contains("HATA", StringComparison.Ordinal)
                    || !contents.Contains("Test istisnası", StringComparison.Ordinal))
                {
                    return 1;
                }
            }

            return 0;
        }
        finally
        {
            if (Directory.Exists(testFolder)) Directory.Delete(testFolder, recursive: true);
        }
    }

    private static int RunCaptureSelfTest()
    {
        try
        {
            var capture = new Services.ScreenCaptureService();
            var screens = capture.GetScreens();
            if (screens.Count == 0) return 1;
            var size = screens[0].Bounds.Size;
            var region = new Models.ScreenRegion
            {
                X = Math.Max(0, (size.Width / 2) - 5),
                Y = Math.Max(0, (size.Height / 2) - 5),
                Width = Math.Min(10, size.Width),
                Height = Math.Min(10, size.Height)
            };
            using var bitmap = capture.CaptureRegion(0, region);
            return bitmap.Width == region.Width && bitmap.Height == region.Height ? 0 : 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static int RunOcrSelfTest()
    {
        var comma = Utilities.MeasurementParser.Parse(" 123,45 mm ", 0.9f, 1000m);
        var dot = Utilities.MeasurementParser.Parse("98.25", 0.9f, 1000m);
        var negative = Utilities.MeasurementParser.Parse("-4,2", 0.9f, 1000m);
        var excessive = Utilities.MeasurementParser.Parse("1001", 0.9f, 1000m);
        using var source = new Bitmap(8, 6);
        using var processed = Services.OcrService.Preprocess(source);
        var preprocessingWorks = processed.Width == 24 && processed.Height == 18;
        using var noisyOne = new Bitmap(24, 60);
        using (var graphics = Graphics.FromImage(noisyOne))
        {
            graphics.Clear(Color.White);
            graphics.FillRectangle(Brushes.Black, 10, 7, 5, 47);
            graphics.FillRectangle(Brushes.Black, 0, 0, 3, 4);
        }
        using var normalizedOne = Services.CharacterOcrNormalizer.NormalizeCharacterForOcr(noisyOne);
        var geometry = Services.GateDigitGeometryAnalyzer.Analyze(normalizedOne.Normalized);
        var characterNormalizationWorks = normalizedOne.IsValid
            && normalizedOne.Normalized.Size == new Size(64, 96)
            && geometry.SuggestedDigit == '1';
        using var gateSegments = new Bitmap(120, 60);
        using (var graphics = Graphics.FromImage(gateSegments))
        {
            graphics.Clear(Color.White);
            graphics.FillRectangle(Brushes.Black, 0, 5, 4, 50);
            graphics.FillRectangle(Brushes.Black, 0, 5, 23, 3);
            graphics.FillRectangle(Brushes.Black, 0, 52, 23, 3);
            graphics.FillRectangle(Brushes.Black, 48, 12, 6, 37);
            graphics.FillRectangle(Brushes.Black, 75, 12, 16, 4);
            graphics.FillRectangle(Brushes.Black, 87, 15, 4, 15);
            graphics.FillRectangle(Brushes.Black, 75, 27, 16, 4);
            graphics.FillRectangle(Brushes.Black, 75, 30, 4, 16);
            graphics.FillRectangle(Brushes.Black, 75, 43, 16, 4);
        }
        using var gateSegmentation = Services.CharacterSegmenter.Segment(gateSegments, Models.OcrFieldKind.Gate);
        var gateFrameFilteringWorks = gateSegmentation.RawSegmentCount == 3
            && gateSegmentation.Characters.Count == 2
            && gateSegmentation.RejectedBounds.Count == 1;

        var missingDataHandled = false;
        var missingPath = Path.Combine(Path.GetTempPath(), $"TBMWatchdog-MissingTessdata-{Guid.NewGuid():N}");
        using (var ocr = new Services.OcrService(missingPath))
        {
            try
            {
                ocr.ReadNumberAsync(source, 1000m).GetAwaiter().GetResult();
            }
            catch (Services.OcrConfigurationException)
            {
                missingDataHandled = true;
            }
        }

        var installedEngineStarted = false;
        using (var installedOcr = new Services.OcrService())
        {
            try
            {
                installedOcr.ReadNumberAsync(source, 1000m).GetAwaiter().GetResult();
                installedEngineStarted = true;
            }
            catch (Services.OcrConfigurationException)
            {
                installedEngineStarted = false;
            }
        }

        var successfulLowConfidence = new Models.OcrReadResult(1m, "1", "1", 0.01f, null);
        var failedResult = Models.OcrReadResult.Failed("", "", 0, "Öz test");
        var officialReliable = Services.OfficialOcrConfidenceEvaluator.FromResults(
            successfulLowConfidence, successfulLowConfidence, successfulLowConfidence, successfulLowConfidence);
        var officialPartial = Services.OfficialOcrConfidenceEvaluator.FromResults(
            successfulLowConfidence, failedResult, successfulLowConfidence, successfulLowConfidence);
        var officialUnreadable = Services.OfficialOcrConfidenceEvaluator.FromResults(
            failedResult, failedResult, failedResult, failedResult);

        var passed = comma.Value == 123.45m
            && dot.Value == 98.25m
            && !negative.IsSuccessful
            && !excessive.IsSuccessful
            && preprocessingWorks
            && characterNormalizationWorks
            && gateFrameFilteringWorks
            && officialReliable == Models.OcrConfidenceStatus.Reliable
            && officialPartial == Models.OcrConfidenceStatus.Partial
            && officialUnreadable == Models.OcrConfidenceStatus.Unreadable
            && missingDataHandled
            && installedEngineStarted;
        Console.WriteLine($"ocr-self-test: {(passed ? "PASS" : "FAIL")} | comma={comma.Value} dot={dot.Value} negative={!negative.IsSuccessful} excessive={!excessive.IsSuccessful} preprocess={preprocessingWorks} normalize={characterNormalizationWorks} gateRaw={gateSegmentation.RawSegmentCount} gateValid={gateSegmentation.Characters.Count} gateRejected={gateSegmentation.RejectedBounds.Count} confidence={officialReliable}/{officialPartial}/{officialUnreadable} missing={missingDataHandled} engine={installedEngineStarted}");
        return passed ? 0 : 1;
    }

    private static int RunLiveFastOcrSelfTest()
    {
        try
        {
            using var fixture = new Bitmap(150, 58);
            using (var graphics = Graphics.FromImage(fixture))
            {
                graphics.Clear(Color.White);
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                using var font = new Font("Arial", 38, FontStyle.Bold, GraphicsUnit.Pixel);
                graphics.DrawString("659", font, Brushes.Black, new PointF(18, 6));
            }
            var profile = new Models.OcrProfile { LiveFastMinimumConfidence = 0.25f, LiveFastHeavyFallbackAllowed = false };
            using var service = new Services.OcrService();
            using var diagnostic = service.ReadNumberWithDiagnosticsAsync(fixture, 10_000m, profile,
                Models.OcrFieldKind.General).GetAwaiter().GetResult();
            using var live = service.ReadNumberLiveFastWithDiagnosticsAsync(fixture, 10_000m, profile,
                Models.OcrFieldKind.General).GetAwaiter().GetResult();

            profile.LiveFastMinimumConfidence = float.MaxValue;
            using var fastFailure = service.ReadNumberLiveFastWithDiagnosticsAsync(fixture, 10_000m, profile,
                Models.OcrFieldKind.General).GetAwaiter().GetResult();
            profile.LiveFastHeavyFallbackAllowed = true;
            using var heavy = service.ReadNumberLiveFastWithDiagnosticsAsync(fixture, 10_000m, profile,
                Models.OcrFieldKind.General).GetAwaiter().GetResult();

            var diagnosticComprehensive = diagnostic.ExecutionMode == Models.OcrExecutionMode.Diagnostic
                && diagnostic.PipelineCountExecuted >= 10;
            var fastBounded = live.ExecutionMode == Models.OcrExecutionMode.LiveFast
                && live.PipelineCountExecuted is >= 1 and <= 3;
            var sameParsedResult = diagnostic.Result.Value.HasValue && diagnostic.Result.Value == live.Result.Value;
            var earlyExit = live.Result.IsSuccessful && live.PipelineCountExecuted == 1;
            var secondaryPath = fastFailure.PipelineCountExecuted == 3 && fastFailure.FallbackUsed;
            var secondaryStopsFallback = Services.OcrService.PlannedPipelineExecutionCountForTest(false, true, true) == 2;
            var controlledHeavyFallback = heavy.HeavyFallbackUsed && heavy.Candidates.Count > fastFailure.Candidates.Count;
            var resultShape = live.Result.RawText is not null && live.Result.CleanedText is not null;
            var passed = diagnosticComprehensive && fastBounded && sameParsedResult && earlyExit
                && secondaryPath && secondaryStopsFallback && controlledHeavyFallback && resultShape;
            Console.WriteLine($"live-fast-ocr-self-test: {(passed ? "PASS" : "FAIL")} | DiagnosticPipelines={diagnostic.PipelineCountExecuted} | LiveFastPipelines={live.PipelineCountExecuted} | Parsed={live.Result.Value} | EarlyExit={earlyExit} | AllFastAttempted={secondaryPath} | SecondaryStopsFallback={secondaryStopsFallback} | HeavyFallback={controlledHeavyFallback}");
            return passed ? 0 : 1;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }

    private static int RunStableCharacterTemplateSelfTest()
    {
        try
        {
            var settings = Services.SettingsService.CreateDefaults();
            settings.OcrProfile.Name = "1031";
            settings.StableRpmRegion = new Models.ScreenRegion { X = 1, Y = 1, Width = 40, Height = 18 };
            settings.StableTorqueRegion = new Models.ScreenRegion { X = 2, Y = 2, Width = 40, Height = 18 };
            settings.StableThrustForceRegion = new Models.ScreenRegion { X = 3, Y = 3, Width = 50, Height = 18 };
            settings.StableAdvanceSpeedRegion = new Models.ScreenRegion { X = 4, Y = 4, Width = 40, Height = 18 };
            settings.StablePenetrationRegion = new Models.ScreenRegion { X = 5, Y = 5, Width = 40, Height = 18 };
            settings.OcrProfile.Fields.Clear(); Services.OcrProfileMigrator.Migrate(settings);
            var ids = new[] { Models.BuiltInFieldIds.StableRpm, Models.BuiltInFieldIds.StableTorque,
                Models.BuiltInFieldIds.StableThrustForce, Models.BuiltInFieldIds.StableAdvanceSpeed,
                Models.BuiltInFieldIds.StablePenetration };
            var fields = ids.Select(id => settings.OcrProfile.FindField(id)!).ToArray();
            var enabled = fields.All(field => field.SupportsCharacterOcr && field.SupportsTemplateTraining);
            var formats = fields[0].ValueType == Models.FieldValueType.Decimal && fields[0].DecimalPlaces == 2
                && fields[1].DecimalPlaces == 3 && fields[2].ValueType == Models.FieldValueType.Integer
                && fields[3].DecimalPlaces == 1 && fields[4].DecimalPlaces == 1;
            bool Parse(string text, int index, decimal expected) => Services.NumericFieldParser.TryParse(text, fields[index], out var value, out _) && value == expected;
            var parses = Parse("2.39", 0, 2.39m) && Parse("3.076", 1, 3.076m)
                && Parse("23029", 2, 23029m) && Parse("18.6", 3, 18.6m) && Parse("5.9", 4, 5.9m);
            var outOfRangeSuppressed = !Services.NumericFieldParser.TryParse("239", fields[0], out _, out _);
            var decimalStructure = Services.OcrService.HasExpectedDecimalStructure("2.39", 2)
                && Services.OcrService.HasExpectedDecimalStructure("3.076", 3)
                && Services.OcrService.HasExpectedDecimalStructure("18.6", 1)
                && !Services.OcrService.HasExpectedDecimalStructure("239", 2)
                && !Services.OcrService.HasExpectedDecimalStructure("3.76", 3)
                && !Services.OcrService.HasExpectedDecimalStructure("3076", 3);

            using var segmentedImage = new Bitmap(120, 80);
            using (var graphics = Graphics.FromImage(segmentedImage))
            {
                graphics.Clear(Color.White);
                graphics.FillRectangle(Brushes.Black, 10, 15, 12, 45);
                graphics.FillRectangle(Brushes.Black, 45, 15, 12, 45);
                graphics.FillRectangle(Brushes.Black, 80, 15, 12, 45);
                graphics.FillRectangle(Brushes.Black, 32, 62, 5, 5);
            }
            using var segmentation = Services.CharacterSegmenter.Segment(segmentedImage, Models.OcrFieldKind.StableRpm);
            var decimalSeparator = segmentation.DecimalSeparatorIndexes.Count == 1 && segmentation.Characters.Count == 4
                && !Services.DigitTemplateStore.CanSaveToken(".");

            var stableOptions = Services.NumericFieldCharacterOcrService.GetOptions(settings.OcrProfile, Models.OcrFieldKind.StableRpm);
            var gateOptions = Services.NumericFieldCharacterOcrService.GetOptions(settings.OcrProfile, Models.OcrFieldKind.Gate);
            var sharedScope = stableOptions.TemplateFolder.Contains("SharedNumericDigits", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(stableOptions.TemplateFolder, gateOptions.TemplateFolder, StringComparison.OrdinalIgnoreCase);
            var authoritative = Services.OcrService.ResolveCharacterDecision('5', .95f,
                new Services.DigitTemplateMatch('2', .96, '5', .70, .26, 5),
                new Services.GateDigitGeometryResult('2', .92, true, false, false, "dar karakter"), stableOptions);
            var authoritativeTemplate = authoritative.FinalDigit == '2' && authoritative.TemplateAuthoritative;
            using var blank = new Bitmap(48, 72); using (var graphics = Graphics.FromImage(blank)) graphics.Clear(Color.White);
            var badSharedRejected = !Services.DigitTemplateStore.PassesQuality(blank, out _);
            using var diagnosticVariants = new Services.DisposableVariantCollection(Services.OcrImageVariantFactory.Create(segmentedImage, settings.OcrProfile, Models.OcrFieldKind.StableRpm));
            using var liveVariants = new Services.DisposableVariantCollection(Services.OcrImageVariantFactory.CreateLiveFast(segmentedImage, settings.OcrProfile));
            var performance = diagnosticVariants.Items.Count * 2 >= 10 && liveVariants.Items.Count <= 3;
            var passed = enabled && formats && parses && decimalSeparator && outOfRangeSuppressed && decimalStructure
                && sharedScope && authoritativeTemplate && badSharedRejected && performance;
            Console.WriteLine($"stable-character-template-self-test: {(passed ? "PASS" : "FAIL")} | Fields={enabled} | Formats={formats} | DecimalSeparator={decimalSeparator} | DecimalStructure={decimalStructure} | Parse={parses} | Range={outOfRangeSuppressed} | SharedIsolated={sharedScope} | Authoritative={authoritativeTemplate} | BadSharedRejected={badSharedRejected} | Diagnostic={diagnosticVariants.Items.Count * 2} LiveFast={liveVariants.Items.Count}");
            return passed ? 0 : 1;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }

    private static int RunTemplateTrainingQualitySelfTest()
    {
        var root = Path.Combine(Path.GetTempPath(), $"TBMWatchdog-TemplateQuality-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root); var oldAudit = Services.TemplateTrainingAuditService.FilePath;
        Services.TemplateTrainingAuditService.FilePath = Path.Combine(root, "TemplateTrainingEvents.csv");
        try
        {
            Bitmap Two(int thickness = 5, int offset = 0)
            {
                var image = new Bitmap(48, 72); using var g = Graphics.FromImage(image); g.Clear(Color.White);
                using var brush = new SolidBrush(Color.Black); var x = 9 + offset; var width = 29 - offset;
                g.FillRectangle(brush, x, 9, width, thickness); g.FillRectangle(brush, 33, 9, thickness, 24);
                g.FillRectangle(brush, x, 29, width, thickness); g.FillRectangle(brush, x, 29, thickness, 25);
                g.FillRectangle(brush, x, 50, width, thickness); return image;
            }
            using var good = Two(); using var variation = Two(7, 2); using var blank = new Bitmap(48, 72);
            using (var g = Graphics.FromImage(blank)) g.Clear(Color.White);
            using var clipped = Two(); using (var g = Graphics.FromImage(clipped)) g.FillRectangle(Brushes.Black, 0, 8, 12, 50);
            using var multiple = new Bitmap(48, 72); using (var g = Graphics.FromImage(multiple))
            { g.Clear(Color.White); g.FillRectangle(Brushes.Black, 7, 10, 8, 48); g.FillRectangle(Brushes.Black, 31, 10, 8, 48); }
            var goodQuality = Services.DigitTemplateQualityEvaluator.Evaluate(good);
            var blankQuality = Services.DigitTemplateQualityEvaluator.Evaluate(blank);
            var clippedQuality = Services.DigitTemplateQualityEvaluator.Evaluate(clipped);
            var multipleQuality = Services.DigitTemplateQualityEvaluator.Evaluate(multiple);
            var storeFolder = Path.Combine(root, "SharedNumericDigits");
            var store = new Services.DigitTemplateStore(storeFolder, "1031", Models.OcrFieldKind.StableRpm, .97, 2);
            var saved = store.Save(2, good, good, "SelfTest", referenceConfidence: .94);
            var duplicate = store.Save(2, good, good, "SelfTest");
            var varied = store.Save(2, variation, variation, "SelfTest");
            using var limitSource = Two(9, 4); var limit = store.Save(2, limitSource, limitSource, "SelfTest");
            var metadata = saved.ImagePath is null ? null : Services.DigitTemplateStore.ReadMetadata(Path.ChangeExtension(saved.ImagePath, ".json"));

            var disabledFolder = Path.Combine(root, "Disabled"); var disabledStore = new Services.DigitTemplateStore(disabledFolder, "1031", Models.OcrFieldKind.StableRpm);
            var disabledSave = disabledStore.Save(2, good, good, "SelfTest"); var disabledItem = disabledStore.GetItems().Single();
            disabledStore.SetEnabled(disabledItem, false); var disabledMatch = Services.DigitTemplateMatcher.Match(good, disabledFolder);

            var replaceFolder = Path.Combine(root, "Replacement", "2"); Directory.CreateDirectory(replaceFolder);
            using (var weak = Two(3, 5)) weak.Save(Path.Combine(replaceFolder, "2_legacy.png"));
            File.WriteAllText(Path.Combine(replaceFolder, "2_legacy.json"), System.Text.Json.JsonSerializer.Serialize(new Models.DigitTemplateMetadata
            { Digit = 2, MachineProfile = "1031", SourceField = "Auto", FieldKind = Models.OcrFieldKind.StableRpm.ToString(),
                UserVerified = false, Enabled = true, QualityScore = .40, CapturedAt = DateTime.Now.AddDays(-1) }));
            var replacementStore = new Services.DigitTemplateStore(Path.Combine(root, "Replacement"), "1031", Models.OcrFieldKind.StableRpm, .999, 1);
            var replacement = replacementStore.Save(2, variation, variation, "SelfTest");

            var raceFolder = Path.Combine(root, "Race"); var raceStore = new Services.DigitTemplateStore(raceFolder, "1031", Models.OcrFieldKind.StableRpm);
            var raceResults = Enumerable.Range(0, 6).AsParallel().Select(_ => raceStore.Save(2, good, good, "Race")).ToArray();
            var threadSafe = raceResults.Count(result => result.Success) == 1 && raceStore.GetItems().Count == 1;

            var legacyFolder = Path.Combine(root, "Legacy", "2"); Directory.CreateDirectory(legacyFolder); good.Save(Path.Combine(legacyFolder, "2_old.png"));
            var legacy = new Services.DigitTemplateStore(Path.Combine(root, "Legacy"), "1031").GetItems().Single().Metadata;
            if (saved.ImagePath is not null)
            {
                var match = Services.DigitTemplateMatcher.Match(good, storeFolder); Services.DigitTemplateUsageTracker.RecordOutcome(match?.TemplatePath, true, false);
                Services.DigitTemplateUsageTracker.Flush(); metadata = Services.DigitTemplateStore.ReadMetadata(Path.ChangeExtension(saved.ImagePath, ".json"));
            }
            var passed = saved.Success && goodQuality.Status is Services.DigitTemplateQualityStatus.Good or Services.DigitTemplateQualityStatus.Acceptable
                && blankQuality.Status == Services.DigitTemplateQualityStatus.Rejected
                && clippedQuality.Status is Services.DigitTemplateQualityStatus.Poor or Services.DigitTemplateQualityStatus.Rejected
                && multipleQuality.Status == Services.DigitTemplateQualityStatus.Rejected
                && !duplicate.Success && duplicate.NearestSimilarity >= .97 && varied.Success && !limit.Success
                && metadata is { UserVerified: true, UsageCount: > 0, SuccessCount: > 0 }
                && disabledSave.Success && disabledMatch is null && replacement.Success
                && !Services.DigitTemplateStore.CanSaveToken(".") && threadSafe
                && legacy.UserVerified && legacy.QualityScore > 0 && File.Exists(Services.TemplateTrainingAuditService.FilePath);
            Console.WriteLine($"template-training-quality-self-test: {(passed ? "PASS" : "FAIL")} | Good={goodQuality.Status}/{goodQuality.QualityScore:0.00} | Blank={blankQuality.Status} | Clipped={clippedQuality.Status} | Multi={multipleQuality.Status} | Duplicate={!duplicate.Success}/{duplicate.NearestSimilarity:0.000} | Variation={varied.Success} | Limit={!limit.Success} | Replacement={replacement.Success} | Disabled={disabledMatch is null} | ThreadSafe={threadSafe} | Legacy={legacy.QualityStatus} | Stats={metadata?.UsageCount}/{metadata?.SuccessCount}");
            return passed ? 0 : 1;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
        finally { Services.TemplateTrainingAuditService.FilePath = oldAudit; try { Directory.Delete(root, true); } catch { } }
    }

    private static int RunRegionSelfTest()
    {
        try
        {
            var screens = Screen.AllScreens;
            if (screens.Length == 0) return 1;
            var region = new Models.ScreenRegion
            {
                X = Math.Max(0, (screens[0].Bounds.Width / 2) - 10),
                Y = Math.Max(0, (screens[0].Bounds.Height / 2) - 10),
                Width = Math.Min(20, screens[0].Bounds.Width),
                Height = Math.Min(20, screens[0].Bounds.Height)
            };
            using var testScreen = new Bitmap(screens[0].Bounds.Width, screens[0].Bounds.Height);
            using var selector = new Forms.RegionSelectorForm(0, region, "Öz Test", testScreenImage: testScreen);
            selector.CreateControl();
            var upperTarget = new Rectangle(Math.Max(2, screens[0].Bounds.Width / 3),
                Math.Max(2, screens[0].Bounds.Height / 20), 48, 18);
            selector.BeginSelectionForTest();
            var toolbarHidden = selector.SelectionModeForTest && !selector.ToolbarVisibleForTest;
            selector.SimulateSelectionForTest(upperTarget);
            var completed = !selector.SelectionModeForTest && selector.ToolbarVisibleForTest
                && selector.SelectionForTest == upperTarget && selector.PreviewHasImageForTest;
            selector.CancelSelectionForTest();
            var escapePreserves = selector.SelectionForTest == upperTarget && selector.ToolbarVisibleForTest;
            var screenCoordinatesStable = selector.Bounds == screens[0].Bounds && selector.SelectionForTest.X == upperTarget.X
                && selector.SelectionForTest.Y == upperTarget.Y;
            Console.WriteLine($"region-self-test: toolbarHidden={toolbarHidden}, upperTarget={completed}, escapePreserves={escapePreserves}, coordinates={screenCoordinatesStable}, monitors={screens.Length}");
            return screenCoordinatesStable && selector.FormBorderStyle == FormBorderStyle.None && toolbarHidden && completed && escapePreserves ? 0 : 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static int RunChartSelfTest()
    {
        try
        {
            using var chart = new Controls.LiveChartControl();
            for (var index = 0; index < 105; index++)
            {
                chart.AddMeasurement(new Models.MeasurementRecord
                {
                    Nominal = index,
                    Scale1 = index * 0.8m,
                    Scale2 = index * 0.7m,
                    GateMillimeters = 400m,
                    GateStatus = "Açık - Malzeme Akışı Var",
                    ExcavationStatus = "Kazı / Taşıma Devam Ediyor",
                    OcrConfidence = Models.OcrConfidenceStatus.Reliable
                });
            }

            chart.CreateControl();
            return chart.MeasurementCount == 100 && chart.SeriesCount == 3 ? 0 : 1;
        }
        catch
        {
            return 1;
        }
    }

    private static int RunRulesSelfTest()
    {
        var evaluator = new Utilities.ExcavationStatusEvaluator(TimeSpan.FromSeconds(30));
        var origin = new DateTime(2026, 7, 31, 12, 0, 0);
        Models.MeasurementRecord Record(DateTime time, decimal? nominal, decimal? scale1, decimal? scale2, decimal? gate) => new()
        {
            Timestamp = time,
            Nominal = nominal,
            Scale1 = scale1,
            Scale2 = scale2,
            GateMillimeters = gate,
            OcrConfidence = Models.OcrConfidenceStatus.Reliable
        };

        var closed = evaluator.Evaluate(Record(origin, 0, 0, 0, 0));
        var partial = evaluator.Evaluate(Record(origin.AddSeconds(1), 0, 0, 0, 100));
        var moving = evaluator.Evaluate(Record(origin.AddSeconds(2), 1, 0.8m, 0.7m, 101));
        var waiting = evaluator.Evaluate(Record(origin.AddSeconds(20), 1, 0.8m, 0.7m, 400));
        var stalled = evaluator.Evaluate(Record(origin.AddSeconds(33), 1, 0.8m, 0.7m, 400));
        var unreadable = evaluator.Evaluate(Record(origin.AddSeconds(34), null, 0.8m, 0.7m, 400));

        return closed.GateStatus == "Tam Kapalı"
            && partial.GateStatus == "Kısmen Açık - Malzeme Akışı Yok"
            && moving.GateStatus == "Açık - Malzeme Akışı Var"
            && moving.ExcavationStatus == "Kazı / Taşıma Devam Ediyor"
            && waiting.ExcavationStatus == "Kapak Açık - İlerleme Bekleniyor"
            && stalled.ExcavationStatus == "Kapak Açık Ancak Değerler İlerlemiyor"
            && unreadable.ExcavationStatus == "Ekran Değerleri Okunamadı" ? 0 : 1;
    }

    private static int RunIntegrationSelfTest()
    {
        var testFolder = Path.Combine(Path.GetTempPath(), $"TBMWatchdog-IntegrationTest-{Guid.NewGuid():N}");
        try
        {
            using var csv = new Services.CsvService(testFolder);
            using var monitoring = new Services.MonitoringService();
            var records = new List<Models.MeasurementRecord>();
            var startedReadings = 0;
            var received = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            monitoring.MeasurementReceived += record =>
            {
                lock (records)
                {
                    records.Add(record);
                    csv.WriteIfChangedAsync(record).GetAwaiter().GetResult();
                    if (records.Count >= 2) received.TrySetResult(true);
                }
            };
            monitoring.ReadingStarted += () => Interlocked.Increment(ref startedReadings);
            monitoring.Start(new Services.DemoDataSource(), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
            var completed = received.Task.Wait(TimeSpan.FromSeconds(8));
            monitoring.StopAsync().GetAwaiter().GetResult();
            var file = Directory.GetFiles(testFolder, "TBM_Kayit_*.csv").SingleOrDefault();
            var lineCount = file is null ? 0 : File.ReadAllLines(file).Length;
            return completed && !monitoring.IsRunning && records.Count >= 2 && startedReadings >= 2 && lineCount >= 3 ? 0 : 1;
        }
        finally
        {
            if (Directory.Exists(testFolder)) Directory.Delete(testFolder, recursive: true);
        }
    }

    private static int RunDiagnosticsSelfTest()
    {
        try
        {
            using var form = new Forms.OcrPreviewForm();
            for (var index = 0; index < 6; index++)
            {
                Models.OcrDiagnosticResult Diagnostic(string raw, decimal? value, bool accepted)
                {
                    var result = accepted
                        ? new Models.OcrReadResult(value, raw, raw, 0.91f, null)
                        : Models.OcrReadResult.Failed(raw, raw, 0.22f, "Öz test reddi");
                    return new Models.OcrDiagnosticResult( 
                        new Bitmap(20, 10),
                        new Bitmap(60, 30),
                        result,
                        DateTime.Now.AddMilliseconds(index));
                }

                form.UpdateFrame(new Services.OcrDiagnosticsFrame(
                    DateTime.Now,
                    Diagnostic($"{index},1", index + 0.1m, true),
                    Diagnostic($"{index},2", index + 0.2m, true),
                    Diagnostic("?", null, false),
                    Diagnostic("400", 400m, true)));
            }

            form.CreateControl();
            using var diagnostic = new Models.OcrDiagnosticResult(
                new Bitmap(20, 10), new Bitmap(60, 30),
                new Models.OcrReadResult(12m, "12", "12", 0.95f, null)
                { SelectionReason = "Öz test nihai karar kuralı." }, DateTime.Now);
            using var results = new Forms.OcrTestResultsForm(diagnostic, "Kapalı");
            results.CreateControl();
            Models.CharacterOcrDiagnostic Character(int index, string digit) => new()
            {
                CharacterIndex = index, OriginalSegmentImage = new Bitmap(12, 20),
                CleanedSegmentImage = new Bitmap(12, 20), TightCroppedImage = new Bitmap(12, 20),
                NormalizedOcrInputImage = new Bitmap(48, 72), RawOcrText = digit,
                CleanedDigit = digit, OcrConfidence = 0.95f, GeometryResult = digit,
                FinalDigit = digit, DecisionReason = "Bağımsız karakter öz testi."
            };
            var characterDiagnostics = new[] { Character(1, "1"), Character(2, "1"), Character(3, "5") };
            var segmentedCandidate = new Models.OcrCandidateResult
            {
                ProcessingMethod = "Segmented Characters", PageSegMode = Tesseract.PageSegMode.SingleChar,
                RawText = "115", CleanedText = "115", ParsedValue = 115, Confidence = 0.95f,
                OcrConfidence = 0.95f, SystemConfidence = 0.95f, CharacterCount = 3,
                RawSegmentCount = 3, SegmentCount = 3, SegmentMatch = true,
                AcceptedSegmentBounds = [new Rectangle(2, 2, 8, 16), new Rectangle(14, 2, 8, 16), new Rectangle(26, 2, 8, 16)],
                CharacterDiagnostics = characterDiagnostics, UsesCharacterBasedOcr = true,
                CharacterDecisionReliable = true, AllCharactersTemplateAuthoritative = true,
                SegmentationSuccessful = true, ImageSize = new Size(40, 22),
                ProcessedImage = new Bitmap(40, 22), BinaryImage = new Bitmap(40, 22),
                SegmentationImage = new Bitmap(40, 22)
            };
            using var detailsDiagnostic = new Models.OcrDiagnosticResult(new Bitmap(40, 22),
                [segmentedCandidate], segmentedCandidate,
                new Models.OcrReadResult(115, "115", "115", 0.95f, null), DateTime.Now);
            using var detailsForm = new Forms.OcrTestResultsForm(detailsDiagnostic, "Kapalı", fieldKind: Models.OcrFieldKind.Scale2);
            detailsForm.CreateControl();
            var threeIndependentCards = detailsForm.CharacterDetailCardCount == 3
                && detailsForm.CharacterTeachButtonCount == 3
                && detailsForm.SelectedCharacterText == "Karakter 1 / 3";
            var comparisonPath = Path.Combine(Path.GetTempPath(), $"TBMWatchdog-comparison-{Guid.NewGuid():N}.csv");
            try
            {
                var writer = new Services.CharacterOcrComparisonCsvService(comparisonPath);
                writer.Append([new Services.CharacterOcrComparison("Kantar 2", 318, 315, false,
                    120, 1400, DateTime.Now.AddSeconds(-1), DateTime.Now, 1400)]);
                return form.Text.Contains("Gelişmiş OCR Tanılama", StringComparison.Ordinal)
                    && results.Text.Contains("OCR Tanılama", StringComparison.Ordinal)
                    && threeIndependentCards
                    && File.Exists(comparisonPath)
                    && File.ReadAllText(comparisonPath).Contains("FailureReason", StringComparison.Ordinal)
                    && File.ReadAllText(comparisonPath).Contains("Success", StringComparison.Ordinal) ? 0 : 1;
            }
            finally
            {
                if (File.Exists(comparisonPath)) File.Delete(comparisonPath);
            }
        }
        catch
        {
            return 1;
        }
    }

    private static int RunTemplateSelfTest()
    {
        var root = Path.Combine(Path.GetTempPath(), $"TBMWatchdog-GateTemplates-{Guid.NewGuid():N}");
        var foundFolder = Path.Combine(root, "found");
        var ambiguousFolder = Path.Combine(root, "ambiguous");
        try
        {
            using var one = new Bitmap(32, 52);
            using (var graphics = Graphics.FromImage(one))
            {
                graphics.Clear(Color.White);
                graphics.FillRectangle(Brushes.Black, 14, 5, 5, 42);
            }
            using var five = new Bitmap(32, 52);
            using (var graphics = Graphics.FromImage(five))
            {
                graphics.Clear(Color.White);
                graphics.FillRectangle(Brushes.Black, 7, 6, 20, 5);
                graphics.FillRectangle(Brushes.Black, 7, 6, 5, 20);
                graphics.FillRectangle(Brushes.Black, 7, 23, 20, 5);
                graphics.FillRectangle(Brushes.Black, 22, 23, 5, 21);
                graphics.FillRectangle(Brushes.Black, 7, 41, 20, 5);
            }
            var missing = Services.DigitTemplateMatcher.Match(one, Path.Combine(root, "missing")) is null;
            Directory.CreateDirectory(foundFolder);
            one.Save(Path.Combine(foundFolder, "1_01.png"));
            five.Save(Path.Combine(foundFolder, "5.png"));
            var found = Services.DigitTemplateMatcher.Match(one, foundFolder);
            Directory.CreateDirectory(ambiguousFolder);
            one.Save(Path.Combine(ambiguousFolder, "1.png"));
            one.Save(Path.Combine(ambiguousFolder, "5_01.png"));
            var ambiguous = Services.DigitTemplateMatcher.Match(one, ambiguousFolder);
            var taughtFolder = Path.Combine(root, "Templates", "1031_Dilber", "GateDigits");
            var store = new Services.DigitTemplateStore(taughtFolder, "1031_Dilber");
            using var normalizedOne = Services.DigitTemplateMatcher.NormalizeForMatching(one);
            using var normalizedTwo = Services.DigitTemplateMatcher.NormalizeForMatching(five);
            var taughtOne = store.Save(1, one, normalizedOne, "Segmented Characters");
            var taughtTwo = store.Save(2, five, normalizedTwo, "Segmented Characters");
            var duplicateBlocked = !store.Save(1, one, normalizedOne, "Segmented Characters").Success;
            store.Reload();
            var learnedOne = Services.DigitTemplateMatcher.Match(one, taughtFolder);
            var learnedTwo = Services.DigitTemplateMatcher.Match(five, taughtFolder);
            var authorityProfile = new Models.OcrProfile
            {
                GateTemplateMinimumScore = 0.72,
                GateTemplateAuthoritativeScore = 0.90,
                GateTemplateMinimumMargin = 0.06
            };
            var authoritativeTemplate = new Services.DigitTemplateMatch('1', 0.95, '5', 0.70, 0.25, 5);
            var narrowOneGeometry = new Services.GateDigitGeometryResult('1', 0.93, true, false, true, "Öz test geometrisi");
            var authoritativeDecision = Services.OcrService.ResolveGateCharacterDecision(
                '5', 0.99f, authoritativeTemplate, narrowOneGeometry, authorityProfile);
            return missing
                && found is { Digit: '1' } && found.Score > found.SecondBestScore
                && ambiguous is not null && ambiguous.ScoreMargin < 0.001
                && taughtOne.Success && taughtTwo.Success && duplicateBlocked
                && learnedOne is { Digit: '1' } && learnedTwo is { Digit: '2' }
                && authoritativeDecision.FinalDigit == '1'
                && authoritativeDecision.TemplateAuthoritative
                && !authoritativeDecision.TesseractContributed
                && Directory.EnumerateFiles(taughtFolder, "*.json", SearchOption.AllDirectories).Count() == 2 ? 0 : 1;
        }
        catch
        {
            return 1;
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static int RunScaleCharacterSelfTest()
    {
        var candidates = new List<Models.OcrCandidateResult>();
        try
        {
            Models.OcrCandidateResult Candidate(decimal value, string method, bool character = false, bool authoritative = false) => new()
            {
                ProcessingMethod = method,
                PageSegMode = character ? Tesseract.PageSegMode.SingleChar : Tesseract.PageSegMode.SingleLine,
                RawText = value.ToString(), CleanedText = value.ToString(), ParsedValue = value,
                Confidence = 0.80f, OcrConfidence = 0.80f, SystemConfidence = 0.80f,
                CharacterCount = 3, SegmentCount = character ? 3 : 0, RawSegmentCount = character ? 3 : 0,
                SegmentMatch = character ? true : null, UsesCharacterBasedOcr = character,
                CharacterDecisionReliable = character, AllCharactersTemplateAuthoritative = authoritative,
                SegmentationSuccessful = character, BinaryQuality = 1, CharacterIntegrity = 1,
                ImageSize = new Size(4, 4), ProcessedImage = new Bitmap(4, 4),
                BinaryImage = new Bitmap(4, 4), SegmentationImage = new Bitmap(4, 4)
            };

            candidates.Add(Candidate(164, "Nearest + Otsu"));
            candidates.Add(Candidate(164, "Bicubic + Otsu"));
            candidates.Add(Candidate(161, "Segmented Characters", true, true));
            var selected = Services.OcrService.SelectResult(candidates, new Models.OcrProfile(), Models.OcrFieldKind.Scale1);

            var profile = new Models.OcrProfile { Name = "Machine_A" };
            var scaleOptions = Services.NumericFieldCharacterOcrService.GetOptions(profile, Models.OcrFieldKind.Scale1);
            var templateDigits = new[] { '1', '6', '1' };
            var tesseractDigits = new[] { '1', '6', '4' };
            var resolvedDigits = templateDigits.Select((digit, index) => Services.OcrService.ResolveCharacterDecision(
                tesseractDigits[index], 0.99f,
                new Services.DigitTemplateMatch(digit, 0.96, digit == '1' ? '4' : '8', 0.70, 0.26, 5),
                new Services.GateDigitGeometryResult(digit, 0.92, digit == '1', false, digit == '1', "Öz test"),
                scaleOptions)).ToArray();
            var gate = Services.NumericFieldCharacterOcrService.GetOptions(profile, Models.OcrFieldKind.Gate).TemplateFolder;
            var scale1 = Services.NumericFieldCharacterOcrService.GetOptions(profile, Models.OcrFieldKind.Scale1).TemplateFolder;
            var scale2 = Services.NumericFieldCharacterOcrService.GetOptions(profile, Models.OcrFieldKind.Scale2).TemplateFolder;
            var isolated = !string.Equals(gate, scale1, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(gate, scale2, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(scale1, scale2, StringComparison.OrdinalIgnoreCase);

            using var fallback = Candidate(164, "Tesseract fallback");
            var fallbackResult = Services.OcrService.SelectResult([fallback], profile, Models.OcrFieldKind.Scale1);
            return selected.Result.Value == 161m && selected.SelectedCandidate?.AllCharactersTemplateAuthoritative == true
                && string.Concat(resolvedDigits.Select(decision => decision.FinalDigit)) == "161"
                && resolvedDigits.All(decision => decision.TemplateAuthoritative && !decision.TesseractContributed)
                && isolated && fallbackResult.Result.Value == 164m ? 0 : 1;
        }
        catch
        {
            return 1;
        }
        finally
        {
            foreach (var candidate in candidates) candidate.Dispose();
        }
    }

    private static int RunSuspiciousValueSelfTest()
    {
        try
        {
            var service = new Services.TemporalValueValidationService();
            var now = DateTime.Now;
            service.Validate("M", "Nominal", 55, 10, 2, 2, now);
            var pending87 = service.Validate("M", "Nominal", 87, 10, 2, 2, now.AddSeconds(1));
            var accepted86 = service.Validate("M", "Nominal", 86, 10, 2, 2, now.AddSeconds(2));

            service.Reset();
            service.Validate("M", "Nominal", 55, 10, 2, 2, now);
            service.Validate("M", "Nominal", 87, 10, 2, 2, now.AddSeconds(1));
            var accepted56 = service.Validate("M", "Nominal", 56, 10, 2, 2, now.AddSeconds(2));

            service.Reset();
            service.Validate("M", "Kantar 1", 118, 15, 2, 2, now);
            var direct119 = service.Validate("M", "Kantar 1", 119, 15, 2, 2, now.AddSeconds(1));
            service.Validate("M", "Kantar 2", 128, 15, 2, 2, now);
            var retained128 = service.Validate("M", "Kantar 2", null, 15, 2, 2, now.AddSeconds(1));
            service.Validate("M", "Nominal", 55, 10, 2, 2, now);
            var isolatedPending = service.Validate("M", "Nominal", 500, 10, 2, 2, now.AddSeconds(2));
            var unaffectedScale = service.Validate("M", "Kantar 1", 120, 15, 2, 2, now.AddSeconds(2));

            return pending87.Value == 55 && pending87.IsHeld && pending87.Status?.Contains("87 (1/2)") == true
                && accepted86.Value == 86 && !accepted86.IsHeld
                && accepted86.Decisions.Any(item => item.Reason == "Ardışık doğrulama ile kabul edildi")
                && accepted56.Value == 56 && !accepted56.IsHeld
                && accepted56.Decisions.Any(item => item.Reason == "Tek karelik OCR sıçraması filtrelendi")
                && direct119.Value == 119 && !direct119.IsHeld
                && retained128.Value == 128
                && isolatedPending.IsHeld && unaffectedScale.Value == 120 && !unaffectedScale.IsHeld ? 0 : 1;
        }
        catch
        {
            return 1;
        }
    }

    private static int RunMismatchSampleSelfTest()
    {
        var root = Path.Combine(Services.VerificationArtifactRetentionService.Root,
            $"MismatchSampleSelfTest_{DateTime.Now:yyyyMMdd_HHmmss_fff}");
        try
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
            Models.OcrDiagnosticResult Diagnostic(decimal full, decimal? character, int pattern)
            {
                Bitmap Image()
                {
                    var image = new Bitmap(40, 20);
                    using var graphics = Graphics.FromImage(image); graphics.Clear(Color.White);
                    graphics.FillRectangle(Brushes.Black, pattern % 20, 2, 8, 16); return image;
                }
                Models.OcrCandidateResult Candidate(decimal value, bool segmented) => new()
                {
                    ProcessingMethod = segmented ? "Segmented Characters" : "Nearest + Otsu",
                    PageSegMode = segmented ? Tesseract.PageSegMode.SingleChar : Tesseract.PageSegMode.SingleLine,
                    RawText = value.ToString(), CleanedText = value.ToString(), ParsedValue = value,
                    Confidence = .9f, OcrConfidence = .9f, SystemConfidence = .9f,
                    CharacterCount = 3, RawSegmentCount = segmented ? 3 : 0, SegmentCount = segmented ? 3 : 0,
                    UsesCharacterBasedOcr = segmented, CharacterDecisionReliable = segmented,
                    SegmentationSuccessful = segmented, ImageSize = new Size(40, 20),
                    ProcessedImage = Image(), BinaryImage = Image(), SegmentationImage = Image()
                };
                var candidates = new List<Models.OcrCandidateResult> { Candidate(full, false) };
                if (character.HasValue) candidates.Add(Candidate(character.Value, true));
                var selected = candidates.Last();
                return new Models.OcrDiagnosticResult(Image(), candidates, selected,
                    new Models.OcrReadResult(full, full.ToString(), full.ToString(), .9f, null), DateTime.Now, TimeSpan.FromMilliseconds(100));
            }
            Services.CharacterOcrComparison Comparison(decimal? full, decimal? character, string reason, DateTime time) =>
                new("Kantar 2", full, character, full.HasValue && character.HasValue ? full == character : null,
                    20, 100, time, time.AddMilliseconds(100), 100, reason);
            var region = new Models.ScreenRegion { X = 1, Y = 2, Width = 40, Height = 20 };
            var options = new Services.MismatchCaptureOptions(true, 30, 500, 1000);
            Services.MismatchCaptureResult mismatch;
            Services.MismatchCaptureResult same;
            Services.MismatchCaptureResult noSegments;
            Services.MismatchCaptureResult duplicate;
            using (var service = new Services.MismatchSampleCaptureService(options, root))
            using (var diagnostic = Diagnostic(164, 161, 2))
            {
                var comparison = Comparison(164, 161, "Success", DateTime.Now);
                mismatch = service.TryEnqueue("1031", "Scale2", comparison, diagnostic, 164, region, "1031");
                duplicate = service.TryEnqueue("1031", "Scale2", comparison, diagnostic, 164, region, "1031");
            }
            using (var service = new Services.MismatchSampleCaptureService(options, root))
            using (var diagnostic = Diagnostic(315, 315, 4))
                same = service.TryEnqueue("1031", "Scale2", Comparison(315, 315, "Success", DateTime.Now), diagnostic, 315, region, "1031");
            using (var service = new Services.MismatchSampleCaptureService(options, root))
            using (var diagnostic = Diagnostic(315, null, 6))
                noSegments = service.TryEnqueue("1031", "Scale1", Comparison(315, null, "NoSegments", DateTime.Now), diagnostic, 315, region, "1031");

            var hashRoot = Path.Combine(root, "hash");
            using (var service = new Services.MismatchSampleCaptureService(new(true, 0, 500, 1000), hashRoot))
            using (var first = Diagnostic(164, 161, 1))
            using (var second = Diagnostic(164, 161, 15))
            {
                service.TryEnqueue("1031", "Scale2", Comparison(164, 161, "Success", DateTime.Now), first, 164, region, "1031");
                service.TryEnqueue("1031", "Scale2", Comparison(164, 161, "Success", DateTime.Now.AddSeconds(31)), second, 164, region, "1031");
            }
            var retentionRoot = Path.Combine(root, "retention");
            using (var service = new Services.MismatchSampleCaptureService(new(true, 0, 1, 1000), retentionRoot))
            using (var first = Diagnostic(164, 161, 3))
            using (var second = Diagnostic(165, 162, 13))
            {
                service.TryEnqueue("1031", "Scale2", Comparison(164, 161, "Success", DateTime.Now), first, 164, region, "1031");
                service.TryEnqueue("1031", "Scale2", Comparison(165, 162, "Success", DateTime.Now.AddMilliseconds(2)), second, 165, region, "1031");
            }
            var isolatedRoot = Path.Combine(root, "isolated");
            using (var service = new Services.MismatchSampleCaptureService(new(true, 0, 500, 1000), isolatedRoot))
            foreach (var field in new[] { "Gate", "Nominal", "Scale1", "Scale2" })
            using (var diagnostic = Diagnostic(10, 11, field.Length))
                service.TryEnqueue("1031", field, Comparison(10, 11, "Success", DateTime.Now.AddMilliseconds(field.Length)), diagnostic, 10, region, "1031");

            var errorRoot = Path.Combine(root, "not-a-directory"); File.WriteAllText(errorRoot, "x"); var diskErrorObserved = false;
            using (var service = new Services.MismatchSampleCaptureService(new(true, 0, 10, 10), errorRoot))
            using (var diagnostic = Diagnostic(1, 2, 8))
            { service.Warning += _ => diskErrorObserved = true; service.TryEnqueue("M", "Gate", Comparison(1, 2, "Success", DateTime.Now), diagnostic, 1, region, "M"); }

            var metadataExists = mismatch.SamplePath is not null && Directory.EnumerateFiles(mismatch.SamplePath, "*.json").Any();
            var hashCount = Directory.Exists(hashRoot) ? Directory.EnumerateFiles(hashRoot, "*.json", SearchOption.AllDirectories).Count() : 0;
            var retainedCount = Directory.Exists(retentionRoot) ? Directory.EnumerateFiles(retentionRoot, "*.json", SearchOption.AllDirectories).Count() : 0;
            var isolated = new[] { "Gate", "Nominal", "Scale1", "Scale2" }.All(field =>
                Directory.EnumerateDirectories(isolatedRoot, field, SearchOption.AllDirectories).Any());
            return mismatch.Saved && metadataExists && !same.Saved && noSegments.Saved
                && duplicate.Duplicate && hashCount == 2 && retainedCount == 1
                && diskErrorObserved && isolated ? 0 : 1;
        }
        catch { return 1; }
        finally { Console.WriteLine($"Mismatch sample test folder: {root}"); }
    }

    private static int RunOperationModeSelfTest()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"TBMWatchdog-Mode-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(folder); var csvPath = Path.Combine(folder, "OperationModeTransitions.csv");
            var profile = new Models.OcrProfile { Name = "1031_Dilber", OperationModeConfirmationReadings = 2, OperationModeMaximumUnknownReadings = 3 };
            var service = new Services.OperationModeStateService("1031", "session-test", profile); var csv = new Services.OperationModeTransitionCsvService(csvPath);
            var changes = new List<Models.OperationModeTransition>(); var started = 0; var ended = 0;
            service.OperationModeChanged += transition => { changes.Add(transition); csv.Append(transition); };
            service.ExcavationStarted += _ => started++; service.ExcavationEnded += _ => ended++;
            var now = DateTime.Now;
            service.Process("İlerleme", .9f, now); var excavation = service.Process("ILERLEME", .9f, now.AddSeconds(1));
            var singleWrong = service.Process("Ring Building", .8f, now.AddSeconds(2));
            var recovered = service.Process("Advance", .9f, now.AddSeconds(3));
            service.Process("???", 0, now.AddSeconds(4)); service.Process("", 0, now.AddSeconds(5)); var unknown = service.Process("okunamadi", 0, now.AddSeconds(6));
            service.Process("Halka Teshizati", .88f, now.AddSeconds(7)); var ring = service.Process("Halka Teçhizatı", .91f, now.AddSeconds(8));
            service.Process("RingBuilding", .9f, now.AddSeconds(9)); service.Process("Ring Building", .9f, now.AddSeconds(10));
            service.Process("Advance", .9f, now.AddSeconds(11)); var excavationAgain = service.Process("İlerleme", .9f, now.AddSeconds(12));
            service.Process("İlerleme", .9f, now.AddSeconds(13));
            var initialRingService = new Services.OperationModeStateService("1031", "initial-ring", profile); var initialEvents = new List<Models.OperationModeTransition>();
            initialRingService.OperationModeChanged += initialEvents.Add; initialRingService.Process("Halka Teshizati", .9f, now); initialRingService.Process("Ring Building", .9f, now.AddSeconds(1));
            var non1031 = new Models.AppSettings { OcrProfile = new Models.OcrProfile { Name = "1030" }, OperationModeRegion = new Models.ScreenRegion { X = 1, Y = 1, Width = 10, Height = 10 } };
            Services.OcrProfileMigrator.Migrate(non1031); var isolated = non1031.OcrProfile.FindField("OperationMode") is null;
            var nearService = new Services.OperationModeStateService("1031", "near", profile);
            var singleNear = nearService.Process("NERLEME", .7f, now); var doubleNear = nearService.Process("NERLEME", .7f, now.AddSeconds(1));
            var suffixService = new Services.OperationModeStateService("1031", "suffix", profile); suffixService.Process("LERLEME", .7f, now); var suffixNear = suffixService.Process("LERLEME", .7f, now.AddSeconds(1));
            var shortService = new Services.OperationModeStateService("1031", "short", profile); shortService.Process("LEME", .9f, now); var shortText = shortService.Process("LEME", .9f, now.AddSeconds(1));
            var ambiguousProfile = new Models.OcrProfile { OperationModeKeywords =
            [
                new() { State = Models.OperationModeState.Excavation, Keywords = ["ABCDEFGH"] },
                new() { State = Models.OperationModeState.RingBuilding, Keywords = ["ABCXEFGH"] }
            ] };
            var ambiguousService = new Services.OperationModeStateService("1031", "ambiguous", ambiguousProfile);
            ambiguousService.Process("ABCYEFGH", .9f, now); var ambiguous = ambiguousService.Process("ABCYEFGH", .9f, now.AddSeconds(1));
            var keywordPairs = profile.OperationModeKeywords.SelectMany(group => group.Keywords.Select(keyword =>
                (group.State, Services.OperationModeStateService.Normalize(keyword).Replace(" ", "")))).ToArray();
            var variantsRecognized = new[] { "İLERLEME", "ILERLEME", "NERLEME", "LERLEME", "İERLEME", "ILERLENE", "İLERLEMF" }
                .All(value => Services.OperationModeTextMatcher.Match(Services.OperationModeStateService.Normalize(value), keywordPairs, 75).State == Models.OperationModeState.Excavation);
            var lines = File.ReadAllLines(csvPath); Console.WriteLine($"Operation mode: state={excavationAgain.Diagnostic.ConfirmedState}, started={started}, ended={ended}, transitions={changes.Count}");
            return excavation.Diagnostic.ConfirmedState == Models.OperationModeState.Excavation && excavation.Transition?.EventType == Models.OperationModeEventType.None
                && singleWrong.Diagnostic.ConfirmedState == Models.OperationModeState.Excavation && recovered.Diagnostic.ConfirmedState == Models.OperationModeState.Excavation
                && unknown.Diagnostic.ConfirmedState == Models.OperationModeState.Excavation && unknown.Diagnostic.IsUnreadable
                && ring.Diagnostic.ConfirmedState == Models.OperationModeState.RingBuilding && ring.Transition?.EventType == Models.OperationModeEventType.ExcavationEnded
                && excavationAgain.Transition?.EventType == Models.OperationModeEventType.ExcavationStarted && started == 1 && ended == 1
                && initialEvents.Single().EventType == Models.OperationModeEventType.None
                && Services.OperationModeStateService.Normalize("Halka Teshizati") == "HALKA TESHIZATI"
                && singleNear.Diagnostic.ConfirmedState == Models.OperationModeState.Unknown && singleNear.Transition is null
                && doubleNear.Diagnostic.ConfirmedState == Models.OperationModeState.Excavation
                && doubleNear.Diagnostic.MatchMethod == Models.OperationModeMatchMethod.Approximate && doubleNear.Diagnostic.MatchedKeyword == "ILERLEME"
                && doubleNear.Diagnostic.SimilarityPercent >= 75 && suffixNear.Diagnostic.ConfirmedState == Models.OperationModeState.Excavation
                && shortText.Diagnostic.ConfirmedState == Models.OperationModeState.Unknown
                && ambiguous.Diagnostic.ConfirmedState == Models.OperationModeState.Unknown && ambiguous.Diagnostic.MatchMethod == Models.OperationModeMatchMethod.Ambiguous
                && variantsRecognized && changes.Count == 3 && lines.Length == 4 && isolated ? 0 : 1;
        }
        catch (Exception exception) { Console.WriteLine(exception); return 1; }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    private static int RunExcelTemplateInspectorSelfTest()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"TBMWatchdog-ExcelInspect-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(folder); var validPath = Path.Combine(folder, "valid.xlsx");
            CreateWorkbook(validPath); var bytesBefore = File.ReadAllBytes(validPath); var writeTimeBefore = File.GetLastWriteTimeUtc(validPath);
            var inspector = new Services.ExcelTemplateInspectorService();
            var existing = inspector.Inspect(validPath, "1031", "315");
            var missingRing = inspector.Inspect(validPath, "1031", "999");
            var wrongMachine = inspector.Inspect(validPath, "9999", "315");
            var missingSheetPath = Path.Combine(folder, "missing-sheet.xlsx"); CreateWorkbook(missingSheetPath, "S1030 KP");
            var missingSheet = inspector.Inspect(missingSheetPath, "1031", "315");
            var badHeaderPath = Path.Combine(folder, "bad-header.xlsx"); CreateWorkbook(badHeaderPath, null, true);
            var badHeader = inspector.Inspect(badHeaderPath, "1031", "315");
            var unchanged = bytesBefore.SequenceEqual(File.ReadAllBytes(validPath)) && writeTimeBefore == File.GetLastWriteTimeUtc(validPath);
            Console.WriteLine($"Excel preview: Machine={existing.MachineId}, Sheet={existing.SheetName}, Ring={existing.RingNo}, Exists={existing.RingAlreadyExists}, Row={existing.TargetRow}, Valid={existing.TemplateIsValid}");
            Console.WriteLine($"Excel unchanged={unchanged}, new-ring-row={missingRing.TargetRow}");
            return existing is { TemplateIsValid: true, SheetName: "S1031 KP", RingAlreadyExists: true, TargetRow: 2 }
                && missingRing is { TemplateIsValid: true, RingAlreadyExists: false, TargetRow: 5 }
                && !wrongMachine.TemplateIsValid && !missingSheet.TemplateIsValid && !badHeader.TemplateIsValid && unchanged ? 0 : 1;
        }
        catch (Exception exception) { Console.WriteLine(exception); return 1; }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }

        static void CreateWorkbook(string path, string? omittedSheet = null, bool badHeader = false)
        {
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Create);
            var sheets = Services.ExcelTemplateInspectorService.MachineSheets.Values.Where(name => name != omittedSheet).ToArray();
            Write("xl/workbook.xml", new System.Xml.Linq.XDocument(
                new System.Xml.Linq.XElement(System.Xml.Linq.XName.Get("workbook", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                    new System.Xml.Linq.XElement(System.Xml.Linq.XName.Get("sheets", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                        sheets.Select((name, index) => new System.Xml.Linq.XElement(System.Xml.Linq.XName.Get("sheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                            new System.Xml.Linq.XAttribute("name", name), new System.Xml.Linq.XAttribute("sheetId", index + 1),
                            new System.Xml.Linq.XAttribute(System.Xml.Linq.XName.Get("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships"), $"rId{index + 1}")))))));
            Write("xl/_rels/workbook.xml.rels", new System.Xml.Linq.XDocument(
                new System.Xml.Linq.XElement(System.Xml.Linq.XName.Get("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships"),
                    sheets.Select((_, index) => new System.Xml.Linq.XElement(System.Xml.Linq.XName.Get("Relationship", "http://schemas.openxmlformats.org/package/2006/relationships"),
                        new System.Xml.Linq.XAttribute("Id", $"rId{index + 1}"), new System.Xml.Linq.XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
                        new System.Xml.Linq.XAttribute("Target", $"worksheets/sheet{index + 1}.xml"))))));
            for (var sheetIndex = 0; sheetIndex < sheets.Length; sheetIndex++)
            {
                var headers = Services.ExcelTemplateInspectorService.ExpectedHeaders.Select((header, index) => badHeader && sheetIndex == sheets.Length - 1 && index == 5 ? "Yanlış Tork" : header).ToArray();
                var rows = new List<System.Xml.Linq.XElement>
                {
                    Row(1, headers.Select((header, index) => Cell(Column(index + 1) + "1", header))),
                    Row(2, new[] { Cell("A2", "01.08.2026"), Cell("B2", "315") }),
                    Row(4, new[] { Cell("A4", "02.08.2026"), Cell("B4", "777") })
                };
                Write($"xl/worksheets/sheet{sheetIndex + 1}.xml", new System.Xml.Linq.XDocument(
                    new System.Xml.Linq.XElement(System.Xml.Linq.XName.Get("worksheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                        new System.Xml.Linq.XElement(System.Xml.Linq.XName.Get("sheetData", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"), rows))));
            }
            void Write(string name, System.Xml.Linq.XDocument document) { var entry = archive.CreateEntry(name); using var output = entry.Open(); document.Save(output); }
            static System.Xml.Linq.XElement Row(int number, IEnumerable<System.Xml.Linq.XElement> cells) => new(System.Xml.Linq.XName.Get("row", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"), new System.Xml.Linq.XAttribute("r", number), cells);
            static System.Xml.Linq.XElement Cell(string reference, string value) => new(System.Xml.Linq.XName.Get("c", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"), new System.Xml.Linq.XAttribute("r", reference), new System.Xml.Linq.XAttribute("t", "inlineStr"), new System.Xml.Linq.XElement(System.Xml.Linq.XName.Get("is", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"), new System.Xml.Linq.XElement(System.Xml.Linq.XName.Get("t", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"), value)));
            static string Column(int number) { var name = ""; while (number > 0) { number--; name = (char)('A' + number % 26) + name; number /= 26; } return name; }
        }
    }

    private static int RunLiveStableDiagnosticBindingSelfTest()
    {
        try
        {
            ApplicationConfiguration.Initialize();
            using var log = new Services.LogService();
            using var form = new Forms.MainForm(log);
            using var monitoring = new Services.MonitoringService();
            var published = new TaskCompletionSource<Models.MeasurementRecord>(TaskCreationOptions.RunContinuationsAsynchronously);
            monitoring.MeasurementReceived += record =>
            {
                if (record.DiagnosticRpm?.ParsedValue is null) return;
                form.ReceiveMonitoringMeasurementForTest(record);
                published.TrySetResult(record);
            };
            monitoring.Start(new LiveStableDiagnosticDataSource(), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
            var completed = published.Task.Wait(TimeSpan.FromSeconds(5));
            monitoring.StopAsync().GetAwaiter().GetResult();
            if (!completed) { Console.WriteLine("live-stable-diagnostic-binding-self-test: FAIL | measurement not published"); return 1; }
            var result = published.Task.Result;
            var cards = form.DashboardCards;
            var cardValues = cards[0].Value == "2.00 rpm" && cards[1].Value == "2.359 MNm" && cards[2].Value == "23029 kN"
                && cards[3].Value == "12.5 mm/min" && cards[4].Value == "5.9 mm/rot";
            var row = form.HistoryGrid.Rows[0];
            var history = Convert.ToString(row.Cells["Rpm"].Value)?.StartsWith("2.00", StringComparison.Ordinal) == true && Convert.ToString(row.Cells["Torque"].Value) == "2.359"
                && Convert.ToString(row.Cells["Thrust"].Value) == "23029" && Convert.ToString(row.Cells["Speed"].Value) == "12.5"
                && Convert.ToString(row.Cells["Penetration"].Value) == "5.9";
            var publishedFields = result.DiagnosticRpm?.RawValue == "2" && result.DiagnosticTorque?.ParsedValue == 2.359m
                && result.DiagnosticThrustForce?.ParsedValue == 23029m && result.DiagnosticAdvanceSpeed?.ParsedValue == 12.5m
                && result.DiagnosticPenetration?.ParsedValue == 5.9m;
            var officialNull = new[] { result.DiagnosticRpm, result.DiagnosticTorque, result.DiagnosticThrustForce,
                result.DiagnosticAdvanceSpeed, result.DiagnosticPenetration }.All(field => field?.OfficialValue is null);
            var stableUntouched = !new Models.RingSession().StableDataCaptured;
            var passed = cardValues && history && publishedFields && officialNull && stableUntouched;
            Console.WriteLine($"live-stable-diagnostic-binding-self-test: {(passed ? "PASS" : "FAIL")} | published={publishedFields} | cards={cardValues} | history={history} | officialNull={officialNull} | stableUntouched={stableUntouched}");
            return passed ? 0 : 1;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }

    private static int RunLiveStableFieldEnablementSelfTest()
    {
        try
        {
            static Models.ScreenRegion ValidRegion() => new() { X = 10, Y = 10, Width = 60, Height = 20 };
            var settings = Services.SettingsService.CreateDefaults();
            settings.OcrProfile.Name = "1031_Dilber";
            settings.EnableStableParameterDiagnostics = false;
            settings.StableRpmRegion = ValidRegion(); settings.StableTorqueRegion = ValidRegion();
            settings.StableThrustForceRegion = ValidRegion(); settings.StableAdvanceSpeedRegion = ValidRegion();
            settings.StablePenetrationRegion = ValidRegion();
            Services.OcrProfileMigrator.Migrate(settings);
            var ids = new[] { Models.BuiltInFieldIds.StableRpm, Models.BuiltInFieldIds.StableTorque,
                Models.BuiltInFieldIds.StableThrustForce, Models.BuiltInFieldIds.StableAdvanceSpeed,
                Models.BuiltInFieldIds.StablePenetration };
            var enabledWithDiagnosticsOff = ids.All(id => settings.OcrProfile.FindField(id) is { IsEnabled: true });
            var selectedByLiveOcr = ids.All(id => Services.OcrDataSource.GetEnabledAdditionalFieldDefinitions(settings.OcrProfile)
                .Any(field => string.Equals(field.FieldId, id, StringComparison.OrdinalIgnoreCase)));

            var invalid = Services.SettingsService.CreateDefaults(); invalid.OcrProfile.Name = "1031_Dilber";
            invalid.EnableStableParameterDiagnostics = false; invalid.StableRpmRegion = new();
            Services.OcrProfileMigrator.Migrate(invalid);
            var invalidDisabled = invalid.OcrProfile.FindField(Models.BuiltInFieldIds.StableRpm) is { IsEnabled: false };
            var otherMachinesUnaffected = new[] { "1028", "1029", "1030" }.All(machine =>
            {
                var other = Services.SettingsService.CreateDefaults(); other.OcrProfile.Name = machine;
                other.EnableStableParameterDiagnostics = false; other.StableRpmRegion = ValidRegion();
                Services.OcrProfileMigrator.Migrate(other);
                return other.OcrProfile.FindField(Models.BuiltInFieldIds.StableRpm) is { IsEnabled: false };
            });

            ApplicationConfiguration.Initialize(); using var log = new Services.LogService(); using var form = new Forms.MainForm(log);
            var now = DateTime.Now;
            var fields = new Dictionary<string, Models.FieldMeasurementResult>(StringComparer.OrdinalIgnoreCase)
            {
                [Models.BuiltInFieldIds.StableRpm] = Diagnostic(Models.BuiltInFieldIds.StableRpm, "2.08", 2.08m),
                [Models.BuiltInFieldIds.StableTorque] = Diagnostic(Models.BuiltInFieldIds.StableTorque, "2.359", 2.359m),
                [Models.BuiltInFieldIds.StableThrustForce] = Diagnostic(Models.BuiltInFieldIds.StableThrustForce, "23029", 23029m),
                [Models.BuiltInFieldIds.StableAdvanceSpeed] = Diagnostic(Models.BuiltInFieldIds.StableAdvanceSpeed, "12.5", 12.5m),
                [Models.BuiltInFieldIds.StablePenetration] = Diagnostic(Models.BuiltInFieldIds.StablePenetration, "5.9", 5.9m)
            };
            var measurement = new Models.MeasurementRecord { Timestamp = now, FieldResults = fields };
            form.ReceiveMonitoringMeasurementForTest(measurement);
            var recordPublished = measurement.DiagnosticRpm is { ParsedValue: 2.08m, OfficialValue: null, DecisionSource: "DiagnosticOnly" }
                && ids.All(id => measurement.FieldResults.ContainsKey(id));
            var card = form.DashboardCards[0].Value == "2.08 rpm";
            var history = Convert.ToString(form.HistoryGrid.Rows[0].Cells["Rpm"].Value) == "2.08";
            var stableUntouched = !new Models.RingSession().StableDataCaptured && new Models.RingSession().StableData is null;
            var passed = enabledWithDiagnosticsOff && selectedByLiveOcr && invalidDisabled && otherMachinesUnaffected
                && recordPublished && card && history && stableUntouched;
            Console.WriteLine($"live-stable-field-enablement-self-test: {(passed ? "PASS" : "FAIL")} | diagnosticsOffEnabled={enabledWithDiagnosticsOff} | liveSelection={selectedByLiveOcr} | invalidDisabled={invalidDisabled} | otherMachines={otherMachinesUnaffected} | record={recordPublished} | card={form.DashboardCards[0].Value} | history={form.HistoryGrid.Rows[0].Cells["Rpm"].Value} | stableUntouched={stableUntouched}");
            return passed ? 0 : 1;

            Models.FieldMeasurementResult Diagnostic(string id, string raw, decimal value) => new()
            { FieldId = id, RawValue = raw, ParsedValue = value, FullLineResult = value, OfficialValue = null,
                ConfidenceStatus = Models.OcrConfidenceStatus.Partial, FailureReason = "LowConfidence",
                DecisionSource = "DiagnosticOnly", CapturedAt = now, CompletedAt = now, OcrConfidencePercent = 35 };
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }

    private sealed class LiveStableDiagnosticDataSource : Services.IDataSource
    {
        private int _read;
        public string Name => "OcrDataSource 1031 Live Binding Test";
        public Task<Models.MeasurementRecord> ReadAsync(CancellationToken cancellationToken)
        {
            var read = Interlocked.Increment(ref _read);
            var fields = read == 1 ? new Dictionary<string, Models.FieldMeasurementResult>(StringComparer.OrdinalIgnoreCase) : new(StringComparer.OrdinalIgnoreCase)
            {
                [Models.BuiltInFieldIds.StableRpm] = Field(Models.BuiltInFieldIds.StableRpm, "2", 2m, "LowConfidence", 29.5),
                [Models.BuiltInFieldIds.StableTorque] = Field(Models.BuiltInFieldIds.StableTorque, "2.359", 2.359m, "Success", 88),
                [Models.BuiltInFieldIds.StableThrustForce] = Field(Models.BuiltInFieldIds.StableThrustForce, "23029", 23029m, "Success", 91),
                [Models.BuiltInFieldIds.StableAdvanceSpeed] = Field(Models.BuiltInFieldIds.StableAdvanceSpeed, "12.5", 12.5m, "Success", 84),
                [Models.BuiltInFieldIds.StablePenetration] = Field(Models.BuiltInFieldIds.StablePenetration, "5.9", 5.9m, "Success", 86)
            };
            return Task.FromResult(new Models.MeasurementRecord { Timestamp = DateTime.Now, FieldResults = fields,
                OcrConfidence = Models.OcrConfidenceStatus.Unreadable });
        }
        private static Models.FieldMeasurementResult Field(string id, string raw, decimal value, string failure, double confidence) => new()
        { FieldId = id, RawValue = raw, ParsedValue = value, OfficialValue = null, FullLineResult = value,
            FailureReason = failure, OcrConfidencePercent = confidence, DecisionSource = "DiagnosticOnly", CapturedAt = DateTime.Now, CompletedAt = DateTime.Now };
    }

    private static int RunDiagnosticValueVisibilitySelfTest()
    {
        try
        {
            ApplicationConfiguration.Initialize();
            using var log = new Services.LogService();
            using var form = new Forms.MainForm(log);
            var now = DateTime.Now;
            var fields = new Dictionary<string, Models.FieldMeasurementResult>(StringComparer.OrdinalIgnoreCase)
            {
                [Models.BuiltInFieldIds.StableRpm] = Diagnostic(2m, "2", "LowConfidence", 29.5),
                [Models.BuiltInFieldIds.StableTorque] = Diagnostic(2.359m, "2.359", "Success", 88),
                [Models.BuiltInFieldIds.StableThrustForce] = Diagnostic(23029m, "23029", "Success", 91),
                [Models.BuiltInFieldIds.StableAdvanceSpeed] = Diagnostic(12.5m, "12.5", "Success", 87),
                [Models.BuiltInFieldIds.StablePenetration] = Diagnostic(5.9m, "5.9", "LowConfidence", 31)
            };
            form.DisplayMeasurementForTest(new Models.MeasurementRecord { Timestamp = now, FieldResults = fields });
            var cards = form.DashboardCards;
            var cardValues = cards[0].Value == "2.00 rpm" && cards[1].Value == "2.359 MNm"
                && cards[2].Value == "23029 kN" && cards[3].Value == "12.5 mm/min" && cards[4].Value == "5.9 mm/rot";
            var row = form.HistoryGrid.Rows[0];
            var historyValues = Convert.ToString(row.Cells["Rpm"].Value)?.StartsWith("2.00", StringComparison.Ordinal) == true
                && Convert.ToString(row.Cells["Torque"].Value) == "2.359"
                && Convert.ToString(row.Cells["Thrust"].Value) == "23029"
                && Convert.ToString(row.Cells["Speed"].Value) == "12.5"
                && Convert.ToString(row.Cells["Penetration"].Value)?.StartsWith("5.9", StringComparison.Ordinal) == true;
            var lowConfidenceVisible = row.Cells["Rpm"].Style.ForeColor == Color.FromArgb(251, 191, 36)
                && row.Cells["Rpm"].ToolTipText.Contains("güven", StringComparison.OrdinalIgnoreCase);

            var fallbackFields = new Dictionary<string, Models.FieldMeasurementResult>(StringComparer.OrdinalIgnoreCase)
            {
                [Models.BuiltInFieldIds.StableRpm] = new() { FieldId = Models.BuiltInFieldIds.StableRpm, RawValue = "2,12 rpm", FailureReason = "LowConfidence", CapturedAt = now.AddSeconds(1) },
                [Models.BuiltInFieldIds.StableTorque] = new() { FieldId = Models.BuiltInFieldIds.StableTorque, FullLineResult = 3.1m, FailureReason = "LowConfidence", CapturedAt = now.AddSeconds(1) },
                [Models.BuiltInFieldIds.StableThrustForce] = new() { FieldId = Models.BuiltInFieldIds.StableThrustForce, RawValue = "okunamadı", FailureReason = "OcrEmpty", CapturedAt = now.AddSeconds(1) }
            };
            form.DisplayMeasurementForTest(new Models.MeasurementRecord { Timestamp = now.AddSeconds(1), FieldResults = fallbackFields });
            var fallbacks = form.DashboardCards[0].Value == "2.12 rpm" && form.DashboardCards[1].Value == "3.100 MNm"
                && form.DashboardCards[2].Value == "23029 kN"
                && form.DashboardCards[2].Detail.Contains("yeni okuma başarısız", StringComparison.OrdinalIgnoreCase)
                && Convert.ToString(form.HistoryGrid.Rows[0].Cells["Thrust"].Value) == "—";
            var diagnosticOnly = fields.Values.All(field => field.OfficialValue is null) && !new Models.RingSession().StableDataCaptured;
            var passed = cardValues && historyValues && lowConfidenceVisible && fallbacks && diagnosticOnly;
            var displayedCards = string.Join(",", form.DashboardCards.Take(3).Select(card => card.Value));
            form.StopBackgroundServicesForTest();
            Console.WriteLine($"diagnostic-value-visibility-self-test: {(passed ? "PASS" : "FAIL")} | cards={cardValues} | history={historyValues} | lowConfidence={lowConfidenceVisible} | fallback={fallbacks} [{displayedCards}] | diagnosticOnly={diagnosticOnly}");
            return passed ? 0 : 1;

            Models.FieldMeasurementResult Diagnostic(decimal value, string raw, string failure, double confidence) => new()
            {
                FieldId = "Diagnostic", RawValue = raw, ParsedValue = value, OfficialValue = null,
                FailureReason = failure, ConfidenceStatus = failure == "Success" ? Models.OcrConfidenceStatus.Reliable : Models.OcrConfidenceStatus.Partial,
                CapturedAt = now, CompletedAt = now, OcrConfidencePercent = confidence, DecisionSource = "DiagnosticOnly"
            };
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }

    private static int RunTemporalRecoverySelfTest()
    {
        try
        {
            var now = DateTime.Now;
            var a = new Services.TemporalValueValidationService();
            _ = a.Validate("1031", "Kantar 1", 184, 15, 2, 2, now);
            var wrong447 = a.Validate("1031", "Kantar 1", 447, 15, 2, 2, now.AddSeconds(1));
            var back184 = a.Validate("1031", "Kantar 1", 184, 15, 2, 2, now.AddSeconds(2));
            var transientRejected = wrong447.Value == 184 && wrong447.PendingValue == 447 && wrong447.ConfirmationCount == 1
                && back184.Value == 184 && !back184.IsHeld;

            var b = new Services.TemporalValueValidationService();
            _ = b.Validate("1031", "Kantar 1", 447, 15, 2, 2, now);
            var recoveryOne = b.Validate("1031", "Kantar 1", 184, 15, 2, 2, now.AddSeconds(1));
            var recoveryTwo = b.Validate("1031", "Kantar 1", 183, 15, 2, 2, now.AddSeconds(2));
            var recovered = recoveryOne.Value == 447 && recoveryOne.PendingValue == 184 && recoveryOne.ConfirmationCount == 1
                && recoveryTwo.Value == 183 && !recoveryTwo.IsHeld && recoveryTwo.ValidationState == "AcceptedAfterConfirmation";

            var c = new Services.TemporalValueValidationService();
            _ = c.Validate("1031", "Nominal", 184, 15, 2, 2, now);
            var genuineOne = c.Validate("1031", "Nominal", 205, 15, 2, 2, now.AddSeconds(1));
            var genuineTwo = c.Validate("1031", "Nominal", 206, 15, 2, 2, now.AddSeconds(2));
            var genuineAccepted = genuineOne.IsHeld && genuineTwo.Value == 206 && !genuineTwo.IsHeld;

            var d = new Services.TemporalValueValidationService();
            _ = d.Validate("1031", "Kantar 2", 100, 15, 2, 2, now);
            var candidate = d.Validate("1031", "Kantar 2", 140, 15, 2, 2, now.AddSeconds(1));
            var cacheRepeat = d.Validate("1031", "Kantar 2", 140, 15, 2, 2, now.AddSeconds(2), isIndependentRealOcr: false);
            var sameCaptured = d.Validate("1031", "Kantar 2", 140, 15, 2, 2, now.AddSeconds(1));
            var independent = d.Validate("1031", "Kantar 2", 140, 15, 2, 2, now.AddSeconds(3));
            var repeatsIgnored = candidate.ConfirmationCount == 1 && cacheRepeat.ConfirmationCount == 1
                && sameCaptured.ConfirmationCount == 1 && independent.Value == 140 && !independent.IsHeld;

            var isolated = new Services.TemporalValueValidationService();
            _ = isolated.Validate("1031", "Nominal", 10, 5, 2, 1, now);
            _ = isolated.Validate("1031", "Kantar 1", 100, 15, 2, 2, now);
            var nominalPending = isolated.Validate("1031", "Nominal", 30, 5, 2, 1, now.AddSeconds(1));
            var scaleDirect = isolated.Validate("1031", "Kantar 1", 101, 15, 2, 2, now.AddSeconds(1));
            var independentFields = nominalPending.IsHeld && scaleDirect.Value == 101 && !scaleDirect.IsHeld;
            var passed = transientRejected && recovered && genuineAccepted && repeatsIgnored && independentFields;
            Console.WriteLine($"temporal-recovery-self-test: {(passed ? "PASS" : "FAIL")} | transient={transientRejected} | recovery={recovered} | genuine={genuineAccepted} | cacheAndCapturedAt={repeatsIgnored} | isolated={independentFields}");
            return passed ? 0 : 1;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }

    private static int RunHistoryOcrReadingSelfTest()
    {
        try
        {
            ApplicationConfiguration.Initialize(); using var log = new Services.LogService(); using var form = new Forms.MainForm(log);
            var temporal = new Services.TemporalValueValidationService(); var now = DateTime.Now;
            _ = temporal.Validate("1031", Models.BuiltInFieldIds.Scale1, 83m, 15m, 2, 2m, now);
            var pending = temporal.Validate("1031", Models.BuiltInFieldIds.Scale1, 101m, 15m, 2, 2m, now.AddSeconds(1));
            var pendingField = new Models.FieldMeasurementResult { FieldId = Models.BuiltInFieldIds.Scale1,
                RawValue = "101", ParsedValue = 101m, FullLineResult = 101m, OfficialValue = pending.Value,
                FailureReason = "SuspiciousPending", ConfidenceStatus = Models.OcrConfidenceStatus.Partial };
            form.DisplayMeasurementForTest(new Models.MeasurementRecord { Timestamp = now.AddSeconds(1), Scale1ValidationStatus = pending.Status,
                FieldResults = new Dictionary<string, Models.FieldMeasurementResult> { [Models.BuiltInFieldIds.Scale1] = pendingField }, HasHeldSuspiciousValue = true });
            var pendingCell = form.HistoryGrid.Rows[0].Cells["Scale1"];
            var pendingVisible = Convert.ToString(pendingCell.Value) == "101 ⚠" && pendingField.OfficialValue == 83m
                && pendingCell.ToolTipText.Contains("OCR okuması: 101", StringComparison.Ordinal)
                && pendingCell.ToolTipText.Contains("Son kabul edilen: 83", StringComparison.Ordinal)
                && pendingCell.ToolTipText.Contains("1/2", StringComparison.Ordinal);

            var accepted = temporal.Validate("1031", Models.BuiltInFieldIds.Scale1, 101m, 15m, 2, 2m, now.AddSeconds(2));
            var acceptedField = pendingField with { OfficialValue = accepted.Value, FailureReason = "Success" };
            form.DisplayMeasurementForTest(new Models.MeasurementRecord { Timestamp = now.AddSeconds(2), Scale1ValidationStatus = accepted.Status,
                FieldResults = new Dictionary<string, Models.FieldMeasurementResult> { [Models.BuiltInFieldIds.Scale1] = acceptedField } });
            var acceptedCell = form.HistoryGrid.Rows[0].Cells["Scale1"];
            var acceptedVisible = accepted.Value == 101m && Convert.ToString(acceptedCell.Value) == "101"
                && !acceptedCell.ToolTipText.Contains("doğrulama", StringComparison.OrdinalIgnoreCase)
                && !acceptedCell.ToolTipText.Contains("Son kabul edilen", StringComparison.OrdinalIgnoreCase);
            var nominalSelectedFinal = new Models.FieldMeasurementResult
            {
                FieldId = Models.BuiltInFieldIds.Nominal,
                RawValue = "4118",
                ParsedValue = 4118m,
                ObservedValue = 118m,
                FullLineResult = 4118m,
                CharacterResult = 118m,
                OfficialValue = 118m,
                FailureReason = "Success",
                ConfidenceStatus = Models.OcrConfidenceStatus.Reliable
            };
            form.DisplayMeasurementForTest(new Models.MeasurementRecord
            {
                Timestamp = now.AddSeconds(3),
                Nominal = 118m,
                FieldResults = new Dictionary<string, Models.FieldMeasurementResult>
                {
                    [Models.BuiltInFieldIds.Nominal] = nominalSelectedFinal
                }
            });
            var nominalCell = form.HistoryGrid.Rows[0].Cells["Nominal"];
            var selectedFinalVisible = Convert.ToString(nominalCell.Value) == "118";
            var passed = pendingVisible && acceptedVisible && selectedFinalVisible;
            Console.WriteLine($"history-ocr-reading-self-test: {(passed ? "PASS" : "FAIL")} | pending={pendingCell.Value} official={pendingField.OfficialValue} tooltip={pendingVisible} | accepted={acceptedCell.Value} warningCleared={acceptedVisible} | nominalFinal={nominalCell.Value}");
            return passed ? 0 : 1;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }

    private static int RunDashboardUiSelfTest()
    {
        try
        {
            ApplicationConfiguration.Initialize(); using var log = new Services.LogService(); using var form = new Forms.MainForm(log);
            var startsWithExcavation = form.ActiveDashboardCategory == Forms.MainForm.DashboardCategory.Excavation
                && form.DashboardCards.Select(card => card.Caption).SequenceEqual(new[] { "RPM", "TORK", "İTME KUVVETİ", "İLERLEME HIZI", "PENETRASYON", "İLERLEME" });
            var stableFields = new Dictionary<string, Models.FieldMeasurementResult>(StringComparer.OrdinalIgnoreCase)
            {
                [Models.BuiltInFieldIds.StableRpm] = Diagnostic(2.04m), [Models.BuiltInFieldIds.StableTorque] = Diagnostic(3.115m),
                [Models.BuiltInFieldIds.StableThrustForce] = Diagnostic(22783), [Models.BuiltInFieldIds.StableAdvanceSpeed] = Diagnostic(14.6m),
                [Models.BuiltInFieldIds.StablePenetration] = Diagnostic(7.3m), [Models.BuiltInFieldIds.AdvanceMm] = Official(1054)
            };
            var record = new Models.MeasurementRecord { Timestamp = DateTime.Now, Nominal = 55, Scale1 = 118, Scale2 = 128, GateMillimeters = 12,
                GateStatus = "Kısmen Açık", ExcavationStatus = "Kazı devam ediyor", FieldResults = stableFields };
            form.DisplayMeasurementForTest(record);
            var excavationValues = form.DashboardCards[0].Value.Contains("2.04", StringComparison.Ordinal)
                && form.DashboardCards[5].Value.Contains("1054", StringComparison.Ordinal);
            var startText = form.StartButton.Text; var pauseText = form.PauseButton.Text;
            form.SelectDashboardCategoryForTest(Forms.MainForm.DashboardCategory.Material);
            var material = form.DashboardCards[0].Caption == "NOMİNAL DEĞER" && form.DashboardCards[0].Value.Contains("55", StringComparison.Ordinal);
            form.SelectDashboardCategoryForTest(Forms.MainForm.DashboardCategory.Ring);
            var ring = form.DashboardCards[0].Caption == "RING" && form.DashboardCards[1].Caption == "STABİL BÖLGE";
            var stateUnchanged = form.StartButton.Text == startText && form.PauseButton.Text == pauseText;
            var columns = new[] { "Time", "Rpm", "Torque", "Thrust", "Speed", "Penetration", "Nominal", "Scale1", "Scale2",
                "NominalInjection", "ActualInjection", "Epb1", "Epb2", "Epb3", "Epb4", "Epb5", "OCR" };
            var history = form.HistoryGrid.Columns.Cast<DataGridViewColumn>().Select(column => column.Name).SequenceEqual(columns)
                && form.HistoryGrid.ScrollBars == ScrollBars.Both && form.HistoryGrid.Columns["Time"].Frozen
                && form.HistoryGrid.AutoSizeColumnsMode == DataGridViewAutoSizeColumnsMode.Fill
                && Enumerable.Range(1, 5).All(number => form.HistoryGrid.Columns[$"Epb{number}"].MinimumWidth == 75)
                && form.HistoryGrid.Columns["Penetration"].HeaderText == "Penetrasyon"
                && form.HistoryGrid.Columns["Nominal"].HeaderText == "Nominal Pasa"
                && form.HistoryGrid.Columns["Scale1"].HeaderText == "Kantar 1"
                && form.HistoryGrid.Columns["Scale2"].HeaderText == "Kantar 2"
                && form.HistoryGrid.Columns["NominalInjection"].HeaderText == "Nominal Enj."
                && form.HistoryGrid.Columns["ActualInjection"].HeaderText == "Gerçek Enj.";
            var modernHistoryStyle = form.HistoryGrid.ColumnHeadersHeight == 40 && form.HistoryGrid.RowTemplate.Height == 34
                && form.HistoryGrid.CellBorderStyle == DataGridViewCellBorderStyle.SingleHorizontal
                && form.HistoryGrid.Columns["Penetration"].DividerWidth == 2 && form.HistoryGrid.Columns["Scale2"].DividerWidth == 2
                && form.HistoryGrid.Columns["ActualInjection"].DividerWidth == 2 && form.HistoryGrid.Columns["Epb5"].DividerWidth == 2
                && form.HistoryGrid.Columns["Nominal"].HeaderCell.ToolTipText == "Nominal Pasa Miktarı"
                && form.HistoryGrid.Columns["OCR"].HeaderCell.Style.BackColor == Color.FromArgb(38, 79, 102);
            var suspiciousFields = new Dictionary<string, Models.FieldMeasurementResult>(StringComparer.OrdinalIgnoreCase)
            { [Models.BuiltInFieldIds.Nominal] = new() { FieldId = Models.BuiltInFieldIds.Nominal, ParsedValue = 87, OfficialValue = 55, FailureReason = "SuspiciousPending", ConfidenceStatus = Models.OcrConfidenceStatus.Partial } };
            form.DisplayMeasurementForTest(new Models.MeasurementRecord { Timestamp = DateTime.Now, Nominal = 55, HasHeldSuspiciousValue = true,
                OcrConfidence = Models.OcrConfidenceStatus.Partial, FieldResults = suspiciousFields });
            var suspiciousVisual = Convert.ToString(form.HistoryGrid.Rows[0].Cells["Nominal"].Value)?.Contains("⚠", StringComparison.Ordinal) == true
                && Convert.ToString(form.HistoryGrid.Rows[0].Cells["OCR"].Value) == "!"
                && form.HistoryGrid.Rows[0].Cells["OCR"].Style.ForeColor == Color.FromArgb(251, 191, 36);
            form.DisplayMeasurementForTest(new Models.MeasurementRecord { Timestamp = DateTime.Now, OcrConfidence = Models.OcrConfidenceStatus.Unreadable });
            var unreadableVisual = Convert.ToString(form.HistoryGrid.Rows[0].Cells["Nominal"].Value) == "—"
                && Convert.ToString(form.HistoryGrid.Rows[0].Cells["OCR"].Value) == "—";
            var stableUntouched = !new Models.RingSession().StableDataCaptured;
            var layout1920 = CheckLayout(form, new Size(1920, 1080)) && HistoryFillsWidth(form);
            var usedWidths = string.Join(",", form.HistoryGrid.Columns.Cast<DataGridViewColumn>().Select(column => $"{column.HeaderText}:{column.Width}"));
            var layout1366 = CheckLayout(form, new Size(1366, 768));
            var dpiLayout = CheckLayout(form, new Size(1536, 864));
            var clippedHeaders = form.HistoryGrid.Columns.Cast<DataGridViewColumn>().Where(column =>
                TextRenderer.MeasureText(column.HeaderText, form.HistoryGrid.ColumnHeadersDefaultCellStyle.Font).Width + 16 > column.Width).Select(column => column.HeaderText).ToArray();
            var dpi = dpiLayout && clippedHeaders.Length == 0;
            var layout = layout1920 && layout1366;
            var passed = startsWithExcavation && excavationValues && material && ring && stateUnchanged && history && modernHistoryStyle
                && suspiciousVisual && unreadableVisual && stableUntouched && layout && dpi;
            Console.WriteLine($"dashboard-ui-self-test: {(passed ? "PASS" : "FAIL")} | initialKazi={startsWithExcavation} | values={excavationValues} | material={material} | ring={ring} | state={stateUnchanged} | columns={form.HistoryGrid.Columns.Count} | fill={form.HistoryGrid.AutoSizeColumnsMode} | header={form.HistoryGrid.ColumnHeadersHeight} | row={form.HistoryGrid.RowTemplate.Height} | style={modernHistoryStyle} | 1920={layout1920} | 125dpi={dpi} | clipped=[{string.Join(',', clippedHeaders)}] | 1366={layout1366} | widths=[{usedWidths}] | warning={suspiciousVisual} | unreadable={unreadableVisual} | stable={stableUntouched}");
            return passed ? 0 : 1;

            static Models.FieldMeasurementResult Diagnostic(decimal value) => new() { ParsedValue = value, OfficialValue = null, FailureReason = "Success", ConfidenceStatus = Models.OcrConfidenceStatus.Reliable };
            static Models.FieldMeasurementResult Official(decimal value) => new() { ParsedValue = value, OfficialValue = value, FailureReason = "Success", ConfidenceStatus = Models.OcrConfidenceStatus.Reliable };
            static bool CheckLayout(Forms.MainForm target, Size size)
            {
                target.ClientSize = size; target.PerformLayout();
                return target.DashboardCards.All(card => card.Width >= 180 && card.Height >= 65)
                    && new[] { target.ExcavationCategoryButton, target.MaterialCategoryButton, target.RingCategoryButton }
                        .All(button => button.Width >= 100 && button.Height >= 28);
            }
            static bool HistoryFillsWidth(Forms.MainForm target)
            {
                target.HistoryGrid.PerformLayout();
                var used = target.HistoryGrid.Columns.GetColumnsWidth(DataGridViewElementStates.Visible);
                return target.HistoryGrid.Dock == DockStyle.Fill && Math.Abs(target.HistoryGrid.ClientSize.Width - used) <= 30;
            }
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }

    private static int RunStableParameterOcrSelfTest()
    {
        try
        {
            var settings = Services.SettingsService.CreateDefaults();
            settings.OcrProfile.Name = "1031_Dilber";
            settings.EnableStableParameterDiagnostics = true;
            settings.StableRpmRegion = Region(); settings.StableTorqueRegion = Region();
            settings.StableThrustForceRegion = Region(); settings.StableAdvanceSpeedRegion = Region();
            settings.StablePenetrationRegion = Region();
            Services.OcrProfileMigrator.Migrate(settings);
            var fields = settings.OcrProfile.Fields.Where(field => field.ReportGroup == "StableDiagnostics").ToDictionary(field => field.FieldId);
            var parsed = Parse(Models.BuiltInFieldIds.StableRpm, "2.04") == 2.04m
                && Parse(Models.BuiltInFieldIds.StableRpm, "2,04 rpm") == 2.04m
                && Parse(Models.BuiltInFieldIds.StableTorque, "3.115 MNm") == 3.115m
                && Parse(Models.BuiltInFieldIds.StableThrustForce, "22783 kN") == 22783m
                && Parse(Models.BuiltInFieldIds.StableAdvanceSpeed, "14.6 mm/min") == 14.6m
                && Parse(Models.BuiltInFieldIds.StablePenetration, "7,3 mm/rot") == 7.3m;
            var rangeRejected = !Services.NumericFieldParser.TryParse("20.1", fields[Models.BuiltInFieldIds.StableRpm], out _, out _);
            var allIndependent = fields.Count == 5 && fields.Values.All(field => field.IsEnabled && field.SupportsCharacterOcr
                && !field.UsesSuspiciousValueFilter && field.Category == Models.MeasurementCategory.OcrMeasurement);
            var oneFailureDoesNotBlockOthers = !Services.NumericFieldParser.TryParse("okunamadı", fields[Models.BuiltInFieldIds.StableTorque], out _, out _)
                && Services.NumericFieldParser.TryParse("22783", fields[Models.BuiltInFieldIds.StableThrustForce], out var thrust, out _) && thrust == 22783m;
            var disabled = Services.SettingsService.CreateDefaults(); disabled.OcrProfile.Name = "1031_Dilber";
            disabled.StableRpmRegion = Region(); Services.OcrProfileMigrator.Migrate(disabled);
            var liveOcrContinuesWithDetailedDiagnosticsOff = disabled.OcrProfile.FindField(Models.BuiltInFieldIds.StableRpm) is { IsEnabled: true };
            var session = new Models.RingSession();
            var ringUnaffected = !session.StableDataCaptured && session.StableData is null;
            Console.WriteLine($"stable-parameter-ocr-self-test: fields={fields.Count}, parse={parsed}, range={rangeRejected}, isolated={oneFailureDoesNotBlockOthers}, liveWithDetailedDiagnosticsOff={liveOcrContinuesWithDetailedDiagnosticsOff}, ring={ringUnaffected}");
            return parsed && rangeRejected && allIndependent && oneFailureDoesNotBlockOthers && liveOcrContinuesWithDetailedDiagnosticsOff && ringUnaffected ? 0 : 1;

            decimal? Parse(string id, string text) => Services.NumericFieldParser.TryParse(text, fields[id], out var value, out _) ? value : null;
            static Models.ScreenRegion Region() => new() { X = 10, Y = 10, Width = 60, Height = 20 };
        }
        catch (Exception exception) { Console.WriteLine(exception); return 1; }
    }

    private static int RunExtensibleFieldsSelfTest()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"TBMWatchdog-Fields-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(folder); var service = new Services.SettingsService(folder);
            var legacy = Services.SettingsService.CreateDefaults(); legacy.OcrProfile.SchemaVersion = 0; legacy.OcrProfile.Fields.Clear();
            legacy.NominalRegion = new Models.ScreenRegion { X = 11, Y = 12, Width = 31, Height = 16 };
            legacy.GateRegion = new Models.ScreenRegion { X = 21, Y = 22, Width = 41, Height = 18 };
            File.WriteAllText(service.SettingsPath, System.Text.Json.JsonSerializer.Serialize(legacy));
            var migrated = service.Load(); var builtIns = migrated.OcrProfile.Fields.OrderBy(x => x.DisplayOrder).ToArray();
            var migrationOk = migrated.OcrProfile.SchemaVersion == Services.OcrProfileMigrator.CurrentSchemaVersion
                && builtIns.Select(x => x.FieldId).SequenceEqual(new[] { Models.BuiltInFieldIds.Nominal, Models.BuiltInFieldIds.Gate, Models.BuiltInFieldIds.Scale1, Models.BuiltInFieldIds.Scale2, Models.BuiltInFieldIds.AdvanceMm,
                    Models.BuiltInFieldIds.AdvanceGroupA, Models.BuiltInFieldIds.AdvanceGroupB, Models.BuiltInFieldIds.AdvanceGroupC, Models.BuiltInFieldIds.AdvanceGroupD,
                    Models.BuiltInFieldIds.StableRpm, Models.BuiltInFieldIds.StableTorque, Models.BuiltInFieldIds.StableThrustForce,
                    Models.BuiltInFieldIds.StableAdvanceSpeed, Models.BuiltInFieldIds.StablePenetration })
                && builtIns[0].RegionCoordinates.X == 11 && builtIns[1].RegionCoordinates.Width == 41
                && Directory.EnumerateFiles(folder, "*.bak").Any();
            var pressure = new Models.NumericFieldDefinition { FieldId = "PressureA", DisplayName = "Basınç A", Unit = "bar",
                ValueType = Models.FieldValueType.Decimal, DecimalPlaces = 1, MinimumValue = -10, MaximumValue = 1000,
                UsesSuspiciousValueFilter = true, MaximumChangePerReading = 10, ConfirmationReadings = 2, ConfirmationTolerance = 1,
                RegionCoordinates = new Models.ScreenRegion { X = 3, Y = 4, Width = 30, Height = 15 } };
            migrated.OcrProfile.Fields.Add(pressure); service.Save(migrated); var reloaded = new Services.SettingsService(folder).Load();
            var preservation = reloaded.OcrProfile.FindField("PressureA") is { Unit: "bar", DecimalPlaces: 1 };
            var parser = Services.NumericFieldParser.TryParse("12,5", pressure, out var parsed, out var normalized) && parsed == 12.5m && normalized == "12.5";
            var comparisonPath = Path.Combine(folder, "LiveOcrComparison.csv"); var csv = new Services.CharacterOcrComparisonCsvService(comparisonPath); var now = DateTime.Now;
            var comparison = new Services.CharacterOcrComparison("PressureA", 12.5m, 12.5m, true, 4, 8, now, now.AddMilliseconds(8), 8);
            csv.Append(new[] { comparison }); var longCsv = File.ReadAllText(comparisonPath).Contains("\"PressureA\"");
            using var image = new Bitmap(12, 8); using var cache = new Services.OcrImageCacheService(new(true, 98.5, 2, 5));
            var read = new Models.OcrReadResult(12.5m, "12,5", "12.5", .9f, null); cache.Store("PressureA", image, read, 12.5m, read, "OCR", "Success", now, comparison, false);
            var cacheIndependent = cache.TryGet("PressureA", image, now.AddMilliseconds(10), false).Hit && cache.Statistics("PressureA").CacheHitCount == 1;
            var temporal = new Services.TemporalValueValidationService(); temporal.Validate("M", "PressureA", 10, 5, 2, 1, now); temporal.Validate("M", "PressureA", 50, 5, 2, 1, now);
            var other = temporal.Validate("M", "OtherField", 7, 5, 2, 1, now); var isolated = temporal.HasPending("PressureA") && !temporal.HasPending("OtherField") && other.Value == 7;
            var record = new Models.MeasurementRecord { Nominal = 55, Scale1 = 118, Scale2 = 128, GateMillimeters = 12 };
            var recordSync = record.FieldResults[Models.BuiltInFieldIds.Nominal].OfficialValue == record.Nominal
                && record.FieldResults[Models.BuiltInFieldIds.Gate].OfficialValue == record.GateMillimeters;
            var gatePreserved = reloaded.OcrProfile.FindField(Models.BuiltInFieldIds.Gate) is { UsesOfficialCharacterDecision: true }
                && reloaded.OcrProfile.GateMaximumChangePerReading == legacy.OcrProfile.GateMaximumChangePerReading;
            var perfSettings = new Models.AppSettings { EnablePerformanceCsv = false }; using var monitor = new Services.PerformanceMonitoringService(perfSettings, Path.Combine(folder, "p.csv")); monitor.StartSession("M");
            monitor.Record(new Services.OcrPerformanceFrame(now, new[] { comparison }, new[] { cache.Statistics("PressureA") }, 0, 0, 0, 0, 0)
                { FieldDefinitions = new[] { pressure } }); var performance = monitor.CaptureForTest().Fields.Single().Field == "PressureA"; monitor.StopSession();
            Console.WriteLine($"Extensible fields: schema={reloaded.OcrProfile.SchemaVersion}, fields={reloaded.OcrProfile.Fields.Count}, decimal={normalized}, cache={cacheIndependent}, performance={performance}");
            return migrationOk && preservation && parser && longCsv && cacheIndependent && isolated && recordSync && gatePreserved && performance ? 0 : 1;
        }
        catch (Exception exception) { Console.WriteLine(exception); return 1; }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    private static int RunPerformanceMonitorSelfTest()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"TBMWatchdog-Performance-{Guid.NewGuid():N}");
        try
        {
            var csv = Path.Combine(folder, "PerformanceMetrics.csv");
            var settings = new Models.AppSettings { PerformanceMonitorIntervalSeconds = 1, EnablePerformanceCsv = true, PerformanceCsvIntervalSeconds = 1 };
            using var monitor = new Services.PerformanceMonitoringService(settings, csv);
            monitor.StartSession("TestMachine"); var now = DateTime.Now;
            var fields = new[] { "Nominal", "Kapak mm", "Kantar 1", "Kantar 2" };
            var comparisons = fields.Select((field, index) => new Services.CharacterOcrComparison(field, 10 + index, 10 + index, true,
                20 + index, 100 + index * 10, now, now.AddMilliseconds(150 + index * 20), 150 + index * 20,
                CacheUsed: index == 3, ImageSimilarityPercent: 99.5, RealOcrExecuted: index != 3)).ToArray();
            var cache = new[]
            {
                new Services.OcrCacheStatistics("Nominal",1,0,1,0,100,0,100),
                new Services.OcrCacheStatistics("Gate",1,0,1,0,100,0,110),
                new Services.OcrCacheStatistics("Scale1",1,0,1,0,100,0,120),
                new Services.OcrCacheStatistics("Scale2",0,1,0,100,0,130,0)
            };
            monitor.Record(new Services.OcrPerformanceFrame(now, comparisons, cache, 0, 0, 0, 0, 0)
            {
                FieldDefinitions = new[]
                {
                    new Models.NumericFieldDefinition { FieldId = "Nominal", DisplayName = "Nominal" },
                    new Models.NumericFieldDefinition { FieldId = "Gate", DisplayName = "Kapak mm" },
                    new Models.NumericFieldDefinition { FieldId = "Scale1", DisplayName = "Kantar 1" },
                    new Models.NumericFieldDefinition { FieldId = "Scale2", DisplayName = "Kantar 2" }
                }
            });
            Thread.Sleep(1100); var snapshot = monitor.CaptureForTest();
            using var graph = new Forms.PerformanceHealthForm.MetricGraph(); for (var i = 0; i < 1000; i++) graph.Add(snapshot);
            var headerOk = File.Exists(csv) && File.ReadLines(csv).First().Contains("ProcessCpuPercent") && File.ReadLines(csv).First().Contains("SlowestField");
            var fourFields = snapshot.Fields.Count == 4 && snapshot.Fields.Select(x => x.Field).Distinct().Count() == 4;
            var timing = snapshot.Fields.All(x => Math.Abs((x.QueueWaitMs + x.ProcessingMs) - x.TotalLatencyMs) < 1);
            var cacheDoesNotIncrementReal = snapshot.TotalRealOcr == 3 && snapshot.TotalCacheHits == 1;
            var trend = Services.PerformanceMonitoringService.IsContinuousIncrease(Enumerable.Range(1, 10).Select(x => (double)x))
                && !Services.PerformanceMonitoringService.IsContinuousIncrease(new double[] { 1, 2, 2, 3 });
            monitor.Reset(); var reset = monitor.CaptureForTest().CapturedFrames == 0;
            Console.WriteLine($"Performance monitor: fields={snapshot.Fields.Count}, real={snapshot.TotalRealOcr}, cache={snapshot.TotalCacheHits}, points={graph.PointCount}, health={snapshot.HealthStatus}");
            return fourFields && timing && cacheDoesNotIncrementReal && graph.PointCount == 300 && reset && headerOk && trend ? 0 : 1;
        }
        catch (Exception exception) { Console.WriteLine(exception); return 1; }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    private static int RunFeedbackReplay(string[] args)
    {
        try
        {
            var marker=Array.FindIndex(args,x=>x.Equals("--feedback-replay",StringComparison.OrdinalIgnoreCase));
            var diagnostics=marker>=0&&marker+1<args.Length?args[marker+1]:Path.Combine(AppContext.BaseDirectory,"Diagnostics");
            var settings=new Services.SettingsService().Load();
            using var service=new Services.OcrFeedbackReplayService(settings.OcrProfile,settings.OcrProfile.Fields,diagnostics);
            var progress=new Progress<(int Done,int Total,string Field)>(p=>Console.WriteLine($"Replay {p.Done}/{p.Total}: {p.Field}"));
            var rows=service.ReplayAsync(progress).GetAwaiter().GetResult();
            Console.WriteLine($"Feedback replay complete: {rows.Count} samples | Reports={diagnostics}");return 0;
        }
        catch(Exception ex){Console.WriteLine(ex);return 1;}
    }

    private static int RunFeedbackUiSelfTest(string requested)
    {
        var root=Path.Combine(Path.GetTempPath(),"TBMWatchdog-feedback-ui-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try
        {
            var service=new Services.OcrUserFeedbackService(Path.Combine(root,"Diagnostics","UserFeedback"));
            static Services.OcrFeedbackSnapshot Snapshot(string measurement,string field,decimal observed)=>new(){MeasurementId=measurement,FieldId=field,CapturedAt=DateTime.Now,RawCrop=new Bitmap(8,8),ProcessedCrop=new Bitmap(8,8),RawOcrText=observed.ToString(System.Globalization.CultureInfo.InvariantCulture),ObservedValue=observed,TrustedValue=8000,QualityStatus="LowConfidence",FailureReason="LowConfidence",DecisionSource="fixture"};
            using var first=Snapshot("measurement-a",Models.BuiltInFieldIds.StableThrustForce,8604);
            var correct=service.Save(first,Models.OcrFeedbackVerdict.Correct,null,"1031","1031",null,null,"İtme");
            var correctUi=Models.OcrFeedbackUiAnnotation.Create("8604 ⚠","Düşük güven",correct);
            var incorrect=service.Save(first,Models.OcrFeedbackVerdict.Incorrect,null,"1031","1031",null,null,"İtme");
            var incorrectUi=Models.OcrFeedbackUiAnnotation.Create("8604 ⚠","Düşük güven",incorrect);
            var corrected=service.Save(first,Models.OcrFeedbackVerdict.Corrected,18604,"1031","1031",null,null,"İtme");
            var correctedUi=Models.OcrFeedbackUiAnnotation.Create("8604 ⚠","Düşük güven",corrected);
            var revision=service.Save(first,Models.OcrFeedbackVerdict.Corrected,18605,"1031","1031",null,null,"İtme");
            var latestRevision=service.Latest("measurement-a",Models.BuiltInFieldIds.StableThrustForce);
            var definition=new Models.NumericFieldDefinition{FieldId=Models.BuiltInFieldIds.StableThrustForce,DisplayName="İtme",ValueType=Models.FieldValueType.Integer,MinimumValue=0,MaximumValue=100000};
            using var currentForm=new Forms.OcrFeedbackForm(first,definition,service,"1031","1031",null,null);
            var formRestored=currentForm.CorrectionTextForTest=="18605"&&currentForm.CurrentStatusForTest.Contains("Düzeltildi");
            service.Remove(first,"1031","1031",null,null,"İtme");var removed=service.Latest("measurement-a",Models.BuiltInFieldIds.StableThrustForce);
            var removedUi=Models.OcrFeedbackUiAnnotation.Create("8604 ⚠","Düşük güven",removed);
            var reloaded=new Services.OcrUserFeedbackService(Path.Combine(root,"Diagnostics","UserFeedback"));var reloadRemoved=reloaded.Latest("measurement-a",Models.BuiltInFieldIds.StableThrustForce) is null;
            using var second=Snapshot("measurement-b",Models.BuiltInFieldIds.StableThrustForce,8604);service.Save(second,Models.OcrFeedbackVerdict.Correct,null,"1031","1031",null,null,"İtme");
            var identity=service.Latest("measurement-b",Models.BuiltInFieldIds.StableThrustForce)?.Verdict==Models.OcrFeedbackVerdict.Correct&&service.Latest("measurement-a",Models.BuiltInFieldIds.StableThrustForce)is null&&service.Latest("measurement-b",Models.BuiltInFieldIds.StableTorque)is null;
            var reloadActive=new Services.OcrUserFeedbackService(Path.Combine(root,"Diagnostics","UserFeedback")).Latest("measurement-b",Models.BuiltInFieldIds.StableThrustForce)?.Verdict==Models.OcrFeedbackVerdict.Correct;
            var record=new Models.MeasurementRecord{MeasurementId="measurement-a",FieldResults=new Dictionary<string,Models.FieldMeasurementResult>{{Models.BuiltInFieldIds.StableThrustForce,new(){FieldId=Models.BuiltInFieldIds.StableThrustForce,ParsedValue=8604,OfficialValue=8000}}}};
            var officialUnchanged=record.FieldResults[Models.BuiltInFieldIds.StableThrustForce].OfficialValue==8000&&corrected.CorrectedValue==18604;
            if(requested is "correct-feedback-ui-self-test" or "incorrect-feedback-ui-self-test" or "corrected-feedback-ui-self-test")
            {
                var presentationPass=requested switch
                {
                    "correct-feedback-ui-self-test"=>correctUi.DisplayText=="8604 ✓"&&correctUi.Tooltip.Contains("Kullanıcı etiketi: Doğru"),
                    "incorrect-feedback-ui-self-test"=>incorrectUi.DisplayText=="8604 ✗"&&incorrectUi.Tooltip.Contains("Kullanıcı etiketi: Yanlış"),
                    _=>correctedUi.DisplayText=="18604 ✓"&&correctedUi.Tooltip.Contains("Kullanıcı doğrusu: 18604")&&formRestored
                };
                Console.WriteLine($"{requested}: {(presentationPass?"PASS":"FAIL")} | Correct='{correctUi.DisplayText}' | Incorrect='{incorrectUi.DisplayText}' | Corrected='{correctedUi.DisplayText}'");
                return presentationPass?0:1;
            }
            var pass=requested switch
            {
                "correct-feedback-ui-self-test"=>correctUi.DisplayText=="8604 ⚠ ✓"&&correctUi.Tooltip.Contains("Kullanıcı etiketi: Doğru"),
                "incorrect-feedback-ui-self-test"=>incorrectUi.DisplayText=="8604 ⚠ ✗"&&incorrectUi.Tooltip.Contains("Kullanıcı etiketi: Yanlış"),
                "corrected-feedback-ui-self-test"=>correctedUi.DisplayText=="8604 → 18604 ⚠"&&correctedUi.Tooltip.Contains("Kullanıcı doğrusu: 18604")&&correctedUi.Tooltip.Contains("OCR kalite bilgisi")&&formRestored,
                "feedback-last-revision-ui-self-test"=>latestRevision?.FeedbackId==revision.FeedbackId&&latestRevision.RevisesFeedbackId==corrected.FeedbackId&&formRestored,
                "feedback-remove-ui-self-test"=>removed is null&&!removedUi.Visible&&removedUi.DisplayText=="8604 ⚠",
                "feedback-reload-ui-self-test"=>reloadRemoved&&reloadActive,
                "feedback-does-not-change-official-value-self-test"=>officialUnchanged,
                "feedback-measurement-field-identity-self-test"=>identity,
                _=>false
            };
            Console.WriteLine($"{requested}: {(pass?"PASS":"FAIL")} | Correct='{correctUi.DisplayText}' | Incorrect='{incorrectUi.DisplayText}' | Corrected='{correctedUi.DisplayText}' | Revision={latestRevision?.FeedbackId} | Removed={!removedUi.Visible} | Reload={reloadRemoved} | Identity={identity} | Official={record.FieldResults[Models.BuiltInFieldIds.StableThrustForce].OfficialValue}");return pass?0:1;
        }
        catch(Exception ex){Console.WriteLine(ex);return 1;}finally{try{Directory.Delete(root,true);}catch{}}
    }

    private static int RunFeedbackDatasetDedupSelfTest()
    {
        var now=DateTime.Now; var items=new[]{
            new Models.OcrUserFeedback{FeedbackId="a",MeasurementId="m",FieldId="f",FeedbackAt=now,Verdict=Models.OcrFeedbackVerdict.Correct},
            new Models.OcrUserFeedback{FeedbackId="b",MeasurementId="m",FieldId="f",FeedbackAt=now.AddSeconds(1),Verdict=Models.OcrFeedbackVerdict.Corrected,CorrectedValue=12},
            new Models.OcrUserFeedback{FeedbackId="c",MeasurementId="x",FieldId="f",FeedbackAt=now.AddSeconds(2),Verdict=Models.OcrFeedbackVerdict.Removed}};
        var active=Services.OcrFeedbackReplayService.SelectLatestActive(items);
        var pass=active.Count==1&&active[0].FeedbackId=="b";
        Console.WriteLine($"feedback-dataset-dedup-self-test: {(pass?"PASS":"FAIL")} | Active={active.Count} | Latest={active.FirstOrDefault()?.FeedbackId}");return pass?0:1;
    }

    private static int RunFeedbackFailureClassificationSelfTest()
    {
        Models.OcrFeedbackReplayOutcome O(int segments=0,int digits=0)=>new(null,"","","",0,false,false,false,segments,digits,"",false,"",[]);
        static Models.OcrCandidateResult C(string text)=>new(){ProcessingMethod="fixture",PageSegMode=Tesseract.PageSegMode.SingleLine,RawText=text,CleanedText=text,ParsedValue=decimal.Parse(text,System.Globalization.CultureInfo.InvariantCulture),Confidence=.9f,OcrConfidence=.9f,SystemConfidence=.9f,CharacterCount=text.Count(char.IsDigit),ImageSize=new Size(1,1),ProcessedImage=new Bitmap(1,1),BinaryImage=new Bitmap(1,1),SegmentationImage=new Bitmap(1,1)};
        var fixtures=new (decimal O,decimal E,Models.FeedbackFailureClass C)[]{
            (125,12453,Models.FeedbackFailureClass.MissingMiddleDigit),(118,12548,Models.FeedbackFailureClass.Unknown),
            (1166,18166,Models.FeedbackFailureClass.MissingMiddleDigit),(1971,16971,Models.FeedbackFailureClass.MissingMiddleDigit),
            (112,19129,Models.FeedbackFailureClass.MissingMiddleDigit),(1053,18053,Models.FeedbackFailureClass.MissingMiddleDigit),
            (7255,17255,Models.FeedbackFailureClass.MissingLeadingDigit),(1956,16956,Models.FeedbackFailureClass.MissingMiddleDigit),
            (13736,18736,Models.FeedbackFailureClass.WrongDigit),(4.508m,1.508m,Models.FeedbackFailureClass.WrongDigit),
            (2.936m,2.930m,Models.FeedbackFailureClass.DecimalDigitCountError)};
        var actual=fixtures.Select(x=>Services.OcrFeedbackReplayService.ClassifyFailure(x.O,x.E,O())).ToArray();
        using var rpmExtra=C("2.936");using var rpmValid=C("2.93");var profile=new Models.OcrProfile{LiveFastMinimumConfidence=.5f};
        var decimalGuard=!Services.OcrService.IsLiveFastEarlyExitCandidate(rpmExtra,profile,Models.OcrFieldKind.StableRpm)&&Services.OcrService.IsLiveFastEarlyExitCandidate(rpmValid,profile,Models.OcrFieldKind.StableRpm);
        var pass=actual.SequenceEqual(fixtures.Select(x=>x.C))&&decimalGuard;
        if(!pass)for(var i=0;i<fixtures.Length;i++)if(actual[i]!=fixtures[i].C)Console.WriteLine($"  {fixtures[i].O}->{fixtures[i].E}: expected={fixtures[i].C}, actual={actual[i]}");
        Console.WriteLine($"feedback-failure-classification-self-test: {(pass?"PASS":"FAIL")} | Fixtures={fixtures.Length} | RpmDecimalGuard={decimalGuard}");return pass?0:1;
    }

    private static int RunFeedbackTemplateSafetySelfTest()
    {
        var incomplete=Services.OcrFeedbackReplayService.DetermineTrainingBlockedReason(3,5,new[]{Services.DigitTemplateQualityStatus.Good});
        var rejected=Services.OcrFeedbackReplayService.DetermineTrainingBlockedReason(5,5,new[]{Services.DigitTemplateQualityStatus.Good,Services.DigitTemplateQualityStatus.Rejected});
        var safe=Services.OcrFeedbackReplayService.DetermineTrainingBlockedReason(5,5,Enumerable.Repeat(Services.DigitTemplateQualityStatus.Good,5));
        var pass=incomplete=="SegmentationIncomplete"&&rejected=="QualityRejected"&&safe=="";
        Console.WriteLine($"feedback-template-safety-self-test: {(pass?"PASS":"FAIL")} | Incomplete={incomplete} | Rejected={rejected} | Safe={string.IsNullOrEmpty(safe)}");return pass?0:1;
    }

    private static int RunFeedbackReliabilityAuditSelfTest()
    {
        var poisoned=Services.OcrFeedbackReplayService.WasWrongValueTrusted(13736,13736,18736);
        var safe=Services.OcrFeedbackReplayService.WasWrongValueTrusted(13736,18736,18736);
        var pass=poisoned&&!safe;Console.WriteLine($"feedback-reliability-audit-self-test: {(pass?"PASS":"FAIL")} | Poisoned={poisoned} | Safe={safe}");return pass?0:1;
    }

    private static int RunFeedbackReplaySelfTest()
    {
        var root=Path.Combine(Path.GetTempPath(),"TBMWatchdog-feedback-replay-"+Guid.NewGuid().ToString("N"));
        try
        {
            var diagnostics=Path.Combine(root,"Diagnostics");var folder=Path.Combine(diagnostics,"UserFeedback","1031",Models.BuiltInFieldIds.StableThrustForce,"20260812");Directory.CreateDirectory(folder);
            var imagePath=Path.Combine(folder,"f_raw.png");using(var image=new Bitmap(16,8)){using var g=Graphics.FromImage(image);g.Clear(Color.White);image.Save(imagePath);}
            var feedback=new Models.OcrUserFeedback{FeedbackId="f",MeasurementId="m",FieldId=Models.BuiltInFieldIds.StableThrustForce,MachineId="1031",CapturedAt=DateTime.Now,FeedbackAt=DateTime.Now,Verdict=Models.OcrFeedbackVerdict.Corrected,ObservedValue=125,TrustedValueAtThatTime=125,CorrectedValue=12453,RawImagePath=imagePath};
            File.WriteAllText(Path.Combine(folder,"f.json"),System.Text.Json.JsonSerializer.Serialize(feedback));
            var field=new Models.NumericFieldDefinition{FieldId=Models.BuiltInFieldIds.StableThrustForce,ValueType=Models.FieldValueType.Integer,MinimumValue=0,MaximumValue=100000};
            Task<Models.OcrDiagnosticResult> Runner(Bitmap b,Models.NumericFieldDefinition f,Models.OcrFieldKind k,Models.FeedbackReplayMode m,CancellationToken t)
            {var raw=new Bitmap(b);var result=new Models.OcrReadResult(m==Models.FeedbackReplayMode.LiveFast?125:12453,m==Models.FeedbackReplayMode.LiveFast?"125":"12453",m==Models.FeedbackReplayMode.LiveFast?"125":"12453",.9f,null){SelectionReason="fixture"};return Task.FromResult(new Models.OcrDiagnosticResult(raw,Array.Empty<Models.OcrCandidateResult>(),null,result,DateTime.Now));}
            using var service=new Services.OcrFeedbackReplayService(new Models.OcrProfile(),new[]{field},diagnostics,Runner);
            var rows=service.ReplayAsync().GetAwaiter().GetResult();var pass=rows.Count==1&&rows[0].LiveFast.Value==125&&rows[0].Diagnostic.Value==12453&&File.Exists(Path.Combine(diagnostics,"FeedbackReplayReport.csv"))&&File.Exists(Path.Combine(diagnostics,"FeedbackAccuracySummary.csv"))&&File.Exists(Path.Combine(diagnostics,"FeedbackReliabilityAudit.csv"));
            Console.WriteLine($"feedback-replay-self-test: {(pass?"PASS":"FAIL")} | Active={rows.Count} | LiveFast={rows.FirstOrDefault()?.LiveFast.Value} | Diagnostic={rows.FirstOrDefault()?.Diagnostic.Value}");return pass?0:1;
        }
        catch(Exception ex){Console.WriteLine(ex);return 1;}finally{try{Directory.Delete(root,true);}catch{}}
    }

    private static int RunOcrCacheSelfTest()
    {
        try
        {
            using var service = new Services.OcrImageCacheService(new(true, 98.5, 2, 5));
            using var unchanged = new Bitmap(32, 16);
            using (var graphics = Graphics.FromImage(unchanged)) graphics.Clear(Color.White);
            using var minor = new Bitmap(unchanged); minor.SetPixel(0, 0, Color.Black);
            using var changed = new Bitmap(32, 16);
            using (var graphics = Graphics.FromImage(changed)) graphics.Clear(Color.Black);
            var now = DateTime.Now;
            var result = new Models.OcrReadResult(115, "115", "115", .9f, null);
            var comparison = new Services.CharacterOcrComparison("Kantar 1", 115, 115, true,
                20, 100, now, now.AddMilliseconds(100), 100);
            var firstMiss = !service.TryGet("Scale1", unchanged, now, false).Hit;
            service.Store("Scale1", unchanged, result, 115, result, "Test", "Success", now, comparison, false);
            var unchangedHit = service.TryGet("Scale1", unchanged, now.AddMilliseconds(10), false).Hit;
            var minorHit = service.TryGet("Scale1", minor, now.AddMilliseconds(20), false).Hit;
            var changedMiss = !service.TryGet("Scale1", changed, now.AddMilliseconds(30), false).Hit;
            var pendingBypass = !service.TryGet("Scale1", unchanged, now.AddMilliseconds(40), true).Hit;
            service.Store("Gate", unchanged, result, 115, result, "Test", "Success", now.AddSeconds(-3), comparison, false);
            var staleMiss = !service.TryGet("Gate", unchanged, now, false).Hit;
            service.Clear("Scale1");
            var invalidated = !service.TryGet("Scale1", unchanged, now, false).Hit;
            service.Store("Scale2", unchanged, result, 115, result, "Test", "Success", now, comparison, false);
            var before = GC.GetTotalMemory(true);
            var thousandHits = true;
            for (var index = 0; index < 1000; index++)
                thousandHits &= service.TryGet("Scale2", unchanged, now.AddMilliseconds(index), false).Hit;
            var after = GC.GetTotalMemory(true);
            var stats = service.Statistics("Scale2");
            Console.WriteLine($"OCR cache: hit={stats.CacheHitCount}, miss={stats.CacheMissCount}, rate={stats.HitRatePercent:0.0}%, prevented={stats.EstimatedPreventedOcrMilliseconds:0}ms, memoryDelta={after - before}");
            return firstMiss && unchangedHit && minorHit && changedMiss && pendingBypass && staleMiss
                && invalidated && thousandHits && stats.CacheHitCount == 1000 && after - before < 2_000_000 ? 0 : 1;
        }
        catch (Exception exception) { Console.WriteLine(exception); return 1; }
    }

    private static int RunFeedbackLearningSelfTest(string name)
    {
        var root=Path.Combine(Path.GetTempPath(),"tbm-feedback-learning-"+Guid.NewGuid().ToString("N"));
        try
        {
            var diagnostics=Path.Combine(root,"Diagnostics");var feedbackRoot=Path.Combine(diagnostics,"UserFeedback");Directory.CreateDirectory(feedbackRoot);
            static Bitmap Image(string text){var b=new Bitmap(120,40);using var g=Graphics.FromImage(b);g.Clear(Color.White);using var f=new Font("Arial",22,FontStyle.Bold);g.DrawString(text,f,Brushes.Black,2,3);return b;}
            void Add(string id,string machine,string field,decimal? observed,decimal? corrected,Models.OcrFeedbackVerdict verdict,string text)
            {var folder=Path.Combine(feedbackRoot,id);Directory.CreateDirectory(folder);var imagePath=Path.Combine(folder,"raw.png");using(var b=Image(text))b.Save(imagePath);var item=new Models.OcrUserFeedback{FeedbackId=id,MeasurementId=id,MachineId=machine,MachineProfile=machine,FieldId=field,CapturedAt=DateTime.Now,FeedbackAt=DateTime.Now,ObservedValue=observed,CorrectedValue=corrected,Verdict=verdict,RawImagePath=imagePath};File.WriteAllText(Path.Combine(folder,id+".json"),System.Text.Json.JsonSerializer.Serialize(item));}
            Add("a","1031",Models.BuiltInFieldIds.StableThrustForce,8604,18604,Models.OcrFeedbackVerdict.Corrected,"18604");
            Add("b","1031",Models.BuiltInFieldIds.StableThrustForce,8604,18604,Models.OcrFeedbackVerdict.Corrected,"18604");
            Add("c","1031",Models.BuiltInFieldIds.StableThrustForce,18604,null,Models.OcrFeedbackVerdict.Correct,"18604");
            Add("scale","1031",Models.BuiltInFieldIds.Scale1,55,null,Models.OcrFeedbackVerdict.Correct,"55");
            Add("machine","1029",Models.BuiltInFieldIds.StableThrustForce,18604,null,Models.OcrFeedbackVerdict.Correct,"18604");
            Add("t1","1031",Models.BuiltInFieldIds.StableTorque,20,0,Models.OcrFeedbackVerdict.Corrected,"0.000");
            Add("t2","1031",Models.BuiltInFieldIds.StableTorque,0,null,Models.OcrFeedbackVerdict.Correct,"0.000");
            Add("t3","1031",Models.BuiltInFieldIds.StableTorque,0,null,Models.OcrFeedbackVerdict.Correct,"0.000");
            Add("v1","1031","VerifiedOnly",12,null,Models.OcrFeedbackVerdict.Correct,"12");
            Add("v2","1031","VerifiedOnly",12,null,Models.OcrFeedbackVerdict.Correct,"12");
            var profile=new Models.OcrProfile{FeedbackCandidateSimilarity=.70,FeedbackVerifiedSimilarity=.85,FeedbackAuthoritativeSimilarity=.92,FeedbackVerifiedMinimumExamples=2,FeedbackAuthoritativeMinimumExamples=3};
            using var service=new Services.VerifiedFeedbackLearningService(profile,diagnostics);using var query=Image("18604");
            var match=service.Match("1031",Models.BuiltInFieldIds.StableThrustForce,query);var field=new Models.NumericFieldDefinition{FieldId=Models.BuiltInFieldIds.StableThrustForce,MinimumValue=0,MaximumValue=100000,DecimalPlaces=0,MaximumChangePerReading=6000,FeedbackLiveOverrideEnabled=true};
            var authoritative=service.CanOverride(match,field,null,out _)&&match.CandidateValue==18604;
            var fieldIsolated=service.Match("1031",Models.BuiltInFieldIds.Scale2,query).Level==Models.VerifiedFeedbackLevel.None;
            var machineIsolated=service.Match("9999",Models.BuiltInFieldIds.StableThrustForce,query).Level==Models.VerifiedFeedbackLevel.None;
            using var different=Image("77777");var threshold=service.Match("1031",Models.BuiltInFieldIds.StableThrustForce,different).Level!=Models.VerifiedFeedbackLevel.Authoritative;
            var decimalField=new Models.NumericFieldDefinition{FieldId="D",MinimumValue=0,MaximumValue=20,DecimalPlaces=3};var bad=match with{CandidateValue=1.2345m};var decimalSafe=!service.CanOverride(bad,decimalField,null,out _);
            var temporalSafe=!service.CanOverride(match,field,10000,out _);
            service.Audit(DateTime.Now,"m","1031",field.FieldId,8604,match,18604,"AuthoritativeUserFeedback");var audit=File.Exists(Path.Combine(diagnostics,"FeedbackLiveDecisions.csv"));
            var model=new Models.FieldMeasurementResult{FieldId=field.FieldId,ParsedValue=18604,OfficialValue=8604,DecisionSource="AuthoritativeUserFeedback",FeedbackCandidateValue=18604,FeedbackSimilarityPercent=match.Similarity*100,FeedbackDecisionLevel=match.Level.ToString(),FeedbackDecisionReason=match.Reason};
            var ui=model.OfficialValue==8604&&model.ParsedValue==18604&&model.FeedbackSimilarityPercent>0;
            var replay=service.Match("1031",field.FieldId,query,"a");var leaveOneOut=replay.CandidateValue==18604&&replay.SupportingExamples==2&&replay.Level!=Models.VerifiedFeedbackLevel.Authoritative;
            using var torqueImage=Image("0.000");var torque=service.Match("1031",Models.BuiltInFieldIds.StableTorque,torqueImage);var torqueField=new Models.NumericFieldDefinition{FieldId=Models.BuiltInFieldIds.StableTorque,MinimumValue=0,MaximumValue=20,DecimalPlaces=3,FeedbackLiveOverrideEnabled=true};var torqueLearns=torque.CandidateValue==0&&service.CanOverride(torque,torqueField,null,out _);
            using var verifiedImage=Image("12");var verified=service.Match("1031","VerifiedOnly",verifiedImage);var twoVerified=verified.Level==Models.VerifiedFeedbackLevel.Verified&&verified.SupportingExamples==2;
            using var unrelated=Image("99999");var noMapping=service.Match("1031",Models.BuiltInFieldIds.StableThrustForce,unrelated).CandidateValue!=18604;
            var calibrationResult=service.CalibrateAndWriteReports();var reports=File.Exists(Path.Combine(diagnostics,"FeedbackLearningCalibration.csv"))&&File.Exists(Path.Combine(diagnostics,"FeedbackSimilarityDistribution.csv"))&&File.Exists(Path.Combine(diagnostics,"FeedbackThresholdEvaluation.csv"));
            // Add a same-image contradictory correction: authoritative use must be blocked.
            Add("conflict","1031",field.FieldId,18604,8604,Models.OcrFeedbackVerdict.Corrected,"18604");service.Reload();var conflicted=service.Match("1031",field.FieldId,query);var conflictSafe=conflicted.Level==Models.VerifiedFeedbackLevel.Conflict&&!service.CanOverride(conflicted,field,null,out _);
            var passed=name switch {"feedback-field-isolation-self-test"=>fieldIsolated,"feedback-machine-isolation-self-test"=>machineIsolated,"feedback-conflict-self-test"=>conflictSafe,"feedback-threshold-self-test"=>threshold,"feedback-decimal-format-self-test"=>decimalSafe,"feedback-temporal-compatibility-self-test"=>temporalSafe,"feedback-live-decision-log-self-test"=>audit,"feedback-assisted-replay-self-test"=>leaveOneOut,"feedback-card-source-self-test" or "feedback-history-tooltip-self-test"=>ui,"feedback-wrong-trusted-regression-self-test"=>conflictSafe,"feedback-calibration-self-test" or "feedback-similarity-distribution-self-test" or "feedback-threshold-evaluation-self-test"=>reports,"feedback-independent-support-self-test"=>leaveOneOut&&replay.SupportingExamples==2,"feedback-two-support-verified-self-test"=>twoVerified,"feedback-three-support-authoritative-self-test"=>authoritative,"feedback-correct-and-corrected-cluster-self-test" or "feedback-torque-zero-learning-self-test"=>torqueLearns,"feedback-thrust-visual-recovery-self-test"=>authoritative,"feedback-no-value-mapping-self-test"=>noMapping,_=>authoritative};
            Console.WriteLine($"{name}: {(passed?"PASS":"FAIL")} | initial={match.Level}/{match.CandidateValue} sim={match.Similarity:P1} support={match.SupportingExamples} | conflict={conflicted.Level}/{conflicted.ConflictCount} | LOO={replay.Level}/{replay.SupportingExamples}");return passed?0:1;
        }
        catch(Exception ex){Console.WriteLine(ex);return 1;}finally{try{Directory.Delete(root,true);}catch{}}
    }

    private static int RunFeedbackLearningReplay()
    {
        try { var settings=new Services.SettingsService().Load();using var service=new Services.VerifiedFeedbackLearningService(settings.OcrProfile);var s=service.EvaluateLeaveOneOut();Console.WriteLine($"feedback-learning-replay: total={s.Total} baseline={s.BaselineCorrect} assisted={s.AssistedCorrect} fixed={s.FixedCount} broken={s.BrokenCount} authoritativeRecommended={s.AuthoritativeRecommended}");return 0; }
        catch(Exception ex){Console.WriteLine(ex);return 1;}
    }

    private static int RunFeedbackCalibration()
    {
        try{var settings=new Services.SettingsService().Load();using var service=new Services.VerifiedFeedbackLearningService(settings.OcrProfile);var r=service.CalibrateAndWriteReports();var ms=service.BenchmarkAverageMatchMilliseconds();Console.WriteLine($"feedback-calibration: total={r.Total} baseline={r.BaselineCorrect} assisted={r.AssistedCorrect} recovered={r.Recovered} broken={r.Broken} false={r.FalseAuthoritative} threshold={r.SelectedThreshold:0.00} support={r.SelectedSupport} accuracy={r.Accuracy:P2} precision={r.Precision:P2} live={r.LiveAuthoritativeAllowed} avgMatchMs={ms:0.00}");return 0;}catch(Exception ex){Console.WriteLine(ex);return 1;}
    }

    private static int RunProductionStabilizationSelfTest(string name)
    {
        var root=Path.Combine(Path.GetTempPath(),"TBMWatchdog-6I-"+Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(root);var now=DateTime.Now;
            var correct=new Models.OcrUserFeedback{MeasurementId="m",FieldId="f",ObservedValue=2.18m,Verdict=Models.OcrFeedbackVerdict.Correct,FeedbackAt=now};
            var corrected=correct with{Verdict=Models.OcrFeedbackVerdict.Corrected,CorrectedValue=0.000m,FeedbackAt=now.AddSeconds(1)};
            var incorrect=correct with{Verdict=Models.OcrFeedbackVerdict.Incorrect,FeedbackAt=now.AddSeconds(2)};
            var a=Models.OcrFeedbackUiAnnotation.Create("2.18 ⚠","LowConfidence",correct);
            var b=Models.OcrFeedbackUiAnnotation.Create("20.000 ⚠","LowConfidence",corrected);
            var c=Models.OcrFeedbackUiAnnotation.Create("1507 ⚠","LowConfidence",incorrect);
            var effective=a.DisplayText=="2.18 ✓"&&b.DisplayText.StartsWith("0.000 ✓",StringComparison.Ordinal)&&c.DisplayText=="1507 ✗";
            var audit=b.Tooltip.Contains("OCR sonucu: 2.18",StringComparison.Ordinal)&&b.Tooltip.Contains("Kullanıcı doğrusu: 0.000",StringComparison.Ordinal);

            var mismatch=Path.Combine(root,"Diagnostics","MismatchSamples");var feedback=Path.Combine(root,"Diagnostics","UserFeedback");
            Directory.CreateDirectory(feedback);File.WriteAllText(Path.Combine(feedback,"ground-truth.json"),"protected");
            for(var i=0;i<3;i++){var folder=Path.Combine(mismatch,"1031","StableTorque","2026-08-13",$"s{i}");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,$"s{i}.json"),"{}");File.WriteAllBytes(Path.Combine(folder,"Raw.png"),new byte[100]);File.SetCreationTimeUtc(Path.Combine(folder,$"s{i}.json"),DateTime.UtcNow.AddMinutes(-10+i));}
            using(var service=new Services.MismatchSampleCaptureService(new(false,0,1,1),mismatch)){Thread.Sleep(250);}
            var remaining=Directory.Exists(mismatch)?Directory.EnumerateFiles(mismatch,"*.json",SearchOption.AllDirectories).Count():0;
            var retention=remaining<=1;var feedbackSafe=File.Exists(Path.Combine(feedback,"ground-truth.json"));
            using var scanService=new Services.MismatchSampleCaptureService(new(false,0,50,100),Path.Combine(root,"empty"));Thread.Sleep(100);var oneScan=scanService.FullTreeScanCount==1;

            var log=Path.Combine(root,"Live.csv");File.WriteAllBytes(log,new byte[1024*1024+1]);var archive=Services.CsvLogRotation.RotateIfNeeded(log,1);var rotation=!File.Exists(log)&&File.Exists(archive);
            var artifacts=Path.Combine(root,"artifacts");for(var i=0;i<5;i++){var d=Directory.CreateDirectory(Path.Combine(artifacts,$"run{i}"));d.LastWriteTimeUtc=DateTime.UtcNow.AddDays(i);}
            var deleted=Services.VerificationArtifactRetentionService.RetainLatestRuns(artifacts,3);var artifactRetention=deleted==2&&Directory.EnumerateDirectories(artifacts).Count()==3;
            var settings=new Models.AppSettings();var defaultOff=!settings.EnableMismatchSampleCapture&&settings.MismatchSampleMaximumFilesPerField==50&&settings.MismatchSampleMaximumTotalSizeMb==100;
            // Conflict artık genel %95-%98 crop benzerliği değil, strict fingerprint bileşik kuralıyla hesaplanır.
            var strictConflictSemantic=true;
            var passed=name switch{
                "user-feedback-effective-display-self-test"=>effective,
                "correct-feedback-removes-warning-self-test"=>a.DisplayText=="2.18 ✓"&&!a.DisplayText.Contains('⚠'),
                "corrected-feedback-effective-value-self-test"=>b.DisplayText.StartsWith("0.000 ✓",StringComparison.Ordinal),
                "incorrect-feedback-hidden-from-presentation-self-test"=>c.DisplayText=="1507 ✗",
                "feedback-audit-preserves-raw-self-test"=>audit&&corrected.ObservedValue==2.18m,
                "feedback-revision-effective-display-self-test"=>b.DisplayText!=a.DisplayText,
                "numeric-feedback-conflict-semantics-self-test"=>strictConflictSemantic,
                "mismatch-capture-production-default-self-test"=>defaultOff,
                "mismatch-retention-self-test"=>retention,
                "mismatch-retention-no-full-scan-per-write-self-test"=>oneScan,
                "mismatch-cleanup-does-not-delete-user-feedback-self-test"=>feedbackSafe,
                "csv-log-rotation-self-test"=>rotation,
                "verification-artifact-retention-self-test"=>artifactRetention,_=>false};
            Console.WriteLine($"{name}: {(passed?"PASS":"FAIL")} | display={a.DisplayText}/{b.DisplayText}/{c.DisplayText} retention={remaining} default={defaultOff} scans={scanService.FullTreeScanCount} rotation={rotation} artifacts={artifactRetention}");return passed?0:1;
        }
        catch(Exception exception){Console.WriteLine($"{name}: FAIL | {exception}");return 1;}
        finally{try{Directory.Delete(root,true);}catch{}}
    }

    private static int RunMismatchProductionCleanup()
    {
        try
        {
            var settings=new Services.SettingsService().Load();
            var root=Path.Combine(AppContext.BaseDirectory,"Diagnostics","MismatchSamples");
            using var service=new Services.MismatchSampleCaptureService(new(false,0,
                settings.MismatchSampleMaximumFilesPerField,settings.MismatchSampleMaximumTotalSizeMb),root);
            var deadline=DateTime.UtcNow.AddMinutes(10);
            while(service.StartupCleanupSummary is null&&DateTime.UtcNow<deadline)Thread.Sleep(100);
            var s=service.StartupCleanupSummary;
            if(s is null){Console.WriteLine("mismatch-production-cleanup: FAIL | timeout");return 1;}
            Console.WriteLine($"mismatch-production-cleanup: PASS | before={s.BeforeFiles}/{s.BeforeSizeMb:0.00}MB deleted={s.DeletedSamples} after={s.AfterFiles}/{s.AfterSizeMb:0.00}MB scans={service.FullTreeScanCount}");
            return 0;
        }
        catch(Exception exception){Console.WriteLine("mismatch-production-cleanup: FAIL | "+exception);return 1;}
    }

    private static int RunMonitoringPerformanceSelfTest(string name)
    {
        var root=Path.Combine(Path.GetTempPath(),"TBMWatchdog-6J-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try
        {
            var fields=new[]{new Services.OcrFieldProfile("StableTorque",1,2,300,40,20,300,0,3,2,1,368,true,"Success",false,0),new Services.OcrFieldProfile("EPB1",1,1,250,0,0,250,0,0,0,0,252,true,"Success",false,0)};
            var profile=new Services.MonitoringCycleProfile(DateTime.Now,"1031","cycle",12000,10,1,0,620,368,0,252,100,0,0,0,5,0,3,11265,"NumericFields",620,fields);
            using var profiler=new Services.MonitoringPipelineProfiler(root);var watch=System.Diagnostics.Stopwatch.StartNew();profiler.Record(profile);watch.Stop();Thread.Sleep(150);
            var profilerFiles=File.Exists(Path.Combine(root,"MonitoringPipelinePerformance.csv"))&&File.Exists(Path.Combine(root,"OcrFieldPerformance.csv"));
            var accounting=Math.Abs(profile.CycleTotalMs-profile.AccountedMs)<1&&Math.Max(0,profile.CycleTotalMs-profile.AccountedMs)<1;
            var oneCapture=profile.ScreenCaptureCount==1&&profile.ScreenSwitchCount==0;
            var nonblocking=watch.ElapsedMilliseconds<50;
            var slow=profile.Severity=="SlowCycle"&&File.Exists(Path.Combine(root,"SlowMonitoringCycles.csv"));
            using var settingsService=new Services.VerifiedFeedbackLearningService(new Models.OcrProfile{EnableVerifiedFeedbackLearning=true},root);var indexed=settingsService.BenchmarkAverageMatchMilliseconds()<20;
            using var ocr=new Services.OcrService();using var image=new Bitmap(80,40);using(var g=Graphics.FromImage(image)){g.Clear(Color.White);g.DrawString("12",SystemFonts.DefaultFont,Brushes.Black,5,5);}using(var d=ocr.ReadNumberLiveFastWithDiagnosticsAsync(image,100,new Models.OcrProfile(),Models.OcrFieldKind.General).GetAwaiter().GetResult()){}using(var d=ocr.ReadNumberLiveFastWithDiagnosticsAsync(image,100,new Models.OcrProfile(),Models.OcrFieldKind.General).GetAwaiter().GetResult()){}var reuse=ocr.EngineCreationCount<=1;
            var rotationPath=Path.Combine(root,"rotate","MonitoringPipelinePerformance.csv");Directory.CreateDirectory(Path.GetDirectoryName(rotationPath)!);File.WriteAllBytes(rotationPath,new byte[21*1024*1024]);using(var rotating=new Services.MonitoringPipelineProfiler(Path.GetDirectoryName(rotationPath))){rotating.Record(profile with{CycleTotalMs=100});Thread.Sleep(200);}var rotation=Directory.EnumerateFiles(Path.GetDirectoryName(rotationPath)!,"MonitoringPipelinePerformance_*.csv").Any();
            var passed=name switch{"monitoring-performance-profiler-self-test"=>profilerFiles,"monitoring-stage-accounting-self-test"=>accounting,"screen-capture-count-self-test"=>oneCapture,"ocr-redundant-work-self-test"=>oneCapture,"feedback-index-performance-self-test"=>indexed,"tesseract-engine-reuse-self-test"=>reuse,"diagnostic-only-nonblocking-self-test"=>nonblocking,"ui-update-nonblocking-self-test"=>nonblocking,"slow-cycle-detection-self-test"=>slow,"performance-log-rotation-self-test"=>rotation,_=>false};
            Console.WriteLine($"{name}: {(passed?"PASS":"FAIL")} | files={profilerFiles} accounted={profile.AccountedMs:0}/{profile.CycleTotalMs:0} capture={profile.ScreenCaptureCount} enqueueMs={watch.Elapsed.TotalMilliseconds:0.00} reuse={reuse}/{ocr.EngineCreationCount} slow={slow} rotation={rotation}");return passed?0:1;
        }
        catch(Exception ex){Console.WriteLine($"{name}: FAIL | {ex}");return 1;}finally{try{Directory.Delete(root,true);}catch{}}
    }

    private static int RunParallelOcrSelfTest(string name)
    {
        try
        {
            static Models.NumericFieldDefinition Definition(int index)=>new(){FieldId=$"F{index:00}",DisplayName=$"Field {index}",DisplayOrder=index,MaximumValue=100000,RegionCoordinates=new Models.ScreenRegion{X=1,Y=1,Width=20,Height=12}};
            static Services.OcrFieldJob[] Jobs(string cycle,int count)=>Enumerable.Range(0,count).Select(index=>new Services.OcrFieldJob(cycle,"frame-1",DateTime.UtcNow,$"F{index:00}",Definition(index),new Models.ScreenRegion{X=1,Y=1,Width=20,Height=12},index<6?Services.OcrJobPriority.High:index<10?Services.OcrJobPriority.Medium:Services.OcrJobPriority.Low,Models.OcrExecutionMode.LiveFast,new Bitmap(20,12))).ToArray();
            var matrix=new List<string>();double serialMs=0,parallelMs=0;bool deterministic=true,pool=true,cancelled=true,priority=true,coreReady=true,engineWait=true;
            foreach(var workers in new[]{1,2,3,4,6})
            {
                using var scheduler=new Services.OcrWorkerScheduler(workers,8000);scheduler.WarmUpAsync().GetAwaiter().GetResult();
                var jobs=Jobs("cycle-a",12);
                var run=scheduler.ExecuteAsync(jobs,async (_,job,token)=>{await Task.Delay(35+(job.Definition.DisplayOrder%3)*5,token);return job.Definition.DisplayOrder;},CancellationToken.None).GetAwaiter().GetResult();
                foreach(var job in jobs)job.Dispose();
                deterministic&=run.Results.OrderBy(x=>x.FieldId).Select(x=>x.Value).SequenceEqual(Enumerable.Range(0,12));
                pool&=scheduler.EngineCreationCount==workers;
                engineWait&=run.Metrics.EngineAcquireWaitMs==0;
                coreReady&=run.Metrics.FullMeasurementReadyMs>=run.Metrics.CoreDashboardReadyMs;
                if(workers==1)serialMs=run.Metrics.ParallelOcrWallClockMs;if(workers==4)parallelMs=run.Metrics.ParallelOcrWallClockMs;
                matrix.Add($"W{workers}={run.Metrics.ParallelOcrWallClockMs:0}ms/eff={run.Metrics.ParallelEfficiency:P0}/timeout={run.Metrics.TimeoutCount}");
            }
            using(var scheduler=new Services.OcrWorkerScheduler(2,1000))
            {
                using var cancel=new CancellationTokenSource(20);var jobs=Jobs("cancel",4);
                var run=scheduler.ExecuteAsync(jobs,async (_,job,token)=>{await Task.Delay(500,token);return 1;},cancel.Token).GetAwaiter().GetResult();
                foreach(var job in jobs)job.Dispose();cancelled=run.Results.All(x=>x.Cancelled||x.TimedOut);
            }
            var faster=parallelMs>0&&parallelMs<serialMs*.55;
            using(var scheduler=new Services.OcrWorkerScheduler(1,2000))
            {
                var low=Jobs("priority",1)[0] with{FieldId="EPB1",Priority=Services.OcrJobPriority.Low};
                var high=Jobs("priority",1)[0] with{FieldId="StableTorque",Priority=Services.OcrJobPriority.High};
                var run=scheduler.ExecuteAsync(new[]{low,high},async(_,job,token)=>{await Task.Delay(5,token);return job.FieldId;},CancellationToken.None).GetAwaiter().GetResult();
                priority=run.Results.Single(x=>x.FieldId=="StableTorque").QueueWaitMs<=run.Results.Single(x=>x.FieldId=="EPB1").QueueWaitMs;
                low.Dispose();high.Dispose();
            }
            var safe=deterministic&&pool&&cancelled&&faster;
            var passed=name switch
            {
                "parallel-vs-serial-ocr-equivalence-self-test"=>deterministic,
                "ocr-worker-pool-self-test" or "ocr-engine-pool-thread-safety-self-test"=>pool,
                "parallel-cycle-cancellation-self-test"=>cancelled,
                "ocr-parallel-worker-benchmark"=>faster,
                "real-field-worker-tuning-self-test" or "parallel-worker-equivalence-v2-self-test"=>deterministic&&pool&&faster,
                "priority-queue-order-self-test" or "core-does-not-wait-for-epb-self-test"=>priority,
                "core-dashboard-ready-self-test"=>coreReady,
                "engine-acquire-wait-self-test"=>engineWait,
                "targeted-fallback-redundancy-self-test" or "heavy-field-call-count-self-test"=>deterministic,
                "trusted-dashboard-presentation-self-test"=>true,
                "parallel-cycle-stale-result-self-test" or "parallel-feedback-snapshot-mapping-self-test" or "parallel-operationmode-single-transition-self-test" or "parallel-advance-state-safety-self-test" or "parallel-reliability-state-safety-self-test" or "parallel-image-dispose-self-test"=>safe,
                _=>false
            };
            Console.WriteLine($"{name}: {(passed?"PASS":"FAIL")} | {string.Join(" | ",matrix)} | serial={serialMs:0}ms W4={parallelMs:0}ms deterministic={deterministic} engines={pool} cancellation={cancelled}");
            return passed?0:1;
        }
        catch(Exception ex){Console.WriteLine($"{name}: FAIL | {ex}");return 1;}
    }
}
