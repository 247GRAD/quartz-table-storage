using Quartz;

namespace Community.Quartz.TableStorage.Examples;

public class ExampleJob : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        // Get data map for reading old value and writing new value.
        var dataMap = context.JobDetail.JobDataMap;
        
        // Update count.
        var count = dataMap.GetOrDefault<int>("count");
        dataMap.Put("count", count + 1);
        
        // Write count.
        Console.WriteLine($"Updated at {DateTimeOffset.UtcNow:O}: {count} to {count + 1}");
        return Task.CompletedTask;
    }
}