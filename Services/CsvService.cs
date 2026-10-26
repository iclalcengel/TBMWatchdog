using System.Globalization;
using System.Text;
using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public sealed class CsvService : IDisposable
{
    private const string Header = "Tarih;Saat;Nominal;Kantar 1;Kantar 2;Kapak mm;Kapak Durumu;Kazı Durumu;OCR Güven Durumu";
    private static readonly CultureInfo DecimalCulture = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private string? _lastSignature;
    private DateOnly? _lastSignatureDate;
    private bool _disposed;

    public CsvService(string outputFolder)
    {
        if (string.IsNullOrWhiteSpace(outputFolder)) throw new ArgumentException("CSV klasörü boş olamaz.", nameof(outputFolder));
        OutputFolder = Path.GetFullPath(Environment.ExpandEnvironmentVariables(outputFolder));
    }

    public string OutputFolder { get; }

    public event Action<string, Exception>? WriteFailed;

    public async Task<CsvWriteResult> WriteIfChangedAsync(MeasurementRecord record, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(record);
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var recordDate = DateOnly.FromDateTime(record.Timestamp);
            var signature = CreateSignature(record);
            if (_lastSignatureDate == recordDate && string.Equals(_lastSignature, signature, StringComparison.Ordinal))
            {
                return CsvWriteResult.SkippedDuplicate;
            }

            Directory.CreateDirectory(OutputFolder);
            var filePath = GetFilePath(record.Timestamp);
            var isNewFile = !File.Exists(filePath) || new FileInfo(filePath).Length == 0;

            await using var stream = new FileStream(
                filePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);
            await using var writer = new StreamWriter(stream, Utf8WithBom);
            if (isNewFile)
            {
                await writer.WriteLineAsync(Header.AsMemory(), cancellationToken).ConfigureAwait(false);
            }

            var line = CreateCsvLine(record);
            await writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            _lastSignature = signature;
            _lastSignatureDate = recordDate;
            return CsvWriteResult.Written;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            var filePath = GetFilePath(record.Timestamp);
            WriteFailed?.Invoke(filePath, exception);
            return CsvWriteResult.Failed;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public string GetFilePath(DateTime date) =>
        Path.Combine(OutputFolder, $"TBM_Kayit_{date:yyyy-MM-dd}.csv");

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }

    private static string CreateCsvLine(MeasurementRecord record)
    {
        var fields = new[]
        {
            record.Timestamp.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
            record.Timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture),
            FormatNumber(record.Nominal),
            FormatNumber(record.Scale1),
            FormatNumber(record.Scale2),
            FormatNumber(record.GateMillimeters),
            record.GateStatus,
            record.ExcavationStatus,
            ConfidenceText(record.OcrConfidence)
        };
        return string.Join(';', fields.Select(Escape));
    }

    private static string CreateSignature(MeasurementRecord record) => string.Join('|', new[]
    {
        InvariantNumber(record.Nominal),
        InvariantNumber(record.Scale1),
        InvariantNumber(record.Scale2),
        InvariantNumber(record.GateMillimeters),
        record.GateStatus,
        record.ExcavationStatus,
        record.OcrConfidence.ToString()
    });

    private static string FormatNumber(decimal? value) =>
        value?.ToString("0.############################", DecimalCulture) ?? "Okunamadı";

    private static string InvariantNumber(decimal? value) =>
        value?.ToString("G29", CultureInfo.InvariantCulture) ?? "<null>";

    private static string Escape(string value)
    {
        if (!value.Contains(';') && !value.Contains('"') && !value.Contains('\r') && !value.Contains('\n')) return value;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static string ConfidenceText(OcrConfidenceStatus status) => status switch
    {
        OcrConfidenceStatus.Reliable => "Güvenilir",
        OcrConfidenceStatus.Partial => "Kısmi",
        _ => "Okunamadı"
    };
}

public enum CsvWriteResult
{
    Written,
    SkippedDuplicate,
    Failed
}
