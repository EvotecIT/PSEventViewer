---
Module Name: PSEventViewer
Module Guid: 5df72a79-cdf6-4add-b38d-bcacf26fb7bc
Download Help Link: https://github.com/EvotecIT/PSEventViewer
Help Version: 4.0.0
Locale: en-US
---
# PSEventViewer Module
## Description
High-performance Windows Event Log queries, streaming exports, subscriptions, diagnostics, and administration for PowerShell.

## PSEventViewer Cmdlets
### [Clear-EVXLog](Clear-EVXLog.md)
Clears Windows Event Log channels through the native engine.

Supports local or remote channels, explicit credentials, and an optional native EVTX backup. Failures are terminating and retain their Windows error code.

### [ConvertTo-EVXProviderDefinition](ConvertTo-EVXProviderDefinition.md)
Converts a friendly hashtable or custom object into a validated provider definition.

Accepts concise PowerShell aliases such as ProviderName, ProviderGuid, Version, Message, and ordered field hashtables while retaining the complete typed EventViewerX provider schema for advanced channels, levels, tasks, opcodes, keywords, maps, localization, and versioned events.

### [Export-EVXEvent](Export-EVXEvent.md)
Streams Windows events directly to CSV, JSON Lines, XML, or native EVTX.

Uses the EventViewerX native engine and writes directly to the destination without materializing PowerShell objects. Completed output is promoted atomically, so cancellation or failure does not replace an existing file.

### [Get-EVXCollectorSubscription](Get-EVXCollectorSubscription.md)
Returns normalized Windows Event Collector subscription configuration.

Reads local or remote WEC subscription inventory and returns detached snapshots with normalized XML details and query definitions. Remote access uses the caller's Windows identity.

### [Get-EVXEvent](Get-EVXEvent.md)
Enhanced event querying cmdlet that replaces and extends Get-WinEvent functionality.

Supports local/remote logs, named event shortcuts, record ID resumes, parallel queries, and rich filtering (IDs, providers, keywords, levels, time windows, named data).

### [Get-EVXEventStatistics](Get-EVXEventStatistics.md)
Builds bounded statistics from a live Windows event log.

Scans event metadata without formatting messages or parsing XML and reports top event IDs, providers, levels, computers, and the observed time range.

### [Get-EVXFilter](Get-EVXFilter.md)
Generates XPath filters for Windows Event Log queries.

Produces filter strings compatible with Get-WinEvent -FilterXPath and Event Viewer Custom Views, supporting include/exclude IDs, time windows, providers, users, keywords, levels, and named data.

### [Get-EVXLog](Get-EVXLog.md)
Retrieves event log details by name.

Lists log metadata (size, record count, status) on local or remote machines; supports wildcards.

### [Get-EVXPowerShellScript](Get-EVXPowerShellScript.md)
Retrieves PowerShell scripts from event logs and optionally saves them.

### [Get-EVXPowerShellScriptExecution](Get-EVXPowerShellScriptExecution.md)
Retrieves PowerShell execution-context events from live operational logs or exported EVTX files.

### [Get-EVXProvider](Get-EVXProvider.md)
Returns detached Windows Event Log provider metadata.

Supports local and remote provider discovery, wildcard names, deterministic culture, linked channels, levels, tasks, opcodes, keywords, and optional event definitions.

### [Get-EVXProviderPackage](Get-EVXProviderPackage.md)
Inspects a portable provider package or lists EventViewerX-managed installations.

Package inspection verifies declared hashes and any detached signature before returning its typed definition. Without Path, the command returns the active machine-wide EventViewerX provider catalog.

### [Get-EVXWatcher](Get-EVXWatcher.md)
Retrieves information about active EVX watchers.

Filters by watcher Id or Name and returns watcher metadata such as log, machine, filters, and runtime state.

### [Install-EVXProviderPackage](Install-EVXProviderPackage.md)
Installs or upgrades a portable custom Windows event provider package.

Verifies package hashes and signatures before changing machine state, enforces schema and version compatibility, stages resources under ProgramData, registers the manifest, verifies Windows metadata and channels, and rolls back to the previous provider if activation fails.

The target machine does not require the Windows SDK, Visual Studio, a C# compiler, generated source, or package build tools.

