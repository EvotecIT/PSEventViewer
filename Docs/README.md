# Documentation

PSEventViewer is the PowerShell interface. EventViewerX is the reusable .NET
engine used by the module and by C# applications. Pick the guide that matches
the job:

| I need to... | Start here |
| --- | --- |
| Query local, remote, or offline logs from PowerShell | [PowerShell guide](PowerShell-Guide.md#read-events) |
| Choose between metadata, messages, structured data, and raw XML | [PowerShell read modes](PowerShell-Guide.md#choose-a-read-mode) |
| Process or export a very large log | [Large-log workflows](PowerShell-Guide.md#process-large-logs) |
| Resume polling or watch new events | [Checkpoints and watchers](PowerShell-Guide.md#resume-and-watch) |
| Inspect providers/channels or administer logs/WEC | [Administration workflows](PowerShell-Guide.md#inspect-and-administer-windows-event-log) |
| Recover PowerShell script blocks | [PowerShell script recovery](PowerShell-Guide.md#recover-powershell-script-blocks) |
| Write classic or registered manifest events | [Writing events](PowerShell-Guide.md#write-events) |
| Create a provider with named, typed fields | [Custom provider guide](Custom-Providers.md) |
| Use the engine directly from C# | [EventViewerX .NET guide](EventViewerX-Guide.md) |
| Diagnose permissions, remoting, rendering, or provider deployment | [Troubleshooting](Troubleshooting.md) |
| Reproduce the published performance comparisons | [Benchmark contract](../Benchmarks/EventLogParsing/README.md) |

Runnable PowerShell starting points are in [`Examples`](../Examples). Runnable
C# examples are in [`Sources/EventViewerX.Examples`](../Sources/EventViewerX.Examples).
Those files use the same public APIs described by these guides.

## Supported environments

- PSEventViewer: Windows PowerShell 5.1 and PowerShell 7+
- EventViewerX: .NET Framework 4.7.2, .NET 8 for Windows, and .NET 10 for Windows
- Local channels, remote channels through Windows Event Log remoting, offline
  `.evtx` files, and native structured `QueryList` XML

Reading and exporting do not require administrator rights when the current
identity can read the selected source. Channel policy, classic source/log,
collector subscription, and custom-provider installation operations can
require elevation.
