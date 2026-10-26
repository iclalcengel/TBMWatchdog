using System.Collections.Concurrent;

namespace TBMWatchdog.Services;

public static class DigitTemplateUsageTracker
{
    private sealed class Delta { public long Usage; public long Success; public long Conflict; }
    private static readonly ConcurrentDictionary<string, Delta> Pending = new(StringComparer.OrdinalIgnoreCase);
    static DigitTemplateUsageTracker() { AppDomain.CurrentDomain.ProcessExit += (_, _) => Flush(); }
    public static void RecordUsage(string? imagePath) { if (!string.IsNullOrWhiteSpace(imagePath)) Pending.GetOrAdd(imagePath, _ => new()).Usage++; }
    public static void RecordOutcome(string? imagePath, bool success, bool conflict)
    { if (string.IsNullOrWhiteSpace(imagePath)) return; var delta = Pending.GetOrAdd(imagePath, _ => new()); if (success) delta.Success++; if (conflict) delta.Conflict++; }
    public static void Flush()
    {
        foreach (var pair in Pending.ToArray())
        {
            if (!Pending.TryRemove(pair.Key, out var delta)) continue;
            var metadataPath = Path.ChangeExtension(pair.Key, ".json"); var metadata = DigitTemplateStore.ReadMetadata(metadataPath);
            if (metadata is null) continue;
            metadata.UsageCount += delta.Usage; metadata.SuccessCount += delta.Success; metadata.ConflictCount += delta.Conflict;
            try { var json = System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                var temp = metadataPath + ".stats.tmp"; File.WriteAllText(temp, json); File.Move(temp, metadataPath, true); } catch { }
        }
    }
}
