using TBMWatchdog.Models;
using TBMWatchdog.Services;

namespace TBMWatchdog.Forms;

public partial class MainForm : Form
{
    private sealed record FeedbackCellState(string MeasurementId,string FieldId,string BaseText,string BaseTooltip);
    internal enum DashboardCategory { Excavation, Material, Ring }
    private readonly SettingsService _settingsService = new();
    private readonly MonitoringService _monitoringService = new();
    private readonly LogService _logService;
    private AppSettings _settings;
    private CsvService? _csvService;
    private IDataSource? _activeDataSource;
    private bool _allowClose;
    private OcrPreviewForm? _ocrPreviewForm;
    private readonly MonitoringSessionStore _sessionStore = new();
    private MonitoringSessionState? _session;
    private TimeSpan _displayedSessionElapsed;
    private PerformanceMonitoringService? _performanceMonitor;
    private PerformanceHealthForm? _performanceForm;
    private readonly RingSessionService _ringSessionService;
    private readonly OperationModeRingSessionCoordinator _operationModeRingCoordinator;
    private StableZoneCandidateService? _stableZoneCandidateService;
    private AdvanceGroupDecisionService? _advanceGroupDecisionService;
    private AdvancePhysicalConsistencyService? _advancePhysicalConsistencyService;
    private OperationModeState _confirmedOperationMode = OperationModeState.Unknown;
    private string _stableZoneDisplay = "Stabil bölge: Bekleniyor";
    private string _advanceGroupDisplay = "";
    private string? _lastAdvanceDecisionSource;
    private decimal? _lastOfficialAdvanceMm;
    private bool _screenAlignmentValid;
    private MonitoringStatus _lastMonitoringStatus = MonitoringStatus.Ready;
    private string _lastMonitoringDetail = "İzleme başlatılmaya hazır";
    private readonly Dictionary<string, FieldMeasurementResult> _liveDiagnosticFields = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, FieldMeasurementResult> _lastNumericDiagnosticFields = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _liveFieldsSync = new();
    private MeasurementRecord? _lastDisplayedRecord;
    private DashboardCategory _activeDashboardCategory = DashboardCategory.Excavation;
    private OcrFeedbackSnapshotCache _feedbackSnapshots = null!;
    private OcrUserFeedbackService _feedbackService = null!;
    internal DashboardCategory ActiveDashboardCategory => _activeDashboardCategory;
    internal IReadOnlyList<Controls.DashboardCard> DashboardCards => [NominalCard, Scale1Card, Scale2Card, GatePositionCard, GateStatusCard, ExcavationStatusCard];
    internal void SelectDashboardCategoryForTest(DashboardCategory category) { _activeDashboardCategory = category; RefreshDashboardCards(); }
    internal void DisplayMeasurementForTest(MeasurementRecord record) => DisplayMeasurement(record);
    internal void ReceiveMonitoringMeasurementForTest(MeasurementRecord record) => BindMonitoringMeasurement(record);
    internal string LatestHistoryValueForTest(string columnName) => HistoryGrid.Rows.Count == 0 ? "" : Convert.ToString(HistoryGrid.Rows[0].Cells[columnName].Value) ?? "";
    internal string LatestHistoryToolTipForTest(string columnName) => HistoryGrid.Rows.Count == 0 ? "" : HistoryGrid.Rows[0].Cells[columnName].ToolTipText;
    internal static FieldMeasurementResult CreateOfficialAdvanceFieldForTest(AdvanceGroupDecision decision, DateTime timestamp)
        => CreateOfficialAdvanceField(decision, timestamp);
    internal void StopBackgroundServicesForTest()
    {
        clockTimer.Stop(); _performanceMonitor?.Dispose(); _monitoringService.Dispose();
    }

    public MainForm(LogService logService)
    {
        _logService = logService;
        InitializeComponent();
        _ringSessionService = new RingSessionService();
        _operationModeRingCoordinator = new OperationModeRingSessionCoordinator(_ringSessionService);
        _operationModeRingCoordinator.UpdateFailed += (message, exception) => _logService.Warning(message, exception);
        _ringSessionService.PersistenceFailed += (message, exception) => _logService.Warning(message, exception);
        _ringSessionService.RingSessionCreated += session =>
        {
            _stableZoneCandidateService?.BeginRing(session.SessionId); _lastOfficialAdvanceMm = null;
            _lastAdvanceDecisionSource = null; _advanceGroupDisplay = ""; _stableZoneDisplay = "Stabil bölge: Bekleniyor";
            RunOnUiThread(() => { RingNoTextBox.Text = session.RingNo; UpdateRingSessionDisplay(session); });
        };
        _ringSessionService.RingSessionUpdated += _ => RunOnUiThread(() => UpdateRingSessionDisplay(_ringSessionService.GetActive("1031")));
        _ringSessionService.RingSessionCompleted += _ => RunOnUiThread(() => UpdateRingSessionDisplay(null));
        _ringSessionService.RingSessionCancelled += _ => RunOnUiThread(() => UpdateRingSessionDisplay(null));
        if (_ringSessionService.HasIncompleteSessions)
        {
            _logService.Warning($"Tamamlanmamış ring oturumu bulundu. Adet={_ringSessionService.IncompleteSessions.Count}. Kullanıcı onayı olmadan işleme alınmadı.");
            OperationModeLabel.AccessibleDescription = "Tamamlanmamış ring oturumu bulundu.";
        }
        RingStartButton.Click += RingStartButton_Click;
        RingCancelButton.Click += RingCancelButton_Click;
        Shown += (_, _) => ResolveIncompleteRingSession();
        _settings = LoadSettings();
        _feedbackSnapshots = new OcrFeedbackSnapshotCache(_settings.OcrFeedbackSnapshotMaxMeasurements,
            _settings.OcrFeedbackSnapshotMaxAgeMinutes);
        _feedbackService = new OcrUserFeedbackService();
        HistoryGrid.CellClick += HistoryGrid_CellClick;
        _stableZoneCandidateService = new StableZoneCandidateService(_settings.OcrProfile);
        _advanceGroupDecisionService = new AdvanceGroupDecisionService(_settings.OcrProfile);
        _advancePhysicalConsistencyService = new AdvancePhysicalConsistencyService(_settings.OcrProfile);
        ApplyRingTrackingAvailability();
        RefreshDashboardCards();
        CreatePerformanceMonitor();
        SettingsButton.Click += SettingsButton_Click;
        PerformanceButton.Click += (_, _) => ShowPerformancePanel();
        StartButton.Click += StartButton_Click;
        StopButton.Click += StopButton_Click;
        PauseButton.Click += PauseButton_Click;
        _monitoringService.MeasurementReceived += MonitoringService_MeasurementReceived;
        _monitoringService.ReadingCompleted += MonitoringService_ReadingCompleted;
        _monitoringService.ReadingStarted += MonitoringService_ReadingStarted;
        _monitoringService.StatusChanged += MonitoringService_StatusChanged;
        _monitoringService.ReadingFailed += MonitoringService_ReadingFailed;
        _monitoringService.RunStateChanged += state =>
        {
            _operationModeRingCoordinator.SetMonitoringState(state);
            _stableZoneCandidateService?.OnMonitoringStateChanged(state);
            RunOnUiThread(() =>
            {
                if (state == MonitoringRunState.Running) { _lastMonitoringStatus = MonitoringStatus.Active; _lastMonitoringDetail = "İzleme aktif"; }
                else if (state == MonitoringRunState.Paused) { _lastMonitoringStatus = MonitoringStatus.Paused; _lastMonitoringDetail = "İzleme kullanıcı tarafından duraklatıldı."; }
                else if (state == MonitoringRunState.Resuming) { _lastMonitoringStatus = MonitoringStatus.Resuming; _lastMonitoringDetail = "Yeniden doğrulanıyor..."; }
                else { _lastMonitoringStatus = MonitoringStatus.Stopped; _lastMonitoringDetail = "İzleme kapalı"; }
                UpdateMonitoringControls();
            });
        };
        UpdateClock();
        RecoverInterruptedSession();
        UpdateSessionDisplay();
        DisplayMonitoringStatus(MonitoringStatus.Ready, "İzleme başlatılmaya hazır");
        clockTimer.Start();
        if (_settings.ShowPerformancePanel) ShowPerformancePanel();
    }

    private void ClockTimer_Tick(object? sender, EventArgs e)
    {
        UpdateClock();
        UpdateSessionElapsed();
    }

    private void UpdateClock()
    {
        dateLabel.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
        timeLabel.Text = DateTime.Now.ToString("HH:mm:ss");
    }

