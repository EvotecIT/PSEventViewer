# EventViewerX .NET guide

EventViewerX is the dependency-free reusable C# engine beneath PSEventViewer.
It targets .NET Framework 4.7.2, .NET 8 for Windows, and .NET 10 for Windows.
The typed APIs own native query construction, filter partitioning, bounded
multi-source reads, projection, message rendering, export, subscriptions,
catalog/administration, and event writing.

```xml
<PackageReference Include="EventViewerX" Version="4.0.0" />
```

The examples below use:

```csharp
using EventViewerX;
using EventViewerX.Providers;
using System.Globalization;
```

## Stream a local channel

```csharp
var filter = new EventFilter {
    EventIds = new[] { 4624, 4625 },
    StartTime = DateTime.UtcNow.AddHours(-1),
    NamedData = new Dictionary<string, IReadOnlyList<string>> {
        ["TargetUserName"] = new[] { "alice" }
    }
};

var options = new EventLogQueryOptions {
    ReadMode = EventReadMode.StructuredData,
    MaxEvents = 1_000
};

foreach (EventObject item in EventLogEngine.ReadChannel(
             "Security",
             filter,
             options: options)) {
    Console.WriteLine(
        $"{item.RecordId}: {item.Id} {item.ProviderName}");
}
```

Enumeration is lazy. Do not call `ToList()` unless the application actually
needs the complete result in memory.

## Read offline files

```csharp
var options = new EventLogQueryOptions {
    Oldest = true,
    ReadMode = EventReadMode.Metadata
};

foreach (EventObject item in EventLogEngine.ReadFiles(
             new[] {
                 @"C:\Logs\DC01-Security.evtx",
                 @"C:\Logs\DC02-Security.evtx"
             },
             options: options)) {
    Console.WriteLine($"{item.TimeCreated:o} {item.Id}");
}
```

`ReadFile` and `ReadFiles` use the same typed filtering and projection
contracts as live channels.

## Resume from a bookmark

Bookmark creation is opt-in. `EventObject.BookmarkXml` exposes the same
portable string on .NET Framework 4.7.2 and modern .NET:

```csharp
EventObject last = EventLogEngine.ReadChannel(
    new EventLogChannelQuery("System") {
        ReadMode = EventReadMode.Metadata,
        IncludeBookmark = true,
        MaxEvents = 1
    }).Single();

var resumedQuery = new EventLogChannelQuery("System") {
    ReadMode = EventReadMode.Metadata,
    BookmarkXml = last.BookmarkXml
};

foreach (EventObject item in EventLogEngine.ReadChannel(resumedQuery)) {
    // Starts after the bookmarked event by default.
}
```

Persist `BookmarkXml`, not the framework bookmark object. Set
`BookmarkOffset = 0` when the bookmarked event should be included again.

## Read several hosts or channels asynchronously

```csharp
using var cancellation = new CancellationTokenSource(
    TimeSpan.FromMinutes(2));

var failures = new List<EventLogQueryFailure>();
var options = new EventLogQueryOptions {
    ReadMode = EventReadMode.Message,
    MessageCulture = CultureInfo.GetCultureInfo("en-US"),
    MaxConcurrency = 4,
    BufferCapacity = 64,
    ContinueOnError = true,
    FailureHandler = failures.Add
};

await foreach (EventObject item in EventLogEngine.ReadChannelsAsync(
                   new[] { "System", "Application" },
                   new string?[] { "DC01", "DC02" },
                   new EventFilter {
                       Levels = new byte[] { 1, 2 }
                   },
                   options,
                   cancellation.Token)) {
    Console.WriteLine(
        $"{item.QueriedMachine} {item.TimeCreated:o} {item.Message}");
}
```

The merge has bounded per-source buffers. `ContinueOnError` allows healthy
sources to continue and reports isolated failures through `FailureHandler`.
Cancellation closes native handles and remote sessions.

## Choose projection cost

`EventReadMode` is a workload contract:

