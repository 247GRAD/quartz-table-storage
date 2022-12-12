# Quartz Job Persistence on Azure Table Storage

This is an alpha stage of Quartz Job Persistence on Azure Table Storage. It implements the `IJobStore` on top
of an Azure `TableClient`. The implementation supports job persistence, accessing job storage from multiple
instances in clustering mode, non-concurrent job execution, job data persistence, and JSON serialization of
Quartz types.

## Setup

Install the package from NuGet and add the following to your startup. Add the required services:

```csharp
// Add table job store support. Can configure the options for the job store if needed.
builder.Services.AddTableJobStore();
```

Configure Quartz to use table store persistence.

```csharp
// Setup within the Quartz service configuration. 
builder.Services.AddQuartz(options =>
{
    // Use table storage persistence. Clustering can be optionally allowed.
    options.UseTableStorePersistence(storeOptions =>
    {
        // Enable clustering if needed.
        storeOptions.UseClustering(clusterOptions =>
            clusterOptions.SetProperty("quartz.scheduler.instanceId", "AUTO"));
    });
});
```

The job store requires a table client. This can be configured for example like this:

```csharp
// Add Azure services.
builder.Services.AddAzureClients(options =>
{
    // Add table service client.
    options.AddTableServiceClient(builder.Configuration.GetConnectionString("TableStorage"));
});
```

If you have multiple table clients in the application, you can use the `WithName` specification within
the `AddAzureClients` method and configure the job store to use a specific table client from `AddTableJobStore`. For
local testing we suggest `azurite`.

### Example project

Add your connection string as `appsettings.Local.json` with this content:

```json
{
  "ConnectionStrings": {
    "TableStorage": "Your storage account connection string"
  }
}
```

Be advised that the sample clears the scheduling data.

## Functions

Table Job Store supports persistence, persisting job data, non-concurrent execution, and clustering. The time
to trigger acquisition is automatically updated from measurements.

The job data map is not limited to the serializable types. An extension is provided to set any data that can
be JSON serialized. For custom additions to the job data serialization, the `Details.JsonOptions` can be adjusted
or replaced. The options should minimally support `Type`, `CronExpression`, `TimeZoneInfo`, `JobDataMap` and all
Quartz Calendars, Triggers, and Job Detail Implementation.

## Limitations

* Persistence between versions can be allowed by allowing `TypeConverter` to pick non-exact matches for types
  based on full type names. Otherwise, the explicit assembly is required including the public key token if given.

* Pause and resume and group based pause and resumes have not been tested thoroughly. The implementations are based
  on a rough translation of the `RAMJobStore` and might have some discrepancies. Please note any problems with the
  GitHub issues.

* Calendars are not thoroughly tested.

* When clustering is enabled, the Table Job Store globally locks the table for writing operations. We do not have
  a more specific locking method at the moment.

* Examples are limited. We've used the examples project for some test of our own and worked on the code in place
  before committing, therefore only the last sample is present.