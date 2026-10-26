using System.Text.Json;
using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public sealed class SettingsService
{
    private List<NumericFieldDefinition> _loadedCustomFields = [];
    private List<OperationModeKeywordDefinition> _loadedOperationKeywords = [];
    private int _loadedOperationConfirmationReadings = 2;
    private int _loadedOperationMaximumUnknownReadings = 3;
    private double _loadedOperationMinimumSimilarity = 75;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public SettingsService(string? settingsDirectory = null)
    {
        SettingsDirectory = settingsDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TBMWatchdog");
        SettingsPath = Path.Combine(SettingsDirectory, "settings.json");
    }

    public string SettingsDirectory { get; }
    public string SettingsPath { get; }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return CreateDefaults();
            }

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? CreateDefaults();
            Normalize(settings);
            if (OcrProfileMigrator.Migrate(settings))
            {
                var backup = SettingsPath + $".pre-schema-{OcrProfileMigrator.CurrentSchemaVersion}.{DateTime.Now:yyyyMMdd_HHmmss_fff}.bak";
                File.Copy(SettingsPath, backup, overwrite: false);
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
            }
            _loadedCustomFields = CloneFields(settings.OcrProfile.Fields.Where(field => !IsBuiltIn(field.FieldId)));
            _loadedOperationKeywords = CloneKeywords(settings.OcrProfile.OperationModeKeywords);
            _loadedOperationConfirmationReadings = settings.OcrProfile.OperationModeConfirmationReadings;
            _loadedOperationMaximumUnknownReadings = settings.OcrProfile.OperationModeMaximumUnknownReadings;
            _loadedOperationMinimumSimilarity = settings.OcrProfile.OperationModeMinimumTextSimilarityPercent;
            return settings;
        }
        catch (JsonException exception)
        {
            throw new SettingsException("Ayar dosyasının biçimi geçersiz.", exception);
        }
        catch (IOException exception)
        {
            throw new SettingsException("Ayar dosyası okunamadı.", exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new SettingsException("Ayar dosyasına erişim izni bulunamadı.", exception);
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.OcrProfile ??= new OcrProfile();
        foreach (var custom in _loadedCustomFields)
            if (settings.OcrProfile.Fields.All(field => !string.Equals(field.FieldId, custom.FieldId, StringComparison.OrdinalIgnoreCase)))
                settings.OcrProfile.Fields.Add(CloneField(custom));
        if (_loadedOperationKeywords.Count > 0)
        {
            settings.OcrProfile.OperationModeKeywords = CloneKeywords(_loadedOperationKeywords);
            settings.OcrProfile.OperationModeConfirmationReadings = _loadedOperationConfirmationReadings;
            settings.OcrProfile.OperationModeMaximumUnknownReadings = _loadedOperationMaximumUnknownReadings;
            settings.OcrProfile.OperationModeMinimumTextSimilarityPercent = _loadedOperationMinimumSimilarity;
        }
        OcrProfileMigrator.Migrate(settings);
        Validate(settings);

        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            var temporaryPath = SettingsPath + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
            File.Move(temporaryPath, SettingsPath, true);
            _loadedCustomFields = CloneFields(settings.OcrProfile.Fields.Where(field => !IsBuiltIn(field.FieldId)));
        }
        catch (IOException exception)
        {
            throw new SettingsException("Ayarlar diske yazılamadı.", exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new SettingsException("Ayarları kaydetmek için erişim izni bulunamadı.", exception);
        }
    }

    public static AppSettings CreateDefaults() => new()
    {
        CsvOutputFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "TBMWatchdog",
            "exports")
    };

    public static void Validate(AppSettings settings)
    {
        if (settings.ScreenNumber < 0)
        {
            throw new SettingsException("Ekran numarası negatif olamaz.");
        }

        if (settings.ReadingIntervalSeconds is < 1 or > 3600)
        {
            throw new SettingsException("Okuma sıklığı 1–3600 saniye arasında olmalıdır.");
        }
        if (settings.OcrMaxParallelWorkers is < 1 or > 16 || settings.OcrFieldTimeoutMs is < 1000 or > 60000)
            throw new SettingsException("Paralel OCR işçi/timeout ayarları geçersiz.");

        if (settings.InactivityWarningSeconds is < 1 or > 86400)
        {
            throw new SettingsException("Hareketsizlik süresi 1–86400 saniye arasında olmalıdır.");
        }
        if (settings.MismatchSampleCooldownSeconds is < 0 or > 86400
            || settings.MismatchSampleMaximumFilesPerField < 1
            || settings.MismatchSampleMaximumTotalSizeMb < 1)
            throw new SettingsException("Uyuşmazlık örneği saklama ayarları geçersiz.");
        if (settings.OcrImageSimilarityThresholdPercent is < 0 or > 100
            || settings.GateOcrCacheMaximumAgeSeconds is < 1 or > 3600
            || settings.NumericFieldOcrCacheMaximumAgeSeconds is < 1 or > 3600)
            throw new SettingsException("OCR görüntü önbelleği ayarları geçersiz.");

        if (settings.PerformanceMonitorIntervalSeconds is < 1 or > 10
            || settings.PerformanceCsvIntervalSeconds is < 1 or > 3600
            || settings.WarningProcessCpuPercent is < 0 or > 100
            || settings.CriticalProcessCpuPercent < settings.WarningProcessCpuPercent
            || settings.CriticalProcessCpuPercent > 100
            || settings.WarningAverageLatencyMs < 0
            || settings.CriticalAverageLatencyMs < settings.WarningAverageLatencyMs)
            throw new SettingsException("Performans izleme ayarları geçersiz.");

        if (settings.OcrPaddingPixels is < 0 or > 50)
        {
            throw new SettingsException("OCR kenar boşluğu 0–50 piksel arasında olmalıdır.");
        }

        var profile = settings.OcrProfile;
        if (new[] { profile.PaddingLeft, profile.PaddingRight, profile.PaddingTop, profile.PaddingBottom }.Any(value => value is < 0 or > 100))
            throw new SettingsException("OCR profil padding değerleri 0–100 piksel arasında olmalıdır.");
        if (new[] { profile.GatePaddingLeft, profile.GatePaddingRight, profile.GatePaddingTop, profile.GatePaddingBottom }.Any(value => value is < 0 or > 50))
            throw new SettingsException("Kapak OCR padding değerleri 0–50 piksel arasında olmalıdır.");
        if (profile.GateMaximumValue <= 0 || profile.GateMaximumValue > 1_000_000m)
            throw new SettingsException("Kapak maksimum değeri 0–1.000.000 arasında olmalıdır.");
        if (profile.OperationModeConfirmationReadings is < 2 or > 20
            || profile.OperationModeMaximumUnknownReadings is < 1 or > 100
            || profile.OperationModeMinimumTextSimilarityPercent is < 50 or > 100
            || profile.OperationModeKeywords.Count == 0
            || profile.OperationModeKeywords.Any(group => group.Keywords.Count == 0))
            throw new SettingsException("Çalışma modu doğrulama veya anahtar kelime ayarları geçersiz.");
        if (profile.GateChangeTolerance < 0 || profile.GateConfirmationReadings is < 2 or > 10)
            throw new SettingsException("Kapak kararlılık filtresi ayarları geçersiz.");
        if (profile.GateMaximumChangePerReading < 0 || profile.GateMaximumChangePerReading > profile.GateMaximumValue)
            throw new SettingsException("Kapak okuma başına maksimum değişim değeri geçersiz.");
        if (profile.GateTemplateMinimumScore is < 0 or > 1)
            throw new SettingsException("Kapak şablon eşleşme eşiği 0–1 arasında olmalıdır.");
        if (profile.GateTemplateAuthoritativeScore < profile.GateTemplateMinimumScore || profile.GateTemplateAuthoritativeScore > 1)
            throw new SettingsException("Kapak otoriter şablon eşiği minimum şablon eşiği ile 1 arasında olmalıdır.");
        if (profile.GateTemplateMinimumMargin is < 0 or > 1)
            throw new SettingsException("Kapak şablon fark eşiği 0–1 arasında olmalıdır.");
        if (profile.GateMaximumAllowedInkLoss is < 0 or > 1)
            throw new SettingsException("Kapak maksimum mürekkep kaybı 0–1 arasında olmalıdır.");
        if (profile.GateMinimumRecommendedTemplatesPerDigit is < 1 or > 100)
            throw new SettingsException("Rakam başına önerilen şablon sayısı 1–100 arasında olmalıdır.");
        ValidateTemplateThresholds(profile.Scale1TemplateMinimumScore, profile.Scale1TemplateAuthoritativeScore,
            profile.Scale1TemplateMinimumMargin, "Kantar 1");
        ValidateTemplateThresholds(profile.Scale2TemplateMinimumScore, profile.Scale2TemplateAuthoritativeScore,
            profile.Scale2TemplateMinimumMargin, "Kantar 2");
        if (profile.ScaleMaximumAllowedInkLoss is < 0 or > 1)
            throw new SettingsException("Kantar maksimum mürekkep kaybı 0–1 arasında olmalıdır.");
        if (profile.ScaleMinimumRecommendedTemplatesPerDigit is < 1 or > 100)
            throw new SettingsException("Kantar için önerilen şablon sayısı 1–100 arasında olmalıdır.");
        if (new[] { profile.NominalMaximumChangePerReading, profile.Scale1MaximumChangePerReading,
                profile.Scale2MaximumChangePerReading, profile.SuspiciousValueConfirmationTolerance }
            .Any(value => value < 0))
            throw new SettingsException("Şüpheli değer filtresi sınırları negatif olamaz.");
        if (profile.SuspiciousValueConfirmationReadings is < 2 or > 10)
            throw new SettingsException("Şüpheli değer doğrulama sayısı 2–10 arasında olmalıdır.");
        if (profile.AdvanceMaximumChangePerReading is < 0 or > 2500
            || profile.AdvanceConfirmationReadings is < 2 or > 10
            || profile.AdvanceConfirmationTolerance is < 0 or > 2500
            || profile.StableZoneMinimumAdvanceMm < 0
            || profile.StableZoneMaximumAdvanceMm > 2500
            || profile.StableZoneMaximumAdvanceMm < profile.StableZoneMinimumAdvanceMm
            || profile.StableZoneConfirmationReadings is < 2 or > 10)
            throw new SettingsException("İlerleme mesafesi veya stabil bölge ayarları geçersiz.");
        if (profile.AdvanceEnabledGroups.Count == 0 || profile.AdvanceEnabledGroups.Any(group => group is not ("A" or "B" or "C" or "D"))
            || profile.AdvancePrimaryGroup is not ("A" or "B" or "C" or "D")
            || profile.AdvanceMinimumValidValue is < 0 or > 2500
            || profile.AdvanceMaximumAllowedGroupDifferenceMm is < 0 or > 2500
            || profile.AdvanceMinimumRequiredValidGroups is < 1 or > 4
            || profile.AdvanceGroupAgreementToleranceMm is < 0 or > 2500
            || profile.MinimumConsistentAdvanceGroups is < 2 or > 4
            || profile.AdvanceSingleGroupConfirmationReadings is < 2 or > 10
            || profile.AdvanceSensorZeroWarningReadings is < 1 or > 100)
            throw new SettingsException("İlerleme grup karar ayarları geçersiz.");
        foreach (var range in new[] { profile.NominalLength, profile.ScaleLength, profile.GateLength })
            if (range.Minimum < 1 || range.Maximum < range.Minimum || range.Maximum > 12)
                throw new SettingsException("OCR karakter uzunlukları 1–12 aralığında ve minimum maksimumdan büyük olmayacak biçimde olmalıdır.");
        if (profile.AdaptiveThresholdRadius is < 1 or > 100 || profile.AdaptiveThresholdBias is < -100 or > 100)
            throw new SettingsException("OCR adaptive threshold profil değerleri geçersiz.");

        if (string.IsNullOrWhiteSpace(settings.CsvOutputFolder))
        {
            throw new SettingsException("CSV kayıt klasörü boş bırakılamaz.");
        }

        ValidateRegion(settings.NominalRegion, "Nominal");
        ValidateRegion(settings.Scale1Region, "Kantar 1");
        ValidateRegion(settings.Scale2Region, "Kantar 2");
        ValidateRegion(settings.GateRegion, "Kapak");
        ValidateRegion(settings.OperationModeRegion, "Çalışma Modu");
        // 1031 uses only the four advance-group regions. The legacy property is
        // retained solely so older profile JSON can be migrated without data loss.
        if (!string.Equals(settings.OcrProfile.Name, "1031", StringComparison.OrdinalIgnoreCase))
            ValidateRegion(settings.AdvanceMmRegion, "İlerleme Mesafesi");
        ValidateRegion(settings.AdvanceGroupARegion, "İlerleme Grup A"); ValidateRegion(settings.AdvanceGroupBRegion, "İlerleme Grup B");
        ValidateRegion(settings.AdvanceGroupCRegion, "İlerleme Grup C"); ValidateRegion(settings.AdvanceGroupDRegion, "İlerleme Grup D");
        ValidateRegion(settings.StableRpmRegion, "RPM"); ValidateRegion(settings.StableTorqueRegion, "Tork");
        ValidateRegion(settings.StableThrustForceRegion, "İtme Kuvveti"); ValidateRegion(settings.StableAdvanceSpeedRegion, "İlerleme Hızı");
        ValidateRegion(settings.StablePenetrationRegion, "Penetrasyon");
        ValidateRegion(settings.NominalInjectionRegion, "Nominal Enjeksiyon");
        ValidateRegion(settings.RealInjectionRegion, "Gerçek Enjeksiyon");
        ValidateRegion(settings.Epb1Region, "EPB 1"); ValidateRegion(settings.Epb2Region, "EPB 2");
        ValidateRegion(settings.Epb3Region, "EPB 3"); ValidateRegion(settings.Epb4Region, "EPB 4"); ValidateRegion(settings.Epb5Region, "EPB 5");
    }

    private static void ValidateRegion(ScreenRegion region, string name)
    {
        if (region.X < 0 || region.Y < 0 || region.Width < 0 || region.Height < 0)
        {
            throw new SettingsException($"{name} ekran bölgesi negatif değer içeremez.");
        }

        var isEmpty = region.Width == 0 && region.Height == 0;
        if (!isEmpty && (region.Width == 0 || region.Height == 0))
        {
            throw new SettingsException($"{name} bölgesinin genişlik ve yüksekliği birlikte girilmelidir.");
        }
    }

    private static void ValidateTemplateThresholds(double minimum, double authoritative, double margin, string field)
    {
        if (minimum is < 0 or > 1 || authoritative < minimum || authoritative > 1 || margin is < 0 or > 1)
            throw new SettingsException($"{field} şablon eşikleri geçersizdir.");
    }

    private static void Normalize(AppSettings settings)
    {
        if (settings.OcrFieldTimeoutMs == 8000) settings.OcrFieldTimeoutMs = 20000;
        var legacyMismatchDefaults = settings.MismatchSampleMaximumFilesPerField == 500
            && settings.MismatchSampleMaximumTotalSizeMb == 2000;
        if (legacyMismatchDefaults) settings.EnableMismatchSampleCapture = false;
        settings.MismatchSampleMaximumFilesPerField = Math.Clamp(settings.MismatchSampleMaximumFilesPerField, 1, 50);
        settings.MismatchSampleMaximumTotalSizeMb = Math.Clamp(settings.MismatchSampleMaximumTotalSizeMb, 1, 100);
        settings.OcrMaxParallelWorkers = Math.Clamp(settings.OcrMaxParallelWorkers, 1, 16);
        settings.OcrFieldTimeoutMs = Math.Clamp(settings.OcrFieldTimeoutMs, 1000, 60000);
        settings.NominalRegion ??= new ScreenRegion();
        settings.Scale1Region ??= new ScreenRegion();
        settings.Scale2Region ??= new ScreenRegion();
        settings.GateRegion ??= new ScreenRegion();
        settings.OperationModeRegion ??= new ScreenRegion();
        settings.AdvanceMmRegion ??= new ScreenRegion();
        settings.AdvanceGroupARegion ??= new ScreenRegion(); settings.AdvanceGroupBRegion ??= new ScreenRegion();
        settings.AdvanceGroupCRegion ??= new ScreenRegion(); settings.AdvanceGroupDRegion ??= new ScreenRegion();
        settings.StableRpmRegion ??= new ScreenRegion(); settings.StableTorqueRegion ??= new ScreenRegion();
        settings.StableThrustForceRegion ??= new ScreenRegion(); settings.StableAdvanceSpeedRegion ??= new ScreenRegion(); settings.StablePenetrationRegion ??= new ScreenRegion();
        settings.NominalInjectionRegion ??= new ScreenRegion(); settings.RealInjectionRegion ??= new ScreenRegion();
        settings.Epb1Region ??= new ScreenRegion(); settings.Epb2Region ??= new ScreenRegion(); settings.Epb3Region ??= new ScreenRegion();
        settings.Epb4Region ??= new ScreenRegion(); settings.Epb5Region ??= new ScreenRegion();
        settings.OcrProfile ??= new OcrProfile();
        settings.OcrProfile.OperationModeKeywords ??= OperationModeKeywordDefinition.CreateDefaults();
        if (string.Equals(settings.OcrProfile.GateDigitTemplateFolder, "DigitTemplates", StringComparison.OrdinalIgnoreCase))
        {
            settings.OcrProfile.GateDigitTemplateFolder = "Templates/GateDigits";
            settings.OcrProfile.EnableGateDigitTemplateMatcher = true;
        }
        if (string.IsNullOrWhiteSpace(settings.CsvOutputFolder))
        {
            settings.CsvOutputFolder = CreateDefaults().CsvOutputFolder;
        }
    }

    private static bool IsBuiltIn(string id) => id is BuiltInFieldIds.Nominal or BuiltInFieldIds.Gate or BuiltInFieldIds.Scale1 or BuiltInFieldIds.Scale2 or BuiltInFieldIds.AdvanceMm
        or BuiltInFieldIds.AdvanceGroupA or BuiltInFieldIds.AdvanceGroupB or BuiltInFieldIds.AdvanceGroupC or BuiltInFieldIds.AdvanceGroupD
        or BuiltInFieldIds.StableRpm or BuiltInFieldIds.StableTorque or BuiltInFieldIds.StableThrustForce
        or BuiltInFieldIds.StableAdvanceSpeed or BuiltInFieldIds.StablePenetration
        or BuiltInFieldIds.NominalInjection or BuiltInFieldIds.RealInjection
        or BuiltInFieldIds.Epb1 or BuiltInFieldIds.Epb2 or BuiltInFieldIds.Epb3 or BuiltInFieldIds.Epb4 or BuiltInFieldIds.Epb5;
        
    private static List<NumericFieldDefinition> CloneFields(IEnumerable<NumericFieldDefinition> fields) => fields.Select(CloneField).ToList();
    private static NumericFieldDefinition CloneField(NumericFieldDefinition field) =>
        JsonSerializer.Deserialize<NumericFieldDefinition>(JsonSerializer.Serialize(field, JsonOptions), JsonOptions)!;
    private static List<OperationModeKeywordDefinition> CloneKeywords(IEnumerable<OperationModeKeywordDefinition> keywords) =>
        JsonSerializer.Deserialize<List<OperationModeKeywordDefinition>>(JsonSerializer.Serialize(keywords, JsonOptions), JsonOptions)!;
}

public sealed class SettingsException : Exception
{
    public SettingsException(string message) : base(message) { }
    public SettingsException(string message, Exception innerException) : base(message, innerException) { }
}
