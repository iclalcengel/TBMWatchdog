using System.Globalization;
using System.Text;
using TBMWatchdog.Models;

namespace TBMWatchdog.Utilities;

public static class MeasurementParser
{
    public static OcrReadResult Parse(string? rawText, float confidence, decimal maximumValue)
    {
        var raw = rawText?.Trim() ?? string.Empty;
        var cleaned = Clean(raw);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return OcrReadResult.Failed(raw, cleaned, confidence, "Sayısal değer okunamadı.");
        }

        if (cleaned[0] == '-')
        {
            return OcrReadResult.Failed(raw, cleaned, confidence, "Negatif OCR sonucu reddedildi.");
        }

        var normalized = NormalizeDecimalSeparators(cleaned);
        if (!decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
        {
            return OcrReadResult.Failed(raw, cleaned, confidence, "OCR sonucu geçerli bir sayıya dönüştürülemedi.");
        }

        if (value < 0 || value > maximumValue)
        {
            return OcrReadResult.Failed(raw, cleaned, confidence, $"OCR sonucu beklenen 0–{maximumValue:0.##} aralığı dışında.");
        }

        return new OcrReadResult(value, raw, cleaned, confidence, null);
    }

    private static string Clean(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsDigit(character) || character is ',' or '.' or '-') builder.Append(character);
        }

        return builder.ToString();
    }

    private static string NormalizeDecimalSeparators(string value)
    {
        var lastComma = value.LastIndexOf(',');
        var lastDot = value.LastIndexOf('.');
        var decimalIndex = Math.Max(lastComma, lastDot);
        if (decimalIndex < 0) return value;

        var digitsBefore = new string(value[..decimalIndex].Where(char.IsDigit).ToArray());
        var digitsAfter = new string(value[(decimalIndex + 1)..].Where(char.IsDigit).ToArray());
        return digitsAfter.Length == 0 ? digitsBefore : $"{digitsBefore}.{digitsAfter}";
    }
}
