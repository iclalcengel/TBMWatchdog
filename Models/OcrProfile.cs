namespace TBMWatchdog.Models;

public sealed class OcrProfile
{
    public bool EnableVerifiedFeedbackLearning { get; set; } = true;
    public bool EnableAuthoritativeFeedbackOverride { get; set; } = true;
    public double FeedbackCandidateSimilarity { get; set; } = 0.82;
    public double FeedbackVerifiedSimilarity { get; set; } = 0.93;
    public double FeedbackAuthoritativeSimilarity { get; set; } = 0.97;
    public int FeedbackVerifiedMinimumExamples { get; set; } = 2;
    public int FeedbackAuthoritativeMinimumExamples { get; set; } = 3;
    public int FeedbackMaximumConflictCount { get; set; } = 0;
    public int SchemaVersion { get; set; }
    public List<NumericFieldDefinition> Fields { get; set; } = [];
    public int OperationModeConfirmationReadings { get; set; } = 2;
    public int OperationModeMaximumUnknownReadings { get; set; } = 3;
    public double OperationModeMinimumTextSimilarityPercent { get; set; } = 75;
    public List<OperationModeKeywordDefinition> OperationModeKeywords { get; set; } = OperationModeKeywordDefinition.CreateDefaults();
    public NumericFieldDefinition? FindField(string fieldId) => Fields.FirstOrDefault(field => string.Equals(field.FieldId, fieldId, StringComparison.OrdinalIgnoreCase));
    public string Name { get; set; } = "Herrenknecht_Default";
    public int PaddingLeft { get; set; } = 12;
    public int PaddingRight { get; set; } = 12;
    public int PaddingTop { get; set; } = 8;
    public int PaddingBottom { get; set; } = 8;
    public int GatePaddingLeft { get; set; } = 2;
    public int GatePaddingRight { get; set; } = 2;
    public int GatePaddingTop { get; set; } = 1;
    public int GatePaddingBottom { get; set; } = 1;
    public decimal GateMaximumValue { get; set; } = 10_000m;
    public decimal GateChangeTolerance { get; set; } = 50m;
    public int GateConfirmationReadings { get; set; } = 2;
    public decimal GateMaximumChangePerReading { get; set; } = 30m;
    public bool EnableGateCharacterGeometry { get; set; } = true;
    public bool EnableGateDigitTemplateMatcher { get; set; } = true;
    public string GateDigitTemplateFolder { get; set; } = "Templates/GateDigits";
    public double GateTemplateMinimumScore { get; set; } = 0.72;
    public double GateTemplateAuthoritativeScore { get; set; } = 0.90;
    public double GateTemplateMinimumMargin { get; set; } = 0.06;
    public double GateMaximumAllowedInkLoss { get; set; } = 0.25;
    public int GateMinimumRecommendedTemplatesPerDigit { get; set; } = 5;
    public bool EnableScaleCharacterOcr { get; set; } = true;
    public string Scale1DigitTemplateFolder { get; set; } = "Templates/Scale1Digits";
    public string Scale2DigitTemplateFolder { get; set; } = "Templates/Scale2Digits";
    public double Scale1TemplateMinimumScore { get; set; } = 0.72;
    public double Scale1TemplateAuthoritativeScore { get; set; } = 0.90;
    public double Scale1TemplateMinimumMargin { get; set; } = 0.06;
    public double Scale2TemplateMinimumScore { get; set; } = 0.72;
    public double Scale2TemplateAuthoritativeScore { get; set; } = 0.90;
    public double Scale2TemplateMinimumMargin { get; set; } = 0.06;
    public double ScaleMaximumAllowedInkLoss { get; set; } = 0.25;
    public double CharacterTemplateMinimumScore { get; set; } = 0.72;
    public double CharacterTemplateAuthoritativeScore { get; set; } = 0.90;
    public double CharacterTemplateMinimumMargin { get; set; } = 0.06;
    public double CharacterMaximumAllowedInkLoss { get; set; } = 0.25;
    public double DuplicateTemplateSimilarityThreshold { get; set; } = 0.97;
    public int MaximumTemplatesPerDigit { get; set; } = 30;
    public int ScaleMinimumRecommendedTemplatesPerDigit { get; set; } = 5;
    public decimal NominalMaximumChangePerReading { get; set; } = 10m;
    public decimal Scale1MaximumChangePerReading { get; set; } = 15m;
    public decimal Scale2MaximumChangePerReading { get; set; } = 15m;
    public int SuspiciousValueConfirmationReadings { get; set; } = 2;
    public decimal SuspiciousValueConfirmationTolerance { get; set; } = 2m;
    public decimal AdvanceMaximumChangePerReading { get; set; } = 150m;
    public int AdvanceConfirmationReadings { get; set; } = 2;
    public decimal AdvanceConfirmationTolerance { get; set; } = 20m;
    public decimal AdvanceMaximumBackwardToleranceMm { get; set; } = 50m;
    public decimal AdvanceLargeBackwardJumpMm { get; set; } = 200m;
    public decimal AdvanceMaximumForwardJumpPerRealReadingMm { get; set; } = 250m;
    public decimal StableZoneMinimumAdvanceMm { get; set; } = 950m;
    public decimal StableZoneMaximumAdvanceMm { get; set; } = 1150m;
    public int StableZoneConfirmationReadings { get; set; } = 2;
    public AdvanceSelectionMode AdvanceSelectionMode { get; set; } = AdvanceSelectionMode.GroupA;
    public List<string> AdvanceEnabledGroups { get; set; } = ["A", "C"];
    public string AdvancePrimaryGroup { get; set; } = "A";
    public decimal AdvanceMinimumValidValue { get; set; } = 1m;
    public decimal AdvanceMaximumAllowedGroupDifferenceMm { get; set; } = 100m;
    public int AdvanceMinimumRequiredValidGroups { get; set; } = 2;
    public int MinimumConsistentAdvanceGroups { get; set; } = 2;
    public decimal AdvanceGroupAgreementToleranceMm { get; set; } = 30m;
    public int AdvanceSingleGroupConfirmationReadings { get; set; } = 2;
    public int AdvanceSensorZeroWarningReadings { get; set; } = 5;
    public decimal ThrustMaximumRelativeChangePerReading { get; set; } = 0.35m;
    public decimal ThrustMaximumAbsoluteChangePerReading { get; set; } = 6000m;
    public int ThrustRecoveryConfirmationCount { get; set; } = 3;
    public decimal TorqueMaximumRelativeChangePerReading { get; set; } = 0.75m;
    public decimal TorqueMaximumAbsoluteChangePerReading { get; set; } = 2.5m;
    public int TorqueRecoveryConfirmationCount { get; set; } = 2;
    public int ScaleFactor { get; set; } = 8;
    public bool EnableGaussianBlur { get; set; } = true;
    public bool EnableMedianFilter { get; set; } = true;
    public bool EnableMorphology { get; set; } = true;
    public float LiveFastMinimumConfidence { get; set; } = 0.55f;
    public bool LiveFastHeavyFallbackAllowed { get; set; }
    public bool EnableSegmentation { get; set; } = true;
    public int AdaptiveThresholdRadius { get; set; } = 12;
    public int AdaptiveThresholdBias { get; set; } = 7;
    public bool PreserveLeadingZeros { get; set; }
    public OcrLengthRange NominalLength { get; set; } = new(3, 4);
    public OcrLengthRange ScaleLength { get; set; } = new(1, 4);
    public OcrLengthRange GateLength { get; set; } = new(1, 3);

