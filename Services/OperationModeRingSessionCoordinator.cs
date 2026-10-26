using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public sealed record OperationModeRingContext(bool OperationRegionValid, bool ScreenAlignmentValid,
    decimal? OfficialAdvanceMm = null, string? MonitoringSessionId = null);
public sealed record OperationModeRingResult(bool Updated, string Reason, RingSession? Session = null)
{
    public RingSession? PendingPreviousRing { get; init; }
    public bool NextRingCreated { get; init; }
}

public sealed class OperationModeRingSessionCoordinator
{
    private readonly object _sync = new();
    private readonly RingSessionService _rings;
    private MonitoringRunState _monitoringState = MonitoringRunState.Stopped;
    private string? _baselinedRingSessionId;
    private OperationModeState _lastMode = OperationModeState.Unknown;
    private bool _awaitingResumeBaseline;

    public OperationModeRingSessionCoordinator(RingSessionService rings) => _rings = rings;
    public event Action<string, Exception>? UpdateFailed;

    public void SetMonitoringState(MonitoringRunState state)
    {
        lock (_sync)
        {
            _monitoringState = state;
            if (state == MonitoringRunState.Resuming) _awaitingResumeBaseline = true;
        }
    }

    public OperationModeRingResult Process(OperationModeTransition transition, OperationModeRingContext context)
    {
        lock (_sync)
        {
            if (!CanProcess(transition.MachineId, context, out var ring, out var reason)) return new(false, reason, ring);
            if (_awaitingResumeBaseline)
            {
                _lastMode = transition.NewState;
                return new(false, "Devam sonrası ilk doğrulanmış durum yalnızca senkronizasyon tabanı olarak kabul edildi", ring);
            }
            return ApplyConfirmedState(ring!, transition.NewState, transition.Timestamp, transition.PreviousState);
        }
    }

    public OperationModeRingResult ProcessConfirmedState(string machineId, OperationModeState state, DateTime timestamp,
        OperationModeRingContext context)
    {
        lock (_sync)
        {
            if (state == OperationModeState.Unknown) return new(false, "OperationMode doğrulanmadı");
            if (!CanProcess(machineId, context, out var ring, out var reason)) return new(false, reason, ring);
            if (_awaitingResumeBaseline)
            {
                _awaitingResumeBaseline = false; _lastMode = state;
                return new(false, "Devam sonrası doğrulanmış ekran durumu yeni taban olarak kaydedildi", ring);
            }
            return ApplyConfirmedState(ring!, state, timestamp, _lastMode);
        }
    }

