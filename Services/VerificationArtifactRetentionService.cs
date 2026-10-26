namespace TBMWatchdog.Services;

public static class VerificationArtifactRetentionService
{
    public static string Root => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "artifacts", "verify"));

    public static int RetainLatestRuns(string root, int maximumRuns)
    {
        if (!Directory.Exists(root)) return 0;
        var deleted = 0;
        foreach (var directory in new DirectoryInfo(root).EnumerateDirectories()
                     .OrderByDescending(item => item.LastWriteTimeUtc).Skip(Math.Max(1, maximumRuns)))
        {
            directory.Delete(true); deleted++;
        }
        return deleted;
    }
}
