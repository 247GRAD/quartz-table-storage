using Azure;
using Azure.Data.Tables;

namespace Community.Quartz.TableStorage.Entities;

/// <summary>
/// A remotely stored lock entity.
/// </summary>
public class Lock : ITableEntity
{
    public string PartitionKey { get; set; } = null!;
    public string RowKey { get; set; } = null!;

    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    /// <summary>
    /// The instance ID holding the lock.
    /// </summary>
    public string? InstanceId { get; set; }

    /// <summary>
    /// Constructor for initialization by deserializer.
    /// </summary>
    [Obsolete("Internal use only", true)]
    public Lock()
    {
    }

    /// <summary>
    /// Creates the instance with the given values.
    /// </summary>
    /// <param name="partitionKey">The partition key of the entity.</param>
    /// <param name="rowKey">The row key of the entity.</param>
    public Lock(string partitionKey, string rowKey)
    {
        PartitionKey = partitionKey;
        RowKey = rowKey;
    }
}