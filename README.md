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

- **EventViewerX (.NET library)** - Targets `net472`, `netstandard2.0`, `net8.0`, `net9.0`; Windows-only; ships the query engine, watcher manager, filter builder, and log management APIs.
- **PSEventViewer (PowerShell module)** - Built on PowerShellStandard.Library 5.1; works on Windows PowerShell 5.1 and PowerShell 7+; exposes the EVX cmdlets and aliases for familiarity with native verbs.
- **Examples** - PowerShell examples live in `Examples/`; C# samples in `Sources/EventViewerX.Examples/` show how to embed the library.

## Coverage

Coverage is uploaded from GitHub Actions test jobs to Codecov; the badge tracks the latest status for the `master` branch. If you see "unknown", rerun tests in GitHub Actions to refresh the report.

## Supported platforms and dependencies

| Component | Target frameworks / Editions | Notes |
| --- | --- | --- |
| EventViewerX library | .NET Framework 4.7.2, .NET Standard 2.0, .NET 8, .NET 9 | Windows-only; uses `System.Diagnostics.EventLog` and `System.DirectoryServices`; depends on `DnsClientX` for DNS lookups used in helpers. |
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

Tip: use `Get-EVXEvent -Type <NamedEvents>` to query any of the packs without remembering underlying event IDs. Combine multiple values to cover a scenario set.

## C# quick start (EventViewerX)

```csharp
using EventViewerX;
using System;
using System.Collections.Generic;

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

## Reading large event logs

`Get-EVXEvent` streams records, but the work performed for each record depends on `-ReadMode`. The default remains
`Full` for compatibility. Choose a narrower mode when you do not need every representation of the event.

| Read mode | Materialized data | Use it for |
| --- | --- | --- |
| `Metadata` | Core system fields only; no message, XML, attachments, or bookmark | Counting, filtering, timelines, IDs, providers, record-ID checkpoints, and large exports |
| `Message` | Metadata, provider display names, the provider-formatted message, and bookmark; `MessageData` is parsed only when accessed | Text search and readable provider messages |
| `StructuredData` | Metadata, raw properties, XML, parsed `Data`, and bookmark; no formatted message or decoded attachments | Named event fields, payload analysis, and `-Expand` |
| `Full` | Message and structured data together, including parsed message fields and decoded binary attachments | Compatibility and workflows that need every representation |

Formatting a provider message is often the most expensive per-event operation. Filter by log, event ID, provider,
time, record ID, level, keyword, user, or named data before choosing `Message` or `Full`.

```powershell
# Fast bounded-memory scan: no provider message or XML work
Get-EVXEvent -Path C:\Logs\DC01-Security.evtx -Oldest -ReadMode Metadata |
    Select-Object TimeCreated, RecordId, Id, ProviderName, MachineName |
    Export-Csv C:\Logs\Security-metadata.csv -NoTypeInformation

# Format messages only for the event IDs that need them
Get-EVXEvent -LogName Security -EventId 4624,4625 -TimePeriod Last24Hours -ReadMode Message |
    Select-Object TimeCreated, Id, MachineName, Message

