# PSEventViewer PowerShell guide

This guide is organized by task. All examples work in Windows PowerShell 5.1
and PowerShell 7 unless a Windows permission or feature is explicitly required.

```powershell
Install-Module -Name PSEventViewer -Scope CurrentUser
Import-Module PSEventViewer
```

## Read events

### Local channels

Use native filters whenever possible. They reduce the records Windows must
return and the work PowerShell must perform.

```powershell
Get-EVXEvent `
    -LogName Security `
    -EventId 4624, 4625 `
    -StartTime (Get-Date).AddHours(-1) `
    -ReadMode Metadata `
    -MaxEvents 1000
```

Provider-only discovery finds the channels linked to that provider:

```powershell
Get-EVXEvent `
    -ProviderName Microsoft-Windows-Kernel-General `
    -EventId 12 `
    -MaxEvents 10
```

`Get-EVXFilter` produces native QueryList XML by default. Add `-XPathOnly`
when every requested condition can be represented safely as raw Windows Event
Log XPath.

```powershell
Get-EVXFilter `
    -ID 4624, 4625 `
    -StartTime (Get-Date).AddHours(-1) `
    -Level Error, Warning
```

Named-data exclusion uses a native `Suppress` clause so events without the
named field remain in the result:

```powershell
$queryXml = Get-EVXFilter `
    -LogName Security `
    -ID 4624, 4625 `
    -NamedDataExcludeFilter @{
        TargetUserName = 'svc-noisy'
    }

Get-EVXEvent -FilterXml $queryXml -ReadMode StructuredData
```

Do not combine `-NamedDataExcludeFilter` with `-XPathOnly`. The Windows Event
Log XPath subset cannot safely express “exclude this value but keep events
where the field is absent,” so the command fails explicitly instead.

### Get-WinEvent-compatible filter hashtables

The familiar keys `LogName`, `ProviderName`, `Path`, `Id`, `Level`,
`Keywords`, `StartTime`, `EndTime`, `UserId`, and `Data` are supported. Extra
keys are treated as named `EventData` fields.

```powershell
Get-EVXEvent -FilterHashtable @{
    LogName       = 'Security'
    Id            = 4625
    StartTime     = (Get-Date).AddHours(-1)
    TargetUserName = 'alice'
} -MaxEvents 100
```

Use `SuppressHashFilter` when a broad native selection needs native
suppressions:

```powershell
Get-EVXEvent -FilterHashtable @{
    LogName           = 'System'
    StartTime         = (Get-Date).AddDays(-1)
    SuppressHashFilter = @{
        Id = 7036
    }
} -MaxEvents 500
```

Offline provider wildcards that must be expanded before a structured
Select/Suppress query are discovered with metadata-only reads. Discovery is
cancellable and stops at `-MaxEventsScanned`, or at 65,536 records when no
explicit scan bound is supplied. If the file is larger, the command fails
instead of silently compiling an incomplete provider list. Use exact provider
names for the fastest large-file path, or raise `-MaxEventsScanned`
deliberately.

### Structured QueryList XML

Use `-FilterXml` for several paths, explicit Select/Suppress combinations, or
an existing Windows Event Viewer custom view.

```powershell
$query = @'
<QueryList>
  <Query Id="0">
    <Select Path="System">*[System[(Level=1 or Level=2)]]</Select>
    <Suppress Path="System">*[System[EventID=10016]]</Suppress>
  </Query>
</QueryList>
'@

Get-EVXEvent -FilterXml $query -ReadMode Message -MaxEvents 100
```

### Resume from a bookmark

Bookmark creation is opt-in so ordinary large scans do not pay for it. Save
the event's `BookmarkXml` string rather than the framework-specific bookmark
object; the string works in Windows PowerShell 5.1 and PowerShell 7.

```powershell
$last = Get-EVXEvent `
    -LogName System `
    -ReadMode Metadata `
    -IncludeBookmark `
    -MaxEvents 1

$last.BookmarkXml | Set-Content -LiteralPath .\system.bookmark.xml

Get-EVXEvent `
    -LogName System `
    -ReadMode Metadata `
    -BookmarkXml (Get-Content -LiteralPath .\system.bookmark.xml -Raw)
```

The default bookmark offset resumes after the saved event. Use
`-BookmarkOffset 0` when the saved event itself must be returned again.

### Offline EVTX files

```powershell
Get-EVXEvent `
    -Path C:\Logs\Security.evtx `
    -Oldest `
    -ReadMode StructuredData `
    -NamedDataFilter @{ TargetUserName = 'alice' } `
    -MaxEvents 100
```

