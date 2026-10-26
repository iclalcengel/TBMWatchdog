using TBMWatchdog.Models;
using TBMWatchdog.Utilities;

namespace TBMWatchdog.Services;

public sealed class MonitoringService : IDisposable
{
    private readonly object _sync = new();
    private readonly MonitoringStateTransitionCsvService _transitionCsv;
    private readonly Func<DateTime> _clock;
    private CancellationTokenSource? _cancellation;
    private Task? _monitoringTask;
    private TaskCompletionSource<bool> _resumeGate = CompletedGate();
    private TaskCompletionSource<bool> _readCompleted = CompletedGate();
    private IDataSource? _dataSource;
    private MonitoringRunState _state = MonitoringRunState.Stopped;
    private DateTime _startedAt;
    private DateTime? _pausedAt;
    private TimeSpan _pausedTotal;
    private string _sessionId = "";
    private string _machine = "";
    private int _resumeValidationRemaining;
    private bool _disposed;

    public MonitoringService(MonitoringStateTransitionCsvService? transitionCsv = null, Func<DateTime>? clock = null)
    { _transitionCsv = transitionCsv ?? new(); _clock = clock ?? (() => DateTime.Now); }

    public event Action<MeasurementRecord>? MeasurementReceived;
    public event Action<MeasurementRecord>? ReadingCompleted;
    public event Action? ReadingStarted;
    public event Action<MonitoringStatus, string>? StatusChanged;
    public event Action<MonitoringRunState>? RunStateChanged;
    public event Action<Exception>? ReadingFailed;

    public bool IsRunning { get { lock (_sync) return _monitoringTask is { IsCompleted: false }; } }
    public MonitoringRunState State { get { lock (_sync) return _state; } }
    public string SessionId { get { lock (_sync) return _sessionId; } }

    public MonitoringTimingSnapshot GetTimingSnapshot()
    {
        lock (_sync)
        {
            var now = _clock();
            var session = _startedAt == default ? TimeSpan.Zero : now - _startedAt;
            var paused = _pausedTotal + (_pausedAt.HasValue ? now - _pausedAt.Value : TimeSpan.Zero);
            return new(_machine, _sessionId, _state, _startedAt, _pausedAt, session,
                session > paused ? session - paused : TimeSpan.Zero, paused);
        }
    }

    public void Start(IDataSource dataSource, TimeSpan interval, TimeSpan inactivityThreshold)
    {
        ObjectDisposedException.ThrowIf(_disposed, this); ArgumentNullException.ThrowIfNull(dataSource);
        if (interval < TimeSpan.FromSeconds(1)) throw new ArgumentOutOfRangeException(nameof(interval));
        if (inactivityThreshold < TimeSpan.FromSeconds(1)) throw new ArgumentOutOfRangeException(nameof(inactivityThreshold));
        lock (_sync)
        {
            if (_monitoringTask is { IsCompleted: false }) return;
            _dataSource = dataSource; _machine = dataSource.Name; _sessionId = Guid.NewGuid().ToString("N");
            _startedAt = _clock(); _pausedAt = null; _pausedTotal = TimeSpan.Zero; _resumeValidationRemaining = 0;
            _resumeGate = CompletedGate(); _cancellation = new();
            TransitionLocked(MonitoringRunState.Running, "İzleme kullanıcı tarafından başlatıldı");
            _monitoringTask = RunAsync(dataSource, interval, inactivityThreshold, _cancellation.Token);
        }
    }

    public async Task PauseAsync()
    {
        Task currentRead;
        lock (_sync)
        {
            if (_state != MonitoringRunState.Running) return;
            _resumeGate = NewGate(); _pausedAt = _clock();
            TransitionLocked(MonitoringRunState.Paused, "Kullanıcı tarafından duraklatıldı");
            currentRead = _readCompleted.Task;
        }
        await currentRead.ConfigureAwait(false);
        StatusChanged?.Invoke(MonitoringStatus.Paused, "İzleme kullanıcı tarafından duraklatıldı.");
    }

