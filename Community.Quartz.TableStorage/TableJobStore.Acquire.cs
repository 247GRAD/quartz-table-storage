using System.Collections.Immutable;
using System.Diagnostics;
using Community.Quartz.TableStorage.Entities;
using Quartz;
using Quartz.Spi;

namespace Community.Quartz.TableStorage;

// Contains trigger acquisition and release.
public sealed partial class TableJobStore
{
    public async Task<IReadOnlyCollection<IOperableTrigger>> AcquireNextTriggers(
        DateTimeOffset noLaterThan,
        int maxCount,
        TimeSpan timeWindow,
        CancellationToken cancel)
    {
        // Stopwatch for lock and acquisition timing.
        var timestamp = Stopwatch.StartNew();

        await AcquireLock(cancel);
        try
        {
            var now = SystemTime.UtcNow();
            var batchEnd = noLaterThan;
            var result = ImmutableList.CreateBuilder<IOperableTrigger>();
            await foreach (var entity in Triggers.QueryAsync<Trigger>(cancellationToken: cancel))
            {
                // Get job, required.
                if (await GetJob(entity.JobKey, cancel) is not { } jobEntity)
                    continue;

                // Assert state is correct.
                if (entity.State is not Trigger.States.Waiting)
                    continue;

                var nextFireTime = entity.Detail.GetNextFireTimeUtc();
                if (!nextFireTime.HasValue)
                    continue;

                switch (await ApplyMisfire(entity.Detail, cancel))
                {
                    case MisfireBehavior.Ok:
                        break;
                    case MisfireBehavior.Unchanged:
                        await UpdateTrigger(entity, cancel);
                        break;
                    case MisfireBehavior.Changed:
                        await UpdateTrigger(entity, cancel);
                        break;
                    case MisfireBehavior.Descheduled:
                        entity.State = Trigger.States.Complete;
                        await UpdateTrigger(entity, cancel);
                        goto softContinue;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                nextFireTime = entity.Detail.GetNextFireTimeUtc();
                if (nextFireTime > batchEnd)
                    continue;

                if (jobEntity.Detail.ConcurrentExecutionDisallowed)
                {
                    // If any already returned trigger is on this job key, skip.
                    if (result.Any(existing => Equals(entity.JobKey, existing.JobKey)))
                        continue;

                    // If any other trigger already acquired in storage, skip.
                    await foreach (var unused in Triggers.QueryAsync<Trigger>(
                                       filter: item => item.JobGroup == entity.JobGroup &&
                                                       item.JobName == entity.JobName &&
                                                       item.State == Trigger.States.Acquired,
                                       cancellationToken: cancel))
                        // Needs to go to soft continue, as continue here refers to the query for each.
                        goto softContinue;
                }

                // Update detail with fire instance ID, new details and new state.
                entity.Detail.FireInstanceId = Guid.NewGuid().ToString();
                entity.State = Trigger.States.Acquired;
                await UpdateTrigger(entity, cancel);

                // Add to triggers to fire.
                result.Add(entity.Detail);

                if (result.Count == maxCount)
                    break;
                if (result.Count == 1)
                    batchEnd = (now > nextFireTime ? now : nextFireTime.GetValueOrDefault()) + timeWindow;

                // End of loop.
                softContinue: ;
            }

            return result.ToImmutable();
        }
        finally
        {
            // Stop timing and try to add measurement.
            timestamp.Stop();
            AcquisitionMeasurements.Accept((int) timestamp.ElapsedMilliseconds);

            await ReleaseLock();
        }
    }

    public async Task ReleaseAcquiredTrigger(
        IOperableTrigger trigger,
        CancellationToken cancel)
    {
        await AcquireLock(cancel);
        try
        {
            if (await GetTrigger(trigger.Key, cancel) is not {State: Trigger.States.Acquired} entity)
                return;

            entity.Detail.FireInstanceId = string.Empty;
            entity.State = Trigger.States.Waiting;
            await UpdateTrigger(entity, cancel);
        }
        finally
        {
            await ReleaseLock();
        }
    }
}