Several files can be read as one deterministic stream:

```powershell
Get-EVXEvent `
    -Path C:\Logs\DC01-Security.evtx, C:\Logs\DC02-Security.evtx `
    -Oldest `
    -ReadMode Metadata `
    -MaxConcurrency 2
```

### Remote computers

```powershell
Get-EVXEvent `
    -LogName Security `
    -MachineName DC01, DC02 `
    -EventId 4740 `
    -MaxConcurrency 4 `
    -ContinueOnError `
    -MaxEvents 500
```

`-ContinueOnError` isolates a failed source so healthy hosts can continue.
Use `-Credential`, `-Authentication`, `-RemoteConnectionTimeoutMs`, and
`-RemoteReadTimeoutMs` when the default Windows Event Log session is not
appropriate. Results retain both the queried source and the computer recorded
inside the event.

### Named event scenarios

Named scenarios turn common event families into useful typed objects.

```powershell
Get-EVXEvent `
    -Type ADUserLogonFailed, ADUserLockouts `
    -MachineName DC01, DC02 `
    -TimePeriod Last24Hours `
    -MaxEvents 500 |
    Select-Object When, Type, Computer, UserName, IpAddress
```

DNS enrichment is opt-in and bounded:

```powershell
Get-EVXEvent `
    -Type ADSMBServerAuditV1 `
    -MachineName DC01, DC02 `
    -TimePeriod Last3Days `
    -ResolveDns `
    -DnsTimeoutMs 1000 `
    -DnsMaxConcurrency 8
```

See [`Query-NamedEvents.ps1`](../Examples/Query-NamedEvents.ps1).

## Choose a read mode

Select the least expensive mode that contains the fields the next step needs.

| Mode | Materialized data | Typical use |
| --- | --- | --- |
| `Metadata` | System fields only | Counts, IDs, timestamps, provider, record ID, large scans |
| `Message` | Metadata plus formatted provider message and labels | Human-readable reports and alerts |
| `StructuredData` | Metadata, typed payload, named data, and XML; no message formatting | Analysis and structured export |
| `RawXml` | Native event XML | Lowest-overhead, byte-stable interchange |
| `Full` | Message and complete structured projection | Interactive investigation |

Messages can be deterministic instead of following the machine locale:

```powershell
Get-EVXEvent `
    -LogName System `
    -ReadMode Message `
    -MessageCulture en-US `
    -FallbackMessageCulture de-DE `
    -MaxEvents 100
```

The result exposes render status. A missing provider resource is distinguishable
from an event whose message is genuinely empty.

## Process large logs

### Stream instead of accumulating

`Get-EVXEvent` writes records as they are detached from Windows. Process the
pipeline incrementally and avoid wrapping the complete command in `@(...)`
unless all records really must be retained.

```powershell
Get-EVXEvent `
    -Path C:\Logs\Security.evtx `
    -Oldest `
    -ReadMode Metadata |
    ForEach-Object {
        # Process one record. Keep this block small.
        if ($_.Id -eq 4625) {
            $_
        }
    }
```

For aggregation without materializing event objects into a PowerShell array,
use the compiled statistics path:

```powershell
Get-EVXEventStatistics `
    -LogName Security `
    -MaxEvents 100000 `
    -Top 20
```

### Export directly

`Export-EVXEvent` keeps parsing, projection, serialization, buffering, and the
atomic destination-file replacement in compiled code.

```powershell
# Fastest byte-stable interchange.
Export-EVXEvent `
    -Path C:\Logs\Security.evtx `
    -OutputPath C:\Exports\Security.xml `
    -Format Xml `
    -Oldest `
    -Force

# Complete structured records plus deterministic English messages.
Export-EVXEvent `
    -Path C:\Logs\Security.evtx `
    -OutputPath C:\Exports\Security.jsonl `
    -Format JsonLines `
    -ReadMode Full `
    -MessageCulture en-US `
    -Oldest `
    -Force

# Bounded remote export written on the calling computer.
Export-EVXEvent `
    -LogName System `
    -MachineName DC01 `
    -OutputPath C:\Exports\DC01-System.csv `
    -Format Csv `
    -ReadMode Message `
    -BufferCapacity 64 `
    -Force
```

CSV and JSON Lines are projections, so their schemas and byte counts are not
equivalent to another tool unless the selected fields are identical. XML
comparison can use the exact-output contract documented in the
[benchmark guide](../Benchmarks/EventLogParsing/README.md).

