using TBMWatchdog.Models;
using TBMWatchdog.Services;

namespace TBMWatchdog.Forms;

public sealed class OcrPreviewForm : Form
{
    private readonly FieldDiagnosticView _nominal = new();
    private readonly FieldDiagnosticView _scale1 = new();
    private readonly FieldDiagnosticView _scale2 = new();
    private readonly FieldDiagnosticView _gate = new();
    private readonly Label _timestamp = new();

    public OcrPreviewForm()
    {
        Text = "TBM Watchdog — Gelişmiş OCR Tanılama";
        BackColor = Color.FromArgb(15, 23, 42);
        ForeColor = Color.FromArgb(226, 232, 240);
        Font = new Font("Segoe UI", 9F);
        StartPosition = FormStartPosition.Manual;
        Size = new Size(1040, 740);
        MinimumSize = new Size(850, 620);
        ShowInTaskbar = false;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(12) };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 10F)
        };
        tabs.TabPages.Add(NewTab("Nominal", _nominal));
        tabs.TabPages.Add(NewTab("Kantar 1", _scale1));
        tabs.TabPages.Add(NewTab("Kantar 2", _scale2));
        tabs.TabPages.Add(NewTab("Kapak mm", _gate));
        _timestamp.Dock = DockStyle.Fill;
        _timestamp.ForeColor = Color.FromArgb(148, 163, 184);
        _timestamp.TextAlign = ContentAlignment.MiddleRight;
        root.Controls.Add(tabs, 0, 0);
        root.Controls.Add(_timestamp, 0, 1);
        Controls.Add(root);
    }

    public void UpdateFrame(OcrDiagnosticsFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        try
        {
            CharacterOcrComparison? Comparison(string field) => frame.Comparisons.FirstOrDefault(item => item.Field == field);
            OcrCacheStatistics? Cache(string field) => frame.CacheStatistics.FirstOrDefault(item => item.Field == field);
            _nominal.UpdateDiagnostic(frame.Nominal, Comparison("Nominal"), Cache("Nominal"));
            _scale1.UpdateDiagnostic(frame.Scale1, Comparison("Kantar 1"), Cache("Scale1"));
            _scale2.UpdateDiagnostic(frame.Scale2, Comparison("Kantar 2"), Cache("Scale2"));
            _gate.UpdateDiagnostic(frame.Gate, Comparison("Kapak mm"), Cache("Gate"));
            _timestamp.Text = $"Yakalama: {frame.CapturedAt:dd.MM.yyyy HH:mm:ss.fff} | Gösterim: {frame.DisplayedAt:HH:mm:ss.fff} | Gecikme: {(frame.DisplayedAt - frame.CapturedAt).TotalMilliseconds:0} ms";
        }
        finally
        {
            frame.Dispose();
        }
    }

    private static TabPage NewTab(string title, Control content)
    {
        var tab = new TabPage(title)
        {
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.FromArgb(226, 232, 240),
            Padding = new Padding(8)
        };
        content.Dock = DockStyle.Fill;
        tab.Controls.Add(content);
        return tab;
    }

    private sealed class FieldDiagnosticView : UserControl
    {
        private readonly PictureBox _rawImage = NewImageBox();
        private readonly PictureBox _processedImage = NewImageBox();
        private readonly Label _rawText = NewValueLabel();
        private readonly Label _confidence = NewValueLabel();
        private readonly Label _decision = NewValueLabel();
        private readonly ListView _history = NewHistoryList();
        private readonly ListView _candidates = NewCandidateList();

        public FieldDiagnosticView()
        {
            BackColor = Color.FromArgb(15, 23, 42);
            ForeColor = Color.FromArgb(226, 232, 240);
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1 };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 138F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 32F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 28F));
            root.Controls.Add(CreateImagesPanel(), 0, 0);
            root.Controls.Add(CreateDetailsPanel(), 0, 1);
            root.Controls.Add(CreateCandidatesPanel(), 0, 2);
            root.Controls.Add(CreateHistoryPanel(), 0, 3);
            Controls.Add(root);
            _candidates.SelectedIndexChanged += (_, _) =>
            {
                if (_candidates.SelectedItems.Count > 0 && _candidates.SelectedItems[0].Tag is Bitmap image)
                    SetImage(_processedImage, image);
            };
        }

        public void UpdateDiagnostic(OcrDiagnosticResult diagnostic, CharacterOcrComparison? comparison = null,
            OcrCacheStatistics? cache = null)
        {
            SetImage(_rawImage, diagnostic.RawImage);
            SetImage(_processedImage, diagnostic.ProcessedImage);
            _rawText.Text = string.IsNullOrWhiteSpace(diagnostic.Result.RawText)
                ? "∅ (boş)"
                : diagnostic.Result.RawText.Replace("\r", " ").Replace("\n", " ↵ ").Trim();
            _confidence.Text = $"%{diagnostic.Result.Confidence * 100:0.0}";
            _decision.Text = comparison is null ? diagnostic.DecisionReason
                : $"Tam satır: {comparison.FullLineResult?.ToString("0.##") ?? "—"} | Karakter: {comparison.CharacterResult?.ToString("0.##") ?? "—"} | {comparison.IsSame switch { true => "Aynı", false => "Farklı", _ => "Karşılaştırılamadı" }} | Karakter süresi: {comparison.CharacterDurationMilliseconds:0} ms | Toplam OCR: {comparison.TotalOcrDurationMilliseconds:0} ms | Gecikme: {comparison.TotalLatencyMilliseconds:0} ms | Tanılama Durumu: {comparison.FailureReason}";
            if (comparison is not null)
            {
                _decision.Text += $"\r\nGerçek OCR: {(comparison.RealOcrExecuted ? "Evet" : "Hayır")} | " +
                    $"Önbellek: {(comparison.CacheUsed ? "Kullanıldı" : "Kullanılmadı")} | " +
                    $"Benzerlik: %{comparison.ImageSimilarityPercent:0.000} | Yaş: {comparison.CacheAgeMs:0} ms";
                if (cache is not null)
                    _decision.Text += $" | Hit/Miss: {cache.CacheHitCount}/{cache.CacheMissCount} | " +
                        $"Hit oranı: %{cache.HitRatePercent:0.0} | Önlenen OCR: {cache.EstimatedPreventedOcrMilliseconds:0} ms";
            }
            _decision.ForeColor = diagnostic.IsAccepted
                ? Color.FromArgb(110, 231, 183)
                : Color.FromArgb(254, 202, 202);

            foreach (ListViewItem oldItem in _candidates.Items) (oldItem.Tag as Bitmap)?.Dispose();
            _candidates.Items.Clear();
            foreach (var candidate in diagnostic.Candidates)
            {
                var item = new ListViewItem(candidate.ProcessingMethod) { Tag = new Bitmap(candidate.ProcessedImage) };
                item.SubItems.Add(candidate.PageSegMode.ToString());
                item.SubItems.Add(string.IsNullOrWhiteSpace(candidate.RawText) ? "∅" : candidate.RawText.Replace("\r", " ").Replace("\n", " ").Trim());
                item.SubItems.Add(candidate.CleanedText);
                item.SubItems.Add(candidate.ParsedValue?.ToString("0.##") ?? "—");
                item.SubItems.Add($"%{candidate.Confidence * 100:0.0}");
                item.SubItems.Add(candidate.IsValid ? "Geçerli" : candidate.Error ?? "Geçersiz");
                if (ReferenceEquals(candidate, diagnostic.SelectedCandidate)) item.ForeColor = Color.FromArgb(110, 231, 183);
                _candidates.Items.Add(item);
            }

            var historyItem = new ListViewItem(diagnostic.Timestamp.ToString("HH:mm:ss.fff"));
            historyItem.SubItems.Add(string.IsNullOrWhiteSpace(diagnostic.Result.RawText)
                ? "∅"
                : diagnostic.Result.RawText.Replace("\r", " ").Replace("\n", " ").Trim());
            historyItem.SubItems.Add(diagnostic.Result.Value?.ToString("0.##") ?? "—");
            historyItem.SubItems.Add($"%{diagnostic.Result.Confidence * 100:0.0}");
            historyItem.SubItems.Add(diagnostic.IsAccepted ? "Kabul" : $"Red — {diagnostic.Result.Error}");
            historyItem.ForeColor = diagnostic.IsAccepted
                ? Color.FromArgb(167, 243, 208)
                : Color.FromArgb(254, 202, 202);
            _history.Items.Insert(0, historyItem);
            while (_history.Items.Count > 5) _history.Items.RemoveAt(_history.Items.Count - 1);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _rawImage.Image?.Dispose();
                _processedImage.Image?.Dispose();
                _rawImage.Image = null;
                _processedImage.Image = null;
                foreach (ListViewItem item in _candidates.Items) (item.Tag as Bitmap)?.Dispose();
            }

            base.Dispose(disposing);
        }

        private Control CreateImagesPanel()
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            panel.Controls.Add(NewImageGroup("Ham Görüntü", _rawImage), 0, 0);
            panel.Controls.Add(NewImageGroup("İşlenmiş Görüntü (3×, gri, kontrast, threshold)", _processedImage), 1, 0);
            return panel;
        }

        private Control CreateDetailsPanel()
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(4) };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panel.Controls.Add(NewCaption("Ham OCR metni"), 0, 0);
            panel.Controls.Add(_rawText, 1, 0);
            panel.Controls.Add(NewCaption("Güven"), 0, 1);
            panel.Controls.Add(_confidence, 1, 1);
            panel.Controls.Add(NewCaption("Kabul / Red"), 0, 2);
            panel.Controls.Add(_decision, 1, 2);
            return panel;
        }

        private Control CreateHistoryPanel()
        {
            var group = new GroupBox
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(203, 213, 225),
                Padding = new Padding(8),
                Text = "Son Beş Okuma"
            };
            group.Controls.Add(_history);
            return group;
        }

        private Control CreateCandidatesPanel()
        {
            var group = new GroupBox { Dock = DockStyle.Fill, ForeColor = Color.FromArgb(203, 213, 225), Padding = new Padding(8), Text = "OCR Adayları (satıra tıklayın)" };
            group.Controls.Add(_candidates);
            return group;
        }

        private static GroupBox NewImageGroup(string title, Control content)
        {
            var group = new GroupBox
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(203, 213, 225),
                Margin = new Padding(6),
                Padding = new Padding(8),
                Text = title
            };
            group.Controls.Add(content);
            return group;
        }

        private static PictureBox NewImageBox() => new()
        {
            BackColor = Color.Black,
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom
        };

        private static Label NewCaption(string text) => new()
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(148, 163, 184),
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft
        };

        private static Label NewValueLabel() => new()
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 9.5F),
            ForeColor = Color.FromArgb(226, 232, 240),
            TextAlign = ContentAlignment.MiddleLeft
        };

        private static ListView NewHistoryList()
        {
            var list = new ListView
            {
                BackColor = Color.FromArgb(30, 41, 59),
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(226, 232, 240),
                FullRowSelect = true,
                GridLines = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                HideSelection = false,
                View = View.Details
            };
            list.Columns.Add("Saat", 100);
            list.Columns.Add("Ham Metin", 180);
            list.Columns.Add("Değer", 100);
            list.Columns.Add("Güven", 90);
            list.Columns.Add("Sonuç / Neden", 430);
            return list;
        }

        private static ListView NewCandidateList()
        {
            var list = NewHistoryList();
            list.Columns.Clear();
            list.Columns.Add("İşleme Yöntemi", 210);
            list.Columns.Add("PSM", 90);
            list.Columns.Add("Ham", 90);
            list.Columns.Add("Temiz", 80);
            list.Columns.Add("Değer", 75);
            list.Columns.Add("Güven", 75);
            list.Columns.Add("Sonuç / Neden", 350);
            return list;
        }

        private static void SetImage(PictureBox target, Bitmap source)
        {
            var previous = target.Image;
            target.Image = new Bitmap(source);
            previous?.Dispose();
        }
    }
}
