using System.Globalization;
using System.Text;
using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public sealed record MonitoringTimingSnapshot(string MachineId, string SessionId, MonitoringRunState State,
    DateTime StartedAt, DateTime? PausedAt, TimeSpan SessionElapsed, TimeSpan ActiveMonitoring,
    TimeSpan PausedDuration);

public sealed record MonitoringStateTransition(DateTime Timestamp, string MachineId, string SessionId,
    MonitoringRunState PreviousState, MonitoringRunState NewState, string Reason,
    double SessionElapsedMs, double ActiveMonitoringMs, double PausedDurationMs);

public sealed class MonitoringStateTransitionCsvService
{
    private readonly object _sync = new();
    public MonitoringStateTransitionCsvService(string? path = null) => FilePath = path ??
        Path.Combine(AppContext.BaseDirectory, "Diagnostics", "MonitoringStateTransitions.csv");
    public string FilePath { get; }
    public void Append(MonitoringStateTransition value)
    {
        lock (_sync)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            if (!File.Exists(FilePath)) File.WriteAllText(FilePath,
                "Timestamp,MachineId,SessionId,PreviousState,NewState,Reason,SessionElapsedMs,ActiveMonitoringMs,PausedDurationMs\r\n",
                new UTF8Encoding(true));
            File.AppendAllText(FilePath, string.Join(',', value.Timestamp.ToString("O"), Q(value.MachineId),
                Q(value.SessionId), value.PreviousState, value.NewState, Q(value.Reason),
                N(value.SessionElapsedMs), N(value.ActiveMonitoringMs), N(value.PausedDurationMs)) + "\r\n",
                new UTF8Encoding(false));
        }
    }
    private static string N(double value) => value.ToString("0", CultureInfo.InvariantCulture);
    private static string Q(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}

public interface IMonitoringLifecycleDataSource
{
    void PrepareForResume(int forcedRealOcrReadings);
    void ResetAfterStop();
}
