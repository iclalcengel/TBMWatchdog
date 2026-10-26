using TBMWatchdog.Services;

namespace TBMWatchdog.Forms;

public sealed class PerformanceHealthForm : Form
{
    private readonly Label _health = new() { Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 16), TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label _general = new() { Dock = DockStyle.Fill, AutoSize = false, Font = new Font("Segoe UI", 9.5F), ForeColor = Color.FromArgb(203, 213, 225) };
    private readonly DataGridView _fields = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.FromArgb(30, 41, 59), ForeColor = Color.White };
    private readonly MetricGraph _graph = new() { Dock = DockStyle.Fill };
    public PerformanceHealthForm()
    {
        Text = "Performans ve Sağlık"; BackColor = Color.FromArgb(15, 23, 42); ForeColor = Color.White;
        Size = new Size(1180, 720); MinimumSize = new Size(900, 600); StartPosition = FormStartPosition.CenterParent;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 145));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 55)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        root.Controls.Add(_health, 0, 0); root.Controls.Add(_general, 0, 1); root.Controls.Add(_fields, 0, 2); root.Controls.Add(_graph, 0, 3); Controls.Add(root);
        foreach (var title in new[] { "Alan", "Son OCR", "Karakter", "Sıra", "Toplam", "Ort.", "Son 20", "Gerçek", "Cache", "Hit %", "Benzerlik", "Kaynak", "Durum", "Son Başarı" }) _fields.Columns.Add(title, title);
        _fields.EnableHeadersVisualStyles = false; _fields.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(51, 65, 85);
        _fields.ColumnHeadersDefaultCellStyle.ForeColor = Color.White; _fields.DefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59); _fields.DefaultCellStyle.ForeColor = Color.White;
    }
    public void UpdateSnapshot(PerformanceSnapshot s)
    {
        if (IsDisposed) return; _health.Text = $"{s.HealthStatus}   |   En yavaş alan: {s.SlowestField}";
        _health.ForeColor = s.HealthStatus switch { "Kritik" => Color.FromArgb(248, 113, 113), "Sağlıklı" => Color.FromArgb(52, 211, 153), _ => Color.FromArgb(251, 191, 36) };
        var hitRate = s.TotalCacheHits + s.TotalCacheMisses == 0 ? 0 : s.TotalCacheHits * 100d / (s.TotalCacheHits + s.TotalCacheMisses);
        _general.Text = $"Oturum: {s.SessionDuration:hh\\:mm\\:ss}   | Kare: {s.CapturedFrames}   | Gerçek OCR: {s.TotalRealOcr}   | Cache hit/miss: {s.TotalCacheHits}/{s.TotalCacheMisses} (%{hitRate:0.0})\r\n" +
            $"Ort./maks. gecikme: {s.AverageLatencyMs:0}/{s.MaximumLatencyMs:0} ms   | Son dakika: {s.ReadingsLastMinute}   | OCR hata: {s.TotalOcrErrors}   | Şüpheli: {s.SuspiciousValues}   | Filtrelenen: {s.FilteredSpikes}\r\n" +
            $"CPU: {Value(s.ProcessCpuPercent, "%")} (tek çekirdek eşdeğeri: {Value(s.ProcessCpuSingleCoreEquivalent, "%")}, mantıksal CPU: {s.LogicalProcessorCount})   | Bellek: {s.ProcessMemoryMb:0} MB   | Kullanılabilir: {Value(s.AvailableMemoryMb, " MB")}   | Thread: {s.ThreadCount}   | GDI: {s.GdiHandleCount?.ToString() ?? "Ölçülemedi"}   | Handle: {s.ProcessHandleCount}\r\n{s.DiagnosticMessage}";
        if(s.LastCycle is not null)_general.Text += $"\r\nCycle son/ort/P50/P95/maks: {s.LastCycle.CycleTotalMs:0}/{s.AverageCycleMs:0}/{s.P50CycleMs:0}/{s.P95CycleMs:0}/{s.MaximumCycleMs:0} ms\r\n"+
            string.Join(" | ",s.LastCycle.Fields.OrderByDescending(x=>x.TotalFieldMs).Take(5).Select((x,i)=>$"{i+1}. {x.Field} {x.TotalFieldMs:0} ms"));
        _fields.Rows.Clear(); foreach (var f in s.Fields) { var rowIndex = _fields.Rows.Add(f.Field, $"{f.LastRealOcrMs:0} ms", $"{f.LastCharacterOcrMs:0} ms", $"{f.QueueWaitMs:0} ms", $"{f.TotalLatencyMs:0} ms",
            $"{f.AverageRealOcrMs:0} ms", $"{f.Last20AverageMs:0} ms", f.RealOcrCount, f.CacheHitCount, $"%{f.CacheHitRate:0.0}", $"%{f.SimilarityPercent:0.000}", f.DecisionSource, f.FailureReason, f.LastSuccessfulOcrAt?.ToString("HH:mm:ss") ?? "—");
            foreach (DataGridViewCell cell in _fields.Rows[rowIndex].Cells) cell.ToolTipText = f.TimingBreakdown; }
        _graph.Add(s);
    }
    private static string Value(double? value, string suffix) => value.HasValue ? $"{value:0.0}{suffix}" : "Ölçülemedi";

    internal sealed class MetricGraph : Control
    {
        private readonly Queue<(double Cpu, double Memory, double Latency, double Cache)> _points = new(); public int PointCount => _points.Count;
        public MetricGraph() { DoubleBuffered = true; BackColor = Color.FromArgb(30, 41, 59); }
        public void Add(PerformanceSnapshot s) { var cache = s.TotalCacheHits + s.TotalCacheMisses == 0 ? 0 : s.TotalCacheHits * 100d / (s.TotalCacheHits + s.TotalCacheMisses); _points.Enqueue((s.ProcessCpuPercent ?? 0, s.ProcessMemoryMb, s.AverageLatencyMs, cache)); while (_points.Count > 300) _points.Dequeue(); Invalidate(); }
        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); var values = _points.ToArray(); if (values.Length < 2) return; Draw(e.Graphics, values.Select(x => x.Cpu), Color.FromArgb(56, 189, 248), 100); Draw(e.Graphics, values.Select(x => x.Cache), Color.FromArgb(52, 211, 153), 100); Draw(e.Graphics, values.Select(x => x.Latency), Color.FromArgb(251, 191, 36), Math.Max(5000, values.Max(x => x.Latency))); e.Graphics.DrawString("CPU mavi | Cache yeşil | Gecikme sarı (son 300 nokta)", Font, Brushes.White, 8, 8); }
        private void Draw(Graphics g, IEnumerable<double> source, Color color, double max) { var a = source.ToArray(); using var pen = new Pen(color, 2); var points = a.Select((v, i) => new PointF(i * (Width - 12f) / Math.Max(1, a.Length - 1) + 6, Height - 8 - (float)Math.Clamp(v / max, 0, 1) * (Height - 28))).ToArray(); if (points.Length > 1) g.DrawLines(pen, points); }
    }
}
