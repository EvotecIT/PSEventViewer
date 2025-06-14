Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

# $findWinEventSplat = @{
#     Type        = 'ADGroupPolicyLinks', 'ADGroupPolicyEdits', 'ADGroupPolicyChanges'
#     #Type = 'ADComputerChangeDetailed', 'ADUserChangeDetailed', 'ADGroupChange', 'ADGroupCreateDelete', 'ADGroupMembershipChange'
#     MachineName = 'AD1', 'AD2', 'AD0'
#     Verbose     = $true
# }

$findWinEventSplat1 = @{
    Type        = 'ADGroupPolicyLinks'
    MachineName = 'AD1', 'AD2', 'AD0'
    Verbose     = $true
}

$findWinEventSplat2 = @{
    Type        = 'ADGroupPolicyEdits'
    MachineName = 'AD1', 'AD2', 'AD0'
    Verbose     = $true
}

$findWinEventSplat3 = @{
    Type        = 'ADGroupPolicyChanges'
    MachineName = 'AD1', 'AD2', 'AD0'
    Verbose     = $true
}

#$Test1 = Find-WinEvent @findWinEventSplat1 -TimePeriod Last7Days
#$Test1 | Format-Table *

$Test2 = Find-WinEvent @findWinEventSplat2 -TimePeriod CurrentDay
$Test2 | Format-Table

#$Test3 = Find-WinEvent @findWinEventSplat3 -TimePeriod CurrentDay
#$Test3 | Format-Table

<# 5141 => delete gpo
- <Event xmlns="http://schemas.microsoft.com/win/2004/08/events/event">
- <System>
  <Provider Name="Microsoft-Windows-Security-Auditing" Guid="{54849625-5478-4994-a5ba-3e3b0328c30d}" />
  <EventID>5141</EventID>
  <Version>0</Version>
  <Level>0</Level>
  <Task>14081</Task>
  <Opcode>0</Opcode>
  <Keywords>0x8020000000000000</Keywords>
  <TimeCreated SystemTime="2024-12-26T16:44:14.0120206Z" />
  <EventRecordID>164745504</EventRecordID>
  <Correlation ActivityID="{ef1b96da-bff1-4bb6-9df5-fab72c615f94}" />
  <Execution ProcessID="712" ThreadID="4720" />
  <Channel>Security</Channel>
  <Computer>AD1.ad.evotec.xyz</Computer>
  <Security />
  </System>
- <EventData>
  <Data Name="OpCorrelationID">{24046d04-3eec-430a-b05d-51d96d43e1f9}</Data>
  <Data Name="AppCorrelationID">-</Data>
  <Data Name="SubjectUserSid">S-1-5-21-853615985-2870445339-3163598659-1105</Data>
  <Data Name="SubjectUserName">przemyslaw.klys</Data>
  <Data Name="SubjectDomainName">EVOTEC</Data>
  <Data Name="SubjectLogonId">0x2a1a279f</Data>
  <Data Name="DSName">ad.evotec.xyz</Data>
  <Data Name="DSType">%%14676</Data>
  <Data Name="ObjectDN">CN={6BE92AC1-7509-402E-954F-1FE086AEC1B9},CN=Policies,CN=System,DC=ad,DC=evotec,DC=xyz</Data>
  <Data Name="ObjectGUID">{bb77e3e5-adf8-4361-958b-42f1ff46e304}</Data>
  <Data Name="ObjectClass">groupPolicyContainer</Data>
  <Data Name="TreeDelete">%%14679</Data>
  </EventData>
  </Event>
#>

<# 5137 => create gpo
- <Event xmlns="http://schemas.microsoft.com/win/2004/08/events/event">
- <System>
  <Provider Name="Microsoft-Windows-Security-Auditing" Guid="{54849625-5478-4994-a5ba-3e3b0328c30d}" />
  <EventID>5137</EventID>
  <Version>0</Version>
  <Level>0</Level>
  <Task>14081</Task>
  <Opcode>0</Opcode>
  <Keywords>0x8020000000000000</Keywords>
  <TimeCreated SystemTime="2024-12-26T16:46:34.0826380Z" />
  <EventRecordID>164745695</EventRecordID>
  <Correlation ActivityID="{9c5aba6e-37c1-4aa0-8c61-41bbffcd5718}" />
  <Execution ProcessID="712" ThreadID="6072" />
  <Channel>Security</Channel>
  <Computer>AD1.ad.evotec.xyz</Computer>
  <Security />
  </System>
- <EventData>
  <Data Name="OpCorrelationID">{ca2484a3-fe17-40d2-85d8-ad7609d26720}</Data>
  <Data Name="AppCorrelationID">-</Data>
  <Data Name="SubjectUserSid">S-1-5-21-853615985-2870445339-3163598659-1105</Data>
  <Data Name="SubjectUserName">przemyslaw.klys</Data>
  <Data Name="SubjectDomainName">EVOTEC</Data>
  <Data Name="SubjectLogonId">0x2a1a279f</Data>
  <Data Name="DSName">ad.evotec.xyz</Data>
  <Data Name="DSType">%%14676</Data>
  <Data Name="ObjectDN">CN=User,CN={6D18B287-A1A5-4DBA-B7D8-B656DB280EFA},CN=Policies,CN=System,DC=ad,DC=evotec,DC=xyz</Data>
  <Data Name="ObjectGUID">{32f434ec-f9d8-42e2-a223-4fe55e0807c1}</Data>
  <Data Name="ObjectClass">container</Data>
  </EventData>
  </Event>
#>