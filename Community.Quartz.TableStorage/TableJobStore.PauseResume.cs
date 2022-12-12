using Community.Quartz.TableStorage.Entities;
using Quartz;
using Quartz.Impl.Matchers;

namespace Community.Quartz.TableStorage;

// Trigger and job pause and resume routines.
public sealed partial class TableJobStore
{
    public async Task PauseTrigger(
        TriggerKey triggerKey,
        CancellationToken cancel)
    {
        await AcquireLock(cancel);
        try
        {
            if (await GetTrigger(triggerKey, cancel) is not { } entity)
                return;
            if (entity.State is Trigger.States.Complete)
                return;
            entity.State = entity.State is Trigger.States.Blocked
                ? Trigger.States.PausedAndBlocked
                : Trigger.States.Paused;
            await UpdateTrigger(entity, cancel);
        }
        finally
        {
            await ReleaseLock();
        }
    }

    public async Task<IReadOnlyCollection<string>> PauseTriggers(
        GroupMatcher<TriggerKey> matcher,
        CancellationToken cancel)
    {
        await AcquireLock(cancel);
        try
        {
            var result = new HashSet<string>();
            await foreach (var entity in Triggers.QueryAsync<Trigger>(cancellationToken: cancel))
            {
                if (!matcher.IsMatch(entity.Key))
                    continue;
                if (entity.State is Trigger.States.Complete)
                    continue;
                entity.State = entity.State is Trigger.States.Blocked
                    ? Trigger.States.PausedAndBlocked
                    : Trigger.States.Paused;
                await UpdateTrigger(entity, cancel);
                result.Add(entity.PartitionKey);
            }

            return result;
        }
        finally
        {
            await ReleaseLock();
        }
    }

    public async Task PauseJob(JobKey jobKey, CancellationToken cancel)
    {
        await AcquireLock(cancel);
        try
        {
            await foreach (var entity in Triggers.QueryAsync<Trigger>(
                               filter: item => item.JobGroup == jobKey.Group &&
                                               item.JobName == jobKey.Name,
                               cancellationToken: cancel))
            {
                if (entity.State is Trigger.States.Complete)
                    return;
                entity.State = entity.State is Trigger.States.Blocked
                    ? Trigger.States.PausedAndBlocked
                    : Trigger.States.Paused;
                await UpdateTrigger(entity, cancel);
            }
        }
        finally
        {
            await ReleaseLock();
        }
    }

    public async Task<IReadOnlyCollection<string>> PauseJobs(
        GroupMatcher<JobKey> matcher,
        CancellationToken cancel)
    {
        await AcquireLock(cancel);
        try
        {
            var result = new HashSet<string>();
            await foreach (var entity in Triggers.QueryAsync<Trigger>(cancellationToken: cancel))
            {
                if (!matcher.IsMatch(entity.JobKey))
                    continue;
                if (entity.State is Trigger.States.Complete)
                    continue;
                entity.State = entity.State is Trigger.States.Blocked
                    ? Trigger.States.PausedAndBlocked
                    : Trigger.States.Paused;
                await UpdateTrigger(entity, cancel);
                result.Add(entity.JobGroup);
            }

            return result;
        }
        finally
        {
            await ReleaseLock();
        }
    }

    public async Task ResumeTrigger(
        TriggerKey triggerKey,
        CancellationToken cancel)
    {
        await AcquireLock(cancel);
        try
        {
            if (await GetTrigger(triggerKey, cancel) is not { } entity)
                return;
            if (entity.State is not Trigger.States.Paused or Trigger.States.PausedAndBlocked)
                return;
            entity.State = entity.State is Trigger.States.PausedAndBlocked
                ? Trigger.States.Blocked
                : Trigger.States.Waiting;
            await ApplyMisfire(entity.Detail, cancel);
            if (!entity.Detail.GetNextFireTimeUtc().HasValue)
                entity.State = Trigger.States.Complete;
            await UpdateTrigger(entity, cancel);
        }
        finally
        {
            await ReleaseLock();
        }
    }

    public async Task<IReadOnlyCollection<string>> ResumeTriggers(
        GroupMatcher<TriggerKey> matcher,
        CancellationToken cancel)
    {
        await AcquireLock(cancel);
        try
        {
            var result = new HashSet<string>();
            await foreach (var entity in Triggers.QueryAsync<Trigger>(cancellationToken: cancel))
            {
                if (!matcher.IsMatch(entity.Key))
                    continue;
                if (entity.State is not Trigger.States.Paused or Trigger.States.PausedAndBlocked)
                    continue;
                entity.State = entity.State is Trigger.States.PausedAndBlocked
                    ? Trigger.States.Blocked
                    : Trigger.States.Waiting;
                await UpdateTrigger(entity, cancel);
                result.Add(entity.PartitionKey);
            }

            return result;
        }
        finally
        {
            await ReleaseLock();
        }
    }

    public async Task ResumeJob(JobKey jobKey, CancellationToken cancel)
    {
        await AcquireLock(cancel);
        try
        {
            await foreach (var entity in Triggers.QueryAsync<Trigger>(
                               filter: item => item.JobGroup == jobKey.Group &&
                                               item.JobName == jobKey.Name,
                               cancellationToken: cancel))
            {
                if (entity.State is not Trigger.States.Paused or Trigger.States.PausedAndBlocked)
                    continue;
                entity.State = entity.State is Trigger.States.PausedAndBlocked
                    ? Trigger.States.Blocked
                    : Trigger.States.Waiting;
                await UpdateTrigger(entity, cancel);
            }
        }
        finally
        {
            await ReleaseLock();
        }
    }

    public async Task<IReadOnlyCollection<string>> ResumeJobs(
        GroupMatcher<JobKey> matcher,
        CancellationToken cancel)
    {
        await AcquireLock(cancel);
        try
        {
            var result = new HashSet<string>();
            await foreach (var entity in Triggers.QueryAsync<Trigger>(cancellationToken: cancel))
            {
                if (!matcher.IsMatch(entity.JobKey))
                    continue;
                if (entity.State is not Trigger.States.Paused or Trigger.States.PausedAndBlocked)
                    continue;
                entity.State = entity.State is Trigger.States.PausedAndBlocked
                    ? Trigger.States.Blocked
                    : Trigger.States.Waiting;
                await UpdateTrigger(entity, cancel);
                result.Add(entity.JobGroup);
            }

            return result;
        }
        finally
        {
            await ReleaseLock();
        }
    }

    public Task PauseAll(CancellationToken cancel) =>
        PauseTriggers(GroupMatcher<TriggerKey>.AnyGroup(), cancel);

    public Task ResumeAll(CancellationToken cancel) =>
        ResumeTriggers(GroupMatcher<TriggerKey>.AnyGroup(), cancel);
}