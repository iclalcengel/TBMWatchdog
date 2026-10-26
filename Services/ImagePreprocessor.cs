using System.Drawing.Imaging;

namespace TBMWatchdog.Services;

public static class ImagePreprocessor
{
    public static Bitmap WarmDigitMask(Bitmap source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var output = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
        for (var y = 0; y < source.Height; y++) for (var x = 0; x < source.Width; x++)
        {
            var p = source.GetPixel(x, y);
            var maximum = Math.Max(p.R, Math.Max(p.G, p.B));
            var minimum = Math.Min(p.R, Math.Min(p.G, p.B));
            var chroma = maximum - minimum;
            // 1031 HMI tork rakamları sıcak kahverengi, kutu/çerçeve ise nötr gridir.
            // Parlaklık yerine renk farkını kullanmak ince baştaki "1" çizgisini korur.
            var warmForeground = chroma >= 12 && p.R >= p.G + 7 && p.G >= p.B + 2
                && p.R < 245 && p.G < 220;
            output.SetPixel(x, y, warmForeground ? Color.Black : Color.White);
        }
        return output;
    }

    public static Bitmap Grayscale(Bitmap source)
    {
        var output = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
        for (var y = 0; y < source.Height; y++) for (var x = 0; x < source.Width; x++)
        {
            var p = source.GetPixel(x, y);
            var v = (byte)((p.R * 299 + p.G * 587 + p.B * 114) / 1000);
            output.SetPixel(x, y, Color.FromArgb(v, v, v));
        }
        return output;
    }

    public static Bitmap AutoContrast(Bitmap source)
    {
        var min = 255; var max = 0;
        for (var y = 0; y < source.Height; y++) for (var x = 0; x < source.Width; x++)
        { var v = source.GetPixel(x, y).R; min = Math.Min(min, v); max = Math.Max(max, v); }
        var output = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
        var range = Math.Max(1, max - min);
        for (var y = 0; y < source.Height; y++) for (var x = 0; x < source.Width; x++)
        { var v = (source.GetPixel(x, y).R - min) * 255 / range; output.SetPixel(x, y, Color.FromArgb(v, v, v)); }
        return output;
    }

    public static Bitmap GaussianBlur(Bitmap source) => Convolve(source, new[,] { { 1, 2, 1 }, { 2, 4, 2 }, { 1, 2, 1 } }, 16);

    public static Bitmap MedianFilter(Bitmap source)
    {
        var output = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
        var values = new byte[9];
        for (var y = 0; y < source.Height; y++) for (var x = 0; x < source.Width; x++)
        {
            var index = 0;
            for (var yy = -1; yy <= 1; yy++) for (var xx = -1; xx <= 1; xx++)
                values[index++] = source.GetPixel(Math.Clamp(x + xx, 0, source.Width - 1), Math.Clamp(y + yy, 0, source.Height - 1)).R;
            Array.Sort(values); var v = values[4]; output.SetPixel(x, y, Color.FromArgb(v, v, v));
        }
        return output;
    }

    public static Bitmap MorphologyOpen(Bitmap binary) { using var eroded = Erode(binary); return Dilate(eroded); }
    public static Bitmap MorphologyClose(Bitmap binary) { using var dilated = Dilate(binary); return Erode(dilated); }

    public static Bitmap RemoveHorizontalLines(Bitmap binary, double minimumWidthRatio = 0.45)
    {
        var output = new Bitmap(binary);
        var minimumRun = Math.Max(6, (int)(binary.Width * minimumWidthRatio));
        for (var y = 0; y < binary.Height; y++)
        {
            var longestRun = 0; var currentRun = 0;
            for (var x = 0; x < binary.Width; x++)
            {
                if (binary.GetPixel(x, y).R < 128) { currentRun++; longestRun = Math.Max(longestRun, currentRun); }
                else currentRun = 0;
            }
            if (longestRun < minimumRun) continue;
            for (var x = 0; x < output.Width; x++) output.SetPixel(x, y, Color.White);
        }
        return output;
    }

    private static Bitmap Erode(Bitmap source) => Morph(source, foregroundMustFill: true);
    private static Bitmap Dilate(Bitmap source) => Morph(source, foregroundMustFill: false);

    private static Bitmap Morph(Bitmap source, bool foregroundMustFill)
    {
        var output = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
        for (var y = 0; y < source.Height; y++) for (var x = 0; x < source.Width; x++)
        {
            var black = foregroundMustFill;
            for (var yy = -1; yy <= 1; yy++) for (var xx = -1; xx <= 1; xx++)
            {
                var sample = source.GetPixel(Math.Clamp(x + xx, 0, source.Width - 1), Math.Clamp(y + yy, 0, source.Height - 1)).R < 128;
                black = foregroundMustFill ? black && sample : black || sample;
            }
            var v = black ? 0 : 255; output.SetPixel(x, y, Color.FromArgb(v, v, v));
        }
        return output;
    }

    private static Bitmap Convolve(Bitmap source, int[,] kernel, int divisor)
    {
        var output = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
        for (var y = 0; y < source.Height; y++) for (var x = 0; x < source.Width; x++)
        {
            var sum = 0;
            for (var yy = -1; yy <= 1; yy++) for (var xx = -1; xx <= 1; xx++)
                sum += source.GetPixel(Math.Clamp(x + xx, 0, source.Width - 1), Math.Clamp(y + yy, 0, source.Height - 1)).R * kernel[yy + 1, xx + 1];
            var v = Math.Clamp(sum / divisor, 0, 255); output.SetPixel(x, y, Color.FromArgb(v, v, v));
        }
        return output;
    }
}