| Mode | Use |
| --- | --- |
| `Metadata` | System fields and high-volume scans |
| `Message` | Metadata plus localized provider rendering |
| `StructuredData` | Typed/named payload and XML without message rendering |
| `RawXml` | Native XML interchange |
| `Full` | Message plus complete structured projection |

For deterministic English output:

```csharp
var options = new EventLogQueryOptions {
    ReadMode = EventReadMode.Message,
    MessageCulture = CultureInfo.GetCultureInfo("en-US"),
    FallbackMessageCulture = CultureInfo.GetCultureInfo("de-DE")
};
```

Consumers should inspect the message render status when an absent provider
resource must be distinguished from an empty message.

## Build advanced queries

`EventFilter` covers IDs, record boundaries, providers, levels, keywords,
time, users, unnamed data, named data, and exclusions. The query factory
partitions filters that exceed the native Windows expression limit and
consolidates equivalent sources:

```csharp
EventLogBatchQuery batch = EventLogQueryFactory.ForChannels(
    new[] { "Security", "System" },
    new string?[] { null, "DC01" },
    new EventFilter {
        EventIds = Enumerable.Range(1, 100).ToArray(),
        ExcludedEventIds = new[] { 42 }
    },
    new EventLogQueryOptions {
        MaxConcurrency = 4,
        ContinueOnError = true
    });

foreach (EventObject item in EventLogBatchEngine.Read(batch)) {
    // One deterministic merged stream.
}
```

Named-data exclusions are translated into native QueryList suppressions:

```csharp
EventLogBatchQuery batch = EventLogQueryFactory.ForChannels(
    new[] { "Security" },
    filter: new EventFilter {
        EventIds = new[] { 4624, 4625 },
        ExcludedNamedData =
            new Dictionary<string, IReadOnlyList<string>> {
                ["TargetUserName"] = new[] { "svc-noisy" }
            }
    });
```

Use `EventLogQueryFactory`, `BuildChannelQueryXml`, or `BuildFileQueryXml`
for this filter. `EventFilterCompiler.BuildXPath` rejects
`ExcludedNamedData` because the native raw-XPath subset cannot keep records
where that field is absent while excluding a matching value.

Use `EventLogStructuredQuery` for an existing native `QueryList` containing
multiple Select/Suppress paths. A native bookmark identifies one independent
query handle, so bookmarked structured reads must target one channel session
or one offline file; split multi-file or mixed-source reads reject one shared
bookmark instead of seeking unrelated sources.

## Export without object-per-record application code

```csharp
var query = new EventLogFileQuery(
    @"C:\Logs\Security.evtx") {
    Oldest = true,
    ReadMode = EventReadMode.Full,
    MessageCulture = CultureInfo.GetCultureInfo("en-US")
};

EventExportResult result = EventLogExporter.ExportFile(
    query,
    @"C:\Exports\Security.jsonl",
    EventExportFormat.JsonLines,
    overwrite: true);

Console.WriteLine(
    $"{result.EventCount} events, {result.Bytes} bytes, {result.Sha256}");
```

`ExportFile`, `ExportChannel`, `ExportStructured`, and `ExportBatch` write to a
temporary destination and atomically publish the completed file. CSV, JSON
Lines, XML, and native EVTX are supported where the underlying Windows API
supports that source/format combination.

## Subscribe to new events

```csharp
var query = new EventLogSubscriptionQuery("Security") {
    XPath = "*[System[EventID=4625]]",
    Start = EventLogSubscriptionStart.Future,
    ReadMode = EventReadMode.StructuredData,
    BufferCapacity = 256
};

using var subscription = new EventLogSubscription(
    query,
    item => Console.WriteLine(
        $"{item.TimeCreated:o} {item.MachineName}"),
    failure => Console.Error.WriteLine(failure.Exception.Message));

Console.ReadLine();
```

Subscriptions support local/remote sessions, future/oldest/bookmark starts,
strict or tolerant bookmarks, bounded handle queues, and cancellation. A
managed watcher remains active while any partitioned native subscription is
healthy and retires itself after the last subscription reports a terminal
failure.
`EventLogSubscriptionQuery.XPath` also accepts QueryList XML when a
subscription needs native `Suppress` clauses; build it with
`EventFilterCompiler.BuildChannelQueryXml`.
The higher-level `WatcherManager` adds named lifecycle, stop-after, timeout,
and named-event projection.

