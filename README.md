# PSEventViewer has moved

Active development now lives in
[`EvotecIT/EventViewerX`](https://github.com/EvotecIT/EventViewerX).

The PowerShell module is still named `PSEventViewer`, so existing installation
and import commands do not change:

```powershell
Install-Module -Name PSEventViewer -Scope CurrentUser
Import-Module PSEventViewer
```

## Why the repository moved

PSEventViewer, PSWinReporting, and PSWinReportingV2 all evolved around Windows
Event Log querying, reporting, and automation. Keeping that work in separate
repositories made ownership and release planning harder than it needed to be.

In August 2026, the PSWinReporting lines were frozen and their histories were
combined with PSEventViewer. The resulting repository was named EventViewerX,
matching the reusable .NET engine while keeping PSEventViewer as the familiar
PowerShell module name.

The original histories were preserved in the new repository. This repository
remains available as a read-only archive and landing page.

## Continue here

- [Source, documentation, and examples](https://github.com/EvotecIT/EventViewerX)
- [Issues and feature requests](https://github.com/EvotecIT/EventViewerX/issues)
- [Releases](https://github.com/EvotecIT/EventViewerX/releases)
- [PSEventViewer on PowerShell Gallery](https://www.powershellgallery.com/packages/PSEventViewer)
