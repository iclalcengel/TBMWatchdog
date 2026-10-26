using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Channels;
using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public sealed record MismatchCaptureOptions(bool Enabled, int CooldownSeconds,
    int MaximumFilesPerField, int MaximumTotalSizeMb);
public sealed record MismatchCaptureResult(bool Saved, string? SampleId = null, string? SamplePath = null, bool Duplicate = false);
public sealed record MismatchCleanupSummary(int BeforeFiles, double BeforeSizeMb, int DeletedSamples, int AfterFiles, double AfterSizeMb);

public sealed record MismatchCharacterMetadata(int Index, string TesseractDigit, float TesseractConfidence,
    string? TemplateDigit, double TemplateConfidence, string? SecondTemplateDigit,
    double SecondTemplateConfidence, double TemplateMargin, string GeometryDigit,
    double GeometryConfidence, string FinalDigit, string DecisionReason, double InkLossRatio, string InputSource);

public sealed record MismatchSampleMetadata(DateTime Timestamp, string Machine, string Field,
    decimal? FullLineTesseract, decimal? CharacterResult, decimal? OfficialFinalValue,
    string Comparison, string FailureReason, string DecisionSource, double TemplateConfidence,
    double GeometryConfidence, double CharacterDurationMs, double TotalOcrDurationMs,
    double TotalLatencyMs, int SegmentCount, bool SegmentSuccess, DateTime CapturedAt,
    DateTime DisplayedAt, ScreenRegion RawRegionCoordinates, string SelectedProcessingMethod,
    string OcrProfileName, IReadOnlyList<MismatchCharacterMetadata> Characters);

public sealed class MismatchSampleCaptureService : IDisposable
{
    private sealed class WorkItem : IDisposable
    {
        public required string Id; public required string Folder; public required MismatchSampleMetadata Metadata;
        public required Bitmap Raw; public Bitmap? Processed; public Bitmap? Binary; public Bitmap? Segmentation;
        public required IReadOnlyList<Bitmap> Characters;
        public void Dispose() { Raw.Dispose(); Processed?.Dispose(); Binary?.Dispose(); Segmentation?.Dispose(); foreach (var image in Characters) image.Dispose(); }
    }

    private readonly Channel<WorkItem> _queue = Channel.CreateBounded<WorkItem>(new BoundedChannelOptions(100)
    { FullMode = BoundedChannelFullMode.Wait, SingleReader = true, SingleWriter = false });
    private readonly Dictionary<string, DateTime> _recent = new(StringComparer.Ordinal);
    private readonly object _sync = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _worker;
    private readonly MismatchCaptureOptions _options;
    private readonly List<SampleIndexEntry> _sampleIndex = [];
    private long _indexedBytes;
    private bool _indexInitialized;
    private int _pendingCount;
    private sealed record SampleIndexEntry(string MetadataPath, string Field, DateTime CreatedUtc, long Bytes);
    public MismatchSampleCaptureService(MismatchCaptureOptions options, string? root = null)
    {
        _options = options;
        RootFolder = root ?? Path.Combine(AppContext.BaseDirectory, "Diagnostics", "MismatchSamples");
        _worker = Task.Run(ProcessQueueAsync);
    }
    public string RootFolder { get; }
    public long SavedCount { get; private set; }
    public long DuplicateSkipCount { get; private set; }
    public long DroppedCount { get; private set; }
    public int PendingCount => Volatile.Read(ref _pendingCount);
    public int FullTreeScanCount { get; private set; }
    public MismatchCleanupSummary? StartupCleanupSummary { get; private set; }
    public event Action<string>? Warning;

