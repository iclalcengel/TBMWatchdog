using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace TBMWatchdog.Services;

public sealed record DigitTemplateMatch(char Digit, double Score, char? SecondBestDigit,
    double SecondBestScore, double ScoreMargin, int TemplateCount, string? TemplatePath = null);

public static class DigitTemplateMatcher
{
    public static event Action<string>? TemplatesReloaded;
    public const int CanvasWidth = 48;
    public const int CanvasHeight = 72;

    private static readonly object CacheLock = new();
    private static readonly Dictionary<string, List<TemplateSample>> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static DigitTemplateMatch? Match(Bitmap character, string? templateFolder, double maximumAllowedInkLoss = 0.25)
    {
        if (string.IsNullOrWhiteSpace(templateFolder) || !Directory.Exists(templateFolder)) return null;
        using var normalizedCharacter = NormalizeForMatching(character, maximumAllowedInkLoss);
        lock (CacheLock)
        {
            var templates = GetTemplates(templateFolder, maximumAllowedInkLoss);
            var digitScores = new List<(char Digit, double Score, string? Path)>();
            for (var digit = '0'; digit <= '9'; digit++)
            {
                var bestScores = templates.Where(sample => sample.Digit == digit)
                    .Select(sample => (Score: WeightedScore(CalculateSimilarity(normalizedCharacter, sample.Image), sample.Metadata), sample.Path))
                    .OrderByDescending(item => item.Score).Take(3).ToArray();
                if (bestScores.Length > 0) digitScores.Add((digit, bestScores.Average(item => item.Score), bestScores[0].Path));
            }
            if (digitScores.Count == 0) return null;
            var ordered = digitScores.OrderByDescending(item => item.Score).ToArray();
            var second = ordered.Length > 1 ? ordered[1] : ((char Digit, double Score, string? Path)?)null;
            var result = new DigitTemplateMatch(ordered[0].Digit, ordered[0].Score,
                second?.Digit, second?.Score ?? 0, ordered[0].Score - (second?.Score ?? 0), templates.Count, ordered[0].Path);
            DigitTemplateUsageTracker.RecordUsage(result.TemplatePath); return result;
        }
    }

    public static void Reload(string templateFolder)
    {
        lock (CacheLock)
        {
            var prefix = Path.GetFullPath(templateFolder) + "|";
            foreach (var key in Cache.Keys.Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToArray())
                if (Cache.Remove(key, out var samples)) foreach (var sample in samples) sample.Image.Dispose();
        }
        TemplatesReloaded?.Invoke(templateFolder);
    }

    public static Bitmap NormalizeForMatching(Bitmap source, double maximumAllowedInkLoss = 0.25)
    {
        using var standardized = StandardizeBinaryDirection(source);
        using var normalized = CharacterOcrNormalizer.NormalizeCharacterForOcr(
            standardized, CanvasWidth, CanvasHeight, maximumAllowedInkLoss);
        return new Bitmap(normalized.Normalized);
    }

    private static Bitmap StandardizeBinaryDirection(Bitmap source)
    {
        var output = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
        var darkBorder = 0; var borderCount = 0;
        for (var y = 0; y < source.Height; y++)
        for (var x = 0; x < source.Width; x++)
        {
            if (x != 0 && y != 0 && x != source.Width - 1 && y != source.Height - 1) continue;
            borderCount++;
            if (source.GetPixel(x, y).GetBrightness() < 0.5f) darkBorder++;
        }
        var invert = darkBorder > borderCount / 2;
        for (var y = 0; y < source.Height; y++)
        for (var x = 0; x < source.Width; x++)
        {
            var dark = source.GetPixel(x, y).GetBrightness() < 0.5f;
            var foreground = invert ? !dark : dark;
            output.SetPixel(x, y, foreground ? Color.Black : Color.White);
        }
        return output;
    }

