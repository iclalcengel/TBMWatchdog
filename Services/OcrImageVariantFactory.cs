using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace TBMWatchdog.Services;

public sealed record TorqueGlyphGeometry(char? SuggestedDigit, double Confidence, string Reason);

public sealed class OcrImageVariant : IDisposable
{
    public OcrImageVariant(string method, Bitmap processedImage, Bitmap binaryImage, bool leadingNarrowDigit = false,
        IReadOnlyList<TorqueGlyphGeometry>? torqueGlyphs = null)
    { Method = method; ProcessedImage = processedImage; Image = binaryImage; LeadingNarrowDigit = leadingNarrowDigit; TorqueGlyphs = torqueGlyphs ?? Array.Empty<TorqueGlyphGeometry>(); }
    public string Method { get; }
    public Bitmap ProcessedImage { get; }
    public Bitmap Image { get; }
    public bool LeadingNarrowDigit { get; }
    public IReadOnlyList<TorqueGlyphGeometry> TorqueGlyphs { get; }
    public void Dispose() { ProcessedImage.Dispose(); Image.Dispose(); }
}

public static class OcrImageVariantFactory
{
    public const int ScaleFactor = 8;

    public static IReadOnlyList<OcrImageVariant> Create(Bitmap source, Models.OcrProfile? profile = null,
        Models.OcrFieldKind field = Models.OcrFieldKind.General)
    {
        ArgumentNullException.ThrowIfNull(source);
        profile ??= new Models.OcrProfile();
        using var nearest = Resize(source, InterpolationMode.NearestNeighbor);
        using var bicubic = Resize(source, InterpolationMode.HighQualityBicubic);
        using var grayNearest = ImagePreprocessor.Grayscale(nearest);
        using var grayBicubic = ImagePreprocessor.Grayscale(bicubic);
        using var contrast = ImagePreprocessor.AutoContrast(grayNearest);
        using var gaussian = profile.EnableGaussianBlur ? ImagePreprocessor.GaussianBlur(contrast) : new Bitmap(contrast);
        using var gaussianMedian = profile.EnableMedianFilter ? ImagePreprocessor.MedianFilter(gaussian) : new Bitmap(gaussian);
        using var baseBinary = BinaryImageBuilder.Otsu(contrast);
        using var sequentialBinary = BinaryImageBuilder.Otsu(gaussianMedian);
        using var opened = profile.EnableMorphology ? ImagePreprocessor.MorphologyOpen(sequentialBinary) : new Bitmap(sequentialBinary);
        using var openedClosed = profile.EnableMorphology ? ImagePreprocessor.MorphologyClose(opened) : new Bitmap(opened);

        var variants = new List<OcrImageVariant>
        {
            New("Nearest + Otsu", grayNearest, BinaryImageBuilder.Otsu(grayNearest)),
            New("Bicubic + Otsu", grayBicubic, BinaryImageBuilder.Otsu(grayBicubic)),
            New("Otomatik Kontrast + Otsu", contrast, new Bitmap(baseBinary)),
            New("Gaussian + Median + Otsu", gaussianMedian, new Bitmap(sequentialBinary)),
            New("Open + Close", gaussianMedian, new Bitmap(openedClosed))
        };
        // Küçük ondalık ayıracı threshold sırasında kaybolabildiği için gri
        // bicubic görüntüyü de doğrudan OCR adayı olarak koru.
        if (IsDecimalField(field))
            variants.Insert(0, New("Bicubic Gri (Ondalık)", grayBicubic, new Bitmap(grayBicubic)));
        if (field == Models.OcrFieldKind.Gate)
        {
            using var lineCleaned = ImagePreprocessor.RemoveHorizontalLines(sequentialBinary);
            using var repaired = profile.EnableMorphology ? ImagePreprocessor.MorphologyClose(lineCleaned) : new Bitmap(lineCleaned);
            variants.Add(New("Kapak Yatay Çizgi Temizleme", gaussianMedian, new Bitmap(repaired)));
        }
        else
            variants.Add(New("Adaptive Threshold", contrast,
                BinaryImageBuilder.Adaptive(contrast, profile.AdaptiveThresholdRadius, profile.AdaptiveThresholdBias)));
        return variants;
    }

