using Azure;
using Azure.Data.Tables;

namespace Community.Quartz.TableStorage;

public static class TableClientExtensions
{
    /// <summary>
    /// Gets an entity or the default, i.e., null for object types.
    /// </summary>
    /// <param name="client">The client to read from./</param>
    /// <param name="partitionKey">The partition key of the entity.</param>
    /// <param name="rowKey">The row key of the entity.</param>
    /// <param name="select">The selection query.</param>
    /// <param name="cancellationToken">Cancellation of the operation.</param>
    /// <typeparam name="T">An entity type, must be a class, empty constructable and implement <see cref="ITableEntity"/>.</typeparam>
    /// <returns>Returns a task to the entity or null.</returns>
    public static async Task<T?> GetEntityOrDefaultAsync<T>(this TableClient client, string partitionKey, string rowKey,
        IEnumerable<string>? select = null, CancellationToken cancellationToken = default)
        where T : class, ITableEntity, new()
    {
        var result = await client.GetEntityIfExistsAsync<T>(partitionKey, rowKey, select, cancellationToken);
        return result.HasValue ? result.Value : default;
    }

    /// <summary>
    /// Tries to add an entity, if it does not exist already.
    /// </summary>
    /// <param name="client">The client to write to./</param>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">Cancellation of the operation.</param>
    /// <typeparam name="T">An entity type, must be a class, empty constructable and implement <see cref="ITableEntity"/>.</typeparam>
    /// <returns>Returns a task specifying true if the entity was successfully added.</returns>
    public static async Task<bool> TryAddEntityAsync<T>(this TableClient client, T entity,
        CancellationToken cancellationToken = default)
        where T : class, ITableEntity, new()
    {
        try
        {
            await client.AddEntityAsync(entity, cancellationToken);
            return true;
        }
        catch (RequestFailedException)
        {
            return false;
        }
    }
}