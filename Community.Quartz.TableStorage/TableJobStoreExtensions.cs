using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Spi;

namespace Community.Quartz.TableStorage;

/// <summary>
/// Extension methods to add <see cref="TableJobStore"/> functions to the service container.
/// </summary>
public static class TableJobStoreExtensions
{
    /// <summary>
    /// Adds <see cref="TableJobStoreOptions"/>, <see cref="TableJobStore"/>, and <see cref="DetailsObjectSerializer"/>. 
    /// </summary>
    /// <param name="receiver">The container to add to.</param>
    /// <param name="configure">Configures the options.</param>
    /// <returns>Returns the container.</returns>
    public static IServiceCollection AddTableJobStore(
        this IServiceCollection receiver,
        Action<TableJobStoreOptions> configure)
    {
        receiver.AddOptions<TableJobStoreOptions>().Configure(configure);
        receiver.AddSingleton<IJobStore, TableJobStore>();
        receiver.AddSingleton<IObjectSerializer, DetailsObjectSerializer>();
        return receiver;
    }

    /// <summary>
    /// Adds <see cref="TableJobStoreOptions"/>, <see cref="TableJobStore"/>, and <see cref="DetailsObjectSerializer"/>
    /// with default options. 
    /// </summary>
    /// <param name="receiver">The container to add to.</param>
    /// <returns>Returns the container.</returns>
    public static IServiceCollection AddTableJobStore(this IServiceCollection receiver) =>
        receiver.AddTableJobStore(_ => { });

    /// <summary>
    /// Configures the Quartz configurator to use <see cref="TableJobStore"/> persistence and
    /// <see cref="DetailsObjectSerializer"/> for Quartz internal serialization.
    /// </summary>
    /// <remarks>
    /// <see cref="TableJobStoreOptions"/> should be available as options in the service collection.
    /// <see cref="TableJobStore"/> should be available as a singleton in the service collection.
    /// <see cref="DetailsObjectSerializer"/> should be available as a singleton in the service collection.
    /// An <see cref="IAzureClientFactory{TableServiceClient}"/> should be available at singleton level, usually
    /// configured with <see cref="TableClientBuilderExtensions.AddTableServiceClient{TBuilder}(TBuilder,string)"/>.
    /// </remarks>
    /// <param name="receiver">The container to add to.</param>
    /// <param name="configure">Configures the options.</param>
    public static void UseTableStorePersistence(
        this IServiceCollectionQuartzConfigurator receiver,
        Action<SchedulerBuilder.PersistentStoreOptions> configure) =>
        receiver.UsePersistentStore<TableJobStore>(options =>
        {
            options.UseSerializer<DetailsObjectSerializer>();
            configure(options);
        });

    /// <summary>
    /// Configures the Quartz configurator to use <see cref="TableJobStore"/> persistence and
    /// <see cref="DetailsObjectSerializer"/> for Quartz internal serialization. Does not apply further configuration
    /// of the options.
    /// </summary>
    /// <remarks>
    /// <see cref="TableJobStoreOptions"/> should be available as options in the service collection.
    /// <see cref="TableJobStore"/> should be available as a singleton in the service collection.
    /// <see cref="DetailsObjectSerializer"/> should be available as a singleton in the service collection.
    /// An <see cref="IAzureClientFactory{TableServiceClient}"/> should be available at singleton level, usually
    /// configured with <see cref="TableClientBuilderExtensions.AddTableServiceClient{TBuilder}(TBuilder,string)"/>.
    /// </remarks>
    /// <param name="receiver">The container to add to.</param>
    public static void UseTableStorePersistence(this IServiceCollectionQuartzConfigurator receiver) =>
        receiver.UseTableStorePersistence(_ => { });
}