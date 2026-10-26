namespace TBMWatchdog.Models;

public sealed record OcrTestProgress(int Completed, int Total, string Status, TimeSpan Elapsed);
