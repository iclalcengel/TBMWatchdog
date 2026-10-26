using System.Globalization;
using System.Text;
using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public sealed class OperationModeStateService
{
    private readonly string _machineId; private readonly string _sessionId; private readonly int _required; private readonly int _maximumUnknown;
    private readonly IReadOnlyList<(OperationModeState State, string Keyword)> _keywords; private readonly double _minimumSimilarity;
    private OperationModeState _confirmed; private OperationModeState _candidate; private int _candidateCount; private int _unknownCount;
    public OperationModeStateService(string machineId, string sessionId, OcrProfile profile)
    {
        _machineId = machineId; _sessionId = sessionId; _required = Math.Max(2, profile.OperationModeConfirmationReadings);
        _maximumUnknown = Math.Max(1, profile.OperationModeMaximumUnknownReadings);
        _keywords = profile.OperationModeKeywords.SelectMany(group => group.Keywords.Select(keyword => (group.State, Normalize(keyword).Replace(" ", "")))).ToArray();
        _minimumSimilarity = Math.Clamp(profile.OperationModeMinimumTextSimilarityPercent, 0, 100);
    }
    public event Action<OperationModeTransition>? OperationModeChanged;
    public event Action<OperationModeTransition>? ExcavationStarted;
    public event Action<OperationModeTransition>? ExcavationEnded;

    public void ResetForResume()
    {
        _confirmed = OperationModeState.Unknown; _candidate = OperationModeState.Unknown;
        _candidateCount = 0; _unknownCount = 0;
    }

    public (OperationModeDiagnostic Diagnostic, OperationModeTransition? Transition) Process(string? rawText, float confidence, DateTime timestamp)
    {
        var normalized = Normalize(rawText); var match = OperationModeTextMatcher.Match(normalized, _keywords, _minimumSimilarity);
        var detected = match.State;
        if (detected == OperationModeState.Unknown)
        {
            _unknownCount++; return (Diagnostic(rawText, normalized, confidence, match), null);
        }
        _unknownCount = 0;
        if (detected == _confirmed) { _candidate = OperationModeState.Unknown; _candidateCount = 0; return (Diagnostic(rawText, normalized, confidence, match), null); }
        if (detected == _candidate) _candidateCount++; else { _candidate = detected; _candidateCount = 1; }
        if (_candidateCount < _required) return (Diagnostic(rawText, normalized, confidence, match), null);
        var previous = _confirmed; _confirmed = detected; var confirmedCount = _candidateCount; _candidate = OperationModeState.Unknown; _candidateCount = 0;
        var eventType = previous switch
        {
            OperationModeState.RingBuilding when _confirmed == OperationModeState.Excavation => OperationModeEventType.ExcavationStarted,
            OperationModeState.Excavation when _confirmed == OperationModeState.RingBuilding => OperationModeEventType.ExcavationEnded,
            _ => OperationModeEventType.None
        };
        var transition = new OperationModeTransition(timestamp, _machineId, previous, _confirmed, rawText ?? "", normalized,
            confirmedCount, eventType, confidence, _sessionId);
        OperationModeChanged?.Invoke(transition);
        if (eventType == OperationModeEventType.ExcavationStarted) ExcavationStarted?.Invoke(transition);
        if (eventType == OperationModeEventType.ExcavationEnded) ExcavationEnded?.Invoke(transition);
        return (Diagnostic(rawText, normalized, confidence, match), transition);
    }
    private OperationModeDiagnostic Diagnostic(string? raw, string normalized, float confidence, OperationModeTextMatch match) => new(_confirmed, _candidate,
        _candidateCount, _required, _unknownCount, _unknownCount >= _maximumUnknown, raw ?? "", normalized, confidence,
        match.MatchedKeyword, match.Method, match.SimilarityPercent);

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var decomposed = value.Trim().ToUpper(new CultureInfo("tr-TR")).Normalize(NormalizationForm.FormD); var builder = new StringBuilder();
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            var normalized = character switch { 'İ' or 'I' or 'ı' => 'I', 'Ş' => 'S', 'Ğ' => 'G', 'Ü' => 'U', 'Ö' => 'O', 'Ç' => 'C', '0' => 'O', '1' => 'I', '|' => 'I', _ => character };
            builder.Append(char.IsLetterOrDigit(normalized) ? normalized : ' ');
        }
        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}

