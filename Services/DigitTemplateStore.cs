using System.Text.Json;
using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public sealed record TemplateSaveResult(bool Success, string Message, string? ImagePath = null,
    DigitTemplateQualityResult? Quality = null, double NearestSimilarity = 0, int TemplateCount = 0,
    bool CanOverridePoorQuality = false);
public sealed record DigitTemplateItem(int Digit, string ImagePath, string MetadataPath, bool Enabled,
    DigitTemplateMetadata Metadata);

public sealed class DigitTemplateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly object StoreLock = new();
    public DigitTemplateStore(string gateFolder, string machineProfile, OcrFieldKind fieldKind = OcrFieldKind.Gate,
        double duplicateThreshold = .97, int maximumTemplatesPerDigit = 30)
    {
        GateFolder = gateFolder; MachineProfile = machineProfile; FieldKind = fieldKind;
        DuplicateThreshold = Math.Clamp(duplicateThreshold, .85, .9999);
        MaximumTemplatesPerDigit = Math.Clamp(maximumTemplatesPerDigit, 1, 200);
    }
    public string GateFolder { get; }
    public string MachineProfile { get; }
    public OcrFieldKind FieldKind { get; }
    public double DuplicateThreshold { get; }
    public int MaximumTemplatesPerDigit { get; }
    public static bool CanSaveToken(string token) => token.Length == 1 && char.IsDigit(token[0]);

    public TemplateSaveResult Save(int digit, Bitmap source, Bitmap normalized, string preprocessingMethod,
        bool allowPoorQuality = false, double referenceConfidence = 0)
    {
        lock (StoreLock)
        {
            if (digit is < 0 or > 9) return new(false, "Doğru rakam 0–9 arasında olmalıdır.");
            var quality = DigitTemplateQualityEvaluator.Evaluate(normalized);
            using var preparedNew = DigitTemplateMatcher.NormalizeForMatching(normalized);
            var existing = GetItems().Where(item => item.Digit == digit).ToList(); var before = existing.Count;
            if (quality.Status == DigitTemplateQualityStatus.Rejected
                || quality.Status == DigitTemplateQualityStatus.Poor && !allowPoorQuality)
            {
                TemplateTrainingAuditService.Append(MachineProfile, FieldKind.ToString(), digit, "RejectedPoorQuality",
                    quality.QualityScore, 0, before, before, "Rejected", quality.Reason);
                return new(false, $"Bu örnek şablon için düşük kaliteli görünüyor. {quality.Reason}", Quality: quality,
                    TemplateCount: before, CanOverridePoorQuality: quality.Status == DigitTemplateQualityStatus.Poor);
            }
            var digitFolder = Path.Combine(GateFolder, digit.ToString()); Directory.CreateDirectory(digitFolder);
            var nearest = 0d;
            foreach (var item in existing)
            {
                try { using var image = new Bitmap(item.ImagePath); using var prepared = DigitTemplateMatcher.NormalizeForMatching(image);
                    nearest = Math.Max(nearest, DigitTemplateMatcher.CalculateSimilarity(preparedNew, prepared)); }
                catch { }
            }
            if (nearest >= DuplicateThreshold)
            {
                TemplateTrainingAuditService.Append(MachineProfile, FieldKind.ToString(), digit, "RejectedDuplicate",
                    quality.QualityScore, nearest, before, before, "Rejected", "Bu rakam için çok benzer bir şablon zaten mevcut.");
                return new(false, "Bu rakam için çok benzer bir şablon zaten mevcut.", Quality: quality,
                    NearestSimilarity: nearest, TemplateCount: before);
            }
            DigitTemplateItem? replacement = null;
            if (before >= MaximumTemplatesPerDigit)
            {
                replacement = existing.Where(item => !item.Metadata.UserVerified).OrderBy(item => item.Metadata.QualityScore)
                    .FirstOrDefault(item => item.Metadata.QualityScore + .08 < quality.QualityScore);
                if (replacement is null)
                {
                    var reason = $"Şablon limiti dolu ({MaximumTemplatesPerDigit}); doğrulanmış örnekler sessizce silinmedi.";
                    TemplateTrainingAuditService.Append(MachineProfile, FieldKind.ToString(), digit, "Teach", quality.QualityScore,
                        nearest, before, before, "Rejected", reason);
                    return new(false, reason, Quality: quality, NearestSimilarity: nearest, TemplateCount: before);
                }
            }
            var stem = $"{digit}_{DateTime.Now:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}";
            var imagePath = Path.Combine(digitFolder, stem + ".png"); var metadataPath = Path.ChangeExtension(imagePath, ".json");
            var metadata = new DigitTemplateMetadata
            {
                Digit = digit, MachineProfile = MachineProfile, FieldKind = FieldKind.ToString(), SourceField = FieldKind.ToString(),
                TemplateScope = IsSharedNumericField(FieldKind) ? TemplateScope.SharedNumericDigit : TemplateScope.FieldSpecific,
                CapturedAt = DateTime.Now, SourceImageSize = source.Size, NormalizedImageSize = preparedNew.Size,
                PreprocessingMethod = preprocessingMethod, UserVerified = true, Enabled = true,
                ReferenceConfidence = referenceConfidence, QualityScore = quality.QualityScore,
                QualityStatus = quality.Status.ToString(), QualityReason = quality.Reason
            };
            var tempImage = imagePath + ".tmp"; var tempMetadata = metadataPath + ".tmp";
            preparedNew.Save(tempImage, System.Drawing.Imaging.ImageFormat.Png);
            File.WriteAllText(tempMetadata, JsonSerializer.Serialize(metadata, JsonOptions));
            File.Move(tempImage, imagePath); File.Move(tempMetadata, metadataPath);
            if (replacement is not null)
            {
                DeleteFiles(replacement);
                TemplateTrainingAuditService.Append(MachineProfile, FieldKind.ToString(), digit, "ReplacedRedundant",
                    quality.QualityScore, nearest, before, before, "Success", Path.GetFileName(replacement.ImagePath));
            }
            DigitTemplateMatcher.Reload(GateFolder);
            var after = GetItems().Count(item => item.Digit == digit);
            TemplateTrainingAuditService.Append(MachineProfile, FieldKind.ToString(), digit, "Teach", quality.QualityScore,
                nearest, before, after, "Success", "Kullanıcı doğrulamalı şablon kaydedildi.");
            return new(true, $"{digit} rakamı öğretildi. Kalite: %{quality.QualityScore * 100:0}. Bu rakam için toplam şablon: {after}",
                imagePath, quality, nearest, after);
        }
    }

    public IReadOnlyList<DigitTemplateItem> GetItems() => Directory.Exists(GateFolder)
        ? Directory.EnumerateFiles(GateFolder, "*.png", SearchOption.AllDirectories).Select(ToItem).ToArray()
        : Array.Empty<DigitTemplateItem>();

    public void SetEnabled(DigitTemplateItem item, bool enabled)
    {
        lock (StoreLock)
        {
            var metadata = ReadMetadata(item.MetadataPath) ?? NeutralMetadata(item);
            metadata.Enabled = enabled; AtomicWrite(item.MetadataPath, metadata); DigitTemplateMatcher.Reload(GateFolder);
            TemplateTrainingAuditService.Append(MachineProfile, metadata.SourceField, item.Digit, "Disabled", metadata.QualityScore,
                0, GetItems().Count(x => x.Digit == item.Digit), GetItems().Count(x => x.Digit == item.Digit),
                "Success", enabled ? "Etkinleştirildi" : "Devre dışı bırakıldı");
        }
    }

    public void Delete(DigitTemplateItem item)
    {
        lock (StoreLock)
        {
            var before = GetItems().Count(x => x.Digit == item.Digit); DeleteFiles(item); DigitTemplateMatcher.Reload(GateFolder);
            TemplateTrainingAuditService.Append(MachineProfile, item.Metadata.SourceField, item.Digit, "Deleted", item.Metadata.QualityScore,
                0, before, Math.Max(0, before - 1), "Success", Path.GetFileName(item.ImagePath));
        }
    }
    public void Reload() => DigitTemplateMatcher.Reload(GateFolder);
    internal static bool PassesQuality(Bitmap image, out string? error)
    { var quality = DigitTemplateQualityEvaluator.Evaluate(image); error = quality.Status == DigitTemplateQualityStatus.Rejected ? quality.Reason : null; return error is null; }
    internal static DigitTemplateMetadata? ReadMetadata(string path)
    { try { return File.Exists(path) ? JsonSerializer.Deserialize<DigitTemplateMetadata>(File.ReadAllText(path)) : null; } catch { return null; } }

    private DigitTemplateItem ToItem(string path)
    {
        var metadataPath = Path.ChangeExtension(path, ".json"); var metadata = ReadMetadata(metadataPath);
        var folderDigit = int.TryParse(Directory.GetParent(path)?.Name, out var digit) ? digit : ParseLegacyDigit(path);
        metadata ??= new DigitTemplateMetadata { Digit = folderDigit, MachineProfile = MachineProfile, FieldKind = FieldKind.ToString(),
            SourceField = FieldKind.ToString(), CapturedAt = File.GetCreationTime(path), UserVerified = true, Enabled = true,
            QualityScore = .80, QualityStatus = "Acceptable", QualityReason = "Legacy/neutral quality" };
        return new(metadata.Digit, path, metadataPath, metadata.Enabled, metadata);
    }
    private DigitTemplateMetadata NeutralMetadata(DigitTemplateItem item) => new()
    { Digit = item.Digit, MachineProfile = MachineProfile, FieldKind = FieldKind.ToString(), SourceField = FieldKind.ToString(),
        CapturedAt = File.GetCreationTime(item.ImagePath), UserVerified = true, Enabled = true, QualityScore = .80,
        QualityStatus = "Acceptable", QualityReason = "Legacy/neutral quality" };
    private static void AtomicWrite(string path, DigitTemplateMetadata metadata)
    { var temp = path + ".tmp"; File.WriteAllText(temp, JsonSerializer.Serialize(metadata, JsonOptions)); File.Move(temp, path, true); }
    private static void DeleteFiles(DigitTemplateItem item)
    { if (File.Exists(item.ImagePath)) File.Delete(item.ImagePath); if (File.Exists(item.MetadataPath)) File.Delete(item.MetadataPath); }
    private static int ParseLegacyDigit(string path) => int.TryParse(Path.GetFileNameWithoutExtension(path).Split('_')[0], out var digit) ? digit : -1;
    private static bool IsSharedNumericField(OcrFieldKind field) => field is OcrFieldKind.StableRpm or OcrFieldKind.StableTorque
        or OcrFieldKind.StableThrustForce or OcrFieldKind.StableAdvanceSpeed or OcrFieldKind.StablePenetration
        or OcrFieldKind.NominalInjection or OcrFieldKind.RealInjection
        or OcrFieldKind.Epb1 or OcrFieldKind.Epb2 or OcrFieldKind.Epb3 or OcrFieldKind.Epb4 or OcrFieldKind.Epb5;
}
