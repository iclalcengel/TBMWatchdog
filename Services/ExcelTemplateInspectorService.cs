using System.IO.Compression;
using System.Xml.Linq;
using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

/// <summary>Excel şablonunu ZIP/XML düzeyinde yalnızca okur; hiçbir yazma API'si içermez.</summary>
public sealed class ExcelTemplateInspectorService
{
    public static readonly IReadOnlyDictionary<string, string> MachineSheets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    { ["1028"] = "S1028 KP", ["1029"] = "S1029 KP", ["1030"] = "S1030 KP", ["1031"] = "S1031 KP" };

    public static readonly string[] ExpectedHeaders =
    {
        "Tarih", "Ring No", "Km", "RPM", "Kazı süresi", "Tork", "İtme Kuvveti", "İlerleme hızı",
        "Penetrasyon", "Nominal Pasa Miktarı", "Kantar 1", "Kantar 2", "Yoğunluk", "Nominal Enjeksiyon",
        "Gerçek Enjeksiyon", "1 no'lu EPB sensörü", "2 no'lu EPB sensörü", "3 no'lu EPB sensörü",
        "4 no'lu EPB sensörü", "5 no'lu EPB sensörü", "Kullanılan köpük", "Köpük Tüketimi Litre",
        "Kullanılan Kuyruk Gresi", "Kuyruk Gresi Tüketimi Kg"
    };


