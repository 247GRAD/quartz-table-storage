using System.Collections.Immutable;
using Community.Quartz.TableStorage.Entities;
using Quartz;
using Quartz.Spi;

namespace Community.Quartz.TableStorage;

// Inverse relation navigation.
public sealed partial class TableJobStore
{
    public async Task<IReadOnlyCollection<IOperableTrigger>> GetTriggersForJob(JobKey jobKey,
        CancellationToken cancel)
    {
        var result = ImmutableList.CreateBuilder<IOperableTrigger>();
        await foreach (var entity in Triggers.QueryAsync<Trigger>(
                           filter: item => item.JobGroup == jobKey.Group &&
                                           item.JobName == jobKey.Name,
                           cancellationToken: cancel))
            result.Add(entity.Detail);
        return result.ToImmutable();
    }
}