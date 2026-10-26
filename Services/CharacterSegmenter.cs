using System.Drawing.Imaging;

namespace TBMWatchdog.Services;

public sealed record CharacterSegmentationOptions(bool ProjectionSplitEnabled = false,
    int ExpectedDigitCountMin = 1, int ExpectedDigitCountMax = 8,
    double MergedSegmentWidthRatio = 1.45, double ProjectionValleyThreshold = 0.35)
{
    public static CharacterSegmentationOptions For(Models.OcrProfile profile, Models.OcrFieldKind field)
    {
        var id = field == Models.OcrFieldKind.StableThrustForce ? Models.BuiltInFieldIds.StableThrustForce : null;
        var definition = id is null ? null : profile.FindField(id);
        return definition is null ? new(field == Models.OcrFieldKind.StableThrustForce, 4, 5, 1.10, 0.38)
            : new(definition.ProjectionSplitEnabled, definition.ExpectedDigitCountMin, definition.ExpectedDigitCountMax,
                definition.MergedSegmentWidthRatio, definition.ProjectionValleyThreshold);
    }
}

public sealed class CharacterSegmentationResult : IDisposable
{
    public required IReadOnlyList<Bitmap> Characters { get; init; }
    public required int RawSegmentCount { get; init; }
    public required IReadOnlyList<Rectangle> RejectedBounds { get; init; }
    public required IReadOnlyList<Rectangle> AcceptedBounds { get; init; }
    public IReadOnlySet<int> DecimalSeparatorIndexes { get; init; } = new HashSet<int>();
    public required Bitmap Visualization { get; init; }
    public Bitmap? SegmentCrop { get; init; }
    public Rectangle? UnionBounds { get; init; }
    public required double Integrity { get; init; }
    public int DetectedComponentCount { get; init; }
    public int MergedComponentCount { get; init; }
    public int SplitAttemptCount { get; init; }
    public int ProjectionSplitCount { get; init; }
    public IReadOnlyList<int> ProjectionValleys { get; init; } = Array.Empty<int>();
    public IReadOnlyList<string> SegmentSources { get; init; } = Array.Empty<string>();
    public bool HasUnresolvedMergedComponents { get; init; }
    public bool SegmentationFallbackUsed { get; init; }
    public TimeSpan ProcessingElapsed { get; init; }
    public bool IsSuccessful => Characters.Count > 0;
    public bool IsReliable => Characters.Count > 0 && Integrity >= 0.60;
    public void Dispose() { foreach (var image in Characters) image.Dispose(); Visualization.Dispose(); SegmentCrop?.Dispose(); }
}

