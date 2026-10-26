using System.Text.Json;
using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public sealed class MonitoringSessionStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public MonitoringSessionStore(string? directory = null)
    {
        DirectoryPath = directory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TBMWatchdog");
        FilePath = Path.Combine(DirectoryPath, "active-monitoring-session.json");
    }
    public string DirectoryPath { get; }
    public string FilePath { get; }

    public MonitoringSessionState? Load()
    {
        try { return File.Exists(FilePath) ? JsonSerializer.Deserialize<MonitoringSessionState>(File.ReadAllText(FilePath), Options) : null; }
        catch { return null; }
    }

    public void Save(MonitoringSessionState state)
    {
        try
        {
            Directory.CreateDirectory(DirectoryPath); var temporary = FilePath + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(state, Options)); File.Move(temporary, FilePath, true);
        }
        catch { /* Oturum kaydı ana izlemeyi durdurmamalı. */ }
    }
}