public sealed class OperationModeTransitionCsvService
{
    private readonly object _sync = new(); public string FilePath { get; }
    public OperationModeTransitionCsvService(string? path = null) => FilePath = path ?? Path.Combine(AppContext.BaseDirectory, "Diagnostics", "OperationModeTransitions.csv");
    public void Append(OperationModeTransition transition)
    {
        lock (_sync) { Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!); if (!File.Exists(FilePath)) File.WriteAllText(FilePath,
            "Timestamp,MachineId,PreviousState,NewState,RawOcrText,NormalizedText,ConfirmationCount,EventType,Confidence,SessionId\r\n", new UTF8Encoding(true));
            File.AppendAllText(FilePath, string.Join(',', transition.Timestamp.ToString("O"), Q(transition.MachineId), transition.PreviousState, transition.NewState,
                Q(transition.RawOcrText), Q(transition.NormalizedText), transition.ConfirmationCount, transition.EventType,
                transition.Confidence.ToString("0.000", CultureInfo.InvariantCulture), Q(transition.SessionId)) + "\r\n", new UTF8Encoding(false)); }
    }
    private static string Q(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}

public static class OperationModeTextMatcher
{
    public static OperationModeTextMatch Match(string normalizedText,
        IReadOnlyList<(OperationModeState State, string Keyword)> keywords, double minimumSimilarityPercent)
    {
        var compact = normalizedText.Replace(" ", "");
        if (compact.Length == 0) return None();
        var exact = keywords.Where(item => compact.Contains(item.Keyword, StringComparison.Ordinal))
            .GroupBy(item => item.State).Select(group => group.OrderByDescending(item => item.Keyword.Length).First()).ToArray();
        if (exact.Length == 1) return new(exact[0].State, exact[0].Keyword, OperationModeMatchMethod.Exact, 100);
        if (exact.Length > 1) return new(OperationModeState.Unknown, "", OperationModeMatchMethod.Ambiguous, 100);
        if (compact.Length < 5) return None();

        var inputs = normalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Append(compact)
            .Where(value => value.Length >= 5).Distinct(StringComparer.Ordinal).ToArray();
        var scored = keywords.Select(keyword =>
        {
            var score = inputs.Max(input => Similarity(input, keyword.Keyword));
            return (keyword.State, keyword.Keyword, Score: score * 100);
        }).GroupBy(item => item.State).Select(group => group.OrderByDescending(item => item.Score).First())
          .OrderByDescending(item => item.Score).ToArray();
        var qualifiedStates = scored.Where(item => item.Score >= minimumSimilarityPercent).ToArray();
        if (qualifiedStates.Length == 0) return None(scored.FirstOrDefault().Score);
        if (qualifiedStates.Length > 1) return new(OperationModeState.Unknown, "", OperationModeMatchMethod.Ambiguous, qualifiedStates[0].Score);
        var best = qualifiedStates[0]; return new(best.State, best.Keyword, OperationModeMatchMethod.Approximate, best.Score);
    }

    private static double Similarity(string left, string right)
    {
        var maximum = Math.Max(left.Length, right.Length); if (maximum == 0) return 1;
        var levenshtein = 1d - Levenshtein(left, right) / (double)maximum;
        var suffix = CommonSuffix(left, right) / (double)Math.Min(left.Length, right.Length);
        var subsequence = LongestCommonSubsequence(left, right) / (double)maximum;
        var combined = levenshtein * 0.5 + suffix * 0.3 + subsequence * 0.2;
        return Math.Clamp(Math.Max(levenshtein, combined), 0, 1);
    }
    private static int Levenshtein(string left, string right)
    {
        var previous = Enumerable.Range(0, right.Length + 1).ToArray(); var current = new int[right.Length + 1];
        for (var i = 1; i <= left.Length; i++) { current[0] = i; for (var j = 1; j <= right.Length; j++)
            current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + (left[i - 1] == right[j - 1] ? 0 : 1));
            (previous, current) = (current, previous); }
        return previous[right.Length];
    }
    private static int CommonSuffix(string left, string right) { var count = 0; while (count < left.Length && count < right.Length && left[^(count + 1)] == right[^(count + 1)]) count++; return count; }
    private static int LongestCommonSubsequence(string left, string right)
    {
        var values = new int[right.Length + 1]; for (var i = 1; i <= left.Length; i++) { var diagonal = 0; for (var j = 1; j <= right.Length; j++)
        { var old = values[j]; values[j] = left[i - 1] == right[j - 1] ? diagonal + 1 : Math.Max(values[j], values[j - 1]); diagonal = old; } } return values[^1];
    }
    private static OperationModeTextMatch None(double similarity = 0) => new(OperationModeState.Unknown, "", OperationModeMatchMethod.None, similarity);
}