    public Task ResumeAsync()
    {
        lock (_sync)
        {
            if (_state != MonitoringRunState.Paused) return Task.CompletedTask;
            try
            {
                if (_pausedAt.HasValue) _pausedTotal += _clock() - _pausedAt.Value;
                _pausedAt = null; _resumeValidationRemaining = 2;
                (_dataSource as IMonitoringLifecycleDataSource)?.PrepareForResume(_resumeValidationRemaining);
                TransitionLocked(MonitoringRunState.Resuming, "Kullanıcı devam etmeyi seçti; yeniden doğrulama başladı");
                StatusChanged?.Invoke(MonitoringStatus.Resuming, "Ekran ve OCR alanları yeniden doğrulanıyor...");
                TransitionLocked(MonitoringRunState.Running, "İzleme kullanıcı isteğiyle devam ediyor; doğrulama arka planda sürüyor");
                _resumeGate.TrySetResult(true);
                StatusChanged?.Invoke(MonitoringStatus.Active, "İzleme devam ediyor");
            }
            catch (Exception exception)
            {
                _pausedAt = _clock(); TransitionLocked(MonitoringRunState.Paused, "Devam etme hazırlığı başarısız");
                ReadingFailed?.Invoke(exception); StatusChanged?.Invoke(MonitoringStatus.Paused, "Devam etme başarısız; izleme duraklatıldı.");
            }
        }
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        Task? task;
        lock (_sync) { _cancellation?.Cancel(); _resumeGate.TrySetResult(true); task = _monitoringTask; }
        if (task is not null) try { await task.ConfigureAwait(false); } catch (OperationCanceledException) { }
    }

    public void Dispose()
    {
        if (_disposed) return; _disposed = true;
        lock (_sync) { _cancellation?.Cancel(); _resumeGate.TrySetResult(true); _cancellation?.Dispose(); _cancellation = null; }
    }