## Use named event rules

```csharp
var query = new NamedEventQuery(new[] {
    NamedEvents.ADUserLogonFailed,
    NamedEvents.ADUserLockouts
}) {
    MachineNames = new string?[] { "DC01", "DC02" }
};

await foreach (EventObjectSlim item in NamedEventEngine.ReadAsync(query)) {
    Console.WriteLine(
        $"{item.Type} {item.EventID} {item.GatheredFrom}");
}
```

Named events are projections over the same engine. They do not require a
different reader or a second copy of query logic.

## Catalog, health, and administration

```csharp
foreach (EventLogDetails log in EventLogCatalog.DisplayEventLogs()) {
    Console.WriteLine(
        $"{log.LogName} Enabled={log.IsEnabled}");
}

EventLogProbeResult health = EventLogProbe.ProbeLatestEvent(
    "System");

ChannelPolicy? policy = EventLogChannelPolicyService.Get(
    "System");

EventLogChannelPolicyService.Apply(new ChannelPolicy {
    LogName = "System",
    MaximumSizeInBytes = 64L * 1024 * 1024,
    Mode = System.Diagnostics.Eventing.Reader.EventLogMode.Circular
});
```

Use `ClassicEventLogManager` for classic logs/sources and
`CollectorSubscriptionManager` for Windows Event Collector inventory and local
updates. Administrative changes normally require elevation.
Managed catalog and administration sessions require a `NetworkCredential` when
`Kerberos`, `Ntlm`, or `Negotiate` is selected explicitly. The current-identity
`EventLogSession` overload cannot enforce a specific authentication package;
use `Default` when current-identity negotiation is acceptable.

## Write events

For classic logs:

```csharp
ClassicEventLogManager.Write(new ClassicEventWriteRequest {
    SourceName = "Contoso-App",
    LogName = "Application",
    Message = "Service started",
    EventId = 1000,
    EntryType = System.Diagnostics.EventLogEntryType.Information
});
```

For a registered manifest provider:

```csharp
using var writer = ResolvedManifestEventWriter.Open(
    "Contoso.Scanner",
    "ScanCompleted");

writer.Write(new Dictionary<string, object?> {
    ["ComputerName"] = Environment.MachineName,
    ["FindingCount"] = 7U
});
```

Reuse one `ResolvedManifestEventWriter` for high-volume writes so the native
provider registration is cached. For compile-time payload contracts, see
[Custom providers](Custom-Providers.md#typed-c-schema-and-writes).

## API selection

| Need | API |
| --- | --- |
| One typed local/remote channel | `EventLogEngine.ReadChannel[Async]` |
| One offline file | `EventLogEngine.ReadFile[Async]` |
| Several channels/hosts/files | `ReadChannels[Async]`, `ReadFiles[Async]` |
| Explicit mixed or structured batch | `EventLogQueryFactory`, `EventLogBatchQuery`, `EventLogBatchEngine` |
| Direct export | `EventLogExporter` |
| Native real-time subscription | `EventLogSubscription` |
| Managed watcher lifecycle | `WatcherManager` |
| Named scenarios | `NamedEventEngine` |
| Catalog and health | `EventLogCatalog`, `EventProviderCatalog`, `EventLogProbe` |
| Administration | `EventLogChannelPolicyService`, `ClassicEventLogManager`, `CollectorSubscriptionManager` |
| Provider packages | `EventProviderPackageBuilder`, `EventProviderPackageManager` |
| Manifest writes | `ManifestEventWriter`, `ResolvedManifestEventWriter` |

Runnable source examples are in
[`Sources/EventViewerX.Examples`](../Sources/EventViewerX.Examples), including
the typed
[`custom-provider lifecycle`](../Sources/EventViewerX.Examples/Examples.CustomProviders.cs).