    // The live list is deliberately ordered. Field diagnostics collected in the
    // application consistently favour the contrast/denoise Otsu input, while
    // nearest-neighbour preserves thin HMI strokes as the safest second choice.
    public static IReadOnlyList<OcrImageVariant> CreateLiveFast(Bitmap source, Models.OcrProfile? profile = null,
        Models.OcrFieldKind field = Models.OcrFieldKind.General)
    {
        ArgumentNullException.ThrowIfNull(source);
        profile ??= new Models.OcrProfile();
        using var nearest = Resize(source, InterpolationMode.NearestNeighbor);
        using var gray = ImagePreprocessor.Grayscale(nearest);
        using var contrast = ImagePreprocessor.AutoContrast(gray);
        using var gaussian = profile.EnableGaussianBlur ? ImagePreprocessor.GaussianBlur(contrast) : new Bitmap(contrast);
        using var denoised = profile.EnableMedianFilter ? ImagePreprocessor.MedianFilter(gaussian) : new Bitmap(gaussian);
        var variants = new List<OcrImageVariant>
        {
            New("Gaussian + Median + Otsu", denoised, BinaryImageBuilder.Otsu(denoised)),
            New("Nearest + Otsu", gray, BinaryImageBuilder.Otsu(gray)),
            New("Otomatik Kontrast + Otsu", contrast, BinaryImageBuilder.Otsu(contrast))
        };
        if (IsDecimalField(field))
        {
            using var bicubic = Resize(source, InterpolationMode.HighQualityBicubic);
            using var bicubicGray = ImagePreprocessor.Grayscale(bicubic);
            variants.Insert(0, New("Bicubic Gri (Ondalık)", bicubicGray, new Bitmap(bicubicGray)));
        }
        return variants;
    }

    private static bool IsDecimalField(Models.OcrFieldKind field) => field is
        Models.OcrFieldKind.StableRpm or Models.OcrFieldKind.StableTorque
        or Models.OcrFieldKind.StableAdvanceSpeed or Models.OcrFieldKind.StablePenetration
        or Models.OcrFieldKind.NominalInjection or Models.OcrFieldKind.RealInjection
        or Models.OcrFieldKind.Epb1 or Models.OcrFieldKind.Epb2 or Models.OcrFieldKind.Epb3
        or Models.OcrFieldKind.Epb4 or Models.OcrFieldKind.Epb5;

    public static OcrImageVariant CreateThrustTargetedFallback(Bitmap source)
    {
        ArgumentNullException.ThrowIfNull(source);
        using var bicubic = Resize(source, InterpolationMode.HighQualityBicubic);
        using var gray = ImagePreprocessor.Grayscale(bicubic);
        return New("Bicubic + Otsu", gray, BinaryImageBuilder.Otsu(gray));
    }

    public static OcrImageVariant CreateAdvanceTargetedFallback(Bitmap source)
    {
        ArgumentNullException.ThrowIfNull(source);
        using var bicubic = Resize(source, InterpolationMode.HighQualityBicubic);
        using var gray = ImagePreprocessor.Grayscale(bicubic);
        return New("Bicubic + Otsu", gray, BinaryImageBuilder.Otsu(gray));
    }

    public static OcrImageVariant CreateTorqueColorPreservingFallback(Bitmap source)
    {
        ArgumentNullException.ThrowIfNull(source);
        using var mask = ImagePreprocessor.WarmDigitMask(source);
        using var enlarged = Resize(mask, InterpolationMode.NearestNeighbor);
        return New("Torque Warm Digit Mask", enlarged, new Bitmap(enlarged));
    }

