using System.Runtime.Serialization;
using Azure;
using Azure.Data.Tables;
using Quartz;

namespace Community.Quartz.TableStorage.Entities;

/// <summary>
/// A remotely stored calendar entity.
/// </summary>
public class Calendar : ITableEntity
{
    public string PartitionKey { get; set; } = null!;
    public string RowKey { get; set; } = null!;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    /// <summary>
    /// The serialized details.
    /// </summary>
    public string Data
    {
        get => Details.Serialize(Detail);
        set => Detail = Details.Deserialize<ICalendar>(value);
    }

    /// <summary>
    /// Constructor for initialization by deserializer.
    /// </summary>
    [Obsolete("Internal use only", true)]
    public Calendar()
    {
    }

    /// <summary>
    /// Creates the instance with the given values.
    /// </summary>
    /// <param name="partitionKey">The partition key of the entity.</param>
    /// <param name="rowKey">The row key of the entity.</param>
    /// <param name="detail">The calendar details.</param>
    public Calendar(string partitionKey, string rowKey, ICalendar detail)
    {
        PartitionKey = partitionKey;
        RowKey = rowKey;
        Detail = detail;
    }

    /// <summary>
    /// The actual calendar details.
    /// </summary>
    [IgnoreDataMember]
    public ICalendar Detail { get; set; } = null!;
}