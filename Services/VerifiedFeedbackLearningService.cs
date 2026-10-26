using System.Globalization;
using System.Text;
using System.Text.Json;
using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

/// <summary>Image-only, machine/field isolated feedback memory. OCR text/value is never used as a lookup key.</summary>
public sealed class VerifiedFeedbackLearningService : IDisposable
{
    private sealed record Entry(OcrUserFeedback Feedback, decimal? Expected, bool Negative, ImageSignature Signature);
    private sealed record ImageSignature(float[] Gray, bool[] Ink, double Aspect, double InkRatio,
        double BoundsX, double BoundsY, double BoundsW, double BoundsH, int Components, double DecimalX);
    private readonly OcrProfile _profile; private readonly string _diagnostics; private readonly object _gate = new();
    private List<Entry> _entries = [];
    private Dictionary<string,Entry[]> _index=new(StringComparer.OrdinalIgnoreCase);

    public VerifiedFeedbackLearningService(OcrProfile profile, string? diagnostics = null)
    { _profile = profile; _diagnostics = diagnostics ?? Path.Combine(AppContext.BaseDirectory, "Diagnostics"); Reload(); OcrUserFeedbackService.FeedbackChanged += Reload; }

    public int Count { get { lock (_gate) return _entries.Count; } }
    public double BenchmarkAverageMatchMilliseconds(int iterations=50)
    {Entry? sample;lock(_gate)sample=_entries.FirstOrDefault();if(sample is null)return 0;using var image=new Bitmap(sample.Feedback.RawImagePath);var watch=System.Diagnostics.Stopwatch.StartNew();for(var i=0;i<iterations;i++)Match(sample.Feedback.MachineId,sample.Feedback.FieldId,image);watch.Stop();return watch.Elapsed.TotalMilliseconds/Math.Max(1,iterations);}
    public void Reload()
    {
        var root=Path.Combine(_diagnostics,"UserFeedback"); var all=new List<OcrUserFeedback>();
        if(Directory.Exists(root)) foreach(var path in Directory.EnumerateFiles(root,"*.json",SearchOption.AllDirectories))
            try { var f=JsonSerializer.Deserialize<OcrUserFeedback>(File.ReadAllText(path)); if(f is not null) all.Add(f); } catch { }
        var loaded=new List<Entry>();
        foreach(var f in OcrFeedbackReplayService.SelectLatestActive(all))
        {
            var expected=f.Verdict switch { OcrFeedbackVerdict.Correct=>f.ObservedValue, OcrFeedbackVerdict.Corrected=>f.CorrectedValue, _=>null };
            var negative=f.Verdict==OcrFeedbackVerdict.Incorrect && !f.CorrectedValue.HasValue;
            if((!expected.HasValue&&!negative)||!File.Exists(f.RawImagePath)) continue;
            try { using var b=new Bitmap(f.RawImagePath); loaded.Add(new(f,expected,negative,Signature(b))); } catch { }
        }
        lock(_gate){_entries=loaded;_index=loaded.GroupBy(x=>IndexKey(x.Feedback.MachineId,x.Feedback.FieldId),StringComparer.OrdinalIgnoreCase).ToDictionary(g=>g.Key,g=>g.ToArray(),StringComparer.OrdinalIgnoreCase);}
    }

