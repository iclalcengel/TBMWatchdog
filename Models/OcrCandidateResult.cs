using Tesseract;

namespace TBMWatchdog.Models;

public sealed class OcrCandidateResult : IDisposable
{
    public required string ProcessingMethod { get; init; }
    public required PageSegMode PageSegMode { get; init; }
    public required string RawText { get; init; }
    public required string CleanedText { get; init; }
    public decimal? ParsedValue { get; set; }
    public float Confidence { get; set; }
    public float OcrConfidence { get; init; }
    public float SystemConfidence { get; set; }
    public int CharacterCount { get; init; }
    public int SegmentCount { get; init; }
    public int RawSegmentCount { get; init; }
    public IReadOnlyList<Rectangle> AcceptedSegmentBounds { get; init; } = Array.Empty<Rectangle>();
    public IReadOnlyList<Rectangle> RejectedSegmentBounds { get; init; } = Array.Empty<Rectangle>();
    public int OcrDigitCount => CharacterCount;
    public bool? SegmentMatch { get; init; }
    public float FinalScore => SystemConfidence;
    public IReadOnlyList<CharacterOcrDiagnostic> CharacterDiagnostics { get; init; } = Array.Empty<CharacterOcrDiagnostic>();
    public bool UsesCharacterBasedOcr { get; init; }
    public string CharacterOcrText { get; init; } = "";
    public string CombinedText { get; init; } = "";
    public bool CharacterDecisionReliable { get; init; }
    public bool AllCharactersTemplateAuthoritative { get; init; }
    public string CharacterDecisionSummary { get; init; } = "";
    public string? TemplateWarning { get; init; }
    public string CharacterBasedResult => CharacterOcrText;
    public string TemplateResult => string.Join(" | ", CharacterDiagnostics.Select(c => c.TemplateResult is null
        ? "—"
        : $"{c.TemplateResult} %{c.TemplateScore * 100:0} (2: {c.SecondBestTemplateResult ?? "—"} %{c.SecondBestTemplateScore * 100:0}, Δ%{c.TemplateScoreMargin * 100:0})"));
    public string GeometryResult => string.Join(" | ", CharacterDiagnostics.Select(c => c.GeometryResult));
    public string CombinedCharacterResult => CombinedText;
    public bool SegmentationSuccessful { get; init; }
    public double BinaryQuality { get; init; }
    public double NoiseRatio { get; init; }
    public double CharacterIntegrity { get; init; }
    public TimeSpan PreprocessingElapsed { get; init; }
    public TimeSpan OcrElapsed { get; init; }
    public TimeSpan SegmentationElapsed { get; init; }
    public TimeSpan TemplateMatchElapsed { get; init; }
    public TimeSpan TotalElapsed { get; init; }
    public bool IsSlow => TotalElapsed > TimeSpan.FromSeconds(3);
    public required Size ImageSize { get; init; }
    public required Bitmap ProcessedImage { get; init; }
    public required Bitmap BinaryImage { get; init; }
    public required Bitmap SegmentationImage { get; init; }
    public string? Error { get; set; }
    public bool ExtraCharacterDetected { get; set; }
    public string RemovedNoiseCharacter { get; set; } = "";
    public string ExtraCharacterEliminationReason { get; set; } = "";
    public decimal? CorrectedFullLineResult { get; set; }
    public int DetectedComponentCount { get; init; }
    public int MergedComponentCount { get; init; }
    public int SplitAttemptCount { get; init; }
    public int ProjectionSplitCount { get; init; }
    public IReadOnlyList<int> ProjectionValleys { get; init; } = Array.Empty<int>();
    public bool HasUnresolvedMergedComponents { get; init; }
    public bool SegmentationFallbackUsed { get; init; }
    public string CharacterValidationStatus { get; init; } = "NotAvailable";
    public string? CharacterValidationWarning { get; init; }
    public int EstimatedDigitCount { get; set; }
    public string DigitPlausibility { get; set; } = "NotEvaluated";
    public double PlausibilityScore { get; set; }
    public bool LeadingNarrowDigitGeometry { get; init; }
    public IReadOnlyList<Services.TorqueGlyphGeometry> TorqueGlyphs { get; init; } = Array.Empty<Services.TorqueGlyphGeometry>();

    public bool IsValid => ParsedValue.HasValue && Error is null;

    public void Dispose()
    {
        ProcessedImage.Dispose(); BinaryImage.Dispose(); SegmentationImage.Dispose();
        foreach (var diagnostic in CharacterDiagnostics) diagnostic.Dispose();
    }
}
