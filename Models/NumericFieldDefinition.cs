namespace TBMWatchdog.Models;

public enum FieldValueType { Integer, Decimal, Boolean, Text, State }
public enum MeasurementCategory { OcrMeasurement, DerivedMeasurement, OperationalEvent }
public enum MeasurementSourceType { Ocr, Derived, OperationalEvent, Manual }
public enum TemplateScope { FieldSpecific, SharedNumericDigit }

public sealed class NumericFieldDefinition
{
    public string FieldId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public MeasurementCategory Category { get; set; } = MeasurementCategory.OcrMeasurement;
    public string Unit { get; set; } = "";
    public FieldValueType ValueType { get; set; } = FieldValueType.Integer;
    public decimal? MinimumValue { get; set; }
    public decimal? MaximumValue { get; set; }
    public int DecimalPlaces { get; set; }
    public bool AllowDecimalSeparator { get; set; }
    public int MinimumExpectedDigits { get; set; } = 1;
    public int MaximumExpectedDigits { get; set; } = 8;
    public ScreenRegion RegionCoordinates { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
    public bool SupportsCharacterOcr { get; set; } = true;
    public bool SupportsTemplateTraining { get; set; }
    public bool UsesOfficialCharacterDecision { get; set; }
    public bool UsesSuspiciousValueFilter { get; set; }
    public decimal MaximumChangePerReading { get; set; }
    public int ConfirmationReadings { get; set; } = 2;
    public decimal ConfirmationTolerance { get; set; } = 2;
    public string TemplateFolderName { get; set; } = "";
    public TemplateScope TemplateScope { get; set; } = TemplateScope.FieldSpecific;
    public int DisplayOrder { get; set; }
    public string ReportGroup { get; set; } = "OCR";
    public string PreferredLiveOcrPipeline { get; set; } = "Gaussian + Median + Otsu/SingleLine";
    public string FallbackLiveOcrPipeline { get; set; } = "Nearest + Otsu/SingleWord";
    public int ExpectedDigitCountMin { get; set; } = 1;
    public int ExpectedDigitCountMax { get; set; } = 8;
    public double MergedSegmentWidthRatio { get; set; } = 1.45;
    public bool ProjectionSplitEnabled { get; set; }
    public double ProjectionValleyThreshold { get; set; } = 0.35;
    public double? FeedbackCandidateSimilarityOverride { get; set; }
    public double? FeedbackVerifiedSimilarityOverride { get; set; }
    public double? FeedbackAuthoritativeSimilarityOverride { get; set; }
    public bool FeedbackLiveOverrideEnabled { get; set; }
}

public static class BuiltInFieldIds
{
    public const string Nominal = "Nominal"; public const string Gate = "Gate";
    public const string Scale1 = "Scale1"; public const string Scale2 = "Scale2";
    public const string AdvanceMm = "AdvanceMm";
    public const string AdvanceGroupA = "AdvanceGroupA"; public const string AdvanceGroupB = "AdvanceGroupB";
    public const string AdvanceGroupC = "AdvanceGroupC"; public const string AdvanceGroupD = "AdvanceGroupD";
    public const string StableRpm = "StableRpm"; public const string StableTorque = "StableTorque";
    public const string StableThrustForce = "StableThrustForce"; public const string StableAdvanceSpeed = "StableAdvanceSpeed";
    public const string StablePenetration = "StablePenetration";
    public const string NominalInjection = "NominalInjection";
    public const string RealInjection = "RealInjection";
    public const string Epb1 = "Epb1"; public const string Epb2 = "Epb2"; public const string Epb3 = "Epb3";
    public const string Epb4 = "Epb4"; public const string Epb5 = "Epb5";
}

public sealed record FieldMeasurementResult
{
    public string FieldId { get; init; } = ""; public string? RawValue { get; init; }
    public decimal? ParsedValue { get; init; } public decimal? OfficialValue { get; init; }
    public decimal? FullLineResult { get; init; } public decimal? CharacterResult { get; init; }
    public string DecisionSource { get; init; } = ""; public OcrConfidenceStatus ConfidenceStatus { get; init; }
    public string FailureReason { get; init; } = ""; public DateTime CapturedAt { get; init; }
    public DateTime CompletedAt { get; init; } public double ProcessingDurationMs { get; init; }
    public double TotalLatencyMs { get; init; } public bool CacheUsed { get; init; }
    public double OcrConfidencePercent { get; init; }
    public decimal? ObservedValue { get; init; }
    public decimal? AcceptedValue { get; init; }
    public decimal? PendingValue { get; init; }
    public string ValidationState { get; init; } = "";
    public DateTime? ObservedAt { get; init; }
    public OcrConfidenceStatus ObservedQuality { get; init; }
    public decimal? TrustedValue { get; init; }
    public DateTime? TrustedAt { get; init; }
    public OcrConfidenceStatus TrustedQuality { get; init; }
    public string ReliabilityReason { get; init; } = "";
    public decimal? FeedbackCandidateValue { get; init; }
    public double FeedbackSimilarityPercent { get; init; }
    public double FeedbackConfidencePercent { get; init; }
    public int FeedbackSupportingExamples { get; init; }
    public int FeedbackConflictCount { get; init; }
    public string FeedbackDecisionLevel { get; init; } = "None";
    public string FeedbackDecisionReason { get; init; } = "";
}

public sealed record TimeSeriesMeasurement(DateTime Timestamp, string MachineId, string SessionId,
    string? RingId, string FieldId, decimal? Value, string Unit, string QualityStatus,
    string DecisionSource, bool IsOfficial, MeasurementSourceType SourceType);

public interface ITimeSeriesMeasurementRepository
{
    Task AppendAsync(TimeSeriesMeasurement measurement, CancellationToken cancellationToken = default);
    IAsyncEnumerable<TimeSeriesMeasurement> QueryAsync(string machineId, DateTime from, DateTime to,
        CancellationToken cancellationToken = default);
}
