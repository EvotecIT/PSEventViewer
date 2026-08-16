---
external help file: PSEventViewer-help.xml
Module Name: PSEventViewer
online version: https://github.com/EvotecIT/PSEventViewer
schema: 2.0.0
---
# Show-EVXEvent
## SYNOPSIS
Queries or accepts EventViewerX events and creates polished HTML, Excel, or email output.

Show-EVXEvent uses one normalized report snapshot for every selected output. A Type owns its source channels and event IDs; LogName is reserved for generic event queries.

## SYNTAX
### Type (Default)
```powershell
Show-EVXEvent [-Type] <EventType[]> [-Path <string[]>] [-EventRecordId <long[]>] [-MachineName <string[]>] [-Collector <string[]>] [-StartTime <DateTime>] [-EndTime <DateTime>] [-TimePeriod <TimePeriod>] [-MaxEvents <long>] [-MaxEventsScanned <long>] [-MaxConcurrency <int>] [-Oldest] [-ResolveDns] [-Credential <pscredential>] [-Authentication <EventLogAuthentication>] [-Title <string>] [-HtmlPath <string>] [-ExcelPath <string>] [-EmailPackage] [-Open] [-PassThru] [<CommonParameters>]
```

### Log
```powershell
Show-EVXEvent [-LogName] <string> [-EventId <int[]>] [-EventRecordId <long[]>] [-MachineName <string[]>] [-StartTime <DateTime>] [-EndTime <DateTime>] [-TimePeriod <TimePeriod>] [-MaxEvents <long>] [-MaxConcurrency <int>] [-Oldest] [-Credential <pscredential>] [-Authentication <EventLogAuthentication>] [-Title <string>] [-HtmlPath <string>] [-ExcelPath <string>] [-EmailPackage] [-Open] [-PassThru] [<CommonParameters>]
```

### Path
```powershell
Show-EVXEvent [-Path] <string[]> [-EventId <int[]>] [-EventRecordId <long[]>] [-StartTime <DateTime>] [-EndTime <DateTime>] [-TimePeriod <TimePeriod>] [-MaxEvents <long>] [-MaxConcurrency <int>] [-Oldest] [-Title <string>] [-HtmlPath <string>] [-ExcelPath <string>] [-EmailPackage] [-Open] [-PassThru] [<CommonParameters>]
```

### Definition
```powershell
Show-EVXEvent [-Definition] <Object> [-Path <string[]>] [-EventRecordId <long[]>] [-MachineName <string[]>] [-Collector <string[]>] [-StartTime <DateTime>] [-EndTime <DateTime>] [-TimePeriod <TimePeriod>] [-MaxEvents <long>] [-MaxEventsScanned <long>] [-MaxConcurrency <int>] [-Oldest] [-Credential <pscredential>] [-Authentication <EventLogAuthentication>] [-Title <string>] [-HtmlPath <string>] [-ExcelPath <string>] [-EmailPackage] [-Open] [-PassThru] [<CommonParameters>]
```

### Input
```powershell
Show-EVXEvent -InputObject <Object> [-Title <string>] [-HtmlPath <string>] [-ExcelPath <string>] [-EmailPackage] [-Open] [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
Queries or accepts EventViewerX events and creates polished HTML, Excel, or email output.

Show-EVXEvent uses one normalized report snapshot for every selected output. A Type owns its source channels and event IDs; LogName is reserved for generic event queries.

## EXAMPLES

### EXAMPLE 1
```powershell
Show-EVXEvent -Type ADUserLogonFailed -TimePeriod Last24Hours
```

Queries the definition-owned Security events and opens a self-contained interactive HTML report.

### EXAMPLE 2
```powershell
Show-EVXEvent -Type ActiveDirectoryAuthentication -Collector WEC01 -HtmlPath .\Auth.html -ExcelPath .\Auth.xlsx -PassThru
```

Reads ForwardedEvents once and renders both formats from the same snapshot.

### EXAMPLE 3
```powershell
Get-EVXEvent -LogName System -EventId 41,6008 | Show-EVXEvent -HtmlPath .\Startup.html
```

Does not query the event log again.

## PARAMETERS

### -Authentication
Remote Windows Event Log authentication package.

```yaml
Type: EventLogAuthentication
Parameter Sets: Type, Log, Definition
Aliases: None
Possible values: Default, Negotiate, Kerberos, Ntlm

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Collector
Windows Event Collector targets. Typed source channels are matched inside ForwardedEvents.

```yaml
Type: String[]
Parameter Sets: Type, Definition
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Credential
Remote query credential.

```yaml
Type: PSCredential
Parameter Sets: Type, Log, Definition
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Definition
Custom JSON definition path or an EventDefinition instance.

```yaml
Type: Object
Parameter Sets: Definition
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EmailPackage
Returns a responsive transport-neutral email package for Mailozaurr.

```yaml
Type: SwitchParameter
Parameter Sets: Type, Log, Path, Definition, Input
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EndTime
Absolute end time.

```yaml
Type: DateTime
Parameter Sets: Type, Log, Path, Definition
Aliases: DateTo
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EventId
Optional event IDs for a generic LogName query.

```yaml
Type: Int32[]
Parameter Sets: Log, Path
Aliases: Id
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EventRecordId
Exact event record identifiers, including IDs passed by an event-triggered scheduled task.

```yaml
Type: Int64[]
Parameter Sets: Type, Log, Path, Definition
Aliases: RecordId
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExcelPath
Excel workbook output path.

```yaml
Type: String
Parameter Sets: Type, Log, Path, Definition, Input
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -HtmlPath
Self-contained interactive HTML output path.

