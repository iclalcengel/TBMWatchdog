using System.Globalization;
using System.Text;
using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public enum StableZoneStatus { Waiting, Candidate, Confirmed, Missed }

public sealed record StableZoneCandidateInput(DateTime Timestamp, string MachineId, RingSession? RingSession,
    decimal? AdvanceMm, bool ValueValidated, bool RealOcrExecuted, bool RegionValid, bool ScreenAlignmentValid,
    OperationModeState OperationMode, MonitoringRunState MonitoringState);

public sealed record StableZoneCandidateResult(bool Candidate, bool Confirmed, bool NewlyConfirmed,
    int ConfirmationCount, int RequiredConfirmationCount, decimal? AdvanceMm, string DisplayText, string Reason,
    StableZoneStatus Status, bool NewlyMissed = false);

public sealed class StableZoneCandidateService
{
    private readonly object _sync = new(); private readonly decimal _minimum; private readonly decimal _maximum;
    private readonly int _required; private readonly string _csvPath; private string? _ringSessionId;
    private int _count; private StableZoneStatus _status = StableZoneStatus.Waiting; private decimal? _lastAdvance;
    private bool _waitingLogged;

    public StableZoneCandidateService(OcrProfile profile, string? csvPath = null)
    { _minimum = profile.StableZoneMinimumAdvanceMm; _maximum = profile.StableZoneMaximumAdvanceMm;
        _required = Math.Max(2, profile.StableZoneConfirmationReadings);
        _csvPath = csvPath ?? Path.Combine(AppContext.BaseDirectory, "Diagnostics", "StableZoneCandidates.csv"); }

    public void OnMonitoringStateChanged(MonitoringRunState state)
    { if (state is MonitoringRunState.Paused or MonitoringRunState.Resuming or MonitoringRunState.Stopped)
        lock (_sync) { if (_status == StableZoneStatus.Candidate) { _count = 0; _status = StableZoneStatus.Waiting; } } }
    public void BeginRing(string? ringSessionId) { lock (_sync) { _ringSessionId = ringSessionId; _count = 0; _status = StableZoneStatus.Waiting; _lastAdvance = null; _waitingLogged = false; } }

    public StableZoneCandidateResult Evaluate(StableZoneCandidateInput input)
    {
        lock (_sync)
        {
            if (!string.Equals(input.MachineId, "1031", StringComparison.OrdinalIgnoreCase) || input.RingSession is null)
                return new(false, false, false, 0, _required, input.AdvanceMm, "Stabil bölge: Bekleniyor",
                    input.RingSession is null ? "Aktif RingSession yok." : "Stabil bölge yalnızca 1031 için etkin.", StableZoneStatus.Waiting);
            EnsureRing(input.RingSession?.SessionId);
            if (input.AdvanceMm.HasValue && input.ValueValidated) _lastAdvance = input.AdvanceMm;
            if (_status == StableZoneStatus.Confirmed) return Result(input, false, false, "Bu ring için stabil bölge daha önce doğrulandı.");
            if (_status == StableZoneStatus.Missed) return Result(input, false, false, "Bu ring için stabil bölge daha önce kaçırıldı.");

            var ringBuilding = input.RingSession?.CurrentState == RingSessionState.RingBuilding || input.OperationMode == OperationModeState.RingBuilding;
            var passedMaximum = input.ValueValidated && input.AdvanceMm > _maximum;
            if (input.RingSession is not null && (passedMaximum || ringBuilding))
            {
                _count = 0; _status = StableZoneStatus.Missed;
                const string missedReason = "Stabil aralıkta yeterli doğrulama alınamadı.";
                Append(input, missedReason); return Result(input, false, true, missedReason);
            }

            var eligibility = EligibilityFailure(input);
            if (eligibility is not null)
            {
                if (input.RingSession is not null && input.AdvanceMm < _minimum) { _count = 0; _status = StableZoneStatus.Waiting;
                    if (!_waitingLogged) { Append(input, "Resmî Advance stabil bölge minimumunun altında."); _waitingLogged = true; } }
                return Result(input, false, false, eligibility);
            }
            if (!input.RealOcrExecuted) return Result(input, false, false, "Önbellek sonucu doğrulama sayacına eklenmedi.");
            _count++; _status = _count >= _required ? StableZoneStatus.Confirmed : StableZoneStatus.Candidate;
            var newlyConfirmed = _status == StableZoneStatus.Confirmed;
            var reason = newlyConfirmed ? "Ardışık gerçek OCR okumalarıyla stabil bölge adayı doğrulandı."
                : "Stabil bölge adayı için ardışık gerçek OCR doğrulaması bekleniyor.";
            Append(input, reason); return Result(input, newlyConfirmed, false, reason);
        }
    }

