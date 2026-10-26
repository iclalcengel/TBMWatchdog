using System.Drawing.Imaging;
using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public sealed class ScreenCaptureService
{
    public ScreenCaptureService(string? screenshotDirectory = null)
    {
        ScreenshotDirectory = screenshotDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TBMWatchdog",
            "screenshots");
    }

    public string ScreenshotDirectory { get; }

    public IReadOnlyList<ScreenInfo> GetScreens() => Screen.AllScreens
        .Select((screen, index) => new ScreenInfo(
            index,
            screen.DeviceName,
            screen.Bounds,
            screen.Primary))
        .ToArray();

    public Bitmap CaptureRegion(int screenNumber, ScreenRegion region)
    {
        var screen = GetScreen(screenNumber);
        ValidateRegion(region, screen.Bounds.Size);

        var bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format24bppRgb);
        try
        {
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(
                screen.Bounds.X + region.X,
                screen.Bounds.Y + region.Y,
                0,
                0,
                new Size(region.Width, region.Height),
                CopyPixelOperation.SourceCopy);
            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    public Bitmap CaptureScreen(int screenNumber)
    {
        var screen = GetScreen(screenNumber);
        var bitmap = new Bitmap(screen.Bounds.Width, screen.Bounds.Height, PixelFormat.Format24bppRgb);
        try
        {
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(screen.Bounds.Location, Point.Empty, screen.Bounds.Size, CopyPixelOperation.SourceCopy);
            return bitmap;
        }
        catch { bitmap.Dispose(); throw; }
    }

    public static Bitmap CropRegionWithPadding(Bitmap screenImage, ScreenRegion region, OcrProfile profile,
        OcrFieldKind field = OcrFieldKind.General)
    {
        ArgumentNullException.ThrowIfNull(screenImage); ArgumentNullException.ThrowIfNull(region);
        var padded = GetPaddedRegion(region, profile, screenImage.Size, field);
        var result = new Bitmap(padded.Width, padded.Height, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(result);
        graphics.DrawImage(screenImage, new Rectangle(0, 0, result.Width, result.Height),
            new Rectangle(padded.X, padded.Y, padded.Width, padded.Height), GraphicsUnit.Pixel);
        return result;
    }

    public Task<CapturedScreenRegions> CaptureConfiguredRegionsAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            Bitmap? screenImage = null;
            Bitmap? nominal = null;
            Bitmap? scale1 = null;
            Bitmap? scale2 = null;
            Bitmap? gate = null;
            try
            {
                screenImage = CaptureScreen(settings.ScreenNumber);
                var passCapturedAt = DateTime.Now;
                nominal = CropRegionWithPadding(screenImage, settings.NominalRegion, settings.OcrProfile);
                var nominalCapturedAt = passCapturedAt;
                cancellationToken.ThrowIfCancellationRequested();
                scale1 = CropRegionWithPadding(screenImage, settings.Scale1Region, settings.OcrProfile, OcrFieldKind.Scale1);
                var scale1CapturedAt = passCapturedAt;
                cancellationToken.ThrowIfCancellationRequested();
                scale2 = CropRegionWithPadding(screenImage, settings.Scale2Region, settings.OcrProfile, OcrFieldKind.Scale2);
                var scale2CapturedAt = passCapturedAt;
                cancellationToken.ThrowIfCancellationRequested();
                gate = CropRegionWithPadding(screenImage, settings.GateRegion, settings.OcrProfile, OcrFieldKind.Gate);
                var gateCapturedAt = passCapturedAt;
                return new CapturedScreenRegions(nominal, scale1, scale2, gate,
                    nominalCapturedAt, scale1CapturedAt, scale2CapturedAt, gateCapturedAt, screenImage);
            }
            catch
            {
                nominal?.Dispose();
                scale1?.Dispose();
                scale2?.Dispose();
                gate?.Dispose();
                screenImage?.Dispose();
                throw;
            }
        }, cancellationToken);
    }

    public string SaveRegion(Bitmap bitmap, string regionName, DateTime timestamp)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        var safeName = string.Concat(regionName.Select(character =>
            Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
        Directory.CreateDirectory(ScreenshotDirectory);
        var path = Path.Combine(ScreenshotDirectory, $"{timestamp:yyyyMMdd_HHmmss_fff}_{safeName}.png");
        bitmap.Save(path, ImageFormat.Png);
        return path;
    }

    public Bitmap CaptureRegionWithPadding(int screenNumber, ScreenRegion region, int padding)
    {
        if (padding < 0) throw new ArgumentOutOfRangeException(nameof(padding));
        var screen = GetScreen(screenNumber);
        ValidateRegion(region, screen.Bounds.Size);
        var padded = GetPaddedRegion(region, padding, screen.Bounds.Size);
        return CaptureRegion(screenNumber, padded);
    }

    public Bitmap CaptureRegionWithPadding(int screenNumber, ScreenRegion region, OcrProfile profile, OcrFieldKind field = OcrFieldKind.General)
    {
        var screen = GetScreen(screenNumber);
        ValidateRegion(region, screen.Bounds.Size);
        var padded = GetPaddedRegion(region, profile, screen.Bounds.Size, field);
        return CaptureRegion(screenNumber, padded);
    }

    public static ScreenRegion GetPaddedRegion(ScreenRegion region, OcrProfile profile, Size screenSize, OcrFieldKind field = OcrFieldKind.General)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var gate = field == OcrFieldKind.Gate;
        var left = Math.Max(0, region.X - (gate ? profile.GatePaddingLeft : profile.PaddingLeft));
        var top = Math.Max(0, region.Y - (gate ? profile.GatePaddingTop : profile.PaddingTop));
        var right = Math.Min(screenSize.Width, region.X + region.Width + (gate ? profile.GatePaddingRight : profile.PaddingRight));
        var bottom = Math.Min(screenSize.Height, region.Y + region.Height + (gate ? profile.GatePaddingBottom : profile.PaddingBottom));
        return new ScreenRegion { X = left, Y = top, Width = Math.Max(1, right - left), Height = Math.Max(1, bottom - top) };
    }

    public static ScreenRegion GetPaddedRegion(ScreenRegion region, int padding, Size screenSize)
    {
        ArgumentNullException.ThrowIfNull(region);
        if (padding < 0) throw new ArgumentOutOfRangeException(nameof(padding));
        var left = Math.Max(0, region.X - padding);
        var top = Math.Max(0, region.Y - padding);
        var right = Math.Min(screenSize.Width, region.X + region.Width + padding);
        var bottom = Math.Min(screenSize.Height, region.Y + region.Height + padding);
        return new ScreenRegion
        {
            X = left,
            Y = top,
            Width = Math.Max(1, right - left),
            Height = Math.Max(1, bottom - top)
        };
    }

    public void ValidateConfiguration(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var screen = GetScreen(settings.ScreenNumber);
        ValidateRegion(settings.NominalRegion, screen.Bounds.Size, "Nominal");
        ValidateRegion(settings.Scale1Region, screen.Bounds.Size, "Kantar 1");
        ValidateRegion(settings.Scale2Region, screen.Bounds.Size, "Kantar 2");
        ValidateRegion(settings.GateRegion, screen.Bounds.Size, "Kapak");
    }

    private static Screen GetScreen(int screenNumber)
    {
        var screens = Screen.AllScreens;
        if (screenNumber < 0 || screenNumber >= screens.Length)
        {
            throw new ScreenCaptureException(
                $"Ekran {screenNumber} bulunamadı. Kullanılabilir ekran numaraları: 0–{Math.Max(0, screens.Length - 1)}.");
        }

        return screens[screenNumber];
    }

    private static void ValidateRegion(ScreenRegion region, Size screenSize, string? regionName = null)
    {
        ArgumentNullException.ThrowIfNull(region);
        var name = string.IsNullOrWhiteSpace(regionName) ? "Ekran" : regionName;
        if (!region.IsValid)
        {
            throw new ScreenCaptureException($"{name} bölgesinin genişlik ve yüksekliği sıfırdan büyük olmalıdır.");
        }

        if (region.X < 0 || region.Y < 0
            || (long)region.X + region.Width > screenSize.Width
            || (long)region.Y + region.Height > screenSize.Height)
        {
            throw new ScreenCaptureException(
                $"{name} bölgesi seçilen ekranın {screenSize.Width}×{screenSize.Height} sınırları dışında.");
        }
    }
}

