# PSEventViewer and EventViewerX

High-performance Windows Event Log tooling for PowerShell and .NET.

PSEventViewer is the thin PowerShell surface. EventViewerX is the reusable C#
engine underneath it. Both use the Windows Event Log API directly for live,
remote, and offline EVTX work; there is no bundled EVTX parser, EvtxECmd
runtime, Rust engine, or provider-specific parsing dependency.

[![PowerShell Gallery](https://img.shields.io/powershellgallery/v/PSEventViewer.svg)](https://www.powershellgallery.com/packages/PSEventViewer)
[![PowerShell Gallery downloads](https://img.shields.io/powershellgallery/dt/PSEventViewer.svg)](https://www.powershellgallery.com/packages/PSEventViewer)
[![Test .NET](https://github.com/EvotecIT/PSEventViewer/actions/workflows/test-dotnet.yml/badge.svg)](https://github.com/EvotecIT/PSEventViewer/actions/workflows/test-dotnet.yml)
[![Test PowerShell](https://github.com/EvotecIT/PSEventViewer/actions/workflows/test-powershell.yml/badge.svg)](https://github.com/EvotecIT/PSEventViewer/actions/workflows/test-powershell.yml)
[![License](https://img.shields.io/github/license/EvotecIT/PSEventViewer.svg)](https://github.com/EvotecIT/PSEventViewer)

## Why use it

- Stream local channels, remote channels, offline EVTX files, or structured
  QueryList XML without accumulating the complete result.
- Push event ID, provider, time, record ID, level, keyword, user, and event-data
  filtering into the Windows query engine.
- Choose exactly how much work each record needs: metadata, formatted message,
  structured payload, or the complete projection.
- Request deterministic provider messages such as `en-US`, with explicit
  fallback and render status.
- Query several hosts, channels, or files concurrently and merge results in a
  deterministic order with bounded memory.
- Export directly to CSV, JSON Lines, XML, or native EVTX without passing one
  PowerShell object per event through a file pipeline.
- Use native bookmarks, durable record checkpoints, subscriptions, watchers,
  provider and channel catalogs, classic log management, WEC subscription
  management, and both classic and manifest event writing.
- Query named scenarios such as failed logons, lockouts, group changes,
  Kerberos failures, AAD Connect health, IIS failures, and OS crashes.

## Install

```powershell
Install-Module -Name PSEventViewer -Scope CurrentUser
Import-Module PSEventViewer
```

The module supports Windows PowerShell 5.1 and PowerShell 7+. EventViewerX
targets .NET Framework 4.7.2, .NET 8 for Windows, and .NET 10 for Windows.

## PowerShell quick start

```powershell
# Fast system-field scan. No provider message or XML is materialized.
Get-EVXEvent -LogName Security -EventId 4624, 4625 `
    -TimePeriod Last24Hours -ReadMode Metadata -MaxEvents 1000

# Deterministic English messages.
Get-EVXEvent -LogName System -Level 1, 2 `
    -ReadMode Message -MessageCulture en-US -MaxEvents 100

# Provider-only discovery searches all channels linked to the provider.
Get-EVXEvent -ProviderName Microsoft-Windows-Kernel-General `
    -EventId 12 -ReadMode Metadata -MaxEvents 10

# Get-WinEvent-compatible hashtable, including a named EventData key.
Get-EVXEvent -FilterHashtable @{
    LogName       = 'Security'
    Id            = 4625
    StartTime     = (Get-Date).AddHours(-1)
    TargetUserName = 'alice'
} -MaxEvents 100

# Offline EVTX, oldest first.
Get-EVXEvent -Path C:\Logs\Security.evtx -Oldest `
    -ReadMode StructuredData `
    -NamedDataFilter @{ TargetUserName = 'alice' }

# Query several remote hosts. Healthy targets can continue when one fails.
Get-EVXEvent -LogName Security -MachineName DC01, DC02 `
    -EventId 4740 -MaxConcurrency 4 -ContinueOnError -MaxEvents 500

# Query reusable scenario rules.
Get-EVXEvent -Type ADUserLogonFailed, ADUserLockouts `
    -MachineName DC01, DC02 -TimePeriod Last24Hours -MaxEvents 500
```

See the focused scripts in [Examples](Examples/) for live, remote, offline,
export, watcher, catalog, administration, collector, event-writing, and
PowerShell-script-recovery workflows.

## Read modes

The general query default is `Message`: it gives interactive users readable
events without paying for XML and structured payload parsing. Choose an
explicit mode for automation and benchmarks.

| Read mode | Materialized work | Best use |
| --- | --- | --- |
| `Metadata` | System fields only. No message, XML, payload dictionary, attachments, or bookmark unless requested. | Counts, timelines, filtering, record IDs, compact scans. |
| `Message` | Metadata, provider display values, provider-formatted message, and lazy parsed message fields. | Human-readable triage and text search. |
| `StructuredData` | Metadata, typed properties, raw XML, and named/unnamed payload data. No formatted message. | Field automation, `-Expand`, and schema-preserving analysis. |
| `RawXml` | Metadata and raw event XML without provider formatting or typed payload projection. | Lowest-cost XML streaming and custom downstream parsers. |
| `Full` | Message and structured data together, including decoded attachments when present. | Consumers that genuinely need every projection. |

Bookmarks are opt-in with `-IncludeBookmark`. Provider formatting is often the
largest per-record cost; use `Metadata`, `RawXml`, or `StructuredData` when a
formatted message is not needed.

```powershell
# English first, then another installed resource culture if English is absent.
Get-EVXEvent -LogName Application -ReadMode Message `
    -MessageCulture en-US -FallbackMessageCulture de-DE
```

An event exposes message-render status, so a missing provider resource is not
silently treated as a valid empty message.

## Filtering parity

`Get-EVXEvent` supports the natural `Get-WinEvent` query forms:

- `-LogName`, `-ProviderName`, `-Path`, `-FilterXPath`,
  `-FilterHashtable`, and `-FilterXml`;
- arrays and wildcards for channels, providers, and files;
- ID, record ID, time, level, keyword, user, unnamed `Data`, and named
  EventData keys;
- QueryList `Select` and `Suppress` clauses, with truthful per-query
  diagnostics through `-TolerateQueryErrors`;
- credentials and authentication for remote Windows Event Log sessions;
- newest-first or `-Oldest`, `-MaxEvents` as a 64-bit count, and cancellation.

PSEventViewer additionally provides `-NamedDataFilter`,
`-NamedDataExcludeFilter`, `-MessageRegex`, time-period shortcuts,
multi-source concurrency, per-source failure continuation, output expansion,
native bookmarks, and durable checkpoints.

```powershell
$xml = @'
<QueryList>
  <Query Id="0">
    <Select Path="Security">*[System[(EventID=4624 or EventID=4625)]]</Select>
    <Suppress Path="Security">*[EventData[Data[@Name="TargetUserName"]="svc-noisy"]]</Suppress>
  </Query>
</QueryList>
'@

Get-EVXEvent -FilterXml $xml -ReadMode StructuredData -MaxEvents 1000
```

## Large logs and direct export

`Get-EVXEvent` streams detached records. `Export-EVXEvent` is faster for a
durable file because the compiled cmdlet connects the shared engine directly
to the writer.

```powershell
# Lowest-overhead, byte-stable interchange representation.
Export-EVXEvent -Path C:\Logs\Security.evtx `
    -OutputPath C:\Exports\Security.xml -Format Xml -Oldest -Force

# Complete structured output with deterministic English messages.
Export-EVXEvent -Path C:\Logs\Security.evtx `
    -OutputPath C:\Exports\Security.jsonl -Format JsonLines `
    -ReadMode Full -MessageCulture en-US -Oldest -Force

# Bounded remote export written on the caller.
Export-EVXEvent -LogName System -MachineName DC01 `
    -OutputPath C:\Exports\DC01-System.csv -Format Csv `
    -ReadMode Message -MessageCulture en-US -BufferCapacity 64 -Force

# Native EVTX export, with provider resources archived for portability.
Export-EVXEvent -LogName System `
    -OutputPath C:\Exports\System.evtx -Format Evtx `
    -ArchiveResources -Force
```

CSV and JSON Lines honor `ReadMode`. XML streams raw native event XML inside
one well-formed `Events` document. Native EVTX export is local-only because
Windows creates the file in the target session; remote CSV, JSON Lines, and XML
are supported.

Exports write a temporary sibling, flush and optionally hash it, and atomically
promote it only after success. Cancellation or a corrupt input does not replace
an existing destination. Use `-SkipHash` only when another layer already
validates integrity.

## Checkpoints, bookmarks, and real-time events

```powershell
# Durable polling checkpoint. Progress is scoped by source and generation.
Get-EVXEvent -LogName Security -EventId 4625 `
    -RecordIdFile "$env:TEMP\failed-logons.state" `
    -RecordIdKey security-failures

Reset-EVXEventCheckpoint `
    -Path "$env:TEMP\failed-logons.state" `
    -Key security-failures -PassThru

# Bounded native subscription exposed as a PowerShell watcher.
$watcher = Start-EVXWatcher -Name FailedLogons `
    -LogName Security -EventId 4625 -Start Future `
    -StopAfter 10 -TimeOut (New-TimeSpan -Minutes 30) `
    -Action { param($Event) $Event | Select-Object Id, TimeCreated, Data }

Stop-EVXWatcher -Id $watcher.Id -Confirm:$false
```

The C# `EventLogSubscription` uses native `EvtSubscribe`, bounded channels,
real backpressure, explicit start/bookmark behavior, cancellation, and
terminal/non-terminal failure reporting.

## Provider, channel, classic log, and collector administration

```powershell
# Detached provider metadata; -IncludeEvents adds template definitions.
Get-EVXProvider -Name Microsoft-Windows-PowerShell -IncludeEvents
Get-EVXProvider -Name 'Microsoft-Windows-Kernel-*' -NameOnly

# Channel inventory and health probe.
Get-EVXLog -LogName 'Microsoft-Windows-PowerShell/*' -Force
Test-EVXLog -LogName System -MaxEventsToScan 100

# Manifest channel policy.
Set-EVXLog -LogName Microsoft-Windows-PowerShell/Operational `
    -Enabled $true -MaximumSizeMB 64 -Mode Circular

# Classic log and source lifecycle.
New-EVXLog -LogName Contoso-App -ProviderName Contoso-App-Source `
    -MaximumKilobytes 20480 -OverflowAction OverwriteAsNeeded
New-EVXSource -LogName Application -SourceName Contoso-App
Clear-EVXLog -LogName Contoso-App -BackupPath C:\EventBackups
Remove-EVXSource -LogName Application -SourceName Contoso-App
Remove-EVXLog -LogName Contoso-App

# Windows Event Collector inventory and local updates.
Get-EVXCollectorSubscription -Name '*'
Set-EVXCollectorSubscription -Name 'Domain Controllers' `
    -Enabled $true -Confirm:$false
```

Collector inventory can target a remote collector. Updates are deliberately
local-only because the Windows Event Collector write API does not define a
remote session contract.

## Writing events

Classic Event Log sources and manifest/ETW providers are different Windows
contracts, so the module keeps them explicit.

```powershell
# Classic log write. Source creation is an explicit administrative opt-in.
Write-EVXEntry -LogName Application -ProviderName Contoso-App `
    -EventId 1000 -Message 'Service started' -CreateSource

# Registered manifest provider write. Values are validated against the
# provider template and converted to the declared native types.
$result = Write-EVXEvent `
    -ProviderName Microsoft-Windows-PowerShell `
    -Id 4100 `
    -Payload @('Context', 'User data', 'Payload')

$result | Select-Object Success, NativeStatus, PayloadCount, Definition
```

`Write-EVXEvent` resolves an exact provider event/version, rejects ambiguous
versions, validates payload count and types, enforces native ETW size limits,
and owns `EventRegister`, `EventWrite`, and `EventUnregister`. Windows still
decides whether a provider's target channel is enabled.

## Custom providers without SDK work on target machines

EventViewerX can own the complete manifest-provider lifecycle. You describe
named, typed fields once; build one portable `.evxprovider` package on a
developer or CI machine; then install and use that package on ordinary Windows
machines. Targets need PSEventViewer/EventViewerX and administrator rights for
installation, but do not need the Windows SDK, Visual Studio, a C# compiler,
generated source, or a package repository.

### PowerShell: hashtable to working provider

The concise form creates a conventional `<ProviderName>/Operational` channel.
A single event or channel may be one hashtable; arrays are only needed when
there is more than one.

```powershell
$provider = @{
    ProviderName = 'Contoso.Scanner'
    ProviderGuid = '7a87f315-4b5e-40a2-b748-b0cdd8adab41'
    Version      = '1.0.0'
    Events       = @{
        Name    = 'ScanCompleted'
        Id      = 1000
        Message = 'Scan of {ComputerName} found {FindingCount} issues.'
        Fields  = [ordered] @{
            ComputerName = 'String'
            FindingCount = 'UInt32'
        }
    }
}

# See the strongly typed result or validate it before invoking build tools.
$definition = $provider | ConvertTo-EVXProviderDefinition
$definition | Test-EVXProviderDefinition

# Build once. mc.exe, rc.exe, and link.exe are discovered on this machine.
New-EVXProviderPackage `
    -Definition $provider `
    -OutputPath .\Contoso.Scanner-1.0.0.evxprovider `
    -CertificateThumbprint '0123456789ABCDEF0123456789ABCDEF01234567'

# Run elevated on each target. No SDK/compiler is used here.
Install-EVXProviderPackage `
    -Path .\Contoso.Scanner-1.0.0.evxprovider `
    -TrustMode RequireTrustedSignature

# Hashtable order is irrelevant. Names are mapped to canonical manifest order.
Write-EVXEvent `
    -ProviderName Contoso.Scanner `
    -EventName ScanCompleted `
    -Data @{
        FindingCount = 7
        ComputerName = $env:COMPUTERNAME
    }
```

`String`, `Int`, `UInt`, `Long`, `ULong`, `DateTime`, and `Bool` are friendly
field aliases. Native names such as `UnicodeString`, `UInt32`, `FileTime`,
`Guid`, `Sid`, `Binary`, and every supported output/map option remain
available. For advanced providers, the same hashtable/JSON contract includes
explicit channels, localization, levels, tasks, opcodes, keywords, value/bit
maps, event versions, field length/count references, and channel policy.

Use a new event `Version` when field order, names, types, dimensions, maps, or
descriptor metadata changes. The package builder and installer refuse to
rewrite an existing `(EventId, Version)` schema or remove metadata needed to
render old records.

```powershell
# Compile v1.1 only if it remains compatible with the released v1 package.
New-EVXProviderPackage `
    -Definition $providerV11 `
    -BaselinePath .\Contoso.Scanner-1.0.0.evxprovider `
    -OutputPath .\Contoso.Scanner-1.1.0.evxprovider

# Transactional upgrade: verify, stage, unregister v1, register and verify v1.1.
# A failed activation restores v1.
Install-EVXProviderPackage .\Contoso.Scanner-1.1.0.evxprovider

# Active and retained versions remain visible and reinstallable.
Get-EVXProviderPackage |
    Where-Object ProviderName -eq Contoso.Scanner |
    Select-Object ProviderName, PackageVersion, IsActive, IsRegistered, PackagePath

# Keep resources by default so historical EVTX messages still render.
Uninstall-EVXProviderPackage -ProviderName Contoso.Scanner
```

`-RemoveFiles` asks Windows to remove retained schema/resources too. Windows
may briefly keep a message-resource DLL mapped after unregistering; the result
then reports `FileRemovalPendingReboot = $true` and moves/schedules the
retained tree safely instead of leaving it as an active managed version.

### What is inside an `.evxprovider`

The package is a constrained ZIP container:

- `provider.definition.json` is the complete typed source of truth;
- `provider.man` is the generated Windows instrumentation manifest;
- `provider.resources.dll` contains message/schema resources and has no code
  entry point;
- `schema-lock.json` is the compatibility-critical identity snapshot;
- `package.json` binds provider identity, build provenance, and SHA-256 for
  every payload file.

An optional RSA package signature covers `package.json`, including all file
hashes. Package inspection always validates hashes and any present signature.
Installation can allow unsigned packages, require a valid signature, or
require a trusted signer. With `RequireTrustedSignature`, a supplied
`TrustedSignerThumbprint` list is an exact signer allowlist. Without pins, the
certificate must contain the Code Signing EKU and build a trusted Windows
chain. A different chain-valid certificate is therefore not accepted when
publisher pins are configured. This detached package signature is the
`.evxprovider` trust contract; it is separate from Authenticode signing of
PowerShell module files.

```powershell
# Pin a private enterprise signer even if its root is not in the machine store.
Install-EVXProviderPackage .\Contoso.Scanner-1.0.0.evxprovider `
    -TrustMode RequireTrustedSignature `
    -TrustedSignerThumbprint '0123456789ABCDEF0123456789ABCDEF01234567'

# Copy and install with ordinary PowerShell remoting.
$session = New-PSSession -ComputerName APP01
Copy-Item .\Contoso.Scanner-1.0.0.evxprovider `
    -Destination C:\Windows\Temp\Contoso.Scanner.evxprovider `
    -ToSession $session
Invoke-Command -Session $session {
    Import-Module PSEventViewer
    Install-EVXProviderPackage `
        C:\Windows\Temp\Contoso.Scanner.evxprovider `
        -TrustMode RequireTrustedSignature `
        -Confirm:$false
}
```

Every activation is extracted from verified package bytes into a fresh,
restricted directory. `SYSTEM` and Administrators receive full control;
Local Service and local Users receive read/execute only. Installed JSON,
manifests, and resource DLLs are checked against the retained package before
reuse. Lifecycle changes for the same provider GUID are serialized across
processes. If files were modified or somebody removed the registration with
`wevtutil`, installing the exact package repairs it and reports `Repaired`.
The default ProgramData provider root is a dedicated managed security boundary.
A C# caller may select another root, but it must be dedicated to EventViewerX:
installation refuses to claim a directory containing unrelated content, then
sets and verifies the exact ACL throughout the managed tree.

Released versions are immutable by default. For an intentional development
replacement using the same semantic version, opt in explicitly:

```powershell
Install-EVXProviderPackage .\Contoso.Scanner-1.0.0.evxprovider `
    -AllowSameVersionReplacement
```

The replacement receives its own activation directory, the former package is
retained for historical rendering and rollback, and only one package is
reported as `IsActive`. Prefer a new package version for published providers.

### C#: typed schema and typed writes

The C# layer is the owner; PowerShell only adapts hashtables and command
semantics. Public payload properties become named manifest fields. Explicit
attributes make ordering stable across refactors.

```csharp
using EventViewerX;
using EventViewerX.Providers;

public sealed class ScanCompletedPayload {
    [EventProviderPayloadField(0)]
    public string ComputerName { get; init; } = string.Empty;

    [EventProviderPayloadField(1)]
    public uint FindingCount { get; init; }
}

var provider = EventProviderDefinition.Create(
        "Contoso.Scanner",
        Guid.Parse("7a87f315-4b5e-40a2-b748-b0cdd8adab41"),
        "1.0.0")
    .AddChannel(EventProviderChannelDefinition.Operational(
        "Operational",
        "Contoso.Scanner/Operational"));

var scanCompleted =
    EventProviderEventDefinition.FromType<ScanCompletedPayload>(
        "ScanCompleted",
        1000,
        "Operational");
scanCompleted.Messages["en-US"] =
    "Scan of {ComputerName} found {FindingCount} issues.";
provider.AddEvent(scanCompleted);

// Build/CI machine only.
EventProviderPackageBuildResult package =
    EventProviderPackageBuilder.Build(
        provider,
        "Contoso.Scanner-1.0.0.evxprovider");

// Elevated target machine; no build toolchain.
EventProviderPackageManager.Install(package.OutputPath);

// One native registration is cached for high-volume writes.
using var writer = ResolvedManifestEventWriter.Open(
    "Contoso.Scanner",
    "ScanCompleted");
writer.Write(new ScanCompletedPayload {
    ComputerName = Environment.MachineName,
    FindingCount = 7
});
```

Arrays and binary fields must declare an explicit fixed dimension or reference
an earlier numeric count/length property. Typed inference rejects ambiguous
arrays instead of silently publishing a schema that cannot encode its values.
The runnable [custom-provider build script](Examples/Build-CustomProvider.ps1)
and its [complete JSON definition](Examples/CustomProvider.definition.json)
provide copyable starting points.

## C# quick start

```csharp
using EventViewerX;
using EventViewerX.Exports;
using System.Globalization;

var filter = new EventFilter {
    EventIds = new[] { 4624, 4625 },
    StartTime = DateTime.UtcNow.AddHours(-1),
    NamedData = new Dictionary<string, IReadOnlyList<string>> {
        ["TargetUserName"] = new[] { "alice" }
    }
};

var query = new EventLogChannelQuery("Security") {
    XPath = EventFilterCompiler.BuildXPath(filter),
    ReadMode = EventReadMode.StructuredData,
    MaxEvents = 1_000
};

foreach (EventObject item in EventLogEngine.ReadChannel(query)) {
    Console.WriteLine($"{item.RecordId}: {item.Id} {item.ProviderName}");
}

var offline = new EventLogFileQuery(@"C:\Logs\Security.evtx") {
    Oldest = true,
    ReadMode = EventReadMode.Message,
    MessageCulture = CultureInfo.GetCultureInfo("en-US")
};

EventExportResult exported = EventLogExporter.ExportFile(
    offline,
    @"C:\Exports\Security.jsonl",
    EventExportFormat.JsonLines,
    overwrite: true);
```

Multi-source code uses `EventLogBatchQuery` with
`EventLogEngine.ReadBatchAsync`. Scenario code uses `NamedEventQuery` with
`NamedEventEngine.ReadAsync`. Both reuse the same query, native reader,
projection, cancellation, culture, and failure contracts.

```csharp
var namedQuery = new NamedEventQuery(new[] {
    NamedEvents.ADUserLogonFailed,
    NamedEvents.ADUserLockouts
}) {
    MachineNames = new string?[] { "DC01", "DC02" },
    TimePeriod = TimePeriod.Last24Hours,
    MaxConcurrency = 4,
    MaxEvents = 500
};

await foreach (EventObjectSlim item in
               NamedEventEngine.ReadAsync(namedQuery)) {
    Console.WriteLine($"{item.When:u} {item.Type} {item.Computer}");
}
```

## Native PowerShell parity

The goal is parity with the event-log automation contracts that belong in a
library and module, then a stronger reusable surface. A GUI clone of
`Show-EventLog` is intentionally outside that scope.

| Native surface | PSEventViewer/EventViewerX status | Added capability |
| --- | --- | --- |
| `Get-WinEvent` live, remote, path, XPath, hashtable, XML, list log/provider | Covered | Bounded multi-source engine, named data filters, deterministic culture, typed filters, explicit diagnostics, direct exports. |
| `Get-WinEvent` provider messages and raw event data | Covered | Five explicit read modes, render status, event-specific fallback culture, lazy expensive projections. |
| `New-WinEvent` manifest provider writes | Exceeded by `Write-EVXEvent` | Positional compatibility plus named dictionaries, typed payloads, cached registration, package event names, strict schema conversion, structured result. |
| Custom manifest provider authoring and deployment | Additional capability | Typed/hashtable definitions, localization/maps, deterministic SDK build, signed portable package, SDK-free transactional install/upgrade/rollback/uninstall, immutable schema checks. |
| `Get/New/Remove/Clear/Limit/Write-EventLog` classic APIs | Covered by canonical EVX administration cmdlets | Explicit source ownership, verification results, backup-before-clear, consistent local/remote boundaries. |
| `EventLogWatcher` / native subscription | Covered | Bounded backpressure, cancellation, bookmarks, watcher lifecycle and PowerShell actions. |
| `wevtutil` channel/export work | Covered for query, policy, archive, and export | Atomic output, hashes, culture/projection choices, compiled streaming. |
| Windows Event Collector subscriptions | Additional capability | Typed inventory and local mutation with truthful remote limits. |
| Scenario interpretation | Additional capability | Reusable named-event rules and optional bounded DNS enrichment. |

## PowerShell command surface

Version 4 intentionally exposes one canonical command for each responsibility:

| Area | Commands |
| --- | --- |
| Query and export | `Get-EVXEvent`, `Export-EVXEvent`, `Get-EVXFilter`, `Get-EVXEventStatistics` |
| Catalog and diagnostics | `Get-EVXLog`, `Get-EVXProvider`, `Test-EVXLog` |
| Watchers and checkpoints | `Start-EVXWatcher`, `Get-EVXWatcher`, `Stop-EVXWatcher`, `Reset-EVXEventCheckpoint` |
| Log and source administration | `New-EVXLog`, `Set-EVXLog`, `Clear-EVXLog`, `Remove-EVXLog`, `New-EVXSource`, `Remove-EVXSource`, `Update-EVXLogArchive` |
| Event writing | `Write-EVXEntry`, `Write-EVXEvent` |
| Custom providers | `ConvertTo-EVXProviderDefinition`, `Test-EVXProviderDefinition`, `New-EVXProviderPackage`, `Get-EVXProviderPackage`, `Install-EVXProviderPackage`, `Uninstall-EVXProviderPackage` |
| Collector subscriptions | `Get-EVXCollectorSubscription`, `Set-EVXCollectorSubscription` |
| PowerShell recovery | `Get-EVXPowerShellScript`, `Get-EVXPowerShellScriptExecution` |

`Find-WinEvent` is the only retained command alias and maps to
`Get-EVXEvent`.

## Version 4 migration

Version 4 is a deliberate API cleanup:

- C# callers use `EventLogEngine`, `NamedEventEngine`,
  `ClassicEventLogManager`, `EventLogCatalog`, `EventLogSubscription`,
  `EventLogExporter`, and `ManifestEventWriter`; the monolithic
  `SearchEvents` API is removed.
- PowerShell uses the canonical commands above. Historical duplicate aliases
  are removed except `Find-WinEvent`.
- General queries default to `ReadMode Message`, not eager `Full`.
- Bookmarks are opt-in. Durable polling uses explicit checkpoint files/keys.
- `MaxEvents` and counters are 64-bit.
- Payload and parsed message projections are lazy where the chosen mode allows
  it.
- Native EVTX export is local-only; remote CSV, JSON Lines, and XML are
  supported and written locally.

These are breaking changes intended to remove duplicate behavior and make cost,
ownership, and failure boundaries predictable.

## Performance evidence

The [PowerForge benchmark suite](Benchmarks/EventLogParsing/README.md) separates
byte-identical comparisons from common public jobs and different-schema native
exports. Every published table requires at least three rotated iterations plus
event count, order, identity, output size, and hash validation.

These tables used one 231,804,928-byte Security EVTX containing 190,645
readable events
(`FF2F428E0D7DD59EEEA3A5D87477AFFECD87C6541DF417261F21E4B144E7D6AD`).
They ran on the same 32-logical-processor Windows host with .NET SDK 10.0.302
and PowerShell 7.6.4. EvtxECmd was pinned to
`2026.5.0+bfc7f47ccbf65ffc9a3777cde5498db2fdd94664`
(`DE169B2AC7F6B1E54A684E0CDDDA30223651937B75941B21EA53A98F5A2502EE`);
its 386-file maps manifest was also hashed. Generated payloads are deleted
after their size and SHA-256 are validated, while the small summaries and
provenance remain.

<!-- event-log-common-benchmark:start -->
| Scenario | Host | Operation | PSEventViewer | DotNet | EventViewerX | GetWinEvent | Result |
| --- | --- | --- | ---: | ---: | ---: | ---: | --- |
| Large-Common-Sample-Full | Core-7.6.4 | Scan | 1.00x (12.22s) | 4.00x (48.93s) | 1.12x (13.70s) | 4.70x (57.45s) | PSEventViewer fastest |
| Large-Common-Sample-Message | Core-7.6.4 | Scan | 1.00x (10.45s) | 4.67x (48.78s) | 0.81x (8.51s) | 4.89x (51.12s) | PSEventViewer slower than EventViewerX |
| Large-Common-Sample-StructuredData | Core-7.6.4 | Scan | 1.00x (3.25s) | 1.21x (3.94s) | 0.94x (3.04s) | 8.20x (26.63s) | PSEventViewer slower than EventViewerX |
| Large-Common-Scan-Metadata | Core-7.6.4 | Scan | 1.00x (2.54s) | 0.83x (2.10s) | 0.72x (1.82s) | 17.27x (43.90s) | PSEventViewer slower than EventViewerX |
<!-- event-log-common-benchmark:end -->

Common-public-job rows keep the input window and materialization category
equal, but the public APIs can return different object schemas. Exact-output
rows below require identical bytes and SHA-256.

<!-- event-log-exact-output-benchmark:start -->
| Scenario | Host | Operation | Metric | PSEventViewer | DotNet | EventViewerXExport | GetWinEvent | Result |
| --- | --- | --- | --- | ---: | ---: | ---: | ---: | --- |
| Large-Exact-Export-MetadataCsv | Core-7.6.4 | Scan | MedianMs | 1.00x (3.83s) | 0.69x (2.64s) | Skipped | 12.66x (48.46s) | PSEventViewer slower than DotNet |
| Large-Exact-Export-MetadataCsv | Core-7.6.4 | Scan | OutputBytes | 1.00x (19055567) | 1.00x (19055567) | Skipped | 1.00x (19055567) | PSEventViewer baseline |
| Large-Exact-Export-RawXml | Core-7.6.4 | Scan | MedianMs | 1.00x (3.68s) | 1.30x (4.80s) | 0.85x (3.12s) | 13.58x (50.01s) | PSEventViewer slower than EventViewerXExport |
| Large-Exact-Export-RawXml | Core-7.6.4 | Scan | OutputBytes | 1.00x (293062655) | 1.00x (293062655) | 1.00x (293062655) | 1.00x (293062655) | PSEventViewer baseline |
<!-- event-log-exact-output-benchmark:end -->

EventViewerX and EvtxECmd native formats are not interchangeable. Read these
times together with output bytes and fields; do not turn them into an
unqualified speed claim.

<!-- event-log-native-output-benchmark:start -->
| Scenario | Host | Operation | Metric | EventViewerXExport | EvtxECmd | Result |
| --- | --- | --- | --- | ---: | ---: | --- |
| Large-Native-Output-Csv | Core-7.6.4 | Scan | MedianMs | 1.00x (26.46s) | 1.09x (28.73s) | EventViewerXExport fastest |
| Large-Native-Output-Csv | Core-7.6.4 | Scan | OutputBytes | 1.00x (698462495) | 0.46x (318630958) | EventViewerXExport baseline |
| Large-Native-Output-FullJson | Core-7.6.4 | Scan | MedianMs | 1.00x (32.02s) | 1.27x (40.58s) | EventViewerXExport fastest |
| Large-Native-Output-FullJson | Core-7.6.4 | Scan | OutputBytes | 1.00x (915259866) | 0.32x (292846026) | EventViewerXExport baseline |
| Large-Native-Output-Xml | Core-7.6.4 | Scan | MedianMs | 1.00x (2.85s) | 11.15x (31.78s) | EventViewerXExport fastest |
| Large-Native-Output-Xml | Core-7.6.4 | Scan | OutputBytes | 1.00x (293062655) | 1.12x (329124038) | EventViewerXExport baseline |
<!-- event-log-native-output-benchmark:end -->

EventViewerX full JSON includes provider-formatted messages, typed properties,
named data, render status, raw XML, and attachments. The generated
`OutputBytes` rows make that extra work visible instead of hiding it inside an
unqualified timing claim. EvtxECmd is only a pinned external benchmark target
and is not a source, package, or runtime dependency.

<!-- event-log-evtx-native-benchmark:start -->
| Scenario | Host | Operation | Metric | EvtxECmd | Result |
| --- | --- | --- | --- | ---: | --- |
| Large-Evtx-ForensicCsv | Core-7.6.4 | Scan | MedianMs | 1.00x (26.09s) | EvtxECmd only successful |
| Large-Evtx-ForensicCsv | Core-7.6.4 | Scan | OutputBytes | 1.00x (318630958) | EvtxECmd baseline |
| Large-Evtx-FullJson | Core-7.6.4 | Scan | MedianMs | 1.00x (36.26s) | EvtxECmd only successful |
| Large-Evtx-FullJson | Core-7.6.4 | Scan | OutputBytes | 1.00x (292846026) | EvtxECmd baseline |
| Large-Evtx-NativeParse | Core-7.6.4 | Scan | MedianMs | 1.00x (17.53s) | EvtxECmd only successful |
| Large-Evtx-NativeParse | Core-7.6.4 | Scan | OutputBytes | n/a (0) | EvtxECmd baseline |
| Large-Evtx-Xml | Core-7.6.4 | Scan | MedianMs | 1.00x (31.96s) | EvtxECmd only successful |
| Large-Evtx-Xml | Core-7.6.4 | Scan | OutputBytes | 1.00x (329124038) | EvtxECmd baseline |
<!-- event-log-evtx-native-benchmark:end -->

The committed smoke fixture is small and non-sensitive. Large EVTX fixtures
and generated multi-gigabyte outputs remain external and temporary.

## Runtime dependencies

EventViewerX uses the Windows `wevtapi` contract and Microsoft/BCL packages:
`System.Diagnostics.EventLog`, `System.DirectoryServices` for optional Group
Policy enrichment, and compatibility packages required by the .NET Framework
target. PSEventViewer ships the compiled engine and cmdlets; it has no
third-party EVTX parser or PowerShell helper-module dependency.

## Development and release

The root build wrapper delegates versioning, library packaging, module
packaging, signing, artifacts, NuGet, PowerShell Gallery, and GitHub release
coordination to PSPublishModule/PowerForge. EventViewerX and PSEventViewer are
built and released from one version source and validated as packed artifacts.

```powershell
.\Build\Build-Module.ps1 -ConfigurationGateMode Build
```

Browse [the benchmark contract](Benchmarks/EventLogParsing/README.md),
[the named-rule architecture](Sources/EventViewerX/Rules/README-Rules-System.md),
the PowerShell [examples](Examples/), and the C#
[examples](Sources/EventViewerX.Examples/) for deeper integrations.
