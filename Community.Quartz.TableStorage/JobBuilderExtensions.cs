using System.Reflection;
using Quartz;

namespace Community.Quartz.TableStorage;

/// <summary>
/// Adds extensions that can set data directly for use in combination with the generic JSON serializer.
/// </summary>
public static class JobBuilderExtensions
{
    private static readonly FieldInfo JobDataMapField = typeof(JobBuilder).GetField(
        name: "jobDataMap",
        bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance)!;

    /// <summary>
    /// Add the given key-value pair to the JobDetail's <see cref="JobDataMap" />.
    /// </summary>
    /// <param name="receiver">The target builder.</param>
    /// <param name="key">The key of the job data entry.</param>
    /// <param name="value">The value to put.</param>
    /// <returns>The updated JobBuilder.</returns>
    /// <seealso cref="IJobDetail.JobDataMap" />
    public static JobBuilder UsingJobData(this JobBuilder receiver, string key, object value)
    {
        var jobDataMap = (JobDataMap) JobDataMapField.GetValue(receiver)!;
        jobDataMap.Put(key, value);
        return receiver;
    }
}