using TBMWatchdog.Models;
using TBMWatchdog.Services;
using System.Drawing.Imaging;

namespace TBMWatchdog.Forms;

public sealed class RegionSelectorForm : Form
{
    private readonly Bitmap _screenImage;
    private readonly OcrService _ocrService = new();
    private readonly Panel _toolbar;
    private readonly PictureBox _preview;
    private readonly Label _resultLabel;
    private Point _dragStart;
    private Rectangle _selection;
    private bool _dragging;
    private readonly string _regionName;
    private readonly SemaphoreSlim _testGate = new(1, 1);
    private Button _testButton = null!;
    private Button _cancelTestButton = null!;
    private Button _useButton = null!;
    private Button _closeButton = null!;
    private ProgressBar _testProgress = null!;
    private CheckBox _saveDebugImages = null!;
    private CancellationTokenSource? _testCancellation;
    private readonly Rectangle _selectionScreenWorkingArea;
    private bool _diagnosticOpen;
    private bool _selectionMode;
    private Rectangle _selectionBeforeDrag;
    private Button _selectAgainButton = null!;
    private bool _toolbarRequestedVisible = true;
    internal bool ToolbarVisibleForTest => _toolbarRequestedVisible;
    internal bool SelectionModeForTest => _selectionMode;
    internal Rectangle SelectionForTest => _selection;
    internal bool PreviewHasImageForTest => _preview.Image is not null;

    public RegionSelectorForm(int screenNumber, ScreenRegion currentRegion, string regionName, OcrProfile? ocrProfile = null, Bitmap? testScreenImage = null)
    {
        var screens = Screen.AllScreens;
        if (screenNumber < 0 || screenNumber >= screens.Length)
        {
            throw new ScreenCaptureException($"Ekran {screenNumber} bulunamadı.");
        }

        var screen = screens[screenNumber];
        _selectionScreenWorkingArea = screen.WorkingArea;
        _screenImage = testScreenImage is null ? CaptureScreen(screen.Bounds) : new Bitmap(testScreenImage);
        _regionName = regionName;
        OcrProfile = ocrProfile ?? new OcrProfile();
        _selection = currentRegion.IsValid
            ? Rectangle.Intersect(new Rectangle(Point.Empty, screen.Bounds.Size), new Rectangle(currentRegion.X, currentRegion.Y, currentRegion.Width, currentRegion.Height))
            : Rectangle.Empty;

        Text = $"Bölge Seç — {regionName}";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = screen.Bounds;
        TopMost = true;
        DoubleBuffered = true;
        KeyPreview = true;
        BackgroundImage = _screenImage;
        BackgroundImageLayout = ImageLayout.None;
        Cursor = Cursors.Cross;

        _preview = new PictureBox
        {
            BackColor = Color.FromArgb(15, 23, 42),
            BorderStyle = BorderStyle.FixedSingle,
            Dock = DockStyle.Fill,
            Margin = new Padding(8),
            SizeMode = PictureBoxSizeMode.Zoom
        };
        _resultLabel = new Label
        {
            AutoEllipsis = true,
            ForeColor = Color.FromArgb(203, 213, 225),
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 2, 4, 2),
            Text = "Fareyle takip edilecek değerin çevresini çizin. Esc: İptal"
        };
        _toolbar = CreateToolbar(regionName);
        Controls.Add(_toolbar);

