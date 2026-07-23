# Event Log Parsing Benchmark

This suite measures event enumeration and projection costs against identical EVTX bytes. It replaces the old
`Example.ComparePerformance.ps1` stopwatch comparison with PowerForge-managed cases, validation, metrics, and artifacts.

The committed smoke fixture contains 184 events. Large fixtures are intentionally external because real Security logs
can exceed 1 GB and may contain sensitive data.

## Engines

- `DotNet`: direct `System.Diagnostics.Eventing.Reader.EventLogReader` enumeration.
- `PropertySelector`: direct `EventLogPropertySelector` projection of eighteen core metadata fields.
- `EventViewerX`: the reusable `SearchEvents.QueryLogFile` engine.
- `PSEventViewer`: the public `Get-EVXEvent` cmdlet consumed by a streaming PowerShell process block.
- `GetWinEvent`: `Get-WinEvent` consumed by the same streaming process-block shape.
- `EvtxECmd`: the official command-line parser in metrics mode, with its own maps and parsing model.
- Optional baseline engines run pinned pre-change EventViewerX/PSEventViewer binaries supplied by path.

`EvtxECmd` is a separate `NativeParse` workload. Its parser and map-enrichment model are not equivalent to Windows
`EventLogReader`, so its result is useful competitor evidence but not a drop-in semantic comparison.

## Workloads

- `Metadata`: core event metadata only.
- `Message`: metadata plus provider display names and formatted message.
- `StructuredData`: metadata, event properties, XML, and EventViewerX structured-data projection.
- `Full`: message and structured-data work together.
- `MetadataCsv`: the five-field metadata projection shown in the public README. `Get-EVXEvent` and `Get-WinEvent`
  stream through PowerShell's `Export-Csv`; the direct .NET lane writes byte-equivalent CSV as a lower bound.
- `NativeParse`: EvtxECmd's native parse/metrics path.
- `EvtxCsv`: EvtxECmd's parser plus its fixed-schema CSV writer. This remains a competitor-specific workload.

Each large fixture also gets deterministic `Sample` cases (100,000 events by default) for expensive message, XML,
and full-projection comparisons. Complete `Scan` cases still cover the entire file.

Every successful lane validates the event count. Additional metrics include internal query time, events per second,
managed allocation, peak working set, message/XML characters, and property count. PowerForge also records end-to-end
duration, which includes child-process startup for public command-line/PowerShell lanes. Run metadata records the
repository head and dirty status, fixture size/hash, built host/module/EventViewerX hashes, every benchmark script
hash, optional baseline binary/dependency hashes, and the EvtxECmd hash. Results therefore remain attributable even
when a developer intentionally benchmarks uncommitted source.

## Run

```powershell
.\Benchmarks\EventLogParsing\Invoke-EventLogParsingBenchmark.ps1 `
    -Case Smoke-Scan-Metadata `
    -Engine DotNet, EventViewerX, GetWinEvent, PSEventViewer `
    -IterationCount 3
```

Run a large external fixture and include EvtxECmd:

```powershell
.\Benchmarks\EventLogParsing\Invoke-EventLogParsingBenchmark.ps1 `
    -LargeFixturePath C:\Temp\Security.evtx `
    -ExpectedLargeCount 1000000 `
    -ExpensiveSampleCount 100000 `
    -EvtxECmdPath C:\Tools\EvtxECmd.exe `
    -Case Large-Scan-Metadata, Large-Sample-Message, Large-Sample-StructuredData, Large-Sample-Full, Large-Export-MetadataCsv, Large-Export-EvtxCsv `
    -IterationCount 1
```

Use `-Plan` to inspect the resolved matrix. Artifacts are written under
`Ignore\Benchmarks\EventLogParsing\Runs` unless `-OutputRoot` is supplied.
