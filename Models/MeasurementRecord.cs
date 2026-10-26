namespace TBMWatchdog.Models;

public sealed record MeasurementRecord
{
    public string MeasurementId { get; init; } = Guid.NewGuid().ToString("N");
    private decimal? _nominal; private decimal? _scale1; private decimal? _scale2; private decimal? _gate;
    private Dictionary<string, FieldMeasurementResult> _fieldResults = new(StringComparer.OrdinalIgnoreCase);
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public decimal? Nominal { get => _nominal; init { _nominal = value; Upsert(BuiltInFieldIds.Nominal, value); } }
    public decimal? Scale1 { get => _scale1; init { _scale1 = value; Upsert(BuiltInFieldIds.Scale1, value); } }
    public decimal? Scale2 { get => _scale2; init { _scale2 = value; Upsert(BuiltInFieldIds.Scale2, value); } }
    public decimal? GateMillimeters { get => _gate; init { _gate = value; Upsert(BuiltInFieldIds.Gate, value); } }
    public Dictionary<string, FieldMeasurementResult> FieldResults
    {
        get => _fieldResults;
        init
        {
            _fieldResults = value is null ? new(StringComparer.OrdinalIgnoreCase) : new(value, StringComparer.OrdinalIgnoreCase);
            _nominal = Official(BuiltInFieldIds.Nominal, _nominal); _scale1 = Official(BuiltInFieldIds.Scale1, _scale1);
            _scale2 = Official(BuiltInFieldIds.Scale2, _scale2); _gate = Official(BuiltInFieldIds.Gate, _gate);
        }
    }
    public string GateStatus { get; init; } = "Okunamadı";
    public string ExcavationStatus { get; init; } = "Ekran Değerleri Okunamadı";
    public OcrConfidenceStatus OcrConfidence { get; init; } = OcrConfidenceStatus.Unreadable;
    public string? NominalValidationStatus { get; init; }
    public string? Scale1ValidationStatus { get; init; }
    public string? Scale2ValidationStatus { get; init; }
    public bool HasHeldSuspiciousValue { get; init; }
    public FieldMeasurementResult? DiagnosticRpm => Diagnostic(BuiltInFieldIds.StableRpm);
    public FieldMeasurementResult? DiagnosticTorque => Diagnostic(BuiltInFieldIds.StableTorque);
    public FieldMeasurementResult? DiagnosticThrustForce => Diagnostic(BuiltInFieldIds.StableThrustForce);
    public FieldMeasurementResult? DiagnosticAdvanceSpeed => Diagnostic(BuiltInFieldIds.StableAdvanceSpeed);
    public FieldMeasurementResult? DiagnosticPenetration => Diagnostic(BuiltInFieldIds.StablePenetration);
    public FieldMeasurementResult? NominalInjectionObservation => Diagnostic(BuiltInFieldIds.NominalInjection);
    public FieldMeasurementResult? RealInjectionObservation => Diagnostic(BuiltInFieldIds.RealInjection);
    public FieldMeasurementResult? Epb1Observation => Diagnostic(BuiltInFieldIds.Epb1);
    public FieldMeasurementResult? Epb2Observation => Diagnostic(BuiltInFieldIds.Epb2);
    public FieldMeasurementResult? Epb3Observation => Diagnostic(BuiltInFieldIds.Epb3);
    public FieldMeasurementResult? Epb4Observation => Diagnostic(BuiltInFieldIds.Epb4);
    public FieldMeasurementResult? Epb5Observation => Diagnostic(BuiltInFieldIds.Epb5);

    private decimal? Official(string id, decimal? fallback) => _fieldResults.TryGetValue(id, out var result) ? result.OfficialValue : fallback;
    private FieldMeasurementResult? Diagnostic(string id) => _fieldResults.TryGetValue(id, out var result) ? result : null;
    private void Upsert(string id, decimal? value)
    {
        _fieldResults[id] = new FieldMeasurementResult { FieldId = id, ParsedValue = value, OfficialValue = value,
            CapturedAt = Timestamp, CompletedAt = Timestamp, ConfidenceStatus = value.HasValue ? OcrConfidenceStatus.Reliable : OcrConfidenceStatus.Unreadable };
    }
}
