using System.Globalization;

namespace TBMWatchdog.Models;

public sealed record OcrFeedbackUiAnnotation(bool Visible, string DisplayText, string Tooltip,
    OcrFeedbackVerdict? Verdict, decimal? CorrectedValue)
{
    public static OcrFeedbackUiAnnotation None(string baseText, string baseTooltip = "") =>
        new(false, baseText, baseTooltip, null, null);

    public static OcrFeedbackUiAnnotation Create(string baseText, string baseTooltip, OcrUserFeedback? feedback)
    {
        if (feedback is null || feedback.Verdict == OcrFeedbackVerdict.Removed) return None(baseText, baseTooltip);
        var automaticDisplay = CleanAutomaticMarkers(baseText);
        var observed = feedback.ObservedValue?.ToString(CultureInfo.InvariantCulture) ?? automaticDisplay;
        var display = feedback.Verdict switch
        {
            OcrFeedbackVerdict.Correct => automaticDisplay + " ✓",
            // Yanlış etiketi OCR gözlemini gizlemez; değer görünür kalır.
            OcrFeedbackVerdict.Incorrect => automaticDisplay + " ✗",
            OcrFeedbackVerdict.Corrected => $"{feedback.CorrectedValue?.ToString(CultureInfo.InvariantCulture) ?? "—"} ✓",
            _ => baseText
        };
        var label = feedback.Verdict switch
        { OcrFeedbackVerdict.Correct => "Doğru", OcrFeedbackVerdict.Incorrect => "Yanlış", OcrFeedbackVerdict.Corrected => "Düzeltildi", _ => "—" };
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(baseTooltip))
            lines.Add("Otomatik kalite motoru bilgisi:\n" + baseTooltip + "\nKullanıcı doğrulaması presentation görünümünde önceliklidir.");
        lines.Add($"OCR sonucu: {observed}");
        lines.Add($"Kullanıcı etiketi: {label}");
        if (feedback.CorrectedValue.HasValue) lines.Add($"Kullanıcı doğrusu: {feedback.CorrectedValue.Value.ToString(CultureInfo.InvariantCulture)}");
        if (feedback.Verdict == OcrFeedbackVerdict.Incorrect) lines.Add("Doğru değer girilmedi.");
        lines.Add($"Feedback zamanı: {feedback.FeedbackAt:dd.MM.yyyy HH:mm:ss}");
        if (!string.IsNullOrWhiteSpace(feedback.RevisesFeedbackId)) lines.Add($"Son revision: {feedback.FeedbackId} (önceki: {feedback.RevisesFeedbackId})");
        return new(true, display, string.Join("\n", lines), feedback.Verdict, feedback.CorrectedValue);
    }

    private static string CleanAutomaticMarkers(string value) => value
        .Replace("⚠", "", StringComparison.Ordinal)
        .Replace("?", "", StringComparison.Ordinal)
        .Trim();
}
