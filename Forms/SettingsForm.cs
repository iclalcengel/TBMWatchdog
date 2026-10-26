using TBMWatchdog.Models;
using TBMWatchdog.Services;

namespace TBMWatchdog.Forms;

public sealed class SettingsForm : Form
{
    private static readonly KeyValuePair<AdvanceSelectionMode, string>[] AdvanceSelectionOptions =
    [
        new(AdvanceSelectionMode.GroupA, "Grup A"), new(AdvanceSelectionMode.GroupB, "Grup B"),
        new(AdvanceSelectionMode.GroupC, "Grup C"), new(AdvanceSelectionMode.GroupD, "Grup D"),
        new(AdvanceSelectionMode.AverageOfValidGroups, "Geçerli grupların ortalaması"),
        new(AdvanceSelectionMode.ClosestValidPairAverage, "En yakın iki grubun ortalaması"),
        new(AdvanceSelectionMode.MedianOfValidGroups, "Medyan")
    ];
    private readonly SettingsService _settingsService;
    private readonly LogService _logService;
    private readonly OcrProfile _originalOcrProfile;
    private readonly NumericUpDown _screenNumber = NewNumber(0, 32);
    private readonly NumericUpDown _readingInterval = NewNumber(1, 3600);
    private readonly NumericUpDown _inactivityWarning = NewNumber(1, 86400);
    private readonly NumericUpDown _paddingLeft = NewNumber(0, 100);
    private readonly NumericUpDown _paddingRight = NewNumber(0, 100);
    private readonly NumericUpDown _paddingTop = NewNumber(0, 100);
    private readonly NumericUpDown _paddingBottom = NewNumber(0, 100);
    private readonly NumericUpDown _nominalMin = NewNumber(1, 12);
    private readonly NumericUpDown _nominalMax = NewNumber(1, 12);
    private readonly NumericUpDown _scaleMin = NewNumber(1, 12);
    private readonly NumericUpDown _scaleMax = NewNumber(1, 12);
    private readonly NumericUpDown _gateMin = NewNumber(1, 12);
    private readonly NumericUpDown _gateMax = NewNumber(1, 12);
    private readonly NumericUpDown _gatePaddingLeft = NewNumber(0, 50);
    private readonly NumericUpDown _gatePaddingRight = NewNumber(0, 50);
    private readonly NumericUpDown _gatePaddingTop = NewNumber(0, 50);
    private readonly NumericUpDown _gatePaddingBottom = NewNumber(0, 50);
    private readonly NumericUpDown _gateMaximumValue = NewNumber(1, 1_000_000);
    private readonly NumericUpDown _gateChangeTolerance = NewNumber(0, 100_000);
    private readonly NumericUpDown _gateConfirmationReadings = NewNumber(2, 10);
    private readonly NumericUpDown _gateMaximumChange = NewNumber(0, 100_000);
    private readonly CheckBox _enableGateCharacterGeometry = NewCheckBox("Kapak karakter geometri doğrulamasını kullan");
    private readonly CheckBox _enableGateDigitTemplates = NewCheckBox("Kapak rakam şablon eşleştiricisini kullan");
    private readonly NumericUpDown _gateTemplateMinimumScore = NewNumber(0, 100);
    private readonly NumericUpDown _gateTemplateAuthoritativeScore = NewNumber(0, 100);
    private readonly NumericUpDown _gateTemplateMinimumMargin = NewNumber(0, 100);
    private readonly NumericUpDown _gateMaximumInkLoss = NewNumber(0, 100);
    private readonly NumericUpDown _gateMinimumTemplateCount = NewNumber(1, 100);
    private readonly CheckBox _enableScaleCharacterOcr = NewCheckBox("Kantar karakter ve rakam şablonlarını kullan");
    private readonly NumericUpDown _scale1TemplateMinimumScore = NewNumber(0, 100);
    private readonly NumericUpDown _scale1TemplateAuthoritativeScore = NewNumber(0, 100);
    private readonly NumericUpDown _scale1TemplateMinimumMargin = NewNumber(0, 100);
    private readonly NumericUpDown _scale2TemplateMinimumScore = NewNumber(0, 100);
    private readonly NumericUpDown _scale2TemplateAuthoritativeScore = NewNumber(0, 100);
    private readonly NumericUpDown _scale2TemplateMinimumMargin = NewNumber(0, 100);
    private readonly NumericUpDown _scaleMaximumInkLoss = NewNumber(0, 100);
    private readonly NumericUpDown _nominalMaximumChange = NewNumber(0, 1_000_000);
    private readonly NumericUpDown _scale1MaximumChange = NewNumber(0, 1_000_000);
    private readonly NumericUpDown _scale2MaximumChange = NewNumber(0, 1_000_000);
    private readonly NumericUpDown _suspiciousConfirmationReadings = NewNumber(2, 10);
    private readonly NumericUpDown _suspiciousConfirmationTolerance = NewNumber(0, 100_000);
    private readonly NumericUpDown _advanceMaximumChange = NewNumber(0, 2500);
    private readonly NumericUpDown _advanceConfirmationReadings = NewNumber(2, 10);
    private readonly NumericUpDown _advanceConfirmationTolerance = NewNumber(0, 2500);
    private readonly NumericUpDown _stableZoneMinimum = NewNumber(0, 2500);
    private readonly NumericUpDown _stableZoneMaximum = NewNumber(0, 2500);
    private readonly NumericUpDown _stableZoneConfirmations = NewNumber(2, 10);
    private readonly ComboBox _advanceSelectionMode = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _advanceEnabledGroups = new();
    private readonly ComboBox _advancePrimaryGroup = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _advanceMinimumValid = NewNumber(0, 2500);
    private readonly NumericUpDown _advanceMaximumGroupDifference = NewNumber(0, 2500);
    private readonly NumericUpDown _advanceMinimumGroups = NewNumber(1, 4);
    private readonly NumericUpDown _advanceZeroWarningReadings = NewNumber(1, 100);
    private readonly CheckBox _enableStableParameterDiagnostics = NewCheckBox("Stabil parametre ayrıntılı tanılamasını etkinleştir (canlı OCR her zaman çalışır)");
    private readonly TextBox _machineProfileName = new();
    private readonly Dictionary<string, RegionInputs> _regions = [];
    private readonly TextBox _csvFolder = new();
    private readonly CheckBox _saveScreenshots = NewCheckBox("Ekran görüntülerini sakla");
    private readonly CheckBox _showOcrPreview = NewCheckBox("OCR ön izlemesini göster");
    private readonly CheckBox _demoMode = NewCheckBox("Demo Modu (OCR çalışmaz)");
    private readonly CheckBox _enableMismatchCapture = NewCheckBox("OCR uyuşmazlık örneklerini kaydet");
    private readonly NumericUpDown _mismatchCooldown = NewNumber(0, 86400);
    private readonly NumericUpDown _mismatchMaximumPerField = NewNumber(1, 100000);
    private readonly NumericUpDown _mismatchMaximumTotalMb = NewNumber(1, 100000);
    private readonly CheckBox _enableOcrCache = NewCheckBox("Değişmeyen görüntülerde OCR önbelleğini kullan");
    private readonly NumericUpDown _ocrSimilarityThreshold = NewNumber(0, 100, 1);
    private readonly NumericUpDown _gateCacheAge = NewNumber(1, 3600);
    private readonly NumericUpDown _numericCacheAge = NewNumber(1, 3600);
    private readonly CheckBox _showPerformancePanel = NewCheckBox("Performans panelini göster");
    private readonly NumericUpDown _performanceInterval = NewNumber(1, 10);
    private readonly CheckBox _enablePerformanceCsv = NewCheckBox("Performans CSV kaydını etkinleştir");
    private readonly NumericUpDown _performanceCsvInterval = NewNumber(1, 3600);
    private readonly NumericUpDown _warningCpu = NewNumber(1, 100);
    private readonly NumericUpDown _criticalCpu = NewNumber(1, 100);
    private readonly NumericUpDown _warningLatency = NewNumber(1, 60000);
    private readonly NumericUpDown _criticalLatency = NewNumber(1, 60000);

