using Quartz;

namespace Community.Quartz.TableStorage;

// Checks of entity existence.
public sealed partial class TableJobStore
{
    public async Task<bool> CalendarExists(string calName, CancellationToken cancel) =>
        await GetCalendar(calName, cancel) is not null;

    public async Task<bool> CheckExists(JobKey jobKey, CancellationToken cancel) =>
        await GetJob(jobKey, cancel) is not null;

    public async Task<bool> CheckExists(TriggerKey triggerKey, CancellationToken cancel) =>
        await GetTrigger(triggerKey, cancel) is not null;
}