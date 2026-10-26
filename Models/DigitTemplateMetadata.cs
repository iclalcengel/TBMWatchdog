namespace TBMWatchdog.Models;

public sealed class DigitTemplateMetadata
{
    public int Digit { get; set; }
    public string MachineProfile { get; set; } = "";
    public string FieldKind { get; set; } = OcrFieldKind.Gate.ToString();
    public string SourceField { get; set; } = "";
    public TemplateScope TemplateScope { get; set; } = TemplateScope.FieldSpecific;
    public DateTime CapturedAt { get; set; }
    public Size SourceImageSize { get; set; }
    public Size NormalizedImageSize { get; set; }
    public string PreprocessingMethod { get; set; } = "";
    public bool UserVerified { get; set; }
    public bool Enabled { get; set; } = true;
    public double ReferenceConfidence { get; set; }
    public string NormalizedImage { get; set; } = "48x72 binary, aspect ratio preserved";
    public double QualityScore { get; set; } = 0.80;
    public string QualityStatus { get; set; } = "Acceptable";
    public string QualityReason { get; set; } = "Legacy/neutral quality";
    public long UsageCount { get; set; }
    public long SuccessCount { get; set; }
    public long ConflictCount { get; set; }
}
