using System.Drawing.Imaging;

namespace TBMWatchdog.Services;

public static class BinaryImageBuilder
{
    public static Bitmap Otsu(Bitmap grayscale)
    {
        var histogram = new long[256];
        for (var y = 0; y < grayscale.Height; y++) for (var x = 0; x < grayscale.Width; x++) histogram[grayscale.GetPixel(x, y).R]++;
        var total = (long)grayscale.Width * grayscale.Height; long sum = 0;
        for (var i = 0; i < 256; i++) sum += i * histogram[i];
        long weight = 0, partial = 0; double bestVariance = -1; var threshold = 127;
        for (var i = 0; i < 256; i++)
        {
            weight += histogram[i]; if (weight == 0) continue; var foreground = total - weight; if (foreground == 0) break;
            partial += i * histogram[i]; var a = (double)partial / weight; var b = (double)(sum - partial) / foreground;
            var variance = weight * (double)foreground * (a - b) * (a - b); if (variance > bestVariance) { bestVariance = variance; threshold = i; }
        }
        return Build(grayscale, (x, y) => grayscale.GetPixel(x, y).R > threshold);
    }

    public static Bitmap Adaptive(Bitmap grayscale, int radius = 12, int bias = 7) => Build(grayscale, (x, y) =>
    {
        long sum = 0; var count = 0;
        for (var yy = Math.Max(0, y - radius); yy <= Math.Min(grayscale.Height - 1, y + radius); yy += 2)
        for (var xx = Math.Max(0, x - radius); xx <= Math.Min(grayscale.Width - 1, x + radius); xx += 2)
        { sum += grayscale.GetPixel(xx, yy).R; count++; }
        return grayscale.GetPixel(x, y).R >= sum / Math.Max(1, count) + bias;
    });

    public static (double ForegroundRatio, double NoiseRatio, double Quality) Analyze(Bitmap binary)
    {
        var black = 0; var isolated = 0; var total = binary.Width * binary.Height;
        for (var y = 1; y < binary.Height - 1; y++) for (var x = 1; x < binary.Width - 1; x++)
        {
            if (binary.GetPixel(x, y).R >= 128) continue; black++; var neighbors = 0;
            for (var yy = -1; yy <= 1; yy++) for (var xx = -1; xx <= 1; xx++) if ((xx != 0 || yy != 0) && binary.GetPixel(x + xx, y + yy).R < 128) neighbors++;
            if (neighbors <= 1) isolated++;
        }
        var ratio = black / (double)Math.Max(1, total); var noise = isolated / (double)Math.Max(1, black);
        var occupancy = 1 - Math.Min(1, Math.Abs(ratio - 0.18) / 0.18);
        return (ratio, noise, Math.Clamp(occupancy * 0.65 + (1 - noise) * 0.35, 0, 1));
    }

    private static Bitmap Build(Bitmap source, Func<int, int, bool> isWhite)
    {
        var output = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
        for (var y = 0; y < source.Height; y++) for (var x = 0; x < source.Width; x++)
        { var v = isWhite(x, y) ? 255 : 0; output.SetPixel(x, y, Color.FromArgb(v, v, v)); }
        NormalizePolarity(output); return output;
    }

    private static void NormalizePolarity(Bitmap image)
    {
        var darkBorder = 0; var border = 0;
        for (var x = 0; x < image.Width; x++) { Count(x, 0); Count(x, image.Height - 1); }
        for (var y = 1; y < image.Height - 1; y++) { Count(0, y); Count(image.Width - 1, y); }
        if (darkBorder * 2 <= border) return;
        for (var y = 0; y < image.Height; y++) for (var x = 0; x < image.Width; x++)
        { var v = image.GetPixel(x, y).R < 128 ? 255 : 0; image.SetPixel(x, y, Color.FromArgb(v, v, v)); }
        void Count(int x, int y) { border++; if (image.GetPixel(x, y).R < 128) darkBorder++; }
    }
}