```yaml
Type: String
Parameter Sets: Type, Log, Path, Definition, Input
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputObject
Existing EventObject or EventTypeRecord values. No source query is performed.

```yaml
Type: Object
Parameter Sets: Input
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -LogName
Generic event channel. Mutually exclusive with Type.

```yaml
Type: String
Parameter Sets: Log
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MachineName
Direct local or remote query targets.

```yaml
Type: String[]
Parameter Sets: Type, Log, Definition
Aliases: ComputerName, ServerName
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxConcurrency
Maximum sources opened concurrently.

```yaml
Type: Int32
Parameter Sets: Type, Log, Path, Definition
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxEvents
Maximum report rows. Zero is unlimited.

```yaml
Type: Int64
Parameter Sets: Type, Log, Path, Definition
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxEventsScanned
Maximum raw candidates evaluated by typed definitions. Zero is unlimited.

```yaml
Type: Int64
Parameter Sets: Type, Definition
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Oldest
Reads oldest matches first.

```yaml
Type: SwitchParameter
Parameter Sets: Type, Log, Path, Definition
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Open
Opens generated files with the registered desktop applications.

```yaml
Type: SwitchParameter
Parameter Sets: Type, Log, Path, Definition, Input
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
Returns the normalized report snapshot in addition to generated output.

```yaml
Type: SwitchParameter
Parameter Sets: Type, Log, Path, Definition, Input
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
One or more offline EVTX files. Type or Definition may be supplied to apply typed semantics; Path alone creates a generic report.

```yaml
Type: String[]
Parameter Sets: Type, Path, Definition
Aliases: PSPath
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ResolveDns
Enriches typed IP-address properties through DnsClientX.

```yaml
Type: SwitchParameter
Parameter Sets: Type
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StartTime
Absolute start time.

```yaml
Type: DateTime
Parameter Sets: Type, Log, Path, Definition
Aliases: DateFrom
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TimePeriod
Relative time window.

```yaml
Type: TimePeriod
Parameter Sets: Type, Log, Path, Definition
Aliases: None
Possible values: PastHour, CurrentHour, PastDay, CurrentDay, PastMonth, CurrentMonth, PastQuarter, CurrentQuarter, Last3Days, Last7Days, Last14Days, Last1Hour, Last2Hours, Last3Hours, Last6Hours, Last12Hours, Last16Hours, Last24Hours, Today, Yesterday, Everything, TillLastMonday, TillLastTuesday, TillLastWednesday, TillLastThursday, TillLastFriday, TillLastSaturday, TillLastSunday

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
Report title.

```yaml
Type: String
Parameter Sets: Type, Log, Path, Definition, Input
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Type
Built-in leaf or composite event definitions. Each definition owns its channels and event IDs.

```yaml
Type: EventType[]
Parameter Sets: Type
Aliases: None
Possible values: ADComputerCreateChange, ADComputerDeleted, ADComputerChangeDetailed, ADGroupMembershipChange, ADGroupEnumeration, ADGroupChange, ADGroupCreateDelete, ADGroupChangeDetailed, ADGroupPolicyChanges, ADGroupPolicyEdits, ADGroupPolicyLinks, ADGroupPolicyChangesDetailed, GpoCreated, GpoDeleted, GpoModified, ADLdapBindingSummary, ADLdapBindingDetails, ADUserCreateChange, ADUserStatus, ADUserChangeDetailed, ADUserLockouts, ADUserLogon, ADUserLogonNTLMv1, ADUserLogonFailed, ADUserUnlocked, ADUserPrivilegeUse, ADUserRightsAssignment, KerberosTGTRequest, KerberosServiceTicket, KerberosTicketFailure, KerberosPolicyChange, ADOrganizationalUnitChangeDetailed, ADOtherChangeDetailed, ADSMBServerAuditV1, LogsClearedSecurity, LogsClearedOther, LogsFullSecurity, NetworkAccessAuthenticationPolicy, CertificateIssued, AuditPolicyChange, FirewallRuleChange, DhcpLeaseCreated, BitLockerKeyChange, BitLockerSuspended, DeviceRecognized, DeviceDisabled, ObjectDeletion, ScheduledTaskDeleted, ScheduledTaskCreated, OSCrash, OSBugCheck, OSStartup, OSShutdown, OSUncleanShutdown, OSStartupSecurity, OSCrashOnAuditFailRecovery, OSTimeChange, WindowsUpdateFailure, ClientGroupPoliciesApplication, ClientGroupPoliciesSystem, HyperVVirtualMachineShutdown, HyperVVirtualMachineStarted, IISSiteBindingFailure, HyperVCheckpointCreated, IISSiteStopped, ExchangeDatabaseMounted, DfsReplicationError, SqlDatabaseCreated, SyncCompleted, AADConnectStagingEnabled, AADConnectStagingDisabled, AADConnectPasswordSyncFailed, AADConnectRunProfile, AADSyncCycleStage, AADSyncProvisionCredentialsPing, AADSyncPasswordHashSyncStatus, AADSyncImportStatus, AADSyncFilterStatus, NetworkMonitorDriverLoaded, NetworkPromiscuousMode, ActiveDirectoryAuthentication, ActiveDirectoryAccountLifecycle, ActiveDirectoryChanges, GroupPolicyActivity, KerberosActivity, OperatingSystemLifecycle, WindowsSecurityChanges, EntraConnectHealth, NetworkSecurity, InfrastructureHealth

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.String[]`
- `System.Object`

## OUTPUTS

- `EventViewerX.Reporting.EventReport`
- `EventViewerX.Reporting.EventEmailPackage`

## RELATED LINKS

- None
