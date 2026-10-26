using System.Drawing.Drawing2D;

namespace TBMWatchdog.Controls;

public sealed class DashboardCard : Panel
{
    private readonly Label _captionLabel;
    private readonly Label _valueLabel;
    private readonly Label _detailLabel;

    public DashboardCard()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(30, 41, 59);
        ForeColor = Color.FromArgb(241, 245, 249);
        Margin = new Padding(5);
        Padding = new Padding(16, 7, 16, 6);
        MinimumSize = new Size(180, 70);

        _captionLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 20,
            Font = new Font("Segoe UI Semibold", 9.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _detailLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Bottom,
            Height = 20,
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _valueLabel = new Label
        {
            AutoEllipsis = true,
            AutoSize = false,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 20F),
            ForeColor = ForeColor,
            TextAlign = ContentAlignment.MiddleLeft
        };

        Controls.Add(_valueLabel);
        Controls.Add(_detailLabel);
        Controls.Add(_captionLabel);
        Resize += (_, _) => UpdateRegion();
        UpdateRegion();
    }

    public string Caption
    {
        get => _captionLabel.Text;
        set => _captionLabel.Text = value;
    }

    public string Value
    {
        get => _valueLabel.Text;
        set => _valueLabel.Text = value;
    }

    public string Detail
    {
        get => _detailLabel.Text;
        set => _detailLabel.Text = value;
    }

    public Color AccentColor { get; set; } = Color.FromArgb(56, 189, 248);

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var accentBrush = new SolidBrush(AccentColor);
        e.Graphics.FillRectangle(accentBrush, 0, 18, 4, Math.Max(0, Height - 36));
    }

    private void UpdateRegion()
    {
        using var path = new GraphicsPath();
        const int radius = 14;
        var bounds = new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height));
        path.AddArc(bounds.Left, bounds.Top, radius, radius, 180, 90);
        path.AddArc(bounds.Right - radius, bounds.Top, radius, radius, 270, 90);
        path.AddArc(bounds.Right - radius, bounds.Bottom - radius, radius, radius, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - radius, radius, radius, 90, 90);
        path.CloseFigure();
        Region = new Region(path);
    }
}
