using System.Text;

namespace TBMWatchdog.Services;

public sealed class LogService : IDisposable
{
    private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);
    private readonly object _sync = new();
    private bool _disposed;

    public LogService(string? logDirectory = null)
    {
        LogDirectory = logDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TBMWatchdog",
            "logs");

        try
        {
            Directory.CreateDirectory(LogDirectory);
        }
        catch
        {
            // Log altyapısı ana uygulamanın açılmasını engellememelidir.
        }
    }

    public string LogDirectory { get; }

    public void Information(string message) => Write("BİLGİ", message, null);
    public void Warning(string message, Exception? exception = null) => Write("UYARI", message, exception);
    public void Error(string message, Exception exception) => Write("HATA", message, exception);

    public string GetLogPath(DateTime date) => Path.Combine(LogDirectory, $"TBMWatchdog_{date:yyyy-MM-dd}.log");

    public void Dispose()
    {
        lock (_sync) _disposed = true;
    }

    private void Write(string level, string message, Exception? exception)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        lock (_sync)
        {
            if (_disposed) return;
            try
            {
                Directory.CreateDirectory(LogDirectory);
                var now = DateTime.Now;
                var path = GetLogPath(now);
                using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using var writer = new StreamWriter(stream, Utf8WithBom);
                writer.Write(now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                writer.Write(" | ");
                writer.Write(level);
                writer.Write(" | ");
                writer.WriteLine(SingleLine(message));
                if (exception is not null)
                {
                    writer.WriteLine(exception.ToString());
                }
            }
            catch
            {
                // Loglama hatası uygulama akışını kesmemelidir.
            }
        }
    }

    private static string SingleLine(string value) => value.Replace("\r", " ").Replace("\n", " ");
}
