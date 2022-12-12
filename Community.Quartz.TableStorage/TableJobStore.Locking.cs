using Azure;
using Azure.Data.Tables;
using Community.Quartz.TableStorage.Entities;
using Quartz.Util;

namespace Community.Quartz.TableStorage;

// Local and remote locking.
public sealed partial class TableJobStore
{
    /// <summary>
    /// Acquires a local semaphore and shared spin lock.
    /// </summary>
    /// <param name="cancel">Cancellation of acquisition.</param>
    private async Task AcquireLock(CancellationToken cancel)
    {
        // Special case for local only locks.
        if (!Clustered)
        {
            // Acquire local and return.
            await LockLocal.WaitAsync(cancel);
            return;
        }

        // Backoff variables for waiting.
        const int backoffMax = 400;
        var backoff = 20;

        while (true)
        {
            // Acquire local lock first.
            await LockLocal.WaitAsync(cancel);


            // Get the shared lock entity.
            var entity = await Locks.GetEntityAsync<Lock>(All, All, cancellationToken: cancel);

            // Check shared lock status.
            if (entity.Value.InstanceId == InstanceId)
            {
                // Is locked by self, can happen when the lock was prematurely interrupted.
                break;
            }
            else if (entity.Value.InstanceId.IsNullOrWhiteSpace())
            {
                // Lock is locked by no one.
                try
                {
                    // Lock, match ETag.
                    await Locks.UpdateEntityAsync(new Lock(All, All) {InstanceId = InstanceId}, entity.Value.ETag,
                        TableUpdateMode.Replace, cancel);

                    // Locking went through, return from loop.
                    break;
                }
                catch (RequestFailedException)
                {
                    // Locking failed, assume ETag mismatch. Release local, backoff, and try again.
                    LockLocal.Release();
                    await Task.Delay(backoff = Math.Min(backoffMax, backoff * 2), cancel);
                }
            }
            else
            {
                // Lock is locked by someone else, release local, backoff, and try again.
                LockLocal.Release();
                await Task.Delay(backoff = Math.Min(backoffMax, backoff * 2), cancel);
            }
        }
    }

    /// <summary>
    /// Releases the shared and local lock.
    /// </summary>
    private async Task ReleaseLock()
    {
        // Special case for local only locks.
        if (!Clustered)
        {
            // Release local lock and return.
            LockLocal.Release();
            return;
        }

        // Unlock any. If we're in a position where we can release, a lock was acquired successfully.
        await Locks.UpdateEntityAsync(new Lock(All, All) {InstanceId = string.Empty}, ETag.All,
            TableUpdateMode.Replace, CancellationToken.None);

        // Release local.
        LockLocal.Release();
    }
}