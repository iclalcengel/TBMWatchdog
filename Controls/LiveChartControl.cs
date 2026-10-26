using TBMWatchdog.Models;
using ScottPlot.WinForms;

namespace TBMWatchdog.Controls;

public sealed class LiveChartControl : UserControl
{
    private const int MaximumPoints = 100;
    private readonly FormsPlot _plot = new() { Dock = DockStyle.Fill };
    private readonly Queue<ChartPoint> _points = new(MaximumPoints);
    private readonly ToolTip _toolTip = new()
    {
        AutoPopDelay = 5000,
        InitialDelay = 100,
        ReshowDelay = 50,
        ShowAlways = true
    };
    private readonly List<ChartSeries> _series = [];
    private long _sequence;
    private string? _hoverKey;

    public LiveChartControl()
    {
        BackColor = Color.FromArgb(30, 41, 59);
        Padding = new Padding(8);
        Controls.Add(_plot);
        _plot.MouseMove += Plot_MouseMove;
        _plot.MouseLeave += (_, _) => HideTooltip();
        ConfigurePlot();
        RenderPlot();
    }

    public int MeasurementCount => _points.Count;
    public int SeriesCount => 3;

    public void AddMeasurement(MeasurementRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        _points.Enqueue(new ChartPoint(
            _sequence++,
            record.Timestamp,
            ToDouble(record.Nominal),
            ToDouble(record.Scale1),
            ToDouble(record.Scale2)));
        while (_points.Count > MaximumPoints) _points.Dequeue();
        RenderPlot();
    }

    public void ClearMeasurements()
    {
        _points.Clear();
        _sequence = 0;
        RenderPlot();
    }

    private void ConfigurePlot()
    {
        _plot.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#1E293B");
        _plot.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#0F172A");
        _plot.Plot.Axes.Color(ScottPlot.Color.FromHex("#94A3B8"));
        _plot.Plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#334155");
        _plot.Plot.Axes.Bottom.Label.Text = "Son ölçümler";
        _plot.Plot.Axes.Left.Label.Text = "Değer";
        _plot.Plot.Title("Canlı Ölçüm Eğrileri");
    }

    private void RenderPlot()
    {
        _plot.Plot.Clear();
        _series.Clear();
        ConfigurePlot();
        if (_points.Count > 0)
        {
            var points = _points.ToArray();
            var x = points.Select(point => (double)point.Sequence).ToArray();
            var nominal = _plot.Plot.Add.Scatter(x, points.Select(point => point.Nominal).ToArray());
            nominal.LegendText = "Nominal";
            nominal.Color = ScottPlot.Color.FromHex("#38BDF8");
            nominal.LineWidth = 2.2f;
            nominal.MarkerSize = 2;
            _series.Add(new ChartSeries("Nominal", point => point.Nominal));

            var scale1 = _plot.Plot.Add.Scatter(x, points.Select(point => point.Scale1).ToArray());
            scale1.LegendText = "Kantar 1";
            scale1.Color = ScottPlot.Color.FromHex("#34D399");
            scale1.LineWidth = 2.2f;
            scale1.MarkerSize = 2;
            _series.Add(new ChartSeries("Kantar 1", point => point.Scale1));

            var scale2 = _plot.Plot.Add.Scatter(x, points.Select(point => point.Scale2).ToArray());
            scale2.LegendText = "Kantar 2";
            scale2.Color = ScottPlot.Color.FromHex("#A78BFA");
            scale2.LineWidth = 2.2f;
            scale2.MarkerSize = 2;
            _series.Add(new ChartSeries("Kantar 2", point => point.Scale2));

            _plot.Plot.ShowLegend(ScottPlot.Alignment.UpperLeft);
            _plot.Plot.Axes.AutoScale();
        }

        _plot.Refresh();
    }

    private void Plot_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_series.Count == 0)
        {
            HideTooltip();
            return;
        }

        var points = _points.ToArray();
        ChartSeries? closestSeries = null;
        var closestValue = double.NaN;
        var closestIndex = -1;
        var closestDistance = double.PositiveInfinity;

        foreach (var series in _series)
        {
            if (points.Length == 1)
            {
                var value = series.ValueSelector(points[0]);
                if (!double.IsFinite(value)) continue;
                var pixel = _plot.Plot.GetPixel(new ScottPlot.Coordinates(points[0].Sequence, value));
                var distance = Distance(e.Location.X, e.Location.Y, pixel.X, pixel.Y);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestSeries = series;
                    closestValue = value;
                    closestIndex = 0;
                }
                continue;
            }

            for (var index = 0; index < points.Length - 1; index++)
            {
                var firstValue = series.ValueSelector(points[index]);
                var secondValue = series.ValueSelector(points[index + 1]);
                if (!double.IsFinite(firstValue) || !double.IsFinite(secondValue)) continue;
                var firstPixel = _plot.Plot.GetPixel(new ScottPlot.Coordinates(points[index].Sequence, firstValue));
                var secondPixel = _plot.Plot.GetPixel(new ScottPlot.Coordinates(points[index + 1].Sequence, secondValue));
                var (distance, fraction) = DistanceToSegment(
                    e.Location.X, e.Location.Y,
                    firstPixel.X, firstPixel.Y,
                    secondPixel.X, secondPixel.Y);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestSeries = series;
                    closestValue = firstValue + ((secondValue - firstValue) * fraction);
                    closestIndex = fraction < 0.5 ? index : index + 1;
                }
            }
        }

        if (closestSeries is null || closestDistance > 10 || closestIndex < 0)
        {
            HideTooltip();
            return;
        }

        var measurement = points[closestIndex];
        var key = $"{closestSeries.Name}:{closestIndex}:{closestValue:0.####}";
        if (string.Equals(_hoverKey, key, StringComparison.Ordinal)) return;
        _hoverKey = key;
        var text = $"{closestSeries.Name}\nDeğer: {closestValue:0.##}\nSaat: {measurement.Timestamp:HH:mm:ss}";
        _toolTip.Show(text, _plot, e.Location.X + 14, e.Location.Y + 14, 5000);
    }

    private static (double Distance, double Fraction) DistanceToSegment(
        double pointX, double pointY,
        double firstX, double firstY,
        double secondX, double secondY)
    {
        var deltaX = secondX - firstX;
        var deltaY = secondY - firstY;
        var lengthSquared = (deltaX * deltaX) + (deltaY * deltaY);
        if (lengthSquared == 0) return (Distance(pointX, pointY, firstX, firstY), 0);
        var fraction = Math.Clamp(((pointX - firstX) * deltaX + (pointY - firstY) * deltaY) / lengthSquared, 0, 1);
        return (Distance(pointX, pointY, firstX + (fraction * deltaX), firstY + (fraction * deltaY)), fraction);
    }

    private static double Distance(double firstX, double firstY, double secondX, double secondY) =>
        Math.Sqrt(Math.Pow(firstX - secondX, 2) + Math.Pow(firstY - secondY, 2));

    private void HideTooltip()
    {
        if (_hoverKey is null) return;
        _hoverKey = null;
        _toolTip.Hide(_plot);
    }

    private static double ToDouble(decimal? value) => value.HasValue ? decimal.ToDouble(value.Value) : double.NaN;

    private sealed record ChartPoint(long Sequence, DateTime Timestamp, double Nominal, double Scale1, double Scale2);
    private sealed record ChartSeries(string Name, Func<ChartPoint, double> ValueSelector);
}
