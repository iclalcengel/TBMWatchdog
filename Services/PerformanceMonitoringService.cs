using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public sealed record OcrPerformanceFrame(DateTime Timestamp, IReadOnlyList<CharacterOcrComparison> Comparisons,
    IReadOnlyList<OcrCacheStatistics> CacheStatistics, int OcrErrors, int SuspiciousValues,
    int FilteredSpikes, long MismatchSaved, int MismatchQueueLength)
{
    public IReadOnlyList<NumericFieldDefinition> FieldDefinitions { get; init; } = Array.Empty<NumericFieldDefinition>();
}

public sealed record FieldPerformanceSnapshot(string Field, double LastRealOcrMs, double LastCharacterOcrMs,
    double QueueWaitMs, double ProcessingMs, double TotalLatencyMs, double AverageRealOcrMs,
    double Last20AverageMs, long RealOcrCount, long CacheHitCount, long CacheMissCount,
    double CacheHitRate, double SimilarityPercent, string DecisionSource, string FailureReason,
    DateTime? LastSuccessfulOcrAt)
{
    public string TimingBreakdown { get; init; } = "";
}

public sealed record PerformanceSnapshot(DateTime Timestamp, string Machine, string SessionId,
    TimeSpan SessionDuration, long CapturedFrames, long TotalRealOcr, long TotalCacheHits, long TotalCacheMisses,
    long TotalOcrErrors, long SuspiciousValues, long FilteredSpikes, long MismatchSaved, int MismatchQueueLength,
    double AverageLatencyMs, double MaximumLatencyMs, int ReadingsLastMinute, DateTime? LastSuccessfulFullRead,
    double? ProcessCpuPercent, double AverageProcessCpuPercent, double MaximumProcessCpuPercent,
    double? SystemCpuPercent, double ProcessMemoryMb, double? AvailableMemoryMb,
    int ThreadCount, int? GdiHandleCount, int ProcessHandleCount, string HealthStatus, string SlowestField,
    string DiagnosticMessage, IReadOnlyList<FieldPerformanceSnapshot> Fields)
{
    public MonitoringCycleProfile? LastCycle { get; init; }
    public double AverageCycleMs { get; init; }
    public double P50CycleMs { get; init; }
    public double P95CycleMs { get; init; }
    public double MaximumCycleMs { get; init; }
    public int LogicalProcessorCount { get; init; } = Environment.ProcessorCount;
    public double? ProcessCpuSingleCoreEquivalent { get; init; }
}

public sealed class PerformanceMonitoringService : IDisposable
{
    private sealed class FieldState { public readonly Queue<double> Last20 = new(); public CharacterOcrComparison? Last; public OcrCacheStatistics? Cache; public DateTime? Success; }
    private readonly object _sync = new(); private readonly Dictionary<string, FieldState> _fields = new(StringComparer.Ordinal);
    private readonly Queue<DateTime> _readings = new(); private readonly Queue<double> _latencies = new();
    private readonly Queue<double> _cpuSamples = new();
    private readonly Queue<(double Memory, int Threads, int Gdi)> _trends = new(); private readonly Process _process = Process.GetCurrentProcess();
    private readonly AppSettings _settings; private readonly System.Threading.Timer _timer; private DateTime _sessionStarted;
    private readonly string _csvPath;
    private readonly Queue<MonitoringCycleProfile> _cycles = new();
    private DateTime _lastCsv; private TimeSpan _previousCpu; private DateTime _previousCpuAt;
    private long _frames, _errors, _suspicious, _filtered, _mismatch; private int _queue; private string _machine = "—";
    public PerformanceMonitoringService(AppSettings settings, string? csvPath = null) { _settings = settings; _csvPath = csvPath ?? Path.Combine(AppContext.BaseDirectory, "Diagnostics", "PerformanceMetrics.csv"); _previousCpu = _process.TotalProcessorTime; _previousCpuAt = DateTime.UtcNow;
        _timer = new System.Threading.Timer(_ => MeasureSafely(), null, Timeout.Infinite, Timeout.Infinite);
        PerformanceMonitorControl.ResetRequested += Reset; }
    public event Action<PerformanceSnapshot>? Updated; public event Action<string>? Warning;
    public PerformanceSnapshot? Latest { get; private set; } public string SessionId { get; private set; } = "";

