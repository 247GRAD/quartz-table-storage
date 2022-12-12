namespace Community.Quartz.TableStorage;

/// <summary>
/// Options for <see cref="TableJobStore"/>.
/// </summary>
public class TableJobStoreOptions
{
    /// <summary>
    /// The table client name to use. Defaults to the Azure Client Factory's standard value of <c>"Default"</c>.
    /// </summary>
    /// <example>
    /// Within <c>AddAzureClients</c>, add a client and set it's name via the <c>WithName</c> extension. Set the
    /// <see cref="ClientName"/> to this value.
    /// <code>
    /// options.AddTableServiceClient(CONNECTION_STRING).WithName("Other");
    /// </code>
    /// </example>
    public string ClientName { get; set; } = "Default";

    /// <summary>
    /// Name of the locks table. Defaults to <c>"Locks"</c>.
    /// </summary>
    public string LocksTable { get; set; } = "Locks";

    /// <summary>
    /// Name of the jobs table. Defaults to <c>"Jobs"</c>.
    /// </summary>
    public string JobsTable { get; set; } = "Jobs";

    /// <summary>
    /// Name of the triggers table. Defaults to <c>"Triggers"</c>.
    /// </summary>
    public string TriggersTable { get; set; } = "Triggers";

    /// <summary>
    /// Name of the calendars table. Defaults to <c>"Calendars"</c>.
    /// </summary>
    public string CalendarsTable { get; set; } = "Calendars";
}