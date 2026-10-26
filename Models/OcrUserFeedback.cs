namespace TBMWatchdog.Models;

public enum OcrFeedbackVerdict { Correct, Incorrect, Corrected, Removed }

public sealed record OcrUserFeedback
{
    public string FeedbackId { get; init; } = Guid.NewGuid().ToString("N");
    public string MeasurementId { get; init; } = "";
    public string MachineId { get; init; } = ""; public string MachineProfile { get; init; } = "";
    public string? RingNo { get; init; } public string? RingSessionId { get; init; }
    public string FieldId { get; init; } = ""; public string FieldDisplayName { get; init; } = "";
    public DateTime CapturedAt { get; init; } public DateTime FeedbackAt { get; init; } = DateTime.Now;
    public string RawOcrText { get; init; } = ""; public decimal? ObservedValue { get; init; }
    public decimal? TrustedValueAtThatTime { get; init; } public OcrFeedbackVerdict Verdict { get; init; }
    public decimal? CorrectedValue { get; init; } public string QualityStatus { get; init; } = "";
    public string FailureReason { get; init; } = ""; public string DecisionSource { get; init; } = "";
    public string Pipeline { get; init; } = "";
    public int RegionX { get; init; } public int RegionY { get; init; } public int RegionWidth { get; init; } public int RegionHeight { get; init; }
    public string RawImagePath { get; init; } = ""; public string ProcessedImagePath { get; init; } = "";
    public string? RevisesFeedbackId { get; init; }
}