# Parse XML payload fields without paying for provider message formatting
Get-EVXEvent -Path C:\Logs\DC01-Security.evtx -EventId 4624 -ReadMode StructuredData -Expand
```

The equivalent C# API accepts `readMode: EventReadMode.Metadata`, `Message`, `StructuredData`, or `Full`.

### Reproducible parser comparison

The [PowerForge event log benchmark](Benchmarks/EventLogParsing/README.md) separates three kinds of evidence:

| Comparison class | Meaning |
| --- | --- |
| Apples to apples: exact output | The same events and selected fields produce a byte-identical CSV with the same SHA-256. |
| Apples to apples: common user job | The APIs perform the same user-facing read job and validate the same event window, order, and identity checks. Additional PSEventViewer projections remain visible in the metrics. |
| Apples to oranges: native output | EvtxECmd runs its own parser, maps, and fixed output schemas. These rows describe a different forensic workflow and are not presented as interchangeable output. |

EvtxECmd's `--fj true` option is a full **raw-event JSON** export: it converts all available event XML to JSON. It is
not equivalent to PSEventViewer `-ReadMode Full`, which also requests the Windows provider-formatted message and
materializes PSEventViewer's parsed data, message fields, and attachments. PSEventViewer snapshots a bookmark for
every non-metadata mode, so bookmark creation is not a `Full`-only distinction. EvtxECmd's normal CSV is likewise a
25-column map-enriched forensic schema, not the five-column metadata export shown above.

The common-work table is generated from validated PowerForge artifacts. The public refresh command requires at least
three rotated iterations and owns its complete case/engine matrix. This snapshot was captured on July 23, 2026 with
PowerShell 7.6.4 and .NET 10 on Windows, using a 225,513,472-byte `ad.evotec.xyz` Security-log export containing
197,716 events (SHA-256 `3B243CA8A627C99844D713345786B85EE680286DDAE392037387949F6F9FAFB1`). Metadata and exact
export rows process the entire file; message, structured-data, and full rows process the same first 100,000 events.
Times are end-to-end medians, lower is better, and ratios are relative to PSEventViewer.

<!-- event-log-common-benchmark:start -->
| Scenario | Host | Operation | PSEventViewer | DotNet | EventViewerX | GetWinEvent | Result |
| --- | --- | --- | ---: | ---: | ---: | ---: | --- |
| Large-Common-Sample-Full | Core-7.6.4 | Scan | 1.00x (57.62s) | 0.87x (49.88s) | 0.93x (53.34s) | 0.97x (56.05s) | PSEventViewer slower than DotNet |
| Large-Common-Sample-Message | Core-7.6.4 | Scan | 1.00x (55.29s) | 0.86x (47.70s) | 0.95x (52.65s) | 1.01x (55.94s) | PSEventViewer slower than DotNet |
| Large-Common-Sample-StructuredData | Core-7.6.4 | Scan | 1.00x (5.68s) | 0.51x (2.91s) | 0.79x (4.51s) | 4.63x (26.28s) | PSEventViewer slower than DotNet |
| Large-Common-Scan-Metadata | Core-7.6.4 | Scan | 1.00x (2.76s) | 0.74x (2.05s) | 0.79x (2.18s) | 16.54x (45.57s) | PSEventViewer slower than DotNet |
| Large-Exact-Export-MetadataCsv | Core-7.6.4 | Scan | 1.00x (3.86s) | 0.59x (2.28s) | Skipped | 13.12x (50.71s) | PSEventViewer slower than DotNet |
<!-- event-log-common-benchmark:end -->

On this fixture, PSEventViewer metadata enumeration was about 16.5 times faster than `Get-WinEvent`; the byte-identical
metadata CSV export was about 13.1 times faster. Message and full materialization were in the same general range as
`Get-WinEvent`, while the raw .NET reader remained the fastest common-work implementation.

The EvtxECmd-native table is generated separately because metrics-only parsing, its forensic CSV, XML, and full JSON
perform different work and produce different output volumes. This single-iteration operational snapshot used
EvtxECmd `2026.5.0+bfc7f47ccbf65ffc9a3777cde5498db2fdd94664` (executable SHA-256
`DE169B2AC7F6B1E54A684E0CDDDA30223651937B75941B21EA53A98F5A2502EE`) and its explicit 386-file map set (manifest
SHA-256 `0A6057FCA0E5BD05767177628D4434D6591E1FE8B14B834EED853A07B8FDD9FB`) on the same complete fixture. The generated
`Median`, `Mean`, and `P95` values are milliseconds; with one sample they are identical and should not be read as a
statistical distribution.

<!-- event-log-evtx-native-benchmark:start -->
| Scenario | Variables | Operation | Host | OS | RunMode | Engine | Samples | Failures | Median | Mean | P95 | StdDev | Status |
| --- | --- | --- | --- | --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| Large-Evtx-ForensicCsv |  | Scan | Core-7.6.4 | Windows | standard | EvtxECmd | 1 | 0 | 26204.0017 | 26204.0017 | 26204.0017 |  | Succeeded |
| Large-Evtx-FullJson |  | Scan | Core-7.6.4 | Windows | standard | EvtxECmd | 1 | 0 | 44270.1647 | 44270.1647 | 44270.1647 |  | Succeeded |
| Large-Evtx-NativeParse |  | Scan | Core-7.6.4 | Windows | standard | EvtxECmd | 1 | 0 | 21123.9797 | 21123.9797 | 21123.9797 |  | Succeeded |
| Large-Evtx-Xml |  | Scan | Core-7.6.4 | Windows | standard | EvtxECmd | 1 | 0 | 46317.1814 | 46317.1814 | 46317.1814 |  | Succeeded |
<!-- event-log-evtx-native-benchmark:end -->

The benchmark also validates event counts, zero reported errors, nonempty export files, and their SHA-256 hashes. The
retained output sizes from this run make the workload differences concrete:

| EvtxECmd workload | Retained output | Bytes | SHA-256 | Meaning |
| --- | --- | ---: | --- | --- |
| Native parse | None | 0 | — | Parses every payload and internally converts event XML to JSON; not metadata-only work. |
| Forensic CSV | Fixed 25-column CSV | 280,292,810 | `DF6B79F89277C8B3037E36D11F83F0A7A12769C4CA3D2EA68756960C4F97B72A` | Core metadata, map fields, `PayloadData1`-`PayloadData6`, and payload. |
| Full JSON | Raw-event JSON | 256,607,897 | `43DABE58496BC09E72C5592ADCFEC6EFE57529320F5DC814807C2309D9F9ED38` | All available event XML converted to JSON by `--fj true`; no provider-formatted message. |
| XML | Raw-event XML | 293,390,357 | `37CC35D82536B37047B90BD90D128EA3C703E82EC4F91806C756D7B8868F6B4E` | Exported event XML for all 197,716 records. |

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