public static class CharacterSegmenter
{
    public static CharacterSegmentationResult Segment(Bitmap binary, Models.OcrFieldKind field = Models.OcrFieldKind.General,
        CharacterSegmentationOptions? options = null)
    {
        var processingWatch = System.Diagnostics.Stopwatch.StartNew();
        options ??= new CharacterSegmentationOptions(field == Models.OcrFieldKind.StableThrustForce, 4, 5, 1.10, 0.38);
        var visited = new bool[binary.Width, binary.Height];
        var components = new List<Rectangle>();
        var separatorCandidates = new List<Rectangle>();
        var decimalField = IsDecimalField(field);
        for (var y = 0; y < binary.Height; y++) for (var x = 0; x < binary.Width; x++)
        {
            if (visited[x, y] || binary.GetPixel(x, y).R >= 128) continue;
            var points = Flood(binary, x, y, visited);
            var left = points.Min(p => p.X); var right = points.Max(p => p.X);
            var top = points.Min(p => p.Y); var bottom = points.Max(p => p.Y);
            var bounds = Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
            if (bounds.Height >= binary.Height * 0.28 && bounds.Width >= 2 && points.Count >= 12
                && (options.ProjectionSplitEnabled || bounds.Width < binary.Width * 0.8))
                components.Add(bounds);
            else if (decimalField && points.Count >= 4 && bounds.Width >= 2 && bounds.Height >= 2
                && bounds.Width <= binary.Width * 0.15 && bounds.Height <= binary.Height * 0.24
                && bounds.Bottom >= binary.Height * 0.55)
                separatorCandidates.Add(bounds);
        }

        components = MergeNearby(components.OrderBy(r => r.Left).ToList(), binary.Height);
        var detectedComponentCount = components.Count;
        if (field is Models.OcrFieldKind.Scale1 or Models.OcrFieldKind.Scale2 && components.Count > 1)
        {
            var medianHeight = Median(components.Select(component => (double)component.Height));
            components = components.Where(component => component.Height >= medianHeight * 0.70).ToList();
        }
        var rawSegmentCount = components.Count + separatorCandidates.Count;
        var rejected = field == Models.OcrFieldKind.Gate
            ? FindGateFrameComponents(binary, components)
            : new HashSet<Rectangle>();
        components = components.Where(component => !rejected.Contains(component)).OrderBy(component => component.Left).ToList();
        var mergedCount = 0; var splitAttempts = 0; var splitCount = 0; var unresolvedMerged = false;
        var projectionValleys = new List<int>(); var segmentSources = new List<string>();
        if (options.ProjectionSplitEnabled)
        {
            var expanded = new List<Rectangle>();
            foreach (var component in components)
            {
                var ratio = component.Width / (double)Math.Max(1, component.Height);
                var edgeMargin = Math.Max(2, binary.Width / 40);
                var separateFrame = component.Left <= edgeMargin && component.Right >= binary.Width - edgeMargin
                    && ratio >= 2.5 && InkDensity(binary, component) < 0.16;
                if (separateFrame) { rejected.Add(component); continue; }
                if (ratio < options.MergedSegmentWidthRatio)
                {
                    expanded.Add(component); segmentSources.Add("ConnectedComponent"); continue;
                }
                mergedCount++; splitAttempts++;
                var split = SplitByVerticalProjection(binary, component, options, projectionValleys);
                if (split.Count >= 2)
                {
                    expanded.AddRange(split); splitCount += split.Count - 1;
                    segmentSources.AddRange(Enumerable.Repeat("ProjectionValley", split.Count));
                }
                else
                {
                    unresolvedMerged = true; rejected.Add(component);
                }
            }
            components = expanded.OrderBy(component => component.Left).ToList();
        }
        var decimalSeparators = decimalField ? FindDecimalSeparators(components, separatorCandidates, binary) : [];
        components.AddRange(decimalSeparators);
        components = components.OrderBy(component => component.Left).ToList();
        var separatorIndexes = components.Select((bounds, index) => (bounds, index))
            .Where(item => decimalSeparators.Contains(item.bounds)).Select(item => item.index).ToHashSet();
        var characters = components.Select(rectangle => CropWithBorder(binary, rectangle, 8)).ToList();
        var visualization = new Bitmap(binary);
        using (var graphics = Graphics.FromImage(visualization))
        using (var acceptedPen = new Pen(Color.LimeGreen, 2))
        using (var rejectedPen = new Pen(Color.OrangeRed, 2))
        using (var labelFont = new Font("Segoe UI", Math.Max(7, binary.Height / 18f), FontStyle.Bold))
        {
            foreach (var rectangle in components) graphics.DrawRectangle(acceptedPen, rectangle);
            foreach (var rectangle in rejected)
            {
                graphics.DrawRectangle(rejectedPen, rectangle);
                var label = "Çerçeve/kenar bileşeni olarak reddedildi.";
                var size = graphics.MeasureString(label, labelFont);
                var labelX = Math.Clamp(rectangle.Left, 0, Math.Max(0, binary.Width - (int)size.Width));
                var labelY = Math.Clamp(rectangle.Top, 0, Math.Max(0, binary.Height - (int)size.Height));
                using var background = new SolidBrush(Color.FromArgb(210, 15, 23, 42));
                graphics.FillRectangle(background, labelX, labelY, size.Width, size.Height);
                graphics.DrawString(label, labelFont, Brushes.OrangeRed, labelX, labelY);
            }
        }
        var heights = components.Where((_, index) => !separatorIndexes.Contains(index)).Select(r => (double)r.Height).ToArray();
        var integrity = heights.Length == 0 ? 0 : Math.Clamp(1 - ((heights.Max() - heights.Min()) / Math.Max(1, heights.Max())), 0, 1);
        Rectangle? union = components.Count == 0 ? null : components.Aggregate(Rectangle.Union);
        var segmentCrop = union.HasValue ? CropWithBorder(binary, union.Value, 8) : null;
        processingWatch.Stop();
        return new CharacterSegmentationResult
        {
            Characters = characters, RawSegmentCount = rawSegmentCount, RejectedBounds = rejected.ToArray(),
            AcceptedBounds = components.ToArray(),
            DecimalSeparatorIndexes = separatorIndexes,
            Visualization = visualization, Integrity = integrity,
            UnionBounds = union, SegmentCrop = segmentCrop,
            DetectedComponentCount = detectedComponentCount, MergedComponentCount = mergedCount,
            SplitAttemptCount = splitAttempts, ProjectionSplitCount = splitCount,
            ProjectionValleys = projectionValleys, SegmentSources = segmentSources,
            HasUnresolvedMergedComponents = unresolvedMerged, SegmentationFallbackUsed = false,
            ProcessingElapsed = processingWatch.Elapsed
        };
    }

