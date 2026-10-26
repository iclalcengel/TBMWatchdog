using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public static class GateTemplatePaths
{
    public static string Resolve(OcrProfile profile)
    {
        var configured = profile.GateDigitTemplateFolder;
        var root = Path.IsPathRooted(configured) ? configured : Path.Combine(AppContext.BaseDirectory, configured);
        if (string.Equals(Path.GetFileName(root), "GateDigits", StringComparison.OrdinalIgnoreCase))
            root = Directory.GetParent(root)?.FullName ?? root;
        return Path.Combine(root, Safe(profile.Name), "GateDigits");
    }

    public static string Safe(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string((string.IsNullOrWhiteSpace(value) ? "Default" : value)
            .Select(character => invalid.Contains(character) ? '_' : character).ToArray());
        return safe.Trim();
    }
}
