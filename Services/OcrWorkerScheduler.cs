using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public enum OcrJobPriority { High = 0, Medium = 1, Low = 2 }

public sealed record OcrFieldJob(
    string CycleId,
    string FrameId,
    DateTime CapturedAt,
    string FieldId,
    NumericFieldDefinition Definition,
    ScreenRegion Region,
    OcrJobPriority Priority,
    OcrExecutionMode ExecutionMode,
    Bitmap Crop) : IDisposable
{
    public void Dispose() => Crop.Dispose();
}

public sealed record OcrScheduledResult<T>(
    string CycleId, string FrameId, string FieldId, T? Value, string? FailureReason,
    bool TimedOut, bool Cancelled, double QueueWaitMs, double ExecutionMs, int WorkerIndex);

public sealed record OcrSchedulerCycleMetrics(int WorkerCount, int JobCount, double ParallelOcrWallClockMs,
    double SumOfFieldOcrMs, double AverageQueueWaitMs, int TimeoutCount, int CancelledCount)
{
    public double ParallelEfficiency => ParallelOcrWallClockMs <= 0 || WorkerCount <= 0 ? 0
        : SumOfFieldOcrMs / (ParallelOcrWallClockMs * WorkerCount);
    public double QueueWaitTotalMs { get; init; }
    public double EngineAcquireWaitMs { get; init; }
    public double CoreDashboardReadyMs { get; init; }
    public double AdvanceDecisionReadyMs { get; init; }
    public double FullMeasurementReadyMs => ParallelOcrWallClockMs;
}

/// <summary>Bounded, priority ordered OCR executor. Every worker owns one OcrService/TesseractEngine.</summary>
public sealed class OcrWorkerScheduler : IDisposable
{
    private sealed record QueuedJob<T>(OcrFieldJob Job, DateTime QueuedAt,
        Func<OcrService, OcrFieldJob, CancellationToken, Task<T>> Execute,
        TaskCompletionSource<OcrScheduledResult<T>> Completion);

    private readonly OcrService[] _workers;
    private readonly int _timeoutMs;
    private readonly CancellationTokenSource _shutdown = new();
    private int _disposed;

    public OcrWorkerScheduler(int workerCount, int timeoutMs)
    {
        WorkerCount = Math.Clamp(workerCount, 1, 16);
        _timeoutMs = Math.Clamp(timeoutMs, 1000, 60000);
        _workers = Enumerable.Range(0, WorkerCount).Select(_ => new OcrService()).ToArray();
    }

    public int WorkerCount { get; }
    public int EngineCreationCount => _workers.Sum(worker => worker.EngineCreationCount);

    public Task WarmUpAsync(CancellationToken token = default) =>
        Task.WhenAll(_workers.Select(worker => worker.WarmUpAsync(token)));