    private OperationModeRingResult ApplyConfirmedState(RingSession ring, OperationModeState state, DateTime timestamp,
        OperationModeState previousMode)
    {
        if (_baselinedRingSessionId != ring.SessionId)
        {
            _baselinedRingSessionId = ring.SessionId; _lastMode = state;
            if (state == OperationModeState.Excavation)
                return Update(ring, value =>
                {
                    value.CurrentState = RingSessionState.ExcavationInProgress;
                    value.WasMonitoringStartedMidExcavation = true; value.StartedAt = null;
                    value.SawConfirmedExcavation = true;
                }, "Ring oturumu aktif kazı sırasında izlenmeye başlandı", timestamp);
            if (state == OperationModeState.RingBuilding)
                return Update(ring, value =>
                {
                    value.CurrentState = RingSessionState.RingBuilding; value.WasMonitoringStartedMidExcavation = false;
                    value.IsCompleted = false; value.EndedAt = null;
                }, "Ring oturumu montaj sırasında başlatıldı", timestamp);
            return new(false, "Desteklenmeyen başlangıç durumu", ring);
        }

        var effectivePrevious = previousMode == OperationModeState.Unknown ? _lastMode : previousMode;
        if (state == _lastMode || state == effectivePrevious)
        {
            var active = _rings.GetActive(ring.MachineId);
            if (state == OperationModeState.RingBuilding
                && ring.CurrentState is not (RingSessionState.RingBuilding or RingSessionState.FinalDataPending)
                && active?.SessionId == ring.SessionId)
                return Update(ring, value => value.CurrentState = RingSessionState.RingBuilding,
                    "Doğrulanmış RingBuilding ile çelişen RingSession durumu yeniden senkronize edildi", timestamp);
            return new(false, "Doğrulanmış durum değişmedi", ring);
        }
        _lastMode = state;
        if (effectivePrevious == OperationModeState.RingBuilding && state == OperationModeState.Excavation)
        {
            if (ring.CurrentState == RingSessionState.FinalDataPending
                && ring.SawConfirmedExcavation && ring.SawConfirmedRingBuildingAfterExcavation)
            {
                try
                {
                    var next = _rings.CreateNextFromPending(ring.MachineId, ring.SessionId,
                        _lastContext.MonitoringSessionId ?? ring.MonitoringSessionId, timestamp);
                    _baselinedRingSessionId = next.SessionId;
                    return new(true, $"Ring {ring.RingNo} kazısı tamamlandı. Ring {next.RingNo} takibi başlatıldı.", next)
                    { PendingPreviousRing = ring, NextRingCreated = true };
                }
                catch (Exception exception)
                {
                    UpdateFailed?.Invoke("Sonraki RingSession oluşturulamadı.", exception);
                    return new(false, exception.Message, null) { PendingPreviousRing = ring };
                }
            }
            // Uygulama RingBuilding ortasında açıldıysa +1 yapılmaz; mevcut manuel ring
            // yalnızca yeni doğrulanmış kazı için tabanlanır.
            return Update(ring, value =>
            {
                value.CurrentState = RingSessionState.ExcavationInProgress; value.StartedAt = timestamp;
                value.WasMonitoringStartedMidExcavation = false; value.SawConfirmedExcavation = true;
            }, "RingBuilding başlangıç tabanından sonra doğrulanmış kazı; otomatik ring artırılmadı", timestamp);
        }
        if (effectivePrevious == OperationModeState.Excavation && state == OperationModeState.RingBuilding)
        {
            if (!ring.SawConfirmedExcavation)
                return new(false, "Ring için önceden doğrulanmış Excavation kanıtı yok; final/pending geçişi yapılmadı", ring);
            try
            {
                var pending = _rings.MoveActiveToFinalDataPending(ring.MachineId,
                    _lastContext.OfficialAdvanceMm, "Doğrulanmış Excavation -> RingBuilding", timestamp);
                return new(true, "Kazı tamamlandı; ring son verileri bekleniyor", null)
                { PendingPreviousRing = pending };
            }
            catch (Exception exception)
            {
                UpdateFailed?.Invoke("Ring FinalDataPending durumuna alınamadı.", exception);
                return new(false, exception.Message, ring);
            }
        }
        return new(false, "OperationMode geçişi RingSession değişikliği gerektirmiyor", ring);
    }

    private OperationModeRingResult Update(RingSession ring, Action<RingSession> change, string reason, DateTime timestamp)
    {
        try
        {
            var current = _rings.GetActive(ring.MachineId);
            if (current is null || current.SessionId != ring.SessionId) return new(false, "Aktif ring oturumu değişti");
            var updated = _rings.Update(ring.MachineId, change, reason, timestamp);
            return new(true, reason, updated);
        }
        catch (Exception exception)
        {
            UpdateFailed?.Invoke("OperationMode -> RingSession güncellemesi başarısız.", exception);
            return new(false, exception.Message, _rings.GetActive(ring.MachineId));
        }
    }

    private bool CanProcess(string machineId, OperationModeRingContext context, out RingSession? ring, out string reason)
    {
        ring = null;
        if (!string.Equals(machineId, "1031", StringComparison.OrdinalIgnoreCase)) { reason = "Makine 1031 değil"; return false; }
        if (_monitoringState != MonitoringRunState.Running) { reason = "İzleme Running durumunda değil"; return false; }
        if (!context.OperationRegionValid) { reason = "OperationMode bölgesi geçersiz"; return false; }
        if (!context.ScreenAlignmentValid) { reason = "Ekran hizalaması geçersiz"; return false; }
        _lastContext = context;
        ring = _rings.GetActive("1031") ?? _rings.GetPendingFinalData("1031").FirstOrDefault();
        if (ring is null) { reason = "Aktif RingSession yok"; return false; }
        reason = "Uygun"; return true;
    }

    private OperationModeRingContext _lastContext = new(false, false);
}
