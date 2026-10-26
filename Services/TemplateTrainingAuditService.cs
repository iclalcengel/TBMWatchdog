using System.Globalization;
using System.Text;

namespace TBMWatchdog.Services;

public static class TemplateTrainingAuditService
{
    private static readonly object Sync = new();
    public static string FilePath { get; set; } = Path.Combine(AppContext.BaseDirectory, "Diagnostics", "TemplateTrainingEvents.csv");
    public static void Append(string machine, string sourceField, int digit, string action, double quality,
        double similarity, int before, int after, string result, string reason)
    {
        lock (Sync)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            if (!File.Exists(FilePath)) File.WriteAllText(FilePath,
                "Timestamp,MachineProfile,SourceField,Digit,Action,QualityScore,SimilarityToNearestExisting,TemplateCountBefore,TemplateCountAfter,Result,Reason\r\n", new UTF8Encoding(true));
            string Q(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
            string N(double value) => value.ToString("0.000", CultureInfo.InvariantCulture);
            File.AppendAllText(FilePath, string.Join(',', DateTime.Now.ToString("O"), Q(machine), Q(sourceField), digit,
                action, N(quality), N(similarity), before, after, Q(result), Q(reason)) + "\r\n", new UTF8Encoding(false));
        }
    }
}
