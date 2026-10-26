using System.Globalization;
using System.Text;
using System.Threading.Channels;

namespace TBMWatchdog.Services;

public sealed record OcrFieldProfile(string Field, double CropMs, double PreprocessMs, double LiveFastMs,
    double CharacterSegmentationMs, double TemplateMatchingMs, double TesseractMs, double DiagnosticPipelineMs,
    double FeedbackMatchingMs, double TemporalReliabilityMs, double PhysicalConsistencyMs, double TotalFieldMs,
    bool FastPathSuccess, string FallbackReason, bool HeavyPipelineExecuted, double HeavyPipelineMs);

public sealed record MonitoringCycleProfile(DateTime Timestamp, string MachineId, string CycleId,
    double CycleTotalMs, double ScreenCaptureMs, int ScreenCaptureCount, int ScreenSwitchCount,
    double NumericFieldsMs, double MaterialFieldsMs, double InjectionMs, double EpbMs,
    double OperationModeMs, double RingSessionCoordinationMs, double HistoryUpdateMs, double UiUpdateMs,
    double CsvLoggingMs, double FeedbackSnapshotCacheMs, double FeedbackMs, double OtherMs,
    string SlowestStage, double SlowestStageMs, IReadOnlyList<OcrFieldProfile> Fields)
{
    public int WorkerCount { get; init; } = 1;
    public double AverageQueueWaitMs { get; init; }
    public double ParallelOcrWallClockMs { get; init; }
    public double SumOfFieldOcrMs { get; init; }
    public int OcrTimeoutCount { get; init; }
    public double QueueWaitTotalMs { get; init; }
    public double EngineAcquireWaitMs { get; init; }
    public double CoreDashboardReadyMs { get; init; }
    public double AdvanceDecisionReadyMs { get; init; }
    public double FullMeasurementReadyMs { get; init; }
    public double ParallelEfficiency => ParallelOcrWallClockMs <= 0 || WorkerCount <= 0 ? 0 : SumOfFieldOcrMs / (ParallelOcrWallClockMs * WorkerCount);
    public string Severity => CycleTotalMs > 20_000 ? "CriticalSlowCycle" : CycleTotalMs > 10_000 ? "SlowCycle" : CycleTotalMs > 5_000 ? "Warning" : "Normal";
    public double AccountedMs => ScreenCaptureMs + NumericFieldsMs + OperationModeMs + RingSessionCoordinationMs
        + HistoryUpdateMs + UiUpdateMs + CsvLoggingMs + FeedbackSnapshotCacheMs + OtherMs;
}

