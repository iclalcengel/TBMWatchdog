namespace TBMWatchdog.Models;

public sealed record OcrReadResult(
    decimal? Value,
    string RawText,
    string CleanedText,
    float Confidence,
    string? Error)
{
    public string? Warning { get; init; }
    public string? SelectionReason { get; init; }
    public decimal? FullLineConsensusValue { get; init; }
    public int FullLineConsensusCount { get; init; }
    public string FullLineConsensusLevel { get; init; } = "None";
    public decimal? CharacterResult { get; init; }
    public string CharacterStatus { get; init; } = "NotAvailable";
    public bool CharacterAuthoritative { get; init; }
    public string CharacterAuthorityReason { get; init; } = "NotEvaluated";
    public int ZeroCandidateCount { get; init; }
    public string DecisionSource { get; init; } = "";
    public int EstimatedDigitCount { get; init; }
    public int SelectedCandidateDigitCount { get; init; }
    public string DigitPlausibility { get; init; } = "NotEvaluated";
    public bool IsSuccessful => Value.HasValue && Error is null;

    public static OcrReadResult Failed(string rawText, string cleanedText, float confidence, string error) =>
        new(null, rawText, cleanedText, confidence, error);
}
