using System.Globalization;
using TBMWatchdog.Models;
using TBMWatchdog.Services;

namespace TBMWatchdog.Forms;

public sealed class OcrFeedbackForm : Form
{
    private readonly OcrFeedbackSnapshot _snapshot; private readonly NumericFieldDefinition _definition;
    private readonly OcrUserFeedbackService _service; private readonly string _machine,_profile,_display; private readonly string? _ring,_session;
    private readonly TextBox _correction=new(){Width=140}; private readonly Label _status=new(){AutoSize=true,ForeColor=Color.FromArgb(251,191,36)};
    public OcrUserFeedback? SavedFeedback { get; private set; }
    internal string CorrectionTextForTest=>_correction.Text; internal string CurrentStatusForTest=>_status.Text;

    public OcrFeedbackForm(OcrFeedbackSnapshot snapshot,NumericFieldDefinition definition,OcrUserFeedbackService service,string machine,string profile,string? ring,string? session)
    {
        _snapshot=snapshot;_definition=definition;_service=service;_machine=machine;_profile=profile;_ring=ring;_session=session;_display=definition.DisplayName;
        Text="OCR Geri Bildirimi";Size=new Size(850,650);StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(15,23,42);ForeColor=Color.White;AutoScaleMode=AutoScaleMode.Dpi;
        var current=service.Latest(snapshot.MeasurementId,snapshot.FieldId);
        if(current?.CorrectedValue is { } corrected)_correction.Text=corrected.ToString(CultureInfo.InvariantCulture);
        _status.Text=current?.Verdict switch{OcrFeedbackVerdict.Correct=>"Mevcut etiket: Doğru",OcrFeedbackVerdict.Incorrect=>"Mevcut etiket: Yanlış",OcrFeedbackVerdict.Corrected=>$"Mevcut etiket: Düzeltildi ({current.CorrectedValue?.ToString(CultureInfo.InvariantCulture)})",_=>""};
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=4,Padding=new Padding(12)};root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
        var info=new Label{Dock=DockStyle.Fill,AutoSize=false,Text=$"Alan: {_display}\nOCR sonucu: {snapshot.ObservedValue?.ToString(CultureInfo.InvariantCulture)??"—"} {definition.Unit}\nSon güvenilir değer: {snapshot.TrustedValue?.ToString(CultureInfo.InvariantCulture)??"—"} {definition.Unit}\nOkuma zamanı: {snapshot.CapturedAt:HH:mm:ss}\nKalite: {snapshot.QualityStatus}\nNeden: {snapshot.FailureReason}\n\nRaw OCR: {snapshot.RawOcrText}\nFull-line: {snapshot.FullLineResult?.ToString(CultureInfo.InvariantCulture)??"—"}\nCharacter: {snapshot.CharacterResult?.ToString(CultureInfo.InvariantCulture)??"—"}\nTemplate: {snapshot.TemplateResult}\nDecisionSource: {snapshot.DecisionSource}"};root.Controls.Add(info,0,0);root.SetColumnSpan(info,2);
        root.Controls.Add(Picture("ORİJİNAL OCR CROP",snapshot.RawCrop),0,1);root.Controls.Add(Picture("İŞLENMİŞ / BINARY OCR CROP",snapshot.ProcessedCrop),1,1);
        var correctionPanel=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true};correctionPanel.Controls.Add(new Label{Text="Doğru değer:",AutoSize=true,Padding=new Padding(0,7,0,0)});correctionPanel.Controls.Add(_correction);correctionPanel.Controls.Add(_status);root.Controls.Add(correctionPanel,0,2);root.SetColumnSpan(correctionPanel,2);
        var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true};buttons.Controls.Add(Button("✓ OCR Doğru",(_,_)=>Save(OcrFeedbackVerdict.Correct,null)));buttons.Controls.Add(Button("✕ OCR Yanlış",(_,_)=>Save(OcrFeedbackVerdict.Incorrect,null)));buttons.Controls.Add(Button("✎ Düzelt ve Kaydet",(_,_)=>Correct()));if(current is not null)buttons.Controls.Add(Button("Geri bildirimi kaldır",(_,_)=>Save(OcrFeedbackVerdict.Removed,null)));root.Controls.Add(buttons,0,3);root.SetColumnSpan(buttons,2);Controls.Add(root);
    }
    private Control Picture(string title,Bitmap image){var box=new PictureBox{Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,Image=new Bitmap(image)};var group=new GroupBox{Text=title,Dock=DockStyle.Fill,ForeColor=Color.White};group.Controls.Add(box);group.Disposed+=(_,_)=>box.Image?.Dispose();return group;}
    private Button Button(string text,EventHandler click){var b=new Button{Text=text,AutoSize=true,Padding=new Padding(8)};b.Click+=click;return b;}
    private void Correct(){if(!_service.TryValidateCorrection(_correction.Text,_definition,out var value,out var error)){_status.Text=error;return;}Save(OcrFeedbackVerdict.Corrected,value);}
    private void Save(OcrFeedbackVerdict verdict,decimal? corrected){SavedFeedback=_service.Save(_snapshot,verdict,corrected,_machine,_profile,_ring,_session,_display);DialogResult=DialogResult.OK;Close();}
}
