using Azure;
using Azure.Data.Tables;
using Community.Quartz.TableStorage.Entities;

namespace Community.Quartz.TableStorage;

// Scheduler hooks.
public sealed partial class TableJobStore
{
    public async Task SchedulerStarted(CancellationToken cancel) =>
        await ResetAcquisition(cancel);

    public Task SchedulerPaused(CancellationToken cancel) => Task.CompletedTask;

    public Task SchedulerResumed(CancellationToken cancel) => Task.CompletedTask;

    public async Task Shutdown(CancellationToken cancel) =>
        await ResetAcquisition(cancel);

    /// <summary>
    /// Resets trigger acquisitions and implicitly locks.
    /// </summary>
    /// <param name="cancel">Operation cancellation.</param>
    private async Task ResetAcquisition(CancellationToken cancel)
    {
        await AcquireLock(cancel);
        try
        {
            await foreach (var entity in Triggers.QueryAsync<Trigger>(cancellationToken: cancel))
            {
                if (entity.State == Trigger.States.Acquired)
                    entity.State = Trigger.States.Waiting;
                await Triggers.UpdateEntityAsync(entity, ETag.All, TableUpdateMode.Replace, cancel);
            }
        }
        finally
        {
            await ReleaseLock();
        }
    }
}