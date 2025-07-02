<p align="center">
  <a href="https://dev.azure.com/evotecpl/PSEventViewer/_build/latest?definitionId=3"><img src="https://dev.azure.com/evotecpl/PSEventViewer/_apis/build/status/EvotecIT.PSEventViewer"></a>
  <a href="https://www.powershellgallery.com/packages/PSEventViewer"><img src="https://img.shields.io/powershellgallery/v/PSEventViewer.svg"></a>
  <a href="https://www.powershellgallery.com/packages/PSEventViewer"><img src="https://img.shields.io/powershellgallery/vpre/PSEventViewer.svg?label=powershell%20gallery%20preview&colorB=yellow"></a>
  <a href="https://github.com/EvotecIT/PSEventViewer"><img src="https://img.shields.io/github/license/EvotecIT/PSEventViewer.svg"></a>
</p>

<p align="center">
  <a href="https://www.powershellgallery.com/packages/PSEventViewer"><img src="https://img.shields.io/powershellgallery/p/PSEventViewer.svg"></a>
  <a href="https://github.com/EvotecIT/PSEventViewer"><img src="https://img.shields.io/github/languages/top/evotecit/PSEventViewer.svg"></a>
  <a href="https://github.com/EvotecIT/PSEventViewer"><img src="https://img.shields.io/github/languages/code-size/evotecit/PSEventViewer.svg"></a>
  <a href="https://www.powershellgallery.com/packages/PSEventViewer"><img src="https://img.shields.io/powershellgallery/dt/PSEventViewer.svg"></a>
</p>

<p align="center">
  <a href="https://twitter.com/PrzemyslawKlys"><img src="https://img.shields.io/twitter/follow/PrzemyslawKlys.svg?label=Twitter%20%40PrzemyslawKlys&style=social"></a>
  <a href="https://evotec.xyz/hub"><img src="https://img.shields.io/badge/Blog-evotec.xyz-2A6496.svg"></a>
  <a href="https://www.linkedin.com/in/pklys"><img src="https://img.shields.io/badge/LinkedIn-pklys-0077B5.svg?logo=LinkedIn"></a>
</p>

# PSEventViewer - PowerShell Module

## Description

This module was built for a project of Events Reporting. As it was a bit inefficient, I've decided to rewrite it and split reading events to separate modules.
While underneath it's just a wrapper over `Get-WinEvent`, it does add few tweaks here and there...

The project was split into 2 parts:

- `PSEventViewer` - this module.
- [PSWinReporting](https://github.com/EvotecIT/PSWinReporting) - reporting on Active Directory Events, Windows Events...

### Why PSEventViewer?

By default in PowerShell we have couple of cmdlets that let you do different things:

- [x] Microsoft.PowerShell.Diagnostics
    - [x] `Get-WinEvent`
    - [x] `New-WinEvent`

- [x] Microsoft.PowerShell.Management - The cmdlets that contain the EventLog noun, the EventLog cmdlets, work only on classic event logs.
    - [x] `Clear-EventLog` - Clears all of the entries from the specified event logs on the local or remote computers.
    - [x] `Get-EventLog -list` - alternative to `Get-WmiObject win32_nteventlogfile` - lists event logs
    - [x] `Get-EventLog` - Gets the events in the event log that match the specified criteria.
    - [x] `Limit-EventLog` - Sets the event log properties that limit the size of the event log and the age of its entries.
    - [x] `New-EventLog` - Creates a new event log and a new event source on a local or remote computer.
    - [x] `Remove-EventLog` - Deletes an event log or unregisters an event source.
    - [x] `Show-EventLog` - Displays the event logs of the local or a remote computer in Event Viewer.

Our module tries to improve on that by providing a bit more flexibility and speed, and also by providing a bit more information about the events.

### Recommended read

- [Documentation for PSEventViewer (overview)](https://evotec.xyz/hub/scripts/pseventviewer-powershell-module/)
- [Documentation for PSEventViewer (examples and how things are different)](https://evotec.xyz/working-with-windows-events-with-powershell/)
- [PowerShell - Everything you wanted to know about Event Logs and then some](https://evotec.xyz/powershell-everything-you-wanted-to-know-about-event-logs/)
- [Sending information to Event Log with extended fields using PowerShell](https://evotec.xyz/sending-information-to-event-log-with-extended-fields-using-powershell/)
- [The only PowerShell Command you will ever need to find out who did what in Active Directory](https://evotec.xyz/the-only-powershell-command-you-will-ever-need-to-find-out-who-did-what-in-active-directory/)

### Long-running monitoring jobs

`Get-EVXEvent` can keep track of the last processed record. Specify `-RecordIdFile` with a file path. The cmdlet stores the newest record ID there and automatically skips older events on the next run. When multiple monitoring jobs share the same file, use `-RecordIdKey` to persist a value per job.

```powershell
Get-EVXEvent -LogName Security -RecordIdFile C:\Temp\evx.state -RecordIdKey Machine1
```
