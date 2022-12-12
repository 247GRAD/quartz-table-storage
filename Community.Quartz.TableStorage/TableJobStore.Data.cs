using Quartz;
using Quartz.Spi;

namespace Community.Quartz.TableStorage;

// Entity to data resolution.
public sealed partial class TableJobStore
{
    public async Task<IJobDetail?> RetrieveJob(JobKey jobKey, CancellationToken cancel) =>
        (await GetJob(jobKey, cancel))?.Detail;

    public async Task<IOperableTrigger?> RetrieveTrigger(TriggerKey triggerKey,
        CancellationToken cancel) =>
        (await GetTrigger(triggerKey, cancel))?.Detail;

    public async Task<ICalendar?> RetrieveCalendar(string calName, CancellationToken cancel) =>
        (await GetCalendar(calName, cancel))?.Detail;
}