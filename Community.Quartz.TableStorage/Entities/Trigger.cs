using System.Runtime.Serialization;
using Azure;
using Azure.Data.Tables;
using Quartz;
using Quartz.Spi;

namespace Community.Quartz.TableStorage.Entities;

/// <summary>
/// A remotely stored trigger entity.
/// </summary>
public class Trigger : ITableEntity
{
    /// <summary>
    /// The states of a trigger.
    /// </summary>
    public enum States
    {
        Waiting,
        Acquired,

        // TODO: Needed?
        Executing,
        Complete,
        Paused,
        Blocked,
        PausedAndBlocked,
        Error
    }

    public string PartitionKey { get; set; } = null!;
    public string RowKey { get; set; } = null!;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    /// <summary>
    /// The job group name. 
    /// </summary>
    public string JobGroup { get; set; } = null!;

    /// <summary>
    /// The job name.
    /// </summary>
    public string JobName { get; set; } = null!;

    /// <summary>
    /// The derived trigger key, composed of row and partition key.
    /// </summary>
    [IgnoreDataMember]
    public TriggerKey Key => new(RowKey, PartitionKey);

    /// <summary>
    /// The derived job key, composed of the job group and name fields.
    /// </summary>
    [IgnoreDataMember]
    public JobKey JobKey => new(JobName, JobGroup);

    /// <summary>
    /// The state of the trigger.
    /// </summary>
    public States State { get; set; }

    /// <summary>
    /// The calendar if applied.
    /// </summary>
    public string? CalendarName { get; set; }

    /// <summary>
    /// The serialized details.
    /// </summary>
    public string Data
    {
        get => Details.Serialize(Detail);
        set => Detail = Details.Deserialize<IOperableTrigger>(value);
    }

    /// <summary>
    /// Constructor for initialization by deserializer.
    /// </summary>
    [Obsolete("Internal use only", true)]
    public Trigger()
    {
    }


    /// <summary>
    /// Creates the instance with the given values. Partition and row key are taken from the detail.
    /// </summary>
    /// <param name="detail">The trigger details.</param>
    public Trigger(IOperableTrigger detail)
    {
        PartitionKey = detail.Key.Group;
        RowKey = detail.Key.Name;
        JobGroup = detail.JobKey.Group;
        JobName = detail.JobKey.Name;
        CalendarName = detail.CalendarName;
        Detail = detail;
    }

    /// <summary>
    /// The actual trigger details.
    /// </summary>
    [IgnoreDataMember]
    public IOperableTrigger Detail { get; set; } = null!;
}