public sealed record ScreenInfo(int Number, string DeviceName, Rectangle Bounds, bool IsPrimary);

public sealed class CapturedScreenRegions : IDisposable
{
    public CapturedScreenRegions(Bitmap nominal, Bitmap scale1, Bitmap scale2, Bitmap gate, DateTime capturedAt)
        : this(nominal, scale1, scale2, gate, capturedAt, capturedAt, capturedAt, capturedAt) { }

    public CapturedScreenRegions(Bitmap nominal, Bitmap scale1, Bitmap scale2, Bitmap gate,
        DateTime nominalCapturedAt, DateTime scale1CapturedAt, DateTime scale2CapturedAt, DateTime gateCapturedAt, Bitmap? screenImage=null)
    {
        Nominal = nominal;
        Scale1 = scale1;
        Scale2 = scale2;
        Gate = gate;
        NominalCapturedAt = nominalCapturedAt;
        Scale1CapturedAt = scale1CapturedAt;
        Scale2CapturedAt = scale2CapturedAt;
        GateCapturedAt = gateCapturedAt;
        ScreenImage = screenImage;
        CapturedAt = new[] { nominalCapturedAt, scale1CapturedAt, scale2CapturedAt, gateCapturedAt }.Max();
    }

    public Bitmap Nominal { get; }
    public Bitmap Scale1 { get; }
    public Bitmap Scale2 { get; }
    public Bitmap Gate { get; }
    public DateTime CapturedAt { get; }
    public DateTime NominalCapturedAt { get; }
    public DateTime Scale1CapturedAt { get; }
    public DateTime Scale2CapturedAt { get; }
    public DateTime GateCapturedAt { get; }
    public Bitmap? ScreenImage { get; }

    public void Dispose()
    {
        Nominal.Dispose();
        Scale1.Dispose();
        Scale2.Dispose();
        Gate.Dispose();
        ScreenImage?.Dispose();
    }
}

public sealed class ScreenCaptureException : Exception
{
    public ScreenCaptureException(string message) : base(message) { }
    public ScreenCaptureException(string message, Exception innerException) : base(message, innerException) { }
}
