using TBMWatchdog.Models;

namespace TBMWatchdog.Services;

public interface IDataSource
{
    string Name { get; }
    Task<MeasurementRecord> ReadAsync(CancellationToken cancellationToken);
}
