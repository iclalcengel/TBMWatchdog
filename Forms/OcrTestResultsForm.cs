using TBMWatchdog.Models;

namespace TBMWatchdog.Forms;

public sealed class OcrTestResultsForm : Form
{
    internal int CharacterDetailCardCount => _characterCards.Count;
    internal int CharacterTeachButtonCount => _characterCards.Sum(card =>
        Descendants(card).OfType<Button>().Count(button => button.Text == "Rakamı Öğret"));
    internal string SelectedCharacterText => _selectedCharacterLabel.Text;
    private readonly PictureBox _processed = NewPicture();
    private readonly PictureBox _binary = NewPicture();
    private readonly PictureBox _segmentation = NewPicture();
    private readonly FlowLayoutPanel _characterDetails = new()
    {
        Dock = DockStyle.Fill, AutoScroll = true, WrapContents = false,
        FlowDirection = FlowDirection.TopDown,
        BackColor = Color.FromArgb(15, 23, 42), Padding = new Padding(8)
    };
    private readonly Label _selectedCharacterLabel = new() { AutoSize = true, ForeColor = Color.White, Padding = new Padding(8, 7, 8, 0) };
    private readonly Label _characterWarning = new() { AutoSize = true, ForeColor = Color.FromArgb(254, 202, 202), Padding = new Padding(8, 7, 8, 0) };
    private readonly Button _previousCharacter = new() { Text = "Önceki Karakter", AutoSize = true };
    private readonly Button _nextCharacter = new() { Text = "Sonraki Karakter", AutoSize = true };
    private readonly List<Control> _characterCards = [];
    private int _selectedCharacterIndex = -1;
    private readonly ListView _candidateList;
    private readonly TabControl _diagnosticTabs = new() { Dock = DockStyle.Fill };
    private OcrCandidateResult? _activeCandidate;
    private readonly string _templateFolder;
    private readonly string _machineProfile;
    private readonly int _minimumRecommendedTemplates;
    private readonly OcrFieldKind _fieldKind;
    private readonly double _duplicateThreshold;
    private readonly int _maximumTemplatesPerDigit;