    public MismatchCaptureResult TryEnqueue(string machine, string field, CharacterOcrComparison comparison,
        OcrDiagnosticResult diagnostic, decimal? officialFinalValue, ScreenRegion region, string profileName)
    {
        if (!_options.Enabled || !ShouldCapture(comparison, officialFinalValue)) return new(false);
        try
        {
            var characterCandidate = diagnostic.Candidates.Where(candidate => candidate.UsesCharacterBasedOcr)
                .OrderByDescending(candidate => candidate.CharacterDiagnostics.Count).FirstOrDefault();
            var selected = characterCandidate ?? diagnostic.SelectedCandidate;
            var hash = PerceptualHash(diagnostic.RawImage);
            var duplicateKey = $"{field}|{comparison.FullLineResult}|{comparison.CharacterResult}|{comparison.FailureReason}|{hash}";
            lock (_sync)
            {
                var now = DateTime.Now;
                if (_recent.TryGetValue(duplicateKey, out var previous)
                    && now - previous < TimeSpan.FromSeconds(_options.CooldownSeconds))
                { DuplicateSkipCount++; return new(false, Duplicate: true); }
                _recent[duplicateKey] = now;
                foreach (var expired in _recent.Where(pair => now - pair.Value > TimeSpan.FromMinutes(10)).Select(pair => pair.Key).ToArray()) _recent.Remove(expired);
            }
            var safeMachine = Safe(machine); var safeField = Safe(field);
            var id = Safe($"{comparison.CapturedAt:yyyyMMdd_HHmmss_fff}_{safeField}_FullLine-{Value(comparison.FullLineResult)}_Character-{Value(comparison.CharacterResult)}");
            var folder = Path.Combine(RootFolder, safeMachine, safeField, comparison.CapturedAt.ToString("yyyy-MM-dd"), id);
            var characterDetails = selected?.CharacterDiagnostics.Select(item => new MismatchCharacterMetadata(
                item.Index, item.CleanedDigit, item.OcrConfidence, item.TemplateResult, item.TemplateScore,
                item.SecondBestTemplateResult, item.SecondBestTemplateScore, item.TemplateScoreMargin,
                item.GeometryResult, item.GeometryConfidence, item.FinalDigit, item.DecisionReason,
                item.InkLossRatio, item.SelectedImageSource)).ToArray() ?? [];
            var metadata = new MismatchSampleMetadata(DateTime.Now, machine, safeField,
                comparison.FullLineResult, comparison.CharacterResult, officialFinalValue,
                comparison.IsSame switch { true => "Same", false => "Different", _ => "Unavailable" },
                comparison.FailureReason, selected?.CharacterDecisionSummary ?? diagnostic.DecisionReason,
                characterDetails.DefaultIfEmpty().Max(item => item?.TemplateConfidence ?? 0),
                characterDetails.DefaultIfEmpty().Max(item => item?.GeometryConfidence ?? 0),
                comparison.CharacterDurationMilliseconds, comparison.TotalOcrDurationMilliseconds,
                comparison.TotalLatencyMilliseconds, selected?.SegmentCount ?? 0,
                selected?.SegmentationSuccessful ?? false, comparison.CapturedAt, comparison.DisplayedAt,
                region, selected?.ProcessingMethod ?? "None", profileName, characterDetails);
            var work = new WorkItem { Id = id, Folder = folder, Metadata = metadata,
                Raw = new Bitmap(diagnostic.RawImage), Processed = Clone(selected?.ProcessedImage),
                Binary = Clone(selected?.BinaryImage), Segmentation = Clone(selected?.SegmentationImage),
                Characters = selected?.CharacterDiagnostics.Select(item => new Bitmap(item.OriginalSegmentImage)).ToArray() ?? [] };
            if (!_queue.Writer.TryWrite(work)) { work.Dispose(); DroppedCount++; Warning?.Invoke("Uyuşmazlık örneği kuyruğu dolu; örnek atlandı."); return new(false); }
            Interlocked.Increment(ref _pendingCount); return new(true, id, folder);
        }
        catch (Exception exception) { Warning?.Invoke($"Uyuşmazlık örneği hazırlanamadı: {exception.Message}"); return new(false); }
    }

    private static bool ShouldCapture(CharacterOcrComparison comparison, decimal? official) =>
        comparison.FullLineResult != comparison.CharacterResult
        || comparison.FailureReason != "Success"
        || (comparison.CharacterResult.HasValue && official != comparison.CharacterResult);

    private async Task ProcessQueueAsync()
    {
        try
        {
            BuildIndexOnce();
            StartupCleanupSummary = EnforceRetention();
            await foreach (var work in _queue.Reader.ReadAllAsync(_stop.Token))
            {
                try
                {
                    var indexed = Write(work);
                    _sampleIndex.Add(indexed); _indexedBytes += indexed.Bytes; SavedCount++;
                    if (IsOverLimit(indexed.Field)) EnforceRetention();
                }
                catch (Exception exception) { Warning?.Invoke($"Uyuşmazlık örneği yazılamadı: {exception.Message}"); }
                finally { work.Dispose(); Interlocked.Decrement(ref _pendingCount); }
            }
        }
        catch (OperationCanceledException) { }
    }

    private SampleIndexEntry Write(WorkItem work)
    {
        Directory.CreateDirectory(work.Folder);
        work.Raw.Save(Path.Combine(work.Folder, "Raw.png"));
        work.Processed?.Save(Path.Combine(work.Folder, "Processed.png"));
        work.Binary?.Save(Path.Combine(work.Folder, "Binary.png"));
        work.Segmentation?.Save(Path.Combine(work.Folder, "Segmentation.png"));
        for (var index = 0; index < work.Characters.Count; index++)
            work.Characters[index].Save(Path.Combine(work.Folder, $"Character_{index + 1:00}.png"));
        var metadataPath = Path.Combine(work.Folder, work.Id + ".json");
        File.WriteAllText(metadataPath, JsonSerializer.Serialize(work.Metadata, new JsonSerializerOptions { WriteIndented = true }));
        var bytes = Directory.EnumerateFiles(work.Folder).Sum(file => new FileInfo(file).Length);
        return new SampleIndexEntry(metadataPath, work.Metadata.Field, File.GetCreationTimeUtc(metadataPath), bytes);
    }