    public VerifiedFeedbackMatch Match(string machineId,string fieldId,Bitmap image,string? excludeFeedbackId=null)
    {
        if(!_profile.EnableVerifiedFeedbackLearning) return VerifiedFeedbackMatch.None("Feedback learning disabled");
        Entry[] pool; lock(_gate) pool=(_index.GetValueOrDefault(IndexKey(machineId,fieldId))??[]).Where(e=>!string.Equals(e.Feedback.FeedbackId,excludeFeedbackId,StringComparison.OrdinalIgnoreCase)).ToArray();
        if(pool.Length==0) return VerifiedFeedbackMatch.None("No verified feedback examples for machine/field");
        var definition=_profile.FindField(fieldId);var candidateThreshold=definition?.FeedbackCandidateSimilarityOverride??_profile.FeedbackCandidateSimilarity;
        var verifiedThreshold=definition?.FeedbackVerifiedSimilarityOverride??_profile.FeedbackVerifiedSimilarity;
        var authoritativeThreshold=definition?.FeedbackAuthoritativeSimilarityOverride??_profile.FeedbackAuthoritativeSimilarity;
        var query=Signature(image);var shortlist=pool.OrderByDescending(e=>QuickSimilarity(query,e.Signature)).Take(24).ToArray();
        var scored=shortlist.Select(e=>(Entry:e,Score:Similarity(query,e.Signature))).OrderByDescending(x=>x.Score).ToArray();
        var top=scored[0]; if(top.Score<candidateThreshold) return VerifiedFeedbackMatch.None("Image similarity below candidate threshold");
        var neighborhood=scored.Where(x=>x.Score>=candidateThreshold).ToArray();
        var positive=neighborhood.Where(x=>x.Entry.Expected.HasValue).GroupBy(x=>x.Entry.Expected!.Value)
            .Select(g=>new {Value=g.Key,Items=Independent(g).ToArray(),Best=g.Max(x=>x.Score),Average=g.Average(x=>x.Score)})
            .Where(g=>g.Items.Length>0)
            .OrderByDescending(g=>g.Best).ThenByDescending(g=>g.Items.Length).ToArray();
        if(positive.Length==0) return new(null,top.Score,top.Score,0,neighborhood.Length,VerifiedFeedbackLevel.Conflict,"Only negative feedback matched",top.Entry.Feedback.FeedbackId);
        var winner=positive[0];
        // Torque crops share almost all of their background and glyph pixels. Values such as
        // 1.191, 1.207, 1.245, 1.273 and 1.286 can therefore score above 99% against each
        // other. That is not enough evidence to replace a fresh OCR result. If two different
        // corrected torque values are visually this close, keep feedback diagnostic-only.
        // Exact/repeated samples of the same value still form one group and remain usable.
        if (fieldId.Equals(BuiltInFieldIds.StableTorque, StringComparison.OrdinalIgnoreCase)
            && positive.Length > 1
            && positive[1].Best >= winner.Best - .015)
            return new(null, winner.Best, 0, winner.Items.Length,
                positive.Skip(1).Sum(x => x.Items.Length), VerifiedFeedbackLevel.Conflict,
                "Torque feedback images are visually ambiguous between different corrected values; live override blocked",
                winner.Items[0].Entry.Feedback.FeedbackId);
        // Normal numeric croplar %95-%98 benzer olabilir. Conflict yalnız neredeyse aynı
        // parmak izi ve aynı foreground/ondalık yapısı birlikte varsa sayılır.
        var winnerSignature=winner.Items.OrderByDescending(x=>x.Score).First().Entry.Signature;
        var positiveConflicts=positive.Skip(1).SelectMany(x=>x.Items)
            .Count(x=>IsStrictFingerprintConflict(winnerSignature,x.Entry.Signature,x.Score));
        var negativeConflicts=Independent(neighborhood.Where(x=>x.Entry.Negative&&IsStrictFingerprintConflict(winnerSignature,x.Entry.Signature,x.Score))).Count();
        var conflicts=positiveConflicts+(negativeConflicts>=2?negativeConflicts:0);
        var confidence=Math.Clamp(winner.Best*.8+Math.Min(.2,winner.Items.Length*.05)-conflicts*.1,0,1);
        var level=VerifiedFeedbackLevel.Candidate;
        if(conflicts>_profile.FeedbackMaximumConflictCount) level=VerifiedFeedbackLevel.Conflict;
        else if(winner.Best>=authoritativeThreshold && winner.Items.Length>=_profile.FeedbackAuthoritativeMinimumExamples)
            level=VerifiedFeedbackLevel.Authoritative;
        else if(winner.Best>=verifiedThreshold && winner.Items.Length>=_profile.FeedbackVerifiedMinimumExamples)
            level=VerifiedFeedbackLevel.Verified;
        return new(winner.Value,winner.Best,confidence,winner.Items.Length,conflicts,level,
            level==VerifiedFeedbackLevel.Conflict?"Conflicting or negative feedback blocks override":$"{level}: {winner.Items.Length} consistent image example(s)",winner.Items[0].Entry.Feedback.FeedbackId);
    }

