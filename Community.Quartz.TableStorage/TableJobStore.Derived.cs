using System.Collections.Immutable;
using Community.Quartz.TableStorage.Entities;
using Quartz;
using Quartz.Impl.Matchers;

namespace Community.Quartz.TableStorage;

// Derived metrics by invocation.
public sealed partial class TableJobStore
{
    public async Task<int> GetNumberOfJobs(CancellationToken cancel)
    {
        var count = 0;
        await foreach (var unused in Jobs.QueryAsync<Job>(cancellationToken: cancel))
            count++;
        return count;
    }

    public async Task<int> GetNumberOfTriggers(CancellationToken cancel)
    {
        var count = 0;
        await foreach (var unused in Triggers.QueryAsync<Trigger>(cancellationToken: cancel))
            count++;
        return count;
    }

    public async Task<int> GetNumberOfCalendars(CancellationToken cancel)
    {
        var count = 0;
        await foreach (var unused in Calendars.QueryAsync<Calendar>(cancellationToken: cancel))
            count++;
        return count;
    }

    public async Task<IReadOnlyCollection<JobKey>> GetJobKeys(GroupMatcher<JobKey> matcher,
        CancellationToken cancel)
    {
        var result = ImmutableList.CreateBuilder<JobKey>();
        await foreach (var entity in Jobs.QueryAsync<Job>(cancellationToken: cancel))
            if (matcher.IsMatch(entity.Key))
                result.Add(entity.Key);
        return result.ToImmutable();
    }

    public async Task<IReadOnlyCollection<TriggerKey>> GetTriggerKeys(GroupMatcher<TriggerKey> matcher,
        CancellationToken cancel)
    {
        var result = ImmutableList.CreateBuilder<TriggerKey>();
        await foreach (var entity in Triggers.QueryAsync<Trigger>(cancellationToken: cancel))
            if (matcher.IsMatch(entity.Key))
                result.Add(entity.Key);
        return result.ToImmutable();
    }

    public async Task<IReadOnlyCollection<string>> GetJobGroupNames(
        CancellationToken cancel)
    {
        var result = new HashSet<string>();
        await foreach (var entity in Jobs.QueryAsync<Job>(cancellationToken: cancel))
            result.Add(entity.PartitionKey);
        return result;
    }

    public async Task<IReadOnlyCollection<string>> GetTriggerGroupNames(
        CancellationToken cancel)
    {
        var result = new HashSet<string>();
        await foreach (var entity in Triggers.QueryAsync<Trigger>(cancellationToken: cancel))
            result.Add(entity.PartitionKey);
        return result;
    }

    public async Task<IReadOnlyCollection<string>> GetCalendarNames(
        CancellationToken cancel)
    {
        var result = ImmutableList.CreateBuilder<string>();
        await foreach (var entity in Calendars.QueryAsync<Calendar>(cancellationToken: cancel))
            result.Add(entity.RowKey);
        return result.ToImmutable();
    }

    public async Task<IReadOnlyCollection<string>> GetPausedTriggerGroups(
        CancellationToken cancel)
    {
        var result = new HashSet<string>();
        await foreach (var entity in Triggers.QueryAsync<Trigger>(cancellationToken: cancel))
            if (entity.State is Trigger.States.Paused or Trigger.States.PausedAndBlocked)
                result.Add(entity.PartitionKey);
        return result;
    }
}