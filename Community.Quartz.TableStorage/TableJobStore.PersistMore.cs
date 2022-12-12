using Quartz;
using Quartz.Spi;

namespace Community.Quartz.TableStorage;

// Extended persistence and bulk operations.
public sealed partial class TableJobStore
{
    public async Task StoreJobAndTrigger(
        IJobDetail newJob,
        IOperableTrigger newTrigger,
        CancellationToken cancel)
    {
        await AcquireLock(cancel);
        try
        {
            await StoreJobInternal(newJob, false, cancel);
            await StoreTriggerInternal(newTrigger, false, cancel);
        }
        finally
        {
            await ReleaseLock();
        }
    }

    public async Task<bool> RemoveJobs(IReadOnlyCollection<JobKey> jobKeys, CancellationToken cancel)
    {
        await AcquireLock(cancel);
        try
        {
            var allFound = true;
            foreach (var key in jobKeys)
                if (!await RemoveJobInternal(key, cancel))
                    allFound = false;
            return allFound;
        }
        finally
        {
            await ReleaseLock();
        }
    }

    public async Task StoreJobsAndTriggers(
        IReadOnlyDictionary<IJobDetail, IReadOnlyCollection<ITrigger>> triggersAndJobs,
        bool replace,
        CancellationToken cancel)
    {
        await AcquireLock(cancel);
        try
        {
            if (!replace)
                foreach (var (job, triggers) in triggersAndJobs)
                {
                    if (await GetJob(job.Key, cancel) is not null)
                        throw new InvalidOperationException("Job already exists");

                    foreach (var trigger in triggers)
                        if (await GetTrigger(trigger.Key, cancel) is not null)
                            throw new InvalidOperationException("Trigger already exists");
                }

            foreach (var (job, triggers) in triggersAndJobs)
            {
                await StoreJobInternal(job, true, cancel);

                foreach (var trigger in triggers)
                    await StoreTriggerInternal((IOperableTrigger) trigger, true, cancel);
            }
        }
        finally
        {
            await ReleaseLock();
        }
    }

    public async Task<bool> ReplaceTrigger(
        TriggerKey triggerKey,
        IOperableTrigger newTrigger,
        CancellationToken cancel)
    {
        await AcquireLock(cancel);
        try
        {
            if (await GetTrigger(triggerKey, cancel) is not { } entity)
                return false;

            if (entity.PartitionKey != newTrigger.Key.Group)
                throw new JobPersistenceException("New trigger is not related to the same job as the old trigger.");
            if (entity.RowKey != newTrigger.Key.Name)
                throw new JobPersistenceException("New trigger is not related to the same job as the old trigger.");
            await StoreTriggerInternal(newTrigger, true, cancel);
            return true;
        }
        finally
        {
            await ReleaseLock();
        }
    }

    public async Task<bool> RemoveTriggers(
        IReadOnlyCollection<TriggerKey> triggerKeys,
        CancellationToken cancel)
    {
        await AcquireLock(cancel);
        try
        {
            var allFound = true;
            foreach (var key in triggerKeys)
                if (!await RemoveTriggerInternal(key, cancel))
                    allFound = false;
            return allFound;
        }
        finally
        {
            await ReleaseLock();
        }
    }
}