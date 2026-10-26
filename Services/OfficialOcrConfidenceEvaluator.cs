using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public static class OfficialOcrConfidenceEvaluator
{
    public static OcrConfidenceStatus FromResults(params OcrReadResult[] results) =>
        FromReadableCount(results.Count(result => result.IsSuccessful), results.Length);

    public static OcrConfidenceStatus FromValues(params decimal?[] values) =>
        FromReadableCount(values.Count(value => value.HasValue), values.Length);

    internal static OcrConfidenceStatus FromReadableCount(int readableCount, int totalCount) => readableCount switch
    {
        0 => OcrConfidenceStatus.Unreadable,
        _ when readableCount == totalCount => OcrConfidenceStatus.Reliable,
        _ => OcrConfidenceStatus.Partial
    };
}
