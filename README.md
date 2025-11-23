# PSEventViewer - Modern Windows Event Log Toolkit for .NET and PowerShell

PSEventViewer ships as a PowerShell module from the PowerShell Gallery and as the underlying **EventViewerX** .NET library. Use the module for day-to-day incident response and automation, or drop the library into services and tools that need the same high-performance event pipeline.

PowerShell Gallery

[![powershell gallery version](https://img.shields.io/powershellgallery/v/PSEventViewer.svg)](https://www.powershellgallery.com/packages/PSEventViewer)
[![powershell gallery preview](https://img.shields.io/powershellgallery/vpre/PSEventViewer.svg?label=powershell%20gallery%20preview&colorB=yellow)](https://www.powershellgallery.com/packages/PSEventViewer)
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
| `ADUserLogon`, `ADUserLogonFailed`, `ADUserLockouts` | User logons, failures, lockouts | Account investigations, SOC triage |
| `ADUserStatus`, `ADUserRightsAssignment` | Enable/disable, rights assigned/removed | Access reviews, privilege change detection |
| `ADUserPrivilegeUse`, `ADUserUnlocked` | Privilege elevation, unlocks | Elevated access monitoring |
| `ADGroupMembershipChange`, `ADGroupChangeDetailed` | Group membership and object changes | Tier-0 group change tracking |
| `ADComputerCreateChange`, `ADComputerDeleted` | Computer object lifecycle | Join/leave monitoring, stale cleanup |
| `ADGroupPolicyChanges`, `ADGroupPolicyChangesDetailed`, `ADGroupPolicyLinks` | GPO create/modify/link events | GPO drift detection |
| `KerberosServiceTicket`, `KerberosTicketFailure`, `KerberosTGTRequest` | Kerberos ticket requests/failures | Lateral movement / Golden Ticket hunting |
| `ADLdapBindingDetails`, `ADLdapBindingSummary` | LDAP binds | Legacy bind and DC load monitoring |
| `AADConnectPasswordSyncFailed`, `AADConnectRunProfile`, `AADConnectStagingEnabled` | Azure AD Connect sync health | Hybrid identity monitoring |
| `NetworkAccessAuthenticationPolicy` | NPS grants/denies | VPN/Wi-Fi auth troubleshooting |
| `FirewallRuleChange` | Windows Firewall rule edits | Workstation/server hardening drift |
| `OSCrash`, `OSBugCheck`, `OSUncleanShutdown`, `OSStartupSecurity` | System crash/boot/shutdown | Reliability tracking, post-crash triage |
| `IISSiteBindingFailure`, `IISSiteStopped` | IIS binding and site state issues | Web farm readiness checks |
| `BitLockerKeyChange`, `BitLockerSuspended` | BitLocker protector changes/suspends | Device compliance monitoring |
| `DeviceRecognized`, `DeviceDisabled` | Plug-and-play device lifecycle | USB and device policy auditing |
| `LogsClearedSecurity`, `LogsFullSecurity` | Security log cleared or full | Tamper detection |
| `NetworkMonitorDriverLoaded`, `NetworkPromiscuousMode` | Packet capture drivers/promiscuous mode | IDS evasion/tooling detection |
| `DfsReplicationError` | DFS-R replication issues | File services health |
| `SqlDatabaseCreated` | SQL database created | DBA change tracking |
| `HyperVVirtualMachineStarted`, `HyperVVirtualMachineShutdown`, `HyperVCheckpointCreated` | Hyper-V lifecycle | VM uptime/audit |
| `WindowsUpdateFailure` | Update failures | Patch compliance follow-up |
| `ScheduledTaskCreated`, `ScheduledTaskDeleted` | Scheduled task changes | Persistence and admin change tracking |
| `ObjectDeletion` | Arbitrary object deletions | Broad deletion detection |

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
Get-EVXEvent -LogName Security -Type ADUserLogon, ADUserLogonFailed -TimePeriod Last24Hours -Expand | \
    Select-Object TimeCreated, MachineName, Id, TargetUserName, IpAddress

# Resume a long-running monitor
Get-EVXEvent -LogName Security -EventId 4625 -RecordIdFile "$env:TEMP\evx.state" -RecordIdKey 'security-failures'

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

## How we're different in practice

- **Productivity**: avoid hand-written XML by generating XPath, using time shortcuts, and calling scenarios by name instead of memorising IDs.
- **Performance**: async/parallel query paths, event ID chunking, and minimal allocations keep queries responsive even on busy Security logs.
- **Safety for long runs**: resume files and per-watcher keys prevent double-processing; watchers include stop-after/timeouts to avoid runaway jobs.
- **Consistency**: the same core runs in both C# and PowerShell, so automation and compiled tools share behaviour, outputs, and bug fixes.

## Where to go next

- Browse PowerShell samples in `Examples/` and C# samples in `Sources/EventViewerX.Examples/`.
- Need a specific filter or scenario? `Get-EVXFilter` and `Get-EVXEvent -Type <NamedEvents>` are the fastest entry points.
- Open an issue or PR if you spot provider differences or missing scenarios—the module translates and normalises common quirks across vendors.