    private AppSettings LoadSettings()
    {
        try
        {
            return _settingsService.Load();
        }
        catch (SettingsException exception)
        {
            _logService.Warning("Ayarlar yüklenemedi; varsayılan ayarlar kullanılacak.", exception);
            MessageBox.Show(this, $"{exception.Message}\n\nVarsayılan ayarlar kullanılacak.", "Ayar Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return SettingsService.CreateDefaults();
        }
    }

    private void SettingsButton_Click(object? sender, EventArgs e)
    {
        using var form = new SettingsForm(_settingsService, _settings, _logService);
        if (form.ShowDialog(this) == DialogResult.OK && form.SavedSettings is not null)
        {
            _settings = form.SavedSettings;
            _stableZoneCandidateService = new StableZoneCandidateService(_settings.OcrProfile);
            _advanceGroupDecisionService = new AdvanceGroupDecisionService(_settings.OcrProfile);
            _advancePhysicalConsistencyService = new AdvancePhysicalConsistencyService(_settings.OcrProfile);
            ApplyRingTrackingAvailability();
            CreatePerformanceMonitor();
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _monitoringService.Dispose();
        (_activeDataSource as IDisposable)?.Dispose();
        _csvService?.Dispose();
        _ocrPreviewForm?.Dispose();
        _performanceForm?.Dispose();
        _performanceMonitor?.Dispose();
        _feedbackSnapshots.Dispose();
        base.OnFormClosed(e);
    }

    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_allowClose && _monitoringService.IsRunning)
        {
            e.Cancel = true;
            Enabled = false;
            await _monitoringService.StopAsync();
            TryDetachActiveRingFromMonitoringSession();
            CompleteMonitoringSession("Program kapanışı");
            (_activeDataSource as IDisposable)?.Dispose();
            _activeDataSource = null;
            _logService.Information("Form kapanırken izleme güvenli biçimde durduruldu.");
            _allowClose = true;
            Close();
            return;
        }

        base.OnFormClosing(e);
    }