    public SettingsForm(SettingsService settingsService, AppSettings settings, LogService logService)
    {
        _settingsService = settingsService;
        _logService = logService;
        _originalOcrProfile = settings.OcrProfile;
        _advanceSelectionMode.DisplayMember = "Value"; _advanceSelectionMode.ValueMember = "Key";
        _advanceSelectionMode.DataSource = AdvanceSelectionOptions.ToList();
        _advancePrimaryGroup.Items.AddRange(["A", "B", "C", "D"]);
        StyleTextBox(_advanceEnabledGroups);
        Text = "TBM Watchdog — Ayarlar";
        BackColor = Color.FromArgb(15, 23, 42);
        ForeColor = Color.FromArgb(226, 232, 240);
        Font = new Font("Segoe UI", 9.5F);
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(790, 690);
        Size = new Size(880, 790);
        AutoScaleMode = AutoScaleMode.Dpi;

        Controls.Add(CreateLayout());
        Populate(settings);
    }

    public AppSettings? SavedSettings { get; private set; }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var inputs in _regions.Values) inputs.Dispose();
        }

        base.Dispose(disposing);
    }

    private Control CreateLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(24)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
        root.Controls.Add(CreateHeading(), 0, 0);
        root.Controls.Add(CreateScrollableContent(), 0, 1);
        root.Controls.Add(CreateActions(), 0, 2);
        return root;
    }

    private static Control CreateHeading()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        panel.Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 21F),
            ForeColor = Color.FromArgb(248, 250, 252),
            Location = new Point(0, 0),
            Text = "Ayarlar"
        });
        panel.Controls.Add(new Label
        {
            AutoSize = true,
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(3, 44),
            Text = "İzleme, ekran bölgeleri ve kayıt seçeneklerini yapılandırın."
        });
        return panel;
    }

    private Control CreateScrollableContent()
    {
        var scroll = new Panel { AutoScroll = true, Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 41, 59), Padding = new Padding(18) };
        var content = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Dock = DockStyle.Top,
            Padding = new Padding(6)
        };
        content.Controls.Add(CreateGeneralSection());
        content.Controls.Add(CreateRegionsSection());
        content.Controls.Add(CreateOutputSection());
        scroll.Controls.Add(content);
        return scroll;
    }

    private Control CreateGeneralSection()
    {
        var grid = NewSection("GENEL İZLEME", 2);
        AddSettingRow(grid, 1, "Ekran numarası", _screenNumber);
        AddSettingRow(grid, 2, "Okuma sıklığı (saniye)", _readingInterval);
        AddSettingRow(grid, 3, "Hareketsizlik uyarısı (saniye)", _inactivityWarning);
        AddSettingRow(grid, 4, "OCR padding sol / sağ", Pair(_paddingLeft, _paddingRight));
        AddSettingRow(grid, 5, "OCR padding üst / alt", Pair(_paddingTop, _paddingBottom));
        AddSettingRow(grid, 6, "Nominal karakter min / max", Pair(_nominalMin, _nominalMax));
        AddSettingRow(grid, 7, "Kantar karakter min / max", Pair(_scaleMin, _scaleMax));
        AddSettingRow(grid, 8, "Kapak karakter min / max", Pair(_gateMin, _gateMax));
        AddSettingRow(grid, 9, "Kapak padding sol / sağ", Pair(_gatePaddingLeft, _gatePaddingRight));
        AddSettingRow(grid, 10, "Kapak padding üst / alt", Pair(_gatePaddingTop, _gatePaddingBottom));
        AddSettingRow(grid, 11, "Kapak maksimum değeri", _gateMaximumValue);
        AddSettingRow(grid, 12, "Kapak sıçrama toleransı", _gateChangeTolerance);
        AddSettingRow(grid, 13, "Kapak onay okuma sayısı", _gateConfirmationReadings);
        AddSettingRow(grid, 14, "Kapak okuma başı maksimum değişim", _gateMaximumChange);
        AddSettingRow(grid, 15, "Kapak karakter geometrisi", _enableGateCharacterGeometry);
        AddSettingRow(grid, 16, "Kapak rakam şablonları", _enableGateDigitTemplates);
        AddSettingRow(grid, 17, "Şablon minimum güveni (%)", _gateTemplateMinimumScore);
        AddSettingRow(grid, 18, "Şablon otoriter güveni (%)", _gateTemplateAuthoritativeScore);
        AddSettingRow(grid, 19, "Şablon minimum farkı (%)", _gateTemplateMinimumMargin);
        AddSettingRow(grid, 20, "Maksimum mürekkep kaybı (%)", _gateMaximumInkLoss);
        AddSettingRow(grid, 21, "Rakam başına önerilen şablon", _gateMinimumTemplateCount);
        AddSettingRow(grid, 22, "Kantar karakter OCR", _enableScaleCharacterOcr);
        AddSettingRow(grid, 23, "Kantar 1 / 2 minimum güveni (%)", Pair(_scale1TemplateMinimumScore, _scale2TemplateMinimumScore));
        AddSettingRow(grid, 24, "Kantar 1 / 2 otoriter güveni (%)", Pair(_scale1TemplateAuthoritativeScore, _scale2TemplateAuthoritativeScore));
        AddSettingRow(grid, 25, "Kantar 1 / 2 minimum farkı (%)", Pair(_scale1TemplateMinimumMargin, _scale2TemplateMinimumMargin));
        AddSettingRow(grid, 26, "Kantar maksimum mürekkep kaybı (%)", _scaleMaximumInkLoss);
        AddSettingRow(grid, 27, "Nominal maksimum okuma değişimi", _nominalMaximumChange);
        AddSettingRow(grid, 28, "Kantar 1 / 2 maksimum okuma değişimi", Pair(_scale1MaximumChange, _scale2MaximumChange));
        AddSettingRow(grid, 29, "Şüpheli değer doğrulama sayısı", _suspiciousConfirmationReadings);
        AddSettingRow(grid, 30, "Şüpheli değer doğrulama toleransı", _suspiciousConfirmationTolerance);
        AddSettingRow(grid, 49, "İlerleme maksimum değişim / tolerans", Pair(_advanceMaximumChange, _advanceConfirmationTolerance));
        AddSettingRow(grid, 50, "İlerleme doğrulama okuması", _advanceConfirmationReadings);
        AddSettingRow(grid, 51, "Stabil bölge min / max (mm)", Pair(_stableZoneMinimum, _stableZoneMaximum));
        AddSettingRow(grid, 52, "Stabil bölge doğrulama okuması", _stableZoneConfirmations);
        AddSettingRow(grid, 53, "İlerleme kaynağı", _advanceSelectionMode);
        AddSettingRow(grid, 54, "Etkin ilerleme grupları (A,C)", _advanceEnabledGroups);
        AddSettingRow(grid, 55, "Birincil ilerleme grubu", _advancePrimaryGroup);
        AddSettingRow(grid, 56, "Minimum geçerli değer / grup farkı", Pair(_advanceMinimumValid, _advanceMaximumGroupDifference));
        AddSettingRow(grid, 57, "Minimum geçerli grup / sıfır uyarısı", Pair(_advanceMinimumGroups, _advanceZeroWarningReadings));
        AddSettingRow(grid, 58, "Stabil parametre ayrıntılı tanılaması", _enableStableParameterDiagnostics);
        AddSettingRow(grid, 31, "Uyuşmazlık örneği kaydı", _enableMismatchCapture);
        AddSettingRow(grid, 32, "Uyuşmazlık tekrar bekleme süresi (sn)", _mismatchCooldown);
        AddSettingRow(grid, 33, "Alan başına maksimum uyuşmazlık örneği", _mismatchMaximumPerField);
        AddSettingRow(grid, 34, "Uyuşmazlık klasörü maksimum boyutu (MB)", _mismatchMaximumTotalMb);
        var openMismatchFolder = new Button { Text = "Uyuşmazlık klasörünü aç", Dock = DockStyle.Fill };
        openMismatchFolder.Click += (_, _) =>
        {
            var folder = Path.Combine(AppContext.BaseDirectory, "Diagnostics", "MismatchSamples");
            Directory.CreateDirectory(folder);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", folder) { UseShellExecute = true });
        };
        AddSettingRow(grid, 35, "Tanılama örnekleri", openMismatchFolder);
        AddSettingRow(grid, 36, "OCR görüntü önbelleği", _enableOcrCache);
        AddSettingRow(grid, 37, "Görüntü benzerlik eşiği (%)", _ocrSimilarityThreshold);
        AddSettingRow(grid, 38, "Gate maksimum önbellek yaşı (sn)", _gateCacheAge);
        AddSettingRow(grid, 39, "Nominal/Kantar maksimum önbellek yaşı (sn)", _numericCacheAge);
        var clearOcrCache = new Button { Text = "OCR önbelleğini temizle", Dock = DockStyle.Fill };
        clearOcrCache.Click += (_, _) => { OcrCacheControl.RequestClear(); MessageBox.Show(this, "OCR önbelleği temizlendi."); };
        AddSettingRow(grid, 40, "Önbellek işlemi", clearOcrCache);
        AddSettingRow(grid, 42, "Performans paneli", _showPerformancePanel);
        AddSettingRow(grid, 43, "Performans ölçüm aralığı (sn)", _performanceInterval);
        AddSettingRow(grid, 44, "Performans CSV", _enablePerformanceCsv);
        AddSettingRow(grid, 45, "Performans CSV aralığı (sn)", _performanceCsvInterval);
        AddSettingRow(grid, 46, "CPU uyarı / kritik eşiği (%)", Pair(_warningCpu, _criticalCpu));
        AddSettingRow(grid, 47, "Gecikme uyarı / kritik eşiği (ms)", Pair(_warningLatency, _criticalLatency));
        var performanceActions = new FlowLayoutPanel { Dock = DockStyle.Fill };
        var resetPerformance = new Button { Text = "Performans verilerini sıfırla", AutoSize = true };
        resetPerformance.Click += (_, _) => PerformanceMonitorControl.RequestReset();
        var openPerformance = new Button { Text = "Performans klasörünü aç", AutoSize = true };
        openPerformance.Click += (_, _) => { var folder = Path.Combine(AppContext.BaseDirectory, "Diagnostics"); Directory.CreateDirectory(folder); System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", folder) { UseShellExecute = true }); };
        performanceActions.Controls.Add(resetPerformance); performanceActions.Controls.Add(openPerformance);
        AddSettingRow(grid, 48, "Performans işlemleri", performanceActions);
        StyleTextBox(_machineProfileName);
        AddSettingRow(grid, 41, "Makine / OCR profil adı", _machineProfileName);
        return grid;
    }

    private Control CreateRegionsSection()
    {
        var grid = NewSection("EKRAN BÖLGELERİ", 7);
        var headers = new[] { "Alan", "X", "Y", "Genişlik", "Yükseklik", "Ön İzleme", "" };
        for (var column = 0; column < headers.Length; column++)
        {
            grid.Controls.Add(NewMutedLabel(headers[column]), column, 1);
        }

        AddRegionRow(grid, 2, "Nominal", "Nominal");
        AddRegionRow(grid, 3, "Kantar 1", "Scale1");
        AddRegionRow(grid, 4, "Kantar 2", "Scale2");
        AddRegionRow(grid, 5, "Kapak mm", "Gate");
        AddRegionRow(grid, 6, "Çalışma Modu", "OperationMode");
        _regions.Add("AdvanceMm", new RegionInputs()); // Eski JSON ile uyumluluk; 1031 UI'da gösterilmez.
        AddRegionRow(grid, 7, "İlerleme Grup A", "AdvanceGroupA"); AddRegionRow(grid, 8, "İlerleme Grup B", "AdvanceGroupB");
        AddRegionRow(grid, 9, "İlerleme Grup C", "AdvanceGroupC"); AddRegionRow(grid, 10, "İlerleme Grup D", "AdvanceGroupD");
        AddRegionRow(grid, 11, "RPM", "StableRpm"); AddRegionRow(grid, 12, "Tork", "StableTorque");
        AddRegionRow(grid, 13, "İtme Kuvveti", "StableThrustForce"); AddRegionRow(grid, 14, "İlerleme Hızı", "StableAdvanceSpeed");
        AddRegionRow(grid, 15, "Penetrasyon", "StablePenetration");
        AddRegionRow(grid, 16, "Nominal Enjeksiyon", "NominalInjection");
        AddRegionRow(grid, 17, "Gerçek Enjeksiyon", "RealInjection");
        AddRegionRow(grid, 18, "EPB 1 (Sensör 1)", "Epb1"); AddRegionRow(grid, 19, "EPB 2 (Sensör 2)", "Epb2");
        AddRegionRow(grid, 20, "EPB 3 (Sensör 3)", "Epb3"); AddRegionRow(grid, 21, "EPB 4 (Sensör 4)", "Epb4");
        AddRegionRow(grid, 22, "EPB 5 (Sensör 5)", "Epb5");
        return grid;
    }

    private Control CreateOutputSection()
    {
        var grid = NewSection("KAYIT VE GÖRÜNTÜLEME", 2);
        var folderPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Height = 36 };
        folderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        folderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105F));
        StyleTextBox(_csvFolder);
        _csvFolder.Dock = DockStyle.Fill;
        var browse = NewButton("Gözat…", Color.FromArgb(51, 65, 85), 95);
        browse.Click += Browse_Click;
        folderPanel.Controls.Add(_csvFolder, 0, 0);
        folderPanel.Controls.Add(browse, 1, 0);
        AddSettingRow(grid, 1, "CSV kayıt klasörü", folderPanel);
        grid.Controls.Add(_saveScreenshots, 0, 2);
        grid.SetColumnSpan(_saveScreenshots, 2);
        grid.Controls.Add(_showOcrPreview, 0, 3);
        grid.SetColumnSpan(_showOcrPreview, 2);
        grid.Controls.Add(_demoMode, 0, 4);
        grid.SetColumnSpan(_demoMode, 2);
        return grid;
    }

    private Control CreateActions()
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 12, 0, 0) };
        var save = NewButton("Kaydet", Color.FromArgb(2, 132, 199), 120);
        var cancel = NewButton("Vazgeç", Color.FromArgb(51, 65, 85), 100);
        save.Click += Save_Click;
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        panel.Controls.Add(save);
        panel.Controls.Add(cancel);
        return panel;
    }

    private void Populate(AppSettings settings)
    {
        _screenNumber.Value = Clamp(settings.ScreenNumber, _screenNumber);
        _readingInterval.Value = Clamp(settings.ReadingIntervalSeconds, _readingInterval);
        _inactivityWarning.Value = Clamp(settings.InactivityWarningSeconds, _inactivityWarning);
        var profile = settings.OcrProfile ?? new OcrProfile();
        _machineProfileName.Text = profile.Name;
        _paddingLeft.Value = Clamp(profile.PaddingLeft, _paddingLeft);
        _paddingRight.Value = Clamp(profile.PaddingRight, _paddingRight);
        _paddingTop.Value = Clamp(profile.PaddingTop, _paddingTop);
        _paddingBottom.Value = Clamp(profile.PaddingBottom, _paddingBottom);
        _nominalMin.Value = Clamp(profile.NominalLength.Minimum, _nominalMin);
        _nominalMax.Value = Clamp(profile.NominalLength.Maximum, _nominalMax);
        _scaleMin.Value = Clamp(profile.ScaleLength.Minimum, _scaleMin);
        _scaleMax.Value = Clamp(profile.ScaleLength.Maximum, _scaleMax);
        _gateMin.Value = Clamp(profile.GateLength.Minimum, _gateMin);
        _gateMax.Value = Clamp(profile.GateLength.Maximum, _gateMax);
        _gatePaddingLeft.Value = Clamp(profile.GatePaddingLeft, _gatePaddingLeft);
        _gatePaddingRight.Value = Clamp(profile.GatePaddingRight, _gatePaddingRight);
        _gatePaddingTop.Value = Clamp(profile.GatePaddingTop, _gatePaddingTop);
        _gatePaddingBottom.Value = Clamp(profile.GatePaddingBottom, _gatePaddingBottom);
        _gateMaximumValue.Value = Math.Clamp(profile.GateMaximumValue, _gateMaximumValue.Minimum, _gateMaximumValue.Maximum);
        _gateChangeTolerance.Value = Math.Clamp(profile.GateChangeTolerance, _gateChangeTolerance.Minimum, _gateChangeTolerance.Maximum);
        _gateConfirmationReadings.Value = Clamp(profile.GateConfirmationReadings, _gateConfirmationReadings);
        _gateMaximumChange.Value = Math.Clamp(profile.GateMaximumChangePerReading, _gateMaximumChange.Minimum, _gateMaximumChange.Maximum);
        _enableGateCharacterGeometry.Checked = profile.EnableGateCharacterGeometry;
        _enableGateDigitTemplates.Checked = profile.EnableGateDigitTemplateMatcher;
        _gateTemplateMinimumScore.Value = Math.Clamp((decimal)(profile.GateTemplateMinimumScore * 100), 0, 100);
        _gateTemplateAuthoritativeScore.Value = Math.Clamp((decimal)(profile.GateTemplateAuthoritativeScore * 100), 0, 100);
        _gateTemplateMinimumMargin.Value = Math.Clamp((decimal)(profile.GateTemplateMinimumMargin * 100), 0, 100);
        _gateMaximumInkLoss.Value = Math.Clamp((decimal)(profile.GateMaximumAllowedInkLoss * 100), 0, 100);
        _gateMinimumTemplateCount.Value = Math.Clamp(profile.GateMinimumRecommendedTemplatesPerDigit, 1, 100);
        _enableScaleCharacterOcr.Checked = profile.EnableScaleCharacterOcr;
        _scale1TemplateMinimumScore.Value = Math.Clamp((decimal)(profile.Scale1TemplateMinimumScore * 100), 0, 100);
        _scale1TemplateAuthoritativeScore.Value = Math.Clamp((decimal)(profile.Scale1TemplateAuthoritativeScore * 100), 0, 100);
        _scale1TemplateMinimumMargin.Value = Math.Clamp((decimal)(profile.Scale1TemplateMinimumMargin * 100), 0, 100);
        _scale2TemplateMinimumScore.Value = Math.Clamp((decimal)(profile.Scale2TemplateMinimumScore * 100), 0, 100);
        _scale2TemplateAuthoritativeScore.Value = Math.Clamp((decimal)(profile.Scale2TemplateAuthoritativeScore * 100), 0, 100);
        _scale2TemplateMinimumMargin.Value = Math.Clamp((decimal)(profile.Scale2TemplateMinimumMargin * 100), 0, 100);
        _scaleMaximumInkLoss.Value = Math.Clamp((decimal)(profile.ScaleMaximumAllowedInkLoss * 100), 0, 100);
        _nominalMaximumChange.Value = Math.Clamp(profile.NominalMaximumChangePerReading, 0, 1_000_000);
        _scale1MaximumChange.Value = Math.Clamp(profile.Scale1MaximumChangePerReading, 0, 1_000_000);
        _scale2MaximumChange.Value = Math.Clamp(profile.Scale2MaximumChangePerReading, 0, 1_000_000);
        _suspiciousConfirmationReadings.Value = Math.Clamp(profile.SuspiciousValueConfirmationReadings, 2, 10);
        _suspiciousConfirmationTolerance.Value = Math.Clamp(profile.SuspiciousValueConfirmationTolerance, 0, 100_000);
        _advanceMaximumChange.Value = Math.Clamp(profile.AdvanceMaximumChangePerReading, 0, 2500);
        _advanceConfirmationReadings.Value = Math.Clamp(profile.AdvanceConfirmationReadings, 2, 10);
        _advanceConfirmationTolerance.Value = Math.Clamp(profile.AdvanceConfirmationTolerance, 0, 2500);
        _stableZoneMinimum.Value = Math.Clamp(profile.StableZoneMinimumAdvanceMm, 0, 2500);
        _stableZoneMaximum.Value = Math.Clamp(profile.StableZoneMaximumAdvanceMm, 0, 2500);
        _stableZoneConfirmations.Value = Math.Clamp(profile.StableZoneConfirmationReadings, 2, 10);
        _advanceSelectionMode.SelectedValue = profile.AdvanceSelectionMode;
        _advanceEnabledGroups.Text = string.Join(',', profile.AdvanceEnabledGroups);
        _advancePrimaryGroup.SelectedItem = profile.AdvancePrimaryGroup;
        _advanceMinimumValid.Value = Math.Clamp(profile.AdvanceMinimumValidValue, 0, 2500);
        _advanceMaximumGroupDifference.Value = Math.Clamp(profile.AdvanceGroupAgreementToleranceMm, 0, 2500);
        _advanceMinimumGroups.Value = Math.Clamp(profile.MinimumConsistentAdvanceGroups, 2, 4);
        _advanceZeroWarningReadings.Value = Math.Clamp(profile.AdvanceSensorZeroWarningReadings, 1, 100);
        _enableStableParameterDiagnostics.Checked = settings.EnableStableParameterDiagnostics;
        _enableMismatchCapture.Checked = settings.EnableMismatchSampleCapture;
        _mismatchCooldown.Value = Math.Clamp(settings.MismatchSampleCooldownSeconds, 0, 86400);
        _mismatchMaximumPerField.Value = Math.Clamp(settings.MismatchSampleMaximumFilesPerField, 1, 100000);
        _mismatchMaximumTotalMb.Value = Math.Clamp(settings.MismatchSampleMaximumTotalSizeMb, 1, 100000);
        _enableOcrCache.Checked = settings.EnableOcrImageCache;
        _ocrSimilarityThreshold.Value = Math.Clamp((decimal)settings.OcrImageSimilarityThresholdPercent, 0, 100);
        _gateCacheAge.Value = Math.Clamp(settings.GateOcrCacheMaximumAgeSeconds, 1, 3600);
        _numericCacheAge.Value = Math.Clamp(settings.NumericFieldOcrCacheMaximumAgeSeconds, 1, 3600);
        _showPerformancePanel.Checked = settings.ShowPerformancePanel;
        _performanceInterval.Value = Math.Clamp(settings.PerformanceMonitorIntervalSeconds, 1, 10);
        _enablePerformanceCsv.Checked = settings.EnablePerformanceCsv;
        _performanceCsvInterval.Value = Math.Clamp(settings.PerformanceCsvIntervalSeconds, 1, 3600);
        _warningCpu.Value = Math.Clamp((decimal)settings.WarningProcessCpuPercent, 1, 100);
        _criticalCpu.Value = Math.Clamp((decimal)settings.CriticalProcessCpuPercent, 1, 100);
        _warningLatency.Value = Math.Clamp((decimal)settings.WarningAverageLatencyMs, 1, 60000);
        _criticalLatency.Value = Math.Clamp((decimal)settings.CriticalAverageLatencyMs, 1, 60000);
        _regions["Nominal"].Populate(settings.NominalRegion);
        _regions["Scale1"].Populate(settings.Scale1Region);
        _regions["Scale2"].Populate(settings.Scale2Region);
        _regions["Gate"].Populate(settings.GateRegion);
        _regions["OperationMode"].Populate(settings.OperationModeRegion);
        _regions["AdvanceMm"].Populate(settings.AdvanceMmRegion);
        _regions["AdvanceGroupA"].Populate(settings.AdvanceGroupARegion); _regions["AdvanceGroupB"].Populate(settings.AdvanceGroupBRegion);
        _regions["AdvanceGroupC"].Populate(settings.AdvanceGroupCRegion); _regions["AdvanceGroupD"].Populate(settings.AdvanceGroupDRegion);
        _regions["StableRpm"].Populate(settings.StableRpmRegion); _regions["StableTorque"].Populate(settings.StableTorqueRegion);
        _regions["StableThrustForce"].Populate(settings.StableThrustForceRegion); _regions["StableAdvanceSpeed"].Populate(settings.StableAdvanceSpeedRegion);
        _regions["StablePenetration"].Populate(settings.StablePenetrationRegion);
        _regions["NominalInjection"].Populate(settings.NominalInjectionRegion);
        _regions["RealInjection"].Populate(settings.RealInjectionRegion);
        _regions["Epb1"].Populate(settings.Epb1Region); _regions["Epb2"].Populate(settings.Epb2Region);
        _regions["Epb3"].Populate(settings.Epb3Region); _regions["Epb4"].Populate(settings.Epb4Region);
        _regions["Epb5"].Populate(settings.Epb5Region);
        _csvFolder.Text = settings.CsvOutputFolder;
        _saveScreenshots.Checked = settings.SaveScreenshots;
        _showOcrPreview.Checked = settings.ShowOcrPreview;
        _demoMode.Checked = settings.DemoMode;
    }

    private void Save_Click(object? sender, EventArgs e)
    {
        try
        {
            var settings = new AppSettings
            {
                ScreenNumber = decimal.ToInt32(_screenNumber.Value),
                ReadingIntervalSeconds = decimal.ToInt32(_readingInterval.Value),
                InactivityWarningSeconds = decimal.ToInt32(_inactivityWarning.Value),
                OcrPaddingPixels = 4,
                OcrProfile = BuildOcrProfile(),
                NominalRegion = _regions["Nominal"].ToRegion(),
                Scale1Region = _regions["Scale1"].ToRegion(),
                Scale2Region = _regions["Scale2"].ToRegion(),
                GateRegion = _regions["Gate"].ToRegion(),
                OperationModeRegion = _regions["OperationMode"].ToRegion(),
                AdvanceMmRegion = _regions["AdvanceMm"].ToRegion(),
                AdvanceGroupARegion = _regions["AdvanceGroupA"].ToRegion(), AdvanceGroupBRegion = _regions["AdvanceGroupB"].ToRegion(),
                AdvanceGroupCRegion = _regions["AdvanceGroupC"].ToRegion(), AdvanceGroupDRegion = _regions["AdvanceGroupD"].ToRegion(),
                StableRpmRegion = _regions["StableRpm"].ToRegion(), StableTorqueRegion = _regions["StableTorque"].ToRegion(),
                StableThrustForceRegion = _regions["StableThrustForce"].ToRegion(), StableAdvanceSpeedRegion = _regions["StableAdvanceSpeed"].ToRegion(),
                StablePenetrationRegion = _regions["StablePenetration"].ToRegion(), EnableStableParameterDiagnostics = _enableStableParameterDiagnostics.Checked,
                NominalInjectionRegion = _regions["NominalInjection"].ToRegion(),
                RealInjectionRegion = _regions["RealInjection"].ToRegion(),
                Epb1Region = _regions["Epb1"].ToRegion(), Epb2Region = _regions["Epb2"].ToRegion(),
                Epb3Region = _regions["Epb3"].ToRegion(), Epb4Region = _regions["Epb4"].ToRegion(),
                Epb5Region = _regions["Epb5"].ToRegion(),
                CsvOutputFolder = _csvFolder.Text.Trim(),
                SaveScreenshots = _saveScreenshots.Checked,
                ShowOcrPreview = _showOcrPreview.Checked,
                EnableMismatchSampleCapture = _enableMismatchCapture.Checked,
                MismatchSampleCooldownSeconds = decimal.ToInt32(_mismatchCooldown.Value),
                MismatchSampleMaximumFilesPerField = decimal.ToInt32(_mismatchMaximumPerField.Value),
                MismatchSampleMaximumTotalSizeMb = decimal.ToInt32(_mismatchMaximumTotalMb.Value),
                EnableOcrImageCache = _enableOcrCache.Checked,
                OcrImageSimilarityThresholdPercent = decimal.ToDouble(_ocrSimilarityThreshold.Value),
                GateOcrCacheMaximumAgeSeconds = decimal.ToInt32(_gateCacheAge.Value),
                NumericFieldOcrCacheMaximumAgeSeconds = decimal.ToInt32(_numericCacheAge.Value),
                ShowPerformancePanel = _showPerformancePanel.Checked,
                PerformanceMonitorIntervalSeconds = decimal.ToInt32(_performanceInterval.Value),
                EnablePerformanceCsv = _enablePerformanceCsv.Checked,
                PerformanceCsvIntervalSeconds = decimal.ToInt32(_performanceCsvInterval.Value),
                WarningProcessCpuPercent = decimal.ToDouble(_warningCpu.Value),
                CriticalProcessCpuPercent = decimal.ToDouble(_criticalCpu.Value),
                WarningAverageLatencyMs = decimal.ToDouble(_warningLatency.Value),
                CriticalAverageLatencyMs = decimal.ToDouble(_criticalLatency.Value),
                DemoMode = _demoMode.Checked
            };
            _settingsService.Save(settings);
            OcrCacheControl.RequestClear();
            _logService.Information("Ayarlar kaydedildi.");
            SavedSettings = settings;
            DialogResult = DialogResult.OK;
        }
        catch (SettingsException exception)
        {
            _logService.Warning("Ayarlar kaydedilemedi.", exception);
            MessageBox.Show(this, exception.Message, "Ayarlar Kaydedilemedi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void Browse_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "CSV kayıtlarının yazılacağı klasörü seçin",
            SelectedPath = Directory.Exists(_csvFolder.Text) ? _csvFolder.Text : string.Empty,
            ShowNewFolderButton = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK) _csvFolder.Text = dialog.SelectedPath;
    }

    private void AddRegionRow(TableLayoutPanel grid, int row, string label, string key)
    {
        var inputs = new RegionInputs();
        _regions.Add(key, inputs);
        grid.Controls.Add(NewPrimaryLabel(label), 0, row);
        grid.Controls.Add(inputs.X, 1, row);
        grid.Controls.Add(inputs.Y, 2, row);
        grid.Controls.Add(inputs.Width, 3, row);
        grid.Controls.Add(inputs.Height, 4, row);
        grid.Controls.Add(inputs.Preview, 5, row);
        var selectButton = NewButton("Ekrandan Seç", Color.FromArgb(3, 105, 161), 108);
        selectButton.Margin = new Padding(4);
        selectButton.Click += async (_, _) => await SelectRegionAsync(key, label, inputs);
        grid.Controls.Add(selectButton, 6, row);
    }

    private async Task SelectRegionAsync(string key, string label, RegionInputs inputs)
    {
        var owner = Owner;
        var settingsOpacity = Opacity;
        var ownerOpacity = owner?.Opacity ?? 1D;
        try
        {
            Opacity = 0;
            if (owner is not null) owner.Opacity = 0;
            await Task.Delay(500);

            using var selector = new RegionSelectorForm(
                decimal.ToInt32(_screenNumber.Value),
                inputs.ToRegion(),
                label,
                BuildOcrProfile());
            if (selector.ShowDialog() == DialogResult.OK && selector.SelectedRegion is not null)
            {
                inputs.Populate(selector.SelectedRegion);
                if (selector.SelectedPreview is not null) inputs.SetPreview(selector.SelectedPreview);
                _logService.Information($"{label} ekran bölgesi seçildi: X={selector.SelectedRegion.X}, Y={selector.SelectedRegion.Y}, G={selector.SelectedRegion.Width}, Yük={selector.SelectedRegion.Height}.");
            }
        }
        catch (Exception exception) when (exception is ScreenCaptureException or System.ComponentModel.Win32Exception or ArgumentException)
        {
            _logService.Warning($"{label} ekran bölgesi seçilemedi.", exception);
            MessageBox.Show(this, exception.Message, "Bölge Seçilemedi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            if (owner is not null && !owner.IsDisposed) owner.Opacity = ownerOpacity;
            if (!IsDisposed)
            {
                Opacity = settingsOpacity;
                Activate();
                BringToFront();
            }
        }
    }

    private static TableLayoutPanel NewSection(string title, int columns)
    {
        var grid = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = columns,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 22),
            Padding = new Padding(0, 0, 8, 8)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
        for (var index = 1; index < columns; index++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64F / (columns - 1)));
        grid.Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9F),
            ForeColor = Color.FromArgb(56, 189, 248),
            Margin = new Padding(0, 2, 0, 12),
            Text = title
        }, 0, 0);
        grid.SetColumnSpan(grid.GetControlFromPosition(0, 0)!, columns);
        return grid;
    }

    private static void AddSettingRow(TableLayoutPanel grid, int row, string label, Control input)
    {
        grid.Controls.Add(NewPrimaryLabel(label), 0, row);
        input.Margin = new Padding(6);
        input.Dock = DockStyle.Fill;
        grid.Controls.Add(input, 1, row);
    }

    private OcrProfile BuildOcrProfile() => new()
    {
        Name = string.IsNullOrWhiteSpace(_machineProfileName.Text) ? "Herrenknecht_Default" : _machineProfileName.Text.Trim(),
        PaddingLeft = decimal.ToInt32(_paddingLeft.Value), PaddingRight = decimal.ToInt32(_paddingRight.Value),
        PaddingTop = decimal.ToInt32(_paddingTop.Value), PaddingBottom = decimal.ToInt32(_paddingBottom.Value),
        GatePaddingLeft = decimal.ToInt32(_gatePaddingLeft.Value), GatePaddingRight = decimal.ToInt32(_gatePaddingRight.Value),
        GatePaddingTop = decimal.ToInt32(_gatePaddingTop.Value), GatePaddingBottom = decimal.ToInt32(_gatePaddingBottom.Value),
        GateMaximumValue = _gateMaximumValue.Value, GateChangeTolerance = _gateChangeTolerance.Value,
        GateConfirmationReadings = decimal.ToInt32(_gateConfirmationReadings.Value),
        GateMaximumChangePerReading = _gateMaximumChange.Value,
        EnableGateCharacterGeometry = _enableGateCharacterGeometry.Checked,
        EnableGateDigitTemplateMatcher = _enableGateDigitTemplates.Checked,
        GateTemplateMinimumScore = decimal.ToDouble(_gateTemplateMinimumScore.Value) / 100d,
        GateTemplateAuthoritativeScore = decimal.ToDouble(_gateTemplateAuthoritativeScore.Value) / 100d,
        GateTemplateMinimumMargin = decimal.ToDouble(_gateTemplateMinimumMargin.Value) / 100d,
        GateMaximumAllowedInkLoss = decimal.ToDouble(_gateMaximumInkLoss.Value) / 100d,
        GateMinimumRecommendedTemplatesPerDigit = decimal.ToInt32(_gateMinimumTemplateCount.Value),
        EnableScaleCharacterOcr = _enableScaleCharacterOcr.Checked,
        Scale1TemplateMinimumScore = decimal.ToDouble(_scale1TemplateMinimumScore.Value) / 100d,
        Scale1TemplateAuthoritativeScore = decimal.ToDouble(_scale1TemplateAuthoritativeScore.Value) / 100d,
        Scale1TemplateMinimumMargin = decimal.ToDouble(_scale1TemplateMinimumMargin.Value) / 100d,
        Scale2TemplateMinimumScore = decimal.ToDouble(_scale2TemplateMinimumScore.Value) / 100d,
        Scale2TemplateAuthoritativeScore = decimal.ToDouble(_scale2TemplateAuthoritativeScore.Value) / 100d,
        Scale2TemplateMinimumMargin = decimal.ToDouble(_scale2TemplateMinimumMargin.Value) / 100d,
        ScaleMaximumAllowedInkLoss = decimal.ToDouble(_scaleMaximumInkLoss.Value) / 100d,
        NominalMaximumChangePerReading = _nominalMaximumChange.Value,
        Scale1MaximumChangePerReading = _scale1MaximumChange.Value,
        Scale2MaximumChangePerReading = _scale2MaximumChange.Value,
        SuspiciousValueConfirmationReadings = decimal.ToInt32(_suspiciousConfirmationReadings.Value),
        SuspiciousValueConfirmationTolerance = _suspiciousConfirmationTolerance.Value,
        AdvanceMaximumChangePerReading = _advanceMaximumChange.Value,
        AdvanceConfirmationReadings = decimal.ToInt32(_advanceConfirmationReadings.Value),
        AdvanceConfirmationTolerance = _advanceConfirmationTolerance.Value,
        AdvanceMaximumBackwardToleranceMm = _originalOcrProfile.AdvanceMaximumBackwardToleranceMm,
        AdvanceLargeBackwardJumpMm = _originalOcrProfile.AdvanceLargeBackwardJumpMm,
        AdvanceMaximumForwardJumpPerRealReadingMm = _originalOcrProfile.AdvanceMaximumForwardJumpPerRealReadingMm,
        StableZoneMinimumAdvanceMm = _stableZoneMinimum.Value,
        StableZoneMaximumAdvanceMm = _stableZoneMaximum.Value,
        StableZoneConfirmationReadings = decimal.ToInt32(_stableZoneConfirmations.Value),
        AdvanceSelectionMode = _advanceSelectionMode.SelectedValue is AdvanceSelectionMode mode ? mode : AdvanceSelectionMode.GroupA,
        AdvanceEnabledGroups = _advanceEnabledGroups.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(value => value.ToUpperInvariant()).Where(value => value is "A" or "B" or "C" or "D").Distinct().DefaultIfEmpty("A").ToList(),
        AdvancePrimaryGroup = _advancePrimaryGroup.SelectedItem?.ToString() ?? "A",
        AdvanceMinimumValidValue = _advanceMinimumValid.Value,
        AdvanceMaximumAllowedGroupDifferenceMm = _advanceMaximumGroupDifference.Value,
        AdvanceMinimumRequiredValidGroups = decimal.ToInt32(_advanceMinimumGroups.Value),
        AdvanceGroupAgreementToleranceMm = _advanceMaximumGroupDifference.Value,
        MinimumConsistentAdvanceGroups = decimal.ToInt32(_advanceMinimumGroups.Value),
        AdvanceSingleGroupConfirmationReadings = decimal.ToInt32(_advanceConfirmationReadings.Value),
        AdvanceSensorZeroWarningReadings = decimal.ToInt32(_advanceZeroWarningReadings.Value),
        NominalLength = new OcrLengthRange(decimal.ToInt32(_nominalMin.Value), decimal.ToInt32(_nominalMax.Value)),
        ScaleLength = new OcrLengthRange(decimal.ToInt32(_scaleMin.Value), decimal.ToInt32(_scaleMax.Value)),
        GateLength = new OcrLengthRange(decimal.ToInt32(_gateMin.Value), decimal.ToInt32(_gateMax.Value))
    };

    private static Control Pair(Control first, Control second)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Height = 32 };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        first.Dock = DockStyle.Fill; second.Dock = DockStyle.Fill; panel.Controls.Add(first, 0, 0); panel.Controls.Add(second, 1, 0);
        return panel;
    }

    private static Label NewPrimaryLabel(string text) => new()
    {
        AutoSize = true,
        ForeColor = Color.FromArgb(226, 232, 240),
        Margin = new Padding(0, 10, 8, 8),
        Text = text
    };

    private static Label NewMutedLabel(string text) => new()
    {
        AutoSize = true,
        ForeColor = Color.FromArgb(148, 163, 184),
        Margin = new Padding(4, 4, 4, 8),
        Text = text
    };

    private static NumericUpDown NewNumber(int minimum, int maximum, int decimalPlaces = 0) => new()
    {
        BackColor = Color.FromArgb(15, 23, 42),
        BorderStyle = BorderStyle.FixedSingle,
        ForeColor = Color.FromArgb(241, 245, 249),
        Minimum = minimum,
        Maximum = maximum,
        DecimalPlaces = decimalPlaces,
        Increment = decimalPlaces > 0 ? 0.1m : 1m,
        Margin = new Padding(4),
        ThousandsSeparator = false
    };

    private static CheckBox NewCheckBox(string text) => new()
    {
        AutoSize = true,
        ForeColor = Color.FromArgb(226, 232, 240),
        Margin = new Padding(0, 8, 0, 8),
        Text = text,
        UseVisualStyleBackColor = true
    };

    private static Button NewButton(string text, Color color, int width) => new()
    {
        BackColor = color,
        FlatAppearance = { BorderSize = 0 },
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI Semibold", 9.5F),
        ForeColor = Color.White,
        Height = 38,
        Margin = new Padding(8, 0, 0, 0),
        Text = text,
        Width = width
    };

    private static void StyleTextBox(TextBox textBox)
    {
        textBox.BackColor = Color.FromArgb(15, 23, 42);
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.ForeColor = Color.FromArgb(241, 245, 249);
    }

    private static decimal Clamp(int value, NumericUpDown input) => Math.Clamp(value, (int)input.Minimum, (int)input.Maximum);

    private sealed class RegionInputs : IDisposable
    {
        public NumericUpDown X { get; } = NewNumber(0, 100000);
        public NumericUpDown Y { get; } = NewNumber(0, 100000);
        public NumericUpDown Width { get; } = NewNumber(0, 100000);
        public NumericUpDown Height { get; } = NewNumber(0, 100000);
        public PictureBox Preview { get; } = new()
        {
            BackColor = Color.FromArgb(15, 23, 42),
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(4),
            Size = new Size(92, 34),
            SizeMode = PictureBoxSizeMode.Zoom
        };

        public void Populate(ScreenRegion region)
        {
            X.Value = Clamp(region.X, X);
            Y.Value = Clamp(region.Y, Y);
            Width.Value = Clamp(region.Width, Width);
            Height.Value = Clamp(region.Height, Height);
        }

        public ScreenRegion ToRegion() => new()
        {
            X = decimal.ToInt32(X.Value),
            Y = decimal.ToInt32(Y.Value),
            Width = decimal.ToInt32(Width.Value),
            Height = decimal.ToInt32(Height.Value)
        };

        public void SetPreview(Bitmap bitmap)
        {
            Preview.Image?.Dispose();
            Preview.Image = new Bitmap(bitmap);
        }

        public void Dispose()
        {
            Preview.Image?.Dispose();
            Preview.Image = null;
        }
    }
}
