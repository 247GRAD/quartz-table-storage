using Community.Quartz.TableStorage.Entities;

namespace Community.Quartz.TableStorage;

// Job and trigger group paused states.
public sealed partial class TableJobStore
{
    public async Task<bool> IsJobGroupPaused(string groupName, CancellationToken cancel)
    {
        await foreach (var entity in Triggers.QueryAsync<Trigger>(
                           filter: item => item.JobGroup == groupName,
                           cancellationToken: cancel))
            if (entity.State is not Trigger.States.Paused or Trigger.States.PausedAndBlocked)
                return false;

        return true;
    }

    public async Task<bool> IsTriggerGroupPaused(string groupName, CancellationToken cancel)
    {
        await foreach (var entity in Triggers.QueryAsync<Trigger>(
                           filter: item => item.PartitionKey == groupName,
                           cancellationToken: cancel))
            if (entity.State is not Trigger.States.Paused or Trigger.States.PausedAndBlocked)
                return false;

        return true;
    }
}