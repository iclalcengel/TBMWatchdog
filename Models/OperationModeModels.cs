namespace TBMWatchdog.Models;

public enum OperationModeState { Unknown, Excavation, RingBuilding }
public enum OperationModeEventType { None, ExcavationStarted, ExcavationEnded }
public enum OperationModeMatchMethod { None, Exact, Approximate, Ambiguous }

public sealed class OperationModeKeywordDefinition
{
    public OperationModeState State { get; set; }
    public List<string> Keywords { get; set; } = [];
    public static List<OperationModeKeywordDefinition> CreateDefaults() =>
    [
        new() { State = OperationModeState.Excavation, Keywords = ["İlerleme", "Ilerleme", "Advance"] },
        new() { State = OperationModeState.RingBuilding, Keywords = ["Ring Building", "RingBuilding", "Halka Teçhizatı", "Halka Teshizati", "Halka Tesisatı", "Halka Montajı"] }
    ];
}

public sealed record OperationModeTransition(DateTime Timestamp, string MachineId,
    OperationModeState PreviousState, OperationModeState NewState, string RawOcrText,
    string NormalizedText, int ConfirmationCount, OperationModeEventType EventType,
    float Confidence, string SessionId);

public sealed record OperationModeDiagnostic(OperationModeState ConfirmedState,
    OperationModeState CandidateState, int ConfirmationCount, int RequiredConfirmations,
    int ConsecutiveUnknownReadings, bool IsUnreadable, string RawOcrText,
    string NormalizedText, float Confidence, string MatchedKeyword,
    OperationModeMatchMethod MatchMethod, double SimilarityPercent)
{
    public string DisplayText => ConfirmedState switch
    {
        OperationModeState.Excavation => "Çalışma modu: İlerleme",
        OperationModeState.RingBuilding => "Çalışma modu: Ring Building",
        _ => "Çalışma modu: Okunamadı"
    };
    public string? CandidateText => CandidateState == OperationModeState.Unknown || CandidateState == ConfirmedState
        ? null : $"Aday durum: {(CandidateState == OperationModeState.Excavation ? "İlerleme" : "Ring Building")} ({ConfirmationCount}/{RequiredConfirmations})";
    public string MatchDetails => $"Ham OCR: {RawOcrText} | Normalize: {NormalizedText} | Eşleşen anahtar kelime: {(string.IsNullOrEmpty(MatchedKeyword) ? "—" : MatchedKeyword)} | Eşleşme yöntemi: {MatchMethod switch { OperationModeMatchMethod.Exact => "Tam", OperationModeMatchMethod.Approximate => "Yaklaşık", OperationModeMatchMethod.Ambiguous => "Belirsiz", _ => "Yok" }} | Benzerlik: %{SimilarityPercent:0.0}";
}

public sealed record OperationModeTextMatch(OperationModeState State, string MatchedKeyword,
    OperationModeMatchMethod Method, double SimilarityPercent);

public sealed record TextOcrCandidate(string PreprocessingMethod, string PageSegMode, string RawText,
    string TrimmedText, float Confidence, string? Error = null);

public sealed record TextOcrResult(string RawText, float Confidence, string? Error = null)
{
    public string TrimmedText { get; init; } = RawText.Trim();
    public string NormalizedText { get; init; } = "";
    public string FinalOperationModeText { get; init; } = "";
    public string BestPageSegMode { get; init; } = "—";
    public string BestPreprocessingMethod { get; init; } = "—";
    public string FailureReason { get; init; } = "Success";
    public IReadOnlyList<TextOcrCandidate> Candidates { get; init; } = Array.Empty<TextOcrCandidate>();
    public bool NumericWhitelistUsed { get; init; }
    public bool IsSuccessful => Error is null && !string.IsNullOrWhiteSpace(TrimmedText);
}
