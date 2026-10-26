using System.Globalization;
using System.Text;
using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public sealed record AdvanceGroupReading(string Group, decimal? Value, bool RegionValid, bool OcrSuccessful, bool CacheUsed)
{
    public DateTime CapturedAt { get; init; }
    public string RawText { get; init; } = "";
    public bool IntegerFormatValid => Value.HasValue && Value.Value == decimal.Truncate(Value.Value)
        && !RawText.Contains('.') && !RawText.Contains(',');
}

public sealed record AdvanceGroupDecision(DateTime Timestamp, string MachineId, string? RingNo,
    IReadOnlyDictionary<string, AdvanceGroupReading> Groups, AdvanceSelectionMode SelectionMode, string PrimaryGroup,
    decimal? OfficialAdvanceMm, string DecisionSource, decimal? ActualGroupDifferenceMm,
    OcrConfidenceStatus ConfidenceStatus, string Reason, IReadOnlyList<string> Warnings)
{
    public bool IsReliable => OfficialAdvanceMm.HasValue && ConfidenceStatus == OcrConfidenceStatus.Reliable && PhysicalAccepted;
    public bool AllContributingReadsReal { get; init; }
    public decimal? DecisionInputValue { get; init; }
    public string? TemporalStatus { get; init; }
    public bool TemporalPending { get; init; }
    public bool PhysicalAccepted { get; init; } = true;
    public AdvancePhysicalStatus PhysicalStatus { get; init; } = AdvancePhysicalStatus.NotApplied;
    public string? PhysicalReason { get; init; }
    public decimal? PreviousOfficialAdvanceMm { get; init; }
    public int ValidGroupCount { get; init; }
    public int ConsistentGroupCount { get; init; }
    public bool TemporalConfirmed { get; init; }
    public DateTime CapturedAt { get; init; }
}

public sealed class AdvanceGroupDecisionService
{
    private readonly OcrProfile _profile;
    private readonly string _csvPath;
    private readonly Dictionary<string, int> _zeroCounts = new();
    private string? _ringSessionId;
    private DateTime _lastSingleCapture;
    private decimal? _singleCandidate;
    private int _singleCount;
    private decimal? _lastOfficial;

    public AdvanceGroupDecisionService(OcrProfile profile, string? csvPath = null)
    {
        _profile = profile;
        _csvPath = csvPath ?? Path.Combine(AppContext.BaseDirectory, "Diagnostics", "AdvanceGroupDecisions.csv");
    }