    private static IEnumerable<(Entry Entry,double Score)> Independent(IEnumerable<(Entry Entry,double Score)> source) => source
        .GroupBy(x=>(x.Entry.Feedback.MeasurementId,x.Entry.Feedback.CapturedAt))
        .Select(g=>g.OrderByDescending(x=>x.Score).First());
    private static string IndexKey(string machine,string field)=>machine+"|"+field;
    private static double QuickSimilarity(ImageSignature a,ImageSignature b){var step=Math.Max(1,a.Ink.Length/128);var same=0;var total=0;for(var i=0;i<a.Ink.Length;i+=step){if(a.Ink[i]==b.Ink[i])same++;total++;}return total==0?0:(double)same/total;}
    private static bool IsStrictFingerprintConflict(ImageSignature a,ImageSignature b,double similarity) =>
        similarity>=.995 && QuickSimilarity(a,b)>=.995 && a.Components==b.Components
        && Math.Abs(a.DecimalX-b.DecimalX)<=.01 && Math.Abs(a.InkRatio-b.InkRatio)<=.01
        && Math.Abs(a.BoundsW-b.BoundsW)<=.015 && Math.Abs(a.BoundsH-b.BoundsH)<=.015;

    public bool CanOverride(VerifiedFeedbackMatch match,NumericFieldDefinition field,decimal? trusted,out string reason)
    {
        reason=match.Reason; if(!field.FeedbackLiveOverrideEnabled){reason="Field calibration did not approve live feedback override";return false;}
        if(match.Level is not (VerifiedFeedbackLevel.Verified or VerifiedFeedbackLevel.Authoritative)||!_profile.EnableAuthoritativeFeedbackOverride||!match.CandidateValue.HasValue) return false;
        // A three-decimal torque crop changes by only a handful of foreground pixels. A
        // merely "very similar" feedback image must not turn 1.245/1.286/1.273/1.207 into
        // an older 1.191 (or 0.000). Only an effectively identical crop may auto-override;
        // all other matches stay available for diagnostics and manual feedback.
        if (field.FieldId.Equals(BuiltInFieldIds.StableTorque, StringComparison.OrdinalIgnoreCase)
            && match.Similarity < .9999)
        {
            reason = "Torque feedback match is not pixel-identical; live override blocked";
            return false;
        }
        var value=match.CandidateValue.Value;
        if(field.MinimumValue.HasValue&&value<field.MinimumValue||field.MaximumValue.HasValue&&value>field.MaximumValue){reason="Feedback value outside field range";return false;}
        if(field.DecimalPlaces>=0&&decimal.Round(value,field.DecimalPlaces)!=value){reason="Feedback decimal format incompatible with field";return false;}
        if(trusted.HasValue&&field.MaximumChangePerReading>0&&Math.Abs(value-trusted.Value)>field.MaximumChangePerReading){reason="Feedback conflicts with temporal/physical limit";return false;}
        return true;
    }