    private static List<Rectangle> SplitByVerticalProjection(Bitmap image, Rectangle component,
        CharacterSegmentationOptions options, List<int> reportedValleys)
    {
        if (component.Width < 6 || component.Height < 4) return [];
        var projection = new int[component.Width];
        for (var x = 0; x < component.Width; x++)
        for (var y = component.Top; y < component.Bottom; y++)
            if (image.GetPixel(component.Left + x, y).R < 128) projection[x]++;
        var smooth = new double[projection.Length];
        for (var x = 0; x < projection.Length; x++)
        {
            var from = Math.Max(0, x - 1); var to = Math.Min(projection.Length - 1, x + 1);
            smooth[x] = Enumerable.Range(from, to - from + 1).Average(index => projection[index]);
        }

        var estimatedDigitWidth = Math.Max(3d, component.Height * 0.65);
        var estimatedCount = Math.Clamp((int)Math.Round(component.Width / estimatedDigitWidth), 2,
            Math.Max(2, options.ExpectedDigitCountMax));
        var counts = Enumerable.Range(2, Math.Max(1, options.ExpectedDigitCountMax - 1))
            .OrderBy(count => Math.Abs(count - estimatedCount)).ThenByDescending(count => count);
        foreach (var count in counts)
        {
            var attempt = TryCount(count);
            if (attempt.Segments.Count < 2) continue;
            reportedValleys.AddRange(attempt.Cuts.Select(cut => component.Left + cut));
            return attempt.Segments;
        }
        return [];

        (List<Rectangle> Segments, List<int> Cuts) TryCount(int count)
        {
            var averageWidth = component.Width / (double)count;
            var cuts = new List<int>(); var previous = 0;
            for (var index = 1; index < count; index++)
            {
                var ideal = index * averageWidth;
                var radius = Math.Max(2, (int)Math.Round(averageWidth * 0.30));
                var minX = Math.Max(previous + Math.Max(2, (int)(component.Height * 0.14)), (int)Math.Round(ideal) - radius);
                var maxX = Math.Min(component.Width - Math.Max(2, (int)(component.Height * 0.14)), (int)Math.Round(ideal) + radius);
                if (minX >= maxX) return ([], []);
                var best = Enumerable.Range(minX, maxX - minX + 1)
                    .OrderBy(x => smooth[x] + Math.Abs(x - ideal) * 0.08).First();
                var peakStart = Math.Max(0, best - radius); var peakEnd = Math.Min(smooth.Length - 1, best + radius);
                var localPeak = Math.Max(1d, smooth.Skip(peakStart).Take(peakEnd - peakStart + 1).Max());
                var valleyRatio = smooth[best] / localPeak;
                if (valleyRatio > options.ProjectionValleyThreshold && smooth[best] > component.Height * 0.10) return ([], []);
                cuts.Add(best); previous = best;
            }
            var boundaries = new[] { 0 }.Concat(cuts).Concat(new[] { component.Width }).ToArray();
            var output = new List<Rectangle>();
            for (var index = 0; index < boundaries.Length - 1; index++)
            {
                var left = component.Left + boundaries[index]; var right = component.Left + boundaries[index + 1];
                var tight = TightInkBounds(image, Rectangle.FromLTRB(left, component.Top, right, component.Bottom));
                if (!tight.HasValue || tight.Value.Width < 2 || tight.Value.Height < component.Height * 0.45) return ([], []);
                output.Add(tight.Value);
            }
            return (output, cuts);
        }
    }

    private static Rectangle? TightInkBounds(Bitmap image, Rectangle area)
    {
        var left = area.Right; var right = area.Left - 1; var top = area.Bottom; var bottom = area.Top - 1;
        for (var y = area.Top; y < area.Bottom; y++) for (var x = area.Left; x < area.Right; x++)
        {
            if (image.GetPixel(x, y).R >= 128) continue;
            left = Math.Min(left, x); right = Math.Max(right, x); top = Math.Min(top, y); bottom = Math.Max(bottom, y);
        }
        return right < left || bottom < top ? null : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }

    public static bool IsDecimalField(Models.OcrFieldKind field) => field is Models.OcrFieldKind.StableRpm
        or Models.OcrFieldKind.StableTorque or Models.OcrFieldKind.StableAdvanceSpeed or Models.OcrFieldKind.StablePenetration
        or Models.OcrFieldKind.NominalInjection or Models.OcrFieldKind.RealInjection
        or Models.OcrFieldKind.Epb1 or Models.OcrFieldKind.Epb2 or Models.OcrFieldKind.Epb3 or Models.OcrFieldKind.Epb4 or Models.OcrFieldKind.Epb5;