Native EVTX export is local-only because Windows creates the file in the target
session. `-ArchiveResources` can retain provider resources needed to render the
archive later.

See [`Export-LargeLog.ps1`](../Examples/Export-LargeLog.ps1).

## Resume and watch

### Durable polling checkpoints

Use a checkpoint when a scheduled task polls a channel repeatedly:

```powershell
Get-EVXEvent `
    -LogName Security `
    -EventId 4625 `
    -RecordIdFile C:\State\FailedLogons.json `
    -RecordIdKey FailedLogons `
    -ReadMode StructuredData
```

Progress is tied to the source and log generation. The engine handles
boundaries without replaying the last accepted event. Reset intentionally:

```powershell
Reset-EVXEventCheckpoint `
    -Path C:\State\FailedLogons.json `
    -Confirm:$false
```

Structured queries can share one checkpoint file without mixing channel record
sequences. Each `Path` in the QueryList receives its own checkpoint entry:

```powershell
[xml] $query = @'
<QueryList>
  <Query Id="0" Path="System">
    <Select Path="System">*[System[Level &lt;= 2]]</Select>
  </Query>
  <Query Id="1" Path="Application">
    <Select Path="Application">*[System[Level &lt;= 2]]</Select>
  </Query>
</QueryList>
'@

Get-EVXEvent `
    -FilterXml $query `
    -RecordIdFile C:\State\CriticalEvents.json `
    -RecordIdKey CriticalEvents `
    -ReadMode StructuredData
```

Use bookmarks when another component owns native bookmark XML. Use checkpoints
for a file-backed polling workflow owned by PSEventViewer.

### Real-time watchers

```powershell
$watcher = Start-EVXWatcher `
    -Name FailedLogons `
    -LogName Security `
    -EventId 4625 `
    -Start Future `
    -ReadMode Full `
    -StopAfter 10 `
    -TimeOut (New-TimeSpan -Minutes 30) `
    -Action {
        param($Event)
        $Event | Select-Object TimeCreated, Id, MachineName, Data
    }

Get-EVXWatcher -Id $watcher.Id
Stop-EVXWatcher -Id $watcher.Id -Confirm:$false
```

Watcher buffers are bounded. Keep the action fast; hand expensive downstream
work to another bounded queue when event arrival can be sustained.

See [`Watch-Events.ps1`](../Examples/Watch-Events.ps1).

## Inspect and administer Windows Event Log

### Providers, channels, and health

```powershell
Get-EVXLog -LogName 'Microsoft-Windows-PowerShell/*' -Force |
    Select-Object LogName, IsEnabled, LogMode, MaximumSizeInBytes

Get-EVXProvider `
    -Name Microsoft-Windows-PowerShell `
    -IncludeEvents

Test-EVXLog -LogName System -MaxEventsToScan 10
```

`Get-EVXProvider -NameOnly` avoids materializing provider metadata when only
discovery is needed. `-IncludeEvents` is the expensive, explicit template
inventory. See [`Inspect-Catalog.ps1`](../Examples/Inspect-Catalog.ps1).

### Manifest channel policy

```powershell
Set-EVXLog `
    -LogName Microsoft-Windows-PowerShell/Operational `
    -Enabled $true `
    -MaximumSizeMB 64 `
    -Mode Circular
```

`Update-EVXLogArchive` archives provider resources beside an EVTX file so
historical messages can be rendered on another machine.

### Classic logs and sources

```powershell
New-EVXLog `
    -LogName Contoso-App `
    -ProviderName Contoso-App-Source `
    -MaximumKilobytes 20480 `
    -OverflowAction OverwriteAsNeeded

New-EVXSource -LogName Contoso-App -SourceName Contoso-Worker

Clear-EVXLog `
    -LogName Contoso-App `
    -BackupPath C:\EventBackups\Contoso-App.evtx `
    -Confirm:$false

Remove-EVXSource `
    -SourceName Contoso-Worker `
    -Confirm:$false

Remove-EVXLog -LogName Contoso-App -Confirm:$false
```

Creating/removing sources and logs changes machine-wide registry state and
normally requires elevation. See [`Manage-Logs.ps1`](../Examples/Manage-Logs.ps1).

### Windows Event Collector

```powershell
Get-EVXCollectorSubscription -Name '*' |
    Select-Object Name, Enabled, ConfigurationMode, DeliveryMode, Query

Set-EVXCollectorSubscription `
    -Name 'Domain Controllers' `
    -Enabled $true `
    -Confirm:$false
