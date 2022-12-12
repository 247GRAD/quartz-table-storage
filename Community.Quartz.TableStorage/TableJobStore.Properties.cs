using Quartz;

namespace Community.Quartz.TableStorage;

// Externally set and type determined properties.
public sealed partial class TableJobStore
{
    [TimeSpanParseRule(TimeSpanParseRule.Milliseconds)]
    public TimeSpan MisfireThreshold
    {
        get => _misfireThreshold;
        set => _misfireThreshold = value.TotalMilliseconds > 1
            ? value
            : throw new ArgumentException("MisfireThreshold must be larger than 0", nameof(value));
    }

    public bool SupportsPersistence => true;

    public string InstanceId { get; set; } = null!;

    public string InstanceName { get; set; } = null!;

    public int ThreadPoolSize
    {
        set { }
    }

    public long EstimatedTimeToReleaseAndAcquireTrigger => AcquisitionMeasurements.Average() ?? 100;

    public bool Clustered { get; set; }
}