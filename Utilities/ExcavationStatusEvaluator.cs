using TBMWatchdog.Models;

namespace TBMWatchdog.Utilities;

public sealed class ExcavationStatusEvaluator
{
    private readonly TimeSpan _inactivityThreshold;
    private MeasurementRecord? _previous;
    private DateTime? _lastProgressAt;

    public ExcavationStatusEvaluator(TimeSpan inactivityThreshold)
    {
        if (inactivityThreshold < TimeSpan.FromSeconds(1)) throw new ArgumentOutOfRangeException(nameof(inactivityThreshold));
        _inactivityThreshold = inactivityThreshold;
    }

    public MeasurementRecord Evaluate(MeasurementRecord input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var filtered = FilterIllogicalRegressions(input);
        var allReadable = filtered.Nominal.HasValue
            && filtered.Scale1.HasValue
            && filtered.Scale2.HasValue
            && filtered.GateMillimeters.HasValue;

        var gateStatus = filtered.GateMillimeters switch
        {
            null => "Okunamadı",
            0m => "Tam Kapalı",
            <= 100m => "Kısmen Açık - Malzeme Akışı Yok",
            _ => "Açık - Malzeme Akışı Var"
        };

        var excavationStatus = EvaluateExcavationStatus(filtered, allReadable);
        var confidence = TBMWatchdog.Services.OfficialOcrConfidenceEvaluator.FromValues(
            filtered.Nominal, filtered.Scale1, filtered.Scale2, filtered.GateMillimeters);

        var result = filtered with
        {
            GateStatus = gateStatus,
            ExcavationStatus = excavationStatus,
            OcrConfidence = confidence
        };
        _previous = result;
        return result;
    }

    private string EvaluateExcavationStatus(MeasurementRecord record, bool allReadable)
    {
        if (!allReadable) return "Ekran Değerleri Okunamadı";
        if (record.GateMillimeters == 0) return "Kapak Tam Kapalı";
        if (record.GateMillimeters <= 100) return "Kapak Kısmen Açık - Malzeme Akışı Beklenmiyor";

        var progressed = _previous is not null && (
            record.Nominal > _previous.Nominal
            || record.Scale1 > _previous.Scale1
            || record.Scale2 > _previous.Scale2);
        if (progressed)
        {
            _lastProgressAt = record.Timestamp;
            return "Kazı / Taşıma Devam Ediyor";
        }

        _lastProgressAt ??= record.Timestamp;
        if (record.Timestamp - _lastProgressAt >= _inactivityThreshold)
        {
            return "Kapak Açık Ancak Değerler İlerlemiyor";
        }

        return "Kapak Açık - İlerleme Bekleniyor";
    }

    private MeasurementRecord FilterIllogicalRegressions(MeasurementRecord input)
    {
        if (_previous is null) return input;
        var simultaneousReset = IsNearZero(input.Nominal)
            && IsNearZero(input.Scale1)
            && IsNearZero(input.Scale2)
            && (_previous.Nominal > 1 || _previous.Scale1 > 1 || _previous.Scale2 > 1);
        if (simultaneousReset)
        {
            _lastProgressAt = input.Timestamp;
            return input;
        }

        return input with
        {
            Nominal = IsRegression(input.Nominal, _previous.Nominal) ? null : input.Nominal,
            Scale1 = IsRegression(input.Scale1, _previous.Scale1) ? null : input.Scale1,
            Scale2 = IsRegression(input.Scale2, _previous.Scale2) ? null : input.Scale2
        };
    }

    private static bool IsRegression(decimal? current, decimal? previous) =>
        current.HasValue && previous.HasValue && current.Value < previous.Value;

    private static bool IsNearZero(decimal? value) => value.HasValue && value.Value is >= 0m and <= 1m;
}