    public static OcrImageVariant CreateTorqueTightColorPreservingFallback(Bitmap source)
    {
        ArgumentNullException.ThrowIfNull(source);
        using var mask = ImagePreprocessor.WarmDigitMask(source);
        using var cropped = CropTorqueNumber(mask, out var leadingNarrowDigit, out var glyphs);
        using var enlarged = Resize(cropped, InterpolationMode.NearestNeighbor);
        return new OcrImageVariant("Torque colour-preserving fallback (tight)", new Bitmap(enlarged), new Bitmap(enlarged), leadingNarrowDigit, glyphs);
    }

    private static Bitmap CropTorqueNumber(Bitmap mask, out bool leadingNarrowDigit,
        out IReadOnlyList<TorqueGlyphGeometry> torqueGlyphs)
    {
        leadingNarrowDigit = false;
        torqueGlyphs = Array.Empty<TorqueGlyphGeometry>();
        var columns = new bool[mask.Width];
        for (var x = 0; x < mask.Width; x++)
            for (var y = 0; y < mask.Height; y++)
                if (mask.GetPixel(x, y).R < 128) { columns[x] = true; break; }
        var runs = new List<(int Start, int End)>();
        for (var x = 0; x < columns.Length; x++)
        {
            if (!columns[x]) continue;
            var start = x;
            while (x + 1 < columns.Length && columns[x + 1]) x++;
            runs.Add((start, x));
        }
        if (runs.Count == 0) return new Bitmap(mask);
        var left = runs[0].Start; var right = runs[^1].End;
        var normalGap = Math.Max(2, mask.Height / 5);
        for (var i = 1; i < runs.Count; i++)
        {
            var gap = runs[i].Start - runs[i - 1].End - 1;
            if (gap > normalGap && runs[i - 1].End - left >= mask.Height)
            { right = runs[i - 1].End; break; }
        }
        var top = mask.Height; var bottom = -1;
        for (var y = 0; y < mask.Height; y++)
            for (var x = left; x <= right; x++)
                if (mask.GetPixel(x, y).R < 128) { top = Math.Min(top, y); bottom = Math.Max(bottom, y); }
        if (bottom < top) return new Bitmap(mask);
        var glyphHeight = bottom - top + 1;
        torqueGlyphs = AnalyzeTorqueGlyphs(mask, runs, top, bottom);
        var firstRunWidth = runs[0].End - runs[0].Start + 1;
        // HMI fontundaki 1 rakamının tabanı gövdesinden geniştir. Bu yüzden yalnız
        // toplam en/boy oranı 1'i 4 sanabiliyordu. İlk glifi ayrıca inceleyip orta
        // yatay çizgi ile baskın dikey gövdeyi birlikte kullanıyoruz.
        using (var firstGlyph = new Bitmap(firstRunWidth + 4, glyphHeight + 4, PixelFormat.Format24bppRgb))
        {
            using var glyphGraphics = Graphics.FromImage(firstGlyph);
            glyphGraphics.Clear(Color.White);
            glyphGraphics.DrawImage(mask,
                new Rectangle(2, 2, firstRunWidth, glyphHeight),
                new Rectangle(runs[0].Start, top, firstRunWidth, glyphHeight), GraphicsUnit.Pixel);
            var geometry = GateDigitGeometryAnalyzer.Analyze(firstGlyph);
            var upperLeft = firstGlyph.Width; var upperRight = -1;
            var upperBottom = 2 + Math.Max(1, glyphHeight * 55 / 100);
            for (var y = 2; y < Math.Min(firstGlyph.Height, upperBottom); y++)
                for (var x = 2; x < firstGlyph.Width - 2; x++)
                    if (firstGlyph.GetPixel(x, y).R < 128)
                    { upperLeft = Math.Min(upperLeft, x); upperRight = Math.Max(upperRight, x); }
            var upperInkWidth = upperRight >= upperLeft ? upperRight - upperLeft + 1 : firstRunWidth;
            leadingNarrowDigit = geometry.SuggestedDigit == '1'
                || (geometry.HasDominantVertical && !geometry.HasStrongMiddleHorizontal
                    && firstRunWidth <= Math.Max(3, glyphHeight * 0.72))
                // 1031 fontundaki 1'in geniş tabanı orta-yatay testini tetikleyebilir;
                // üst yarı ise hâlâ belirgin biçimde dar ve dikeydir. 4'ün üst yarısı
                // diyagonal + sağ gövde nedeniyle bu sınırı aşar.
                || (geometry.HasDominantVertical
                    && upperInkWidth <= Math.Max(2, glyphHeight * 0.52)
                    && firstRunWidth <= Math.Max(3, glyphHeight * 0.78));
        }
        const int padding = 4;
        var width = right - left + 1; var height = bottom - top + 1;
        var output = new Bitmap(width + padding * 2, height + padding * 2, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(output);
        graphics.Clear(Color.White);
        graphics.DrawImage(mask, new Rectangle(padding, padding, width, height),
            new Rectangle(left, top, width, height), GraphicsUnit.Pixel);
        return output;
    }

    private static IReadOnlyList<TorqueGlyphGeometry> AnalyzeTorqueGlyphs(Bitmap mask,
        IReadOnlyList<(int Start, int End)> runs, int top, int bottom)
    {
        var glyphHeight = bottom - top + 1;
        var digitRuns = runs.Where(run => run.End - run.Start + 1 >= 2)
            .Where(run => HasTallInk(mask, run.Start, run.End, top, bottom, glyphHeight))
            .Take(4).ToArray();
        var result = new List<TorqueGlyphGeometry>(digitRuns.Length);
        foreach (var run in digitRuns)
        {
            var width = run.End - run.Start + 1;
            using var glyph = new Bitmap(width + 4, glyphHeight + 4, PixelFormat.Format24bppRgb);
            using (var graphics = Graphics.FromImage(glyph))
            {
                graphics.Clear(Color.White);
                graphics.DrawImage(mask, new Rectangle(2, 2, width, glyphHeight),
                    new Rectangle(run.Start, top, width, glyphHeight), GraphicsUnit.Pixel);
            }
            var geometry = GateDigitGeometryAnalyzer.Analyze(glyph);
            var holes = FindWhiteHoles(glyph);
            var aspect = width / (double)Math.Max(1, glyphHeight);
            if (geometry.SuggestedDigit == '1' || (aspect < .48 && geometry.HasDominantVertical))
                result.Add(new TorqueGlyphGeometry('1', .96, "Dar baskın dikey glif"));
            else if (holes.Count >= 2)
                result.Add(new TorqueGlyphGeometry('8', .94, "İki kapalı iç boşluk"));
            else if (holes.Count == 0 && HasFiveStrokePattern(glyph))
                result.Add(new TorqueGlyphGeometry('5', .91, "Üst-sol ve alt-sağ 5 geometrisi"));
            else if (holes.Count == 1 && IsCenteredTallHole(holes[0], glyph.Height))
                result.Add(new TorqueGlyphGeometry('0', .93, "Merkezde uzun kapalı boşluk"));
            else if (holes.Count == 1 && holes[0].Top > glyph.Height * .34)
                result.Add(new TorqueGlyphGeometry('6', .88, "Alt bölgede kapalı boşluk"));
            else
                result.Add(new TorqueGlyphGeometry(null, 0, "Ayırt edici geometri yok"));
        }
        return result;
    }

    private static bool HasTallInk(Bitmap image, int left, int right, int top, int bottom, int totalHeight)
    {
        var minY = bottom; var maxY = top;
        for (var y = top; y <= bottom; y++)
            for (var x = left; x <= right; x++)
                if (image.GetPixel(x, y).R < 128) { minY = Math.Min(minY, y); maxY = Math.Max(maxY, y); }
        return maxY - minY + 1 >= totalHeight * .55;
    }

    private static List<Rectangle> FindWhiteHoles(Bitmap glyph)
    {
        var visited = new bool[glyph.Width, glyph.Height];
        var holes = new List<Rectangle>();
        for (var sy = 0; sy < glyph.Height; sy++) for (var sx = 0; sx < glyph.Width; sx++)
        {
            if (visited[sx, sy] || glyph.GetPixel(sx, sy).R < 128) continue;
            var queue = new Queue<Point>(); queue.Enqueue(new Point(sx, sy)); visited[sx, sy] = true;
            var touchesEdge = false; var minX = sx; var maxX = sx; var minY = sy; var maxY = sy; var area = 0;
            while (queue.Count > 0)
            {
                var p = queue.Dequeue(); area++; minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X); minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
                if (p.X == 0 || p.Y == 0 || p.X == glyph.Width - 1 || p.Y == glyph.Height - 1) touchesEdge = true;
                foreach (var n in new[] { new Point(p.X - 1, p.Y), new Point(p.X + 1, p.Y), new Point(p.X, p.Y - 1), new Point(p.X, p.Y + 1) })
                    if (n.X >= 0 && n.Y >= 0 && n.X < glyph.Width && n.Y < glyph.Height && !visited[n.X, n.Y] && glyph.GetPixel(n.X, n.Y).R >= 128)
                    { visited[n.X, n.Y] = true; queue.Enqueue(n); }
            }
            if (!touchesEdge && area >= 3) holes.Add(Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1));
        }
        return holes;
    }

    private static bool IsCenteredTallHole(Rectangle hole, int height)
    {
        // 1031 HMI fontunda 0'in iç boşluğu antialiasing/threshold nedeniyle
        // birkaç piksel aşağı kayabiliyor. Eski Top<=%35 kuralı bu temiz 0'ları
        // 6 olarak sınıflandırıyordu (0.631 -> 6.631). Yalnız uzun, orta
        // hattı kesen ve merkezi alt bölgeye sarkmayan kapalı boşluğu 0 say.
        var centerY = hole.Top + hole.Height / 2d;
        return hole.Height >= height * .28
            && hole.Top <= height * .43
            && hole.Bottom >= height * .62
            && centerY >= height * .38
            && centerY <= height * .58;
    }

    private static bool HasFiveStrokePattern(Bitmap glyph)
    {
        var w = glyph.Width; var h = glyph.Height;
        int Count(int x0, int x1, int y0, int y1)
        { var count = 0; for (var y = y0; y < y1; y++) for (var x = x0; x < x1; x++) if (glyph.GetPixel(x, y).R < 128) count++; return count; }
        var upperLeft = Count(0, Math.Max(1, w / 2), 0, Math.Max(1, h / 2));
        var upperRight = Count(w / 2, w, 0, Math.Max(1, h / 2));
        var lowerLeft = Count(0, Math.Max(1, w / 2), h / 2, h);
        var lowerRight = Count(w / 2, w, h / 2, h);
        return upperLeft > upperRight * 1.12 && lowerRight > lowerLeft * 1.05;
    }

    private static OcrImageVariant New(string method, Bitmap processed, Bitmap binary) => new(method, new Bitmap(processed), binary);

    private static Bitmap Resize(Bitmap source, InterpolationMode interpolation)
    {
        var memorySafeScale = Math.Max(1, Math.Min(ScaleFactor,
            Math.Min(1600 / Math.Max(1, source.Width), 800 / Math.Max(1, source.Height))));
        var target = new Bitmap(Math.Max(1, source.Width * memorySafeScale), Math.Max(1, source.Height * memorySafeScale), PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(target);
        graphics.Clear(Color.White); graphics.InterpolationMode = interpolation;
        graphics.PixelOffsetMode = interpolation == InterpolationMode.NearestNeighbor ? PixelOffsetMode.Half : PixelOffsetMode.HighQuality;
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.DrawImage(source, new Rectangle(0, 0, target.Width, target.Height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel);
        return target;
    }
}
