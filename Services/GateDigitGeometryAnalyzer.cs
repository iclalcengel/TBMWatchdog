namespace TBMWatchdog.Services;

public sealed record GateDigitGeometryResult(char? SuggestedDigit, double Confidence, bool IsNarrow,
    bool HasStrongMiddleHorizontal, bool HasDominantVertical, string Description);

public static class GateDigitGeometryAnalyzer
{
    public static GateDigitGeometryResult Analyze(Bitmap image)
    {
        var bounds = FindInkBounds(image);
        if (bounds.IsEmpty) return new(null, 0, false, false, false, "Geçerli siyah piksel bulunamadı.");
        var aspect = bounds.Width / (double)Math.Max(1, bounds.Height);
        var isNarrow = aspect <= 0.48;
        var middleStart = bounds.Top + bounds.Height * 35 / 100;
        var middleEnd = bounds.Top + bounds.Height * 65 / 100;
        var strongestHorizontal = 0;
        for (var y = middleStart; y <= middleEnd && y < bounds.Bottom; y++)
        {
            var count = 0;
            for (var x = bounds.Left; x < bounds.Right; x++) if (image.GetPixel(x, y).R < 128) count++;
            strongestHorizontal = Math.Max(strongestHorizontal, count);
        }
        var horizontalRatio = strongestHorizontal / (double)Math.Max(1, bounds.Height);
        var hasMiddle = horizontalRatio >= 0.42;
        var strongestVertical = 0;
        for (var x = bounds.Left; x < bounds.Right; x++)
        {
            var count = 0;
            for (var y = bounds.Top; y < bounds.Bottom; y++) if (image.GetPixel(x, y).R < 128) count++;
            strongestVertical = Math.Max(strongestVertical, count);
        }
        var verticalRatio = strongestVertical / (double)Math.Max(1, bounds.Height);
        var hasVertical = verticalRatio >= 0.70;
        var suggestsOne = isNarrow && !hasMiddle && hasVertical;
        var confidence = suggestsOne ? Math.Clamp(0.55 + verticalRatio * 0.30 + (0.48 - aspect) * 0.25, 0, 0.95) : 0;
        var description = $"En/boy={aspect:0.00}; orta yatay={horizontalRatio:0.00}; baskın dikey={verticalRatio:0.00}"
            + (suggestsOne ? "; geometri 1'i destekliyor." : "; özel 1 geometrisi yok.");
        return new(suggestsOne ? '1' : null, confidence, isNarrow, hasMiddle, hasVertical, description);
    }

    private static Rectangle FindInkBounds(Bitmap image)
    {
        var left = image.Width; var top = image.Height; var right = -1; var bottom = -1;
        for (var y = 0; y < image.Height; y++)
        for (var x = 0; x < image.Width; x++)
        {
            if (image.GetPixel(x, y).R >= 128) continue;
            left = Math.Min(left, x); top = Math.Min(top, y); right = Math.Max(right, x); bottom = Math.Max(bottom, y);
        }
        return right < left ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }
}
