using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public enum DigitTemplateQualityStatus { Good, Acceptable, Poor, Rejected }
public sealed record DigitTemplateQualityResult(double QualityScore, DigitTemplateQualityStatus Status,
    string Reason, double ForegroundRatio, double BoundingBoxFillRatio, double AspectRatio,
    bool TouchesEdge, double InkLoss, int ComponentCount);

public static class DigitTemplateQualityEvaluator
{
    public static DigitTemplateQualityResult Evaluate(Bitmap image, double inkLoss = 0)
    {
        ArgumentNullException.ThrowIfNull(image);
        var points = new List<Point>();
        for (var y = 0; y < image.Height; y++) for (var x = 0; x < image.Width; x++)
            if (image.GetPixel(x, y).R < 128) points.Add(new Point(x, y));
        if (points.Count == 0) return new(0, DigitTemplateQualityStatus.Rejected,
            "Görüntü boş veya neredeyse boş.", 0, 0, 0, false, inkLoss, 0);
        var foreground = points.Count / (double)Math.Max(1, image.Width * image.Height);
        var bounds = Rectangle.FromLTRB(points.Min(p => p.X), points.Min(p => p.Y), points.Max(p => p.X) + 1, points.Max(p => p.Y) + 1);
        var fill = points.Count / (double)Math.Max(1, bounds.Width * bounds.Height);
        var aspect = bounds.Width / (double)Math.Max(1, bounds.Height);
        var touches = bounds.Left <= 0 || bounds.Top <= 0 || bounds.Right >= image.Width || bounds.Bottom >= image.Height;
        var components = CountComponents(image);
        var reasons = new List<string>();
        var score = 1d;
        if (foreground is < 0.025 or > 0.60) { score -= .55; reasons.Add("Foreground oranı geçersiz."); }
        else if (foreground is < 0.045 or > 0.45) { score -= .16; reasons.Add("Foreground oranı sınırda."); }
        if (fill < .12) { score -= .30; reasons.Add("Karakter sınır kutusu çok seyrek."); }
        if (aspect is < .08 or > 1.15) { score -= .28; reasons.Add("En/boy oranı rakam geometrisine uymuyor."); }
        if (touches) { score -= .38; reasons.Add("Karakterin önemli kısmı kırpılmış veya kenara temas ediyor olabilir."); }
        if (bounds.Height < image.Height * .35 || bounds.Width < 2 || bounds.Width > image.Width * .90)
        { score -= .40; reasons.Add("Karakter bileşeni aşırı küçük veya büyüktür."); }
        if (components != 1) { score -= .65; reasons.Add("Tek segment içinde birden fazla karakter/bileşen bulundu."); }
        if (inkLoss > .25) { score -= Math.Min(.45, inkLoss); reasons.Add($"Mürekkep kaybı yüksek (%{inkLoss * 100:0})."); }
        score = Math.Clamp(score, 0, 1);
        var rejected = components != 1 || foreground is < .012 or > .72 || bounds.Height < image.Height * .20;
        var status = rejected ? DigitTemplateQualityStatus.Rejected : score >= .85 ? DigitTemplateQualityStatus.Good
            : score >= .65 ? DigitTemplateQualityStatus.Acceptable : score >= .40 ? DigitTemplateQualityStatus.Poor
            : DigitTemplateQualityStatus.Rejected;
        return new(score, status, reasons.Count == 0 ? "Temiz ve bütün tek rakam örneği." : string.Join(" ", reasons),
            foreground, fill, aspect, touches, inkLoss, components);
    }

    private static int CountComponents(Bitmap image)
    {
        var visited = new bool[image.Width, image.Height]; var count = 0;
        for (var y = 0; y < image.Height; y++) for (var x = 0; x < image.Width; x++)
        {
            if (visited[x, y] || image.GetPixel(x, y).R >= 128) continue;
            var queue = new Queue<Point>(); var pixels = 0; queue.Enqueue(new Point(x, y)); visited[x, y] = true;
            while (queue.Count > 0)
            {
                var p = queue.Dequeue(); pixels++;
                for (var yy = -1; yy <= 1; yy++) for (var xx = -1; xx <= 1; xx++)
                {
                    var nx = p.X + xx; var ny = p.Y + yy;
                    if (nx < 0 || ny < 0 || nx >= image.Width || ny >= image.Height || visited[nx, ny]) continue;
                    visited[nx, ny] = true; if (image.GetPixel(nx, ny).R < 128) queue.Enqueue(new(nx, ny));
                }
            }
            if (pixels >= 6) count++;
        }
        return count;
    }
}