    private void BuildIndexOnce()
    {
        if (_indexInitialized) return;
        _indexInitialized = true; FullTreeScanCount++;
        if (!Directory.Exists(RootFolder)) return;
        foreach (var metadata in Directory.EnumerateFiles(RootFolder, "*.json", SearchOption.AllDirectories))
        {
            try
            {
                var folder = Path.GetDirectoryName(metadata)!;
                var field = new DirectoryInfo(folder).Parent?.Parent?.Name ?? "Unknown";
                var bytes = Directory.EnumerateFiles(folder).Sum(file => new FileInfo(file).Length);
                _sampleIndex.Add(new SampleIndexEntry(metadata, field, File.GetCreationTimeUtc(metadata), bytes));
                _indexedBytes += bytes;
            }
            catch { /* bozuk/eski sample retention indeksini durdurmaz */ }
        }
    }

    private bool IsOverLimit(string field) =>
        _sampleIndex.Count(item => item.Field.Equals(field, StringComparison.OrdinalIgnoreCase)) > Math.Max(1, _options.MaximumFilesPerField)
        || _indexedBytes > Math.Max(1, _options.MaximumTotalSizeMb) * 1024L * 1024L;

    private MismatchCleanupSummary EnforceRetention()
    {
        var beforeFiles = _sampleIndex.Count; var beforeBytes = _indexedBytes; var deleted = 0;
        foreach (var group in _sampleIndex.GroupBy(item => item.Field, StringComparer.OrdinalIgnoreCase).ToArray())
            foreach (var entry in group.OrderBy(item => item.CreatedUtc).Take(Math.Max(0, group.Count() - Math.Max(1, _options.MaximumFilesPerField))).ToArray())
                if (DeleteIndexed(entry)) deleted++;
        var maximumBytes = Math.Max(1, _options.MaximumTotalSizeMb) * 1024L * 1024L;
        foreach (var entry in _sampleIndex.OrderBy(item => item.CreatedUtc).ToArray())
        {
            if (_indexedBytes <= maximumBytes) break;
            if (DeleteIndexed(entry)) deleted++;
        }
        var summary = new MismatchCleanupSummary(beforeFiles, Mb(beforeBytes), deleted, _sampleIndex.Count, Mb(_indexedBytes));
        if (deleted > 0)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RootFolder)!);
            File.AppendAllText(Path.Combine(Path.GetDirectoryName(RootFolder)!, "MismatchCleanup.log"),
                $"{DateTime.Now:O},BeforeFiles={summary.BeforeFiles},BeforeSizeMb={summary.BeforeSizeMb:0.00},DeletedSamples={summary.DeletedSamples},AfterFiles={summary.AfterFiles},AfterSizeMb={summary.AfterSizeMb:0.00}{Environment.NewLine}");
        }
        return summary;
    }

    private bool DeleteIndexed(SampleIndexEntry entry)
    {
        try
        {
            var folder = Path.GetDirectoryName(entry.MetadataPath);
            if (folder is not null && Directory.Exists(folder)) Directory.Delete(folder, true);
            _sampleIndex.Remove(entry); _indexedBytes = Math.Max(0, _indexedBytes - entry.Bytes); return true;
        }
        catch (Exception exception) { Warning?.Invoke($"Eski uyuşmazlık örneği temizlenemedi: {exception.Message}"); return false; }
    }
    private static double Mb(long bytes) => bytes / 1024d / 1024d;
    private static Bitmap? Clone(Bitmap? image) => image is null ? null : new Bitmap(image);
    private static string Value(decimal? value) => value?.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) ?? "NONE";
    private static string Safe(string value) { var invalid = Path.GetInvalidFileNameChars(); return new string(value.Select(c => invalid.Contains(c) || char.IsWhiteSpace(c) ? '_' : c).ToArray()); }
    private static string PerceptualHash(Bitmap source)
    {
        using var tiny = new Bitmap(source, 8, 8); var values = new byte[64]; var total = 0;
        for (var y = 0; y < 8; y++) for (var x = 0; x < 8; x++) { var c = tiny.GetPixel(x, y); var v = (byte)((c.R + c.G + c.B) / 3); values[y * 8 + x] = v; total += v; }
        var average = total / 64; ulong bits = 0; for (var i = 0; i < 64; i++) if (values[i] >= average) bits |= 1UL << i;
        return bits.ToString("X16");
    }
    public void Dispose()
    {
        _queue.Writer.TryComplete();
        if (!_worker.Wait(TimeSpan.FromSeconds(5))) _stop.Cancel();
        _stop.Dispose();
    }
}