    public static double CalculateSimilarity(Bitmap left, Bitmap right)
    {
        if (left.Size != new Size(CanvasWidth, CanvasHeight) || right.Size != new Size(CanvasWidth, CanvasHeight))
        {
            using var normalizedLeft = NormalizeForMatching(left);
            using var normalizedRight = NormalizeForMatching(right);
            return CombinedSimilarity(normalizedLeft, normalizedRight);
        }
        return CombinedSimilarity(left, right);
    }

    private static List<TemplateSample> GetTemplates(string folder, double maximumAllowedInkLoss)
    {
        var fullPath = Path.GetFullPath(folder);
        var cacheKey = fullPath + "|" + maximumAllowedInkLoss.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        if (Cache.TryGetValue(cacheKey, out var cached)) return cached;
        var samples = new List<TemplateSample>();
        foreach (var path in Directory.EnumerateFiles(fullPath, "*.png", SearchOption.AllDirectories))
        {
            var digit = ParseDigit(path);
            if (digit is null || !IsEnabled(path)) continue;
            try
            {
                using var source = new Bitmap(path);
                var normalized = NormalizeForMatching(source, maximumAllowedInkLoss);
                var sharedNumeric = fullPath.Contains("SharedNumericDigits", StringComparison.OrdinalIgnoreCase);
                if (sharedNumeric && !DigitTemplateStore.PassesQuality(normalized, out _)) { normalized.Dispose(); continue; }
                var metadata = DigitTemplateStore.ReadMetadata(Path.ChangeExtension(path, ".json"))
                    ?? new Models.DigitTemplateMetadata { Digit = digit.Value - '0', UserVerified = true, QualityScore = .80 };
                samples.Add(new TemplateSample(digit.Value, normalized, path, metadata));
            }
            catch (ArgumentException) { }
            catch (ExternalException) { }
        }
        Cache[cacheKey] = samples;
        return samples;
    }

    private static char? ParseDigit(string path)
    {
        var parent = Directory.GetParent(path)?.Name;
        if (parent is { Length: 1 } && char.IsDigit(parent[0])) return parent[0];
        var name = Path.GetFileNameWithoutExtension(path);
        return name.Length > 0 && char.IsDigit(name[0]) && (name.Length == 1 || name[1] == '_') ? name[0] : null;
    }

    private static bool IsEnabled(string imagePath)
    {
        var metadataPath = Path.ChangeExtension(imagePath, ".json");
        if (!File.Exists(metadataPath)) return true;
        try { return System.Text.Json.JsonDocument.Parse(File.ReadAllText(metadataPath)).RootElement.TryGetProperty("Enabled", out var value) ? value.GetBoolean() : true; }
        catch { return true; }
    }

    private static double CombinedSimilarity(Bitmap left, Bitmap right)
    {
        var different = 0; var leftInk = 0; var rightInk = 0; var intersection = 0;
        for (var y = 0; y < CanvasHeight; y++)
        for (var x = 0; x < CanvasWidth; x++)
        {
            var a = left.GetPixel(x, y).R < 128;
            var b = right.GetPixel(x, y).R < 128;
            if (a != b) different++;
            if (a) leftInk++;
            if (b) rightInk++;
            if (a && b) intersection++;
        }
        var pixelDifferenceScore = 1 - different / (double)(CanvasWidth * CanvasHeight);
        var dice = leftInk + rightInk == 0 ? 0 : 2d * intersection / (leftInk + rightInk);
        return Math.Clamp(pixelDifferenceScore * 0.35 + dice * 0.65, 0, 1);
    }

    private static double WeightedScore(double similarity, Models.DigitTemplateMetadata metadata)
    {
        var quality = metadata.QualityScore <= 0 ? .80 : metadata.QualityScore;
        var verified = metadata.UserVerified ? 1d : .5d;
        var historical = metadata.UsageCount == 0 ? .75 : Math.Clamp(metadata.SuccessCount / (double)Math.Max(1, metadata.UsageCount), 0, 1);
        return Math.Clamp(similarity * .85 + quality * .08 + verified * .04 + historical * .03, 0, 1);
    }

    private sealed record TemplateSample(char Digit, Bitmap Image, string Path, Models.DigitTemplateMetadata Metadata);
}
