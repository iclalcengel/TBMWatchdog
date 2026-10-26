#nullable enable

using TBMWatchdog.Controls;

namespace TBMWatchdog.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer? components;
    private System.Windows.Forms.Timer clockTimer = null!;
    private Label dateLabel = null!;
    private Label timeLabel = null!;
    internal Label MonitoringStatusLabel = null!;
    internal Button StartButton = null!;
    internal Button StopButton = null!;
    internal Button PauseButton = null!;
    internal Button SettingsButton = null!;
    internal Button PerformanceButton = null!;
    internal DashboardCard NominalCard = null!;
    internal DashboardCard Scale1Card = null!;
    internal DashboardCard Scale2Card = null!;
    internal DashboardCard GatePositionCard = null!;
    internal DashboardCard GateStatusCard = null!;
    internal DashboardCard ExcavationStatusCard = null!;
    internal DataGridView HistoryGrid = null!;
    internal LiveChartControl LiveChart = null!;
    internal Label SessionStateLabel = null!;
    internal Label SessionStartedLabel = null!;
    internal Label SessionElapsedLabel = null!;
    internal Label SessionCountLabel = null!;
    internal Label SessionLastDataLabel = null!;
    internal Label SessionOcrLabel = null!;
    internal Label OperationModeLabel = null!;
    internal TextBox RingNoTextBox = null!;
    internal Button RingStartButton = null!;
    internal Button RingCancelButton = null!;
    internal Label RingSessionLabel = null!;
    internal TableLayoutPanel RingTrackingPanel = null!;
    internal Button ExcavationCategoryButton = null!;
    internal Button MaterialCategoryButton = null!;
    internal Button RingCategoryButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        clockTimer = new System.Windows.Forms.Timer(components) { Interval = 1000 };
        clockTimer.Tick += ClockTimer_Tick;

        SuspendLayout();
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(15, 23, 42);
        ClientSize = new Size(1360, 900);
        Font = new Font("Segoe UI", 9F);
        ForeColor = Color.FromArgb(226, 232, 240);
        MinimumSize = new Size(1040, 720);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "TBM Watchdog";

        var root = new TableLayoutPanel
        {
            BackColor = BackColor,
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            Padding = new Padding(24, 18, 24, 22)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 240F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 165F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        root.Controls.Add(CreateHeader(), 0, 0);
        root.Controls.Add(CreateSessionStatusPanel(), 0, 1);
        root.Controls.Add(CreateCardsPanel(), 0, 2);
        LiveChart = new LiveChartControl { Dock = DockStyle.Fill, Margin = new Padding(8, 2, 8, 8) };
        root.Controls.Add(LiveChart, 0, 3);
        root.Controls.Add(CreateRingTrackingPanel(), 0, 4);
        root.Controls.Add(CreateHistoryHeader(), 0, 5);
        HistoryGrid = CreateHistoryGrid();
        root.Controls.Add(HistoryGrid, 0, 6);
        void AdjustForWindowHeight()
        {
            var compact = root.ClientSize.Height < 830;
            root.Padding = new Padding(24, 18, 24, 22);
            root.RowStyles[0].Height = 80F;
            root.RowStyles[1].Height = compact ? 90F : 110F;
            root.RowStyles[2].Height = compact ? 200F : 240F;
            root.RowStyles[3].Height = compact ? 120F : 165F;
            root.RowStyles[4].Height = 96F;
            root.RowStyles[5].Height = 40F;
        }
        root.SizeChanged += (_, _) => AdjustForWindowHeight();
        Controls.Add(root);
        AdjustForWindowHeight();
        ResumeLayout(true);
    }

    private Control CreateHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            Margin = Padding.Empty,
            BackColor = Color.Transparent
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));

        var branding = new Panel { Dock = DockStyle.Fill };
        branding.Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 25F, FontStyle.Bold),
            ForeColor = Color.FromArgb(248, 250, 252),
            Location = new Point(0, 4),
            Text = "TBM Watchdog"
        });
        branding.Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(3, 57),
            Text = "Kazı izleme ve kayıt merkezi"
        });

        var clock = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        clock.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
        clock.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
        timeLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 19F),
            ForeColor = Color.FromArgb(226, 232, 240),
            TextAlign = ContentAlignment.BottomCenter
        };
        dateLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(100, 116, 139),
            TextAlign = ContentAlignment.TopCenter
        };
        clock.Controls.Add(timeLabel, 0, 0);
        clock.Controls.Add(dateLabel, 0, 1);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 19, 0, 0)
        };
        SettingsButton = CreateButton("Ayarlar", Color.FromArgb(51, 65, 85), 90);
        PerformanceButton = CreateButton("Performans", Color.FromArgb(67, 56, 202), 105);
        StopButton = CreateButton("İzlemeyi Durdur", Color.FromArgb(185, 28, 28), 142);
        StopButton.Enabled = false;
        StopButton.Visible = false;
        PauseButton = CreateButton("Duraklat", Color.FromArgb(180, 83, 9), 100);
        PauseButton.Enabled = false;
        PauseButton.Visible = false;
        StartButton = CreateButton("İzlemeye Başla", Color.FromArgb(2, 132, 199), 155);
        actions.Controls.Add(SettingsButton);
        actions.Controls.Add(PerformanceButton);
        actions.Controls.Add(StopButton);
        actions.Controls.Add(PauseButton);
        actions.Controls.Add(StartButton);

        header.Controls.Add(branding, 0, 0);
        header.Controls.Add(clock, 1, 0);
        header.Controls.Add(actions, 2, 0);
        return header;
    }

    private Control CreateSessionStatusPanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 1,
            BackColor = Color.FromArgb(30, 41, 59), Margin = new Padding(0, 2, 0, 8), Padding = new Padding(12, 10, 12, 10) };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 17));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        SessionStateLabel = NewSessionValue("HAZIR", Color.FromArgb(148, 163, 184));
        SessionStartedLabel = NewSessionValue("—", Color.FromArgb(226, 232, 240));
        SessionElapsedLabel = NewSessionValue("00:00:00", Color.FromArgb(56, 189, 248));
        SessionElapsedLabel.Font = new Font("Segoe UI Semibold", 8F);
        SessionCountLabel = NewSessionValue("0", Color.FromArgb(226, 232, 240));
        SessionLastDataLabel = NewSessionValue("—", Color.FromArgb(226, 232, 240));
        SessionOcrLabel = NewSessionValue("Henüz okuma yok", Color.FromArgb(226, 232, 240));
        panel.Controls.Add(SessionCell("DURUM", SessionStateLabel), 0, 0);
        panel.Controls.Add(SessionCell("BAŞLANGIÇ", SessionStartedLabel), 1, 0);
        panel.Controls.Add(SessionCell("OTURUM / AKTİF / DURAKLATILAN", SessionElapsedLabel), 2, 0);
        panel.Controls.Add(SessionCell("TOPLAM OKUMA", SessionCountLabel), 3, 0);
        panel.Controls.Add(SessionCell("SON BAŞARILI VERİ", SessionLastDataLabel), 4, 0);
        panel.Controls.Add(SessionCell("SON OCR DURUMU", SessionOcrLabel), 5, 0);
        return panel;
    }

    private static Control SessionCell(string caption, Label value)
    {
        var cell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(4), Padding = Padding.Empty };
        cell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        cell.RowStyles.Add(new RowStyle(SizeType.Absolute, 24)); cell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        cell.Controls.Add(new Label { Dock = DockStyle.Fill, Text = caption, Font = new Font("Segoe UI Semibold", 7.5F),
            ForeColor = Color.FromArgb(100, 116, 139), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        cell.Controls.Add(value, 0, 1); return cell;
    }

    private static Label NewSessionValue(string text, Color color) => new()
    {
        Dock = DockStyle.Fill, Text = text, ForeColor = color, Font = new Font("Segoe UI Semibold", 11F),
        TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true
    };

    private Control CreateCardsPanel()
    {
        var container = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
            Margin = new Padding(0, 2, 0, 6), BackColor = BackColor };
        container.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        container.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        var selector = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false, Margin = Padding.Empty, Padding = new Padding(7, 1, 0, 1), BackColor = BackColor };
        ExcavationCategoryButton = CategoryButton("KAZI", "Excavation");
        MaterialCategoryButton = CategoryButton("MALZEME", "Material");
        RingCategoryButton = CategoryButton("RING", "Ring");
        selector.Controls.AddRange([ExcavationCategoryButton, MaterialCategoryButton, RingCategoryButton]);
        container.Controls.Add(selector, 0, 0);
        var cards = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            Margin = Padding.Empty
        };
        for (var i = 0; i < 3; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        cards.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        cards.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        NominalCard = NewCard("RPM", "—", "Canlı tanılama", Color.FromArgb(56, 189, 248));
        Scale1Card = NewCard("TORK", "—", "Canlı tanılama", Color.FromArgb(52, 211, 153));
        Scale2Card = NewCard("İTME KUVVETİ", "—", "Canlı tanılama", Color.FromArgb(167, 139, 250));
        GatePositionCard = NewCard("İLERLEME HIZI", "—", "Canlı tanılama", Color.FromArgb(251, 191, 36));
        GateStatusCard = NewCard("PENETRASYON", "—", "Canlı tanılama", Color.FromArgb(248, 113, 113));
        ExcavationStatusCard = NewCard("İLERLEME", "—", "Canlı resmî Advance", Color.FromArgb(148, 163, 184));
        ExcavationStatusCard.Controls.OfType<Label>().First(label => label.Dock == DockStyle.Fill).Font = new Font("Segoe UI Semibold", 15F);

        cards.Controls.Add(NominalCard, 0, 0);
        cards.Controls.Add(Scale1Card, 1, 0);
        cards.Controls.Add(Scale2Card, 2, 0);
        cards.Controls.Add(GatePositionCard, 0, 1);
        cards.Controls.Add(GateStatusCard, 1, 1);
        cards.Controls.Add(ExcavationStatusCard, 2, 1);
        container.Controls.Add(cards, 0, 1);
        return container;
    }

    private Button CategoryButton(string text, string category)
    {
        var button = new Button { Text = text, Tag = category, Width = 112, Height = 30, Margin = new Padding(1, 0, 5, 0),
            FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI Semibold", 9F), UseVisualStyleBackColor = false };
        button.FlatAppearance.BorderSize = 1; button.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
        button.Click += CategoryButton_Click; return button;
    }

    private Control CreateHistoryHeader()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty };
        panel.Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 13F),
            ForeColor = Color.FromArgb(226, 232, 240),
            Location = new Point(3, 10),
            Text = "Ölçüm Geçmişi"
        });
        MonitoringStatusLabel = new Label
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            AutoSize = false,
            BackColor = Color.FromArgb(51, 65, 85),
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(203, 213, 225),
            Location = new Point(1080, 8),
            Size = new Size(205, 28),
            Text = "●  İZLEME KAPALI",
            TextAlign = ContentAlignment.MiddleCenter
        };
        panel.Controls.Add(MonitoringStatusLabel);
        return panel;
    }

    private Control CreateRingTrackingPanel()
    {
        RingTrackingPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 7, RowCount = 2,
            BackColor = Color.FromArgb(30, 41, 59), Margin = new Padding(0, 4, 0, 5), Padding = new Padding(12, 7, 12, 7)
        };
        RingTrackingPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135F));
        RingTrackingPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 65F));
        RingTrackingPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        RingTrackingPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135F));
        RingTrackingPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125F));
        RingTrackingPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 18F));
        RingTrackingPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        RingTrackingPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        RingTrackingPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var title = new Label { Dock = DockStyle.Fill, Text = "RING TAKİBİ", Font = new Font("Segoe UI Semibold", 10F),
            ForeColor = Color.FromArgb(125, 211, 252), TextAlign = ContentAlignment.MiddleLeft };
        var numberCaption = new Label { Dock = DockStyle.Fill, Text = "Ring No:", ForeColor = Color.FromArgb(203, 213, 225),
            TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 6, 0) };
        RingNoTextBox = new TextBox { Dock = DockStyle.Fill, MinimumSize = new Size(100, 30), Margin = new Padding(3, 5, 8, 4),
            PlaceholderText = "1234", BackColor = Color.FromArgb(15, 23, 42), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        RingStartButton = RingButton("Ring Başlat", Color.FromArgb(2, 132, 199));
        RingCancelButton = RingButton("Ring İptal", Color.FromArgb(153, 27, 27)); RingCancelButton.Enabled = false;
        OperationModeLabel = new Label { Dock = DockStyle.Fill, AutoEllipsis = true, ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI Semibold", 9F), Text = "Çalışma modu: Okunamadı", TextAlign = ContentAlignment.MiddleLeft };
        RingSessionLabel = new Label { Dock = DockStyle.Fill, AutoEllipsis = true, ForeColor = Color.FromArgb(203, 213, 225),
            Font = new Font("Segoe UI Semibold", 8.5F), Text = "Aktif ring: Yok\nDurum: — | Stabil: Bekliyor | Kazı sonu: Bekliyor | PPS: Bekliyor | Excel: Bekliyor",
            TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(2, 0, 2, 0) };
        RingTrackingPanel.Controls.Add(title, 0, 0); RingTrackingPanel.Controls.Add(numberCaption, 1, 0);
        RingTrackingPanel.Controls.Add(RingNoTextBox, 2, 0); RingTrackingPanel.Controls.Add(RingStartButton, 3, 0);
        RingTrackingPanel.Controls.Add(RingCancelButton, 4, 0); RingTrackingPanel.Controls.Add(OperationModeLabel, 6, 0);
        RingTrackingPanel.Controls.Add(RingSessionLabel, 0, 1); RingTrackingPanel.SetColumnSpan(RingSessionLabel, 7);
        return RingTrackingPanel;
    }

    private static Button RingButton(string text, Color color) => new()
    {
        Dock = DockStyle.Fill, MinimumSize = new Size(110, 32), Margin = new Padding(5, 2, 5, 2), Text = text,
        BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, AutoSize = false,
        Font = new Font("Segoe UI Semibold", 9F), UseVisualStyleBackColor = false,
        FlatAppearance = { BorderSize = 0 }
    };

    private static DataGridView CreateHistoryGrid()
    {
        var grid = new DataGridView
        {
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.FromArgb(30, 41, 59),
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            Dock = DockStyle.Fill,
            EnableHeadersVisualStyles = false,
            GridColor = Color.FromArgb(55, 78, 100),
            ReadOnly = true,
            RowHeadersVisible = false,
            RowTemplate = { Height = 34 },
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            Alignment = DataGridViewContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(45, 61, 82),
            ForeColor = Color.FromArgb(226, 232, 240),
            Font = new Font("Segoe UI Semibold", 9.25F),
            Padding = new Padding(7, 2, 7, 2),
            SelectionBackColor = Color.FromArgb(51, 65, 85)
        };
        grid.ColumnHeadersHeight = 40;
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(29, 40, 58),
            ForeColor = Color.FromArgb(226, 232, 240),
            Padding = new Padding(7, 0, 7, 0),
            SelectionBackColor = Color.FromArgb(2, 112, 165),
            SelectionForeColor = Color.White
        };
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(32, 44, 63);

        Add("Time", "Saat", 78, "Okuma saati");
        Add("Rpm", "RPM", 70, "RPM (rpm)");
        Add("Torque", "Tork", 82, "Tork (MNm)");
        Add("Thrust", "İtme", 88, "İtme Kuvveti (kN)");
        Add("Speed", "Hız", 76, "İlerleme Hızı (mm/min)");
        Add("Penetration", "Penetrasyon", 66, "Penetrasyon (mm/rot)");
        Add("Nominal", "Nominal Pasa", 94, "Nominal Pasa Miktarı");
        Add("Scale1", "Kantar 1", 65, "Kantar 1");
        Add("Scale2", "Kantar 2", 65, "Kantar 2");
        Add("NominalInjection", "Nominal Enj.", 92, "Nominal Enjeksiyon");
        Add("ActualInjection", "Gerçek Enj.", 92, "Gerçek Enjeksiyon");
        Add("Epb1", "EPB1", 62, "1 no'lu EPB sensörü");
        Add("Epb2", "EPB2", 62, "2 no'lu EPB sensörü");
        Add("Epb3", "EPB3", 62, "3 no'lu EPB sensörü");
        Add("Epb4", "EPB4", 62, "4 no'lu EPB sensörü");
        Add("Epb5", "EPB5", 62, "5 no'lu EPB sensörü");
        Add("OCR", "OCR", 64, "Satırın genel OCR güven durumu");
        var widths = new[] { 90, 75, 90, 95, 90, 80, 105, 75, 75, 105, 105, 75, 75, 75, 75, 75, 100 };
        for (var index = 0; index < grid.Columns.Count; index++)
        {
            grid.Columns[index].MinimumWidth = widths[index];
            grid.Columns[index].FillWeight = widths[index];
        }
        grid.Columns["Penetration"].MinimumWidth = 130;
        grid.Columns["Nominal"].MinimumWidth = 140;
        grid.Columns["NominalInjection"].MinimumWidth = 140;
        grid.Columns["ActualInjection"].MinimumWidth = 140;
        grid.Columns["Scale1"].MinimumWidth = 110;
        grid.Columns["Scale2"].MinimumWidth = 110;
        foreach (DataGridViewColumn column in grid.Columns) column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        grid.Columns["Time"].Frozen = true;
        grid.Columns["Time"].DividerWidth = 2;
        grid.Columns["Time"].DefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F);
        grid.Columns["Time"].HeaderCell.Style.BackColor = Color.FromArgb(49, 68, 91);
        grid.Columns["Penetration"].DividerWidth = 2;
        grid.Columns["Scale2"].DividerWidth = 2;
        grid.Columns["ActualInjection"].DividerWidth = 2;
        grid.Columns["Epb5"].DividerWidth = 2;
        grid.Columns["OCR"].HeaderCell.Style.BackColor = Color.FromArgb(38, 79, 102);
        grid.Columns["OCR"].HeaderCell.Style.ForeColor = Color.FromArgb(186, 230, 253);
        grid.Columns["OCR"].DefaultCellStyle.BackColor = Color.FromArgb(27, 46, 62);
        grid.Columns["OCR"].DefaultCellStyle.Font = new Font("Segoe UI Semibold", 10F);
        grid.ScrollBars = ScrollBars.Both;
        return grid;

        void Add(string name, string header, int width, string tooltip)
        {
            var index = grid.Columns.Add(name, header); grid.Columns[index].Width = width;
            grid.Columns[index].HeaderCell.ToolTipText = tooltip;
        }
    }

    private static DashboardCard NewCard(string caption, string value, string detail, Color accent) => new()
    {
        AccentColor = accent,
        Caption = caption,
        Value = value,
        Detail = detail,
        Dock = DockStyle.Fill
    };

    private static Button CreateButton(string text, Color color, int width) => new()
    {
        BackColor = color,
        Cursor = Cursors.Hand,
        FlatAppearance = { BorderSize = 0, MouseOverBackColor = ControlPaint.Light(color, 0.08F) },
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI Semibold", 9.5F),
        ForeColor = Color.White,
        Height = 42,
        Margin = new Padding(8, 0, 0, 0),
        Text = text,
        UseVisualStyleBackColor = false,
        Width = width
    };
}
