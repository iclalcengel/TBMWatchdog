using System.Globalization;
using System.Text;
using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public sealed record CharacterOcrComparison(
    string Field, decimal? FullLineResult, decimal? CharacterResult, bool? IsSame,
    double CharacterDurationMilliseconds, double TotalOcrDurationMilliseconds,
    DateTime CapturedAt, DateTime DisplayedAt, double TotalLatencyMilliseconds,
    string FailureReason = "Success", bool MismatchSampleSaved = false,
    string? MismatchSampleId = null, string? MismatchSamplePath = null,
    bool CacheUsed = false, double CacheAgeMs = 0, double ImageSimilarityPercent = 0,
    bool RealOcrExecuted = true, string RawOcrText = "", decimal? ParsedNumericValue = null,
    string Unit = "", bool RegionValid = true, double NumericConfidencePercent = 0,
    double CaptureMs = 0, double FastFullLineOcrMs = 0, double CharacterSegmentationMs = 0,
    double TemplateMatchMs = 0, double TargetedFallbackMs = 0, double CacheMs = 0,
    double DecisionMs = 0, string ExecutionPath = "Fast", double FeedbackMatchingMs = 0,
    int FeedbackCandidateCount = 0);

public sealed class CharacterOcrComparisonCsvService
{
    private readonly object _sync = new();
    public CharacterOcrComparisonCsvService(string? path = null)
    {
        FilePath = path ?? Path.Combine(AppContext.BaseDirectory, "Diagnostics", "LiveOcrComparison.csv");
    }

    public string FilePath { get; }