        MouseDown += Selector_MouseDown;
        MouseMove += Selector_MouseMove;
        MouseUp += Selector_MouseUp;
        KeyDown += Selector_KeyDown;
        Shown += (_, _) => BeginSelectionMode();
        FormClosing += (_, e) =>
        {
            if (_testCancellation is null) return;
            _testCancellation.Cancel();
            e.Cancel = true;
            _resultLabel.Text = "Test iptal ediliyor…";
        };
    }

    public ScreenRegion? SelectedRegion { get; private set; }
    public Bitmap? SelectedPreview { get; private set; }
    public OcrProfile OcrProfile { get; }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_selection.IsEmpty)
        {
            if (_selectionMode) DrawSelectionHint(e.Graphics);
            return;
        }

        using var shade = new SolidBrush(Color.FromArgb(105, 2, 6, 23));
        e.Graphics.FillRectangle(shade, new Rectangle(0, 0, ClientSize.Width, _selection.Top));
        e.Graphics.FillRectangle(shade, new Rectangle(0, _selection.Bottom, ClientSize.Width, ClientSize.Height - _selection.Bottom));
        e.Graphics.FillRectangle(shade, new Rectangle(0, _selection.Top, _selection.Left, _selection.Height));
        e.Graphics.FillRectangle(shade, new Rectangle(_selection.Right, _selection.Top, ClientSize.Width - _selection.Right, _selection.Height));
        using var border = new Pen(Color.FromArgb(56, 189, 248), 3F);
        e.Graphics.DrawRectangle(border, _selection);
        using var labelBackground = new SolidBrush(Color.FromArgb(220, 3, 105, 161));
        var text = $"X:{_selection.X}  Y:{_selection.Y}  {_selection.Width}×{_selection.Height}";
        var textSize = e.Graphics.MeasureString(text, Font);
        var labelY = Math.Max(0, _selection.Top - (int)textSize.Height - 8);
        e.Graphics.FillRectangle(labelBackground, _selection.Left, labelY, textSize.Width + 12, textSize.Height + 4);
        e.Graphics.DrawString(text, Font, Brushes.White, _selection.Left + 6, labelY + 2);

        if (_selectionMode) DrawSelectionHint(e.Graphics);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _preview.Image?.Dispose();
            SelectedPreview?.Dispose();
            _screenImage.Dispose();
            _testCancellation?.Cancel();
            _testCancellation?.Dispose();
            _ocrService.Dispose();
            _testGate.Dispose();
        }

        base.Dispose(disposing);
    }

    private Panel CreateToolbar(string regionName)
    {
        var panel = new TableLayoutPanel
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.FromArgb(242, 15, 23, 42),
            Location = new Point(12, 18),
            Size = new Size(Math.Max(1, ClientSize.Width - 24), 138),
            ColumnCount = 3,
            RowCount = 2,
            Padding = new Padding(10)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46F));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 72F));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 28F));

        var information = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Margin = new Padding(2) };
        information.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        information.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        information.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        information.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 13F),
            ForeColor = Color.White,
            Text = $"{regionName} bölgesini seçin",
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        information.Controls.Add(_resultLabel, 0, 1);
        _saveDebugImages = new CheckBox { AutoSize = true, Checked = false, ForeColor = Color.FromArgb(203, 213, 225),
            Dock = DockStyle.Fill, Text = "Debug görüntülerini kaydet", BackColor = Color.Transparent };
        information.Controls.Add(_saveDebugImages, 0, 2);

        var previewGroup = new GroupBox { Dock = DockStyle.Fill, Margin = new Padding(6, 0, 10, 0), Padding = new Padding(5),
            ForeColor = Color.FromArgb(148, 163, 184), Text = "OCR Önizleme" };
        previewGroup.Controls.Add(_preview);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true, AutoScroll = true, Margin = new Padding(0), Padding = new Padding(0, 17, 0, 0) };
        _testButton = NewButton("OCR Test Et", Color.FromArgb(51, 65, 85), 105); _testButton.Click += TestButton_Click;
        _cancelTestButton = NewButton("Testi İptal", Color.FromArgb(180, 83, 9), 105); _cancelTestButton.Enabled = false;
        _cancelTestButton.Click += (_, _) => _testCancellation?.Cancel();
        _useButton = NewButton("Seçimi Kullan", Color.FromArgb(2, 132, 199), 118); _useButton.Click += UseButton_Click;
        _closeButton = NewButton("İptal", Color.FromArgb(127, 29, 29), 70); _closeButton.Click += (_, _) => DialogResult = DialogResult.Cancel;
        _selectAgainButton = NewButton("Alanı Yeniden Seç", Color.FromArgb(3, 105, 161), 135); _selectAgainButton.Click += (_, _) => BeginSelectionMode();
        actions.Controls.AddRange([_selectAgainButton, _testButton, _cancelTestButton, _useButton, _closeButton]);

        _testProgress = new ProgressBar { Dock = DockStyle.Fill, Margin = new Padding(4, 6, 4, 4), Minimum = 0, Maximum = 100, Visible = false };
        panel.Controls.Add(information, 0, 0); panel.SetRowSpan(information, 2);
        panel.Controls.Add(previewGroup, 1, 0); panel.SetRowSpan(previewGroup, 2);
        panel.Controls.Add(actions, 2, 0);
        panel.Controls.Add(_testProgress, 2, 1);
        return panel;
    }

    private void Selector_MouseDown(object? sender, MouseEventArgs e)
    {
        if (_diagnosticOpen || !Enabled) return;
        if (e.Button != MouseButtons.Left || !_selectionMode) return;
        _dragStart = e.Location;
        _selection = Rectangle.Empty;
        _dragging = true;
        Capture = true;
    }

    private void Selector_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_diagnosticOpen || !Enabled) return;
        if (!_dragging) return;
        _selection = NormalizeRectangle(_dragStart, ClampToClient(e.Location));
        Invalidate();
    }

    private void Selector_MouseUp(object? sender, MouseEventArgs e)
    {
        if (_diagnosticOpen || !Enabled) return;
        if (!_dragging) return;
        _dragging = false;
        Capture = false;
        _selection = NormalizeRectangle(_dragStart, ClampToClient(e.Location));
        if (_selection.Width < 3 || _selection.Height < 3) _selection = Rectangle.Empty;
        if (_selection.IsEmpty) _selection = _selectionBeforeDrag;
        EndSelectionMode();
        UpdatePreview();
        Invalidate();
    }

    private void Selector_KeyDown(object? sender, KeyEventArgs e)
    {
        if (_diagnosticOpen || !Enabled) return;
        if (e.KeyCode == Keys.Escape)
        {
            if (_selectionMode)
            {
                CancelActiveSelection();
                e.Handled = true;
                return;
            }
            DialogResult = DialogResult.Cancel;
        }
        if (e.KeyCode == Keys.Enter) UseSelection();
    }

    private void BeginSelectionMode()
    {
        if (_diagnosticOpen || _testCancellation is not null) return;
        _selectionBeforeDrag = _selection;
        _selectionMode = true;
        _dragging = false;
        Capture = false;
        _toolbar.Visible = false;
        _toolbarRequestedVisible = false;
        Cursor = Cursors.Cross;
        Focus();
        Invalidate();
    }

    private void EndSelectionMode()
    {
        _selectionMode = false;
        _dragging = false;
        Capture = false;
        _toolbar.Visible = true;
        _toolbarRequestedVisible = true;
        Cursor = Cursors.Default;
        _toolbar.BringToFront();
        Invalidate();
    }

    private void CancelActiveSelection()
    {
        _selection = _selectionBeforeDrag;
        EndSelectionMode();
        UpdatePreview();
        _resultLabel.Text = _selection.IsEmpty
            ? "Alan seçimi iptal edildi. Kayıtlı bölge değiştirilmedi."
            : "Yeni alan seçimi iptal edildi; önceki bölge korundu.";
    }

    private void DrawSelectionHint(Graphics graphics)
    {
        const string hint = "Alanı seçmek için sürükleyin  •  ESC: İptal";
        using var font = new Font("Segoe UI", 9F, FontStyle.Regular);
        var size = graphics.MeasureString(hint, font);
        var bounds = new RectangleF(Math.Max(8, ClientSize.Width - size.Width - 28),
            Math.Max(8, ClientSize.Height - size.Height - 20), size.Width + 16, size.Height + 8);
        using var background = new SolidBrush(Color.FromArgb(185, 15, 23, 42));
        graphics.FillRectangle(background, bounds);
        graphics.DrawString(hint, font, Brushes.White, bounds.X + 8, bounds.Y + 4);
    }

    internal void SimulateSelectionForTest(Rectangle selection)
    {
        BeginSelectionMode();
        _dragStart = selection.Location;
        _dragging = true;
        _selection = NormalizeRectangle(selection.Location, new Point(selection.Right, selection.Bottom));
        _dragging = false;
        EndSelectionMode();
        UpdatePreview();
    }

    internal void BeginSelectionForTest() => BeginSelectionMode();

    internal void CancelSelectionForTest()
    {
        BeginSelectionMode();
        _selection = Rectangle.Empty;
        CancelActiveSelection();
    }

    private async void TestButton_Click(object? sender, EventArgs e)
    {
        if (!await _testGate.WaitAsync(0)) return;
        if (_selection.IsEmpty)
        {
            _resultLabel.Text = "Önce geçerli bir bölge seçin.";
            _testGate.Release();
            return;
        }
        if (_selection.Width > 500 || _selection.Height > 250)
        {
            MessageBox.Show(this, "OCR alanı en fazla 500×250 piksel olabilir. Daha küçük bir sayısal alan seçin.",
                "OCR Alanı Çok Büyük", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _testGate.Release();
            return;
        }

        var previousTopMost = TopMost;
        var selectorHidden = false;
        TopMost = false;
        _testCancellation = new CancellationTokenSource();
        var token = _testCancellation.Token;
        SetTestUi(true);
        try
        {
            var region = new ScreenRegion { X = _selection.X, Y = _selection.Y, Width = _selection.Width, Height = _selection.Height };
            var field = _regionName.Contains("Nominal", StringComparison.OrdinalIgnoreCase) ? OcrFieldKind.Nominal
                : _regionName.Contains("Kapak", StringComparison.OrdinalIgnoreCase) ? OcrFieldKind.Gate
                : _regionName.Contains("Kantar 1", StringComparison.OrdinalIgnoreCase) ? OcrFieldKind.Scale1
                : _regionName.Contains("Kantar 2", StringComparison.OrdinalIgnoreCase) ? OcrFieldKind.Scale2
                : _regionName.Equals("RPM", StringComparison.OrdinalIgnoreCase) ? OcrFieldKind.StableRpm
                : _regionName.Equals("Tork", StringComparison.OrdinalIgnoreCase) ? OcrFieldKind.StableTorque
                : _regionName.Contains("İtme Kuvveti", StringComparison.OrdinalIgnoreCase) ? OcrFieldKind.StableThrustForce
                : _regionName.Contains("İlerleme Hızı", StringComparison.OrdinalIgnoreCase) ? OcrFieldKind.StableAdvanceSpeed
                : _regionName.Contains("Penetrasyon", StringComparison.OrdinalIgnoreCase) ? OcrFieldKind.StablePenetration
                : _regionName.Contains("Nominal Enjeksiyon", StringComparison.OrdinalIgnoreCase) ? OcrFieldKind.NominalInjection
                : _regionName.Contains("Gerçek Enjeksiyon", StringComparison.OrdinalIgnoreCase) ? OcrFieldKind.RealInjection
                : _regionName.Contains("EPB 1", StringComparison.OrdinalIgnoreCase) ? OcrFieldKind.Epb1
                : _regionName.Contains("EPB 2", StringComparison.OrdinalIgnoreCase) ? OcrFieldKind.Epb2
                : _regionName.Contains("EPB 3", StringComparison.OrdinalIgnoreCase) ? OcrFieldKind.Epb3
                : _regionName.Contains("EPB 4", StringComparison.OrdinalIgnoreCase) ? OcrFieldKind.Epb4
                : _regionName.Contains("EPB 5", StringComparison.OrdinalIgnoreCase) ? OcrFieldKind.Epb5
                : IsAdvanceRegionName(_regionName) ? OcrFieldKind.Advance
                : OcrFieldKind.Scale;
            var padded = ScreenCaptureService.GetPaddedRegion(region, OcrProfile, _screenImage.Size, field);
            using var testImage = Crop(_screenImage, new Rectangle(padded.X, padded.Y, padded.Width, padded.Height));
            if (_regionName.Contains("Çalışma Modu", StringComparison.OrdinalIgnoreCase))
            {
                using var textOcr = new TextOcrService();
                var textResult = await Task.Run(() => textOcr.ReadAsync(testImage, token), token);
                var normalizedText = textResult.NormalizedText;
                var evaluation = OperationModeTextOcrEvaluator.Evaluate(textResult, OcrProfile); var match = evaluation.Match;
                var failureReason = evaluation.FailureReason;
                var status = !textResult.IsSuccessful ? "Metin okunamadı"
                    : match.State == OperationModeState.Unknown ? "Metin okundu fakat OperationMode eşleşmedi" : "OperationMode eşleşti";
                _resultLabel.Text = $"{status} | OCR sonucu: {(textResult.IsSuccessful ? textResult.FinalOperationModeText : "—")} | "
                    + $"RawTesseractText: {Empty(textResult.RawText)} | TrimmedText: {Empty(textResult.TrimmedText)} | "
                    + $"NormalizedText: {Empty(normalizedText)} | FinalOperationModeText: {Empty(textResult.FinalOperationModeText)} | "
                    + $"En iyi PSM: {textResult.BestPageSegMode} | Ön işleme: {textResult.BestPreprocessingMethod} | "
                    + $"Güven: %{textResult.Confidence * 100:0.0} | Eşleşen: {(string.IsNullOrEmpty(match.MatchedKeyword) ? "—" : match.MatchedKeyword)} | "
                    + $"Yöntem: {match.Method} | Benzerlik: %{match.SimilarityPercent:0.0} | FailureReason: {failureReason}"
                    + (textResult.Error is null ? "" : $" | Hata: {textResult.Error}");
                return;

                static string Empty(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value.Replace("\r", " ").Replace("\n", " ");
            }
            var progress = new Progress<OcrTestProgress>(value =>
            {
                _testProgress.Maximum = Math.Max(1, value.Total);
                _testProgress.Value = Math.Clamp(value.Completed, 0, _testProgress.Maximum);
                _resultLabel.Text = $"{value.Status} | {value.Elapsed.TotalSeconds:0.0} sn";
            });
            using var diagnostic = await Task.Run(async () =>
                await _ocrService.ReadNumberWithDiagnosticsAsync(testImage,
                    GetTestMaximum(field),
                    OcrProfile, field, token, progress)
                    .ConfigureAwait(false), token);
            var debugFolder = "Kapalı";
            if (_saveDebugImages.Checked)
                debugFolder = await Task.Run(() => SaveDebugImages(diagnostic, token), token);
            _resultLabel.Text = diagnostic.IsAccepted
                ? $"{_regionName}: {diagnostic.Result.Value:0.###}{GetTestUnit()} | Ham OCR: {diagnostic.Result.RawText} | Güven: %{diagnostic.Result.Confidence * 100:0} | FailureReason: Success | {diagnostic.TotalElapsed.TotalMilliseconds:0} ms"
                : $"Okunamadı: {diagnostic.Result.Error}";
            var characterOptions = NumericFieldCharacterOcrService.Supports(field)
                ? NumericFieldCharacterOcrService.GetOptions(OcrProfile, field)
                : NumericFieldCharacterOcrService.GetOptions(OcrProfile, OcrFieldKind.Gate);
            using var resultsForm = new OcrTestResultsForm(diagnostic, debugFolder,
                characterOptions.TemplateFolder, OcrProfile.Name,
                characterOptions.MinimumRecommendedTemplates, field,
                OcrProfile.DuplicateTemplateSimilarityThreshold, OcrProfile.MaximumTemplatesPerDigit);
            resultsForm.PlaceInWorkingArea(_selectionScreenWorkingArea);
            _diagnosticOpen = true;
            _dragging = false;
            Capture = false;
            Enabled = false;
            Hide();
            selectorHidden = true;
            resultsForm.ShowDialog(this);
        }
        catch (OperationCanceledException)
        {
            _resultLabel.Text = "OCR testi kullanıcı tarafından iptal edildi.";
        }
        catch (OcrConfigurationException exception)
        {
            _resultLabel.Text = exception.Message;
        }
        catch (Exception exception)
        {
            _resultLabel.Text = $"OCR testi başarısız: {exception.Message}";
        }
        finally
        {
            _diagnosticOpen = false;
            if (selectorHidden && !IsDisposed && !Disposing)
            {
                Show();
                Enabled = true;
            }
            TopMost = previousTopMost;
            if (selectorHidden && Visible)
            {
                BringToFront();
                Activate();
                Focus();
            }
            _testCancellation?.Dispose();
            _testCancellation = null;
            SetTestUi(false);
            _testGate.Release();
        }
    }

    internal static bool IsAdvanceRegionName(string regionName) =>
        regionName.Contains("İlerleme Mesafesi", StringComparison.OrdinalIgnoreCase)
        || regionName.Contains("İlerleme Grup", StringComparison.OrdinalIgnoreCase)
        || regionName.Contains("AdvanceGroup", StringComparison.OrdinalIgnoreCase)
        || regionName.Equals("AdvanceMm", StringComparison.OrdinalIgnoreCase);

    private decimal GetTestMaximum(OcrFieldKind field) => _regionName.Contains("RPM", StringComparison.OrdinalIgnoreCase) ? 10m
        : _regionName.Contains("Tork", StringComparison.OrdinalIgnoreCase) ? 20m
        : _regionName.Contains("İtme", StringComparison.OrdinalIgnoreCase) ? 100000m
        : _regionName.Contains("İlerleme Hızı", StringComparison.OrdinalIgnoreCase) ? 200m
        : _regionName.Contains("Penetrasyon", StringComparison.OrdinalIgnoreCase) ? 100m
        : field == OcrFieldKind.Gate ? OcrProfile.GateMaximumValue : 1_000_000m;
    private string GetTestUnit() => _regionName.Contains("RPM", StringComparison.OrdinalIgnoreCase) ? " rpm"
        : _regionName.Contains("Tork", StringComparison.OrdinalIgnoreCase) ? " MNm"
        : _regionName.Contains("İtme", StringComparison.OrdinalIgnoreCase) ? " kN"
        : _regionName.Contains("İlerleme Hızı", StringComparison.OrdinalIgnoreCase) ? " mm/min"
        : _regionName.Contains("Penetrasyon", StringComparison.OrdinalIgnoreCase) ? " mm/rot" : "";

    private void UseButton_Click(object? sender, EventArgs e) => UseSelection();

    private void SetTestUi(bool running)
    {
        _testButton.Enabled = !running; _cancelTestButton.Enabled = running;
        _useButton.Enabled = !running; _closeButton.Enabled = !running; _saveDebugImages.Enabled = !running;
        _testProgress.Visible = running;
        if (running) { _testProgress.Minimum = 0; _testProgress.Maximum = 12; _testProgress.Value = 0; _resultLabel.Text = "Test ediliyor…"; }
    }

    private string SaveDebugImages(OcrDiagnosticResult diagnostic, CancellationToken token)
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TBMWatchdog", "debug", $"{DateTime.Now:yyyyMMdd_HHmmss_fff}_{SafeFileName(_regionName)}");
        Directory.CreateDirectory(root);
        token.ThrowIfCancellationRequested();
        diagnostic.RawImage.Save(Path.Combine(root, "00_ham.png"), ImageFormat.Png);
        var selected = diagnostic.Candidates.OrderByDescending(candidate =>
            ReferenceEquals(candidate, diagnostic.SelectedCandidate) ? 2f : candidate.SystemConfidence).Take(3).ToArray();
        for (var index = 0; index < selected.Length; index++)
        {
            token.ThrowIfCancellationRequested();
            var candidate = selected[index];
            var prefix = $"{index + 1:00}_{SafeFileName(candidate.ProcessingMethod)}_{candidate.PageSegMode}";
            candidate.ProcessedImage.Save(Path.Combine(root, prefix + "_islenmis.png"), ImageFormat.Png);
            candidate.BinaryImage.Save(Path.Combine(root, prefix + "_binary.png"), ImageFormat.Png);
            candidate.SegmentationImage.Save(Path.Combine(root, prefix + "_segmentasyon.png"), ImageFormat.Png);
        }
        return root;
    }

    private static string SafeFileName(string value) => string.Concat(value.Select(character =>
        Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));

    private void UseSelection()
    {
        if (_selection.Width < 3 || _selection.Height < 3)
        {
            _resultLabel.Text = "En az 3×3 piksel büyüklüğünde bir bölge seçin.";
            return;
        }

        SelectedRegion = new ScreenRegion
        {
            X = _selection.X,
            Y = _selection.Y,
            Width = _selection.Width,
            Height = _selection.Height
        };
        SelectedPreview?.Dispose();
        SelectedPreview = Crop(_screenImage, _selection);
        DialogResult = DialogResult.OK;
    }

    private void UpdatePreview()
    {
        _preview.Image?.Dispose();
        _preview.Image = _selection.IsEmpty ? null : Crop(_screenImage, _selection);
    }

    private Point ClampToClient(Point point) => new(
        Math.Clamp(point.X, 0, Math.Max(0, ClientSize.Width - 1)),
        Math.Clamp(point.Y, 0, Math.Max(0, ClientSize.Height - 1)));

    private static Rectangle NormalizeRectangle(Point first, Point second) => Rectangle.FromLTRB(
        Math.Min(first.X, second.X),
        Math.Min(first.Y, second.Y),
        Math.Max(first.X, second.X),
        Math.Max(first.Y, second.Y));

    private static Bitmap CaptureScreen(Rectangle bounds)
    {
        var bitmap = new Bitmap(bounds.Width, bounds.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size, CopyPixelOperation.SourceCopy);
        return bitmap;
    }

    private static Bitmap Crop(Bitmap source, Rectangle rectangle)
    {
        var bitmap = new Bitmap(rectangle.Width, rectangle.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.DrawImage(source, new Rectangle(Point.Empty, rectangle.Size), rectangle, GraphicsUnit.Pixel);
        return bitmap;
    }

    private static Button NewButton(string text, Color color, int width) => new()
    {
        BackColor = color,
        FlatAppearance = { BorderSize = 0 },
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI Semibold", 9F),
        ForeColor = Color.White,
        Height = 38,
        Text = text,
        Width = width
    };
}