    public OcrLengthRange GetLengthRange(OcrFieldKind field) => field switch
    {
        OcrFieldKind.Nominal => NominalLength,
        OcrFieldKind.Scale or OcrFieldKind.Scale1 or OcrFieldKind.Scale2 => ScaleLength,
        OcrFieldKind.Gate => GateLength,
        OcrFieldKind.Advance => new OcrLengthRange(1, 4),
        OcrFieldKind.StableRpm or OcrFieldKind.StableTorque or OcrFieldKind.StableAdvanceSpeed
            or OcrFieldKind.StablePenetration or OcrFieldKind.StableThrustForce
            or OcrFieldKind.NominalInjection or OcrFieldKind.RealInjection => new OcrLengthRange(1, 8),
        _ => new OcrLengthRange(1, 8)
    };
}

public sealed class OcrLengthRange
{
    public OcrLengthRange() { }
    public OcrLengthRange(int minimum, int maximum) { Minimum = minimum; Maximum = maximum; }
    public int Minimum { get; set; }
    public int Maximum { get; set; }
    public bool Contains(int length) => length >= Minimum && length <= Maximum;
}

public enum OcrFieldKind { General, Nominal, Scale, Gate, Scale1, Scale2, Advance,
    StableRpm, StableTorque, StableThrustForce, StableAdvanceSpeed, StablePenetration,
    NominalInjection, RealInjection, Epb1, Epb2, Epb3, Epb4, Epb5 }
public enum OcrExecutionMode { Diagnostic, LiveFast }
public enum AdvanceSelectionMode { GroupA, GroupB, GroupC, GroupD, AverageOfValidGroups, MedianOfValidGroups, MaximumOfValidGroups, MinimumOfValidGroups, ClosestValidPairAverage }