    private void EnsureRing(string? ringSessionId)
    {
        if (_ringSessionId == ringSessionId) return;
        _ringSessionId = ringSessionId; _count = 0; _status = StableZoneStatus.Waiting; _lastAdvance = null; _waitingLogged = false;
    }

    private string? EligibilityFailure(StableZoneCandidateInput input)
    {
        if (!string.Equals(input.MachineId, "1031", StringComparison.OrdinalIgnoreCase)) return "Stabil bölge yalnızca 1031 için etkin.";
        if (input.RingSession is null) return "Aktif RingSession yok.";
        if (input.RingSession.CurrentState != RingSessionState.ExcavationInProgress) return "RingSession kazı durumunda değil.";
        if (input.MonitoringState != MonitoringRunState.Running) return "İzleme Running durumunda değil.";
        if (!input.RegionValid) return "AdvanceMm bölgesi geçersiz.";
        if (!input.ScreenAlignmentValid) return "Ekran hizalaması geçersiz.";
        if (!input.ValueValidated || input.AdvanceMm is null) return "AdvanceMm doğrulanmadı.";
        if (input.AdvanceMm < _minimum) return "AdvanceMm stabil bölgeye henüz ulaşmadı.";
        return null;
    }

    private StableZoneCandidateResult Result(StableZoneCandidateInput input, bool newlyConfirmed, bool newlyMissed, string reason)
    {
        var value = input.AdvanceMm ?? _lastAdvance;
        var text = _status switch
        {
            StableZoneStatus.Candidate => $"Stabil bölge adayı: {value:0} mm ({_count}/{_required})",
            StableZoneStatus.Confirmed => "Stabil bölge adayı doğrulandı",
            StableZoneStatus.Missed => "Stabil bölge: Kaçırıldı • Stabil aralıkta yeterli doğrulama alınamadı.",
            _ => "Stabil bölge: Bekleniyor"
        };
        return new(_status == StableZoneStatus.Candidate, _status == StableZoneStatus.Confirmed, newlyConfirmed,
            _count, _required, value, text, reason, _status, newlyMissed);
    }

    private void Append(StableZoneCandidateInput input, string reason)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_csvPath)!); EnsureCurrentSchema();
            var createHeader = !File.Exists(_csvPath) || new FileInfo(_csvPath).Length == 0;
            using var writer = new StreamWriter(_csvPath, true, new UTF8Encoding(true));
            if (createHeader) writer.WriteLine("Timestamp,MachineId,RingNo,RingSessionId,AdvanceMm,MinimumAdvanceMm,MaximumAdvanceMm,ConfirmationCount,RequiredConfirmationCount,CandidateConfirmed,OperationMode,MonitoringState,RegionValid,Status,Reason");
            string Q(object? value) => $"\"{Convert.ToString(value, CultureInfo.InvariantCulture)?.Replace("\"", "\"\"")}\"";
            writer.WriteLine(string.Join(',', Q(input.Timestamp.ToString("O")), Q(input.MachineId), Q(input.RingSession?.RingNo), Q(input.RingSession?.SessionId),
                Q(input.AdvanceMm ?? _lastAdvance), Q(_minimum), Q(_maximum), Q(_count), Q(_required), Q(_status == StableZoneStatus.Confirmed),
                Q(input.OperationMode), Q(input.MonitoringState), Q(input.RegionValid), Q(_status), Q(reason)));
        }
        catch { }
    }

    private void EnsureCurrentSchema()
    {
        if (!File.Exists(_csvPath) || new FileInfo(_csvPath).Length == 0) return;
        var header = File.ReadLines(_csvPath).FirstOrDefault() ?? ""; if (header.Contains(",Status,", StringComparison.Ordinal)) return;
        var backup = _csvPath + $".pre-status.{DateTime.Now:yyyyMMdd_HHmmss_fff}.bak"; File.Move(_csvPath, backup);
    }
}
