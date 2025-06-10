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
- [PowerShell – Everything you wanted to know about Event Logs and then some](https://evotec.xyz/powershell-everything-you-wanted-to-know-about-event-logs/)
- [Sending information to Event Log with extended fields using PowerShell](https://evotec.xyz/sending-information-to-event-log-with-extended-fields-using-powershell/)
- [The only PowerShell Command you will ever need to find out who did what in Active Directory](https://evotec.xyz/the-only-powershell-command-you-will-ever-need-to-find-out-who-did-what-in-active-directory/)

### Example for -RecordID (added in 0.51)

There is a big difference if you ask for `-RecordID` in `FilterXML` a and when you do post-processing of it via Where { }.
And by the huge difference I mean a really huge one. Depending on the amount of Event ID's stored a that you query for...
it may take minutes or even hours to get a single RecordID.
Since -FilterHashTable doesn't allow `RecordID` as parameter, nor `Get-WinEvent` doesn't have the `-RecordID` directly ... one has to use `FilterXML`.
This, as you can see below, speed up the search from `6+ minutes to 141 milliseconds`.

```powershell
Clear-Host
Import-Module PSEventViewer -Force
Get-Events -LogName 'Security' -ID 5379 -RecordID 19626 -Verbose | Format-Table TimeCreated, ProviderName, Id, Message # takes 380 miliseconds

VERBOSE: Get-Events - Overall events processing startVERBOSE: Get-Events - Events to process in Total: 1VERBOSE: Get-Events - Events to process in Total ID: 5379
VERBOSE: Get-Events - Processing computer EVO1 for Events ID: 5379
VERBOSE: Get-Events - Processing computer EVO1 for Events ID Count: 1
VERBOSE: Get-Events - Processing computer EVO1 for Events LogName: Security
VERBOSE: Get-Events - Processing computer EVO1 for Events ProviderName:
VERBOSE: Get-Events - Processing computer EVO1 for Events Keywords:
VERBOSE: Get-Events - Processing computer EVO1 for Events StartTime:
VERBOSE: Get-Events - Processing computer EVO1 for Events EndTime:
VERBOSE: Get-Events - Processing computer EVO1 for Events Path:
VERBOSE: Get-Events - Processing computer EVO1 for Events Level: 0
VERBOSE: Get-Events - Processing computer EVO1 for Events UserID:
VERBOSE: Get-Events - Processing computer EVO1 for Events Data:
VERBOSE: Get-Events - Processing computer EVO1 for Events MaxEvents: 0
VERBOSE: Get-Events - Processing computer EVO1 for Events Path:
VERBOSE: Get-Events - Processing computer EVO1 for Events UserSID:
VERBOSE: Get-Events - Processing computer EVO1 for Events Oldest: False
VERBOSE: Get-Events - Processing computer EVO1 for Events RecordID: 19626
VERBOSE: Get-Events - Running query with parallel enabled...
VERBOSE: Get-Events - Verbose from runspace: Get-Events - preparing to run
VERBOSE: Get-Events - Verbose from runspace: Get-Events - Inside EVO1 executing on: EVO1
VERBOSE: Get-Events - Verbose from runspace: Get-Events - Inside EVO1 for Events ID: 5379
VERBOSE: Get-Events - Verbose from runspace: Get-Events - Inside EVO1 for Events ID: Security
VERBOSE: Get-Events - Verbose from runspace: Get-Events - Inside EVO1 for Events RecordID: 19626
VERBOSE: Get-Events - Verbose from runspace: Get-Events - Inside EVO1 for Events Oldest: False
VERBOSE: Get-Events - Verbose from runspace: Get-Events - Inside EVO1 for Events Max Events: 0
VERBOSE: Get-Events - Verbose from runspace: Get-Events - Inside EVO1 - FilterXML:
                <QueryList>
                    <Query Id="0" Path="Security">
                        <Select Path="Security">
                        *[
                            (System/EventID=5379)
                            and
                            (System/EventRecordID=19626)
                         ]
                        </Select>
                    </Query>
                </QueryList>
VERBOSE: Get-Events - Verbose from runspace: Get-Events - Inside EVO1 Events founds 1
VERBOSE: Get-Events - Verbose from runspace: Get-Events - Inside EVO1 - Processing events...
VERBOSE: Get-Events - Verbose from runspace: Get-Events - Inside EVO1 - Time to generate 0 hours, 0 minutes, 0 seconds, 141 milliseconds
VERBOSE: Get-Events - Verbose from runspace: Get-Events - finished run
VERBOSE: Get-Events - Overall events processed in total for the report: 1
VERBOSE: Get-Events - Overall time to generate 0 hours, 0 minutes, 0 seconds, 260 milliseconds
VERBOSE: Get-Events - Overall events processing end

TimeCreated         ProviderName                          Id Message
-----------         ------------                          -- -------
17.07.2018 19:24:58 Microsoft-Windows-Security-Auditing 5379 Credential Manager credentials were read....
```

```powershell
Get-Events -LogName 'Security' -ID 5379 -Verbose | Where { $_.RecordID -eq 19626 } | Format-Table  TimeCreated, ProviderName, Id, Message  # takes 4-6 minutes depending on amount of events there are.

VERBOSE: Get-Events - Overall events processing start
VERBOSE: Get-Events - Events to process in Total: 1
VERBOSE: Get-Events - Events to process in Total ID: 5379
VERBOSE: Get-Events - Processing computer EVO1 for Events ID: 5379
VERBOSE: Get-Events - Processing computer EVO1 for Events ID Count: 1
VERBOSE: Get-Events - Processing computer EVO1 for Events LogName: Security
VERBOSE: Get-Events - Processing computer EVO1 for Events ProviderName:
VERBOSE: Get-Events - Processing computer EVO1 for Events Keywords:
VERBOSE: Get-Events - Processing computer EVO1 for Events StartTime:
VERBOSE: Get-Events - Processing computer EVO1 for Events EndTime:
VERBOSE: Get-Events - Processing computer EVO1 for Events Path:
VERBOSE: Get-Events - Processing computer EVO1 for Events Level: 0
VERBOSE: Get-Events - Processing computer EVO1 for Events UserID:
VERBOSE: Get-Events - Processing computer EVO1 for Events Data:
VERBOSE: Get-Events - Processing computer EVO1 for Events MaxEvents: 0
VERBOSE: Get-Events - Processing computer EVO1 for Events Path:
VERBOSE: Get-Events - Processing computer EVO1 for Events UserSID:
VERBOSE: Get-Events - Processing computer EVO1 for Events Oldest: False
VERBOSE: Get-Events - Processing computer EVO1 for Events RecordID: 0
VERBOSE: Get-Events - Running query with parallel enabled...
VERBOSE: Get-Events - Verbose from runspace: Get-Events - preparing to run
VERBOSE: Get-Events - Verbose from runspace: Get-Events - Inside EVO1 executing on: EVO1
VERBOSE: Get-Events - Verbose from runspace: Get-Events - Inside EVO1 for Events ID: 5379
VERBOSE: Get-Events - Verbose from runspace: Get-Events - Inside EVO1 for Events ID: Security
VERBOSE: Get-Events - Verbose from runspace: Get-Events - Inside EVO1 for Events RecordID: 0
VERBOSE: Get-Events - Verbose from runspace: Get-Events - Inside EVO1 for Events Oldest: False
VERBOSE: Get-Events - Verbose from runspace: Get-Events - Inside EVO1 for Events Max Events: 0
VERBOSE: Get-Events - Verbose from runspace: Get-Events - Inside EVO1 Data in FilterHashTable LogName Security
VERBOSE: Get-Events - Verbose from runspace: Get-Events - Inside EVO1 Data in FilterHashTable Id 5379
VERBOSE: Get-Events - Verbose from runspace: Constructed structured query:
<QueryList><Query Id="0" Path="security"><Select Path="security">*[((System/EventID=5379))]</Select></Query></QueryList>.
VERBOSE: Get-Events - Verbose from runspace: Get-Events - Inside EVO1 Events founds 9041
VERBOSE: Get-Events - Verbose from runspace: Get-Events - Inside EVO1 - Processing events...
VERBOSE: Get-Events - Verbose from runspace: Get-Events - Inside EVO1 - Time to generate 0 hours, 4 minutes, 55 seconds, 627 milliseconds
VERBOSE: Get-Events - Verbose from runspace: Get-Events - finished run
VERBOSE: Get-Events - Overall events processed in total for the report: 9041
VERBOSE: Get-Events - Overall time to generate 0 hours, 4 minutes, 55 seconds, 751 milliseconds
VERBOSE: Get-Events - Overall events processing end

TimeCreated         ProviderName                          Id Message
-----------         ------------                          -- -------
```


### Registry Entries

Registry entries storing configuration for Events Logs in the registry key `HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\EventLog\LogName`.
It's not advised to modify registry manually as it can cause issues with the Event Logs.

Using following cmdlets can help

```powershell
Get-EventsSettings -LogName 'Application'
Set-WinEventSettings -LogName 'Application' -MaximumSizeMB 15 -Mode AutoBackup -WhatIf
```

**Registry reference table:**

| Registry value      | Description                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
|---------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| CustomSD            | Restricts access to the event log. This value is of type REG_SZ. The format used is Security Descriptor Definition Language (SDDL). Construct an ACL that grants one or more of the following rights:                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
|                     | Clear (0x0004)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
|                     | Read (0x0001)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
|                     | Write (0x0002)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
|                     | To be a syntactically valid SDDL, the CustomSD value must specify an owner and a group owner (for example, O:BAG:SY), but the owner and group owner are not used. If CustomSD is set to a wrong value, an event is fired in the System event log when the event log service starts, and the event log gets a default security descriptor which is identical to the original CustomSD value for the Application log. SACLs are not supported.                                                                                                                                                                                                                                                                                                                                                                                                   |
|                     | For more information, see Event Logging Security.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              |
|                     | Windows Server 2003: SACLs are supported.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
|                     | Windows XP/2000: This value is not supported.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
|                     |                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| DisplayNameFile     | This value is not used. Windows Server 2003 and Windows XP/2000: Name of the file that stores the localized name of the event log. The name stored in this file appears as the log name in Event Viewer. If this entry does not appear in the registry for an event log, Event Viewer displays the name of the registry subkey as the log name. This value is of type REG_EXPAND_SZ. The default value is %SystemRoot%\system32\els.dll.                                                                                                                                                                                                                                                                                                                                                                                                       |
| DisplayNameID       | This value is not used. Windows Server 2003 and Windows XP/2000: Message identification number of the log name string. This number indicates the message in which the localized display name appears. The message is stored in the file specified by the DisplayNameFile value. This value is of type REG_DWORD.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               |
| File                | Fully qualified path to the file where each event log is stored. This enables Event Viewer and other applications to find the log files. This value is of type REG_SZ or REG_EXPAND_SZ. This value is optional. If the value is not specified, it defaults to %SystemRoot%\system32\winevt\logs\ followed by a file name that is based on the event log registry key name.The specific event log file path should be set using the command line utility wevtutil.exe or by using the EvtSetChannelConfigProperty function with EvtChannelLoggingConfigLogFilePath passed into the PropertyId parameter.                                                                                                                                                                                                                                        |
|                     | If a specific file is set, make sure that the event log service has full permissions on the file.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              |
|                     | This value needs to be a valid file name for a file that is located on a local directory (not a remote computer, not a DOS device, not a floppy, and not a pipe). If the file setting is wrong, an event is fired in the System event log when the event log service starts.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
|                     | Do not use environment variables, in the path to the file, that cannot be expanded in the context of the event log service.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
|                     | Windows Server 2003 and Windows XP/2000: This value defaults to %SystemRoot%\system32\config\ followed by a file name that is based on the event log registry key name. If the File setting is set to an invalid value, the log will either not be initialized properly, or all requests will silently go to the default log (Application).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| MaxSize             | Maximum size, in bytes, of the log file. This value is of type REG_DWORD. The value must be set to a multiple of 64K for a System, Application, or Security log. The default value is 1MB.Windows Server 2003 and Windows XP/2000: The value is limited to 0xFFFFFFFF, and the default value is 512K.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| PrimaryModule       | This value is not used.Windows Server 2003 and Windows XP/2000: This value is the name of the subkey that contains the default values for the entries in the subkey for the event source. This value is of type REG_SZ.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| Retention           | This value is of type REG_DWORD. The default value is 0. If this value is 0, the records of events are always overwritten. If this value is 0xFFFFFFFF or any nonzero value, records are never overwritten. When the log file reaches its maximum size, you must clear the log manually; otherwise, new events are discarded. You must also clear the log before you can change its size.Windows Server 2003 and Windows XP/2000: This value is the time interval, in seconds, that records of events are protected from being overwritten. When the age of an event reaches or exceeds this value, it can be overwritten.                                                                                                                                                                                                                     |
| Sources             | This value is not used. Windows Server 2003 and Windows XP/2000: Names of the applications, services, or groups of applications that write events to this log. This value should only be read and not altered. The event log service maintains the list based on each program listed in a subkey under the log. This value is of type REG_MULTI_SZ.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| AutoBackupLogFiles  | This value is of type REG_DWORD, and is used by the event log service to determine whether an event log should be automatically saved. The default value is 0, which disables auto-backup. The service will back up the log file only if the retention value is -1 (0xFFFFFFFF). Other values will be ignored.Windows Server 2003: Retention can be set to -1 (0xFFFFFFFF) or 1 (0x00000001) for AutoBackupLogFiles to work. Other values will be ignored.                                                                                                                                                                                                                                                                                                                                                                                     |
| RestrictGuestAccess | This value is not used. Windows XP/2000: This value is of type REG_DWORD, and the default value is 1. When the value is set to 1, it restricts the Guest and Anonymous account access to the event log, and when this value is 0, it allows Guest account access to the event log.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             |
| Isolation           | Defines the default access permissions for the log. This value is of type REG_SZ. You can specify one of the following values:                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
|                     | Application                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
|                     | System                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         |
|                     | Custom                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         |
|                     | The default isolation is Application. The default permissions for Application are (shown using SDDL):                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
|                     | Copy                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
|                     |             L"O:BAG:SYD:"            L"(A;;0xf0007;;;SY)"                // local system               (read, write, clear)            L"(A;;0x7;;;BA)"                    // built-in admins            (read, write, clear)            L"(A;;0x7;;;SO)"                    // server operators           (read, write, clear)            L"(A;;0x3;;;IU)"                    // INTERACTIVE LOGON          (read, write)            L"(A;;0x3;;;SU)"                    // SERVICES LOGON             (read, write)            L"(A;;0x3;;;S-1-5-3)"               // BATCH LOGON                (read, write)            L"(A;;0x3;;;S-1-5-33)"              // write restricted service   (read, write)            L"(A;;0x1;;;S-1-5-32-573)";         // event log readers          (read)                                                |
|                     | The default permissions for System are (shown using SDDL):                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     |
|                     | Copy                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
|                     |             L"O:BAG:SYD:"            L"(A;;0xf0007;;;SY)"                // local system             (read, write, clear)            L"(A;;0x7;;;BA)"                    // built-in admins          (read, write, clear)            L"(A;;0x3;;;BO)"                    // backup operators         (read, write)            L"(A;;0x5;;;SO)"                    // server operators         (read, clear)             L"(A;;0x1;;;IU)"                    // INTERACTIVE LOGON        (read)            L"(A;;0x3;;;SU)"                    // SERVICES LOGON           (read, write)            L"(A;;0x1;;;S-1-5-3)"               // BATCH LOGON              (read)            L"(A;;0x2;;;S-1-5-33)"              // write restricted service (write)            L"(A;;0x1;;;S-1-5-32-573)";         // event log readers        (read) |
|                     | The default permissions for Custom isolation is the same as Application.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
|                     | Windows Server 2003 and Windows XP/2000: This value is not available.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |