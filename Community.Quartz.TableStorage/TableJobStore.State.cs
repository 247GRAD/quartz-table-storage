using Community.Quartz.TableStorage.Entities;
using Quartz;

namespace Community.Quartz.TableStorage;

// Trigger state determination.
public sealed partial class TableJobStore
{
    public async Task<TriggerState> GetTriggerState(
        TriggerKey triggerKey,
        CancellationToken cancel)
    {
        if (await GetTrigger(triggerKey, cancel) is not { } entity)
            return TriggerState.None;

        return entity.State switch
        {
            Trigger.States.Complete => TriggerState.Complete,
            Trigger.States.Paused => TriggerState.Paused,
            Trigger.States.Blocked => TriggerState.Blocked,
            Trigger.States.PausedAndBlocked => TriggerState.Paused,
            Trigger.States.Error => TriggerState.Error,
            _ => TriggerState.Normal
        };
    }
}