    public ExcelTemplatePreview Inspect(string excelFilePath, string machineId, string ringNo)
    {
        var requestedSheet = MachineSheets.TryGetValue(machineId?.Trim() ?? "", out var mapped) ? mapped : "";
        ExcelTemplatePreview Invalid(string message, string? sheet = null) => new()
        { MachineId = machineId ?? "", SheetName = sheet ?? requestedSheet, RingNo = ringNo ?? "", ExcelFilePath = excelFilePath ?? "", TemplateIsValid = false, ValidationMessage = message };
        if (string.IsNullOrWhiteSpace(requestedSheet)) return Invalid($"Excel şablonu geçersiz: Desteklenmeyen makine kimliği: {machineId}");
        if (string.IsNullOrWhiteSpace(excelFilePath) || !File.Exists(excelFilePath)) return Invalid("Excel şablonu geçersiz: Dosya bulunamadı");
        if (!string.Equals(Path.GetExtension(excelFilePath), ".xlsx", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(Path.GetExtension(excelFilePath), ".xlsm", StringComparison.OrdinalIgnoreCase))
            return Invalid("Excel şablonu geçersiz: Yalnızca .xlsx veya .xlsm dosyaları desteklenir");
        try
        {
            using var stream = new FileStream(excelFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            var sharedStrings = ReadSharedStrings(archive); var sheets = ReadSheetPaths(archive);
            foreach (var required in MachineSheets.Values)
                if (!sheets.ContainsKey(required)) return Invalid($"Excel şablonu geçersiz: Beklenen sayfa bulunamadı: {required}", required);
            if (!sheets.TryGetValue(requestedSheet, out var sheetPath)) return Invalid($"Excel şablonu geçersiz: Beklenen sayfa bulunamadı: {requestedSheet}");
            var entry = archive.GetEntry(sheetPath); if (entry is null) return Invalid($"Excel şablonu geçersiz: Sayfa içeriği bulunamadı: {requestedSheet}");
            XDocument sheet; using (var input = entry.Open()) sheet = XDocument.Load(input, LoadOptions.None);
            var cells = ReadCells(sheet, sharedStrings);
            for (var index = 0; index < ExpectedHeaders.Length; index++)
            {
                var reference = ColumnName(index + 1) + "1"; cells.TryGetValue(reference, out var actual);
                if (!string.Equals(Normalize(actual), Normalize(ExpectedHeaders[index]), StringComparison.OrdinalIgnoreCase))
                    return Invalid($"Excel şablonu geçersiz: Beklenen sütun bulunamadı veya farklı: {ColumnName(index + 1)} {ExpectedHeaders[index]}");
            }
            var normalizedRing = Normalize(ringNo);
            var existing = cells.Where(pair => Column(pair.Key) == "B" && Row(pair.Key) >= 2
                    && string.Equals(Normalize(pair.Value), normalizedRing, StringComparison.OrdinalIgnoreCase))
                .Select(pair => Row(pair.Key)).OrderBy(row => row).FirstOrDefault();
            var maximumUsedRow = cells.Keys.Select(Row).DefaultIfEmpty(1).Max();
            return new ExcelTemplatePreview { MachineId = machineId ?? "", SheetName = requestedSheet, RingNo = ringNo ?? "",
                RingAlreadyExists = existing >= 2, TargetRow = existing >= 2 ? existing : Math.Max(2, maximumUsedRow + 1),
                TemplateIsValid = true, ValidationMessage = "Excel şablonu geçerli", ExcelFilePath = excelFilePath };
        }
        catch (InvalidDataException exception) { return Invalid($"Excel şablonu geçersiz: Dosya yapısı okunamadı: {exception.Message}"); }
        catch (IOException exception) { return Invalid($"Excel şablonu okunamadı: {exception.Message}"); }
        catch (UnauthorizedAccessException exception) { return Invalid($"Excel şablonuna erişilemedi: {exception.Message}"); }
        catch (System.Xml.XmlException exception) { return Invalid($"Excel şablonu geçersiz: XML yapısı okunamadı: {exception.Message}"); }
    }

    private static Dictionary<string, string> ReadSheetPaths(ZipArchive archive)
    {
        var workbookEntry = archive.GetEntry("xl/workbook.xml") ?? throw new InvalidDataException("workbook.xml bulunamadı");
        var relationshipsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels") ?? throw new InvalidDataException("workbook ilişkileri bulunamadı");
        XDocument workbook; XDocument relationships; using (var input = workbookEntry.Open()) workbook = XDocument.Load(input); using (var input = relationshipsEntry.Open()) relationships = XDocument.Load(input);
        var targets = relationships.Descendants().Where(element => element.Name.LocalName == "Relationship")
            .ToDictionary(element => (string?)element.Attribute("Id") ?? "", element => (string?)element.Attribute("Target") ?? "");
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sheet in workbook.Descendants().Where(element => element.Name.LocalName == "sheet"))
        {
            var name = (string?)sheet.Attribute("name") ?? ""; var relationshipId = sheet.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "id")?.Value ?? "";
            if (!targets.TryGetValue(relationshipId, out var target)) continue; var normalized = target.Replace('\\', '/').TrimStart('/');
            if (!normalized.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)) normalized = "xl/" + normalized.TrimStart('/'); result[name] = normalized;
        }
        return result;
    }
    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml"); if (entry is null) return Array.Empty<string>(); XDocument document; using (var input = entry.Open()) document = XDocument.Load(input);
        return document.Descendants().Where(element => element.Name.LocalName == "si").Select(item => string.Concat(item.Descendants().Where(element => element.Name.LocalName == "t").Select(element => element.Value))).ToArray();
    }
    private static Dictionary<string, string> ReadCells(XDocument sheet, IReadOnlyList<string> sharedStrings)
    {
        var cells = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in sheet.Descendants().Where(element => element.Name.LocalName == "c"))
        {
            var reference = ((string?)cell.Attribute("r") ?? "").ToUpperInvariant(); if (reference.Length == 0) continue; var type = (string?)cell.Attribute("t"); string value;
            if (type == "inlineStr") value = string.Concat(cell.Descendants().Where(element => element.Name.LocalName == "t").Select(element => element.Value));
            else { value = cell.Elements().FirstOrDefault(element => element.Name.LocalName == "v")?.Value ?? ""; if (type == "s" && int.TryParse(value, out var index) && index >= 0 && index < sharedStrings.Count) value = sharedStrings[index]; }
            cells[reference] = value;
        }
        return cells;
    }
    private static string Normalize(string? value) => (value ?? "").Trim().Replace("\r", "").Replace("\n", " ");
    private static string Column(string reference) => new(reference.TakeWhile(char.IsLetter).ToArray());
    private static int Row(string reference) => int.TryParse(new string(reference.SkipWhile(char.IsLetter).ToArray()), out var row) ? row : 0;
    private static string ColumnName(int number) { var name = ""; while (number > 0) { number--; name = (char)('A' + number % 26) + name; number /= 26; } return name; }
}
