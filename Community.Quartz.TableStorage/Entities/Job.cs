using System.Runtime.Serialization;
using Azure;
using Azure.Data.Tables;
using Quartz;

namespace Community.Quartz.TableStorage.Entities;

/// <summary>
/// A remotely stored job entity.
/// </summary>
public class Job : ITableEntity
{
    public string PartitionKey { get; set; } = null!;
    public string RowKey { get; set; } = null!;

    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    /// <summary>
    /// The derived job key, composed of row and partition key.
    /// </summary>
    [IgnoreDataMember]
    public JobKey Key => new(RowKey, PartitionKey);

    /// <summary>
    /// The serialized details.
    /// </summary>
    public string Data
    {
        get => Details.Serialize(Detail);
        set => Detail = Details.Deserialize<IJobDetail>(value);
    }

    /// <summary>
    /// Constructor for initialization by deserializer.
    /// </summary>
    [Obsolete("Internal use only", true)]
    public Job()
    {
    }

    /// <summary>
    /// Creates the instance with the given values. Partition and row key are taken from the detail.
    /// </summary>
    /// <param name="detail">The job details.</param>
    public Job(IJobDetail detail)
    {
        PartitionKey = detail.Key.Group;
        RowKey = detail.Key.Name;
        Detail = detail;
    }

    /// <summary>
    /// The actual job details.
    /// </summary>
    [IgnoreDataMember]
    public IJobDetail Detail { get; set; } = null!;
}