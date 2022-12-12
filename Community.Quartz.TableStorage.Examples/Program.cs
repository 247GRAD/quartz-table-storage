using Community.Quartz.TableStorage;
using Community.Quartz.TableStorage.Entities;
using Community.Quartz.TableStorage.Examples;
using Microsoft.Extensions.Azure;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

// Add local settings if present.
builder.Configuration.AddJsonFile("appsettings.Local.json", true);

// Add Azure services.
builder.Services.AddAzureClients(options =>
{
    // Add table service client.
    options.AddTableServiceClient(builder.Configuration.GetConnectionString("TableStorage"));
});

// Add table job store support. Can configure the options for the job store if needed.
builder.Services.AddTableJobStore();

// Add job scheduler.
builder.Services.AddQuartz(options =>
{
    // Allow creating jobs with DI.
    options.UseMicrosoftDependencyInjectionJobFactory();
    options.UseTableStorePersistence(storeOptions =>
    {
        // Enable clustering.
        storeOptions.UseClustering(clusterOptions =>
            clusterOptions.SetProperty("quartz.scheduler.instanceId", "AUTO"));
    });
});


// Add server integration.
builder.Services.AddQuartzServer(options =>
{
    // Wait for sane state.
    options.AwaitApplicationStarted = true;
    options.WaitForJobsToComplete = true;
});

// Complete building for application, start pipeline configuration.
var app = builder.Build();

// Create scheduler from factory.
var schedulerFactory = app.Services.GetRequiredService<ISchedulerFactory>();
var scheduler = await schedulerFactory.GetScheduler();

// Clear existing data.
await scheduler.Clear();

// Schedule new job and trigger.
await scheduler.ScheduleJob(
    jobDetail: JobBuilder.Create<ExampleJob>()
        .WithIdentity("ExampleJob")
        .WithDescription("Increments it's last count each run")
        .UsingJobData("count", 0)
        .DisallowConcurrentExecution()
        .PersistJobDataAfterExecution()
        .Build(),
    trigger: TriggerBuilder.Create()
        .WithIdentity("ExampleJobRecurring")
        .WithDescription("Runs every other minute")
        .WithCronSchedule("0 */2 * * * ? *")
        .StartNow()
        .Build());

// Redirect to secure protocol.
app.UseHttpsRedirection();

// Primary stack.
app.UseRouting();
app.UseEndpoints(configure =>
{
    // Default go to job get.
    configure.MapGet("/", async context => context.Response.Redirect("/job"));

    // Get job detail and render using details serializer.
    configure.MapGet("/job", async context =>
        await context.Response.WriteAsJsonAsync(
            value: await scheduler.GetJobDetail(new JobKey("ExampleJob"), context.RequestAborted),
            options: Details.JsonOptions,
            cancellationToken: context.RequestAborted
        ));

    // Get trigger and render using details serializer.
    configure.MapGet("/trigger", async context =>
        await context.Response.WriteAsJsonAsync(
            value: await scheduler.GetTrigger(new TriggerKey("ExampleJobRecurring"), context.RequestAborted),
            options: Details.JsonOptions,
            cancellationToken: context.RequestAborted
        ));
});


app.Run();