using System.Globalization;
using System.Text;
using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public enum AdvancePhysicalStatus
{
    Accepted,
    PhysicalSuspicious,
    ForwardConfirmationPending,
    HeldOutsideExcavation,
    PhysicalStateReset,
    NotApplied,
    Error
}

public sealed record AdvancePhysicalConsistencyInput(
    string MachineId,
    string? RingNo,
    string? RingSessionId,
    decimal? CurrentOfficialAdvanceMm,
    decimal? ObservedAdvanceMm,
    OperationModeState OperationMode,
    RingSessionState RingSessionState,
    DateTime Timestamp,
    bool WasMonitoringStartedMidExcavation,
    bool TemporalConfirmed,
    bool IsIndependentRealReading,
    MonitoringRunState MonitoringState = MonitoringRunState.Running);

public sealed record AdvancePhysicalConsistencyResult(
    bool AcceptedForAdvanceDecision,
    AdvancePhysicalStatus PhysicalStatus,
    string Reason,
    string SuggestedAction,
    decimal? OfficialAdvanceMm,
    decimal? DeltaMm,
    bool StateReset = false);

/// <summary>Ring içi ilerleme için temporal OCR onayından bağımsız, fiziksel emniyet katmanı.</summary>
public sealed class AdvancePhysicalConsistencyService
{
    private sealed class RingState
    {
        public string? RingNo;
        public decimal? Official;
    }

    private readonly object _sync = new();
    private readonly OcrProfile _profile;
    private readonly string _csvPath;
    private readonly Dictionary<string, RingState> _states = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (string? SessionId, string? RingNo, decimal? Official)> _lastMachineRing = new(StringComparer.OrdinalIgnoreCase);

    public AdvancePhysicalConsistencyService(OcrProfile profile, string? csvPath = null)
    {
        _profile = profile;
        _csvPath = csvPath ?? Path.Combine(AppContext.BaseDirectory, "Diagnostics", "AdvancePhysicalConsistency.csv");
    }

