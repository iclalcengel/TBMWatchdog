using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public sealed class DemoDataSource : IDataSource
{
    private readonly Random _random = new();
    private int _sampleIndex;
    private decimal _nominal;
    private decimal _scale1;
    private decimal _scale2;

    public string Name => "Demo Modu";

    public Task<MeasurementRecord> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var phase = _sampleIndex % 36;
        var gate = phase switch
        {
            < 4 => 0m,
            < 8 => 75m,
            _ => 400m
        };

        var isMoving = phase is >= 8 and < 18;
        if (isMoving)
        {
            _nominal += NextIncrement(0.8m, 1.8m);
            _scale1 += NextIncrement(0.5m, 1.4m);
            _scale2 += NextIncrement(0.4m, 1.2m);
        }

        var gateStatus = gate switch
        {
            0m => "Tam Kapalı",
            <= 100m => "Kısmen Açık - Malzeme Akışı Yok",
            _ => "Açık - Malzeme Akışı Var"
        };

        var excavationStatus = phase switch
        {
            < 4 => "Kapak Tam Kapalı",
            < 8 => "Kapak Kısmen Açık - Malzeme Akışı Beklenmiyor",
            < 18 => "Kazı / Taşıma Devam Ediyor",
            _ => "Kapak Açık Ancak Değerler İlerlemiyor"
        };

        _sampleIndex++;
        return Task.FromResult(new MeasurementRecord
        {
            Timestamp = DateTime.Now,
            Nominal = decimal.Round(_nominal, 2),
            Scale1 = decimal.Round(_scale1, 2),
            Scale2 = decimal.Round(_scale2, 2),
            GateMillimeters = gate,
            GateStatus = gateStatus,
            ExcavationStatus = excavationStatus,
            OcrConfidence = OcrConfidenceStatus.Reliable
        });
    }

    private decimal NextIncrement(decimal minimum, decimal maximum) =>
        minimum + ((decimal)_random.NextDouble() * (maximum - minimum));
}