    public void Audit(DateTime timestamp,string measurementId,string machine,string field,decimal? original,VerifiedFeedbackMatch match,decimal? final,string decision)
    {
        try { var path=Path.Combine(_diagnostics,"FeedbackLiveDecisions.csv"); Directory.CreateDirectory(_diagnostics); CsvLogRotation.RotateIfNeeded(path); var exists=File.Exists(path);
            using var w=new StreamWriter(path,true,new UTF8Encoding(true)); if(!exists)w.WriteLine("Timestamp,MeasurementId,MachineId,FieldId,OriginalOcrValue,FeedbackCandidate,SimilarityPercent,ConfidencePercent,SupportCount,ConflictCount,DecisionLevel,FinalValue,DecisionReason");
            string Q(object? x)=>$"\"{Convert.ToString(x,CultureInfo.InvariantCulture)?.Replace("\"","\"\"")}\"";
            w.WriteLine(string.Join(",",Q(timestamp.ToString("O")),Q(measurementId),Q(machine),Q(field),Q(original),Q(match.CandidateValue),Q(match.Similarity*100),Q(match.Confidence*100),Q(match.SupportingExamples),Q(match.ConflictCount),Q(match.Level),Q(final),Q(decision)));
        } catch { }
    }

    public FeedbackLearningReplaySummary EvaluateLeaveOneOut()
    {
        Entry[] entries; lock(_gate) entries=_entries.Where(x=>x.Expected.HasValue).ToArray();
        var rows=new List<string>();var baseline=0;var assisted=0;var fixedCount=0;var broken=0;
        foreach(var entry in entries)
        {
            using var image=new Bitmap(entry.Feedback.RawImagePath);var match=Match(entry.Feedback.MachineId,entry.Feedback.FieldId,image,entry.Feedback.FeedbackId);
            var before=entry.Feedback.ObservedValue;var after=match.Level==VerifiedFeedbackLevel.Authoritative&&match.CandidateValue.HasValue?match.CandidateValue:before;
            var beforeOk=before==entry.Expected;var afterOk=after==entry.Expected;if(beforeOk)baseline++;if(afterOk)assisted++;if(!beforeOk&&afterOk)fixedCount++;if(beforeOk&&!afterOk)broken++;
            rows.Add(string.Join(',',entry.Feedback.FeedbackId,entry.Feedback.MachineId,entry.Feedback.FieldId,before?.ToString(CultureInfo.InvariantCulture)??"",entry.Expected?.ToString(CultureInfo.InvariantCulture)??"",after?.ToString(CultureInfo.InvariantCulture)??"",match.Level,match.Similarity.ToString("0.0000",CultureInfo.InvariantCulture),match.SupportingExamples,match.ConflictCount,beforeOk,afterOk));
        }
        var recommended=assisted>=baseline&&broken==0;var path=Path.Combine(_diagnostics,"FeedbackAssistedAccuracySummary.csv");Directory.CreateDirectory(_diagnostics);
        File.WriteAllLines(path,new[]{"FeedbackId,MachineId,FieldId,BaselineValue,ExpectedValue,AssistedValue,Level,Similarity,Support,Conflicts,BaselineCorrect,AssistedCorrect"}.Concat(rows),new UTF8Encoding(true));
        return new(entries.Length,baseline,assisted,fixedCount,broken,recommended);
    }

    public FeedbackCalibrationOutcome CalibrateAndWriteReports()
    {
        Entry[] all;lock(_gate)all=_entries.ToArray();var positives=all.Where(x=>x.Expected.HasValue).ToArray();
        var thresholds=new[]{.90,.92,.94,.95,.96,.97};var calibration=new List<string>();
        foreach(var sample in positives)
        {
            var scored=Scores(sample,all);var same=scored.Where(x=>x.Entry.Expected==sample.Expected).ToArray();var different=scored.Where(x=>x.Entry.Expected.HasValue&&x.Entry.Expected!=sample.Expected).ToArray();
            var bestSame=same.Select(x=>x.Score).DefaultIfEmpty(0).Max();var bestDifferent=different.Select(x=>x.Score).DefaultIfEmpty(0).Max();
            var support=thresholds.Select(t=>Independent(same.Where(x=>x.Score>=t)).Count()).ToArray();
            using var currentImage=new Bitmap(sample.Feedback.RawImagePath);var current=Match(sample.Feedback.MachineId,sample.Feedback.FieldId,currentImage,sample.Feedback.FeedbackId);
            var blocked=current.Level switch{VerifiedFeedbackLevel.None=>"Similarity",VerifiedFeedbackLevel.Candidate=>"MinimumSupport",VerifiedFeedbackLevel.Conflict=>"Conflict",_=>"None"};
            var conflicts=Independent(different.Where(x=>x.Score>=.90)).Count();
            var recover=thresholds.Select((t,i)=>sample.Feedback.ObservedValue!=sample.Expected&&support[i]>=2&&bestDifferent<bestSame).ToArray();
            calibration.Add(string.Join(',',Q(sample.Feedback.FeedbackId),Q(sample.Feedback.FieldId),N(sample.Expected),F(scored.FirstOrDefault().Score),F(bestSame),F(bestDifferent),string.Join(',',support),conflicts,Q(blocked),string.Join(',',recover.Select(x=>x?"True":"False"))));
        }
        Write(Path.Combine(_diagnostics,"FeedbackLearningCalibration.csv"),"FeedbackId,FieldId,ExpectedValue,BestSimilarity,BestSameValueSimilarity,BestDifferentValueSimilarity,SupportingCount90,SupportingCount92,SupportingCount94,SupportingCount95,SupportingCount96,SupportingCount97,ConflictCount,CandidateBlockedBy,WouldRecoverAt90,WouldRecoverAt92,WouldRecoverAt94,WouldRecoverAt95,WouldRecoverAt96,WouldRecoverAt97",calibration);

        var distribution=new List<string>();foreach(var field in positives.GroupBy(x=>x.Feedback.FieldId,StringComparer.OrdinalIgnoreCase))
        {var same=new List<double>();var diff=new List<double>();var arr=field.ToArray();for(var i=0;i<arr.Length;i++)for(var j=i+1;j<arr.Length;j++){var s=Similarity(arr[i].Signature,arr[j].Signature);if(arr[i].Expected==arr[j].Expected)same.Add(s);else diff.Add(s);}distribution.Add(string.Join(',',Q(field.Key),arr.Length,same.Count,diff.Count,F(P(same,.50)),F(P(same,.75)),F(P(same,.90)),F(P(same,.95)),F(P(diff,.50)),F(P(diff,.90)),F(P(diff,.95)),F(diff.DefaultIfEmpty(0).Max())));}
        Write(Path.Combine(_diagnostics,"FeedbackSimilarityDistribution.csv"),"FieldId,SampleCount,SameValuePairCount,DifferentValuePairCount,SameValueSimilarityP50,SameValueSimilarityP75,SameValueSimilarityP90,SameValueSimilarityP95,DifferentValueSimilarityP50,DifferentValueSimilarityP90,DifferentValueSimilarityP95,DifferentValueSimilarityMax",distribution);

        var evaluations=new List<(string Field,double T,int S,int Base,int Assisted,int Recovered,int Broken,int False,double Precision,double Recall,int Total)>();
        void EvaluateSet(string field,Entry[] samples){foreach(var threshold in thresholds)foreach(var minimumSupport in new[]{2,3}){var baseline=0;var assisted=0;var recovered=0;var broken=0;var falseAuth=0;var corrected=0;foreach(var sample in samples){var before=sample.Feedback.ObservedValue;var expected=sample.Expected;var beforeOk=before==expected;if(beforeOk)baseline++;else corrected++;var prediction=Predict(sample,all,threshold,minimumSupport);var after=prediction??before;var afterOk=after==expected;if(afterOk)assisted++;if(!beforeOk&&afterOk)recovered++;if(beforeOk&&!afterOk){broken++;falseAuth++;}else if(!beforeOk&&prediction.HasValue&&prediction!=expected)falseAuth++;}var precision=recovered+falseAuth==0?1:(double)recovered/(recovered+falseAuth);var recall=corrected==0?0:(double)recovered/corrected;evaluations.Add((field,threshold,minimumSupport,baseline,assisted,recovered,broken,falseAuth,precision,recall,samples.Length));}}
        EvaluateSet("ALL",positives);foreach(var field in positives.GroupBy(x=>x.Feedback.FieldId,StringComparer.OrdinalIgnoreCase))EvaluateSet(field.Key,field.ToArray());
        Write(Path.Combine(_diagnostics,"FeedbackThresholdEvaluation.csv"),"FieldId,Similarity,MinimumSupport,BaselineCorrect,FeedbackAssistedCorrect,RecoveredCorrected,BrokenCorrect,FalseAuthoritative,FinalAccuracy,Precision,Recall",evaluations.Select(x=>string.Join(',',Q(x.Field),F(x.T),x.S,x.Base,x.Assisted,x.Recovered,x.Broken,x.False,F((double)x.Assisted/Math.Max(1,x.Total)),F(x.Precision),F(x.Recall))));
        var chosen=evaluations.Where(x=>x.Field!="ALL"&&x.False==0&&x.Broken==0&&x.Recovered>0).GroupBy(x=>x.Field,StringComparer.OrdinalIgnoreCase).Select(g=>g.OrderByDescending(x=>x.Recovered).ThenByDescending(x=>x.Precision).ThenBy(x=>x.T).ThenBy(x=>x.S).First()).ToArray();
        Write(Path.Combine(_diagnostics,"FeedbackSelectedCalibration.csv"),"FieldId,Similarity,MinimumSupport,Recovered,Broken,FalseAuthoritative,LiveOverrideEnabled",chosen.Select(x=>string.Join(',',Q(x.Field),F(x.T),x.S,x.Recovered,x.Broken,x.False,"True")));
        var overallBaseline=positives.Count(x=>x.Feedback.ObservedValue==x.Expected);var assistedTotal=overallBaseline;var recoveredTotal=0;foreach(var choice in chosen){assistedTotal+=choice.Recovered;recoveredTotal+=choice.Recovered;}
        var representative=chosen.OrderByDescending(x=>x.Recovered).FirstOrDefault();var allowed=chosen.Length>0&&assistedTotal>overallBaseline;
        return new(positives.Length,overallBaseline,assistedTotal,recoveredTotal,0,0,representative.T,representative.S,(double)assistedTotal/Math.Max(1,positives.Length),1,allowed);

        (Entry Entry,double Score)[] Scores(Entry sample,Entry[] pool)=>pool.Where(x=>!ReferenceEquals(x,sample)&&x.Feedback.MachineId.Equals(sample.Feedback.MachineId,StringComparison.OrdinalIgnoreCase)&&x.Feedback.FieldId.Equals(sample.Feedback.FieldId,StringComparison.OrdinalIgnoreCase)).Select(x=>(x,Similarity(sample.Signature,x.Signature))).OrderByDescending(x=>x.Item2).ToArray();
        decimal? Predict(Entry sample,Entry[] pool,double threshold,int minimumSupport){var scored=Scores(sample,pool).Where(x=>x.Score>=threshold).ToArray();var groups=scored.Where(x=>x.Entry.Expected.HasValue).GroupBy(x=>x.Entry.Expected!.Value).Select(g=>new{Value=g.Key,Items=Independent(g).ToArray(),Best=g.Max(x=>x.Score)}).Where(g=>g.Items.Length>=minimumSupport).OrderByDescending(g=>g.Best).ThenByDescending(g=>g.Items.Length).ToArray();if(groups.Length==0)return null;if(groups.Length>1&&groups[1].Best>=groups[0].Best-.005)return null;var negatives=Independent(scored.Where(x=>x.Entry.Negative)).Count();return negatives>=2?null:groups[0].Value;}
        static double P(List<double> v,double p){if(v.Count==0)return 0;v.Sort();return v[(int)Math.Round((v.Count-1)*p)];}static string F(double v)=>v.ToString("0.0000",CultureInfo.InvariantCulture);static string N(decimal? v)=>v?.ToString(CultureInfo.InvariantCulture)??"";static string Q(string v)=>'"'+v.Replace("\"","\"\"")+'"';
        static void Write(string path,string header,IEnumerable<string> rows){Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.WriteAllLines(path,new[]{header}.Concat(rows),new UTF8Encoding(true));}
    }

    private static double Similarity(ImageSignature a,ImageSignature b)
    { var diff=0d; for(var i=0;i<a.Gray.Length;i++)diff+=Math.Abs(a.Gray[i]-b.Gray[i]); var pixel=1-diff/a.Gray.Length;
      var inter=0;var union=0;for(var i=0;i<a.Ink.Length;i++){if(a.Ink[i]&&b.Ink[i])inter++;if(a.Ink[i]||b.Ink[i])union++;}var iou=union==0?1:(double)inter/union;
      var aspect=1-Math.Min(1,Math.Abs(a.Aspect-b.Aspect)/Math.Max(.01,Math.Max(a.Aspect,b.Aspect)));
      var geometry=1-Math.Min(1,(Math.Abs(a.BoundsX-b.BoundsX)+Math.Abs(a.BoundsY-b.BoundsY)+Math.Abs(a.BoundsW-b.BoundsW)+Math.Abs(a.BoundsH-b.BoundsH))/4);
      var structure=a.Components==b.Components?1:Math.Max(0,1-Math.Abs(a.Components-b.Components)/5d); var decimalScore=(a.DecimalX<0&&b.DecimalX<0)?1:1-Math.Min(1,Math.Abs(a.DecimalX-b.DecimalX));
      return Math.Clamp(pixel*.15+iou*.65+geometry*.05+aspect*.03+structure*.07+decimalScore*.05,0,1); }

    private static ImageSignature Signature(Bitmap source)
    {
        const int w=128,h=48; using var normalized=Normalize(source,w,h);var resized=normalized;
        var gray=new float[w*h];double mean=0;for(var y=0;y<h;y++)for(var x=0;x<w;x++){var c=resized.GetPixel(x,y);var v=(c.R*299+c.G*587+c.B*114)/255000f;gray[y*w+x]=v;mean+=v;}mean/=gray.Length;
        var ink=new bool[gray.Length];int minX=w,minY=h,maxX=-1,maxY=-1,count=0;for(var y=0;y<h;y++)for(var x=0;x<w;x++){var on=gray[y*w+x]<mean*.82;ink[y*w+x]=on;if(on){count++;minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}}
        var components=Components(ink,w,h);var decimalX=-1d; var small=components.Boxes.Where(b=>b.Width<=Math.Max(2,w/10)&&b.Height<=Math.Max(2,h/3)&&b.Y>h/2).OrderByDescending(b=>b.Y).FirstOrDefault();if(small.Width>0)decimalX=(small.X+small.Width/2d)/w;
        return new(gray,ink,(double)source.Width/Math.Max(1,source.Height),(double)count/ink.Length,minX==w?0:(double)minX/w,minY==h?0:(double)minY/h,maxX<0?0:(double)(maxX-minX+1)/w,maxY<0?0:(double)(maxY-minY+1)/h,components.Count,decimalX);
    }
    private static Bitmap Normalize(Bitmap source,int targetW,int targetH)
    {
        var min=255;var max=0;var lum=new byte[source.Width*source.Height];for(var y=0;y<source.Height;y++)for(var x=0;x<source.Width;x++){var c=source.GetPixel(x,y);var v=(byte)((c.R*299+c.G*587+c.B*114)/1000);lum[y*source.Width+x]=v;min=Math.Min(min,v);max=Math.Max(max,v);}var threshold=min+(max-min)*.48;
        var mask=lum.Select(v=>v<threshold).ToArray();var components=Components(mask,source.Width,source.Height).Boxes;
        var useful=components.Where(b=>b.Width<source.Width*.88&&b.Height<source.Height*.92&&!(b.Width>source.Width*.55&&b.Height<=Math.Max(2,source.Height*.15))).ToArray();
        var bounds=useful.Length==0?new Rectangle(0,0,source.Width,source.Height):Rectangle.FromLTRB(useful.Min(b=>b.Left),useful.Min(b=>b.Top),useful.Max(b=>b.Right),useful.Max(b=>b.Bottom));bounds.Inflate(2,2);bounds=Rectangle.Intersect(bounds,new Rectangle(0,0,source.Width,source.Height));
        var output=new Bitmap(targetW,targetH);using var g=Graphics.FromImage(output);g.Clear(Color.White);g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;var scale=Math.Min((targetW-4d)/Math.Max(1,bounds.Width),(targetH-4d)/Math.Max(1,bounds.Height));var dw=Math.Max(1,(int)Math.Round(bounds.Width*scale));var dh=Math.Max(1,(int)Math.Round(bounds.Height*scale));var dest=new Rectangle((targetW-dw)/2,(targetH-dh)/2,dw,dh);g.DrawImage(source,dest,bounds,GraphicsUnit.Pixel);return output;
    }
    private static (int Count,List<Rectangle> Boxes) Components(bool[] p,int w,int h){var seen=new bool[p.Length];var boxes=new List<Rectangle>();var q=new Queue<int>();for(var i=0;i<p.Length;i++){if(!p[i]||seen[i])continue;seen[i]=true;q.Enqueue(i);int minX=w,minY=h,maxX=0,maxY=0,n=0;while(q.Count>0){var v=q.Dequeue();var x=v%w;var y=v/w;n++;minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);foreach(var d in new[]{-1,1,-w,w}){var z=v+d;if(z<0||z>=p.Length||seen[z]||!p[z]||Math.Abs(z%w-x)>1)continue;seen[z]=true;q.Enqueue(z);}}if(n>=2)boxes.Add(Rectangle.FromLTRB(minX,minY,maxX+1,maxY+1));}return(boxes.Count,boxes);}
    public void Dispose(){ OcrUserFeedbackService.FeedbackChanged -= Reload; }
}