    private async void StartButton_Click(object? sender, EventArgs e)
    {
        if (_monitoringService.IsRunning) return;
        _advanceGroupDecisionService = new AdvanceGroupDecisionService(_settings.OcrProfile);
        _advancePhysicalConsistencyService = new AdvancePhysicalConsistencyService(_settings.OcrProfile);
        _lastAdvanceDecisionSource = null; _advanceGroupDisplay = "";
        _screenAlignmentValid = false;
        IDataSource source;
        if (_settings.DemoMode)
        {
            source = new DemoDataSource();
        }
        else
        {
            var captureService = new ScreenCaptureService();
            try
            {
                captureService.ValidateConfiguration(_settings);
                _screenAlignmentValid = true;
            }
            catch (ScreenCaptureException exception)
            {
                _logService.Warning("OCR ekran bölgeleri geçersiz.", exception);
                MessageBox.Show(this, $"İzleme başlatılamadı.\n\n{exception.Message}", "Ekran Bölgeleri", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var ocrSource = new OcrDataSource(_settings, captureService, new OcrService(), _feedbackSnapshots);
            ocrSource.OcrFailed += OcrDataSource_OcrFailed;
            ocrSource.DiagnosticsAvailable += OcrDataSource_DiagnosticsAvailable;
            ocrSource.PerformanceAvailable += frame => _performanceMonitor?.Record(frame);
            ocrSource.PipelinePerformanceAvailable += frame => _performanceMonitor?.RecordCycle(frame);
            ocrSource.OperationModeDiagnosticAvailable += diagnostic =>
            {
                _confirmedOperationMode = diagnostic.ConfirmedState;
                RunOnUiThread(() =>
                {
                    OperationModeLabel.Text = diagnostic.DisplayText + (diagnostic.CandidateText is null ? "" : $"  •  {diagnostic.CandidateText}")
                        + (diagnostic.IsUnreadable ? "  •  Durum okunamıyor" : "");
                    OperationModeLabel.AccessibleDescription = diagnostic.MatchDetails;
                });
                EvaluateStableZoneModeTransition(diagnostic.ConfirmedState);
                var result = _operationModeRingCoordinator.ProcessConfirmedState("1031", diagnostic.ConfirmedState, DateTime.Now, OperationRingContext());
                HandleOperationRingResult(result, "OperationMode");
            };
            ocrSource.NumericFieldReadAvailable += field =>
            {
                lock (_liveFieldsSync)
                {
                    _liveDiagnosticFields[field.FieldId] = field;
                    if (CanReplaceLastDiagnosticValue(field.FieldId, field))
                        _lastNumericDiagnosticFields[field.FieldId] = field;
                }
                if (field.FieldId is BuiltInFieldIds.StableRpm or BuiltInFieldIds.StableTorque
                    or BuiltInFieldIds.StableThrustForce or BuiltInFieldIds.StableAdvanceSpeed
                    or BuiltInFieldIds.StablePenetration)
                    RunOnUiThread(() => { if (_activeDashboardCategory == DashboardCategory.Excavation) RefreshDashboardCards(); });
            };
            ocrSource.OperationModeChanged += transition =>
            {
                _logService.Information($"Çalışma modu değişti: {transition.PreviousState} -> {transition.NewState}; Event={transition.EventType}");
                EvaluateStableZoneModeTransition(transition.NewState);
                var result = _operationModeRingCoordinator.Process(transition, OperationRingContext());
                HandleOperationRingResult(result, "OperationMode geçişi");
            };
            ocrSource.ValidationDecisionLogged += message => _logService.Information(message);
            ocrSource.DiagnosticWarning += message => _logService.Warning(message);
            ocrSource.LiveFieldTraceAvailable += message => _logService.Information("Canlı stabil OCR zinciri | " + message);
            _logService.Information($"StableParameterLiveOcr=EnabledWhenRegionValid | DetailedDiagnostics={(_settings.EnableStableParameterDiagnostics ? "Enabled" : "Disabled")} | Profile={_settings.OcrProfile.Name}");
            source = ocrSource;
        }

        try
        {
            _csvService?.Dispose();
            _csvService = new CsvService(_settings.CsvOutputFolder);
            _csvService.WriteFailed += CsvService_WriteFailed;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            (source as IDisposable)?.Dispose();
            MessageBox.Show(this, $"CSV kayıt klasörü geçersiz.\n\n{exception.Message}", "CSV Ayarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        _activeDataSource = source;
        LiveChart.ClearMeasurements();
        _session = new MonitoringSessionState { IsActive = true, Machine = source.Name, StartedAt = DateTime.Now };
        _displayedSessionElapsed = TimeSpan.Zero;
        _sessionStore.Save(_session);
        _performanceMonitor?.StartSession(source.Name);
        UpdateSessionDisplay();
        _monitoringService.Start(
            source,
            TimeSpan.FromSeconds(_settings.ReadingIntervalSeconds),
            TimeSpan.FromSeconds(_settings.InactivityWarningSeconds));
        _session.SessionId = _monitoringService.SessionId;
        _sessionStore.Save(_session);
        TryAssociateActiveRingWithMonitoringSession();
        _logService.Information($"İzleme oturumu başlatıldı. Makine={source.Name}; Başlangıç={_session.StartedAt:yyyy-MM-dd HH:mm:ss}; Okuma sıklığı={_settings.ReadingIntervalSeconds} saniye.");
    }

    private async void StopButton_Click(object? sender, EventArgs e)
    {
        await StopMonitoringAsync("Kullanıcı tarafından durduruldu");
    }

    private void RingStartButton_Click(object? sender, EventArgs e)
    {
        if (!IsMachine1031())
        {
            MessageBox.Show(this, "Ring takibi bu aşamada yalnızca 1031 makinesi için kullanılabilir.", "Ring Takibi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            var existing = _ringSessionService.GetActive("1031");
            if (existing is not null)
            {
                MessageBox.Show(this, $"1031 için zaten aktif bir ring oturumu var: {existing.RingNo}", "Ring Takibi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var session = _ringSessionService.Create("1031", RingNoTextBox.Text, _session is { IsActive: true } ? _session.SessionId : null);
            RingNoTextBox.Text = session.RingNo; UpdateRingSessionDisplay(session);
            _logService.Information($"Ring oturumu kullanıcı tarafından başlatıldı. Ring={session.RingNo}; RingSessionId={session.SessionId}; MonitoringSessionId={session.MonitoringSessionId ?? "—"}");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            _logService.Warning("Ring oturumu başlatılamadı.", exception);
            MessageBox.Show(this, exception.Message, "Ring Takibi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RingCancelButton_Click(object? sender, EventArgs e)
    {
        var active = _ringSessionService.GetActive("1031"); if (active is null) return;
        if (MessageBox.Show(this, $"{active.RingNo} numaralı ring oturumu iptal edilecek. Emin misiniz?", "Ring Oturumunu İptal Et",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
        try
        {
            _ringSessionService.Cancel("1031", "Kullanıcı tarafından iptal edildi"); RingNoTextBox.Clear();
            _logService.Information($"Ring oturumu kullanıcı tarafından iptal edildi. Ring={active.RingNo}; RingSessionId={active.SessionId}");
        }
        catch (Exception exception)
        {
            _logService.Error("Ring oturumu iptal edilemedi.", exception);
            MessageBox.Show(this, $"Ring oturumu iptal edilemedi.\n\n{exception.Message}", "Ring Takibi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ResolveIncompleteRingSession()
    {
        var incomplete = _ringSessionService.IncompleteSessions.FirstOrDefault(item => item.MachineId == "1031");
        if (incomplete is null) { UpdateRingSessionDisplay(_ringSessionService.GetActive("1031")); return; }
        using var dialog = new IncompleteRingSessionDialog(incomplete); dialog.ShowDialog(this);
        try
        {
            if (dialog.Choice == IncompleteRingChoice.Continue)
                UpdateRingSessionDisplay(_ringSessionService.ResumeIncomplete(incomplete.SessionId));
            else if (dialog.Choice == IncompleteRingChoice.CancelSession)
            {
                _ringSessionService.CancelIncomplete(incomplete.SessionId, "Açılışta kullanıcı tarafından iptal edildi");
                UpdateRingSessionDisplay(null);
            }
            else
            {
                RingSessionLabel.Text = $"Tamamlanmamış ring: {incomplete.RingNo} | Durum: {incomplete.CurrentState} | Çözülmeden yeni ring başlatılamaz";
                RingStartButton.Enabled = false;
            }
        }
        catch (Exception exception)
        {
            _logService.Error("Tamamlanmamış ring oturumu işlenemedi.", exception);
            MessageBox.Show(this, exception.Message, "Ring Takibi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void TryAssociateActiveRingWithMonitoringSession()
    {
        if (_session is not { IsActive: true } || _ringSessionService.GetActive("1031") is not { } ring || !string.IsNullOrWhiteSpace(ring.MonitoringSessionId)) return;
        try { _ringSessionService.Update("1031", value => value.MonitoringSessionId = _session.SessionId, "İzleme oturumuyla ilişkilendirildi"); }
        catch (Exception exception) { _logService.Warning("Ring oturumu izleme oturumuyla ilişkilendirilemedi; izleme devam ediyor.", exception); }
    }

    private void TryDetachActiveRingFromMonitoringSession()
    {
        if (_ringSessionService.GetActive("1031") is not { MonitoringSessionId: not null }) return;
        try { _ringSessionService.Update("1031", value => value.MonitoringSessionId = null, "İzleme oturumu bağlantısı sonlandırıldı"); }
        catch (Exception exception) { _logService.Warning("Ring oturumu izleme bağlantısı sonlandırılamadı; ring korunuyor.", exception); }
    }

    private OperationModeRingContext OperationRingContext() => new(_settings.OperationModeRegion.IsValid,
        _screenAlignmentValid, _lastOfficialAdvanceMm, _session?.SessionId);

    private void HandleOperationRingResult(OperationModeRingResult result, string source)
    {
        if (!result.Updated) return;
        _logService.Information($"RingSession {source} ile güncellendi: {result.Reason}; Ring={result.Session?.RingNo}; Pending={result.PendingPreviousRing?.RingNo}");
        if (result.NextRingCreated && result.Session is not null)
        {
            _lastOfficialAdvanceMm = null; _lastAdvanceDecisionSource = null; _advanceGroupDisplay = "";
            RunOnUiThread(() =>
            {
                RingNoTextBox.Text = result.Session.RingNo;
                _lastMonitoringDetail = result.Reason;
                UpdateRingSessionDisplay(result.Session);
            });
        }
    }

    private bool IsMachine1031() => _settings.OcrProfile.Name.Contains("1031", StringComparison.OrdinalIgnoreCase);
    private bool HasAdvanceGroupRegions() => _settings.AdvanceGroupARegion.IsValid || _settings.AdvanceGroupBRegion.IsValid
        || _settings.AdvanceGroupCRegion.IsValid || _settings.AdvanceGroupDRegion.IsValid;

    private void EvaluateStableZoneModeTransition(OperationModeState mode)
    {
        if (mode != OperationModeState.RingBuilding || _stableZoneCandidateService is null) return;
        try
        {
            var ring = _ringSessionService.GetActive("1031"); if (ring is null) return;
            var regionValid = HasAdvanceGroupRegions();
            var result = _stableZoneCandidateService.Evaluate(new StableZoneCandidateInput(DateTime.Now, "1031", ring,
                null, false, false, regionValid, _screenAlignmentValid, mode, _monitoringService.State));
            _stableZoneDisplay = result.DisplayText;
            if (result.NewlyMissed) _logService.Warning($"Stabil bölge kaçırıldı. Ring={ring.RingNo}; {result.Reason}");
            RunOnUiThread(() => UpdateRingSessionDisplay(ring));
        }
        catch (Exception exception) { _logService.Warning("RingBuilding stabil bölge durumuna uygulanamadı; izleme devam ediyor.", exception); }
    }

    private void UpdateRingSessionDisplay(RingSession? session)
    {
        if (session is null)
        {
            var waitingRing = _ringSessionService.GetPendingFinalData("1031").FirstOrDefault();
            var waitingText = waitingRing is null ? "" : $" | Önceki ring {waitingRing.RingNo}: Son veriler bekleniyor";
            RingSessionLabel.Text = $"Aktif ring: Yok{waitingText}\nDurum: — | {_stableZoneDisplay} | Kazı sonu: Bekliyor | PPS: Bekliyor | Excel: Bekliyor";
            RingCancelButton.Enabled = false; RingStartButton.Enabled = IsMachine1031();
            if (_activeDashboardCategory == DashboardCategory.Ring) RefreshDashboardCards(); return;
        }
        var stateText = session.CurrentState switch
        {
            RingSessionState.WaitingForExcavation => "Kazı bekleniyor",
            RingSessionState.ExcavationInProgress => "Kazı devam ediyor",
            RingSessionState.RingBuilding => "Ring montajı",
            RingSessionState.FinalDataPending => "Son veriler bekleniyor",
            _ => session.CurrentState.ToString()
        };
        var midExcavation = session.WasMonitoringStartedMidExcavation ? " • Bu ring kazının ortasında izlenmeye başlandı." : "";
        var pending = _ringSessionService.GetPendingFinalData("1031").FirstOrDefault();
        var pendingText = pending is null || pending.SessionId == session.SessionId ? ""
            : $" | Önceki ring {pending.RingNo}: Son veriler bekleniyor";
        RingSessionLabel.Text = $"Aktif ring: {session.RingNo} | Durum: {stateText}{midExcavation}{pendingText}\n{_stableZoneDisplay} | Kazı sonu: {(session.EndDataCaptured ? "Hazır" : "Bekliyor")} | PPS: {(session.PpsDataReceived ? "Hazır" : "Bekliyor")} | Excel: {(session.ExcelWritten ? "Yazıldı" : "Bekliyor")}";
        RingSessionLabel.AccessibleDescription = RingSessionLabel.Text; RingCancelButton.Enabled = true; RingStartButton.Enabled = false;
        if (_activeDashboardCategory == DashboardCategory.Ring) RefreshDashboardCards();
    }

    private void ApplyRingTrackingAvailability()
    {
        var available = IsMachine1031(); RingTrackingPanel.Enabled = available;
        if (!available)
        {
            RingSessionLabel.Text = "Ring Takibi yalnızca 1031 profili için kullanılabilir.";
            RingStartButton.Enabled = false; RingCancelButton.Enabled = false; RingNoTextBox.Enabled = false;
        }
        else RingNoTextBox.Enabled = true;
    }

    private async void PauseButton_Click(object? sender, EventArgs e)
    {
        try
        {
            if (_monitoringService.State == MonitoringRunState.Running)
            {
                await _monitoringService.PauseAsync();
                if (_session is not null)
                {
                    _session.PausedAt = DateTime.Now;
                    _session.LastOcrStatus = "İzleme kullanıcı tarafından duraklatıldı.";
                    _sessionStore.Save(_session);
                }
            }
            else if (_monitoringService.State == MonitoringRunState.Paused)
            {
                if (_session?.PausedAt is { } pausedAt) _session.AccumulatedPausedDuration += DateTime.Now - pausedAt;
                if (_session is not null)
                {
                    _session.PausedAt = null; _session.LastOcrStatus = "Yeniden doğrulanıyor..."; _sessionStore.Save(_session);
                }
                await _monitoringService.ResumeAsync();
            }
        }
        catch (Exception exception)
        {
            _logService.Error("İzleme duraklatma/devam ettirme hatası.", exception);
            MessageBox.Show(this, "İşlem tamamlanamadı. İzleme güvenli biçimde duraklatıldı.", "İzleme", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally { UpdateMonitoringControls(); UpdateSessionDisplay(); }
    }

    private async Task StopMonitoringAsync(string reason)
    {
        try
        {
            await _monitoringService.StopAsync();
            TryDetachActiveRingFromMonitoringSession();
            (_activeDataSource as IDisposable)?.Dispose();
            _activeDataSource = null;
            CompleteMonitoringSession(reason);
            LogPerformanceSummary(_performanceMonitor?.StopSession());
        }
        finally { UpdateMonitoringControls(); }
    }

    private void MonitoringService_ReadingCompleted(MeasurementRecord record)
    {
        ProcessAdvanceGroups(record);
        RunOnUiThread(() =>
        {
            if (_session is null || !_session.IsActive) return;
            _session.LastOcrStatus = ConfidenceText(record.OcrConfidence);
            if (record.OcrConfidence != OcrConfidenceStatus.Unreadable) _session.LastSuccessfulDataAt = record.Timestamp;
            _sessionStore.Save(_session);
            UpdateSessionDisplay();
        });
    }

    private void ProcessAdvanceGroups(MeasurementRecord record)
    {
        if (!IsMachine1031() || !HasAdvanceGroupRegions() || _advanceGroupDecisionService is null) return;
        try
        {
            var definitions = new[]
            {
                ("A", BuiltInFieldIds.AdvanceGroupA, _settings.AdvanceGroupARegion), ("B", BuiltInFieldIds.AdvanceGroupB, _settings.AdvanceGroupBRegion),
                ("C", BuiltInFieldIds.AdvanceGroupC, _settings.AdvanceGroupCRegion), ("D", BuiltInFieldIds.AdvanceGroupD, _settings.AdvanceGroupDRegion)
            };
            var readings = definitions.Select(item =>
            {
                record.FieldResults.TryGetValue(item.Item2, out var field);
                var observed = field?.ObservedValue ?? field?.ParsedValue ?? field?.FullLineResult ?? field?.OfficialValue;
                return new AdvanceGroupReading(item.Item1, observed, item.Item3.IsValid,
                    observed.HasValue && field?.ConfidenceStatus != OcrConfidenceStatus.Unreadable, field?.CacheUsed == true)
                { CapturedAt = field?.CapturedAt ?? record.Timestamp, RawText = field?.RawValue ?? "" };
            }).ToArray();
            var ring = _ringSessionService.GetActive("1031");
            var decision = _advanceGroupDecisionService.Decide(record.Timestamp, "1031", ring?.RingNo, readings, ring?.SessionId, _confirmedOperationMode);
            var candidateEligible = decision.DecisionSource is "MultiGroupAgreement" or "PrimaryGroupTemporalConfirmation";
            if (candidateEligible && ring is not null && _advancePhysicalConsistencyService is not null)
            {
                var physical = _advancePhysicalConsistencyService.Evaluate(new AdvancePhysicalConsistencyInput(
                    "1031", ring.RingNo, ring.SessionId, decision.PreviousOfficialAdvanceMm, decision.DecisionInputValue,
                    _confirmedOperationMode, ring.CurrentState, record.Timestamp, ring.WasMonitoringStartedMidExcavation,
                    decision.TemporalConfirmed || decision.DecisionSource == "MultiGroupAgreement",
                    decision.AllContributingReadsReal, _monitoringService.State));
                var physicalRejected = !physical.AcceptedForAdvanceDecision && physical.PhysicalStatus != AdvancePhysicalStatus.NotApplied;
                decision = decision with
                {
                    OfficialAdvanceMm = physical.PhysicalStatus == AdvancePhysicalStatus.NotApplied
                        ? decision.OfficialAdvanceMm : physical.OfficialAdvanceMm,
                    ConfidenceStatus = physicalRejected ? OcrConfidenceStatus.Partial : decision.ConfidenceStatus,
                    Reason = physicalRejected ? (decision.Reason + " " + physical.Reason).Trim() : decision.Reason,
                    PhysicalAccepted = !physicalRejected,
                    PhysicalStatus = physical.PhysicalStatus,
                    PhysicalReason = physical.Reason
                };
            }
            _advanceGroupDecisionService.CommitOfficial(decision.OfficialAdvanceMm);
            _lastOfficialAdvanceMm = decision.OfficialAdvanceMm;
            record.FieldResults[BuiltInFieldIds.AdvanceMm] = CreateOfficialAdvanceField(decision, record.Timestamp);
            var enabledGroups = new HashSet<string>(_settings.OcrProfile.AdvanceEnabledGroups, StringComparer.OrdinalIgnoreCase);
            var groups = string.Join(" | ", readings.Select(value => $"{value.Group}:{(value.Value.HasValue ? $"{value.Value:0} mm" : "—")}" +
                (!enabledGroups.Contains(value.Group) || value.Value == 0 ? " (devre dışı/sensör çalışmıyor)" : "")));
            _advanceGroupDisplay = $"{groups} | Resmî:{(decision.OfficialAdvanceMm.HasValue ? $"{decision.OfficialAdvanceMm:0} mm" : "—")} | Kaynak:{decision.DecisionSource} | Fark:{decision.ActualGroupDifferenceMm:0} mm | Güven:{decision.ConfidenceStatus}";
            if (decision.TemporalPending && !string.IsNullOrWhiteSpace(decision.TemporalStatus))
                _advanceGroupDisplay += $" | Yeni Advance değeri doğrulama bekliyor: {decision.TemporalStatus.Replace("Şüpheli değer bekletiliyor: ", "", StringComparison.Ordinal)}";
            if (!decision.PhysicalAccepted)
                _advanceGroupDisplay += $" | İlerleme OCR: {decision.DecisionInputValue:0} mm ⚠ | Resmî: {decision.OfficialAdvanceMm:0} mm | Neden: {decision.PhysicalReason}";
            foreach (var item in definitions)
            {
                record.FieldResults.TryGetValue(item.Item2, out var field);
                _logService.Information($"Advance canlı zincir | Timestamp={DateTime.Now:O} | Field={item.Item2} | CapturedAt={field?.CapturedAt:O} | RawOcrText={field?.RawValue ?? ""} | ParsedValue={field?.ParsedValue?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? ""} | CacheHit={field?.CacheUsed == true} | TemporalCandidate={decision.TemporalStatus ?? "—"} | TemporalAcceptedValue={decision.OfficialAdvanceMm?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? ""} | DecisionInputValue={decision.DecisionInputValue?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? ""} | OfficialAdvanceMm={decision.OfficialAdvanceMm?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? ""} | DisplayedValue={decision.OfficialAdvanceMm?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? ""}");
            }
            if (_lastAdvanceDecisionSource is not null && !string.Equals(_lastAdvanceDecisionSource, decision.DecisionSource, StringComparison.Ordinal))
                _logService.Information($"Resmî ilerleme kaynağı değişti: {_lastAdvanceDecisionSource} -> {decision.DecisionSource}. {decision.Reason}");
            _lastAdvanceDecisionSource = decision.DecisionSource;
            foreach (var warning in decision.Warnings) _logService.Warning(warning);
            var stable = _stableZoneCandidateService?.Evaluate(new StableZoneCandidateInput(record.Timestamp, "1031", ring,
                decision.OfficialAdvanceMm, decision.IsReliable, decision.AllContributingReadsReal,
                readings.Any(value => value.RegionValid), _screenAlignmentValid, _confirmedOperationMode, _monitoringService.State));
            if (stable is not null)
            {
                _stableZoneDisplay = stable.DisplayText;
                var bypass = stable.Candidate && !stable.Confirmed;
                if (_activeDataSource is OcrDataSource source)
                    foreach (var fieldId in definitions.Select(value => value.Item2)) source.SetFieldCacheBypass(fieldId, bypass);
            }
            RunOnUiThread(() => { OperationModeLabel.Text = $"{OperationModeLabel.Text}  •  {_advanceGroupDisplay}";
                OperationModeLabel.AccessibleDescription = _advanceGroupDisplay + " | " + decision.Reason; UpdateRingSessionDisplay(ring); });
        }
        catch (Exception exception) { _logService.Warning("İlerleme grup kararı üretilemedi; ana izleme devam ediyor.", exception); }
    }

    private static FieldMeasurementResult CreateOfficialAdvanceField(AdvanceGroupDecision decision, DateTime timestamp) => new()
    {
        FieldId = BuiltInFieldIds.AdvanceMm,
        ParsedValue = decision.OfficialAdvanceMm,
        OfficialValue = decision.OfficialAdvanceMm,
        ObservedValue = decision.DecisionInputValue,
        AcceptedValue = decision.OfficialAdvanceMm,
        DecisionSource = $"AdvanceGroupDecision/{decision.DecisionSource}",
        ConfidenceStatus = decision.ConfidenceStatus,
        FailureReason = !decision.PhysicalAccepted ? "PhysicalSuspicious" : decision.IsReliable ? "Success" : decision.Reason,
        CapturedAt = timestamp,
        CompletedAt = DateTime.Now,
        CacheUsed = !decision.AllContributingReadsReal,
        ValidationState = !decision.PhysicalAccepted ? "PhysicalSuspicious" : decision.TemporalPending ? "SuspiciousCandidate" : "Accepted"
    };

    private void MonitoringService_ReadingStarted()
    {
        RunOnUiThread(() =>
        {
            if (_session is null || !_session.IsActive) return;
            _session.ProcessedReadings++;
            _session.LastOcrStatus = "Veri işleniyor...";
            _sessionStore.Save(_session);
            UpdateSessionDisplay();
        });
    }

    private async void MonitoringService_MeasurementReceived(MeasurementRecord record)
    {
        LogReceivedStableDiagnostics(record);
        RunOnUiThread(() => BindMonitoringMeasurement(record));
        var csvService = _csvService;
        if (csvService is not null && !record.HasHeldSuspiciousValue)
        {
            await csvService.WriteIfChangedAsync(record);
        }
    }

    private void BindMonitoringMeasurement(MeasurementRecord record)
    {
        if (record.OcrConfidence == OcrConfidenceStatus.Reliable)
            DisplayMonitoringStatus(MonitoringStatus.Active, "Okuma başarılı");
        DisplayMeasurement(record);
    }

    private void LogReceivedStableDiagnostics(MeasurementRecord record)
    {
        if (!_settings.EnableStableParameterDiagnostics) return;
        var fields = new[] { record.DiagnosticRpm, record.DiagnosticTorque, record.DiagnosticThrustForce,
            record.DiagnosticAdvanceSpeed, record.DiagnosticPenetration };
        foreach (var field in fields.Where(field => field is not null))
        {
            var value = ResolveDiagnosticDisplayValue(field!.FieldId, field);
            _logService.Information($"Canlı stabil OCR alındı | Field={field.FieldId} | ReceivedByMainForm=true | ParsedValue={field.ParsedValue?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null"} | OfficialValue={field.OfficialValue?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null"} | FailureReason={field.FailureReason} | CardDisplayValue={value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "—"} | HistoryDisplayValue={value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "—"}");
        }
    }

    private void MonitoringService_StatusChanged(MonitoringStatus status, string detail)
    {
        RunOnUiThread(() => DisplayMonitoringStatus(status, detail));
    }

    private void MonitoringService_ReadingFailed(Exception exception)
    {
        _logService.Error("Veri okuma hatası.", exception);
        RunOnUiThread(() =>
        {
            ExcavationStatusCard.Detail = exception.Message;
            if (_session is not null) { _session.LastOcrStatus = "Hata"; _sessionStore.Save(_session); }
            SetSessionStatus("HATA", Color.FromArgb(254, 202, 202));
            UpdateSessionDisplay();
        });
    }

    private void CsvService_WriteFailed(string filePath, Exception exception)
    {
        _logService.Error($"CSV yazma hatası. Dosya: {filePath}", exception);
        RunOnUiThread(() =>
        {
            ExcavationStatusCard.Detail = $"CSV yazılamadı: {exception.Message}";
            MonitoringStatusLabel.AccessibleDescription = filePath;
        });
    }

    private void OcrDataSource_OcrFailed(Exception exception)
    {
        _logService.Error("OCR okuma hatası.", exception);
        RunOnUiThread(() =>
        {
            DisplayMonitoringStatus(MonitoringStatus.OcrError, exception.Message);
            ExcavationStatusCard.Value = "Ekran Değerleri Okunamadı";
            ExcavationStatusCard.Detail = exception.Message;
        });
    }

    private void OcrDataSource_DiagnosticsAvailable(OcrDiagnosticsFrame frame)
    {
        if (IsDisposed || Disposing)
        {
            frame.Dispose();
            return;
        }

        RunOnUiThread(() =>
        {
            if (_ocrPreviewForm is null || _ocrPreviewForm.IsDisposed)
            {
                _ocrPreviewForm = new OcrPreviewForm
                {
                    Location = new Point(Right + 10, Top)
                };
                _ocrPreviewForm.FormClosed += (_, _) => _ocrPreviewForm = null;
                _ocrPreviewForm.Show(this);
            }

            _ocrPreviewForm.UpdateFrame(frame);
        });
    }

    private void DisplayMeasurement(MeasurementRecord record)
    {
        if (!record.HasHeldSuspiciousValue) LiveChart.AddMeasurement(record);
        _lastDisplayedRecord = record;
        lock (_liveFieldsSync)
            foreach (var pair in record.FieldResults)
            {
                _liveDiagnosticFields[pair.Key] = pair.Value;
                if (IsStableDiagnosticField(pair.Key) && CanReplaceLastDiagnosticValue(pair.Key, pair.Value))
                    _lastNumericDiagnosticFields[pair.Key] = pair.Value;
            }
        RefreshDashboardCards();

        var epbHistory = BuildEpbHistoryValues(record);
        HistoryGrid.Rows.Insert(0,
            record.Timestamp.ToString("HH:mm:ss"),
            HistoryDiagnostic(BuiltInFieldIds.StableRpm, record.DiagnosticRpm, "0.00"),
            HistoryDiagnostic(BuiltInFieldIds.StableTorque, record.DiagnosticTorque, "0.000"),
            HistoryDiagnostic(BuiltInFieldIds.StableThrustForce, record.DiagnosticThrustForce, "0"),
            HistoryDiagnostic(BuiltInFieldIds.StableAdvanceSpeed, record.DiagnosticAdvanceSpeed, "0.0"),
            HistoryDiagnostic(BuiltInFieldIds.StablePenetration, record.DiagnosticPenetration, "0.0"),
            HistoryOcrReading(record.FieldResults.GetValueOrDefault(BuiltInFieldIds.Nominal), record.Nominal),
            HistoryOcrReading(record.FieldResults.GetValueOrDefault(BuiltInFieldIds.Scale1), record.Scale1),
            HistoryOcrReading(record.FieldResults.GetValueOrDefault(BuiltInFieldIds.Scale2), record.Scale2),
            HistoryDiagnostic(BuiltInFieldIds.NominalInjection, record.NominalInjectionObservation, "0.0"),
            HistoryDiagnostic(BuiltInFieldIds.RealInjection, record.RealInjectionObservation, "0.0"),
            epbHistory[0], epbHistory[1], epbHistory[2], epbHistory[3], epbHistory[4],
            OcrHistorySymbol(record));
        HistoryGrid.Rows[0].Tag = record.MeasurementId;

        ApplyDiagnosticHistoryQuality(record, HistoryGrid.Rows[0]);
        ApplyOfficialHistoryQuality(record, HistoryGrid.Rows[0]);
        ApplyFeedbackAnnotations(HistoryGrid.Rows[0],record.MeasurementId);
        LogDiagnosticVisibility(record);

        var ocrCell = HistoryGrid.Rows[0].Cells["OCR"];
        ocrCell.ToolTipText = record.HasHeldSuspiciousValue ? "Şüpheli değer doğrulama bekliyor"
            : HasStableDiagnosticWarning(record) ? "Geçerli full-line OCR mevcut; karakter doğrulaması başarısız." : ConfidenceText(record.OcrConfidence);
        ocrCell.Style.Font = new Font(HistoryGrid.Font, FontStyle.Bold);
        ocrCell.Style.ForeColor = record.HasHeldSuspiciousValue || HasStableDiagnosticWarning(record) ? Color.FromArgb(251, 191, 36) : record.OcrConfidence switch
        { OcrConfidenceStatus.Reliable => Color.FromArgb(52, 211, 153), OcrConfidenceStatus.Partial => Color.FromArgb(251, 191, 36), _ => Color.FromArgb(148, 163, 184) };

        if (HistoryGrid.Rows.Count > 500) HistoryGrid.Rows.RemoveAt(HistoryGrid.Rows.Count - 1);
    }

    private void HistoryGrid_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
        var row=HistoryGrid.Rows[e.RowIndex]; var fieldId=HistoryFieldId(HistoryGrid.Columns[e.ColumnIndex].Name);
        if (fieldId is null || row.Tag is not string measurementId) return;
        var cell=row.Cells[e.ColumnIndex]; var warning=(Convert.ToString(cell.Value)??"").Contains('⚠') || cell.Style.ForeColor==Color.FromArgb(251,191,36);
        if (!warning && _feedbackService.Latest(measurementId,fieldId) is null) return;
        if (!_feedbackSnapshots.TryGet(measurementId,fieldId,out var snapshot) || snapshot is null)
        { MessageBox.Show(this,"Bu OCR okumasına ait görüntü artık önbellekte bulunmuyor.","OCR Geri Bildirimi",MessageBoxButtons.OK,MessageBoxIcon.Information);return; }
        using(snapshot)
        {
            var definition=_settings.OcrProfile.FindField(fieldId); if(definition is null)return;
            var ring=_ringSessionService.GetActive("1031");
            using var form=new OcrFeedbackForm(snapshot,definition,_feedbackService,"1031",_settings.OcrProfile.Name,ring?.RingNo,ring?.SessionId);
            if(form.ShowDialog(this)==DialogResult.OK&&form.SavedFeedback is not null)
                ApplyFeedbackAnnotation(cell,measurementId,fieldId);
        }
    }

    private void ApplyFeedbackAnnotations(DataGridViewRow row,string measurementId)
    {
        foreach(DataGridViewColumn column in HistoryGrid.Columns)
            if(HistoryFieldId(column.Name)is{ } fieldId)ApplyFeedbackAnnotation(row.Cells[column.Index],measurementId,fieldId);
    }

    private void ApplyFeedbackAnnotation(DataGridViewCell cell,string measurementId,string fieldId)
    {
        var state=cell.Tag as FeedbackCellState;
        if(state is null||!state.MeasurementId.Equals(measurementId,StringComparison.Ordinal)||!state.FieldId.Equals(fieldId,StringComparison.OrdinalIgnoreCase))
            state=new FeedbackCellState(measurementId,fieldId,Convert.ToString(cell.Value)??"—",cell.ToolTipText??"");
        cell.Tag=state;
        var annotation=OcrFeedbackUiAnnotation.Create(state.BaseText,state.BaseTooltip,_feedbackService.Latest(measurementId,fieldId));
        cell.Value=annotation.DisplayText;cell.ToolTipText=annotation.Tooltip;
    }

    private static string? HistoryFieldId(string column)=>column switch
    { "Rpm"=>BuiltInFieldIds.StableRpm,"Torque"=>BuiltInFieldIds.StableTorque,"Thrust"=>BuiltInFieldIds.StableThrustForce,
      "Speed"=>BuiltInFieldIds.StableAdvanceSpeed,"Penetration"=>BuiltInFieldIds.StablePenetration,
      "Nominal"=>BuiltInFieldIds.Nominal,"Scale1"=>BuiltInFieldIds.Scale1,"Scale2"=>BuiltInFieldIds.Scale2,
      "NominalInjection"=>BuiltInFieldIds.NominalInjection,"RealInjection"=>BuiltInFieldIds.RealInjection,
      "Epb1"=>BuiltInFieldIds.Epb1,"Epb2"=>BuiltInFieldIds.Epb2,"Epb3"=>BuiltInFieldIds.Epb3,"Epb4"=>BuiltInFieldIds.Epb4,"Epb5"=>BuiltInFieldIds.Epb5,_=>null};

    private void CategoryButton_Click(object? sender, EventArgs e)
    {
        if (sender is not Button { Tag: string category }
            || !Enum.TryParse<DashboardCategory>(category, true, out var selected)) return;
        _activeDashboardCategory = selected;
        RefreshDashboardCards();
    }

    private void RefreshDashboardCards()
    {
        if (IsDisposed || Disposing) return;
        UpdateCategoryButtons();
        var record = _lastDisplayedRecord;
        if (_activeDashboardCategory == DashboardCategory.Material)
        {
            SetCard(NominalCard, "NOMİNAL DEĞER", FormatValue(record?.Nominal), record?.NominalValidationStatus ?? "Birikimli nominal değer", Color.FromArgb(56, 189, 248));
            SetCard(Scale1Card, "KANTAR 1", FormatValue(record?.Scale1), record?.Scale1ValidationStatus ?? "Bağımsız ölçüm", Color.FromArgb(52, 211, 153));
            SetCard(Scale2Card, "KANTAR 2", FormatValue(record?.Scale2), record?.Scale2ValidationStatus ?? "Bağımsız ölçüm", Color.FromArgb(167, 139, 250));
            SetCard(GatePositionCard, "KAPAK KONUMU", record?.GateMillimeters is { } gate ? $"{gate:0.##} mm" : "—", "Helezon boşaltma kapağı", Color.FromArgb(251, 191, 36));
            SetCard(GateStatusCard, "KAPAK DURUMU", record?.GateStatus ?? "Okunamadı", record?.GateMillimeters > 100 ? "Malzeme akışı var" : "Malzeme akışı yok", Color.FromArgb(248, 113, 113));
            SetCard(ExcavationStatusCard, "KAZI DURUMU", record?.ExcavationStatus ?? "İzleme Kapalı", record is null ? "Henüz ölçüm yok" : $"Son güncelleme: {record.Timestamp:HH:mm:ss}", Color.FromArgb(148, 163, 184));
            return;
        }
        if (_activeDashboardCategory == DashboardCategory.Ring)
        {
            var ring = _ringSessionService.GetActive("1031");
            var state = ring?.CurrentState switch { RingSessionState.WaitingForExcavation => "Kazı bekleniyor", RingSessionState.ExcavationInProgress => "Kazı devam ediyor", RingSessionState.RingBuilding => "Ring montajı", _ => "—" };
            var stable = _stableZoneDisplay.Replace("Stabil bölge:", "", StringComparison.OrdinalIgnoreCase).Trim();
            SetCard(NominalCard, "RING", ring?.RingNo ?? "—", "Aktif RingSession", Color.FromArgb(56, 189, 248));
            SetCard(Scale1Card, "STABİL BÖLGE", stable, "Mevcut StableZone durumu", Color.FromArgb(52, 211, 153));
            SetCard(Scale2Card, "KAZI SONU", ring?.EndDataCaptured == true ? "Hazır" : "Bekleniyor", "Mevcut RingSession bilgisi", Color.FromArgb(167, 139, 250));
            SetCard(GatePositionCard, "PPS", ring?.PpsDataReceived == true ? "Hazır" : "Bekleniyor", "Mevcut RingSession bilgisi", Color.FromArgb(251, 191, 36));
            SetCard(GateStatusCard, "EXCEL", ring?.ExcelWritten == true ? "Yazıldı" : "Bekleniyor", "Mevcut RingSession bilgisi", Color.FromArgb(248, 113, 113));
            SetCard(ExcavationStatusCard, "DURUM", state, "Yeni durum üretilmez", Color.FromArgb(148, 163, 184));
            return;
        }
        SetDiagnosticCard(NominalCard, "RPM", BuiltInFieldIds.StableRpm, "rpm", "0.00", Color.FromArgb(56, 189, 248));
        SetDiagnosticCard(Scale1Card, "TORK", BuiltInFieldIds.StableTorque, "MNm", "0.000", Color.FromArgb(52, 211, 153));
        SetDiagnosticCard(Scale2Card, "İTME KUVVETİ", BuiltInFieldIds.StableThrustForce, "kN", "0", Color.FromArgb(167, 139, 250));
        SetDiagnosticCard(GatePositionCard, "İLERLEME HIZI", BuiltInFieldIds.StableAdvanceSpeed, "mm/min", "0.0", Color.FromArgb(251, 191, 36));
        SetDiagnosticCard(GateStatusCard, "PENETRASYON", BuiltInFieldIds.StablePenetration, "mm/rot", "0.0", Color.FromArgb(248, 113, 113));
        SetDiagnosticCard(ExcavationStatusCard, "İLERLEME", BuiltInFieldIds.AdvanceMm, "mm", "0", Color.FromArgb(148, 163, 184));
    }

    private void SetDiagnosticCard(Controls.DashboardCard card, string caption, string fieldId, string unit, string format, Color accent)
    {
        var field = fieldId switch
        {
            BuiltInFieldIds.StableRpm => _lastDisplayedRecord?.DiagnosticRpm,
            BuiltInFieldIds.StableTorque => _lastDisplayedRecord?.DiagnosticTorque,
            BuiltInFieldIds.StableThrustForce => _lastDisplayedRecord?.DiagnosticThrustForce,
            BuiltInFieldIds.StableAdvanceSpeed => _lastDisplayedRecord?.DiagnosticAdvanceSpeed,
            BuiltInFieldIds.StablePenetration => _lastDisplayedRecord?.DiagnosticPenetration,
            _ => null
        };
        if (field is null) lock (_liveFieldsSync) _liveDiagnosticFields.TryGetValue(fieldId, out field);
        var currentField = field;
        var value = ResolveDiagnosticDisplayValue(fieldId, currentField);
        var observedForDetail = value;
        var showTrusted = field?.TrustedValue.HasValue == true
            && field.FailureReason is "TemporalSuspicious" or "DecimalStructureIncomplete" or "DigitStructureSuspicious"
            && field.FeedbackDecisionLevel is not ("Verified" or "Authoritative");
        if (showTrusted) value = field!.TrustedValue;
        var usingLastNumeric = false;
        if (!value.HasValue)
        {
            lock (_liveFieldsSync)
                if (_lastNumericDiagnosticFields.TryGetValue(fieldId, out var lastNumeric))
                { field = lastNumeric; value = ResolveDiagnosticDisplayValue(fieldId, lastNumeric); usingLastNumeric = value.HasValue; }
        }
        var text = value.HasValue ? $"{value.Value.ToString(format, System.Globalization.CultureInfo.InvariantCulture)} {unit}" : "—";
        var time = field is null ? "" : $" • {field.CapturedAt:HH:mm:ss}";
        var detail = field is null ? "Henüz canlı okuma yok"
            : usingLastNumeric ? $"Son canlı değer • yeni okuma başarısız{time}"
            : field.FailureReason is "Success" or "" ? $"Canlı OCR • Güvenilir{time}"
            : showTrusted
                ? $"Yeni OCR: {observedForDetail?.ToString(format, System.Globalization.CultureInfo.InvariantCulture) ?? "—"} {unit} ⚠ • doğrulama bekliyor{time}"
            : field.FailureReason is "TemporalSuspicious" or "DecimalStructureIncomplete" or "DigitStructureSuspicious"
                ? $"⚠ {FriendlyQuality(field)} • güvenilir son: {field.TrustedValue?.ToString(format, System.Globalization.CultureInfo.InvariantCulture) ?? "—"}{time}"
                : $"Canlı OCR • ⚠ {FriendlyQuality(field)}{time}";
        if (field?.FeedbackDecisionLevel is "Verified" or "Authoritative")
            detail += $" • Kullanıcı geri bildirimi ({field.FeedbackSimilarityPercent:0.0}%)";
        SetCard(card, caption, text, detail, accent);
    }

    private void UpdateCategoryButtons()
    {
        Style(ExcavationCategoryButton, DashboardCategory.Excavation); Style(MaterialCategoryButton, DashboardCategory.Material); Style(RingCategoryButton, DashboardCategory.Ring);
        void Style(Button button, DashboardCategory category)
        {
            var active = category == _activeDashboardCategory; button.BackColor = active ? Color.FromArgb(3, 105, 161) : Color.FromArgb(30, 41, 59);
            button.ForeColor = active ? Color.White : Color.FromArgb(148, 163, 184);
        }
    }

    private static void SetCard(Controls.DashboardCard card, string caption, string value, string detail, Color accent)
    { card.Caption = caption; card.Value = value; card.Detail = detail; card.AccentColor = accent; card.Invalidate(); }

    private string HistoryDiagnostic(string fieldId, FieldMeasurementResult? field, string format)
    {
        var value = ResolveHistoryDiagnosticValue(fieldId, field);
        return FormatDiagnosticHistory(value, field?.FailureReason, format);
    }

    private decimal? ResolveHistoryDiagnosticValue(string fieldId, FieldMeasurementResult? field)
    {
        if (field is null) return null;
        // Ölçüm Geçmişi canlı OCR denetimi içindir: bu karede gözlenen değer,
        // kartta korunabilen önceki TrustedValue'dan her zaman önce gelir.
        if (field.ObservedValue.HasValue) return field.ObservedValue;
        if (field.ParsedValue.HasValue) return field.ParsedValue;
        if (field.FullLineResult.HasValue) return field.FullLineResult;
        if (field.CharacterResult.HasValue) return field.CharacterResult;
        if (fieldId == BuiltInFieldIds.StableTorque) return null;
        var definition = _settings.OcrProfile.FindField(fieldId);
        return definition is not null && NumericFieldParser.TryParse(field.RawValue, definition, out var parsed, out _)
            ? parsed : null;
    }

    internal static string FormatDiagnosticHistory(decimal? value, string? failureReason, string format)
    {
        if (!value.HasValue) return "—";
        var text = value.Value.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
        return failureReason is not null and not "" and not "Success" ? text + " ⚠" : text;
    }

    internal static string[] BuildEpbHistoryValues(MeasurementRecord record)
    {
        var ids = new[] { BuiltInFieldIds.Epb1, BuiltInFieldIds.Epb2, BuiltInFieldIds.Epb3, BuiltInFieldIds.Epb4, BuiltInFieldIds.Epb5 };
        return ids.Select(id => record.FieldResults.TryGetValue(id, out var field)
            ? FormatDiagnosticHistory(field.ParsedValue ?? field.FullLineResult ?? field.CharacterResult, field.FailureReason, "0.00")
            : "—").ToArray();
    }

    private bool CanReplaceLastDiagnosticValue(string fieldId, FieldMeasurementResult field) =>
        ResolveDiagnosticDisplayValue(fieldId, field).HasValue;

    private decimal? ResolveDiagnosticDisplayValue(string fieldId, FieldMeasurementResult? field)
    {
        if (field is null) return null;
        if (fieldId == BuiltInFieldIds.AdvanceMm && field.OfficialValue.HasValue) return field.OfficialValue;
        // Torkta 1.xxx ilk rakamının 4 olarak okunması saha OCR'ında bilinen
        // yapısal/zamansal şüpheli durumdur. Böyle bir aday gözlem/audit içinde
        // kalır; kart ve geçmişte doğrulanmış son değer gösterilir. RawText'i
        // tekrar parse ederek reddedilmiş 4.xxx değerini UI'a geri sokma.
        if (fieldId == BuiltInFieldIds.StableTorque
            && field.FailureReason is "TemporalSuspicious" or "DecimalStructureIncomplete" or "DigitStructureSuspicious"
            && field.TrustedValue.HasValue)
            return field.TrustedValue;
        if (field.ParsedValue.HasValue) return field.ParsedValue;
        if (field.FullLineResult.HasValue) return field.FullLineResult;
        if (field.CharacterResult.HasValue) return field.CharacterResult;
        if (fieldId == BuiltInFieldIds.StableTorque
            && field.FailureReason is "TemporalSuspicious" or "DecimalStructureIncomplete" or "DigitStructureSuspicious")
            return null;
        var definition = _settings.OcrProfile.FindField(fieldId);
        return definition is not null && NumericFieldParser.TryParse(field.RawValue, definition, out var parsed, out _) ? parsed : null;
    }

    private void ApplyDiagnosticHistoryQuality(MeasurementRecord record, DataGridViewRow row)
    {
        var fields = new[] { (BuiltInFieldIds.StableRpm, "RPM"), (BuiltInFieldIds.StableTorque, "Torque"),
            (BuiltInFieldIds.StableThrustForce, "Thrust"), (BuiltInFieldIds.StableAdvanceSpeed, "Speed"),
            (BuiltInFieldIds.StablePenetration, "Penetration") };
        foreach (var (fieldId, column) in fields)
        {
            if (!record.FieldResults.TryGetValue(fieldId, out var field)) continue;
            var feedbackTooltip = field.FeedbackDecisionLevel is "Verified" or "Authoritative" or "Conflict"
                ? $"Kaynak: {field.DecisionSource}\nFeedback adayı: {field.FeedbackCandidateValue?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "—"}\nBenzerlik: %{field.FeedbackSimilarityPercent:0.0}\nDestek: {field.FeedbackSupportingExamples}\nÇelişki: {field.FeedbackConflictCount}\n{field.FeedbackDecisionReason}"
                : "";
            if (field.FailureReason is "Success" or "")
            {
                if (!string.IsNullOrWhiteSpace(feedbackTooltip)) row.Cells[column].ToolTipText = feedbackTooltip;
                continue;
            }
            row.Cells[column].Style.ForeColor = Color.FromArgb(251, 191, 36);
            row.Cells[column].ToolTipText = !string.IsNullOrWhiteSpace(field.ReliabilityReason)
                ? $"OCR gözlemi: {field.ObservedValue?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "—"}\n"
                    + $"Güvenilir son değer: {field.TrustedValue?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "—"}\n"
                    + field.ReliabilityReason
                : field.FailureReason == "CharacterMismatch"
                ? $"Full-line OCR: {field.FullLineResult?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? field.ParsedValue?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "—"}\n"
                    + $"Karakter segmentasyonu: başarısız\nKarar: Tesseract fallback\nKalite: Kısmi"
                : field.FailureReason == "DigitCountMismatch"
                    ? $"Yeni OCR düşük yapısal güvenli: {field.ParsedValue?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? field.FullLineResult?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "—"}\n"
                        + "Hane sayısı görüntü geometrisiyle uyuşmuyor; karttaki son geçerli değer korunuyor."
                : FriendlyQuality(field);
            if (!string.IsNullOrWhiteSpace(feedbackTooltip)) row.Cells[column].ToolTipText += "\n\n" + feedbackTooltip;
        }
    }

    private void LogDiagnosticVisibility(MeasurementRecord record)
    {
        if (!_settings.EnableStableParameterDiagnostics) return;
        var specifications = new[] { (BuiltInFieldIds.StableRpm, "0.00"), (BuiltInFieldIds.StableTorque, "0.000"),
            (BuiltInFieldIds.StableThrustForce, "0"), (BuiltInFieldIds.StableAdvanceSpeed, "0.0"),
            (BuiltInFieldIds.StablePenetration, "0.0") };
        foreach (var (fieldId, format) in specifications)
        {
            if (!record.FieldResults.TryGetValue(fieldId, out var field)) continue;
            var display = ResolveDiagnosticDisplayValue(fieldId, field);
            var raw = (field.RawValue ?? "").Replace("\"", "\"\"");
            _logService.Information($"Canlı diagnostic görünürlük | Field={fieldId} | CapturedAt={field.CapturedAt:O} | RawOcrText=\"{raw}\" | ParsedNumericValue={field.ParsedValue?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null"} | Confidence={field.OcrConfidencePercent:0.0} | FailureReason={field.FailureReason} | DisplayValue={display?.ToString(format, System.Globalization.CultureInfo.InvariantCulture) ?? "—"} | DisplayedOnCard={_activeDashboardCategory == DashboardCategory.Excavation} | WrittenToHistory=true");
        }
    }

    private static string HistoryOcrReading(FieldMeasurementResult? field, decimal? fallback, string format = "0.##")
    {
        var value = field?.ObservedValue ?? field?.ParsedValue ?? field?.FullLineResult ?? field?.CharacterResult ?? field?.OfficialValue ?? fallback;
        if (!value.HasValue) return "—";
        var text = value.Value.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
        if (field?.FailureReason == "SuspiciousPending") return text + " ⚠";
        return text;
    }

    private static void ApplyOfficialHistoryQuality(MeasurementRecord record, DataGridViewRow row)
    {
        var fields = new[]
        {
            (BuiltInFieldIds.Nominal, "Nominal", record.NominalValidationStatus),
            (BuiltInFieldIds.Scale1, "Scale1", record.Scale1ValidationStatus),
            (BuiltInFieldIds.Scale2, "Scale2", record.Scale2ValidationStatus)
        };
        foreach (var (fieldId, column, validationStatus) in fields)
        {
            if (!record.FieldResults.TryGetValue(fieldId, out var field)) continue;
            var read = field.ObservedValue ?? field.ParsedValue ?? field.FullLineResult ?? field.CharacterResult;
            if (field.FailureReason != "SuspiciousPending") continue;
            row.Cells[column].Style.ForeColor = Color.FromArgb(251, 191, 36);
            row.Cells[column].ToolTipText = $"OCR okuması: {read?.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) ?? "—"}\n"
                + $"Son kabul edilen: {(field.AcceptedValue ?? field.OfficialValue)?.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) ?? "—"}\n"
                + (string.IsNullOrWhiteSpace(validationStatus) ? "Şüpheli değer doğrulama bekliyor." : validationStatus);
        }
    }

    private static string FriendlyQuality(FieldMeasurementResult field) => field.FailureReason switch
    { "SuspiciousPending" => "Aday doğrulama bekliyor", "CharacterMismatch" => "Karakter doğrulaması başarısız",
        "DigitCountMismatch" => "Hane doğrulaması başarısız", "DecimalStructureIncomplete" => "Ondalık yapı eksik",
        "DigitStructureSuspicious" => "Hane yapısı şüpheli", "TemporalSuspicious" => "Ani değişim doğrulama bekliyor",
        "NoSegments" => "Segment bulunamadı", "LowConfidence" => "Düşük güven", _ => "Tanılama uyarısı" };

    private static bool IsStableDiagnosticField(string fieldId) => fieldId is BuiltInFieldIds.StableRpm or BuiltInFieldIds.StableTorque
        or BuiltInFieldIds.StableThrustForce or BuiltInFieldIds.StableAdvanceSpeed or BuiltInFieldIds.StablePenetration;

    private static bool HasStableDiagnosticWarning(MeasurementRecord record) => record.FieldResults
        .Any(pair => IsStableDiagnosticField(pair.Key) && pair.Value.FailureReason == "CharacterMismatch");

    private static string OcrHistorySymbol(MeasurementRecord record) => record.HasHeldSuspiciousValue ? "!"
        : HasStableDiagnosticWarning(record) ? "⚠" : record.OcrConfidence switch
    { OcrConfidenceStatus.Reliable => "✓", OcrConfidenceStatus.Partial => "⚠", _ => "—" };

    private void DisplayMonitoringStatus(MonitoringStatus status, string detail)
    {
        _lastMonitoringStatus = status; _lastMonitoringDetail = detail;
        UpdateMonitoringControls();
    }

    private void UpdateMonitoringControls()
    {
        var status = _lastMonitoringStatus;
        var detail = _lastMonitoringDetail;
        var (text, background, foreground) = status switch
        {
            MonitoringStatus.Active => ("●  İZLEME AKTİF", Color.FromArgb(6, 78, 59), Color.FromArgb(110, 231, 183)),
            MonitoringStatus.Processing => ("●  VERİ İŞLENİYOR", Color.FromArgb(30, 64, 175), Color.FromArgb(191, 219, 254)),
            MonitoringStatus.OcrError => ("●  OCR HATASI", Color.FromArgb(127, 29, 29), Color.FromArgb(254, 202, 202)),
            MonitoringStatus.Ready => ("●  HAZIR", Color.FromArgb(51, 65, 85), Color.FromArgb(203, 213, 225)),
            MonitoringStatus.Paused => ("●  DURAKLATILDI", Color.FromArgb(120, 53, 15), Color.FromArgb(253, 230, 138)),
            MonitoringStatus.Resuming => ("●  YENİDEN DOĞRULANIYOR...", Color.FromArgb(30, 64, 175), Color.FromArgb(191, 219, 254)),
            _ => ("●  DURDURULDU", Color.FromArgb(51, 65, 85), Color.FromArgb(203, 213, 225))
        };
        MonitoringStatusLabel.Text = text;
        MonitoringStatusLabel.BackColor = background;
        MonitoringStatusLabel.ForeColor = foreground;
        MonitoringStatusLabel.AccessibleDescription = detail;
        var state = _monitoringService.State;
        var controls = MonitoringControlsFor(state);
        StartButton.Visible = controls.StartVisible; StartButton.Enabled = controls.StartEnabled;
        StartButton.Text = "İzlemeye Başla"; StartButton.BackColor = Color.FromArgb(2, 132, 199);
        StopButton.Visible = controls.StopVisible; StopButton.Enabled = controls.StopEnabled;
        PauseButton.Visible = controls.PauseVisible; PauseButton.Text = controls.PauseText; PauseButton.Enabled = controls.PauseEnabled;
        SettingsButton.Enabled = state is MonitoringRunState.Stopped or MonitoringRunState.Paused;
        SetSessionStatus(status switch
        {
            MonitoringStatus.Active => "İZLENİYOR",
            MonitoringStatus.Processing => "İZLENİYOR • VERİ İŞLENİYOR...",
            MonitoringStatus.OcrError => "HATA",
            MonitoringStatus.Ready => "HAZIR",
            MonitoringStatus.Paused => "DURAKLATILDI",
            MonitoringStatus.Resuming => "YENİDEN DOĞRULANIYOR...",
            _ => "DURDURULDU"
        }, foreground);
    }

    private void RecoverInterruptedSession()
    {
        var previous = _sessionStore.Load();
        if (previous is not { IsActive: true }) return;
        var elapsed = DateTime.Now - previous.StartedAt;
        _logService.Warning($"Yarım kalan izleme oturumu algılandı. Makine={previous.Machine}; Başlangıç={previous.StartedAt:yyyy-MM-dd HH:mm:ss}; Son kayıtlı okuma={previous.ProcessedReadings}; Geçen süre={FormatDuration(elapsed)}.");
        previous.IsActive = false; previous.EndedAt = DateTime.Now; _sessionStore.Save(previous);
    }

    private void CompleteMonitoringSession(string reason)
    {
        if (_session is null || !_session.IsActive) return;
        _session.IsActive = false; _session.EndedAt = DateTime.Now;
        _displayedSessionElapsed = _session.EndedAt.Value - _session.StartedAt;
        _sessionStore.Save(_session);
        _logService.Information($"İzleme oturumu sona erdi. Makine={_session.Machine}; Başlangıç={_session.StartedAt:yyyy-MM-dd HH:mm:ss}; Bitiş={_session.EndedAt:yyyy-MM-dd HH:mm:ss}; Toplam izleme süresi={FormatDuration(_displayedSessionElapsed)}; İşlenen okuma={_session.ProcessedReadings}; Neden={reason}.");
        SetSessionStatus("DURDURULDU", Color.FromArgb(203, 213, 225));
        UpdateSessionDisplay();
    }

    private void UpdateSessionElapsed()
    {
        if (_session is { IsActive: true }) _displayedSessionElapsed = DateTime.Now - _session.StartedAt;
        var timing = _monitoringService.GetTimingSnapshot();
        SessionElapsedLabel.Text = _session is { IsActive: true }
            ? $"Oturum {FormatDuration(timing.SessionElapsed)}\nAktif {FormatDuration(timing.ActiveMonitoring)}\nDurak. {FormatDuration(timing.PausedDuration)}"
            : FormatDuration(_displayedSessionElapsed);
        SessionElapsedLabel.AccessibleDescription = $"Oturum süresi: {FormatDuration(timing.SessionElapsed)}; Aktif izleme: {FormatDuration(timing.ActiveMonitoring)}; Duraklatılan süre: {FormatDuration(timing.PausedDuration)}";
    }

    internal static MonitoringControlsPresentation MonitoringControlsFor(MonitoringRunState state)
    {
        var stopped = state == MonitoringRunState.Stopped;
        return new(stopped, stopped, !stopped, !stopped, !stopped,
            state == MonitoringRunState.Paused ? "Devam Et" : "Duraklat",
            state is MonitoringRunState.Running or MonitoringRunState.Paused);
    }

    internal sealed record MonitoringControlsPresentation(bool StartVisible, bool StartEnabled,
        bool StopVisible, bool StopEnabled, bool PauseVisible, string PauseText, bool PauseEnabled);

    private void UpdateSessionDisplay()
    {
        if (_session is null)
        {
            SetSessionStatus("HAZIR", Color.FromArgb(203, 213, 225));
            SessionStartedLabel.Text = "—"; SessionElapsedLabel.Text = "00:00:00"; SessionCountLabel.Text = "0";
            SessionLastDataLabel.Text = "—"; SessionOcrLabel.Text = "Henüz okuma yok"; return;
        }
        SessionStartedLabel.Text = _session.StartedAt.ToString("dd.MM.yyyy HH:mm:ss");
        SessionCountLabel.Text = _session.ProcessedReadings.ToString("N0");
        SessionLastDataLabel.Text = _session.LastSuccessfulDataAt?.ToString("dd.MM.yyyy HH:mm:ss") ?? "—";
        SessionOcrLabel.Text = _session.LastOcrStatus;
        UpdateSessionElapsed();
    }

    private void SetSessionStatus(string text, Color color)
    {
        SessionStateLabel.Text = text; SessionStateLabel.ForeColor = color;
    }

    private static string FormatDuration(TimeSpan duration) => $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";

    private void CreatePerformanceMonitor()
    {
        _performanceMonitor?.Dispose();
        _performanceMonitor = new PerformanceMonitoringService(_settings);
        _performanceMonitor.Warning += message => _logService.Warning(message);
        _performanceMonitor.Updated += snapshot => RunOnUiThread(() =>
        {
            if (_performanceForm is { IsDisposed: false, Visible: true }) _performanceForm.UpdateSnapshot(snapshot);
        });
    }

    private void ShowPerformancePanel()
    {
        if (_performanceForm is null || _performanceForm.IsDisposed)
        {
            _performanceForm = new PerformanceHealthForm();
            _performanceForm.FormClosed += (_, _) => _performanceForm = null;
        }
        if (!_performanceForm.Visible) _performanceForm.Show(this);
        _performanceForm.BringToFront();
        if (_performanceMonitor?.Latest is { } snapshot) _performanceForm.UpdateSnapshot(snapshot);
    }

    private void LogPerformanceSummary(PerformanceSnapshot? snapshot)
    {
        if (snapshot is null) return;
        var total = snapshot.TotalCacheHits + snapshot.TotalCacheMisses;
        var rate = total == 0 ? 0 : snapshot.TotalCacheHits * 100d / total;
        _logService.Information($"Performans özeti: Oturum={snapshot.SessionDuration}; Gerçek OCR={snapshot.TotalRealOcr}; " +
            $"Cache hit=%{rate:0.0}; Ortalama gecikme={snapshot.AverageLatencyMs:0} ms; Maksimum gecikme={snapshot.MaximumLatencyMs:0} ms; " +
            $"Ortalama CPU={snapshot.AverageProcessCpuPercent:0.0}%; Maksimum CPU={snapshot.MaximumProcessCpuPercent:0.0}%; Uyuşmazlık={snapshot.MismatchSaved}.");
    }

    private void RunOnUiThread(Action action)
    {
        if (IsDisposed || Disposing) return;
        if (InvokeRequired)
        {
            BeginInvoke(action);
            return;
        }

        action();
    }

    private static string FormatValue(decimal? value) => value?.ToString("0.##") ?? "Okunamadı";

    private static string ConfidenceText(OcrConfidenceStatus status) => status switch
    {
        OcrConfidenceStatus.Reliable => "Güvenilir",
        OcrConfidenceStatus.Partial => "Kısmi",
        _ => "Okunamadı"
    };
}