    public OcrTestResultsForm(OcrDiagnosticResult diagnostic, string debugFolder, string? templateFolder = null,
        string machineProfile = "Default", int minimumRecommendedTemplates = 5,
        OcrFieldKind fieldKind = OcrFieldKind.Gate, double duplicateThreshold = .97, int maximumTemplatesPerDigit = 30)
    {
        _templateFolder = templateFolder ?? Path.Combine(AppContext.BaseDirectory, "Templates", "GateDigits");
        _machineProfile = machineProfile;
        _minimumRecommendedTemplates = minimumRecommendedTemplates;
        _fieldKind = fieldKind;
        _duplicateThreshold = duplicateThreshold; _maximumTemplatesPerDigit = maximumTemplatesPerDigit;
        Text = "Profesyonel OCR Tanılama"; StartPosition = FormStartPosition.Manual; Size = new Size(1480, 820);
        MinimumSize = new Size(1050, 620); BackColor = Color.FromArgb(15, 23, 42); ForeColor = Color.FromArgb(226, 232, 240); Font = new Font("Segoe UI", 9F);
        ShowInTaskbar = true;
        TopMost = true;
        Shown += (_, _) => BeginInvoke(() =>
        {
            BringToFront();
            Activate();
            Focus();
            TopMost = false;
        });
        var templateWarning = diagnostic.Candidates.Select(candidate => candidate.TemplateWarning).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        var characterResult = diagnostic.Candidates
            .Where(candidate => candidate.UsesCharacterBasedOcr && candidate.IsValid)
            .OrderByDescending(candidate => candidate.AllCharactersTemplateAuthoritative)
            .ThenByDescending(candidate => candidate.CharacterDecisionReliable)
            .Select(candidate => candidate.ParsedValue?.ToString("0.##"))
            .FirstOrDefault() ?? "—";
        var rowMajority = diagnostic.Candidates
            .Where(candidate => !candidate.UsesCharacterBasedOcr && candidate.IsValid)
            .GroupBy(candidate => candidate.ParsedValue!.Value)
            .OrderByDescending(group => group.Select(candidate => candidate.ProcessingMethod).Distinct().Count())
            .ThenByDescending(group => group.Average(candidate => candidate.OcrConfidence))
            .Select(group => group.Key.ToString("0.##"))
            .FirstOrDefault() ?? "—";
        var formattedFinal = diagnostic.Result.Value.HasValue
            ? diagnostic.Result.Value.Value.ToString(fieldKind switch
            {
                OcrFieldKind.StableRpm => "0.00",
                OcrFieldKind.StableTorque => "0.000",
            OcrFieldKind.StableAdvanceSpeed or OcrFieldKind.StablePenetration => "0.0",
            OcrFieldKind.NominalInjection or OcrFieldKind.RealInjection => "0.0",
            OcrFieldKind.Epb1 or OcrFieldKind.Epb2 or OcrFieldKind.Epb3 or OcrFieldKind.Epb4 or OcrFieldKind.Epb5 => "0.00",
                _ => "0.##"
            }, System.Globalization.CultureInfo.InvariantCulture)
            : "—";
        var result = new Label { Dock = DockStyle.Top, Height = templateWarning is null ? 94 : 114, Padding = new Padding(12, 8, 12, 8), Font = new Font("Segoe UI Semibold", 11F),
            ForeColor = diagnostic.IsAccepted ? Color.FromArgb(110, 231, 183) : Color.FromArgb(254, 202, 202),
            Text = diagnostic.IsAccepted
                ? $"Karakter bazlı sonuç: {characterResult} | Tam satır Tesseract çoğunluğu: {rowMajority}\r\nNihai sonuç: {formattedFinal} | Sistem güveni: %{diagnostic.Result.Confidence * 100:0.0} | Toplam: {diagnostic.TotalElapsed.TotalSeconds:0.00} sn\r\nNihai sonucu belirleyen kural: {diagnostic.DecisionReason}"
                : $"Okunamadı: {diagnostic.Result.Error} | Toplam: {diagnostic.TotalElapsed.TotalSeconds:0.00} sn" };
        if (diagnostic.Result.FullLineConsensusValue.HasValue)
        {
            result.Height += 62;
            result.Text += $"\r\nFull-line consensus: {diagnostic.Result.FullLineConsensusValue:0.##} ({diagnostic.Result.FullLineConsensusCount} aday)"
                + $" | Character result: {diagnostic.Result.CharacterResult?.ToString("0.##") ?? "—"}"
                + $" | Character status: {diagnostic.Result.CharacterStatus}"
                + $"\r\nImage estimated digits: {diagnostic.Result.EstimatedDigitCount} | Full-line candidate digits: {diagnostic.Result.SelectedCandidateDigitCount}"
                + $" | Plausibility: {diagnostic.Result.DigitPlausibility}"
                + $"\r\nFinal: {diagnostic.Result.Value:0.##} | Decision: {diagnostic.Result.DecisionSource}"
                + (diagnostic.TargetedFallbackUsed ? $" | Targeted fallback: {diagnostic.TargetedFallbackPipeline}" : "");
            if (Services.OcrService.ExpectedDecimalPlaces(fieldKind) > 0)
            {
                result.Height += 42;
                result.Text += $"\r\nZero candidates: {diagnostic.Result.ZeroCandidateCount}"
                    + $" | Verified template support: {(diagnostic.Result.CharacterAuthorityReason == "VerifiedTemplateSupport" ? "Var" : "Yok")}"
                    + $" | Character authority: {(diagnostic.Result.CharacterAuthoritative ? "Evet" : "Hayır")}"
                    + $" ({diagnostic.Result.CharacterAuthorityReason})";
            }
        }
        if (templateWarning is not null) result.Text += $"\r\nUyarı: {templateWarning}. Tesseract geri dönüşü kullanıldı.";
        var list = CreateList();
        _candidateList = list;
        foreach (var candidate in diagnostic.Candidates)
        {
            var item = new ListViewItem(candidate.ProcessingMethod) { Tag = candidate };
            item.SubItems.Add(candidate.PageSegMode.ToString()); item.SubItems.Add(Flatten(candidate.RawText));
            item.SubItems.Add(candidate.CleanedText);
            item.SubItems.Add(candidate.RawSegmentCount.ToString());
            item.SubItems.Add(candidate.SegmentCount.ToString());
            item.SubItems.Add(candidate.OcrDigitCount.ToString());
            item.SubItems.Add(candidate.SegmentMatch switch { true => "Evet", false => "Hayır", _ => "—" });
            item.SubItems.Add(EmptyAsDash(candidate.CharacterBasedResult));
            item.SubItems.Add(EmptyAsDash(candidate.TemplateResult));
            item.SubItems.Add(EmptyAsDash(candidate.GeometryResult));
            item.SubItems.Add(EmptyAsDash(candidate.CombinedCharacterResult));
            item.SubItems.Add(candidate.ExtraCharacterDetected ? "Evet" : "Hayır");
            item.SubItems.Add(EmptyAsDash(candidate.RemovedNoiseCharacter));
            item.SubItems.Add(EmptyAsDash(candidate.ExtraCharacterEliminationReason));
            item.SubItems.Add(candidate.CorrectedFullLineResult?.ToString("0.##") ?? "—");
            item.SubItems.Add($"%{candidate.OcrConfidence * 100:0.0}"); item.SubItems.Add($"%{candidate.SystemConfidence * 100:0.0}");
            item.SubItems.Add($"%{candidate.FinalScore * 100:0.0}");
            item.SubItems.Add($"{candidate.PreprocessingElapsed.TotalMilliseconds:0}");
            item.SubItems.Add($"{candidate.SegmentationElapsed.TotalMilliseconds:0}");
            item.SubItems.Add($"{candidate.OcrElapsed.TotalMilliseconds:0}");
            item.SubItems.Add($"{candidate.TotalElapsed.TotalMilliseconds:0}" + (candidate.IsSlow ? " !" : ""));
            item.SubItems.Add(candidate.IsValid ? "Geçerli" : candidate.Error ?? "Geçersiz");
            if (ReferenceEquals(candidate, diagnostic.SelectedCandidate)) { item.Text = "★ " + item.Text; item.ForeColor = Color.FromArgb(110, 231, 183); }
            list.Items.Add(item);
        }
        list.ItemSelectionChanged += (_, e) =>
        {
            if (!e.IsSelected || e.Item?.Tag is not OcrCandidateResult candidate) return;
            _activeCandidate = candidate;
            SetImages(candidate);
        };
        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 900 }; split.Panel1.Controls.Add(list); split.Panel2.Controls.Add(CreateImages());
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 42 };
        footer.Controls.Add(new Label { Dock = DockStyle.Fill, Padding = new Padding(10, 9, 10, 4), ForeColor = Color.FromArgb(148, 163, 184), Text = $"Debug klasörü: {debugFolder}" });
        var templateCount = new Services.DigitTemplateStore(_templateFolder, _machineProfile, _fieldKind).GetItems().Count(item => item.Enabled);
        var fieldTitle = _fieldKind switch
        {
            OcrFieldKind.Nominal => "Nominal",
            OcrFieldKind.Scale1 => "Kantar 1",
            OcrFieldKind.Scale2 => "Kantar 2",
            OcrFieldKind.Gate => "Kapak",
            OcrFieldKind.StableRpm => "RPM",
            OcrFieldKind.StableTorque => "Tork",
            OcrFieldKind.StableThrustForce => "İtme Kuvveti",
            OcrFieldKind.StableAdvanceSpeed => "İlerleme Hızı",
            OcrFieldKind.StablePenetration => "Penetrasyon",
            OcrFieldKind.NominalInjection => "Nominal Enjeksiyon",
            OcrFieldKind.RealInjection => "Gerçek Enjeksiyon",
            OcrFieldKind.Epb1 => "EPB 1", OcrFieldKind.Epb2 => "EPB 2", OcrFieldKind.Epb3 => "EPB 3",
            OcrFieldKind.Epb4 => "EPB 4", OcrFieldKind.Epb5 => "EPB 5",
            OcrFieldKind.Advance => "İlerleme",
            _ => "Sayısal Alan"
        };
        var manage = new Button { Dock = DockStyle.Right, Width = 190, Text = $"{fieldTitle} Şablonları ({templateCount})", BackColor = Color.FromArgb(51, 65, 85), FlatStyle = FlatStyle.Flat };
        manage.Click += (_, _) => { using var manager = new DigitTemplateManagerForm(_templateFolder, _machineProfile, _minimumRecommendedTemplates, _fieldKind, _duplicateThreshold, _maximumTemplatesPerDigit); manager.ShowDialog(this); };
        footer.Controls.Add(manage);
        Controls.Add(split); Controls.Add(result); Controls.Add(footer);
        var selectedItem = list.Items.Cast<ListViewItem>()
            .FirstOrDefault(item => ReferenceEquals(item.Tag, diagnostic.SelectedCandidate));
        selectedItem ??= list.Items.Count > 0 ? list.Items[0] : null;
        if (selectedItem is not null)
        {
            _activeCandidate = selectedItem.Tag as OcrCandidateResult;
            selectedItem.Selected = true;
            selectedItem.Focused = true;
            selectedItem.EnsureVisible();
            if (selectedItem.Tag is OcrCandidateResult selectedCandidate) SetImages(selectedCandidate);
        }
        _diagnosticTabs.SelectedIndexChanged += (_, _) =>
        {
            var taggedCandidate = _candidateList.SelectedItems.Count > 0
                ? _candidateList.SelectedItems[0].Tag as OcrCandidateResult
                : _activeCandidate;
            if (taggedCandidate is not null) SetImages(taggedCandidate);
        };
        _previousCharacter.Click += (_, _) => SelectCharacter(_selectedCharacterIndex - 1);
        _nextCharacter.Click += (_, _) => SelectCharacter(_selectedCharacterIndex + 1);
        _segmentation.MouseClick += Segmentation_MouseClick;
        Shown += (_, _) =>
        {
            if (_activeCandidate is null) return;
            var item = _candidateList.Items.Cast<ListViewItem>().FirstOrDefault(row => ReferenceEquals(row.Tag, _activeCandidate));
            if (item is null) return;
            item.Selected = true; item.Focused = true; item.EnsureVisible(); SetImages(_activeCandidate);
        };
    }

    public void PlaceInWorkingArea(Rectangle workingArea)
    {
        var width = Math.Min(Width, workingArea.Width);
        var height = Math.Min(Height, workingArea.Height);
        Size = new Size(width, height);
        Location = new Point(
            workingArea.Left + Math.Max(0, (workingArea.Width - width) / 2),
            workingArea.Top + Math.Max(0, (workingArea.Height - height) / 2));
    }

    private Control CreateImages()
    {
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f)); grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f)); grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.4f));
        grid.Controls.Add(Group("İşlenmiş Görüntü", _processed), 0, 0); grid.Controls.Add(Group("Binary (OCR Girdisi)", _binary), 0, 1);
        grid.Controls.Add(Group("Karakter Segmentasyonu", _segmentation), 0, 2);
        var overview = new TabPage("Aday Görüntüleri") { BackColor = Color.FromArgb(15, 23, 42) };
        var characters = new TabPage("Karakter Ayrıntıları") { BackColor = Color.FromArgb(15, 23, 42) };
        overview.Controls.Add(grid); characters.Controls.Add(CreateCharacterDetailsPanel());
        _diagnosticTabs.TabPages.Add(overview); _diagnosticTabs.TabPages.Add(characters);
        return _diagnosticTabs;
    }

    private void SetImages(OcrCandidateResult c)
    {
        Set(_processed, c.ProcessedImage); Set(_binary, c.BinaryImage); Set(_segmentation, c.SegmentationImage);
        ClearCharacterDetails();
        if (c.CharacterDiagnostics is { Count: > 0 })
            foreach (var diagnostic in c.CharacterDiagnostics)
            {
                var card = CreateCharacterCard(diagnostic);
                _characterCards.Add(card);
                _characterDetails.Controls.Add(card);
            }
        else
            _characterDetails.Controls.Add(new Label { AutoSize = true, ForeColor = Color.FromArgb(148, 163, 184), Text = "Bu aday karakter bazlı OCR kullanmıyor.", Padding = new Padding(8) });
        var mismatch = c.SegmentCount != c.CharacterDiagnostics.Count
            ? "Segmentasyon ve karakter ayrıntısı sayıları uyuşmuyor. " : string.Empty;
        _characterWarning.Text = mismatch + $"Bileşen={c.DetectedComponentCount} | Birleşik={c.MergedComponentCount} | "
            + $"Split denemesi={c.SplitAttemptCount} | Projection split={c.ProjectionSplitCount} | "
            + $"Vadiler={(c.ProjectionValleys.Count == 0 ? "—" : string.Join(',', c.ProjectionValleys))} | Final segment={c.SegmentCount}"
            + (c.HasUnresolvedMergedComponents ? " | ⚠ Birleşik segment çözülemedi; karakter sonucu kullanılmadı." : "");
        SelectCharacter(_characterCards.Count > 0 ? 0 : -1);
    }

    private Control CreateCharacterCard(CharacterOcrDiagnostic diagnostic)
    {
        var card = new TableLayoutPanel { Width = 560, Height = 320, ColumnCount = 4, RowCount = 2, Margin = new Padding(6), BackColor = Color.FromArgb(30, 41, 59), Padding = new Padding(6), Tag = diagnostic.Index - 1 };
        for (var column = 0; column < 4; column++) card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 56)); card.RowStyles.Add(new RowStyle(SizeType.Percent, 44));
        AddDiagnosticPicture(card, diagnostic.OriginalSegment, "Orijinal", 0);
        AddDiagnosticPicture(card, diagnostic.CleanedSegment, "Temizlenmiş", 1);
        AddDiagnosticPicture(card, diagnostic.TightCroppedImage, "Sıkı Kırpma", 2);
        AddDiagnosticPicture(card, diagnostic.NormalizedInput, "Normalize OCR", 3);
        var detail = new Label
        {
            Dock = DockStyle.Fill, ForeColor = Color.FromArgb(226, 232, 240), Padding = new Padding(5),
            Text = $"Karakter {diagnostic.Index}: X={diagnostic.SegmentBounds.X} Y={diagnostic.SegmentBounds.Y} W={diagnostic.SegmentBounds.Width} H={diagnostic.SegmentBounds.Height} Oran={(diagnostic.SegmentBounds.Height == 0 ? 0 : diagnostic.SegmentBounds.Width / (double)diagnostic.SegmentBounds.Height):0.00} Split={diagnostic.SplitSource}\r\n"
                + $"Ham={Flatten(diagnostic.RawOcrText)} | OCR={EmptyAsDash(diagnostic.CleanedDigit)} (%{diagnostic.OcrConfidence * 100:0.0}) | Şablon={diagnostic.TemplateResult ?? "—"} (%{diagnostic.TemplateScore * 100:0.0}) | İkinci={diagnostic.SecondBestTemplateResult ?? "—"} (%{diagnostic.SecondBestTemplateScore * 100:0.0}) | Fark=%{diagnostic.TemplateScoreMargin * 100:0.0} | Geometri={diagnostic.GeometryResult} | Nihai={EmptyAsDash(diagnostic.FinalDigit)}\r\n"
                + $"Mürekkep kaybı=%{diagnostic.InkLossRatio * 100:0.0} | Karar={diagnostic.DecisionSource} | Kaynak alan={_fieldKind}\r\n"
                + $"Tesseract girdisi={diagnostic.TesseractInputSource} | Şablon girdisi={diagnostic.TemplateInputSource} | Geometri girdisi={diagnostic.GeometryInputSource}\r\n"
                + $"Kaynak: {(diagnostic.TemplateContributed ? "✓" : "✗")} Şablon   {(diagnostic.TesseractContributed ? "✓" : "✗")} Tesseract   {(diagnostic.GeometryContributed ? "✓" : "✗")} Geometri\r\n{diagnostic.DecisionReason}"
        };
        var detailsPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        detailsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); detailsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        var collect = new Button { Dock = DockStyle.Fill, Text = "Rakamı Öğret", BackColor = Color.FromArgb(2, 132, 199), FlatStyle = FlatStyle.Flat };
        if (diagnostic.FinalDigit == ".")
        {
            collect.Text = "Ondalık ayıracı";
            collect.Enabled = false;
        }
        collect.Tag = diagnostic.Index - 1;
        collect.Click += (_, _) =>
        {
            using var collector = new GateDigitTemplateCollectorForm(diagnostic.NormalizedOcrInputImage,
                _templateFolder, _machineProfile, _activeCandidate?.ProcessingMethod ?? "Segmented Characters", _fieldKind,
                _duplicateThreshold, _maximumTemplatesPerDigit, diagnostic.OcrConfidence);
            if (collector.ShowDialog(this) == DialogResult.OK)
                MessageBox.Show(this, $"Şablon kaydedildi:\r\n{collector.SavedPath}\r\nYeni şablon bir sonraki OCR testinde kullanılacak.", "Şablon Kaydı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        detailsPanel.Controls.Add(detail, 0, 0); detailsPanel.Controls.Add(collect, 1, 0);
        card.Controls.Add(detailsPanel, 0, 1); card.SetColumnSpan(detailsPanel, 4);
        BindCharacterSelection(card, diagnostic.Index - 1);
        return card;
    }

    private Control CreateCharacterDetailsPanel()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
        toolbar.Controls.Add(_previousCharacter); toolbar.Controls.Add(_nextCharacter);
        toolbar.Controls.Add(_selectedCharacterLabel); toolbar.Controls.Add(_characterWarning);
        root.Controls.Add(toolbar, 0, 0); root.Controls.Add(_characterDetails, 0, 1);
        return root;
    }

    private void SelectCharacter(int index)
    {
        if (_characterCards.Count == 0)
        {
            _selectedCharacterIndex = -1; _selectedCharacterLabel.Text = "Karakter yok";
            _previousCharacter.Enabled = false; _nextCharacter.Enabled = false; return;
        }
        _selectedCharacterIndex = Math.Clamp(index, 0, _characterCards.Count - 1);
        for (var cardIndex = 0; cardIndex < _characterCards.Count; cardIndex++)
        {
            var selected = cardIndex == _selectedCharacterIndex;
            _characterCards[cardIndex].BackColor = selected ? Color.FromArgb(3, 105, 161) : Color.FromArgb(30, 41, 59);
            foreach (var button in Descendants(_characterCards[cardIndex]).OfType<Button>().Where(button => button.Text == "Rakamı Öğret"))
                button.Enabled = selected;
        }
        _selectedCharacterLabel.Text = $"Karakter {_selectedCharacterIndex + 1} / {_characterCards.Count}";
        _previousCharacter.Enabled = _selectedCharacterIndex > 0;
        _nextCharacter.Enabled = _selectedCharacterIndex < _characterCards.Count - 1;
        _characterDetails.ScrollControlIntoView(_characterCards[_selectedCharacterIndex]);
    }

    private void Segmentation_MouseClick(object? sender, MouseEventArgs e)
    {
        if (_activeCandidate is null || _segmentation.Image is null || _activeCandidate.AcceptedSegmentBounds.Count == 0) return;
        var scale = Math.Min(_segmentation.ClientSize.Width / (double)_segmentation.Image.Width,
            _segmentation.ClientSize.Height / (double)_segmentation.Image.Height);
        var shownWidth = _segmentation.Image.Width * scale;
        var shownHeight = _segmentation.Image.Height * scale;
        var x = (e.X - (_segmentation.ClientSize.Width - shownWidth) / 2) / scale;
        var y = (e.Y - (_segmentation.ClientSize.Height - shownHeight) / 2) / scale;
        var index = _activeCandidate.AcceptedSegmentBounds.ToList().FindIndex(bounds => bounds.Contains((int)x, (int)y));
        if (index >= 0 && index < _characterCards.Count)
        {
            _diagnosticTabs.SelectedIndex = 1;
            SelectCharacter(index);
        }
    }

    private void BindCharacterSelection(Control root, int index)
    {
        root.Click += (_, _) => SelectCharacter(index);
        foreach (Control child in root.Controls) BindCharacterSelection(child, index);
    }

    private static void AddDiagnosticPicture(TableLayoutPanel card, Bitmap source, string title, int column)
    {
        var box = NewPicture(); box.Image = new Bitmap(source);
        card.Controls.Add(Group(title, box), column, 0);
    }

    private void ClearCharacterDetails()
    {
        foreach (Control control in _characterDetails.Controls)
        {
            foreach (var box in Descendants(control).OfType<PictureBox>()) { box.Image?.Dispose(); box.Image = null; }
            control.Dispose();
        }
        _characterDetails.Controls.Clear();
        _characterCards.Clear();
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls) { yield return child; foreach (var nested in Descendants(child)) yield return nested; }
    }
    private static void Set(PictureBox target, Bitmap image) { var old = target.Image; target.Image = new Bitmap(image); old?.Dispose(); }
    protected override void Dispose(bool disposing) { if (disposing) { ClearCharacterDetails(); foreach (var box in new[] { _processed, _binary, _segmentation }) { box.Image?.Dispose(); box.Image = null; } } base.Dispose(disposing); }
    private static PictureBox NewPicture() => new() { BackColor = Color.Black, Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };
    private static GroupBox Group(string title, Control child) { var group = new GroupBox { Dock = DockStyle.Fill, Text = title, ForeColor = Color.FromArgb(203, 213, 225), Padding = new Padding(6) }; group.Controls.Add(child); return group; }
    private static string Flatten(string text) => string.IsNullOrWhiteSpace(text) ? "∅" : text.Replace("\r", " ").Replace("\n", " ").Trim();
    private static string EmptyAsDash(string? text) => string.IsNullOrWhiteSpace(text) ? "—" : text;
    private static ListView CreateList()
    {
        var list = new ListView { BackColor = Color.FromArgb(30, 41, 59), BorderStyle = BorderStyle.None, Dock = DockStyle.Fill, ForeColor = Color.FromArgb(226, 232, 240), FullRowSelect = true, MultiSelect = false, GridLines = true, HideSelection = false, View = View.Details };
        list.Columns.Add("İşleme Yöntemi", 235); list.Columns.Add("PSM", 95); list.Columns.Add("Ham", 85); list.Columns.Add("Temiz", 75);
        list.Columns.Add("Ham Segment", 85); list.Columns.Add("Geçerli Segment", 95);
        list.Columns.Add("OCR Rakam", 75); list.Columns.Add("Segment Uyumu", 100);
        list.Columns.Add("Karakter OCR", 90); list.Columns.Add("Şablon", 75); list.Columns.Add("Geometri", 85); list.Columns.Add("Birleşik", 75);
        list.Columns.Add("Fazladan Karakter", 110); list.Columns.Add("Elenen", 60);
        list.Columns.Add("Eleme Nedeni", 280); list.Columns.Add("Düzeltilmiş Tam Satır", 130);
        list.Columns.Add("OCR Güven", 85); list.Columns.Add("Sistem Güven", 95); list.Columns.Add("Nihai Puan", 85);
        list.Columns.Add("Ön ms", 60); list.Columns.Add("Seg ms", 60); list.Columns.Add("OCR ms", 60); list.Columns.Add("Toplam ms", 75);
        list.Columns.Add("Sonuç / Red Nedeni", 260); return list;
    }
}
