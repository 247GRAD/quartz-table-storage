using Community.Quartz.TableStorage.Entities;
using Quartz;
using Quartz.Spi;

namespace Community.Quartz.TableStorage;

// Entity persistence interface and internal non-locking methods.
public sealed partial class TableJobStore
{
    public async Task ClearAllSchedulingData(CancellationToken cancel)
    {
        await AcquireLock(cancel);
        try
        {
            await Jobs.DeleteAsync(cancel);
            await Triggers.DeleteAsync(cancel);
            await Calendars.DeleteAsync(cancel);
            await Jobs.CreateAsync(cancel);
            await Triggers.CreateAsync(cancel);
            await Calendars.CreateAsync(cancel);
        }
        finally
        {
            await ReleaseLock();
        }
    }

    public async Task StoreJob(IJobDetail newJob, bool replaceExisting, CancellationToken cancel)
    {
        await AcquireLock(cancel);
        try
        {
            await StoreJobInternal(newJob, replaceExisting, cancel);
        }
        finally
        {
            await ReleaseLock();
        }
    }

    private async Task StoreJobInternal(IJobDetail newJob, bool replaceExisting, CancellationToken cancel)
    {
        if (replaceExisting)
            await UpdateJob(new Job(newJob), cancel);
        else
            await AddJob(new Job(newJob), cancel);
    }

    public async Task<bool> RemoveJob(JobKey jobKey, CancellationToken cancel)
    {
        await AcquireLock(cancel);
        try
        {
            return await RemoveJobInternal(jobKey, cancel);
        }
        finally
        {
            await ReleaseLock();
        }
    }

    private async Task<bool> RemoveJobInternal(JobKey jobKey, CancellationToken cancel)
    {
        if (await GetJob(jobKey, cancel) is not { } entityJob)
            return false;

        await DeleteJob(entityJob.PartitionKey, entityJob.RowKey, cancel);

        await foreach (var entityTrigger in Triggers.QueryAsync<Trigger>(
                           filter: item => item.JobGroup == jobKey.Group &&
                                           item.JobName == jobKey.Name,
                           cancellationToken: cancel))
            await DeleteTrigger(entityTrigger.PartitionKey, entityTrigger.RowKey, cancel);

        return true;
    }

    public async Task StoreTrigger(IOperableTrigger newTrigger, bool replaceExisting,
        CancellationToken cancel)
    {
        await AcquireLock(cancel);
        try
        {
            await StoreTriggerInternal(newTrigger, replaceExisting, cancel);
        }
        finally
        {
            await ReleaseLock();
        }
    }

    private async Task StoreTriggerInternal(IOperableTrigger newTrigger, bool replaceExisting, CancellationToken cancel)
    {
        if (await GetJob(newTrigger.JobKey, cancel) is null)
            throw new JobPersistenceException("The referenced job does not exist");

        if (replaceExisting)
            await UpdateTrigger(new Trigger(newTrigger), cancel);
        else
            await AddTrigger(new Trigger(newTrigger), cancel);
    }

    public async Task<bool> RemoveTrigger(TriggerKey triggerKey, CancellationToken cancel)
    {
        await AcquireLock(cancel);
        try
        {
            return await RemoveTriggerInternal(triggerKey, cancel);
        }
        finally
        {
            await ReleaseLock();
        }
    }

    private async Task<bool> RemoveTriggerInternal(TriggerKey triggerKey, CancellationToken cancel)
    {
        if (await GetTrigger(triggerKey, cancel) is not { } entityTrigger)
            return false;

        await DeleteTrigger(entityTrigger.PartitionKey, entityTrigger.RowKey, cancel);

        var hasMore = false;
        await foreach (var unused in Triggers.QueryAsync<Trigger>(
                           filter: item => item.JobGroup == entityTrigger.JobGroup &&
                                           item.JobName == entityTrigger.JobName,
                           cancellationToken: cancel))
        {
            hasMore = true;
            break;
        }

        if (hasMore)
            return true;

        if (await GetJob(entityTrigger.JobKey, cancel) is not { } job)
            return true;

        var jobDetail = job.Detail;
        if (jobDetail.Durable)
            return true;

        await DeleteJob(entityTrigger.JobGroup, entityTrigger.JobName, cancel);

        await _signaler.NotifySchedulerListenersJobDeleted(entityTrigger.JobKey, cancel)
            .ConfigureAwait(false);

        return true;
    }

    public async Task StoreCalendar(
        string name,
        ICalendar calendar,
        bool replaceExisting,
        bool updateTriggers,
        CancellationToken cancel)
    {
        await AcquireLock(cancel);
        try
        {
            await StoreCalendarInternal(name, calendar, replaceExisting, updateTriggers, cancel);
        }
        finally
        {
            await ReleaseLock();
        }
    }

    private async Task StoreCalendarInternal(string name, ICalendar calendar, bool replaceExisting, bool updateTriggers,
        CancellationToken cancel)
    {
        var item = new Calendar(All, name, calendar);

        if (replaceExisting)
            await UpdateCalendar(item, cancel);
        else
            await AddCalendar(item, cancel);

        if (!updateTriggers)
            return;

        await foreach (var entity in Triggers.QueryAsync<Trigger>(
                           filter: entity => entity.CalendarName == name,
                           cancellationToken: cancel))
        {
            var trigger = entity.Detail;
            trigger.UpdateWithNewCalendar(calendar, MisfireThreshold);
            entity.Detail = trigger;
            await UpdateTrigger(entity, cancel);
        }
    }

    public async Task<bool> RemoveCalendar(string calName, CancellationToken cancel)
    {
        await AcquireLock(cancel);
        try
        {
            return await RemoveCalendarInternal(calName, cancel);
        }
        finally
        {
            await ReleaseLock();
        }
    }

    private async Task<bool> RemoveCalendarInternal(string calName, CancellationToken cancel)
    {
        if (await GetCalendar(calName, cancel) is null)
            return false;

        await foreach (var unused in Triggers.QueryAsync<Trigger>(
                           filter: entity => entity.CalendarName == calName,
                           cancellationToken: cancel))
            throw new InvalidOperationException("Calender cannot be removed if it referenced by a Trigger!");

        await DeleteCalendar(calName, cancel);
        return true;
    }
}