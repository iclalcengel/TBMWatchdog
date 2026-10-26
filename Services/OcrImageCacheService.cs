using System.Security.Cryptography;
using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public sealed record OcrCacheOptions(bool Enabled, double SimilarityThresholdPercent,
    int GateMaximumAgeSeconds, int NumericMaximumAgeSeconds);
public sealed record OcrCacheLookup(bool Hit, OcrReadResult? OfficialResult, CharacterOcrComparison? Comparison,
    double AgeMilliseconds, double SimilarityPercent, string ImageHash, string PerceptualHash);
public sealed record OcrCacheStatistics(string Field, long RealOcrCount, long CacheHitCount, long CacheMissCount,
    double HitRatePercent, double MissRatePercent, double EstimatedPreventedOcrMilliseconds,
    double AverageRealOcrMilliseconds);

public sealed class OcrImageCacheService : IDisposable
{
    private sealed class Entry : IDisposable
    {
        public readonly object Sync = new(); public Bitmap? Image; public string Hash = ""; public string PerceptualHash = "";
        public OcrReadResult? FullLine; public decimal? Character; public OcrReadResult? Official;
        public string DecisionSource = ""; public string FailureReason = ""; public DateTime CompletedAt;
        public CharacterOcrComparison? Comparison; public bool CriticalFailure;
        public long Hits; public long Misses; public long Real; public double TotalRealMs; public double PreventedMs;
        public void Dispose() { lock (Sync) { Image?.Dispose(); Image = null; } }
    }
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _entriesSync = new();
    private readonly OcrCacheOptions _options;
    public OcrImageCacheService(OcrCacheOptions options) => _options = options;
    public event Action<string>? Warning;

    public OcrCacheLookup TryGet(string field, Bitmap image, DateTime now, bool bypass)
    {
        try
        {
            var entry = GetOrCreate(field); var fingerprint = Fingerprint(image);
            lock (entry.Sync)
            {
                var similarity = entry.Image is null || entry.Image.Size != image.Size ? 0 : Similarity(entry.Image, image);
                var age = entry.CompletedAt == default ? double.MaxValue : Math.Max(0, (now - entry.CompletedAt).TotalMilliseconds);
                var maximumAge = (field == "Gate" ? _options.GateMaximumAgeSeconds : _options.NumericMaximumAgeSeconds) * 1000d;
                var contentChanged = entry.Image is not null && HasSignificantPixelChange(entry.Image, image);
                var hit = _options.Enabled && !bypass && !contentChanged && entry.Official is not null && !entry.CriticalFailure
                    && entry.Official.IsSuccessful && entry.FailureReason != "Timeout"
                    && age <= maximumAge && similarity >= _options.SimilarityThresholdPercent;
                if (hit)
                {
                    entry.Hits++; entry.PreventedMs += entry.Real == 0 ? 0 : entry.TotalRealMs / entry.Real;
                    return new(true, entry.Official, entry.Comparison, age, similarity, fingerprint.Hash, fingerprint.PerceptualHash);
                }
                entry.Misses++;
                return new(false, null, null, age, similarity, fingerprint.Hash, fingerprint.PerceptualHash);
            }
        }
        catch (Exception exception) { Warning?.Invoke($"OCR önbelleği okunamadı; gerçek OCR kullanılacak: {exception.Message}"); return new(false, null, null, 0, 0, "", ""); }
    }

    public void Store(string field, Bitmap image, OcrReadResult fullLine, decimal? character,
        OcrReadResult official, string decisionSource, string failureReason, DateTime completedAt,
        CharacterOcrComparison comparison, bool criticalFailure)
    {
        try
        {
            var entry = GetOrCreate(field); var fingerprint = Fingerprint(image); var replacement = new Bitmap(image);
            lock (entry.Sync)
            {
                var old = entry.Image; entry.Image = replacement; old?.Dispose();
                entry.Hash = fingerprint.Hash; entry.PerceptualHash = fingerprint.PerceptualHash;
                entry.FullLine = fullLine; entry.Character = character; entry.Official = official;
                entry.DecisionSource = decisionSource; entry.FailureReason = failureReason;
                entry.CompletedAt = completedAt; entry.Comparison = comparison; entry.CriticalFailure = criticalFailure;
                entry.Real++; entry.TotalRealMs += comparison.TotalOcrDurationMilliseconds;
            }
        }
        catch (Exception exception) { Warning?.Invoke($"OCR önbelleği güncellenemedi: {exception.Message}"); }
    }

