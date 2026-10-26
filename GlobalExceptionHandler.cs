using TBMWatchdog.Services;

namespace TBMWatchdog;

internal static class GlobalExceptionHandler
{
    private static LogService? _logService;

    public static void Register(LogService logService)
    {
        _logService = logService;
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, args) => ShowError(args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            ShowError(args.ExceptionObject as Exception ?? new InvalidOperationException("Bilinmeyen uygulama hatası."));
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            args.SetObserved();
            ShowError(args.Exception);
        };
    }

    private static void ShowError(Exception exception)
    {
        _logService?.Error("Beklenmeyen uygulama hatası.", exception);
        MessageBox.Show(
            $"Beklenmeyen bir hata oluştu. Uygulama mümkünse çalışmaya devam edecek.\n\n{exception.Message}",
            "TBM Watchdog",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
