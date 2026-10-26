using System.Runtime.InteropServices;

namespace TBMWatchdog.Forms;

public sealed class GateDigitTemplateCollectorForm : Form
{
    private readonly Bitmap _templateImage;
    private readonly Bitmap _sourceImage;
    private readonly Services.DigitTemplateStore _store;
    private readonly string _preprocessingMethod;
    private readonly double _referenceConfidence;
    private readonly NumericUpDown _digit = new() { Minimum = 0, Maximum = 9, Font = new Font("Segoe UI Semibold", 18), Width = 90 };

    public GateDigitTemplateCollectorForm(Bitmap segment, string templateFolder, string machineProfile, string preprocessingMethod,
        Models.OcrFieldKind fieldKind = Models.OcrFieldKind.Gate, double duplicateThreshold = .97,
        int maximumTemplatesPerDigit = 30, double referenceConfidence = 0)
    {
        _sourceImage = new Bitmap(segment);
        _templateImage = Services.DigitTemplateMatcher.NormalizeForMatching(segment);
        _store = new Services.DigitTemplateStore(templateFolder, machineProfile, fieldKind, duplicateThreshold, maximumTemplatesPerDigit);
        _preprocessingMethod = preprocessingMethod;
        _referenceConfidence = referenceConfidence;
        Text = "Rakamı Öğret"; StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
        ClientSize = new Size(620, 330); BackColor = Color.FromArgb(15, 23, 42); ForeColor = Color.White;
        var preview = new PictureBox { Dock = DockStyle.Fill, BackColor = Color.Black, SizeMode = PictureBoxSizeMode.Zoom, Image = new Bitmap(_templateImage) };
        var question = new Label { AutoSize = true, Text = "Bu karakterin doğru rakamı:", Margin = new Padding(8, 14, 8, 8) };
        var save = new Button { Text = "Şablon Olarak Kaydet", Width = 150, Height = 36, BackColor = Color.FromArgb(2, 132, 199), FlatStyle = FlatStyle.Flat };
        var cancel = new Button { Text = "Vazgeç", Width = 90, Height = 36, BackColor = Color.FromArgb(51, 65, 85), FlatStyle = FlatStyle.Flat };
        save.Click += Save_Click; cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(8) };
        actions.Controls.Add(question); actions.Controls.Add(_digit); actions.Controls.Add(save); actions.Controls.Add(cancel);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(12) };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        layout.Controls.Add(preview, 0, 0); layout.Controls.Add(actions, 0, 1); Controls.Add(layout);
    }

    public string? SavedPath { get; private set; }

    private void Save_Click(object? sender, EventArgs e)
    {
        try
        {
            var digit = decimal.ToInt32(_digit.Value);
            var result = _store.Save(digit, _sourceImage, _templateImage, _preprocessingMethod,
                referenceConfidence: _referenceConfidence);
            if (!result.Success && result.CanOverridePoorQuality)
            {
                var answer = MessageBox.Show(this, $"{result.Message}\r\n\r\nYine de kaydetmek istiyor musunuz?",
                    "Düşük Kaliteli Şablon", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (answer == DialogResult.Yes)
                    result = _store.Save(digit, _sourceImage, _templateImage, _preprocessingMethod, true, _referenceConfidence);
            }
            if (!result.Success)
            {
                MessageBox.Show(this, result.Message, "Şablon Kalite Kontrolü", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            SavedPath = result.ImagePath;
            MessageBox.Show(this, result.Message, "Rakam Öğretme", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ExternalException)
        {
            MessageBox.Show(this, $"Şablon kaydedilemedi: {exception.Message}", "Şablon Kaydı", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var box in Controls.OfType<TableLayoutPanel>().SelectMany(Descendants).OfType<PictureBox>()) { box.Image?.Dispose(); box.Image = null; }
            _templateImage.Dispose();
            _sourceImage.Dispose();
        }
        base.Dispose(disposing);
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls) { yield return child; foreach (var nested in Descendants(child)) yield return nested; }
    }
}
