using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using TBMWatchdog.Models;
using Tesseract;

namespace TBMWatchdog.Services;

public sealed class TextOcrService : IDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1); private TesseractEngine? _engine; private bool _disposed;
    private readonly string _tessdataPath; private readonly string _language;
    public TextOcrService(string? tessdataPath = null, string? language = null)
    {
        _tessdataPath = tessdataPath ?? Path.Combine(AppContext.BaseDirectory, "tessdata");
        _language = language ?? (File.Exists(Path.Combine(_tessdataPath, "tur.traineddata")) ? "tur+eng" : "eng");
    }
    public bool UsesNumericWhitelist => false;
    internal int EngineCreationCount { get; private set; }

    public async Task<TextOcrResult> ReadAsync(Bitmap source, CancellationToken token = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this); ArgumentNullException.ThrowIfNull(source);
        await _lock.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (IsNearlyUniform(source)) return new("", 0, "Seçilen bölgede ayırt edilebilir metin pikseli bulunamadı.")
            { FailureReason = "OcrEmpty", NumericWhitelistUsed = false, TrimmedText = "", NormalizedText = "", FinalOperationModeText = "" };
            _engine ??= CreateEngine(); var candidates = new List<TextOcrCandidate>();
            using var variants = CreateVariants(source);
            foreach (var variant in variants.Items)
            foreach (var mode in new[] { PageSegMode.SingleLine, PageSegMode.SingleWord, PageSegMode.SparseText })
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    using var pix = PixConverter.ToPix(variant.Image); using var page = _engine.Process(pix, mode);
                    var raw = page.GetText() ?? ""; candidates.Add(new(variant.Method, mode.ToString(), raw, raw.Trim(), page.GetMeanConfidence()));
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                { candidates.Add(new(variant.Method, mode.ToString(), "", "", 0, exception.Message)); }
            }
            var best = candidates.Where(candidate => !string.IsNullOrWhiteSpace(candidate.TrimmedText))
                .OrderByDescending(candidate => CandidateScore(candidate)).FirstOrDefault();
            if (best is null)
            {
                var error = candidates.Select(candidate => candidate.Error).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
                return new("", 0, error ?? "Tesseract bütün metin adaylarında boş sonuç üretti.")
                { TrimmedText = "", NormalizedText = "", FinalOperationModeText = "", FailureReason = "OcrEmpty",
                    BestPageSegMode = candidates.OrderByDescending(c => c.Confidence).FirstOrDefault()?.PageSegMode ?? "—",
                    BestPreprocessingMethod = candidates.OrderByDescending(c => c.Confidence).FirstOrDefault()?.PreprocessingMethod ?? "—",
                    Candidates = candidates, NumericWhitelistUsed = false };
            }
            var normalized = OperationModeStateService.Normalize(best.TrimmedText);
            return new(best.RawText, best.Confidence)
            { TrimmedText = best.TrimmedText, NormalizedText = normalized, FinalOperationModeText = best.TrimmedText,
                BestPageSegMode = best.PageSegMode, BestPreprocessingMethod = best.PreprocessingMethod,
                FailureReason = "Success", Candidates = candidates, NumericWhitelistUsed = false };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        { return new("", 0, exception.Message) { FailureReason = "OcrEngineError", NumericWhitelistUsed = false }; }
        finally { _lock.Release(); }
    }

    private TesseractEngine CreateEngine()
    {
        if (!Directory.Exists(_tessdataPath)) throw new OcrConfigurationException($"Tessdata klasörü bulunamadı: {_tessdataPath}");
        var engine = new TesseractEngine(_tessdataPath, _language, EngineMode.LstmOnly);
        EngineCreationCount++;
        engine.SetVariable("preserve_interword_spaces", "1");
        // OperationMode metin OCR'ında tessedit_char_whitelist ve numeric mode özellikle kullanılmaz.
        return engine;
    }

    private static double CandidateScore(TextOcrCandidate candidate)
    {
        var letters = candidate.TrimmedText.Count(char.IsLetter); var controls = candidate.TrimmedText.Count(char.IsControl);
        return candidate.Confidence + Math.Min(0.20, letters * 0.012) - controls * 0.05;
    }
    private static bool IsNearlyUniform(Bitmap source)
    {
        var minimum = 255; var maximum = 0; var stepX = Math.Max(1, source.Width / 100); var stepY = Math.Max(1, source.Height / 50);
        for (var y = 0; y < source.Height; y += stepY) for (var x = 0; x < source.Width; x += stepX)
        { var color = source.GetPixel(x, y); var value = (int)(color.R * .299 + color.G * .587 + color.B * .114); minimum = Math.Min(minimum, value); maximum = Math.Max(maximum, value); }
        return maximum - minimum < 8;
    }

    private static TextVariantCollection CreateVariants(Bitmap source)
    {
        var gray = ScaleAndGray(source, 3, false); var contrast = ScaleAndGray(source, 3, true);
        var otsu = Threshold(gray, OtsuThreshold(gray), false); var adaptive = AdaptiveThreshold(gray, 12, 7);
        return new([new("Orijinal gri + 3x", gray), new("Kontrast artırılmış + 3x", contrast),
            new("Otsu + 3x", otsu), new("Adaptive threshold + 3x", adaptive)]);
    }

    private static Bitmap ScaleAndGray(Bitmap source, int scale, bool contrast)
    {
        var scaled = new Bitmap(Math.Max(1, source.Width * scale), Math.Max(1, source.Height * scale), PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(scaled)) { graphics.Clear(Color.White); graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = PixelOffsetMode.Half; graphics.DrawImage(source, new Rectangle(0, 0, scaled.Width, scaled.Height)); }
        for (var y = 0; y < scaled.Height; y++) for (var x = 0; x < scaled.Width; x++)
        { var color = scaled.GetPixel(x, y); var value = (int)(color.R * .299 + color.G * .587 + color.B * .114);
            if (contrast) value = Math.Clamp((int)((value - 128) * 1.65 + 128), 0, 255); scaled.SetPixel(x, y, Color.FromArgb(value, value, value)); }
        return scaled;
    }
    private static int OtsuThreshold(Bitmap image)
    {
        var histogram = new int[256]; for (var y = 0; y < image.Height; y++) for (var x = 0; x < image.Width; x++) histogram[image.GetPixel(x, y).R]++;
        var total = image.Width * image.Height; double sum = 0; for (var i = 0; i < 256; i++) sum += i * histogram[i];
        double sumB = 0, max = -1; var weightB = 0; var threshold = 128;
        for (var i = 0; i < 256; i++) { weightB += histogram[i]; if (weightB == 0) continue; var weightF = total - weightB; if (weightF == 0) break;
            sumB += i * histogram[i]; var between = weightB * (double)weightF * Math.Pow(sumB / weightB - (sum - sumB) / weightF, 2);
            if (between > max) { max = between; threshold = i; } }
        return threshold;
    }
    private static Bitmap Threshold(Bitmap source, int threshold, bool invert)
    {
        var output = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb); for (var y = 0; y < source.Height; y++) for (var x = 0; x < source.Width; x++)
        { var black = source.GetPixel(x, y).R < threshold; if (invert) black = !black; output.SetPixel(x, y, black ? Color.Black : Color.White); } return output;
    }
    private static Bitmap AdaptiveThreshold(Bitmap source, int radius, int bias)
    {
        var output = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb); var integral = new long[source.Width + 1, source.Height + 1];
        for (var y = 1; y <= source.Height; y++) for (var x = 1; x <= source.Width; x++) integral[x, y] = source.GetPixel(x - 1, y - 1).R + integral[x - 1, y] + integral[x, y - 1] - integral[x - 1, y - 1];
        for (var y = 0; y < source.Height; y++) for (var x = 0; x < source.Width; x++) { var x1 = Math.Max(0, x - radius); var y1 = Math.Max(0, y - radius); var x2 = Math.Min(source.Width - 1, x + radius); var y2 = Math.Min(source.Height - 1, y + radius);
            var sum = integral[x2 + 1, y2 + 1] - integral[x1, y2 + 1] - integral[x2 + 1, y1] + integral[x1, y1]; var mean = sum / (double)((x2 - x1 + 1) * (y2 - y1 + 1));
            output.SetPixel(x, y, source.GetPixel(x, y).R < mean - bias ? Color.Black : Color.White); } return output;
    }
    public void Dispose() { if (_disposed) return; _disposed = true; _engine?.Dispose(); _lock.Dispose(); }
    private sealed record TextVariant(string Method, Bitmap Image);
    private sealed class TextVariantCollection(IReadOnlyList<TextVariant> items) : IDisposable
    { public IReadOnlyList<TextVariant> Items { get; } = items; public void Dispose() { foreach (var item in Items) item.Image.Dispose(); } }
}

public sealed record OperationModeTextOcrEvaluation(TextOcrResult Ocr, OperationModeTextMatch Match, string FailureReason)
{ public bool TextRead => Ocr.IsSuccessful; public bool KeywordMatched => Match.State != OperationModeState.Unknown; }

public static class OperationModeTextOcrEvaluator
{
    public static OperationModeTextOcrEvaluation Evaluate(TextOcrResult result, OcrProfile profile)
    {
        var keywords = profile.OperationModeKeywords.SelectMany(group => group.Keywords.Select(keyword =>
            (group.State, OperationModeStateService.Normalize(keyword).Replace(" ", "")))).ToArray();
        var match = result.IsSuccessful
            ? OperationModeTextMatcher.Match(result.NormalizedText, keywords, profile.OperationModeMinimumTextSimilarityPercent)
            : new OperationModeTextMatch(OperationModeState.Unknown, "", OperationModeMatchMethod.None, 0);
        return new(result, match, !result.IsSuccessful ? result.FailureReason : match.State == OperationModeState.Unknown ? "KeywordNotMatched" : "Success");
    }
}