    public AdvanceGroupDecision Decide(DateTime timestamp, string machineId, string? ringNo,
        IEnumerable<AdvanceGroupReading> readings, string? ringSessionId = null,
        OperationModeState operationMode = OperationModeState.Unknown)
    {
        if (!string.IsNullOrWhiteSpace(ringSessionId) && !string.Equals(_ringSessionId, ringSessionId, StringComparison.Ordinal))
        {
            _ringSessionId = ringSessionId; _lastSingleCapture = default; _singleCandidate = null; _singleCount = 0; _lastOfficial = null;
        }
        var map = readings.ToDictionary(item => item.Group.ToUpperInvariant(), StringComparer.OrdinalIgnoreCase);
        var enabled = new HashSet<string>(_profile.AdvanceEnabledGroups.Select(value => value.ToUpperInvariant()), StringComparer.OrdinalIgnoreCase);
        var observed = map.Values.Where(item => enabled.Contains(item.Group) && item.RegionValid && item.OcrSuccessful).ToList();
        var invalidInteger = observed.Where(item => !item.IntegerFormatValid).ToArray();
        var valid = observed.Where(item => item.IntegerFormatValid
            && item.Value >= _profile.AdvanceMinimumValidValue && item.Value <= 2500).ToList();
        var warnings = UpdateZeroWarnings(map, enabled);
        var consistent = FindLargestConsistentSet(valid, _profile.AdvanceGroupAgreementToleranceMm);
        var required = Math.Max(2, Math.Max(_profile.MinimumConsistentAdvanceGroups, _profile.AdvanceMinimumRequiredValidGroups));
        var capturedAt = valid.Select(item => item.CapturedAt == default ? timestamp : item.CapturedAt).DefaultIfEmpty(timestamp).Max();
        var previousOfficial = _lastOfficial;
        decimal? official = previousOfficial, candidate = null;
        var source = "HoldPreviousOfficial";
        var confidence = previousOfficial.HasValue ? OcrConfidenceStatus.Partial : OcrConfidenceStatus.Unreadable;
        var temporalConfirmed = false;
        string reason;

        if (invalidInteger.Length > 0 && valid.Count == 0)
            reason = "InvalidIntegerFormat: Advance decimal/bozuk OCR adayı resmî karara alınmadı.";
        else if (valid.Count == 0)
            reason = "NoValidIntegerGroups: geçerli integer ilerleme grubu yok.";
        else if (consistent.Count >= required)
        {
            (candidate, _) = Select(consistent, _profile.AdvanceSelectionMode, _profile.AdvancePrimaryGroup);
            official = candidate; source = "MultiGroupAgreement"; confidence = OcrConfidenceStatus.Reliable;
            reason = $"{consistent.Count} grup ±{_profile.AdvanceGroupAgreementToleranceMm:0} mm toleransında uzlaştı.";
            _singleCandidate = null; _singleCount = 0;
        }
        else if (valid.Count > 1)
        {
            source = "GroupConflict";
            reason = $"GroupConflict: gruplar ±{_profile.AdvanceGroupAgreementToleranceMm:0} mm toleransında uzlaşmadı; önceki resmî değer korundu.";
            _singleCandidate = null; _singleCount = 0;
        }
        else
        {
            var one = valid[0]; candidate = one.Value;
            var primary = one.Group.Equals(_profile.AdvancePrimaryGroup, StringComparison.OrdinalIgnoreCase);
            var independent = primary && !one.CacheUsed && capturedAt > _lastSingleCapture;
            if (independent)
            {
                if (_singleCandidate.HasValue && Math.Abs(_singleCandidate.Value - one.Value!.Value) <= _profile.AdvanceConfirmationTolerance) _singleCount++;
                else { _singleCandidate = one.Value; _singleCount = 1; }
                _lastSingleCapture = capturedAt;
            }
            var needed = Math.Max(2, _profile.AdvanceSingleGroupConfirmationReadings);
            if (primary && _singleCount >= needed)
            {
                official = one.Value; source = "PrimaryGroupTemporalConfirmation"; temporalConfirmed = true;
                confidence = OcrConfidenceStatus.Partial;
                reason = $"Tek primary grup {_singleCount}/{needed} bağımsız gerçek okumayla doğrulandı.";
            }
            else reason = primary ? $"Tek grup, doğrulama bekleniyor: {one.Value:0} ({_singleCount}/{needed})."
                : "Tek geçerli grup primary grup değil; önceki resmî değer korundu.";
        }

        if (operationMode == OperationModeState.RingBuilding)
        {
            official = previousOfficial; source = "RingBuildingFreeze";
            confidence = previousOfficial.HasValue ? OcrConfidenceStatus.Reliable : OcrConfidenceStatus.Unreadable;
            reason = "Doğrulanmış RingBuilding sırasında resmî ilerleme donduruldu.";
        }

        var difference = valid.Count > 1 ? valid.Max(item => item.Value) - valid.Min(item => item.Value) : (decimal?)null;
        var pending = source == "HoldPreviousOfficial" && candidate.HasValue;
        var allReal = consistent.Count > 0 && consistent.All(item => !item.CacheUsed);
        var result = new AdvanceGroupDecision(timestamp, machineId, ringNo, map, _profile.AdvanceSelectionMode,
            _profile.AdvancePrimaryGroup, official, source, difference, confidence, reason, warnings)
        {
            AllContributingReadsReal = allReal, DecisionInputValue = candidate,
            TemporalStatus = pending ? reason : null, TemporalPending = pending,
            PreviousOfficialAdvanceMm = previousOfficial, ValidGroupCount = valid.Count,
            ConsistentGroupCount = consistent.Count, TemporalConfirmed = temporalConfirmed, CapturedAt = capturedAt
        };
        Append(result); return result;
    }

    public void CommitOfficial(decimal? value) { if (value.HasValue) _lastOfficial = value; }

    private static List<AdvanceGroupReading> FindLargestConsistentSet(List<AdvanceGroupReading> valid, decimal tolerance)
    {
        var sorted = valid.OrderBy(item => item.Value).ToArray(); var best = new List<AdvanceGroupReading>();
        for (var left = 0; left < sorted.Length; left++)
        for (var right = left; right < sorted.Length && sorted[right].Value - sorted[left].Value <= tolerance; right++)
        {
            var set = sorted[left..(right + 1)].ToList(); if (set.Count > best.Count) best = set;
        }
        return best;
    }

