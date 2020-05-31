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

### Recommended read

- [Documentation for PSEventViewer (overview)](https://evotec.xyz/hub/scripts/pseventviewer-powershell-module/)
- [Documentation for PSEventViewer (examples and how things are different)](https://evotec.xyz/working-with-windows-events-with-powershell/)
- [PowerShell – Everything you wanted to know about Event Logs and then some](https://evotec.xyz/powershell-everything-you-wanted-to-know-about-event-logs/)
- [Sending information to Event Log with extended fields using PowerShell](https://evotec.xyz/sending-information-to-event-log-with-extended-fields-using-powershell/)
- [The only PowerShell Command you will ever need to find out who did what in Active Directory](https://evotec.xyz/the-only-powershell-command-you-will-ever-need-to-find-out-who-did-what-in-active-directory/)

## Changelog

- 1.0.17 - 2020.05.31
  - Fix for `Get-Events` (use of Path and NamedDataFilter) - provided by [danubie #13](https://github.com/EvotecIT/PSEventViewer/pull/13) - solves [#12](https://github.com/EvotecIT/PSEventViewer/issues/12)

- 1.0.16 - 2020.05.31
  - Fix for `Get-Events` for NamedDataFilter - provided by [danubie #11](https://github.com/EvotecIT/PSEventViewer/pull/11) - solves [#10](https://github.com/EvotecIT/PSEventViewer/issues/10)

- 1.0.15 - 2020.05.17
  - Fix for `Get-EventsFilter` - provided by [danubie #9](https://github.com/EvotecIT/PSEventViewer/pull/9) - solves [#7](https://github.com/EvotecIT/PSEventViewer/issues/7) and [#8](https://github.com/EvotecIT/PSEventViewer/issues/8)

- 1.0.14 - 2020.04.11
  - Updates to PSD1

- 1.0.13 - 2020.02.16
  - Added Get-EventsSettings/Set-EventsSettings (Work in progress)

- 1.0.12 - 2020.01.01
  - Added some new aliases

- 1.0.11 - 2019.12.30
  - Added Write-Event

- 1.0.10 - 2019.12.17
  - Path is back. Not sure why it was gone. Need improvements.

- 1.0.9 - 2019.11.12
  - Removed dependency on PSSharedGoods on the published module
  - PSSharedGoods is still dependency, but the building process makes it possible to compile it and push to PSGallery/Releases without that dependency.

- 1.0.7 - 2019.09.12
  - Small changes to Get-EventsInformation

- 0.62 - 2019.01.11
  - Fix for Member Name with a comma inside

- 0.61 - 2019.01.02
  - Multiple new parameters, some new functionality

- 0.51
  - Added -RecordID parameter (currently it only works with LogName + RecordID, you can't use any other parameters with RecordID as it will take LogName + RecordID anyways and crash if it's not there)

- 0.50
  - A version that worked fine :-)

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