    public AdvancePhysicalConsistencyResult Evaluate(AdvancePhysicalConsistencyInput input)
    {
        lock (_sync)
        {
            try
            {
                if (!string.Equals(input.MachineId, "1031", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(input.RingSessionId))
                    return Result(false, AdvancePhysicalStatus.NotApplied,
                        "Fiziksel ilerleme koruması yalnızca 1031 aktif RingSession için uygulanır.", "Mevcut karar akışını koru.",
                        input.CurrentOfficialAdvanceMm, null, input, false);

                var key = input.MachineId + "|" + input.RingSessionId;
                var isNewRing = !_states.TryGetValue(key, out var state);
                if (isNewRing)
                {
                    state = new RingState { RingNo = input.RingNo };
                    _states[key] = state;
                    _lastMachineRing.TryGetValue(input.MachineId, out var previous);
                    state.Official = input.IsIndependentRealReading ? input.ObservedAdvanceMm : null;
                    var baseline = state.Official ?? input.CurrentOfficialAdvanceMm;
                    state.Official = baseline;
                    _lastMachineRing[input.MachineId] = (input.RingSessionId, input.RingNo, state.Official);
                    var resetReason = previous.SessionId is null
                        ? "Ring bazlı fiziksel ilerleme durumu başlatıldı."
                        : $"PhysicalStateReset: önceki ring {previous.RingNo ?? "—"} ({previous.Official:0} mm), yeni ring {input.RingNo ?? "—"}, yeni baseline {state.Official:0} mm.";
                    return Result(true, AdvancePhysicalStatus.PhysicalStateReset, resetReason,
                        "Yeni RingSession baseline'ını kullan.", state.Official, null, input, true);
                }

                var current = state!.Official ?? input.CurrentOfficialAdvanceMm;
                if (!input.ObservedAdvanceMm.HasValue)
                    return Result(false, AdvancePhysicalStatus.PhysicalSuspicious, "Gözlenen ilerleme değeri yok.",
                        "Son resmî ilerlemeyi koru.", current, null, input, false);

                var delta = current.HasValue ? input.ObservedAdvanceMm.Value - current.Value : (decimal?)null;
                if (input.MonitoringState is MonitoringRunState.Paused or MonitoringRunState.Resuming or MonitoringRunState.Stopped)
                    return Result(false, AdvancePhysicalStatus.HeldOutsideExcavation, "Duraklatılmış/yeniden başlatılan izleme fiziksel state'i ilerletmez.",
                        "Son resmî ilerlemeyi koru.", current, delta, input, false);
                if (!input.IsIndependentRealReading)
                    return Result(false, AdvancePhysicalStatus.PhysicalSuspicious, "Cache sonucu yeni fiziksel kanıt sayılmadı.",
                        "Gerçek OCR okumasını bekle.", current, delta, input, false);

                var excavation = input.OperationMode == OperationModeState.Excavation
                    && input.RingSessionState is RingSessionState.ExcavationInProgress or RingSessionState.StableDataPending or RingSessionState.StableDataCaptured;
                if (!excavation)
                    return Result(false, AdvancePhysicalStatus.HeldOutsideExcavation,
                        "Aktif RingSession kazı durumunda değil; resmî ilerleme değiştirilmedi.", "Son resmî ilerlemeyi koru.", current, delta, input, false);

                if (!current.HasValue)
                {
                    state.Official = input.ObservedAdvanceMm;
                    return Accept(state, input, delta, "İlk gerçek ring ilerleme değeri baseline olarak kabul edildi.");
                }

                if (delta < -_profile.AdvanceMaximumBackwardToleranceMm)
                {
                    var large = Math.Abs(delta.Value) >= _profile.AdvanceLargeBackwardJumpMm;
                    var reason = large
                        ? "Ring içi fiziksel olarak beklenmeyen büyük geriye ilerleme."
                        : "Ring içi geriye ilerleme toleransı aşıldı.";
                    return Result(false, AdvancePhysicalStatus.PhysicalSuspicious, reason,
                        "Gözlenen değeri tanılamada tut; son resmî ilerlemeyi koru.", current, delta, input, false);
                }

                if (delta > _profile.AdvanceMaximumForwardJumpPerRealReadingMm && !input.TemporalConfirmed)
                    return Result(false, AdvancePhysicalStatus.ForwardConfirmationPending,
                        "Tek gerçek okumadaki ileri sıçrama temporal doğrulama bekliyor.",
                        "Benzer bağımsız gerçek OCR okumasını bekle.", current, delta, input, false);

                state.Official = input.ObservedAdvanceMm;
                return Accept(state, input, delta, delta > _profile.AdvanceMaximumForwardJumpPerRealReadingMm
                    ? "Büyük ileri hareket ardışık temporal doğrulamayla kabul edildi."
                    : "Ring içi fiziksel hareket izin verilen aralıkta.");
            }
            catch (Exception exception)
            {
                return Result(false, AdvancePhysicalStatus.Error, "Fiziksel tutarlılık hatası: " + exception.Message,
                    "Monitoring devam etsin; mevcut resmî ilerlemeyi koru.", input.CurrentOfficialAdvanceMm, null, input, false);
            }
        }
    }

    private AdvancePhysicalConsistencyResult Accept(RingState state, AdvancePhysicalConsistencyInput input, decimal? delta, string reason)
    {
        _lastMachineRing[input.MachineId] = (input.RingSessionId, input.RingNo, state.Official);
        return Result(true, AdvancePhysicalStatus.Accepted, reason, "Resmî ilerleme kararına aktar.", state.Official, delta, input, false);
    }

    private AdvancePhysicalConsistencyResult Result(bool accepted, AdvancePhysicalStatus status, string reason, string action,
        decimal? official, decimal? delta, AdvancePhysicalConsistencyInput input, bool reset)
    {
        var result = new AdvancePhysicalConsistencyResult(accepted, status, reason, action, official, delta, reset);
        Append(input, result);
        return result;
    }

    private void Append(AdvancePhysicalConsistencyInput input, AdvancePhysicalConsistencyResult result)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_csvPath)!);
            var header = !File.Exists(_csvPath) || new FileInfo(_csvPath).Length == 0;
            using var writer = new StreamWriter(_csvPath, true, new UTF8Encoding(header));
            if (header) writer.WriteLine("Timestamp,MachineId,RingNo,RingSessionId,ObservedAdvanceMm,CurrentOfficialAdvanceMm,DeltaMm,OperationMode,RingState,TemporalConfirmed,PhysicalAccepted,PhysicalStatus,Reason");
            string Q(object? value) => $"\"{Convert.ToString(value, CultureInfo.InvariantCulture)?.Replace("\"", "\"\"")}\"";
            writer.WriteLine(string.Join(',', Q(input.Timestamp.ToString("O")), Q(input.MachineId), Q(input.RingNo), Q(input.RingSessionId),
                Q(input.ObservedAdvanceMm), Q(input.CurrentOfficialAdvanceMm), Q(result.DeltaMm), Q(input.OperationMode), Q(input.RingSessionState),
                Q(input.TemporalConfirmed), Q(result.AcceptedForAdvanceDecision), Q(result.PhysicalStatus), Q(result.Reason)));
        }
        catch { }
    }
}