    private static (decimal? Value, string Source) Select(List<AdvanceGroupReading> valid, AdvanceSelectionMode mode, string primary)
    {
        AdvanceGroupReading? Named(string name) => valid.FirstOrDefault(item => item.Group.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (mode is AdvanceSelectionMode.GroupA or AdvanceSelectionMode.GroupB or AdvanceSelectionMode.GroupC or AdvanceSelectionMode.GroupD)
        {
            var requested = Named(mode.ToString()[^1].ToString()) ?? Named(primary);
            if (requested is not null) return (requested.Value, $"Grup {requested.Group}");
        }
        var values = valid.Select(item => item.Value!.Value).OrderBy(value => value).ToArray();
        return mode switch
        {
            AdvanceSelectionMode.AverageOfValidGroups or AdvanceSelectionMode.ClosestValidPairAverage => (decimal.Round(values.Average(), 0), "Uyumlu gruplar ortalaması"),
            AdvanceSelectionMode.MaximumOfValidGroups => (values.Max(), "Geçerli grupların maksimumu"),
            AdvanceSelectionMode.MinimumOfValidGroups => (values.Min(), "Geçerli grupların minimumu"),
            _ => (values.Length % 2 == 1 ? values[values.Length / 2] : decimal.Round((values[values.Length / 2 - 1] + values[values.Length / 2]) / 2, 0), "Uyumlu gruplar medyanı")
        };
    }

    private IReadOnlyList<string> UpdateZeroWarnings(Dictionary<string, AdvanceGroupReading> map, HashSet<string> enabled)
    {
        var warnings = new List<string>();
        var active = map.Values.Any(item => enabled.Contains(item.Group) && item.Value >= _profile.AdvanceMinimumValidValue);
        foreach (var item in map.Values)
        {
            if (!item.CacheUsed && item.OcrSuccessful && item.Value == 0 && active) _zeroCounts[item.Group] = _zeroCounts.GetValueOrDefault(item.Group) + 1;
            else if (item.Value != 0) _zeroCounts[item.Group] = 0;
            if (_zeroCounts.GetValueOrDefault(item.Group) >= _profile.AdvanceSensorZeroWarningReadings)
                warnings.Add($"Grup {item.Group} ilerleme sensörü çalışmıyor veya devre dışı olabilir.");
        }
        return warnings;
    }

    private void Append(AdvanceGroupDecision decision)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_csvPath)!);
            var header = !File.Exists(_csvPath) || new FileInfo(_csvPath).Length == 0;
            using var writer = new StreamWriter(_csvPath, true, new UTF8Encoding(header));
            if (header) writer.WriteLine("Timestamp,CapturedAt,MachineId,RingNo,RingSessionId,GroupA,GroupB,GroupC,GroupD,ValidGroupCount,ConsistentGroupCount,AgreementTolerance,CandidateAdvance,TemporalConfirmed,PhysicalAccepted,OfficialAdvance,DecisionSource,Confidence,Reason");
            string Q(object? value) => $"\"{Convert.ToString(value, CultureInfo.InvariantCulture)?.Replace("\"", "\"\"")}\"";
            string V(string group) => decision.Groups.TryGetValue(group, out var item) ? Convert.ToString(item.Value, CultureInfo.InvariantCulture) ?? "" : "";
            writer.WriteLine(string.Join(',', Q(decision.Timestamp.ToString("O")), Q(decision.CapturedAt.ToString("O")), Q(decision.MachineId), Q(decision.RingNo), Q(_ringSessionId),
                Q(V("A")), Q(V("B")), Q(V("C")), Q(V("D")), Q(decision.ValidGroupCount), Q(decision.ConsistentGroupCount),
                Q(_profile.AdvanceGroupAgreementToleranceMm), Q(decision.DecisionInputValue), Q(decision.TemporalConfirmed), Q(decision.PhysicalAccepted),
                Q(decision.OfficialAdvanceMm), Q(decision.DecisionSource), Q(decision.ConfidenceStatus), Q(decision.Reason)));
        }
        catch { }
    }
}
