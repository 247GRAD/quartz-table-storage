using System.Collections.Immutable;
using Community.Quartz.TableStorage.Entities;
using Quartz;
using Quartz.Spi;

namespace Community.Quartz.TableStorage;

// Trigger firing and completion hooks.
public sealed partial class TableJobStore
{
    public async Task<IReadOnlyCollection<TriggerFiredResult>> TriggersFired(
        IReadOnlyCollection<IOperableTrigger> triggers,
        CancellationToken cancel)
    {
        var results = ImmutableList.CreateBuilder<TriggerFiredResult>();
        foreach (var trigger in triggers)
        {
            // Assert trigger is present and acquired, otherwise continue with the next trigger. Job is required.
            if (await GetTrigger(trigger.Key, cancel) is not {State: Trigger.States.Acquired} entity)
                continue;
            if (await RetrieveJob(trigger.JobKey, cancel) is not { } jobDetail)
                throw new InvalidOperationException("Job must exist");

            // Get calendar and assert if required.
            var calendar = trigger.CalendarName == null
                ? null
                : (await GetCalendar(trigger.CalendarName, cancel))?.Detail;
            if (calendar == null && trigger.CalendarName != null)
                continue;

            var prevFireTime = trigger.GetPreviousFireTimeUtc();

            // Call triggered on our copy, and the scheduler's copy.
            entity.Detail.Triggered(calendar);
            trigger.Triggered(calendar);

            entity.State = Trigger.States.Waiting;
            await UpdateTrigger(entity, cancel);
            var bundle = new TriggerFiredBundle(jobDetail, trigger, calendar, false,
                SystemTime.UtcNow(),
                trigger.GetPreviousFireTimeUtc(),
                prevFireTime, trigger.GetNextFireTimeUtc());

            if (bundle.JobDetail.ConcurrentExecutionDisallowed)
                await foreach (var other in Triggers.QueryAsync<Trigger>(
                                   filter: item => item.PartitionKey != entity.PartitionKey &&
                                                   item.RowKey != entity.RowKey &&
                                                   item.JobGroup == entity.JobGroup &&
                                                   item.JobName == entity.JobName,
                                   cancellationToken: cancel))
                {
                    other.State = other.State switch
                    {
                        Trigger.States.Waiting => Trigger.States.Blocked,
                        Trigger.States.Paused => Trigger.States.PausedAndBlocked,
                        _ => other.State
                    };
                    await UpdateTrigger(other, cancel);
                }

            results.Add(new TriggerFiredResult(bundle));
        }

        return results;
    }

    public async Task TriggeredJobComplete(
        IOperableTrigger trigger,
        IJobDetail jobDetail,
        SchedulerInstruction triggerInstCode,
        CancellationToken cancel)
    {
        if (await GetJob(jobDetail.Key, cancel) is { } entityJob)
        {
            if (entityJob.Detail.PersistJobDataAfterExecution)
            {
                var newData = jobDetail.JobDataMap;
                newData = (JobDataMap) newData.Clone();
                newData.ClearDirtyFlag();
                entityJob.Detail = entityJob.Detail.GetJobBuilder().SetJobData(newData).Build();
                await UpdateJob(entityJob, cancel);
            }

            if (entityJob.Detail.ConcurrentExecutionDisallowed)
            {
                await foreach (var other in Triggers.QueryAsync<Trigger>(
                                   filter: item => item.JobGroup == entityJob.PartitionKey &&
                                                   item.JobName == entityJob.RowKey,
                                   cancellationToken: cancel))
                {
                    other.State = other.State switch
                    {
                        Trigger.States.Blocked => Trigger.States.Waiting,
                        Trigger.States.PausedAndBlocked => Trigger.States.Paused,
                        _ => other.State
                    };
                    await UpdateTrigger(other, cancel);
                }

                _signaler.SignalSchedulingChange(null, cancel);
            }
        }

        if (await GetTrigger(trigger.Key, cancel) is not { } entityTrigger)
            return;

        switch (triggerInstCode)
        {
            case SchedulerInstruction.DeleteTrigger:
            {
                var passedHasNext = trigger.GetNextFireTimeUtc().HasValue;
                var persistedHasNext = entityTrigger.Detail.GetNextFireTimeUtc().HasValue;
                if (passedHasNext || !persistedHasNext)
                    await DeleteTrigger(entityTrigger.PartitionKey, entityTrigger.RowKey, cancel);
                break;
            }
            case SchedulerInstruction.SetTriggerComplete:
                entityTrigger.State = Trigger.States.Complete;
                // TODO: Finalized here?
                await UpdateTrigger(entityTrigger, cancel);
                _signaler.SignalSchedulingChange(null, cancel);
                break;
            case SchedulerInstruction.SetTriggerError:
                entityTrigger.State = Trigger.States.Error;
                await UpdateTrigger(entityTrigger, cancel);
                _signaler.SignalSchedulingChange(null, cancel);
                break;
            case SchedulerInstruction.SetAllJobTriggersError:
                await SetTriggerStates(trigger.JobKey.Group, trigger.JobKey.Name, Trigger.States.Error, cancel);
                _signaler.SignalSchedulingChange(null, cancel);
                break;
            case SchedulerInstruction.SetAllJobTriggersComplete:
                await SetTriggerStates(trigger.JobKey.Group, trigger.JobKey.Name, Trigger.States.Complete, cancel);
                _signaler.SignalSchedulingChange(null, cancel);
                break;

            default:
            case SchedulerInstruction.NoInstruction:
            case SchedulerInstruction.ReExecuteJob:
                break;
        }
    }
}