```

Inventory can target a remote collector. Updates are intentionally local-only
because the Windows Event Collector write API has no remote-session contract.
See [`Manage-Collector.ps1`](../Examples/Manage-Collector.ps1).

## Recover PowerShell script blocks

Reconstruct script block fragments from Windows PowerShell or PowerShell
operational events:

```powershell
Get-EVXPowerShellScript `
    -Type WindowsPowerShell `
    -MachineName DC01, DC02 `
    -Path C:\RecoveredScripts `
    -MaxScripts 100 `
    -MaxEventsScanned 50000 `
    -MaxPendingScripts 512 `
    -MaxCachedEvents 2048 `
    -IncludeQueryInfo
```

Use `Get-EVXPowerShellScriptExecution` when execution context and event
sequence are needed rather than reconstructed source files:

```powershell
Get-EVXPowerShellScriptExecution `
    -Type WindowsPowerShell `
    -MachineName DC01 `
    -MaxEvents 100 `
    -MaxEventsScanned 50000
```

Use `-EventLogPath` without `-MachineName` to recover from one local exported
operational log. The file is queried exactly once:

```powershell
Get-EVXPowerShellScript `
    -Type WindowsPowerShell `
    -EventLogPath C:\Logs\WindowsPowerShell-Operational.evtx `
    -Path C:\RecoveredScripts `
    -MaxScripts 100
```

The scan limits are intentional resource bounds, not result counts. The engine
uses a one-record lookahead so hitting a limit is reported as truncation only
when another matching record actually exists. See
[`Restore-PowerShellScripts.ps1`](../Examples/Restore-PowerShellScripts.ps1).

## Write events

### Classic Event Log

```powershell
Write-EVXEntry `
    -LogName Application `
    -ProviderName Contoso-App `
    -EventId 1000 `
    -Message 'Service started' `
    -CreateSource `
    -Confirm:$false
```

Source registration is a separate administrative action. `-CreateSource`
makes that opt-in explicit; subsequent writes do not need elevation when the
registered source and log permissions allow them.

### Registered manifest providers

Write an existing provider by ID and ordered payload:

```powershell
Write-EVXEvent `
    -ProviderName Microsoft-Windows-PowerShell `
    -Id 4100 `
    -Payload @('Context', 'User data', 'Payload') `
    -Confirm:$false
```

For an EventViewerX-managed custom provider, write by stable event and field
names:

```powershell
Write-EVXEvent `
    -ProviderName Contoso.Scanner `
    -EventName ScanCompleted `
    -Data @{
        ComputerName = $env:COMPUTERNAME
        FindingCount = 7
    } `
    -Confirm:$false
```

The engine resolves the exact event version, orders named fields according to
the registered schema, converts declared native types, and enforces ETW size
limits. See [Custom providers](Custom-Providers.md) and
[`Write-Events.ps1`](../Examples/Write-Events.ps1).

## Command map

| Job | Commands |
| --- | --- |
| Query/filter | `Get-EVXEvent`, `Get-EVXFilter` |
| Aggregate | `Get-EVXEventStatistics` |
| Direct export | `Export-EVXEvent` |
| Durable progress | `Reset-EVXEventCheckpoint` plus checkpoint parameters on `Get-EVXEvent` |
| Real-time events | `Start-EVXWatcher`, `Get-EVXWatcher`, `Stop-EVXWatcher` |
| Provider/channel catalog | `Get-EVXProvider`, `Get-EVXLog`, `Test-EVXLog` |
| Channel/archive administration | `Set-EVXLog`, `Clear-EVXLog`, `Update-EVXLogArchive` |
| Classic log/source lifecycle | `New-EVXLog`, `Remove-EVXLog`, `New-EVXSource`, `Remove-EVXSource` |
| Collector subscriptions | `Get-EVXCollectorSubscription`, `Set-EVXCollectorSubscription` |
| PowerShell recovery | `Get-EVXPowerShellScript`, `Get-EVXPowerShellScriptExecution` |
| Event writes | `Write-EVXEntry`, `Write-EVXEvent` |
| Provider definitions/packages | `ConvertTo-EVXProviderDefinition`, `Test-EVXProviderDefinition`, `New-EVXProviderPackage`, `Get-EVXProviderPackage`, `Install-EVXProviderPackage`, `Uninstall-EVXProviderPackage` |

Use `Get-Help <command> -Full` for the parameter-level reference generated from
the compiled cmdlet XML documentation.
