namespace TBMWatchdog.Models;

public enum RingSessionState
{
    None,
    WaitingForExcavation,
    ExcavationInProgress,
    StableDataPending,
    StableDataCaptured,
    ExcavationEndPending,
    ExcavationEnded,
    RingBuilding,
    FinalDataPending,
    AwaitingPps,
    ReadyForExcelPreview,
    Completed,
    Cancelled
}

public sealed class RingSession
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    public string? MonitoringSessionId { get; set; }
    public string MachineId { get; set; } = "";
    public string RingNo { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public RingSessionState CurrentState { get; set; } = RingSessionState.WaitingForExcavation;
    public bool WasMonitoringStartedMidExcavation { get; set; }
    public bool StableDataCaptured { get; set; }
    public bool EndDataCaptured { get; set; }
    public bool PpsDataReceived { get; set; }
    public bool ExcelPreviewCreated { get; set; }
    public bool ExcelWritten { get; set; }
    public bool IsCompleted { get; set; }
    public string? CompletionReason { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public StableExcavationData? StableData { get; set; }
    public EndOfExcavationData? EndData { get; set; }
    public PpsData? Pps { get; set; }
    public bool SawConfirmedExcavation { get; set; }
    public bool SawConfirmedRingBuildingAfterExcavation { get; set; }
    public DateTime? ExcavationEndedAt { get; set; }
    public decimal? FinalOfficialAdvanceMm { get; set; }
}

public sealed class StableExcavationData
{
    public DateTime? CapturedAt { get; set; }
    public decimal? AdvanceMm { get; set; }
    public decimal? Rpm { get; set; }
    public decimal? Torque { get; set; }
    public decimal? ThrustForce { get; set; }
    public decimal? AdvanceSpeed { get; set; }
    public decimal? Penetration { get; set; }
    public DateTime? SampleWindowStart { get; set; }
    public DateTime? SampleWindowEnd { get; set; }
    public int? SampleCount { get; set; }
    public string? QualityStatus { get; set; }
}

public sealed class EndOfExcavationData
{
    public DateTime? CapturedAt { get; set; }
    public TimeSpan? TotalExcavationDuration { get; set; }
    public decimal? NominalMuckAmount { get; set; }
    public decimal? Scale1 { get; set; }
    public decimal? Scale2 { get; set; }
    public decimal? Density { get; set; }
    public decimal? NominalInjection { get; set; }
    public decimal? ActualInjection { get; set; }
    public decimal? Epb1 { get; set; }
    public decimal? Epb2 { get; set; }
    public decimal? Epb3 { get; set; }
    public decimal? Epb4 { get; set; }
    public decimal? Epb5 { get; set; }
    public string? FoamType { get; set; }
    public decimal? FoamConsumption { get; set; }
    public string? TailGreaseType { get; set; }
    public decimal? TailGreaseConsumption { get; set; }
    public string? FoamBrand { get; set; }
    public string? TailGreaseBrand { get; set; }
    public decimal? FoamConsumptionLiters { get; set; }
    public DateTime? FoamConsumptionCapturedAt { get; set; }
    public DateTime? TailGreaseConsumptionCapturedAt { get; set; }
    public string? FoamConsumptionSource { get; set; }
    public string? TailGreaseConsumptionSource { get; set; }
    public string? QualityStatus { get; set; }
}

public sealed class PpsData
{
    public DateTime? ReceivedAt { get; set; }
    public decimal? Kilometer { get; set; }
    public string? Source { get; set; }
    public string? QualityStatus { get; set; }
}

public sealed record RingSessionEvent(DateTime Timestamp, string MachineId, string RingNo,
    string RingSessionId, string EventType, RingSessionState PreviousState,
    RingSessionState NewState, string Reason, bool WasMonitoringStartedMidExcavation,
    bool StableDataCaptured, bool EndDataCaptured, bool PpsDataReceived, bool ExcelWritten);
