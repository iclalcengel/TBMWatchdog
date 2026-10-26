using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public sealed class RingSessionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly object _sync = new();
    private readonly Dictionary<string, RingSession> _active = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<RingSession>> _pending = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<RingSession>> _unresolved = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _sessionsRoot;
    private readonly string _eventsPath;

    public RingSessionService(string? diagnosticsRoot = null)
    {
        DiagnosticsRoot = diagnosticsRoot ?? Path.Combine(AppContext.BaseDirectory, "Diagnostics");
        _sessionsRoot = Path.Combine(DiagnosticsRoot, "RingSessions");
        _eventsPath = Path.Combine(DiagnosticsRoot, "RingSessionEvents.csv");
        var recovered = DetectIncompleteSessions();
        IncompleteSessions = recovered.Where(item => item.CurrentState != RingSessionState.FinalDataPending).ToArray();
        foreach (var group in recovered.GroupBy(item => item.MachineId, StringComparer.OrdinalIgnoreCase))
        {
            var pending = group.Where(item => item.CurrentState == RingSessionState.FinalDataPending).Select(Clone).ToList();
            var unresolved = group.Where(item => item.CurrentState != RingSessionState.FinalDataPending).Select(Clone).ToList();
            if (pending.Count > 0) _pending[group.Key] = pending;
            if (unresolved.Count > 0) _unresolved[group.Key] = unresolved;
        }
    }

    public string DiagnosticsRoot { get; }
    public string EventsFilePath => _eventsPath;
    public IReadOnlyList<RingSession> IncompleteSessions { get; }
    public bool HasIncompleteSessions => IncompleteSessions.Count > 0;
    public string? StartupDiagnostic => HasIncompleteSessions ? "Tamamlanmamış ring oturumu bulundu." : null;

    public event Action<RingSession>? RingSessionCreated;
    public event Action<RingSession>? RingSessionUpdated;
    public event Action<RingSession>? RingSessionCompleted;
    public event Action<RingSession>? RingSessionCancelled;
    public event Action<string, Exception>? PersistenceFailed;

    public RingSession Create(string machineId, string ringNo, string? monitoringSessionId = null,
        bool wasMonitoringStartedMidExcavation = false, DateTime? now = null)
    {
        machineId = ValidateMachine(machineId); ringNo = ValidateRingNo(ringNo);
        RingSession session; RingSessionEvent change;
        lock (_sync)
        {
            if (_active.ContainsKey(machineId)) throw new InvalidOperationException($"{machineId} makinesi için zaten aktif bir ring oturumu var.");
            if (_unresolved.TryGetValue(machineId, out var unresolved) && unresolved.Count > 0)
                throw new InvalidOperationException($"{machineId} için tamamlanmamış ring oturumu çözülmeden yeni oturum başlatılamaz: {unresolved[0].RingNo}");
            var timestamp = now ?? DateTime.Now;
            session = new RingSession
            {
                MachineId = machineId, RingNo = ringNo, MonitoringSessionId = monitoringSessionId,
                CreatedAt = timestamp, LastUpdatedAt = timestamp,
                WasMonitoringStartedMidExcavation = wasMonitoringStartedMidExcavation,
                StartedAt = null, CurrentState = RingSessionState.WaitingForExcavation
            };
            _active[machineId] = session;
            change = Event(session, "Created", RingSessionState.None, session.CurrentState, "Ring oturumu oluşturuldu", timestamp);
            PersistSafe(session); AppendEventSafe(change);
            session = Clone(session);
        }
        RingSessionCreated?.Invoke(session); return session;
    }

    public bool HasActive(string machineId) { lock (_sync) return _active.ContainsKey(machineId); }
    public RingSession? GetActive(string machineId) { lock (_sync) return _active.TryGetValue(machineId, out var value) ? Clone(value) : null; }
    public IReadOnlyList<RingSession> GetPendingFinalData(string machineId)
    {
        lock (_sync) return _pending.TryGetValue(machineId, out var values)
            ? values.OrderByDescending(item => item.ExcavationEndedAt ?? item.LastUpdatedAt).Select(Clone).ToArray()
            : Array.Empty<RingSession>();
    }

    public RingSession MoveActiveToFinalDataPending(string machineId, decimal? finalOfficialAdvanceMm,
        string reason, DateTime? now = null)
    {
        RingSession snapshot; RingSessionEvent endedEvent; RingSessionEvent pendingEvent;
        lock (_sync)
        {
            if (!_active.Remove(machineId, out var session)) throw new InvalidOperationException("Aktif ring oturumu bulunamadı.");
            var timestamp = now ?? DateTime.Now; var previous = session.CurrentState;
            session.CurrentState = RingSessionState.FinalDataPending; session.IsCompleted = false;
            session.ExcavationEndedAt = timestamp; session.FinalOfficialAdvanceMm = finalOfficialAdvanceMm;
            session.SawConfirmedRingBuildingAfterExcavation = true; session.LastUpdatedAt = timestamp;
            if (!_pending.TryGetValue(machineId, out var values)) _pending[machineId] = values = [];
            values.RemoveAll(item => item.SessionId == session.SessionId); values.Add(session);
            endedEvent = Event(session, "ExcavationEnded", previous, RingSessionState.FinalDataPending,
                $"{reason}; FinalAdvance={finalOfficialAdvanceMm?.ToString(CultureInfo.InvariantCulture) ?? ""}", timestamp);
            pendingEvent = Event(session, "FinalDataPending", RingSessionState.FinalDataPending,
                RingSessionState.FinalDataPending, "Ring son verileri bekleniyor", timestamp);
            PersistSafe(session); AppendEventSafe(endedEvent); AppendEventSafe(pendingEvent); snapshot = Clone(session);
        }
        RingSessionUpdated?.Invoke(snapshot); return snapshot;
    }

    public RingSession CreateNextFromPending(string machineId, string pendingSessionId,
        string? monitoringSessionId, DateTime? now = null)
    {
        RingSession created; RingSessionEvent change;
        lock (_sync)
        {
            if (_active.ContainsKey(machineId)) throw new InvalidOperationException($"{machineId} makinesi için zaten aktif bir ring oturumu var.");
            var pending = FindPending(machineId, pendingSessionId);
            if (!pending.SawConfirmedExcavation || !pending.SawConfirmedRingBuildingAfterExcavation)
                throw new InvalidOperationException("Sonraki ring için doğrulanmış Excavation -> RingBuilding dizisi yok.");
            if (!long.TryParse(pending.RingNo, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                throw new InvalidOperationException("Önceki ring numarası geçersiz.");
            var timestamp = now ?? DateTime.Now;
            created = new RingSession
            {
                MachineId = machineId, RingNo = checked(number + 1).ToString(CultureInfo.InvariantCulture),
                MonitoringSessionId = monitoringSessionId, CreatedAt = timestamp, StartedAt = timestamp,
                LastUpdatedAt = timestamp, CurrentState = RingSessionState.ExcavationInProgress,
                SawConfirmedExcavation = true, WasMonitoringStartedMidExcavation = false
            };
            _active[machineId] = created;
            change = Event(created, "NextRingCreated", RingSessionState.None, created.CurrentState,
                $"{pending.RingNo} sonrası otomatik ring oluşturuldu", timestamp);
            PersistSafe(created); AppendEventSafe(change); created = Clone(created);
        }
        RingSessionCreated?.Invoke(created); return created;
    }

    public RingSession UpdatePendingRingEndData(string sessionId, Action<EndOfExcavationData> update,
        string reason, DateTime? now = null)
    {
        ArgumentNullException.ThrowIfNull(update); RingSession snapshot; RingSessionEvent change;
        lock (_sync)
        {
            var session = _pending.Values.SelectMany(value => value).FirstOrDefault(item => item.SessionId == sessionId)
                ?? throw new InvalidOperationException("FinalDataPending ring bulunamadı.");
            var candidate = Clone(session); candidate.EndData ??= new EndOfExcavationData(); update(candidate.EndData);
            candidate.LastUpdatedAt = now ?? DateTime.Now;
            var list = _pending[candidate.MachineId]; list[list.FindIndex(item => item.SessionId == sessionId)] = candidate;
            change = Event(candidate, "PendingRingEndDataUpdated", candidate.CurrentState, candidate.CurrentState, reason, candidate.LastUpdatedAt);
            PersistSafe(candidate); AppendEventSafe(change); snapshot = Clone(candidate);
        }
        RingSessionUpdated?.Invoke(snapshot); return snapshot;
    }

    public RingSession ResumeIncomplete(string sessionId, DateTime? now = null)
    {
        RingSession snapshot; RingSessionEvent change;
        lock (_sync)
        {
            var session = FindUnresolved(sessionId);
            if (_active.ContainsKey(session.MachineId)) throw new InvalidOperationException($"{session.MachineId} için zaten aktif ring oturumu var.");
            RemoveUnresolved(session); session.LastUpdatedAt = now ?? DateTime.Now; _active[session.MachineId] = session;
            change = Event(session, "Resumed", session.CurrentState, session.CurrentState, "Tamamlanmamış oturum kullanıcı tarafından devam ettirildi", session.LastUpdatedAt);
            PersistSafe(session); AppendEventSafe(change); snapshot = Clone(session);
        }
        RingSessionUpdated?.Invoke(snapshot); return snapshot;
    }

    public RingSession CancelIncomplete(string sessionId, string reason, DateTime? now = null)
    {
        RingSession snapshot; RingSessionEvent change;
        lock (_sync)
        {
            var session = FindUnresolved(sessionId); RemoveUnresolved(session); var previous = session.CurrentState;
            session.CurrentState = RingSessionState.Cancelled; session.IsCompleted = false; session.EndedAt = now ?? DateTime.Now;
            session.LastUpdatedAt = session.EndedAt.Value; session.CompletionReason = reason;
            change = Event(session, "Cancelled", previous, session.CurrentState, reason, session.LastUpdatedAt);
            PersistSafe(session); AppendEventSafe(change); snapshot = Clone(session);
        }
        RingSessionCancelled?.Invoke(snapshot); return snapshot;
    }

    public RingSession Update(string machineId, Action<RingSession> update, string reason, DateTime? now = null)
    {
        ArgumentNullException.ThrowIfNull(update); RingSession snapshot; RingSessionEvent change;
        lock (_sync)
        {
            if (!_active.TryGetValue(machineId, out var session)) throw new InvalidOperationException("Aktif ring oturumu bulunamadı.");
            var previous = session.CurrentState; var originalId = session.SessionId; var originalMachine = session.MachineId; var originalRing = session.RingNo;
            var candidate = Clone(session); update(candidate);
            if (candidate.SessionId != originalId || !string.Equals(candidate.MachineId, originalMachine, StringComparison.OrdinalIgnoreCase) || candidate.RingNo != originalRing)
                throw new InvalidOperationException("Ring oturumu kimlik alanları güncellenemez.");
            candidate.LastUpdatedAt = now ?? DateTime.Now; _active[machineId] = candidate;
            change = Event(candidate, "Updated", previous, candidate.CurrentState, reason, candidate.LastUpdatedAt);
            PersistSafe(candidate); AppendEventSafe(change); snapshot = Clone(candidate);
        }
        RingSessionUpdated?.Invoke(snapshot); return snapshot;
    }

    public RingSession Complete(string machineId, string reason, DateTime? now = null) => Finish(machineId, reason, true, now);
    public RingSession Cancel(string machineId, string reason, DateTime? now = null) => Finish(machineId, reason, false, now);

    public IReadOnlyList<RingSession> DetectIncompleteSessions(string? machineId = null)
    {
        var found = new List<RingSession>();
        try
        {
            if (!Directory.Exists(_sessionsRoot)) return found;
            var files = Directory.EnumerateFiles(_sessionsRoot, "*.json", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    var session = JsonSerializer.Deserialize<RingSession>(File.ReadAllText(file), JsonOptions);
                    if (session is not null && !session.IsCompleted && session.CurrentState != RingSessionState.Cancelled
                        && (machineId is null || string.Equals(session.MachineId, machineId, StringComparison.OrdinalIgnoreCase))) found.Add(session);
                }
                catch (Exception exception) { PersistenceFailed?.Invoke($"Ring oturumu okunamadı: {file}", exception); }
            }
        }
        catch (Exception exception) { PersistenceFailed?.Invoke("Ring oturumları taranamadı.", exception); }
        return found.OrderBy(item => item.CreatedAt).Select(Clone).ToArray();
    }

    public string GetSessionFilePath(RingSession session) => Path.Combine(_sessionsRoot, Safe(session.MachineId),
        $"{Safe(session.RingNo)}_{Safe(session.SessionId)}.json");

    private RingSession Finish(string machineId, string reason, bool completed, DateTime? now)
    {
        RingSession snapshot; RingSessionEvent change;
        lock (_sync)
        {
            if (!_active.Remove(machineId, out var session)) throw new InvalidOperationException("Aktif ring oturumu bulunamadı.");
            var previous = session.CurrentState; var timestamp = now ?? DateTime.Now;
            session.CurrentState = completed ? RingSessionState.Completed : RingSessionState.Cancelled;
            session.IsCompleted = completed; session.EndedAt = timestamp; session.CompletionReason = reason; session.LastUpdatedAt = timestamp;
            change = Event(session, completed ? "Completed" : "Cancelled", previous, session.CurrentState, reason, timestamp);
            PersistSafe(session); AppendEventSafe(change); snapshot = Clone(session);
        }
        if (completed) RingSessionCompleted?.Invoke(snapshot); else RingSessionCancelled?.Invoke(snapshot);
        return snapshot;
    }

    private RingSession FindUnresolved(string sessionId)
    {
        foreach (var sessions in _unresolved.Values)
        {
            var found = sessions.FirstOrDefault(item => item.SessionId == sessionId);
            if (found is not null) return Clone(found);
        }
        throw new InvalidOperationException("Tamamlanmamış ring oturumu bulunamadı.");
    }

    private RingSession FindPending(string machineId, string sessionId)
    {
        if (_pending.TryGetValue(machineId, out var values))
        {
            var found = values.FirstOrDefault(item => item.SessionId == sessionId);
            if (found is not null) return found;
        }
        throw new InvalidOperationException("FinalDataPending ring bulunamadı.");
    }

    private void RemoveUnresolved(RingSession session)
    {
        if (!_unresolved.TryGetValue(session.MachineId, out var sessions)) return;
        sessions.RemoveAll(item => item.SessionId == session.SessionId);
        if (sessions.Count == 0) _unresolved.Remove(session.MachineId);
    }

    private void PersistSafe(RingSession session)
    {
        var path = GetSessionFilePath(session); var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(temporary, JsonSerializer.Serialize(session, JsonOptions), new UTF8Encoding(false));
            File.Move(temporary, path, true);
        }
        catch (Exception exception) { PersistenceFailed?.Invoke($"Ring oturumu kaydedilemedi: {path}", exception); }
        finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch { } }
    }

    private void AppendEventSafe(RingSessionEvent value)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_eventsPath)!);
            if (!File.Exists(_eventsPath)) File.WriteAllText(_eventsPath,
                "Timestamp,MachineId,RingNo,RingSessionId,EventType,PreviousState,NewState,Reason,WasMonitoringStartedMidExcavation,StableDataCaptured,EndDataCaptured,PpsDataReceived,ExcelWritten\r\n", new UTF8Encoding(true));
            File.AppendAllText(_eventsPath, string.Join(',', value.Timestamp.ToString("O", CultureInfo.InvariantCulture), Q(value.MachineId),
                Q(value.RingNo), Q(value.RingSessionId), value.EventType, value.PreviousState, value.NewState, Q(value.Reason),
                value.WasMonitoringStartedMidExcavation, value.StableDataCaptured, value.EndDataCaptured, value.PpsDataReceived, value.ExcelWritten) + "\r\n", new UTF8Encoding(false));
        }
        catch (Exception exception) { PersistenceFailed?.Invoke($"Ring olay CSV'si yazılamadı: {_eventsPath}", exception); }
    }

    private static RingSessionEvent Event(RingSession value, string type, RingSessionState previous, RingSessionState next, string reason, DateTime at) =>
        new(at, value.MachineId, value.RingNo, value.SessionId, type, previous, next, reason,
            value.WasMonitoringStartedMidExcavation, value.StableDataCaptured, value.EndDataCaptured, value.PpsDataReceived, value.ExcelWritten);
    private static RingSession Clone(RingSession value) => JsonSerializer.Deserialize<RingSession>(JsonSerializer.Serialize(value, JsonOptions), JsonOptions)!;
    private static string ValidateMachine(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("MachineId boş olamaz.") : value.Trim();
    private static string ValidateRingNo(string value) => !long.TryParse(value?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number <= 0
        ? throw new ArgumentException("RingNo pozitif bir tam sayı olmalıdır.") : number.ToString(CultureInfo.InvariantCulture);
    private static string Safe(string value) => string.Concat(value.Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
    private static string Q(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