    public async Task<(IReadOnlyList<OcrScheduledResult<T>> Results, OcrSchedulerCycleMetrics Metrics)> ExecuteAsync<T>(
        IEnumerable<OcrFieldJob> source,
        Func<OcrService, OcrFieldJob, CancellationToken, Task<T>> execute,
        CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        var jobs = source.OrderBy(job => job.Priority).ThenBy(CoreRank).ThenBy(job => job.Definition.DisplayOrder)
            .ThenBy(job => job.FieldId, StringComparer.OrdinalIgnoreCase).ToArray();
        if (jobs.Length == 0) return ([], new(WorkerCount, 0, 0, 0, 0, 0, 0));

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, _shutdown.Token);
        var channel = Channel.CreateBounded<QueuedJob<T>>(new BoundedChannelOptions(Math.Max(WorkerCount * 2, jobs.Length))
        { SingleWriter = true, SingleReader = false, FullMode = BoundedChannelFullMode.Wait });
        var queued = jobs.Select(job => new QueuedJob<T>(job, DateTime.UtcNow, execute,
            new(TaskCreationOptions.RunContinuationsAsynchronously))).ToArray();
        var wall = Stopwatch.StartNew();
        var workerTasks = _workers.Select((worker, index) => ConsumeAsync(channel.Reader, worker, index, linked.Token)).ToArray();
        foreach (var item in queued) await channel.Writer.WriteAsync(item, linked.Token).ConfigureAwait(false);
        channel.Writer.TryComplete();
        var results = await Task.WhenAll(queued.Select(item => item.Completion.Task)).ConfigureAwait(false);
        await Task.WhenAll(workerTasks).ConfigureAwait(false);
        wall.Stop();
        static double Ready(IEnumerable<OcrScheduledResult<T>> selected) => selected.Any()
            ? selected.Max(result => result.QueueWaitMs + result.ExecutionMs) : 0;
        var coreIds = new HashSet<string>(["StableRpm", "StableTorque", "StableThrustForce", "StableAdvanceSpeed", "StablePenetration"], StringComparer.OrdinalIgnoreCase);
        var advanceIds = new HashSet<string>(["AdvanceGroupA", "AdvanceGroupB", "AdvanceGroupC", "AdvanceGroupD", "OperationMode"], StringComparer.OrdinalIgnoreCase);
        var metrics = new OcrSchedulerCycleMetrics(WorkerCount, results.Length, wall.Elapsed.TotalMilliseconds,
            results.Sum(result => result.ExecutionMs), results.Average(result => result.QueueWaitMs),
            results.Count(result => result.TimedOut), results.Count(result => result.Cancelled))
        {
            QueueWaitTotalMs = results.Sum(result => result.QueueWaitMs),
            EngineAcquireWaitMs = 0, // each worker owns its engine; there is no separate pool acquisition
            CoreDashboardReadyMs = Ready(results.Where(result => coreIds.Contains(result.FieldId))),
            AdvanceDecisionReadyMs = Ready(results.Where(result => advanceIds.Contains(result.FieldId)))
        };
        return (results, metrics);
    }

    // High priority is split deliberately: the five dashboard values are released first,
    // then Advance evidence. Medium/low work can never jump ahead of either group.
    internal static int CoreRank(OcrFieldJob job) => job.FieldId switch
    {
        "StableRpm" or "StableTorque" or "StableThrustForce" or
        "StableAdvanceSpeed" or "StablePenetration" => 0,
        "AdvanceGroupA" or "AdvanceGroupB" or "AdvanceGroupC" or "AdvanceGroupD" or
        "OperationMode" => 1,
        _ => 2
    };

    private async Task ConsumeAsync<T>(ChannelReader<QueuedJob<T>> reader, OcrService worker, int workerIndex, CancellationToken token)
    {
        try
        {
            await foreach (var queued in reader.ReadAllAsync(token).ConfigureAwait(false))
            {
                var wait = Math.Max(0, (DateTime.UtcNow - queued.QueuedAt).TotalMilliseconds);
                if (token.IsCancellationRequested)
                {
                    queued.Completion.TrySetResult(new(queued.Job.CycleId, queued.Job.FrameId, queued.Job.FieldId,
                        default, "Cancelled", false, true, wait, 0, workerIndex));
                    continue;
                }
                var watch = Stopwatch.StartNew();
                try
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                    timeout.CancelAfter(_timeoutMs);
                    var value = await queued.Execute(worker, queued.Job, timeout.Token).ConfigureAwait(false);
                    watch.Stop();
                    queued.Completion.TrySetResult(new(queued.Job.CycleId, queued.Job.FrameId, queued.Job.FieldId,
                        value, null, false, false, wait, watch.Elapsed.TotalMilliseconds, workerIndex));
                }
                catch (OperationCanceledException)
                {
                    watch.Stop(); var cancelled = token.IsCancellationRequested;
                    queued.Completion.TrySetResult(new(queued.Job.CycleId, queued.Job.FrameId, queued.Job.FieldId,
                        default, cancelled ? "Cancelled" : "Timeout", !cancelled, cancelled, wait, watch.Elapsed.TotalMilliseconds, workerIndex));
                }
                catch (Exception ex)
                {
                    watch.Stop(); queued.Completion.TrySetResult(new(queued.Job.CycleId, queued.Job.FrameId, queued.Job.FieldId,
                        default, ex.Message, false, false, wait, watch.Elapsed.TotalMilliseconds, workerIndex));
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _shutdown.Cancel(); foreach (var worker in _workers) worker.Dispose(); _shutdown.Dispose();
    }
}
