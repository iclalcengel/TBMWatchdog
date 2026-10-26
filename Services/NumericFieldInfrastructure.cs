using System.Globalization;
using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public static class NumericFieldParser
{
    public static bool TryParse(string? text, NumericFieldDefinition definition, out decimal value, out string normalized)
    {
        value = 0; normalized = ""; if (string.IsNullOrWhiteSpace(text)) return false;
        var candidate = new string(text.Trim().Where(c => char.IsDigit(c) || c is '-' or '+' or '.' or ',').ToArray());
        var lastComma = candidate.LastIndexOf(','); var lastDot = candidate.LastIndexOf('.'); var decimalIndex = Math.Max(lastComma, lastDot);
        if (decimalIndex >= 0) candidate = candidate[..decimalIndex].Replace(".", "").Replace(",", "") + "." + candidate[(decimalIndex + 1)..].Replace(".", "").Replace(",", "");
        if (!decimal.TryParse(candidate, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value)) return false;
        if (definition.ValueType == FieldValueType.Integer && value != decimal.Truncate(value)) return false;
        if (definition.MinimumValue.HasValue && value < definition.MinimumValue || definition.MaximumValue.HasValue && value > definition.MaximumValue) return false;
        value = decimal.Round(value, Math.Clamp(definition.DecimalPlaces, 0, 28), MidpointRounding.AwayFromZero);
        normalized = value.ToString(definition.DecimalPlaces > 0 ? $"0.{new string('0', definition.DecimalPlaces)}" : "0", CultureInfo.InvariantCulture);
        return true;
    }
}