### [New-EVXLog](New-EVXLog.md)
Creates a new Windows event log with optional size and retention settings.

Applies explicit desired state through ClassicEventLogManager and reports exactly what changed.

### [New-EVXProviderPackage](New-EVXProviderPackage.md)
Compiles a portable custom Windows event provider package.

Validates the schema, optionally compares a compatibility baseline, compiles the Windows event metadata and localized messages in-process, hashes every file, optionally signs package identity and hashes, and emits one portable .evxprovider file.

No Windows SDK, Visual Studio, native compiler, generated source, or external build tool is required.

### [New-EVXSource](New-EVXSource.md)
Registers a classic Windows Event Log source explicitly.

Creates only the requested source registration and supports provider message, parameter, and category resource files. The command reports whether it created anything.

### [Remove-EVXLog](Remove-EVXLog.md)
Removes an event log from the system.

Supports local or remote removal with ShouldProcess confirmation; useful for cleanup of custom logs.

### [Remove-EVXSource](Remove-EVXSource.md)
Removes an event source from Windows Event Log.

Deletes the provider registration locally or on a remote machine with optional log scoping.

### [Reset-EVXEventCheckpoint](Reset-EVXEventCheckpoint.md)
Resets persisted event-query checkpoint progress safely.

Starts a new checkpoint generation under the shared file lock so an in-flight query from the previous generation cannot restore stale progress. Use this cmdlet instead of deleting only the RecordIdFile compatibility file because generation state is stored in a visible companion .state.json file.

### [Set-EVXCollectorSubscription](Set-EVXCollectorSubscription.md)
Enables or disables an existing local Windows Event Collector subscription.

Uses the supported Windows Event Collector service API, saves the subscription, and verifies the persisted value. Remote registry mutation and wholesale XML replacement are intentionally not exposed.

### [Set-EVXLog](Set-EVXLog.md)
Updates Windows Event Log channel policy.

Configures enabled state, maximum size, retention mode, file path, or security descriptor and returns a detailed per-log result.

### [Start-EVXWatcher](Start-EVXWatcher.md)
Starts real-time monitoring of Windows Event Logs with customizable filters and actions.

Supports explicit event IDs or NamedEvents, provider-side filtering, optional staging events, auto-stop conditions, and a callback for each match.

### [Stop-EVXWatcher](Stop-EVXWatcher.md)
Stops running EVX watchers by identifier, name, or en masse.

Requires exactly one selector and reports missing identifiers or names instead of silently doing nothing. Use PassThru to return each watcher that was stopped.

### [Test-EVXLog](Test-EVXLog.md)
Runs a bounded Windows Event Log connectivity and query probe.

Executes a metadata-only query through the same owned native reader used by Get-EVXEvent, within a fixed budget, and returns the newest matching timestamp plus optional channel metadata.

### [Test-EVXProviderDefinition](Test-EVXProviderDefinition.md)
Validates a custom Windows event provider definition.

Checks provider identity, channels, event versions, field references, maps, localization, Windows limits, and schema compatibility before any native build tools or machine registration are used.

### [Uninstall-EVXProviderPackage](Uninstall-EVXProviderPackage.md)
Unregisters an EventViewerX-managed custom event provider.

Removes the active manifest registration. Package and schema files are retained by default so archived EVTX records remain renderable and the provider can be restored; use RemoveFiles only when that history is no longer required.

### [Update-EVXLogArchive](Update-EVXLogArchive.md)
Archives provider resources into exported EVTX files.

Makes a Windows-native EVTX export self-contained for message rendering on computers that do not have the source provider installed.

### [Write-EVXEntry](Write-EVXEntry.md)
Writes custom events to Windows Event Logs for testing, debugging, or application logging.

Writes through ClassicEventLogManager. A normal write never performs an implicit administrative source registration; use CreateSource explicitly when that behavior is intended.

### [Write-EVXEvent](Write-EVXEvent.md)
Writes a registered manifest/ETW event using positional, named, or typed schema values.

Resolves and caches the exact registered event schema, validates every value, converts values according to native Windows types, and writes through the dependency-free EventViewerX engine. Named hashtable order does not matter.

EventName is available for providers installed through an EventViewerX .evxprovider package. ProviderName plus Id works with any registered manifest provider. Use Write-EVXEntry for classic Event Log sources.