    private async Task RunAsync(IDataSource dataSource, TimeSpan interval, TimeSpan inactivityThreshold, CancellationToken token)
    {
        StatusChanged?.Invoke(MonitoringStatus.Active, $"{dataSource.Name} aktif");
        var evaluator = new ExcavationStatusEvaluator(inactivityThreshold); MeasurementRecord? lastPublished = null;
        try
        {
            while (!token.IsCancellationRequested)
            {
                await WaitUntilResumedAsync(token).ConfigureAwait(false);
                bool validationRead; lock (_sync) validationRead = _resumeValidationRemaining > 0;
                var readCompletion = NewGate(); lock (_sync) _readCompleted = readCompletion;
                try
                {
                    if (!validationRead) { StatusChanged?.Invoke(MonitoringStatus.Processing, "Veri işleniyor..."); ReadingStarted?.Invoke(); }
                    MeasurementRecord raw;
                    if (validationRead)
                    {
                        using var validationCancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
                        validationCancellation.CancelAfter(TimeSpan.FromSeconds(30));
                        try { raw = await dataSource.ReadAsync(validationCancellation.Token).ConfigureAwait(false); }
                        catch (OperationCanceledException) when (!token.IsCancellationRequested)
                        {
                            CompleteBackgroundValidationAttempt();
                            ReadingFailed?.Invoke(new TimeoutException("Yeniden doğrulama zaman aşımına uğradı; izleme devam ediyor."));
                            continue;
                        }
                    }
                    else raw = await dataSource.ReadAsync(token).ConfigureAwait(false);
                    var measurement = evaluator.Evaluate(raw);
                    if (validationRead)
                    {
                        CompleteBackgroundValidationAttempt();
                        StatusChanged?.Invoke(MonitoringStatus.Active, measurement.OcrConfidence == OcrConfidenceStatus.Reliable
                            ? $"{dataSource.Name} aktif" : "Bazı alanlar okunamadı; izleme devam ediyor");
                    }
                    else
                    {
                        ReadingCompleted?.Invoke(measurement);
                        if (HasMeaningfulChange(lastPublished, measurement)) { MeasurementReceived?.Invoke(measurement); lastPublished = measurement; }
                        StatusChanged?.Invoke(MonitoringStatus.Active, $"{dataSource.Name} aktif");
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
                catch (Exception exception)
                {
                    ReadingFailed?.Invoke(exception);
                    if (validationRead) CompleteBackgroundValidationAttempt();
                    StatusChanged?.Invoke(MonitoringStatus.OcrError, validationRead
                        ? "Arka plan doğrulama hatası; izleme devam ediyor" : "Okuma hatası");
                }
                finally { readCompletion.TrySetResult(true); }
                if (State != MonitoringRunState.Paused) await Task.Delay(interval, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        finally
        {
            lock (_sync)
            {
                if (_pausedAt.HasValue) { _pausedTotal += _clock() - _pausedAt.Value; _pausedAt = null; }
                (_dataSource as IMonitoringLifecycleDataSource)?.ResetAfterStop();
                TransitionLocked(MonitoringRunState.Stopped, "İzleme oturumu durduruldu");
            }
            StatusChanged?.Invoke(MonitoringStatus.Stopped, "İzleme kapalı");
        }
    }

    private async Task WaitUntilResumedAsync(CancellationToken token)
    { Task gate; lock (_sync) gate = _resumeGate.Task; await gate.WaitAsync(token).ConfigureAwait(false); }

    private void ReturnToPaused(string reason)
    {
        lock (_sync)
        {
            _resumeGate = NewGate(); _pausedAt = _clock();
            TransitionLocked(MonitoringRunState.Paused, reason);
        }
        StatusChanged?.Invoke(MonitoringStatus.Paused, $"{reason}. İzleme güvenli biçimde duraklatıldı.");
    }

    private void CompleteBackgroundValidationAttempt()
    {
        lock (_sync) { if (_resumeValidationRemaining > 0) _resumeValidationRemaining--; }
    }

    private void TransitionLocked(MonitoringRunState next, string reason)
    {
        var previous = _state; if (previous == next) return; _state = next;
        var timing = GetTimingSnapshot();
        _transitionCsv.Append(new(_clock(), _machine, _sessionId, previous, next, reason,
            timing.SessionElapsed.TotalMilliseconds, timing.ActiveMonitoring.TotalMilliseconds, timing.PausedDuration.TotalMilliseconds));
        RunStateChanged?.Invoke(next);
    }

    private static TaskCompletionSource<bool> NewGate() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static TaskCompletionSource<bool> CompletedGate() { var value = NewGate(); value.TrySetResult(true); return value; }
    private static bool HasMeaningfulChange(MeasurementRecord? p, MeasurementRecord c) => p is null || p.Nominal != c.Nominal
        || p.Scale1 != c.Scale1 || p.Scale2 != c.Scale2 || p.GateMillimeters != c.GateMillimeters
        || !string.Equals(p.GateStatus, c.GateStatus, StringComparison.Ordinal)
        || !string.Equals(p.ExcavationStatus, c.ExcavationStatus, StringComparison.Ordinal) || p.OcrConfidence != c.OcrConfidence
        || DiagnosticChanged(p.DiagnosticRpm, c.DiagnosticRpm)
        || DiagnosticChanged(p.DiagnosticTorque, c.DiagnosticTorque)
        || DiagnosticChanged(p.DiagnosticThrustForce, c.DiagnosticThrustForce)
        || DiagnosticChanged(p.DiagnosticAdvanceSpeed, c.DiagnosticAdvanceSpeed)
        || DiagnosticChanged(p.DiagnosticPenetration, c.DiagnosticPenetration)
        || FieldChanged(p, c, BuiltInFieldIds.AdvanceMm);

    private static bool DiagnosticChanged(FieldMeasurementResult? previous, FieldMeasurementResult? current)
        => DisplayValue(previous) != DisplayValue(current)
            || !string.Equals(previous?.FailureReason, current?.FailureReason, StringComparison.Ordinal);

    private static decimal? DisplayValue(FieldMeasurementResult? field)
        => field?.ParsedValue ?? field?.FullLineResult ?? field?.CharacterResult;

    private static bool FieldChanged(MeasurementRecord previous, MeasurementRecord current, string fieldId)
    {
        previous.FieldResults.TryGetValue(fieldId, out var before);
        current.FieldResults.TryGetValue(fieldId, out var after);
        return before?.OfficialValue != after?.OfficialValue || !string.Equals(before?.DecisionSource, after?.DecisionSource, StringComparison.Ordinal);
    }
}
