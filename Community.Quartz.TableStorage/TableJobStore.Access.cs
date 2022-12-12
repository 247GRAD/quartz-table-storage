using Azure;
using Azure.Data.Tables;
using Community.Quartz.TableStorage.Entities;
using Quartz;

namespace Community.Quartz.TableStorage;

// Contains internal simplified table access.
public sealed partial class TableJobStore
{
    /// <summary>
    /// Gets the entity by key.
    /// </summary>
    private Task<Job?> GetJob(JobKey key, CancellationToken cancel) =>
        GetJob(key.Group, key.Name, cancel);

    /// <summary>
    /// Gets the entity by key.
    /// </summary>
    private async Task<Job?> GetJob(string group, string name, CancellationToken cancel) =>
        await Jobs.GetEntityOrDefaultAsync<Job>(group, name, null, cancel);

    /// <summary>
    /// Updates the entity in replace mode.
    /// </summary>
    private async Task UpdateJob(Job job, CancellationToken cancel) =>
        await Jobs.UpdateEntityAsync(job, ETag.All, TableUpdateMode.Replace, cancel);

    /// <summary>
    /// Adds the entity with collision detection.
    /// </summary>
    private async Task AddJob(Job job, CancellationToken cancel) =>
        await Jobs.AddEntityAsync(job, cancel);

    /// <summary>
    /// Deletes the entity by key.
    /// </summary>
    private async Task DeleteJob(string group, string name, CancellationToken cancel) =>
        await Jobs.DeleteEntityAsync(group, name, ETag.All, cancel);

    /// <summary>
    /// Gets the entity by key.
    /// </summary>
    private async Task<Trigger?> GetTrigger(string group, string name, CancellationToken cancel) =>
        await Triggers.GetEntityOrDefaultAsync<Trigger>(group, name, null, cancel);

    /// <summary>
    /// Gets the entity by key.
    /// </summary>
    private Task<Trigger?> GetTrigger(TriggerKey key, CancellationToken cancel) =>
        GetTrigger(key.Group, key.Name, cancel);

    /// <summary>
    /// Updates the entity in replace mode.
    /// </summary>
    private async Task UpdateTrigger(Trigger trigger, CancellationToken cancel) =>
        await Triggers.UpdateEntityAsync(trigger, ETag.All, TableUpdateMode.Replace, cancel);

    /// <summary>
    /// Adds the entity with collision detection.
    /// </summary>
    private async Task AddTrigger(Trigger trigger, CancellationToken cancel) =>
        await Triggers.AddEntityAsync(trigger, cancel);

    /// <summary>
    /// Deletes the entity by key.
    /// </summary>
    private async Task DeleteTrigger(string group, string name, CancellationToken cancel) =>
        await Triggers.DeleteEntityAsync(group, name, ETag.All, cancel);

    /// <summary>
    /// Gets the entity by key.
    /// </summary>
    private async Task<Calendar?> GetCalendar(string calName, CancellationToken cancel) =>
        await Calendars.GetEntityOrDefaultAsync<Calendar>(All, calName, null, cancel);

    /// <summary>
    /// Updates the entity in replace mode.
    /// </summary>
    private async Task UpdateCalendar(Calendar calendar, CancellationToken cancel) =>
        await Calendars.UpdateEntityAsync(calendar, ETag.All, TableUpdateMode.Replace, cancel);

    /// <summary>
    /// Adds the entity with collision detection.
    /// </summary>
    private async Task AddCalendar(Calendar calendar, CancellationToken cancel) =>
        await Calendars.AddEntityAsync(calendar, cancel);

    /// <summary>
    /// Deletes the entity by key.
    /// </summary>
    private async Task DeleteCalendar(string name, CancellationToken cancel) =>
        await Calendars.DeleteEntityAsync(All, name, ETag.All, cancel);
}