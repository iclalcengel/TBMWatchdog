namespace TBMWatchdog.Models;

public sealed class MonitoringSessionState
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    public bool IsActive { get; set; }
    public string Machine { get; set; } = "Bilinmiyor";
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public long ProcessedReadings { get; set; }
    public DateTime? LastSuccessfulDataAt { get; set; }
    public string LastOcrStatus { get; set; } = "Henüz okuma yok";
    public DateTime? PausedAt { get; set; }
    public TimeSpan AccumulatedPausedDuration { get; set; }
}
