namespace TBMWatchdog.Models;

public enum MonitoringStatus
{
    Stopped,
    Ready,
    Processing,
    Active,
    Paused,
    Resuming,
    OcrError
}

public enum MonitoringRunState
{
    Stopped,
    Running,
    Paused,
    Resuming
}

public enum OcrConfidenceStatus
{
    Reliable,
    Partial,
    Unreadable
}
