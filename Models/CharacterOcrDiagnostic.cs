namespace TBMWatchdog.Models;

public sealed class CharacterOcrDiagnostic : IDisposable
{
    public required int CharacterIndex { get; init; }
    public required Bitmap OriginalSegmentImage { get; init; }
    public required Bitmap CleanedSegmentImage { get; init; }
    public required Bitmap TightCroppedImage { get; init; }
    public required Bitmap NormalizedOcrInputImage { get; init; }
    public required string RawOcrText { get; init; }
    public required string CleanedDigit { get; init; }
    public required float OcrConfidence { get; init; }
    public string? TemplateResult { get; init; }
    public double TemplateScore { get; init; }
    public string? SecondBestTemplateResult { get; init; }
    public double SecondBestTemplateScore { get; init; }
    public double TemplateScoreMargin { get; init; }
    public bool IsTemplateConfident { get; init; }
    public double InkLossRatio { get; init; }
    public string SelectedImageSource { get; init; } = "Normalize OCR girdisi";
    public string TesseractInputSource { get; init; } = "Normalize OCR girdisi";
    public string TemplateInputSource { get; init; } = "Normalize şablon girdisi";
    public string GeometryInputSource { get; init; } = "Normalize geometri girdisi";
    public bool IsTemplateAuthoritative { get; init; }
    public bool TemplateContributed { get; init; }
    public bool TesseractContributed { get; init; }
    public bool GeometryContributed { get; init; }
    public string DecisionSource { get; init; } = "";
    public required string GeometryResult { get; init; }
    public double GeometryConfidence { get; init; }
    public required string FinalDigit { get; init; }
    public required string DecisionReason { get; init; }
    public Rectangle SegmentBounds { get; init; }
    public string SplitSource { get; init; } = "ConnectedComponent";

    public int Index => CharacterIndex;
    public Bitmap OriginalSegment => OriginalSegmentImage;
    public Bitmap CleanedSegment => CleanedSegmentImage;
    public Bitmap NormalizedInput => NormalizedOcrInputImage;
    public string TesseractResult => CleanedDigit;
    public float TesseractConfidence => OcrConfidence;
    public string CombinedResult => FinalDigit;
    public string DecisionDetail => DecisionReason;

    public void Dispose()
    {
        OriginalSegmentImage.Dispose();
        CleanedSegmentImage.Dispose();
        TightCroppedImage.Dispose();
        NormalizedOcrInputImage.Dispose();
    }
}
