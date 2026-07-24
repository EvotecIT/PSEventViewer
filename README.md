# PSEventViewer - Modern Windows Event Log Toolkit for .NET and PowerShell

PSEventViewer ships as a PowerShell module from the PowerShell Gallery and as the underlying **EventViewerX** .NET library. Use the module for day-to-day incident response and automation, or drop the library into services and tools that need the same high-performance event pipeline.

PowerShell Gallery

[![powershell gallery version](https://img.shields.io/powershellgallery/v/PSEventViewer.svg)](https://www.powershellgallery.com/packages/PSEventViewer)
[![powershell gallery preview](https://img.shields.io/powershellgallery/v/PSEventViewer.svg?label=powershell%20gallery%20preview&colorB=yellow&include_prereleases)](https://www.powershellgallery.com/packages/PSEventViewer)
[![powershell gallery platforms](https://img.shields.io/powershellgallery/p/PSEventViewer.svg)](https://www.powershellgallery.com/packages/PSEventViewer)
[![powershell gallery downloads](https://img.shields.io/powershellgallery/dt/PSEventViewer.svg)](https://www.powershellgallery.com/packages/PSEventViewer)

Project Information

[![Test .NET](https://github.com/EvotecIT/PSEventViewer/actions/workflows/test-dotnet.yml/badge.svg)](https://github.com/EvotecIT/PSEventViewer/actions/workflows/test-dotnet.yml)
[![Test PowerShell](https://github.com/EvotecIT/PSEventViewer/actions/workflows/test-powershell.yml/badge.svg)](https://github.com/EvotecIT/PSEventViewer/actions/workflows/test-powershell.yml)
[![Coverage](https://img.shields.io/codecov/c/github/EvotecIT/PSEventViewer?branch=master&logo=codecov&label=coverage)](https://codecov.io/gh/EvotecIT/PSEventViewer)
[![license](https://img.shields.io/github/license/EvotecIT/PSEventViewer.svg)](https://github.com/EvotecIT/PSEventViewer)
[![top language](https://img.shields.io/github/languages/top/evotecit/PSEventViewer.svg)](https://github.com/EvotecIT/PSEventViewer)

Author & Social

[![Twitter follow](https://img.shields.io/twitter/follow/PrzemyslawKlys.svg?label=Twitter%20%40PrzemyslawKlys&style=social)](https://twitter.com/PrzemyslawKlys)
[![Blog](https://img.shields.io/badge/Blog-evotec.xyz-2A6496.svg)](https://evotec.xyz/hub)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-pklys-0077B5.svg?logo=LinkedIn)](https://www.linkedin.com/in/pklys)
[![Threads](https://img.shields.io/badge/Threads-@PrzemyslawKlys-000000.svg?logo=Threads&logoColor=White)](https://www.threads.net/@przemyslaw.klys)
[![Discord](https://img.shields.io/discord/508328927853281280?style=flat-square&label=discord%20chat)](https://evo.yt/discord)

## What it's all about

**PSEventViewer** replaces the dated experience of `Get-EventLog` and the verbose XML gymnastics of `Get-WinEvent`. It adds fast parallel queries, curated event packs, intelligent filtering, and real-time watchers while keeping output predictable and script-friendly. The same engine is available as the **EventViewerX** library for C#/.NET applications.

## What we do better than the native tools

- **Multi-threaded, multi-host queries** that automatically chunk large ID lists to avoid Windows API limits.
- **Curated `NamedEvents` packs** (AD changes, Kerberos, AAD Connect, IIS, DHCP, device changes, crashes, and more) so you can ask for scenarios instead of memorising event IDs.
- **Stateful runs** with `RecordIdFile`/`RecordIdKey` resume support for monitoring jobs and schedulers.
- **Structured payloads**: default objects keep named data in dictionaries, `-Expand` flattens them into first-class properties for piping into `Select-Object`/CSV.
- **Offline EVTX & wire-speed filtering** using include/exclude named data filters, regex on messages, and pre-built XPath generation (`Get-EVXFilter`).
- **Log lifecycle management** (`New/Limit/Remove/Clear-EVXLog`, `Remove-EVXSource`, `Write-EVXEntry`) without jumping to `wevtutil` or legacy cmdlets.
- **Real-time watchers** with stop-after, timeout, and staging modes that run scriptblocks on match.

## Components

- **EventViewerX (.NET library)** - Targets `net472`, `net8.0-windows`, and `net10.0-windows`; ships the owned native query/projection/export engine, watcher manager, filter builder, and log management APIs.
- **PSEventViewer (PowerShell module)** - Built on PowerShellStandard.Library 5.1; works on Windows PowerShell 5.1 and PowerShell 7+; exposes the EVX cmdlets and aliases for familiarity with native verbs.
- **Examples** - PowerShell examples live in `Examples/`; C# samples in `Sources/EventViewerX.Examples/` show how to embed the library.

## Coverage

Coverage is uploaded from GitHub Actions test jobs to Codecov; the badge tracks the latest status for the `master` branch. If you see "unknown", rerun tests in GitHub Actions to refresh the report.

## Supported platforms and dependencies

| Component | Target frameworks / Editions | Notes |
| --- | --- | --- |
| EventViewerX library | .NET Framework 4.7.2, .NET 8 for Windows, .NET 10 for Windows | Windows-only. The high-throughput engine calls the Windows Event Log API directly and adds no parser dependency. Other library features use `System.Diagnostics.EventLog`, `System.DirectoryServices`, and `DnsClientX`. |
| PSEventViewer module | Windows PowerShell 5.1, PowerShell 7+ | Ships compiled cmdlets; depends on `PSSharedGoods` plus Microsoft.PowerShell.Management/Utility/Diagnostics. |

## Capabilities at a glance

- Parallel queries across many machines with per-query thread caps.
- Built-in time shortcuts (`-TimePeriod Last24Hours`, `PastMonth`, etc.) and `StartTime`/`EndTime` for precise windows.
- `NamedEvents` scenario packs for AD, Kerberos, AAD Connect, DHCP, Hyper-V, IIS, BitLocker, crash detection, device changes, and more.
- Offline `.evtx` parsing with include/exclude named data filters and message regex.
- `Get-EVXFilter` builds XPath for `Get-WinEvent -FilterXPath` or Event Viewer custom views.
- Real-time watchers (`Start-EVXWatcher`) with stop-after, timeout, staging mode, and pluggable actions.
- Log administration: create/remove logs and sources, size and retention tuning, clear logs, and write events.
- Output shapes: stream or `-AsArray`, rich objects or `-Expand` flattened records for tabular exports.

## NamedEvents catalog (high value scenarios)

| NamedEvents value | What it targets | Typical use |
| --- | --- | --- |
| `ADUserLogon`, `ADUserLogonFailed`, `ADUserLockouts`, `ADUserLogonNTLMv1`, `ADUserPrivilegeUse`, `ADUserUnlocked` | User logon/authentication outcomes | Account investigations, SOC triage |
| `ADUserStatus`, `ADUserRightsAssignment`, `ADUserCreateChange`, `ADUserChangeDetailed` | User lifecycle and rights changes | Access reviews, privilege drift detection |
| `ADGroupMembershipChange`, `ADGroupChange`, `ADGroupChangeDetailed`, `ADGroupCreateDelete`, `ADGroupEnumeration` | Group membership/object lifecycle | Tier-0 group change tracking |
| `ADComputerCreateChange`, `ADComputerDeleted`, `ADComputerChangeDetailed` | Computer objects created/modified/deleted | Join/leave monitoring, stale cleanup |
| `ADGroupPolicyChanges`, `ADGroupPolicyChangesDetailed`, `ADGroupPolicyEdits`, `ADGroupPolicyLinks`, `GpoCreated`, `GpoDeleted`, `GpoModified` | GPO create/edit/link | GPO drift and delegation reviews |
| `ADOrganizationalUnitChangeDetailed`, `ADOtherChangeDetailed`, `ObjectDeletion` | OU/other directory object changes/deletions | Broad directory change detection |
| `ADLdapBindingDetails`, `ADLdapBindingSummary` | LDAP bind activity | Legacy bind detection, DC load monitoring |
| `KerberosServiceTicket`, `KerberosTicketFailure`, `KerberosTGTRequest`, `KerberosPolicyChange` | Kerberos tickets/policy | Lateral movement & ticket abuse hunting |
| `ADSMBServerAuditV1` | SMBv1 access | Legacy protocol detection |
| `NetworkAccessAuthenticationPolicy` | NPS grants/denies | VPN/Wi‑Fi/RADIUS auth troubleshooting |
| `FirewallRuleChange` | Windows Firewall rule edits | Hardening drift monitoring |
| `LogsClearedSecurity`, `LogsClearedOther`, `LogsFullSecurity` | Log clear/full events | Tamper and log exhaustion detection |
| `AuditPolicyChange` | Audit policy edits | Compliance and tamper detection |
| `CertificateIssued` | CA certificate issuance | PKI auditing |
| `DhcpLeaseCreated` | DHCP lease creations | Network access tracing |
| `BitLockerKeyChange`, `BitLockerSuspended` | BitLocker protector changes/suspends | Device compliance monitoring |
| `DeviceRecognized`, `DeviceDisabled` | Device/USB lifecycle | Peripheral policy enforcement |
| `ScheduledTaskCreated`, `ScheduledTaskDeleted` | Scheduled task lifecycle | Persistence/admin change tracking |
| `OSCrash`, `OSBugCheck`, `OSStartup`, `OSShutdown`, `OSUncleanShutdown`, `OSStartupSecurity`, `OSCrashOnAuditFailRecovery`, `OSTimeChange`, `WindowsUpdateFailure` | OS crash/boot/time/patch events | Reliability tracking, post-crash triage |
| `ClientGroupPoliciesApplication`, `ClientGroupPoliciesSystem` | Client-side GPO processing | Workstation policy health |
| `HyperVVirtualMachineStarted`, `HyperVVirtualMachineShutdown`, `HyperVCheckpointCreated` | Hyper-V lifecycle | VM uptime/audit |
| `IISSiteBindingFailure`, `IISSiteStopped` | IIS binding/site state | Web farm readiness checks |
| `ExchangeDatabaseMounted` | Exchange mailbox DB mounted | Exchange availability checks |
| `DfsReplicationError` | DFS-R partner errors | File services health |
| `SqlDatabaseCreated` | SQL DB created | DBA change tracking |
| `SyncCompleted` | Sync/replication completion | General sync monitoring |
| `AADConnectStagingEnabled`, `AADConnectStagingDisabled`, `AADConnectPasswordSyncFailed`, `AADConnectRunProfile`, `AADSyncCycleStage`, `AADSyncProvisionCredentialsPing`, `AADSyncPasswordHashSyncStatus`, `AADSyncImportStatus`, `AADSyncFilterStatus` | Azure AD Connect health signals | Hybrid identity monitoring |
| `NetworkMonitorDriverLoaded`, `NetworkPromiscuousMode` | Packet capture drivers/promiscuous mode | IDS evasion/tooling detection |

Tip: use `Get-EVXEvent -Type <NamedEvents>` to query any of the packs without remembering underlying event IDs. Combine multiple values to cover a scenario set.

## C# quick start (EventViewerX)

```csharp
using EventViewerX;
using System;
using System.Collections.Generic;
using System.Globalization;

// Basic queries
var events = SearchEvents.QueryLog("Security", new List<int> { 4624, 4625 }, machineName: "DC01");

// Parallel across hosts with chunked ID batches
await foreach (var ev in SearchEvents.QueryLogsParallel(
    logName: "Security",
    eventIds: new List<int> { 4624, 4625, 4634, 4647 },
    machineNames: new List<string?> { "DC01", "DC02" },
    maxThreads: Environment.ProcessorCount)) {
    Console.WriteLine($"{ev.MachineName} {ev.Id} {ev.TimeCreated}");
}

// Scenario-based search using NamedEvents packs
var named = SearchEvents.FindEventsByNamedEvents(
    new List<NamedEvents> { NamedEvents.ADUserLockouts, NamedEvents.AADConnectPasswordSyncFailed },
    machineNames: new List<string?> { "AADSYNC01" });

// Dependency-free, bounded native projection from an offline log
var fileQuery = new EventLogFileQuery(@"C:\Logs\Security.evtx") {
    Oldest = true,
    ReadMode = EventReadMode.Message,
    MessageCulture = CultureInfo.GetCultureInfo("en-US")
};
foreach (var ev in EventLogEngine.ReadFile(fileQuery)) {
    Console.WriteLine($"{ev.RecordId}: {ev.Message}");
}

// The same engine reads local or remote channels
var channelQuery = new EventLogChannelQuery("System") {
    MachineName = "DC01",
    XPath = "*[System[(Level=1 or Level=2)]]",
    ReadMode = EventReadMode.Full,
    MessageCulture = CultureInfo.GetCultureInfo("en-US"),
    BufferCapacity = 64
};
foreach (var ev in EventLogEngine.ReadChannel(channelQuery)) {
    Console.WriteLine($"{ev.TimeCreated:u} {ev.ProviderName} {ev.Id}");
}

// Bypass object-pipeline overhead for durable streaming output
EventExportResult export = EventLogExporter.ExportFile(
    fileQuery,
    @"C:\Exports\Security.jsonl",
    EventExportFormat.JsonLines,
    overwrite: true);

// Real-time watcher
var watcher = WatcherManager.StartWatcher(
    name: "logons",
    machineName: Environment.MachineName,
    logName: "Security",
    eventIds: new List<int> { 4624, 4625 },
    namedEvents: new List<NamedEvents>(),
    action: e => Console.WriteLine($"Logon event {e.Id} from {e.MachineName}"),
    numberOfThreads: 4,
    staging: false,
    stopOnMatch: false,
    stopAfter: 0,
    timeout: TimeSpan.FromMinutes(5));

// Write your own events
SearchEvents.WriteEvent("PSEventViewer", "Application", "Health check OK", EventLogEntryType.Information, 1000);
```

## PowerShell quick start (PSEventViewer)

```powershell
# Install
Install-Module -Name PSEventViewer -Scope CurrentUser

# Query AD logons in the last day and flatten payload
Get-EVXEvent -LogName Security -Type ADUserLogon, ADUserLogonFailed -TimePeriod Last24Hours -Expand | `
    Select-Object TimeCreated, MachineName, Id, TargetUserName, IpAddress

# Opt in to bounded PTR enrichment for SMB audit events
Get-EVXEvent -Type ADSMBServerAuditV1 -MachineName AD1,AD2 -ResolveDns -DnsTimeoutMs 1000 -DnsMaxConcurrency 8 | `
    Select-Object When, Computer, ClientAddress, ClientDNSName, ClientDnsResolutionStatus

# Resume a long-running monitor
Get-EVXEvent -LogName Security -EventId 4625 -RecordIdFile "$env:TEMP\evx.state" -RecordIdKey 'security-failures'

# Reset one poller safely instead of deleting only the compatibility file
Reset-EVXEventCheckpoint -Path "$env:TEMP\evx.state" -Key 'security-failures' -PassThru

# Offline EVTX with include/exclude filters
Get-EVXEvent -Path C:\Logs\DC01-Security.evtx -NamedDataFilter @{ TargetUserName = 'alice' } -NamedDataExcludeFilter @{ IpAddress = '10.0.0.1' }

# Direct full export with deterministic English provider messages
Export-EVXEvent -Path C:\Logs\DC01-Security.evtx -OutputPath C:\Exports\Security.jsonl `
    -Format JsonLines -ReadMode Full -MessageCulture en-US -Oldest

# Direct bounded remote export without a PowerShell object-to-file pipeline
Export-EVXEvent -LogName System -MachineName DC01 -OutputPath C:\Exports\DC01-System.csv `
    -Format Csv -ReadMode Message -MessageCulture en-US -BufferCapacity 64

# Raw XML takes the shortest native path and is not affected by ReadMode
Export-EVXEvent -Path C:\Logs\DC01-Security.evtx -OutputPath C:\Exports\Security.xml `
    -Format Xml -Oldest

# Build XPath for Event Viewer / Get-WinEvent
Get-EVXFilter -LogName Security -ID 4624,4625 -UserID 'S-1-5-18' -StartTime (Get-Date).AddDays(-7)

# Real-time watcher with auto-stop
Start-EVXWatcher -MachineName . -LogName Security -EventId 4625 -StopAfter 3 -Action { param($e) $e | Select-Object Id, TimeCreated, TargetUserName }

# Log maintenance
New-EVXLog -LogName 'MyApp' -MachineName .
Set-EVXLogLimit -LogName 'MyApp' -MaximumKilobytes 20480 -OverflowAction OverwriteOlder -RetentionDays 7
Write-EVXEntry -LogName 'MyApp' -Source 'PSEventViewer' -Message 'Started' -EntryType Information -Id 1001
Clear-EVXLog -LogName 'MyApp'
Remove-EVXLog -LogName 'MyApp'
```

## Reading and exporting large event logs

`Get-EVXEvent` streams detached records as they are read. `Export-EVXEvent` takes the shorter path for durable output:
the compiled cmdlet connects the shared C# engine directly to a streaming writer, without sending one PowerShell
object per event through the pipeline. Both surfaces use the same query, projection, culture, and cancellation
contracts.

The default `ReadMode` remains `Full` for compatibility. Choose only the representations the caller needs:

| Read mode | Materialized data | Use it for |
| --- | --- | --- |
| `Metadata` | Core system fields only; no provider message, XML, payload dictionary, attachments, or bookmark | Counting, filtering, timelines, IDs, providers, record-ID checkpoints, and compact exports |
| `Message` | Metadata, provider display names, the provider-formatted message, and bookmark; `MessageData` is parsed only when accessed | Readable text, search, triage, and deterministic-language exports |
| `StructuredData` | Metadata, typed raw properties, XML, parsed named `Data`, and bookmark; no formatted message or decoded attachments | Payload analysis, `-Expand`, and field-based automation |
| `Full` | Message and structured data together, including parsed message fields and decoded binary attachments | Compatibility and consumers that genuinely need every projection |

Provider message rendering is normally the most expensive per-event operation. Filter in XPath by channel, event ID,
provider, time, record ID, level, keyword, or user before requesting `Message` or `Full`. Use `-MessageCulture en-US`
when automation needs deterministic English output instead of the current machine's UI culture. Each event exposes
whether message rendering succeeded; a missing provider resource is not silently confused with an empty message.

```powershell
# Fast bounded-memory metadata scan
Get-EVXEvent -Path C:\Logs\DC01-Security.evtx -Oldest -ReadMode Metadata |
    Select-Object TimeCreated, RecordId, Id, ProviderName, MachineName

# Read only English provider messages from a live channel
Get-EVXEvent -LogName Security -EventId 4624,4625 -TimePeriod Last24Hours `
    -ReadMode Message -MessageCulture en-US |
    Select-Object TimeCreated, Id, MachineName, Message, MessageRenderStatus

# Direct offline export: no PowerShell object-to-file pipeline
Export-EVXEvent -Path C:\Logs\DC01-Security.evtx -OutputPath C:\Exports\Security.jsonl `
    -Format JsonLines -ReadMode Full -MessageCulture en-US -Oldest

# Direct local or remote channel export with bounded buffering
Export-EVXEvent -LogName System -MachineName DC01 -OutputPath C:\Exports\DC01-System.csv `
    -Format Csv -ReadMode Message -MessageCulture en-US -BufferCapacity 64

# Fastest lossless interchange path: raw native event XML
Export-EVXEvent -Path C:\Logs\DC01-Security.evtx -OutputPath C:\Exports\Security.xml `
    -Format Xml -Oldest
```

CSV and JSON Lines honor `ReadMode`. XML intentionally ignores it and streams raw native event XML inside one
well-formed `Events` document. Exports write to a temporary file in the destination directory, flush it, and only then
promote it; cancellation, corrupt input, or rendering failure does not replace an existing destination.

The native engine batches Windows event handles, reuses native and managed buffers, closes each handle promptly, and
keeps remote buffering bounded. It does not load an EVTX file or export into memory. EventViewerX owns this engine and
does not embed EvtxECmd, a Rust parser, or another EVTX parser package. On supported Windows systems it deliberately
uses the operating system's authoritative `wevtapi` parser and provider resources, wrapped by our own projection,
culture, error, streaming, and export contracts. Reimplementing the binary EVTX format would add a second correctness
and maintenance burden without improving the supported Windows contract.

### Reproducible performance and correctness comparisons

The [PowerForge event log benchmark](Benchmarks/EventLogParsing/README.md) keeps unlike workloads separate:

| Comparison class | What is proved |
| --- | --- |
| Common public job | Each public API reads the same event window in the same order. Identity fields are validated, while each API's documented extra work remains visible. |
| Exact raw XML output | DotNet, EventViewerX, PSEventViewer, and `Get-WinEvent` must produce byte-identical UTF-8 XML with the same SHA-256 before timing is accepted. |
| Native output | EventViewerX and EvtxECmd each emit their native CSV, JSON, or XML schema. Event count/order are checked, but different fields and output sizes make this apples-to-oranges throughput evidence. |
| EvtxECmd native workflows | EvtxECmd parse-only, forensic CSV, full JSON, and XML are recorded independently so its own workflow remains visible without pretending it matches a PSEventViewer mode. |

Every public table below is generated from validated PowerForge artifacts using three rotated iterations. The fixture
is a real local `Microsoft-Windows-Hyper-V-Compute-Admin` export captured July 23, 2026: 61,935,616 bytes, 103,405
events, SHA-256 `F933AB900D6B42E7A07A1AE63FC5EC6E4C967A5CFE8010F45B19FC9C3277FCAE`. Metadata, exact-output,
native-output, and EvtxECmd-native cases process all events; common Message, StructuredData, and Full comparisons use
the same first 100,000 events. Times are end-to-end medians and lower is better.

<!-- event-log-common-benchmark:start -->
| Scenario | Host | Operation | PSEventViewer | DotNet | EventViewerX | GetWinEvent | Result |
| --- | --- | --- | ---: | ---: | ---: | ---: | --- |
| Large-Common-Sample-Full | Core-7.6.4 | Scan | 1.00x (10.75s) | 3.14x (33.76s) | 0.82x (8.80s) | 3.70x (39.82s) | PSEventViewer slower than EventViewerX |
| Large-Common-Sample-Message | Core-7.6.4 | Scan | 1.00x (8.40s) | 4.02x (33.81s) | 0.82x (6.92s) | 4.62x (38.82s) | PSEventViewer slower than EventViewerX |
| Large-Common-Sample-StructuredData | Core-7.6.4 | Scan | 1.00x (2.63s) | 0.55x (1.46s) | 0.74x (1.94s) | 6.93x (18.25s) | PSEventViewer slower than DotNet |
| Large-Common-Scan-Metadata | Core-7.6.4 | Scan | 1.00x (1.38s) | 0.68x (931ms) | 0.61x (834ms) | 11.94x (16.45s) | PSEventViewer slower than EventViewerX |
<!-- event-log-common-benchmark:end -->

`StructuredData` is intentionally richer than the raw .NET baseline: EventViewerX materializes typed raw properties,
raw XML, a named payload dictionary, and a bookmark. `Message` and `Full` explicitly request `en-US`; the same
provider-message contract and render status are used for offline, local, and remote reads.

<!-- event-log-exact-output-benchmark:start -->
| Scenario | Host | Operation | PSEventViewer | DotNet | EventViewerXExport | GetWinEvent | Result |
| --- | --- | --- | ---: | ---: | ---: | ---: | --- |
| Large-Exact-Export-MetadataCsv | Core-7.6.4 | Scan | 1.00x (2.50s) | 0.49x (1.21s) | Skipped | 7.77x (19.43s) | PSEventViewer slower than DotNet |
| Large-Exact-Export-RawXml | Core-7.6.4 | Scan | 1.00x (2.20s) | 0.67x (1.47s) | 0.49x (1.09s) | 8.91x (19.63s) | PSEventViewer slower than EventViewerXExport |
<!-- event-log-exact-output-benchmark:end -->

The exact-output validator rejects a run before comparison if any event count, record order, byte sequence, or output
SHA-256 differs. This is the strongest apples-to-apples export comparison.

<!-- event-log-native-output-benchmark:start -->
| Scenario | Host | Operation | EventViewerXExport | EvtxECmd | Result |
| --- | --- | --- | ---: | ---: | --- |
| Large-Native-Output-Csv | Core-7.6.4 | Scan | 1.00x (11.00s) | 0.81x (8.90s) | EventViewerXExport slower than EvtxECmd |
| Large-Native-Output-FullJson | Core-7.6.4 | Scan | 1.00x (12.92s) | 1.05x (13.57s) | EventViewerXExport fastest |
| Large-Native-Output-Xml | Core-7.6.4 | Scan | 1.00x (1.07s) | 10.97x (11.69s) | EventViewerXExport fastest |
<!-- event-log-native-output-benchmark:end -->

The retained outputs make the unequal schemas concrete:

| Format | EventViewerX bytes | EvtxECmd bytes | Interpretation |
| --- | ---: | ---: | --- |
| CSV | 172,626,650 | 56,908,614 | EventViewerX was about 24% slower while writing 3.03 times as many bytes and including its full projection. |
| Full JSON | 257,844,950 | 71,626,402 | EventViewerX was 5% faster while writing 3.60 times as many bytes, including provider-formatted messages. |
| XML | 92,377,794 | 94,483,679 | Similar output volume; EventViewerX was 10.97 times faster. XML layouts differ, so byte identity is proved separately against the Windows APIs above. |

EvtxECmd CSV is a map-enriched forensic schema and its `--fj true` output is raw-event JSON derived from XML.
EventViewerX `Full` JSON also includes provider-formatted messages, typed properties, named payload fields, render
status, and bookmark information. Native CSV/JSON timings therefore must be read together with output byte counts in
the retained PowerForge artifacts; faster time alone does not imply equivalent content.

The EvtxECmd comparison uses version `2026.5.0+bfc7f47ccbf65ffc9a3777cde5498db2fdd94664` (executable SHA-256
`DE169B2AC7F6B1E54A684E0CDDDA30223651937B75941B21EA53A98F5A2502EE`) and its explicit 386-file map set
(manifest SHA-256 `0A6057FCA0E5BD05767177628D4434D6591E1FE8B14B834EED853A07B8FDD9FB`). EvtxECmd is a benchmark
target only; it is not a source, runtime, package, or release dependency.

<!-- event-log-evtx-native-benchmark:start -->
| Scenario | Variables | Operation | Host | OS | RunMode | Engine | Samples | Failures | Median | Mean | P95 | StdDev | Status |
| --- | --- | --- | --- | --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| Large-Evtx-ForensicCsv |  | Scan | Core-7.6.4 | Windows | standard | EvtxECmd | 3 | 0 | 8806.3813 | 8806.7223 | 8865.28666 | 64.9395714796457 | Succeeded |
| Large-Evtx-FullJson |  | Scan | Core-7.6.4 | Windows | standard | EvtxECmd | 3 | 0 | 13424.2192 | 13520.2447 | 13723.14646 | 205.665912410419 | Succeeded |
| Large-Evtx-NativeParse |  | Scan | Core-7.6.4 | Windows | standard | EvtxECmd | 3 | 0 | 7997.5513 | 8026.4488 | 8282.79793 | 274.736641532377 | Succeeded |
| Large-Evtx-Xml |  | Scan | Core-7.6.4 | Windows | standard | EvtxECmd | 3 | 0 | 11258.869 | 11400.2634 | 11665.87357 | 269.55414424195 | Succeeded |
<!-- event-log-evtx-native-benchmark:end -->

All benchmark classes record exact repository state, harness and engine hashes, culture, runtime, fixture provenance,
per-iteration order, event counts, output sizes, output hashes, elapsed time, allocation, and peak working set. Heavy
EVTX and export files are temporary; the small summary and validation artifacts are the retained evidence.

Checkpoint generation metadata is stored in the visible `<RecordIdFile>.state.json` companion file. `Reset-EVXEventCheckpoint` updates both representations under the shared file lock and prevents an in-flight query from restoring progress from the previous generation.

Reverse-DNS enrichment is disabled unless `-ResolveDns` is specified. `-DnsTimeoutMs` bounds the whole lookup, including resolver retries, while `-DnsMaxConcurrency` overlaps lookups without changing event or checkpoint order. A timeout, missing PTR record, or resolver failure is reported through `ClientDnsResolutionStatus` and `ClientDnsResolutionError`; it does not remove the SMB audit event or advance its checkpoint before projection completes.

## Timeouts and long-running queries

- **Defaults (safe/unbounded reads)**: `Settings.SessionTimeoutMs` = 5000 (session open), `Settings.QuerySessionTimeoutMs` = 0 (no stall timeout), `Settings.ListLogWarmupMs` = 3000 (log list warm-up), `Settings.PingTimeoutMs` = 1000, `Settings.RpcProbeTimeoutMs` = 2500.
- **When to use limits**: protect against hung remotes or dead firewalled hosts. Leave `QuerySessionTimeoutMs` at `0` when you need complete log exports.
- **C#**:
  ```csharp
  // Global defaults for this process
  Settings.SessionTimeoutMs = 15_000;
  Settings.QuerySessionTimeoutMs = 30_000; // set to 0 for unlimited reads

  // Per-call override takes precedence over defaults
  var events = SearchEvents.QueryLog(
      "Security",
      sessionTimeoutMs: 45_000,
      machineName: "DC01");
  ```
- **PowerShell**:
  ```powershell
  # Set static defaults for the current session
  [EventViewerX.Settings]::SessionTimeoutMs = 15000
  [EventViewerX.Settings]::QuerySessionTimeoutMs = 30000  # or 0 for unlimited

  # Watchers have their own timeout
  Start-EVXWatcher -LogName Security -EventId 4624,4625 -TimeOut (New-TimeSpan -Minutes 10) -Action { $_.WriteToHost() }
  ```
- **Design intent**: timeouts cap connect time and idle/read stalls; they do not truncate already-returned events. Use small budgets for probes/health checks, and `0` (unbounded) for bulk exports.

## How we're different in practice

- **Productivity**: avoid hand-written XML by generating XPath, using time shortcuts, and calling scenarios by name instead of memorising IDs.
- **Performance**: async/parallel query paths, event ID chunking, and minimal allocations keep queries responsive even on busy Security logs.
- **Safety for long runs**: resume files and per-watcher keys prevent double-processing; watchers include stop-after/timeouts to avoid runaway jobs.
- **Consistency**: the same core runs in both C# and PowerShell, so automation and compiled tools share behaviour, outputs, and bug fixes.

## Where to go next

- Browse PowerShell samples in `Examples/` and C# samples in `Sources/EventViewerX.Examples/`.
- Need a specific filter or scenario? `Get-EVXFilter` and `Get-EVXEvent -Type <NamedEvents>` are the fastest entry points.
- Open an issue or PR if you spot provider differences or missing scenarios—the module translates and normalises common quirks across vendors.