public static class OcrProfileMigrator
{
    public const int CurrentSchemaVersion = 10;
    public static bool Migrate(AppSettings settings)
    {
        var profile = settings.OcrProfile ??= new OcrProfile(); var changed = profile.SchemaVersion < CurrentSchemaVersion || profile.Fields.Count == 0;
        Upsert(profile, Definition(BuiltInFieldIds.Nominal, "Nominal", "t", settings.NominalRegion, profile.NominalLength,
            1_000_000, profile.NominalMaximumChangePerReading, profile.SuspiciousValueConfirmationReadings, profile.SuspiciousValueConfirmationTolerance, true, false, false, "NominalDigits", 0));
        Upsert(profile, Definition(BuiltInFieldIds.Gate, "Kapak mm", "mm", settings.GateRegion, profile.GateLength,
            profile.GateMaximumValue, profile.GateMaximumChangePerReading, profile.GateConfirmationReadings, profile.GateChangeTolerance, false, true, true, "GateDigits", 1));
        Upsert(profile, Definition(BuiltInFieldIds.Scale1, "Kantar 1", "t", settings.Scale1Region, profile.ScaleLength,
            1_000_000, profile.Scale1MaximumChangePerReading, profile.SuspiciousValueConfirmationReadings, profile.SuspiciousValueConfirmationTolerance, true, true, false, "Scale1Digits", 2));
        Upsert(profile, Definition(BuiltInFieldIds.Scale2, "Kantar 2", "t", settings.Scale2Region, profile.ScaleLength,
            1_000_000, profile.Scale2MaximumChangePerReading, profile.SuspiciousValueConfirmationReadings, profile.SuspiciousValueConfirmationTolerance, true, true, false, "Scale2Digits", 3));
        if (profile.Name.Contains("1031", StringComparison.OrdinalIgnoreCase)) Upsert(profile, new NumericFieldDefinition
        {
            FieldId = "OperationMode", DisplayName = "Çalışma Modu", Category = MeasurementCategory.OcrMeasurement,
            Unit = "", ValueType = FieldValueType.Text, RegionCoordinates = Clone(settings.OperationModeRegion),
            IsEnabled = settings.OperationModeRegion.IsValid, SupportsCharacterOcr = false, SupportsTemplateTraining = false,
            UsesOfficialCharacterDecision = false, UsesSuspiciousValueFilter = false, DisplayOrder = 4, ReportGroup = "Operation"
        });
        var is1031 = profile.Name.Contains("1031", StringComparison.OrdinalIgnoreCase);
        if (is1031 && !settings.AdvanceGroupARegion.IsValid && settings.AdvanceMmRegion.IsValid)
        {
            settings.AdvanceGroupARegion = Clone(settings.AdvanceMmRegion);
            changed = true;
        }
        profile.Fields.RemoveAll(field => string.Equals(field.FieldId, BuiltInFieldIds.AdvanceMm, StringComparison.OrdinalIgnoreCase));
        if (!is1031) Upsert(profile, new NumericFieldDefinition
        {
            FieldId = BuiltInFieldIds.AdvanceMm, DisplayName = "İlerleme Mesafesi", Category = MeasurementCategory.OcrMeasurement,
            Unit = "mm", ValueType = FieldValueType.Integer, MinimumValue = 0, MaximumValue = 2500, DecimalPlaces = 0,
            MinimumExpectedDigits = 1, MaximumExpectedDigits = 4, RegionCoordinates = Clone(settings.AdvanceMmRegion),
            IsEnabled = false,
            SupportsCharacterOcr = true, SupportsTemplateTraining = false, UsesOfficialCharacterDecision = false,
            UsesSuspiciousValueFilter = true, MaximumChangePerReading = profile.AdvanceMaximumChangePerReading,
            ConfirmationReadings = profile.AdvanceConfirmationReadings, ConfirmationTolerance = profile.AdvanceConfirmationTolerance,
            TemplateFolderName = "AdvanceMmDigits", DisplayOrder = 5, ReportGroup = "Excavation"
        });
        AddAdvanceGroup(BuiltInFieldIds.AdvanceGroupA, "İlerleme Grup A", settings.AdvanceGroupARegion, 6);
        AddAdvanceGroup(BuiltInFieldIds.AdvanceGroupB, "İlerleme Grup B", settings.AdvanceGroupBRegion, 7);
        AddAdvanceGroup(BuiltInFieldIds.AdvanceGroupC, "İlerleme Grup C", settings.AdvanceGroupCRegion, 8);
        AddAdvanceGroup(BuiltInFieldIds.AdvanceGroupD, "İlerleme Grup D", settings.AdvanceGroupDRegion, 9);
        AddStableParameter(BuiltInFieldIds.StableRpm, "RPM", "rpm", settings.StableRpmRegion, 10, 2, 10);
        AddStableParameter(BuiltInFieldIds.StableTorque, "Tork", "MNm", settings.StableTorqueRegion, 11, 3, 20);
        AddStableParameter(BuiltInFieldIds.StableThrustForce, "İtme Kuvveti", "kN", settings.StableThrustForceRegion, 12, 0, 100000);
        AddStableParameter(BuiltInFieldIds.StableAdvanceSpeed, "İlerleme Hızı", "mm/min", settings.StableAdvanceSpeedRegion, 13, 1, 200);
        AddStableParameter(BuiltInFieldIds.StablePenetration, "Penetrasyon", "mm/rot", settings.StablePenetrationRegion, 14, 1, 100);
        AddInjection(BuiltInFieldIds.NominalInjection, "Nominal Enjeksiyon", settings.NominalInjectionRegion, 15);
        AddInjection(BuiltInFieldIds.RealInjection, "Gerçek Enjeksiyon", settings.RealInjectionRegion, 16);
        var epbRegions = new[] { settings.Epb1Region, settings.Epb2Region, settings.Epb3Region, settings.Epb4Region, settings.Epb5Region };
        var epbIds = new[] { BuiltInFieldIds.Epb1, BuiltInFieldIds.Epb2, BuiltInFieldIds.Epb3, BuiltInFieldIds.Epb4, BuiltInFieldIds.Epb5 };
        for (var index = 0; index < epbIds.Length; index++) AddEpb(epbIds[index], $"EPB {index + 1}", epbRegions[index], 17 + index);
        profile.SchemaVersion = CurrentSchemaVersion; return changed;

        void AddAdvanceGroup(string id, string name, ScreenRegion region, int order)
        {
            profile.Fields.RemoveAll(field => string.Equals(field.FieldId, id, StringComparison.OrdinalIgnoreCase));
            Upsert(profile, new NumericFieldDefinition { FieldId = id, DisplayName = name, Category = MeasurementCategory.OcrMeasurement,
                Unit = "mm", ValueType = FieldValueType.Integer, MinimumValue = 0, MaximumValue = 2500, DecimalPlaces = 0,
                MinimumExpectedDigits = 1, MaximumExpectedDigits = 4, RegionCoordinates = Clone(region),
                IsEnabled = profile.Name.Contains("1031", StringComparison.OrdinalIgnoreCase) && region.IsValid,
                SupportsCharacterOcr = true, SupportsTemplateTraining = false, UsesOfficialCharacterDecision = false,
                UsesSuspiciousValueFilter = false, MaximumChangePerReading = profile.AdvanceMaximumChangePerReading,
                ConfirmationReadings = profile.AdvanceConfirmationReadings, ConfirmationTolerance = profile.AdvanceConfirmationTolerance,
                TemplateFolderName = id + "Digits", DisplayOrder = order, ReportGroup = "Excavation" });
        }
        void AddStableParameter(string id, string name, string unit, ScreenRegion region, int order, int decimals, decimal maximum)
        {
            profile.Fields.RemoveAll(field => string.Equals(field.FieldId, id, StringComparison.OrdinalIgnoreCase));
            var calibratedFeedback = id is BuiltInFieldIds.StableTorque or BuiltInFieldIds.StablePenetration;
            var verifiedThreshold = id == BuiltInFieldIds.StableTorque ? .96 : id == BuiltInFieldIds.StablePenetration ? .95 : (double?)null;
            Upsert(profile, new NumericFieldDefinition { FieldId = id, DisplayName = name, Category = MeasurementCategory.OcrMeasurement,
                Unit = unit, ValueType = decimals == 0 ? FieldValueType.Integer : FieldValueType.Decimal, MinimumValue = 0, MaximumValue = maximum,
                DecimalPlaces = decimals, AllowDecimalSeparator = decimals > 0,
                MinimumExpectedDigits = 1, MaximumExpectedDigits = 8, RegionCoordinates = Clone(region),
                IsEnabled = profile.Name.Contains("1031", StringComparison.OrdinalIgnoreCase) && region.IsValid,
                SupportsCharacterOcr = true, SupportsTemplateTraining = true, UsesOfficialCharacterDecision = true,
                UsesSuspiciousValueFilter = false, TemplateFolderName = "SharedNumericDigits",
                TemplateScope = TemplateScope.SharedNumericDigit, DisplayOrder = order, ReportGroup = "StableDiagnostics",
                ExpectedDigitCountMin = id == BuiltInFieldIds.StableThrustForce ? 4 : 1,
                ExpectedDigitCountMax = id == BuiltInFieldIds.StableThrustForce ? 5 : 8,
                ProjectionSplitEnabled = id == BuiltInFieldIds.StableThrustForce,
                MergedSegmentWidthRatio = id == BuiltInFieldIds.StableThrustForce ? 1.10 : 1.45,
                ProjectionValleyThreshold = id == BuiltInFieldIds.StableThrustForce ? 0.38 : 0.35,
                FeedbackLiveOverrideEnabled = calibratedFeedback,
                FeedbackCandidateSimilarityOverride = calibratedFeedback ? .90 : null,
                FeedbackVerifiedSimilarityOverride = verifiedThreshold,
                FeedbackAuthoritativeSimilarityOverride = calibratedFeedback ? .97 : null });
        }
        void AddInjection(string id, string name, ScreenRegion region, int order)
        {
            profile.Fields.RemoveAll(field => string.Equals(field.FieldId, id, StringComparison.OrdinalIgnoreCase));
            Upsert(profile, new NumericFieldDefinition { FieldId = id, DisplayName = name,
                Category = MeasurementCategory.OcrMeasurement, Unit = "m³", ValueType = FieldValueType.Decimal,
                MinimumValue = 0, MaximumValue = 20, DecimalPlaces = 1, AllowDecimalSeparator = true,
                MinimumExpectedDigits = 2, MaximumExpectedDigits = 3, RegionCoordinates = Clone(region),
                IsEnabled = profile.Name.Contains("1031", StringComparison.OrdinalIgnoreCase) && region.IsValid,
                SupportsCharacterOcr = true, SupportsTemplateTraining = true, UsesOfficialCharacterDecision = false,
                UsesSuspiciousValueFilter = false, TemplateFolderName = "SharedNumericDigits",
                TemplateScope = TemplateScope.SharedNumericDigit, DisplayOrder = order,
                ReportGroup = "InjectionDiagnostics", ExpectedDigitCountMin = 2, ExpectedDigitCountMax = 3 });
        }
        void AddEpb(string id, string name, ScreenRegion region, int order)
        {
            profile.Fields.RemoveAll(field => string.Equals(field.FieldId, id, StringComparison.OrdinalIgnoreCase));
            Upsert(profile, new NumericFieldDefinition { FieldId = id, DisplayName = name,
                Category = MeasurementCategory.OcrMeasurement, Unit = "bar", ValueType = FieldValueType.Decimal,
                MinimumValue = 0, MaximumValue = 10, DecimalPlaces = 2, AllowDecimalSeparator = true,
                MinimumExpectedDigits = 3, MaximumExpectedDigits = 4, RegionCoordinates = Clone(region),
                IsEnabled = profile.Name.Contains("1031", StringComparison.OrdinalIgnoreCase) && region.IsValid,
                SupportsCharacterOcr = true, SupportsTemplateTraining = true, UsesOfficialCharacterDecision = false,
                UsesSuspiciousValueFilter = false, TemplateFolderName = "SharedNumericDigits",
                TemplateScope = TemplateScope.SharedNumericDigit, DisplayOrder = order,
                ReportGroup = "EpbDiagnostics", ExpectedDigitCountMin = 3, ExpectedDigitCountMax = 4 });
        }
    }
    private static NumericFieldDefinition Definition(string id, string name, string unit, ScreenRegion region, OcrLengthRange range,
        decimal maximum, decimal change, int confirmations, decimal tolerance, bool suspicious, bool training, bool officialCharacters, string folder, int order) => new()
    { FieldId = id, DisplayName = name, Unit = unit, ValueType = FieldValueType.Integer, MinimumValue = 0, MaximumValue = maximum,
        DecimalPlaces = 0, MinimumExpectedDigits = range.Minimum, MaximumExpectedDigits = range.Maximum, RegionCoordinates = Clone(region),
        SupportsCharacterOcr = true, SupportsTemplateTraining = training, UsesOfficialCharacterDecision = officialCharacters,
        UsesSuspiciousValueFilter = suspicious, MaximumChangePerReading = change, ConfirmationReadings = confirmations,
        ConfirmationTolerance = tolerance, TemplateFolderName = folder, DisplayOrder = order, ReportGroup = "OCR" };
    private static void Upsert(OcrProfile profile, NumericFieldDefinition fallback)
    {
        var existing = profile.Fields.FirstOrDefault(x => string.Equals(x.FieldId, fallback.FieldId, StringComparison.OrdinalIgnoreCase));
        if (existing is null) profile.Fields.Add(fallback);
        else if (existing.RegionCoordinates.Width == 0 && existing.RegionCoordinates.Height == 0) existing.RegionCoordinates = fallback.RegionCoordinates;
    }
    private static ScreenRegion Clone(ScreenRegion source) => new() { X = source.X, Y = source.Y, Width = source.Width, Height = source.Height };
}
