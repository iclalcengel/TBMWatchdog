namespace TBMWatchdog.Models;

public sealed class AppSettings
{
    public int ScreenNumber { get; set; }
    public int ReadingIntervalSeconds { get; set; } = 2;
    public int InactivityWarningSeconds { get; set; } = 30;
    public int OcrPaddingPixels { get; set; } = 4;
    public int OcrMaxParallelWorkers { get; set; } = 6;
    public int OcrFieldTimeoutMs { get; set; } = 20000;
    public OcrProfile OcrProfile { get; set; } = new();
    public ScreenRegion NominalRegion { get; set; } = new();
    public ScreenRegion Scale1Region { get; set; } = new();
    public ScreenRegion Scale2Region { get; set; } = new();
    public ScreenRegion GateRegion { get; set; } = new();
    public ScreenRegion OperationModeRegion { get; set; } = new();
    public ScreenRegion AdvanceMmRegion { get; set; } = new();
    public ScreenRegion AdvanceGroupARegion { get; set; } = new();
    public ScreenRegion AdvanceGroupBRegion { get; set; } = new();
    public ScreenRegion AdvanceGroupCRegion { get; set; } = new();
    public ScreenRegion AdvanceGroupDRegion { get; set; } = new();
    public ScreenRegion StableRpmRegion { get; set; } = new(); public ScreenRegion StableTorqueRegion { get; set; } = new();
    public ScreenRegion StableThrustForceRegion { get; set; } = new(); public ScreenRegion StableAdvanceSpeedRegion { get; set; } = new();
    public ScreenRegion StablePenetrationRegion { get; set; } = new();
    public ScreenRegion NominalInjectionRegion { get; set; } = new();
    public ScreenRegion RealInjectionRegion { get; set; } = new();
    public ScreenRegion Epb1Region { get; set; } = new(); public ScreenRegion Epb2Region { get; set; } = new();
    public ScreenRegion Epb3Region { get; set; } = new(); public ScreenRegion Epb4Region { get; set; } = new();
    public ScreenRegion Epb5Region { get; set; } = new();
    public bool EnableStableParameterDiagnostics { get; set; }
    public string CsvOutputFolder { get; set; } = string.Empty;
    public bool SaveScreenshots { get; set; }
    public bool ShowOcrPreview { get; set; }
    public bool EnableMismatchSampleCapture { get; set; }
    public int MismatchSampleCooldownSeconds { get; set; } = 30;
    // Eski JSON adı geriye dönük uyumluluk için korunur; değer sample klasörü sayısıdır.
    public int MismatchSampleMaximumFilesPerField { get; set; } = 50;
    public int MismatchSampleMaximumSamplesPerField { get => MismatchSampleMaximumFilesPerField; set => MismatchSampleMaximumFilesPerField = value; }
    public int MismatchSampleMaximumTotalSizeMb { get; set; } = 100;
    public int CsvLogRotationMaximumSizeMb { get; set; } = 20;
    public int MaxVerificationArtifactRuns { get; set; } = 3;
    public bool EnableOcrImageCache { get; set; } = true;
    public double OcrImageSimilarityThresholdPercent { get; set; } = 98.5;
    public int GateOcrCacheMaximumAgeSeconds { get; set; } = 2;
    public int NumericFieldOcrCacheMaximumAgeSeconds { get; set; } = 5;
    public bool ShowPerformancePanel { get; set; }
    public int PerformanceMonitorIntervalSeconds { get; set; } = 2;
    public bool EnablePerformanceCsv { get; set; } = true;
    public int PerformanceCsvIntervalSeconds { get; set; } = 10;
    public double WarningProcessCpuPercent { get; set; } = 70;
    public double CriticalProcessCpuPercent { get; set; } = 90;
    public double WarningMemoryMb { get; set; } = 1500;
    public double CriticalMemoryMb { get; set; } = 2500;
    public double WarningAverageLatencyMs { get; set; } = 2000;
    public double CriticalAverageLatencyMs { get; set; } = 5000;
    public int WarningMismatchQueueLength { get; set; } = 50;
    public int PerformanceUiRefreshIntervalMs { get; set; } = 1000;
    public int OcrFeedbackSnapshotMaxMeasurements { get; set; } = 100;
    public int OcrFeedbackSnapshotMaxAgeMinutes { get; set; } = 60;
    public bool DemoMode { get; set; } = true;
}
