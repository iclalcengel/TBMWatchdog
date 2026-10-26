using System.Globalization;
using System.Text;
using System.Text.Json;
using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public sealed class OcrUserFeedbackService
{
    public static event Action? FeedbackChanged;
    private readonly string _root; private readonly string _csv; private readonly object _sync=new();
    private readonly Dictionary<string,OcrUserFeedback> _latest=new(StringComparer.OrdinalIgnoreCase);
    public OcrUserFeedbackService(string? root=null){_root=root??Path.Combine(AppContext.BaseDirectory,"Diagnostics","UserFeedback");_csv=Path.Combine(Path.GetDirectoryName(_root)!,"OcrUserFeedback.csv");Reload();}
    private static string Key(string m,string f)=>m+"|"+f;
    public OcrUserFeedback? Latest(string measurementId,string fieldId){lock(_sync){var value=_latest.GetValueOrDefault(Key(measurementId,fieldId));return value?.Verdict==OcrFeedbackVerdict.Removed?null:value;}}
    internal OcrUserFeedback? LatestRevision(string measurementId,string fieldId){lock(_sync)return _latest.GetValueOrDefault(Key(measurementId,fieldId));}
    public void Reload()
    { lock(_sync){_latest.Clear();if(!File.Exists(_csv))return;var metadata=LoadJsonMetadata();foreach(var row in ReadCsv(_csv))
      {if(row.Count<16||row[0]=="FeedbackId")continue;try{var id=row[0];var item=metadata.GetValueOrDefault(id)??new OcrUserFeedback{FeedbackId=id,MeasurementId=row[1],FeedbackAt=DateTime.Parse(row[2],CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind),CapturedAt=DateTime.Parse(row[3],CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind),MachineId=row[4],RingNo=Empty(row[5]),FieldId=row[6],ObservedValue=Dec(row[7]),TrustedValueAtThatTime=Dec(row[8]),Verdict=Enum.Parse<OcrFeedbackVerdict>(row[9],true),CorrectedValue=Dec(row[10]),QualityStatus=row[11],FailureReason=row[12],DecisionSource=row[13],RawImagePath=row[14],ProcessedImagePath=row[15]};var key=Key(item.MeasurementId,item.FieldId);if(!_latest.TryGetValue(key,out var old)||item.FeedbackAt>=old.FeedbackAt)_latest[key]=item;}catch{/* malformed audit row is ignored */}}}}
    public bool TryValidateCorrection(string text, NumericFieldDefinition definition,out decimal value,out string error)
    { if(!NumericFieldParser.TryParse(text,definition,out value,out error))return false;
      if(definition.MinimumValue.HasValue&&value<definition.MinimumValue||definition.MaximumValue.HasValue&&value>definition.MaximumValue){error=$"Değer {definition.MinimumValue}–{definition.MaximumValue} aralığında olmalı.";return false;} return true; }
    public OcrUserFeedback Save(OcrFeedbackSnapshot snapshot,OcrFeedbackVerdict verdict,decimal? corrected,
        string machine,string profile,string? ring,string? ringSession,string displayName)
    { OcrUserFeedback saved; lock(_sync){var prior=LatestRevision(snapshot.MeasurementId,snapshot.FieldId);var id=Guid.NewGuid().ToString("N");
      var folder=Path.Combine(_root,Safe(machine),Safe(snapshot.FieldId),snapshot.CapturedAt.ToString("yyyyMMdd"));Directory.CreateDirectory(folder);
      var raw=Path.Combine(folder,id+"_raw.png");var processed=Path.Combine(folder,id+"_processed.png");snapshot.RawCrop.Save(raw);snapshot.ProcessedCrop.Save(processed);
      var feedback=new OcrUserFeedback{FeedbackId=id,MeasurementId=snapshot.MeasurementId,MachineId=machine,MachineProfile=profile,RingNo=ring,RingSessionId=ringSession,
        FieldId=snapshot.FieldId,FieldDisplayName=displayName,CapturedAt=snapshot.CapturedAt,FeedbackAt=DateTime.Now,RawOcrText=snapshot.RawOcrText,
        ObservedValue=snapshot.ObservedValue,TrustedValueAtThatTime=snapshot.TrustedValue,Verdict=verdict,CorrectedValue=corrected,
        QualityStatus=snapshot.QualityStatus,FailureReason=snapshot.FailureReason,DecisionSource=snapshot.DecisionSource,Pipeline=snapshot.Pipeline,
        RegionX=snapshot.Region.X,RegionY=snapshot.Region.Y,RegionWidth=snapshot.Region.Width,RegionHeight=snapshot.Region.Height,
        RawImagePath=raw,ProcessedImagePath=processed,RevisesFeedbackId=prior?.FeedbackId};
      File.WriteAllText(Path.Combine(folder,id+".json"),JsonSerializer.Serialize(feedback,new JsonSerializerOptions{WriteIndented=true}),new UTF8Encoding(false));Append(feedback);_latest[Key(snapshot.MeasurementId,snapshot.FieldId)]=feedback;saved=feedback;} FeedbackChanged?.Invoke(); return saved;}
    public OcrUserFeedback Remove(OcrFeedbackSnapshot snapshot,string machine,string profile,string? ring,string? session,string display)
        => Save(snapshot,OcrFeedbackVerdict.Removed,null,machine,profile,ring,session,display);
    private void Append(OcrUserFeedback f){Directory.CreateDirectory(Path.GetDirectoryName(_csv)!);var exists=File.Exists(_csv);using var w=new StreamWriter(_csv,true,new UTF8Encoding(false));if(!exists)w.WriteLine("FeedbackId,MeasurementId,FeedbackAt,CapturedAt,Machine,Ring,Field,ObservedValue,TrustedValue,Verdict,CorrectedValue,QualityStatus,FailureReason,DecisionSource,RawImagePath,ProcessedImagePath");w.WriteLine(string.Join(',',Q(f.FeedbackId),Q(f.MeasurementId),Q(f.FeedbackAt.ToString("O")),Q(f.CapturedAt.ToString("O")),Q(f.MachineId),Q(f.RingNo??""),Q(f.FieldId),N(f.ObservedValue),N(f.TrustedValueAtThatTime),f.Verdict,N(f.CorrectedValue),Q(f.QualityStatus),Q(f.FailureReason),Q(f.DecisionSource),Q(f.RawImagePath),Q(f.ProcessedImagePath)));}
    private Dictionary<string,OcrUserFeedback> LoadJsonMetadata(){var result=new Dictionary<string,OcrUserFeedback>(StringComparer.OrdinalIgnoreCase);if(!Directory.Exists(_root))return result;foreach(var path in Directory.EnumerateFiles(_root,"*.json",SearchOption.AllDirectories))try{var item=JsonSerializer.Deserialize<OcrUserFeedback>(File.ReadAllText(path));if(item is not null)result[item.FeedbackId]=item;}catch{}return result;}
    private static IEnumerable<List<string>> ReadCsv(string path){using var reader=new StreamReader(path,true);while(reader.ReadLine()is{ } line){var cells=new List<string>();var value=new StringBuilder();var quoted=false;for(var i=0;i<line.Length;i++){var c=line[i];if(c=='\"'){if(quoted&&i+1<line.Length&&line[i+1]=='\"'){value.Append('\"');i++;}else quoted=!quoted;}else if(c==','&&!quoted){cells.Add(value.ToString());value.Clear();}else value.Append(c);}cells.Add(value.ToString());yield return cells;}}
    private static decimal? Dec(string s)=>decimal.TryParse(s,NumberStyles.Number,CultureInfo.InvariantCulture,out var v)?v:null;private static string? Empty(string s)=>string.IsNullOrWhiteSpace(s)?null:s;
    private static string Safe(string s)=>string.Concat(s.Select(c=>Path.GetInvalidFileNameChars().Contains(c)?'_':c));private static string N(decimal? v)=>v?.ToString(CultureInfo.InvariantCulture)??"";private static string Q(string s)=>'"'+s.Replace("\"","\"\"")+'"';
}
