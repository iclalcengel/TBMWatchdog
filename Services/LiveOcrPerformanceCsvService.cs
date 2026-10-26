using System.Globalization;
using System.Text;
using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public sealed class LiveOcrPerformanceCsvService
{
    private readonly object _sync = new();
    private bool _schemaReady;
    public LiveOcrPerformanceCsvService(string? path = null) =>
        FilePath = path ?? Path.Combine(AppContext.BaseDirectory, "Diagnostics", "LiveOcrPerformance.csv");
    public string FilePath { get; }

    public void AppendField(DateTime timestamp, string field, OcrDiagnosticResult diagnostic, bool cacheUsed,
        double captureMs, double parseMs, string result, double cacheMs = 0)
    {
        Append("Field", timestamp, field, diagnostic.ExecutionMode.ToString(), diagnostic.PipelineCountExecuted,
            diagnostic.PrimaryPipeline, diagnostic.FallbackUsed, diagnostic.HeavyFallbackUsed, cacheUsed,
            captureMs, diagnostic.TotalElapsed.TotalMilliseconds, parseMs, diagnostic.TotalElapsed.TotalMilliseconds + captureMs,
            result, diagnostic.CharacterSegmentationMs, diagnostic.TemplateMatchMs, diagnostic.FullLineOcrMs,
            0, 0, 0, 0, 0, 0, 0,
            diagnostic.Result.EstimatedDigitCount, diagnostic.Result.SelectedCandidateDigitCount,
            diagnostic.Result.DigitPlausibility, diagnostic.TargetedFallbackUsed, diagnostic.TargetedFallbackMs,
            cacheMs, diagnostic.DecisionMs, cacheUsed ? "Cached" : diagnostic.ExecutionPath);
    }

    public void AppendCycle(DateTime timestamp, double captureTotalMs, double numericOcrTotalMs,
        double operationModeOcrMs, int fieldCount, int cacheHits, int fullExecutions, double totalCycleMs) =>
        Append("Cycle", timestamp, "*", OcrExecutionMode.LiveFast.ToString(), 0, "", false, false, false,
            0, 0, 0, 0, "", 0, 0, 0, captureTotalMs, numericOcrTotalMs, operationModeOcrMs, fieldCount, cacheHits, fullExecutions, totalCycleMs,
            0, 0, "", false, 0, 0, 0, "Cycle");

    private void Append(string rowType, DateTime timestamp, string field, string mode, int count, string primary,
        bool fallback, bool heavy, bool cache, double capture, double ocr, double parse, double total, string result,
        double characterSegmentation, double templateMatch, double fullLineOcr,
        double captureTotal, double numericTotal, double operationMode, int fieldCount, int cacheHits, int executions, double cycle,
        int estimatedDigits, int candidateDigits, string plausibility, bool targetedFallback, double targetedFallbackMs,
        double cacheMs, double decisionMs, string executionPath)
    {
        lock (_sync)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            CsvLogRotation.RotateIfNeeded(FilePath);
            if (!_schemaReady)
            {
                if (File.Exists(FilePath) && !(File.ReadLines(FilePath).FirstOrDefault() ?? "").Contains("DecisionMs", StringComparison.Ordinal))
                    File.Move(FilePath, Path.Combine(Path.GetDirectoryName(FilePath)!, $"LiveOcrPerformance_v3_{DateTime.Now:yyyyMMdd_HHmmss}.csv"), true);
                _schemaReady = true;
            }
            if (!File.Exists(FilePath))
                File.WriteAllText(FilePath, "RowType,Timestamp,Field,ExecutionMode,PipelineCountExecuted,PrimaryPipeline,FallbackUsed,HeavyFallbackUsed,CacheUsed,CaptureMs,OcrMs,ParseMs,TotalMs,Result,CharacterSegmentationMs,TemplateMatchMs,FastFullLineOcrMs,CaptureTotalMs,NumericOcrTotalMs,OperationModeOcrMs,FieldCount,CacheHitCount,FullOcrExecutionCount,TotalMonitoringCycleMs,EstimatedDigitCount,CandidateDigitCount,DigitPlausibility,TargetedFallbackUsed,TargetedFallbackMs,CacheMs,DecisionMs,ExecutionPath\r\n", new UTF8Encoding(true));
            string N(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);
            string Q(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
            File.AppendAllText(FilePath, string.Join(',', rowType, timestamp.ToString("O"), Q(field), mode, count,
                Q(primary), fallback, heavy, cache, N(capture), N(ocr), N(parse), N(total), Q(result),
                N(characterSegmentation), N(templateMatch), N(fullLineOcr),
                N(captureTotal), N(numericTotal), N(operationMode), fieldCount, cacheHits, executions, N(cycle),
                estimatedDigits, candidateDigits, Q(plausibility), targetedFallback, N(targetedFallbackMs),
                N(cacheMs), N(decisionMs), Q(executionPath)) + "\r\n", new UTF8Encoding(false));
        }
    }
}