    private static List<Rectangle> FindDecimalSeparators(IReadOnlyList<Rectangle> digits,
        IReadOnlyList<Rectangle> candidates, Bitmap image)
    {
        if (digits.Count < 2 || candidates.Count == 0) return [];
        var medianHeight = Median(digits.Select(item => (double)item.Height));
        var medianWidth = Median(digits.Select(item => (double)item.Width));
        return candidates.Where(dot =>
        {
            var left = digits.LastOrDefault(digit => digit.Right <= dot.Left + 2);
            var right = digits.FirstOrDefault(digit => digit.Left >= dot.Right - 2);
            if (left == Rectangle.Empty || right == Rectangle.Empty) return false;
            var gap = right.Left - left.Right;
            return dot.Height <= medianHeight * 0.35 && dot.Width <= Math.Max(8, medianWidth * 0.65)
                && dot.Top >= Math.Min(left.Top, right.Top) + medianHeight * 0.55
                && gap <= Math.Max(image.Height * 0.65, medianWidth * 2.5);
        }).OrderByDescending(dot => dot.Bottom).Take(1).ToList();
    }

    private static HashSet<Rectangle> FindGateFrameComponents(Bitmap binary, IReadOnlyList<Rectangle> components)
    {
        var rejected = new HashSet<Rectangle>();
        if (components.Count < 2) return rejected;
        var comparison = components.Where(component => !TouchesHorizontalEdge(component, binary.Width)).ToArray();
        if (comparison.Length == 0) comparison = components.ToArray();
        var medianWidth = Median(comparison.Select(component => (double)component.Width));
        var medianDensity = Median(comparison.Select(component => InkDensity(binary, component)));
        foreach (var component in components)
        {
            if (!TouchesHorizontalEdge(component, binary.Width)) continue;
            var tall = component.Height >= binary.Height * 0.68;
            var unusuallyWide = component.Width >= Math.Max(medianWidth * 1.45, medianWidth + 5);
            var sparseCurve = InkDensity(binary, component) < medianDensity * 0.72;
            var narrowDigitLike = component.Width <= medianWidth * 0.80;
            if (tall && !narrowDigitLike && (unusuallyWide || sparseCurve)) rejected.Add(component);
        }
        return rejected;
    }

    private static bool TouchesHorizontalEdge(Rectangle rectangle, int width)
    {
        var margin = Math.Max(2, (int)Math.Round(width * 0.03));
        return rectangle.Left <= margin || rectangle.Right >= width - margin;
    }

    private static double InkDensity(Bitmap image, Rectangle bounds)
    {
        var ink = 0;
        for (var y = bounds.Top; y < bounds.Bottom; y++)
        for (var x = bounds.Left; x < bounds.Right; x++) if (image.GetPixel(x, y).R < 128) ink++;
        return ink / (double)Math.Max(1, bounds.Width * bounds.Height);
    }

    private static double Median(IEnumerable<double> values)
    {
        var ordered = values.OrderBy(value => value).ToArray();
        if (ordered.Length == 0) return 0;
        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 0 ? (ordered[middle - 1] + ordered[middle]) / 2 : ordered[middle];
    }

    private static List<Point> Flood(Bitmap image, int startX, int startY, bool[,] visited)
    {
        var points = new List<Point>(); var queue = new Queue<Point>(); queue.Enqueue(new Point(startX, startY)); visited[startX, startY] = true;
        while (queue.Count > 0)
        {
            var point = queue.Dequeue(); points.Add(point);
            for (var yy = -1; yy <= 1; yy++) for (var xx = -1; xx <= 1; xx++)
            {
                var x = point.X + xx; var y = point.Y + yy;
                if ((xx == 0 && yy == 0) || x < 0 || y < 0 || x >= image.Width || y >= image.Height || visited[x, y]) continue;
                visited[x, y] = true; if (image.GetPixel(x, y).R < 128) queue.Enqueue(new Point(x, y));
            }
        }
        return points;
    }

    private static List<Rectangle> MergeNearby(List<Rectangle> input, int imageHeight)
    {
        var output = new List<Rectangle>();
        foreach (var current in input)
        {
            if (output.Count > 0 && current.Left - output[^1].Right <= Math.Max(1, imageHeight / 40)
                && Math.Abs(current.Top - output[^1].Top) > imageHeight * 0.2)
                output[^1] = Rectangle.Union(output[^1], current);
            else output.Add(current);
        }
        return output;
    }

    private static Bitmap CropWithBorder(Bitmap source, Rectangle bounds, int border)
    {
        var output = new Bitmap(bounds.Width + border * 2, bounds.Height + border * 2, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(output); graphics.Clear(Color.White);
        graphics.DrawImage(source, new Rectangle(border, border, bounds.Width, bounds.Height), bounds, GraphicsUnit.Pixel);
        return output;
    }
}
