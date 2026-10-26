using System.Diagnostics;

namespace TBMWatchdog.Forms;

public sealed class DigitTemplateManagerForm : Form
{
    private readonly Services.DigitTemplateStore _store; private readonly int _minimumRecommended;
    private readonly ListView _list = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false };
    private ImageList? _images;
    public DigitTemplateManagerForm(string folder, string machineProfile, int minimumRecommended,
        Models.OcrFieldKind fieldKind = Models.OcrFieldKind.Gate, double duplicateThreshold = .97, int maximumTemplatesPerDigit = 30)
    {
        _store = new(folder, machineProfile, fieldKind, duplicateThreshold, maximumTemplatesPerDigit); _minimumRecommended = minimumRecommended;
        Text = $"Rakam Şablonları — {machineProfile} — {fieldKind}"; StartPosition = FormStartPosition.CenterParent;
        Size = new Size(1180, 680); MinimumSize = new Size(900, 500); BackColor = Color.FromArgb(15, 23, 42); ForeColor = Color.White;
        _list.BackColor = Color.FromArgb(30, 41, 59); _list.ForeColor = Color.White;
        foreach (var column in new[] { ("Tür",75), ("Rakam",55), ("Dosya / Sayı",175), ("Etkin",55), ("Kalite",90),
            ("Kaynak",105), ("Son Öğretim",135), ("Kullanım/Başarı/Çatışma",160), ("Durum",235) }) _list.Columns.Add(column.Item1, column.Item2);
        var disable = Button("Etkin / Devre Dışı"); disable.Click += Toggle_Click;
        var delete = Button("Şablonu Sil"); delete.Click += Delete_Click;
        var open = Button("Klasörü Aç"); open.Click += (_, _) => OpenFolder();
        var reload = Button("Şablonları Yeniden Yükle"); reload.Click += (_, _) => { _store.Reload(); LoadItems(); MessageBox.Show(this, "Şablonlar yeniden yüklendi."); };
        var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 58, Padding = new Padding(8) };
        actions.Controls.AddRange([disable, delete, open, reload]); Controls.Add(_list); Controls.Add(actions); LoadItems();
    }
    private void LoadItems()
    {
        _images?.Dispose(); _images = new ImageList { ImageSize = new Size(48, 72), ColorDepth = ColorDepth.Depth32Bit };
        _list.SmallImageList = _images; _list.Items.Clear(); var items = _store.GetItems().Where(item => item.Digit is >= 0 and <= 9).ToArray();
        for (var digit = 0; digit <= 9; digit++)
        {
            var active = items.Where(item => item.Digit == digit && item.Enabled).ToArray(); var count = active.Length;
            var average = count == 0 ? 0 : active.Average(item => item.Metadata.QualityScore);
            var last = items.Where(item => item.Digit == digit).Select(item => item.Metadata.CapturedAt).DefaultIfEmpty().Max();
            var status = count == 0 ? "Şablon yok" : count < _minimumRecommended ? "Daha fazla örnek önerilir" : "Yeterli örnek";
            var summary = new ListViewItem("Özet") { ForeColor = count == 0 ? Color.IndianRed : count < _minimumRecommended ? Color.Orange : Color.LightGreen };
            Add(summary, digit.ToString(), $"{count} şablon", "—", $"%{average * 100:0}", "—",
                last == default ? "—" : last.ToString("dd.MM.yyyy HH:mm"), "—", status); _list.Items.Add(summary);
            foreach (var item in items.Where(item => item.Digit == digit))
            {
                var imageIndex = -1; try { using var image = new Bitmap(item.ImagePath); _images.Images.Add(new Bitmap(image)); imageIndex = _images.Images.Count - 1; } catch { }
                var m = item.Metadata; var row = new ListViewItem("Şablon", imageIndex) { Tag = item, ForeColor = item.Enabled ? Color.White : Color.Gray };
                Add(row, digit.ToString(), Path.GetFileName(item.ImagePath), item.Enabled ? "Evet" : "Hayır",
                    $"%{m.QualityScore * 100:0} {m.QualityStatus}", m.SourceField, m.CapturedAt.ToString("dd.MM.yyyy HH:mm"),
                    $"{m.UsageCount}/{m.SuccessCount}/{m.ConflictCount}", item.Enabled ? m.QualityReason : "Devre dışı"); _list.Items.Add(row);
            }
        }
    }
    private static void Add(ListViewItem row, params string[] values) { foreach (var value in values) row.SubItems.Add(value); }
    private void Toggle_Click(object? sender, EventArgs e) { if (SelectedItem() is { } item) { _store.SetEnabled(item, !item.Enabled); LoadItems(); } }
    private void Delete_Click(object? sender, EventArgs e)
    { if (SelectedItem() is not { } item) return; if (MessageBox.Show(this, $"{Path.GetFileName(item.ImagePath)} şablonu silinsin mi?", "Şablon Sil", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) { _store.Delete(item); LoadItems(); } }
    private Services.DigitTemplateItem? SelectedItem() => _list.SelectedItems.Count > 0 ? _list.SelectedItems[0].Tag as Services.DigitTemplateItem : null;
    private void OpenFolder() { Directory.CreateDirectory(_store.GateFolder); Process.Start(new ProcessStartInfo { FileName = _store.GateFolder, UseShellExecute = true }); }
    private static Button Button(string text) => new() { Text = text, AutoSize = true, Height = 36, BackColor = Color.FromArgb(51, 65, 85), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(5) };
    protected override void Dispose(bool disposing) { if (disposing) _images?.Dispose(); base.Dispose(disposing); }
}
