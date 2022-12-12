using Azure.Data.Tables;
using Community.Quartz.TableStorage.Entities;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;
using Quartz;
using Quartz.Simpl;
using Quartz.Spi;

namespace Community.Quartz.TableStorage;

/// <summary>
/// Job store on an azure storage table.
/// </summary>
public sealed partial class TableJobStore : IJobStore
{
    /// <summary>
    /// Partition key for single-partition entities.
    /// </summary>
    private const string All = "all";

    /// <summary>
    /// Service client providing table access.
    /// </summary>
    private TableServiceClient ServiceClient { get; }

    /// <summary>
    /// Options object.
    /// </summary>
    private TableJobStoreOptions Options { get; }

    /// <summary>
    /// Measurements for time to acquisition.
    /// </summary>
    private Measurements AcquisitionMeasurements { get; } = new(32, 20, 1.5);

    /// <summary>
    /// Locally shared lock. 
    /// </summary>
    private SemaphoreSlim LockLocal { get; } = new(1);

    /// <summary>
    /// Shared lock table.
    /// </summary>
    private TableClient Locks => ServiceClient.GetTableClient(Options.LocksTable);

    /// <summary>
    /// Shared job table.
    /// </summary>
    private TableClient Jobs => ServiceClient.GetTableClient(Options.JobsTable);

    /// <summary>
    /// Shared trigger table.
    /// </summary>
    private TableClient Triggers => ServiceClient.GetTableClient(Options.TriggersTable);

    /// <summary>
    /// Shared calendar table. 
    /// </summary>
    private TableClient Calendars => ServiceClient.GetTableClient(Options.CalendarsTable);

    /// <summary>
    /// The scheduler that should be notified appropriately.
    /// </summary>
    private ISchedulerSignaler _signaler = null!;

    /// <summary>
    /// Internal store of the misfire threshold.
    /// </summary>
    private TimeSpan _misfireThreshold = TimeSpan.FromSeconds(5);


    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public TableJobStore(IAzureClientFactory<TableServiceClient> factory, IOptions<TableJobStoreOptions> options)
    {
        ServiceClient = factory.CreateClient(options.Value.ClientName);
        Options = options.Value;
    }

    /// <summary>
    /// Called by the QuartzScheduler before the <see cref="IJobStore" /> is
    /// used, in order to give the it a chance to Initialize.
    /// </summary>
    public async Task Initialize(
        ITypeLoadHelper loadHelper,
        ISchedulerSignaler signaler,
        CancellationToken cancel)
    {
        _signaler = signaler;
        await Jobs.CreateIfNotExistsAsync(cancel);
        await Triggers.CreateIfNotExistsAsync(cancel);
        await Calendars.CreateIfNotExistsAsync(cancel);

        await Locks.CreateIfNotExistsAsync(cancel);
        await Locks.TryAddEntityAsync(new Lock(All, All), cancel);
    }

    private enum MisfireBehavior
    {
        /// <summary>
        /// No misfire handling was requested or the time aimed for was kept.
        /// </summary>
        Ok,

        /// <summary>
        /// Misfired but next time is unchanged.
        /// </summary>
        Unchanged,

        /// <summary>
        /// The trigger misfired and changed it's next time.
        /// </summary>
        Changed,

        /// <summary>
        /// The trigger misfired and has no new time to fire at.
        /// </summary>
        Descheduled
    }

    /// <summary>
    /// This checks if the allowed misfire threshold is within the planned execution time. If not required or within
    /// the time limit, it stops early. Otherwise, a misfire is communicated, the details are updated, resulting in
    /// an appropriate new next fire time. If there's no new time to fire at, the trigger is marked completed.
    /// </summary>
    /// <param name="detail">The detail to apply for.</param>
    /// <param name="cancel">Cancellation of the operation.</param>
    /// <returns>Returns the kind of misfire handling that was performed.</returns>
    private async Task<MisfireBehavior> ApplyMisfire(IOperableTrigger detail, CancellationToken cancel)
    {
        // This checks if the allowed misfire threshold is within the planned execution time. If not required or within
        // the time limit, it stops early. Otherwise, a misfire is communicated, the details are updated, resulting in
        // an appropriate new next fire time. If there's no new time to fire at, the trigger is marked completed.

        // If misfire instruction is to ignore, stop here.
        if (detail.MisfireInstruction == MisfireInstruction.IgnoreMisfirePolicy)
            return MisfireBehavior.Ok;

        // Get acceptable time.
        var misfireTime = SystemTime.UtcNow();
        if (MisfireThreshold > TimeSpan.Zero)
            misfireTime = misfireTime.AddTicks(-1 * MisfireThreshold.Ticks);

        // Check if not within misfire time. If so, stop here.
        var nextFireTime = detail.GetNextFireTimeUtc();
        if (!nextFireTime.HasValue || nextFireTime > misfireTime)
            return MisfireBehavior.Ok;

        // Notify misfire.
        await _signaler.NotifyTriggerListenersMisfired(detail.Clone(), cancel).ConfigureAwait(false);

        // Try to get calendar for update.
        var calendar = detail.CalendarName == null
            ? null
            : await RetrieveCalendar(detail.CalendarName, cancel);


        // Update details and new fire time.
        detail.UpdateAfterMisfire(calendar);
        var updatedNextFireTime = detail.GetNextFireTimeUtc();


        // Not planned anymore, notify finalize and return removal.
        if (!updatedNextFireTime.HasValue)
        {
            await _signaler.NotifySchedulerListenersFinalized(detail.Clone(), cancel).ConfigureAwait(false);
            return MisfireBehavior.Descheduled;
        }

        // Still planned, return unchanged if the new time is the same as the old time.
        if (updatedNextFireTime == nextFireTime)
            return MisfireBehavior.Unchanged;
        else
            return MisfireBehavior.Changed;
    }

    private async Task SetTriggerStates(string jobGroup, string jobName, Trigger.States state,
        CancellationToken cancel)
    {
        await foreach (var entity in Triggers.QueryAsync<Trigger>(
                           filter: entity => entity.JobGroup == jobGroup &&
                                             entity.JobName == jobName,
                           cancellationToken: cancel))
        {
            entity.State = state;
            await UpdateTrigger(entity, cancel);
        }
    }
}