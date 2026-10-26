namespace TBMWatchdog.Services;

internal static class CsvLogRotation
{
    public const int DefaultMaximumSizeMb = 20;

    public static string RotateIfNeeded(string activePath, int maximumSizeMb = DefaultMaximumSizeMb)
    {
        if (!File.Exists(activePath) || new FileInfo(activePath).Length <= Math.Max(1, maximumSizeMb) * 1024L * 1024L)
            return activePath;
        var directory = Path.GetDirectoryName(activePath)!;
        var stem = Path.GetFileNameWithoutExtension(activePath);
        var extension = Path.GetExtension(activePath);
        var archive = Path.Combine(directory, $"{stem}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}");
        File.Move(activePath, archive, false);
        return archive;
    }
}