    public void Append(IEnumerable<CharacterOcrComparison> comparisons)
    {
        lock (_sync)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            CsvLogRotation.RotateIfNeeded(FilePath);
            UpgradeSchemaIfRequired();
            if (!File.Exists(FilePath))
                File.WriteAllText(FilePath,
                    "Field,FullLineTesseract,CharacterResult,Comparison,CharacterDurationMs,TotalOcrDurationMs,CapturedAt,DisplayedAt,TotalLatencyMs,FailureReason,MismatchSampleSaved,MismatchSampleId,MismatchSamplePath,CacheUsed,CacheAgeMs,ImageSimilarityPercent,RealOcrExecuted,RawOcrText,ParsedNumericValue,Unit,RegionValid\r\n",
                    new UTF8Encoding(true));
            using var writer = new StreamWriter(FilePath, append: true, new UTF8Encoding(false));
            foreach (var item in comparisons)
                writer.WriteLine(string.Join(',',
                    Escape(item.Field), Number(item.FullLineResult), Number(item.CharacterResult),
                    item.IsSame switch { true => "Aynı", false => "Farklı", _ => "Karşılaştırılamadı" },
                    item.CharacterDurationMilliseconds.ToString("0.0", CultureInfo.InvariantCulture),
                    item.TotalOcrDurationMilliseconds.ToString("0.0", CultureInfo.InvariantCulture),
                    item.CapturedAt.ToString("O", CultureInfo.InvariantCulture),
                    item.DisplayedAt.ToString("O", CultureInfo.InvariantCulture),
                    item.TotalLatencyMilliseconds.ToString("0.0", CultureInfo.InvariantCulture),
                    item.FailureReason, item.MismatchSampleSaved ? "true" : "false",
                    Escape(item.MismatchSampleId ?? ""), Escape(item.MismatchSamplePath ?? ""),
                    item.CacheUsed ? "true" : "false", item.CacheAgeMs.ToString("0.0", CultureInfo.InvariantCulture),
                    item.ImageSimilarityPercent.ToString("0.000", CultureInfo.InvariantCulture), item.RealOcrExecuted ? "true" : "false",
                    Escape(item.RawOcrText), Number(item.ParsedNumericValue), Escape(item.Unit), item.RegionValid ? "true" : "false"));
        }
    }

    public static CharacterOcrComparison Create(string field, OcrDiagnosticResult diagnostic,
        DateTime capturedAt, DateTime displayedAt, int maximumSegments = 8)
    {
        var fullLine = OcrService.SelectFullLineTesseractResult(diagnostic.Candidates);
        var character = diagnostic.Candidates
            .Where(candidate => candidate.UsesCharacterBasedOcr && candidate.IsValid)
            .OrderByDescending(candidate => candidate.AllCharactersTemplateAuthoritative)
            .ThenByDescending(candidate => candidate.CharacterDecisionReliable)
            .ThenByDescending(candidate => candidate.SystemConfidence)
            .FirstOrDefault();
        var failureReason = ClassifyFailure(diagnostic.Candidates, character, maximumSegments);
        var numericConfidence = diagnostic.Candidates.Where(candidate => candidate.ParsedValue.HasValue)
            .Select(candidate => (double)candidate.Confidence * 100d).DefaultIfEmpty(0).Max();
        return new CharacterOcrComparison(field, fullLine.Value, character?.ParsedValue,
            fullLine.Value.HasValue && character?.ParsedValue is { } characterValue
                ? fullLine.Value.Value == characterValue : null,
            character?.OcrElapsed.TotalMilliseconds ?? 0,
            diagnostic.TotalElapsed.TotalMilliseconds, capturedAt, displayedAt,
            Math.Max(0, (displayedAt - capturedAt).TotalMilliseconds), failureReason,
            NumericConfidencePercent: numericConfidence);
    }

    internal static string ClassifyFailure(IReadOnlyList<OcrCandidateResult> candidates,
        OcrCandidateResult? character, int maximumSegments)
    {
        if (character is not null) return "Success";
        if (candidates.Any(candidate => candidate.IsValid && candidate.CharacterValidationStatus == "Mismatch")) return "CharacterMismatch";
        if (candidates.Any(candidate => candidate.Error?.Contains("timeout", StringComparison.OrdinalIgnoreCase) == true
            || candidate.Error?.Contains("zaman aş", StringComparison.OrdinalIgnoreCase) == true)) return "Timeout";
        var maximumFound = candidates.Select(candidate => candidate.SegmentCount).DefaultIfEmpty(0).Max();
        if (maximumFound == 0) return "NoSegments";
        if (maximumFound > maximumSegments) return "TooManySegments";
        var diagnostics = candidates.SelectMany(candidate => candidate.CharacterDiagnostics).ToArray();
        if (diagnostics.Any(item => item.InkLossRatio > 0.25 && string.IsNullOrEmpty(item.FinalDigit))) return "InkLossExceeded";
        if (diagnostics.Any(item => item.TemplateResult is not null && !item.IsTemplateConfident
            && item.TemplateResult != item.CleanedDigit)) return "TemplateConflict";
        if (diagnostics.Any(item => item.GeometryResult != "—" && string.IsNullOrEmpty(item.FinalDigit))) return "GeometryRejected";
        return "LowConfidence";
    }

    private void UpgradeSchemaIfRequired()
    {
        if (!File.Exists(FilePath)) return;
        var lines = File.ReadAllLines(FilePath);
        if (lines.Length == 0) return;
        var addFailure = !lines[0].Contains("FailureReason", StringComparison.Ordinal);
        var addMismatch = !lines[0].Contains("MismatchSampleSaved", StringComparison.Ordinal);
        var addCache = !lines[0].Contains("CacheUsed", StringComparison.Ordinal);
        var addNumericDetails = !lines[0].Contains("RawOcrText", StringComparison.Ordinal);
        if (!addFailure && !addMismatch && !addCache && !addNumericDetails) return;
        if (addFailure) lines[0] += ",FailureReason";
        if (addMismatch) lines[0] += ",MismatchSampleSaved,MismatchSampleId,MismatchSamplePath";
        if (addCache) lines[0] += ",CacheUsed,CacheAgeMs,ImageSimilarityPercent,RealOcrExecuted";
        if (addNumericDetails) lines[0] += ",RawOcrText,ParsedNumericValue,Unit,RegionValid";
        for (var index = 1; index < lines.Length; index++)
        {
            var columns = lines[index].Split(',');
            if (addFailure) lines[index] += columns.Length > 2 && !string.IsNullOrWhiteSpace(columns[2]) ? ",Success" : ",LowConfidence";
            if (addMismatch) lines[index] += ",false,\"\",\"\"";
            if (addCache) lines[index] += ",false,0,0,true";
            if (addNumericDetails) lines[index] += ",\"\",,\"\",true";
        }
        File.WriteAllLines(FilePath, lines, new UTF8Encoding(true));
    }

    private static string Number(decimal? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "";
    private static string Escape(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
