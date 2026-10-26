using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public sealed class OcrFeedbackSnapshot : IDisposable
{
    public string MeasurementId { get; init; } = ""; public string FieldId { get; init; } = "";
    public DateTime CapturedAt { get; init; } public Bitmap RawCrop { get; init; } = null!;
    public Bitmap ProcessedCrop { get; init; } = null!; public string RawOcrText { get; init; } = "";
    public decimal? FullLineResult { get; init; } public decimal? CharacterResult { get; init; }
    public string TemplateResult { get; init; } = ""; public decimal? ObservedValue { get; init; }
    public decimal? TrustedValue { get; init; } public string QualityStatus { get; init; } = "";
    public string FailureReason { get; init; } = ""; public string DecisionSource { get; init; } = "";
    public string Pipeline { get; init; } = ""; public ScreenRegion Region { get; init; } = new();
    public OcrFeedbackSnapshot Clone() => new() { MeasurementId=MeasurementId, FieldId=FieldId, CapturedAt=CapturedAt,
        RawCrop=new Bitmap(RawCrop), ProcessedCrop=new Bitmap(ProcessedCrop), RawOcrText=RawOcrText,
        FullLineResult=FullLineResult, CharacterResult=CharacterResult, TemplateResult=TemplateResult,
        ObservedValue=ObservedValue, TrustedValue=TrustedValue, QualityStatus=QualityStatus,
        FailureReason=FailureReason, DecisionSource=DecisionSource, Pipeline=Pipeline,
        Region=new ScreenRegion { X=Region.X,Y=Region.Y,Width=Region.Width,Height=Region.Height } };
    public void Dispose() { RawCrop.Dispose(); ProcessedCrop.Dispose(); }
}

public sealed class OcrFeedbackSnapshotCache : IDisposable
{
    private readonly object _sync=new(); private readonly int _maxMeasurements; private readonly TimeSpan _maxAge;
    private readonly Dictionary<string,OcrFeedbackSnapshot> _items=new(StringComparer.OrdinalIgnoreCase);
    private readonly LinkedList<string> _order=[];
    public OcrFeedbackSnapshotCache(int maxMeasurements=100,int maxAgeMinutes=60)
    { _maxMeasurements=Math.Max(1,maxMeasurements); _maxAge=TimeSpan.FromMinutes(Math.Max(1,maxAgeMinutes)); }
    private static string Key(string measurementId,string fieldId)=>measurementId+"|"+fieldId;
    public void Store(OcrFeedbackSnapshot snapshot)
    { lock(_sync){ Purge(); var key=Key(snapshot.MeasurementId,snapshot.FieldId); if(_items.Remove(key,out var old))old.Dispose();
        _order.Remove(key); _items[key]=snapshot.Clone(); _order.AddLast(key); Trim(); } }
    public bool TryGet(string measurementId,string fieldId,out OcrFeedbackSnapshot? snapshot)
    { lock(_sync){Purge(); if(_items.TryGetValue(Key(measurementId,fieldId),out var found)){snapshot=found.Clone();return true;} snapshot=null;return false;} }
    public int Count { get { lock(_sync){Purge();return _items.Count;} } }
    private void Purge(){var cutoff=DateTime.Now-_maxAge;foreach(var key in _order.ToArray())if(_items.TryGetValue(key,out var item)&&item.CapturedAt<cutoff)Remove(key);}
    private void Trim(){while(_items.Select(x=>x.Value.MeasurementId).Distinct().Count()>_maxMeasurements&&_order.First is not null)Remove(_order.First.Value);}
    private void Remove(string key){_order.Remove(key);if(_items.Remove(key,out var item))item.Dispose();}
    public void Dispose(){lock(_sync){foreach(var item in _items.Values)item.Dispose();_items.Clear();_order.Clear();}}
}
