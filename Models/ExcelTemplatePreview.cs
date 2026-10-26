namespace TBMWatchdog.Models;

public sealed record ExcelTemplatePreview
{
    public string MachineId { get; init; } = "";
    public string SheetName { get; init; } = "";
    public string RingNo { get; init; } = "";
    public bool RingAlreadyExists { get; init; }
    public int? TargetRow { get; init; }
    public bool TemplateIsValid { get; init; }
    public string ValidationMessage { get; init; } = "";
    public string ExcelFilePath { get; init; } = "";
}
