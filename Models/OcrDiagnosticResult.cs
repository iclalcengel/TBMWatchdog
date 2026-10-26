namespace TBMWatchdog.Models;

public sealed class OcrDiagnosticResult : IDisposable
{
    private readonly Bitmap? _legacyProcessedImage;

    public OcrDiagnosticResult(Bitmap rawImage, Bitmap processedImage, OcrReadResult result, DateTime timestamp)
    {
        RawImage = rawImage;
        _legacyProcessedImage = processedImage;
        Result = result;
        Timestamp = timestamp;
        Candidates = Array.Empty<OcrCandidateResult>();
    }

    public OcrDiagnosticResult(Bitmap rawImage, IReadOnlyList<OcrCandidateResult> candidates,
        OcrCandidateResult? selectedCandidate, OcrReadResult result, DateTime timestamp, TimeSpan totalElapsed = default)
    {
        RawImage = rawImage;
        Candidates = candidates;
        SelectedCandidate = selectedCandidate;
        Result = result;
        Timestamp = timestamp;
        TotalElapsed = totalElapsed;
    }

    public Bitmap RawImage { get; }
    public Bitmap ProcessedImage => SelectedCandidate?.ProcessedImage ?? _legacyProcessedImage ?? RawImage;
    public IReadOnlyList<OcrCandidateResult> Candidates { get; }
    public OcrCandidateResult? SelectedCandidate { get; }
    public OcrReadResult Result { get; }
    public DateTime Timestamp { get; }
    public TimeSpan TotalElapsed { get; }
    public OcrExecutionMode ExecutionMode { get; init; } = OcrExecutionMode.Diagnostic;
    public int PipelineCountExecuted { get; init; }
    public string PrimaryPipeline { get; init; } = "";
    public bool FallbackUsed { get; init; }
    public bool HeavyFallbackUsed { get; init; }
    public bool TargetedFallbackUsed { get; init; }
    public string TargetedFallbackPipeline { get; init; } = "";
    public double TargetedFallbackMs { get; init; }
    public double DecisionMs { get; init; }
    public string ExecutionPath => HeavyFallbackUsed ? "HeavyFallback"
        : TargetedFallbackUsed ? "TargetedFallback"
        : FallbackUsed ? "Fallback"
        : "Fast";
    public double FullLineOcrMs => Candidates.Where(candidate => !candidate.UsesCharacterBasedOcr).Sum(candidate => candidate.OcrElapsed.TotalMilliseconds);
    public double CharacterSegmentationMs => Candidates.Count == 0 ? 0 : Candidates.Max(candidate => candidate.SegmentationElapsed.TotalMilliseconds);
    public double TemplateMatchMs => Candidates.Sum(candidate => candidate.TemplateMatchElapsed.TotalMilliseconds);
    public bool IsAccepted => Result.IsSuccessful;
    public string DecisionReason => IsAccepted
        ? Result.SelectionReason ?? $"Kabul: {Result.Value:0.##} geçerli aralıkta ve sayısal biçimde."
        : $"Red: {Result.Error ?? "Bilinmeyen OCR hatası."}";

    public void Dispose()
    {
        RawImage.Dispose();
        _legacyProcessedImage?.Dispose();
        foreach (var candidate in Candidates) candidate.Dispose();
    }
}

/// <summary>Separates the latest observation from the last available numeric sample and any official value.</summary>
public sealed record LiveDiagnosticFieldState(
    decimal? CurrentObservedValue, DateTime? CurrentObservedAt, OcrConfidenceStatus CurrentQuality,
    decimal? LastNumericObservedValue, DateTime? LastNumericObservedAt, OcrConfidenceStatus LastNumericQuality,
    decimal? OfficialValue);
