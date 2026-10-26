using System.Globalization;
using System.Text;

namespace TBMWatchdog.Services;

public sealed record SuspiciousValueDecision(DateTime Timestamp, string Machine, string Field,
    decimal? LastAcceptedValue, decimal? CandidateValue, decimal Difference, decimal Threshold,
    int ConfirmationCount, int RequiredConfirmationCount, string Decision, string Reason);

public sealed record TemporalValidationResult(decimal? Value, bool IsHeld, string? Status,
    IReadOnlyList<SuspiciousValueDecision> Decisions, decimal? PendingValue = null,
    int ConfirmationCount = 0, string ValidationState = "Accepted");

public sealed class TemporalValueValidationService
{
    private sealed class FieldState { public decimal? Accepted; public decimal? Pending; public int Count; public DateTime? LastCandidateCapturedAt; }
    private readonly Dictionary<string, FieldState> _states = new(StringComparer.Ordinal);

    public bool HasPending(string field) => _states.TryGetValue(field, out var state) && state.Pending.HasValue;

    public TemporalValidationResult Snapshot(string field, int requiredConfirmations)
    {
        if (!_states.TryGetValue(field, out var state)) return new(null, false, null, Array.Empty<SuspiciousValueDecision>());
        var status = state.Pending.HasValue
            ? $"Şüpheli değer bekletiliyor: {state.Pending:0.##} ({state.Count}/{requiredConfirmations})"
            : null;
        return new(state.Accepted, state.Pending.HasValue, status, Array.Empty<SuspiciousValueDecision>(),
            state.Pending, state.Count, state.Pending.HasValue ? "SuspiciousCandidate" : "Accepted");
    }

    public TemporalValidationResult Validate(string machine, string field, decimal? incoming,
        decimal threshold, int requiredConfirmations, decimal tolerance, DateTime timestamp,
        bool isIndependentRealOcr = true)
    {
        if (threshold < 0 || tolerance < 0 || requiredConfirmations < 2) throw new ArgumentOutOfRangeException();
        if (!_states.TryGetValue(field, out var state)) _states[field] = state = new FieldState();
        var decisions = new List<SuspiciousValueDecision>();
        var lastAcceptedAtStart = state.Accepted;
        if (!incoming.HasValue)
        {
            var status = state.Pending.HasValue
                ? $"Şüpheli değer bekletiliyor: {state.Pending:0.##} ({state.Count}/{requiredConfirmations})"
                : state.Accepted.HasValue ? "Okunamadı; son güvenilir değer korunuyor." : null;
            return Result(status);
        }
        if (!state.Accepted.HasValue)
        {
            state.Accepted = incoming;
            decisions.Add(Decision("Accepted", "İlk başarılı okuma doğrudan kabul edildi", 0));
            return new(incoming, false, null, decisions, null, 0, "Accepted");
        }

        var difference = Math.Abs(incoming.Value - state.Accepted.Value);
        if (difference <= threshold)
        {
            if (state.Pending.HasValue)
                decisions.Add(Decision("Cancelled", "Tek karelik OCR sıçraması filtrelendi", Math.Abs(state.Pending.Value - state.Accepted.Value), state.Pending));
            state.Pending = null; state.Count = 0; state.LastCandidateCapturedAt = null; state.Accepted = incoming;
            decisions.Add(Decision("Accepted", "Değişim sınır içinde doğrudan kabul edildi", difference));
            return new(incoming, false, null, decisions, null, 0, "Accepted");
        }

        if (!state.Pending.HasValue || Math.Abs(incoming.Value - state.Pending.Value) > tolerance)
        {
            if (state.Pending.HasValue)
                decisions.Add(Decision("Cancelled", "Uyumsuz yeni aday nedeniyle şüpheli aday iptal edildi", Math.Abs(state.Pending.Value - state.Accepted.Value), state.Pending));
            state.Pending = incoming; state.Count = isIndependentRealOcr ? 1 : 0;
            state.LastCandidateCapturedAt = isIndependentRealOcr ? timestamp : null;
            decisions.Add(Decision("Pending", "Şüpheli değer ardışık doğrulama bekliyor", difference));
            return Result($"Şüpheli değer bekletiliyor: {incoming:0.##} ({state.Count}/{requiredConfirmations})");
        }

        if (!isIndependentRealOcr || state.LastCandidateCapturedAt == timestamp)
            return Result($"Şüpheli değer bekletiliyor: {state.Pending:0.##} ({state.Count}/{requiredConfirmations})");

        state.Count++;
        state.LastCandidateCapturedAt = timestamp;
        if (state.Count >= requiredConfirmations)
        {
            state.Accepted = incoming; state.Pending = null;
            decisions.Add(Decision("Accepted", "Ardışık doğrulama ile kabul edildi", difference));
            state.Count = 0; state.LastCandidateCapturedAt = null;
            return new(incoming, false, "Ardışık doğrulama ile kabul edildi", decisions, null, 0, "AcceptedAfterConfirmation");
        }
        decisions.Add(Decision("Pending", "Şüpheli değer ardışık doğrulama bekliyor", difference));
        return Result($"Şüpheli değer bekletiliyor: {incoming:0.##} ({state.Count}/{requiredConfirmations})");

        TemporalValidationResult Result(string? status) => new(state.Accepted, state.Pending.HasValue, status,
            decisions, state.Pending, state.Count, state.Pending.HasValue ? "SuspiciousCandidate" : "Accepted");

        SuspiciousValueDecision Decision(string decision, string reason, decimal diff, decimal? candidate = null) =>
            new(timestamp, machine, field, lastAcceptedAtStart, candidate ?? incoming, diff, threshold,
                state.Count, requiredConfirmations, decision, reason);
    }

    public void Reset() => _states.Clear();

    public void ResetPending()
    {
        foreach (var state in _states.Values) { state.Pending = null; state.Count = 0; state.LastCandidateCapturedAt = null; }
    }
}

public sealed class SuspiciousValueDecisionCsvService
{
    private readonly object _sync = new();
    public SuspiciousValueDecisionCsvService(string? path = null) =>
        FilePath = path ?? Path.Combine(AppContext.BaseDirectory, "Diagnostics", "SuspiciousValueDecisions.csv");
    public string FilePath { get; }
    public void Append(IEnumerable<SuspiciousValueDecision> decisions)
    {
        var items = decisions.ToArray(); if (items.Length == 0) return;
        lock (_sync)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            if (!File.Exists(FilePath)) File.WriteAllText(FilePath,
                "Timestamp,Machine,Field,LastAcceptedValue,CandidateValue,Difference,Threshold,ConfirmationCount,RequiredConfirmationCount,Decision,Reason\r\n", new UTF8Encoding(true));
            using var writer = new StreamWriter(FilePath, true, new UTF8Encoding(false));
            foreach (var item in items) writer.WriteLine(string.Join(',', item.Timestamp.ToString("O"), Q(item.Machine), Q(item.Field),
                N(item.LastAcceptedValue), N(item.CandidateValue), N(item.Difference), N(item.Threshold), item.ConfirmationCount,
                item.RequiredConfirmationCount, Q(item.Decision), Q(item.Reason)));
        }
    }
    private static string N(decimal? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "";
    private static string Q(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