public sealed class MonitoringPipelineProfiler : IDisposable
{
    private readonly Channel<MonitoringCycleProfile> _queue=Channel.CreateBounded<MonitoringCycleProfile>(new BoundedChannelOptions(256){SingleReader=true,SingleWriter=false,FullMode=BoundedChannelFullMode.DropOldest});
    private readonly CancellationTokenSource _stop=new();private readonly Task _worker;private readonly string _diagnostics;
    private readonly Queue<double> _cycles=new();private readonly object _sync=new();
    public MonitoringPipelineProfiler(string? diagnostics=null){_diagnostics=diagnostics??Path.Combine(AppContext.BaseDirectory,"Diagnostics");_worker=Task.Run(WriteLoop);}
    public MonitoringCycleProfile? Latest{get;private set;} public int PendingWrites=>_queue.Reader.Count;
    public (double Average,double P50,double P95,double Maximum) Statistics(){lock(_sync){var a=_cycles.OrderBy(x=>x).ToArray();return a.Length==0?(0,0,0,0):(a.Average(),Percentile(a,.5),Percentile(a,.95),a[^1]);}}
    public void Record(MonitoringCycleProfile profile){Latest=profile;lock(_sync){_cycles.Enqueue(profile.CycleTotalMs);while(_cycles.Count>300)_cycles.Dequeue();}_queue.Writer.TryWrite(profile);}
    private async Task WriteLoop(){try{await foreach(var p in _queue.Reader.ReadAllAsync(_stop.Token)){try{Write(p);}catch{/* diagnostics cannot stop monitoring */}}}catch(OperationCanceledException){}}
    private void Write(MonitoringCycleProfile p)
    {
        Directory.CreateDirectory(_diagnostics);var cycle=Path.Combine(_diagnostics,"MonitoringPipelinePerformance.csv");CsvLogRotation.RotateIfNeeded(cycle);
        Append(cycle,"Timestamp,MachineId,CycleId,CycleTotalMs,ScreenCaptureMs,ScreenCaptureCount,ScreenSwitchCount,NumericFieldsMs,MaterialFieldsMs,InjectionMs,EpbMs,OperationModeMs,FeedbackMs,UiMs,LoggingMs,OtherMs,AccountedMs,UnaccountedMs,SlowestStage,SlowestStageMs,WorkerCount,QueueWaitMs,QueueWaitTotalMs,EngineAcquireWaitMs,CoreDashboardReadyMs,AdvanceDecisionReadyMs,FullMeasurementReadyMs,ParallelOcrWallClockMs,SumOfFieldOcrMs,ParallelEfficiency,OcrTimeoutCount",
            string.Join(',',p.Timestamp.ToString("O"),Q(p.MachineId),Q(p.CycleId),N(p.CycleTotalMs),N(p.ScreenCaptureMs),p.ScreenCaptureCount,p.ScreenSwitchCount,N(p.NumericFieldsMs),N(p.MaterialFieldsMs),N(p.InjectionMs),N(p.EpbMs),N(p.OperationModeMs),N(p.FeedbackMs),N(p.UiUpdateMs),N(p.CsvLoggingMs),N(p.OtherMs),N(p.AccountedMs),N(Math.Max(0,p.CycleTotalMs-p.AccountedMs)),Q(p.SlowestStage),N(p.SlowestStageMs),p.WorkerCount,N(p.AverageQueueWaitMs),N(p.QueueWaitTotalMs),N(p.EngineAcquireWaitMs),N(p.CoreDashboardReadyMs),N(p.AdvanceDecisionReadyMs),N(p.FullMeasurementReadyMs),N(p.ParallelOcrWallClockMs),N(p.SumOfFieldOcrMs),N(p.ParallelEfficiency),p.OcrTimeoutCount));
        var fields=Path.Combine(_diagnostics,"OcrFieldPerformance.csv");CsvLogRotation.RotateIfNeeded(fields);
        foreach(var f in p.Fields)Append(fields,"Timestamp,CycleId,Field,CropMs,PreprocessMs,LiveFastMs,CharacterSegmentationMs,TemplateMatchingMs,TesseractMs,DiagnosticPipelineMs,FeedbackMatchingMs,TemporalReliabilityMs,PhysicalConsistencyMs,TotalFieldMs,FastPathSuccess,FallbackReason,HeavyPipelineExecuted,HeavyPipelineMs",
            string.Join(',',p.Timestamp.ToString("O"),Q(p.CycleId),Q(f.Field),N(f.CropMs),N(f.PreprocessMs),N(f.LiveFastMs),N(f.CharacterSegmentationMs),N(f.TemplateMatchingMs),N(f.TesseractMs),N(f.DiagnosticPipelineMs),N(f.FeedbackMatchingMs),N(f.TemporalReliabilityMs),N(f.PhysicalConsistencyMs),N(f.TotalFieldMs),f.FastPathSuccess,Q(f.FallbackReason),f.HeavyPipelineExecuted,N(f.HeavyPipelineMs)));
        if(p.CycleTotalMs>10_000){var slow=Path.Combine(_diagnostics,"SlowMonitoringCycles.csv");CsvLogRotation.RotateIfNeeded(slow);Append(slow,"Timestamp,MachineId,CycleId,CycleTotalMs,Severity,SlowestStage,SlowestStageMs",string.Join(',',p.Timestamp.ToString("O"),Q(p.MachineId),Q(p.CycleId),N(p.CycleTotalMs),p.Severity,Q(p.SlowestStage),N(p.SlowestStageMs)));}
    }
    private static void Append(string path,string header,string row){var exists=File.Exists(path);using var w=new StreamWriter(path,true,new UTF8Encoding(!exists));if(!exists)w.WriteLine(header);w.WriteLine(row);}
    private static double Percentile(double[] a,double p)=>a[Math.Clamp((int)Math.Ceiling(p*a.Length)-1,0,a.Length-1)];
    private static string N(double v)=>v.ToString("0.0",CultureInfo.InvariantCulture);private static string Q(string v)=>$"\"{v.Replace("\"","\"\"")}\"";
    public void Dispose(){_queue.Writer.TryComplete();if(!_worker.Wait(TimeSpan.FromSeconds(3)))_stop.Cancel();_stop.Dispose();}
}