    public void StartSession(string machine)
    {
        lock (_sync) { _fields.Clear(); _readings.Clear(); _latencies.Clear(); _cpuSamples.Clear(); _trends.Clear(); _frames = _errors = _suspicious = _filtered = _mismatch = 0; _queue = 0;
            _machine = machine; SessionId = Guid.NewGuid().ToString("N"); _sessionStarted = DateTime.Now; _lastCsv = DateTime.MinValue; Latest = null; }
        _timer.Change(0, Math.Clamp(_settings.PerformanceMonitorIntervalSeconds, 1, 10) * 1000);
    }
    public PerformanceSnapshot? StopSession() { _timer.Change(Timeout.Infinite, Timeout.Infinite); MeasureSafely(); return Latest; }
    public void Reset() => StartSession(_machine);
    public void Record(OcrPerformanceFrame frame)
    {
        lock (_sync)
        {
            _frames++; _errors += frame.OcrErrors; _suspicious += frame.SuspiciousValues; _filtered += frame.FilteredSpikes;
            _mismatch = frame.MismatchSaved; _queue = frame.MismatchQueueLength; _readings.Enqueue(frame.Timestamp);
            foreach (var comparison in frame.Comparisons)
            {
                if (!_fields.TryGetValue(comparison.Field, out var state)) _fields[comparison.Field] = state = new();
                state.Last = comparison;
                state.Cache = frame.CacheStatistics.FirstOrDefault(item => string.Equals(item.Field, comparison.Field, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(frame.FieldDefinitions.FirstOrDefault(definition => string.Equals(definition.FieldId, item.Field, StringComparison.OrdinalIgnoreCase))?.DisplayName,
                        comparison.Field, StringComparison.OrdinalIgnoreCase));
                if (comparison.RealOcrExecuted) { state.Last20.Enqueue(comparison.TotalOcrDurationMilliseconds); while (state.Last20.Count > 20) state.Last20.Dequeue(); }
                if (comparison.FailureReason == "Success") state.Success = comparison.DisplayedAt;
                _latencies.Enqueue(comparison.TotalLatencyMilliseconds); while (_latencies.Count > 1200) _latencies.Dequeue();
            }
        }
    }
    public void RecordCycle(MonitoringCycleProfile profile){lock(_sync){_cycles.Enqueue(profile);while(_cycles.Count>300)_cycles.Dequeue();}}
    private void MeasureSafely()
    {
        try { var snapshot = BuildSnapshot(); Latest = snapshot; if (_settings.EnablePerformanceCsv && (snapshot.Timestamp - _lastCsv).TotalSeconds >= Math.Max(1, _settings.PerformanceCsvIntervalSeconds)) { AppendCsv(snapshot); _lastCsv = snapshot.Timestamp; } Updated?.Invoke(snapshot); }
        catch (Exception ex) { Warning?.Invoke($"Performans metrikleri ölçülemedi: {ex.Message}"); }
    }
    private PerformanceSnapshot BuildSnapshot()
    {
        lock (_sync)
        {
            _process.Refresh(); var nowUtc = DateTime.UtcNow; var cpuTime = _process.TotalProcessorTime; var elapsed = (nowUtc - _previousCpuAt).TotalMilliseconds;
            double? cpu = elapsed <= 0 ? null : Math.Clamp((cpuTime - _previousCpu).TotalMilliseconds / elapsed / Environment.ProcessorCount * 100, 0, 100);
            if (cpu.HasValue) { _cpuSamples.Enqueue(cpu.Value); while (_cpuSamples.Count > 300) _cpuSamples.Dequeue(); }
            _previousCpu = cpuTime; _previousCpuAt = nowUtc; var now = DateTime.Now;
            while (_readings.Count > 0 && now - _readings.Peek() > TimeSpan.FromMinutes(1)) _readings.Dequeue();
            var fields = _fields.Select(pair => FieldSnapshot(pair.Key, pair.Value)).ToArray();
            var real = fields.Sum(x => x.RealOcrCount); var hits = fields.Sum(x => x.CacheHitCount); var misses = fields.Sum(x => x.CacheMissCount);
            var average = _latencies.Count == 0 ? 0 : _latencies.Average(); var maximum = _latencies.Count == 0 ? 0 : _latencies.Max();
            var memory = _process.WorkingSet64 / 1048576d; var gdi = GetGdiCount(); _trends.Enqueue((memory, _process.Threads.Count, gdi ?? -1)); while (_trends.Count > 10) _trends.Dequeue();
            var leak = _trends.Count == 10 && (IsContinuousIncrease(_trends.Select(x => x.Memory)) || IsContinuousIncrease(_trends.Select(x => (double)x.Threads)) || (gdi.HasValue && IsContinuousIncrease(_trends.Select(x => (double)x.Gdi))));
            var health = cpu >= _settings.CriticalProcessCpuPercent || memory >= _settings.CriticalMemoryMb || average >= _settings.CriticalAverageLatencyMs ? "Kritik"
                : average >= _settings.WarningAverageLatencyMs ? "OCR gecikiyor"
                : cpu >= _settings.WarningProcessCpuPercent || memory >= _settings.WarningMemoryMb || _queue >= _settings.WarningMismatchQueueLength ? "Yüksek yük" : "Sağlıklı";
            var slowest = fields.OrderByDescending(x => x.AverageRealOcrMs).FirstOrDefault(); var sum = fields.Sum(x => x.AverageRealOcrMs);
            var slowText = slowest is null ? "—" : $"{slowest.Field} — ortalama {slowest.AverageRealOcrMs:0} ms";
            var diagnostic = slowest is null || sum <= 0 ? "" : $"{slowest.Field}, toplam OCR süresinin %{slowest.AverageRealOcrMs * 100 / sum:0} oranını kullanıyor.";
            if (leak) diagnostic += " Kaynak kullanımı sürekli artıyor; olası sızıntı incelenmeli.";
            var cycleArray=_cycles.Select(x=>x.CycleTotalMs).OrderBy(x=>x).ToArray();
            double Percentile(double p)=>cycleArray.Length==0?0:cycleArray[Math.Clamp((int)Math.Ceiling(p*cycleArray.Length)-1,0,cycleArray.Length-1)];
            return new(now, _machine, SessionId, now - _sessionStarted, _frames, real, hits, misses, _errors, _suspicious, _filtered, _mismatch, _queue,
                average, maximum, _readings.Count, fields.All(x => x.LastSuccessfulOcrAt.HasValue) ? fields.Max(x => x.LastSuccessfulOcrAt) : null,
                cpu, _cpuSamples.Count == 0 ? 0 : _cpuSamples.Average(), _cpuSamples.Count == 0 ? 0 : _cpuSamples.Max(),
                null, memory, AvailableMemory(), _process.Threads.Count, gdi, _process.HandleCount, health, slowText, diagnostic, fields)
            {LastCycle=_cycles.LastOrDefault(),AverageCycleMs=cycleArray.Length==0?0:cycleArray.Average(),P50CycleMs=Percentile(.5),P95CycleMs=Percentile(.95),MaximumCycleMs=cycleArray.LastOrDefault(),
                LogicalProcessorCount=Environment.ProcessorCount,ProcessCpuSingleCoreEquivalent=cpu*Environment.ProcessorCount};
        }
    }
    private static FieldPerformanceSnapshot FieldSnapshot(string field, FieldState state) { var c = state.Last; var cache = state.Cache; var processing = c?.RealOcrExecuted == true ? c.TotalOcrDurationMilliseconds : 0;
        var queue = Math.Max(0, (c?.TotalLatencyMilliseconds ?? 0) - processing); return new(field, processing, c?.CharacterDurationMilliseconds ?? 0, queue, processing,
            c?.TotalLatencyMilliseconds ?? 0, cache?.AverageRealOcrMilliseconds ?? 0, state.Last20.Count == 0 ? 0 : state.Last20.Average(), cache?.RealOcrCount ?? 0,
            cache?.CacheHitCount ?? 0, cache?.CacheMissCount ?? 0, cache?.HitRatePercent ?? 0, c?.ImageSimilarityPercent ?? 0,
            c?.ExecutionPath ?? (c?.CacheUsed == true ? "Cached" : "Fast"), c?.FailureReason ?? "—", state.Success)
        { TimingBreakdown = c is null ? "" : $"Capture {c.CaptureMs:0} ms | Fast OCR {c.FastFullLineOcrMs:0} ms | Segment {c.CharacterSegmentationMs:0} ms | Template {c.TemplateMatchMs:0} ms | Targeted {c.TargetedFallbackMs:0} ms | Cache {c.CacheMs:0} ms | Decision {c.DecisionMs:0} ms" }; }
    private void AppendCsv(PerformanceSnapshot s) { var path = _csvPath; Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (!File.Exists(path)) File.WriteAllText(path, "Timestamp,Machine,SessionId,ProcessCpuPercent,ProcessCpuSingleCoreEquivalent,LogicalProcessorCount,SystemCpuPercent,ProcessMemoryMb,AvailableMemoryMb,ThreadCount,GdiHandleCount,ProcessHandleCount,TotalRealOcr,TotalCacheHits,TotalCacheMisses,CacheHitRate,AverageLatencyMs,MaximumLatencyMs,MismatchQueueLength,HealthStatus,SlowestField\r\n", new UTF8Encoding(true));
        var rate = s.TotalCacheHits + s.TotalCacheMisses == 0 ? 0 : s.TotalCacheHits * 100d / (s.TotalCacheHits + s.TotalCacheMisses); File.AppendAllText(path, string.Join(',', s.Timestamp.ToString("O"), Q(s.Machine), s.SessionId, N(s.ProcessCpuPercent),N(s.ProcessCpuSingleCoreEquivalent),s.LogicalProcessorCount, N(s.SystemCpuPercent), N(s.ProcessMemoryMb), N(s.AvailableMemoryMb), s.ThreadCount, s.GdiHandleCount?.ToString() ?? "", s.ProcessHandleCount, s.TotalRealOcr, s.TotalCacheHits, s.TotalCacheMisses, N(rate), N(s.AverageLatencyMs), N(s.MaximumLatencyMs), s.MismatchQueueLength, Q(s.HealthStatus), Q(s.SlowestField)) + "\r\n", new UTF8Encoding(false)); }
    internal static bool IsContinuousIncrease(IEnumerable<double> values) { var a = values.ToArray(); return a.Length > 1 && a.Zip(a.Skip(1), (x, y) => y > x).All(x => x); }
    internal PerformanceSnapshot CaptureForTest() => BuildSnapshot();
    private static double? AvailableMemory() { try { var info = new Microsoft.VisualBasic.Devices.ComputerInfo(); return info.AvailablePhysicalMemory / 1048576d; } catch { return null; } }
    private static int? GetGdiCount() { try { return OperatingSystem.IsWindows() ? GetGuiResources(Process.GetCurrentProcess().Handle, 0) : null; } catch { return null; } }
    [DllImport("user32.dll")] private static extern int GetGuiResources(IntPtr process, int flags);
    private static string N(double? value) => value?.ToString("0.0", CultureInfo.InvariantCulture) ?? ""; private static string Q(string v) => $"\"{v.Replace("\"", "\"\"")}\"";
    public void Dispose() { PerformanceMonitorControl.ResetRequested -= Reset; _timer.Dispose(); _process.Dispose(); }
}

public static class PerformanceMonitorControl
{
    public static event Action? ResetRequested;
    public static void RequestReset() => ResetRequested?.Invoke();
}
