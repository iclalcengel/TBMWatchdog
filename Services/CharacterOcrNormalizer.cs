using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace TBMWatchdog.Services;

public sealed class NormalizedCharacter : IDisposable
{
    public required Bitmap Cleaned { get; init; }
    public required Bitmap TightCropped { get; init; }
    public required Bitmap Normalized { get; init; }
    public required Rectangle InkBounds { get; init; }
    public required bool IsValid { get; init; }
    public required bool UsedOriginalFallback { get; init; }
    public required double RemovedInkRatio { get; init; }
    public void Dispose() { Cleaned.Dispose(); TightCropped.Dispose(); Normalized.Dispose(); }
}

public static class CharacterOcrNormalizer
{
    public static NormalizedCharacter NormalizeCharacterForOcr(Bitmap source, int width = 64, int height = 96,
        double maximumAllowedInkLoss = 0.25)
    {
        var components = FindComponents(source);
        var originalInkCount = components.Sum(c => c.Pixels.Count);
        var frameThickness = Math.Max(2, Math.Min(source.Width, source.Height) / 20);
        bool IsFrameLine(Component component) => component.TouchesEdge
            && ((component.Bounds.Width >= source.Width * 0.75 && component.Bounds.Height <= frameThickness)
                || (component.Bounds.Height >= source.Height * 0.75 && component.Bounds.Width <= frameThickness));
        var largest = components.Where(c => !IsFrameLine(c)).OrderByDescending(c => c.Pixels.Count).FirstOrDefault();
        var minimumArea = Math.Max(4, (int)Math.Ceiling(source.Width * source.Height * 0.0025));
        var retained = largest is null ? [] : components.Where(component =>
            !IsFrameLine(component)
            && (ReferenceEquals(component, largest)
                || component.Pixels.Count >= minimumArea
                && Expanded(largest.Bounds, Math.Max(3, source.Width / 10)).IntersectsWith(component.Bounds))).ToList();

        var retainedInkCount = retained.Sum(c => c.Pixels.Count);
        var removedInkRatio = originalInkCount == 0 ? 0 : 1 - retainedInkCount / (double)originalInkCount;
        var usedOriginalFallback = removedInkRatio > maximumAllowedInkLoss;
        if (usedOriginalFallback) retained = components;

        var cleaned = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(cleaned)) graphics.Clear(Color.White);
        foreach (var component in retained)
            foreach (var point in component.Pixels) cleaned.SetPixel(point.X, point.Y, Color.Black);

        var ink = retained.Count == 0 ? Rectangle.Empty : retained.Select(c => c.Bounds).Aggregate(Rectangle.Union);
        var tightCropped = CropTight(cleaned, ink);
        var normalized = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(normalized))
        {
            graphics.Clear(Color.White);
            if (!ink.IsEmpty)
            {
                const int horizontalPadding = 10;
                const int verticalPadding = 10;
                var scale = Math.Min((width - horizontalPadding * 2) / (double)tightCropped.Width,
                    (height - verticalPadding * 2) / (double)tightCropped.Height);
                var targetWidth = Math.Max(1, (int)Math.Round(tightCropped.Width * scale));
                var targetHeight = Math.Max(1, (int)Math.Round(tightCropped.Height * scale));
                var target = new Rectangle((width - targetWidth) / 2, (height - targetHeight) / 2, targetWidth, targetHeight);
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.PixelOffsetMode = PixelOffsetMode.Half;
                graphics.DrawImage(tightCropped, target, new Rectangle(Point.Empty, tightCropped.Size), GraphicsUnit.Pixel);
            }
        }

        return new NormalizedCharacter
        {
            Cleaned = cleaned,
            TightCropped = tightCropped,
            Normalized = normalized,
            InkBounds = ink,
            IsValid = !ink.IsEmpty && retained.Sum(c => c.Pixels.Count) >= minimumArea,
            UsedOriginalFallback = usedOriginalFallback,
            RemovedInkRatio = removedInkRatio
        };
    }

    private static Bitmap CropTight(Bitmap source, Rectangle bounds)
    {
        if (bounds.IsEmpty) return new Bitmap(1, 1, PixelFormat.Format24bppRgb);
        var output = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(output);
        graphics.Clear(Color.White);
        graphics.DrawImage(source, new Rectangle(Point.Empty, output.Size), bounds, GraphicsUnit.Pixel);
        return output;
    }

    private static Rectangle Expanded(Rectangle value, int amount)
    {
        value.Inflate(amount, amount);
        return value;
    }

    private static List<Component> FindComponents(Bitmap source)
    {
        var visited = new bool[source.Width, source.Height];
        var output = new List<Component>();
        for (var y = 0; y < source.Height; y++)
        for (var x = 0; x < source.Width; x++)
        {
            if (visited[x, y] || source.GetPixel(x, y).R >= 128) continue;
            var queue = new Queue<Point>();
            var pixels = new List<Point>();
            queue.Enqueue(new Point(x, y)); visited[x, y] = true;
            while (queue.Count > 0)
            {
                var point = queue.Dequeue(); pixels.Add(point);
                for (var yy = -1; yy <= 1; yy++)
                for (var xx = -1; xx <= 1; xx++)
                {
                    var nx = point.X + xx; var ny = point.Y + yy;
                    if ((xx == 0 && yy == 0) || nx < 0 || ny < 0 || nx >= source.Width || ny >= source.Height || visited[nx, ny]) continue;
                    visited[nx, ny] = true;
                    if (source.GetPixel(nx, ny).R < 128) queue.Enqueue(new Point(nx, ny));
                }
            }
            var bounds = Rectangle.FromLTRB(pixels.Min(p => p.X), pixels.Min(p => p.Y),
                pixels.Max(p => p.X) + 1, pixels.Max(p => p.Y) + 1);
            output.Add(new Component(pixels, bounds,
                bounds.Left == 0 || bounds.Top == 0 || bounds.Right == source.Width || bounds.Bottom == source.Height));
        }
        return output;
    }

    private sealed record Component(List<Point> Pixels, Rectangle Bounds, bool TouchesEdge);
}