    public OcrCacheStatistics Statistics(string field)
    {
        var entry = GetOrCreate(field); lock (entry.Sync)
        {
            var total = entry.Hits + entry.Misses;
            return new(field, entry.Real, entry.Hits, entry.Misses,
                total == 0 ? 0 : entry.Hits * 100d / total, total == 0 ? 0 : entry.Misses * 100d / total,
                entry.PreventedMs, entry.Real == 0 ? 0 : entry.TotalRealMs / entry.Real);
        }
    }
    public void Clear(string? field = null)
    {
        KeyValuePair<string, Entry>[] entries; lock (_entriesSync) entries = _entries.Where(pair => field is null || string.Equals(pair.Key, field, StringComparison.OrdinalIgnoreCase)).ToArray();
        foreach (var pair in entries)
            lock (pair.Value.Sync)
            {
                pair.Value.Image?.Dispose(); pair.Value.Image = null; pair.Value.Official = null;
                pair.Value.Comparison = null; pair.Value.CompletedAt = default; pair.Value.CriticalFailure = false;
            }
    }
    public void Dispose() { Entry[] entries; lock (_entriesSync) entries = _entries.Values.ToArray(); foreach (var entry in entries) entry.Dispose(); }

    private Entry GetOrCreate(string field)
    {
        if (string.IsNullOrWhiteSpace(field)) throw new ArgumentException("Alan kimliği boş olamaz.", nameof(field));
        lock (_entriesSync) return _entries.TryGetValue(field, out var entry) ? entry : _entries[field] = new Entry();
    }

    private static (string Hash, string PerceptualHash) Fingerprint(Bitmap image)
    {
        using var stream = new MemoryStream(); image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        var hash = Convert.ToHexString(SHA256.HashData(stream.ToArray()));
        using var tiny = new Bitmap(image, 9, 8); ulong bits = 0; var bit = 0;
        for (var y = 0; y < 8; y++) for (var x = 0; x < 8; x++, bit++)
        { if (Gray(tiny.GetPixel(x, y)) > Gray(tiny.GetPixel(x + 1, y))) bits |= 1UL << bit; }
        return (hash, bits.ToString("X16"));
    }
    internal static double Similarity(Bitmap first, Bitmap second)
    {
        using var a = new Bitmap(first, 16, 16); using var b = new Bitmap(second, 16, 16); double difference = 0;
        for (var y = 0; y < 16; y++) for (var x = 0; x < 16; x++) difference += Math.Abs(Gray(a.GetPixel(x, y)) - Gray(b.GetPixel(x, y)));
        return Math.Clamp(100d * (1d - difference / (16 * 16 * 255d)), 0, 100);
    }
    internal static bool HasSignificantPixelChange(Bitmap first, Bitmap second)
    {
        if (first.Size != second.Size) return true;
        var changed = 0; var total = first.Width * first.Height;
        var allowed = Math.Max(1, (int)Math.Ceiling(total * 0.005));
        for (var y = 0; y < first.Height; y++)
            for (var x = 0; x < first.Width; x++)
            {
                if (Math.Abs(Gray(first.GetPixel(x, y)) - Gray(second.GetPixel(x, y))) < 32) continue;
                if (++changed > allowed) return true;
            }
        return false;
    }
    private static int Gray(Color color) => (color.R * 299 + color.G * 587 + color.B * 114) / 1000;
}

public static class OcrCacheControl
{
    public static event Action? ClearRequested;
    public static void RequestClear() => ClearRequested?.Invoke();
}
