---
external help file: PSEventViewer-help.xml
Module Name: PSEventViewer
online version: https://github.com/EvotecIT/PSEventViewer
schema: 2.0.0
---
# Get-EVXEvent
## SYNOPSIS
Enhanced event querying cmdlet that replaces and extends Get-WinEvent functionality.

Supports local/remote logs, named event shortcuts, record ID resumes, parallel queries, and rich filtering (IDs, providers, keywords, levels, time windows, named data).

## SYNTAX
### ProviderEvents (Default)
```powershell
Get-EVXEvent [[-EventId] <int[]>] -ProviderName <string[]> [-EventRecordId <long[]>] [-RecordIdFile <string>] [-RecordIdKey <string>] [-MachineName <List[string]>] [-Keywords <long[]>] [-Level <int[]>] [-StartTime <DateTime>] [-EndTime <DateTime>] [-TimePeriod <TimePeriod>] [-UserId <string[]>] [-MessageRegex <regex>] [-MaxConcurrency <int>] [-MaxEvents <long>] [-MaxEventsScanned <long>] [-ReadMode <EventReadMode>] [-MessageCulture <cultureinfo>] [-SessionTimeoutMs <int>] [-BufferCapacity <int>] [-Expand] [-Oldest] [-NamedDataFilter <hashtable>] [-NamedDataExcludeFilter <hashtable>] [-DisableParallel] [-AsArray] [-FallbackMessageCulture <cultureinfo>] [-Credential <pscredential>] [-Authentication <EventLogAuthentication>] [-BookmarkXml <string>] [-BookmarkOffset <long>] [-IgnoreStaleBookmark] [-ContinueOnError] [-Force] [-IncludeBookmark] [<CommonParameters>]
```

### GenericEvents
```powershell
Get-EVXEvent [-LogName] <string[]> [[-EventId] <int[]>] [-EventRecordId <long[]>] [-RecordIdFile <string>] [-RecordIdKey <string>] [-MachineName <List[string]>] [-ProviderName <string[]>] [-Keywords <long[]>] [-Level <int[]>] [-StartTime <DateTime>] [-EndTime <DateTime>] [-TimePeriod <TimePeriod>] [-UserId <string[]>] [-MessageRegex <regex>] [-MaxConcurrency <int>] [-MaxEvents <long>] [-MaxEventsScanned <long>] [-ReadMode <EventReadMode>] [-MessageCulture <cultureinfo>] [-SessionTimeoutMs <int>] [-BufferCapacity <int>] [-Expand] [-Oldest] [-NamedDataFilter <hashtable>] [-NamedDataExcludeFilter <hashtable>] [-DisableParallel] [-AsArray] [-FallbackMessageCulture <cultureinfo>] [-FilterXPath <string>] [-Credential <pscredential>] [-Authentication <EventLogAuthentication>] [-BookmarkXml <string>] [-BookmarkOffset <long>] [-IgnoreStaleBookmark] [-ContinueOnError] [-Force] [-IncludeBookmark] [<CommonParameters>]
```

### NamedEvents
```powershell
Get-EVXEvent -Type <NamedEvents[]> [-LogName <string[]>] [-EventId <int[]>] [-RecordIdFile <string>] [-RecordIdKey <string>] [-MachineName <List[string]>] [-StartTime <DateTime>] [-EndTime <DateTime>] [-TimePeriod <TimePeriod>] [-MessageRegex <regex>] [-MaxConcurrency <int>] [-MaxEvents <long>] [-MaxEventsScanned <long>] [-ResolveDns] [-DnsTimeoutMs <int>] [-DnsMaxConcurrency <int>] [-ReadMode <EventReadMode>] [-MessageCulture <cultureinfo>] [-SessionTimeoutMs <int>] [-BufferCapacity <int>] [-Expand] [-Oldest] [-DisableParallel] [-AsArray] [-FallbackMessageCulture <cultureinfo>] [-Credential <pscredential>] [-Authentication <EventLogAuthentication>] [-ContinueOnError] [-IncludeBookmark] [<CommonParameters>]
```

### PathEvents
```powershell
Get-EVXEvent -Path <string[]> [-EventId <int[]>] [-EventRecordId <long[]>] [-RecordIdFile <string>] [-RecordIdKey <string>] [-ProviderName <string[]>] [-Keywords <long[]>] [-Level <int[]>] [-StartTime <DateTime>] [-EndTime <DateTime>] [-TimePeriod <TimePeriod>] [-UserId <string[]>] [-MessageRegex <regex>] [-MaxConcurrency <int>] [-MaxEvents <long>] [-MaxEventsScanned <long>] [-ReadMode <EventReadMode>] [-MessageCulture <cultureinfo>] [-BufferCapacity <int>] [-Expand] [-Oldest] [-NamedDataFilter <hashtable>] [-NamedDataExcludeFilter <hashtable>] [-DisableParallel] [-AsArray] [-FallbackMessageCulture <cultureinfo>] [-FilterXPath <string>] [-BookmarkXml <string>] [-BookmarkOffset <long>] [-IgnoreStaleBookmark] [-ContinueOnError] [-IncludeBookmark] [<CommonParameters>]
```

### FilterHashtableEvents
```powershell
Get-EVXEvent [-FilterHashtable] <hashtable[]> [-RecordIdFile <string>] [-RecordIdKey <string>] [-MachineName <List[string]>] [-MessageRegex <regex>] [-MaxConcurrency <int>] [-MaxEvents <long>] [-MaxEventsScanned <long>] [-ReadMode <EventReadMode>] [-MessageCulture <cultureinfo>] [-SessionTimeoutMs <int>] [-BufferCapacity <int>] [-Expand] [-Oldest] [-NamedDataFilter <hashtable>] [-NamedDataExcludeFilter <hashtable>] [-DisableParallel] [-AsArray] [-FallbackMessageCulture <cultureinfo>] [-Credential <pscredential>] [-Authentication <EventLogAuthentication>] [-BookmarkXml <string>] [-BookmarkOffset <long>] [-IgnoreStaleBookmark] [-ContinueOnError] [-TolerateQueryErrors] [-Force] [-IncludeBookmark] [<CommonParameters>]
```

### FilterXmlEvents
```powershell
Get-EVXEvent [-FilterXml] <xml> [-RecordIdFile <string>] [-RecordIdKey <string>] [-MachineName <List[string]>] [-MessageRegex <regex>] [-MaxConcurrency <int>] [-MaxEvents <long>] [-MaxEventsScanned <long>] [-ReadMode <EventReadMode>] [-MessageCulture <cultureinfo>] [-SessionTimeoutMs <int>] [-BufferCapacity <int>] [-Expand] [-Oldest] [-DisableParallel] [-AsArray] [-FallbackMessageCulture <cultureinfo>] [-Credential <pscredential>] [-Authentication <EventLogAuthentication>] [-BookmarkXml <string>] [-BookmarkOffset <long>] [-IgnoreStaleBookmark] [-ContinueOnError] [-TolerateQueryErrors] [-IncludeBookmark] [<CommonParameters>]
```

## DESCRIPTION
Enhanced event querying cmdlet that replaces and extends Get-WinEvent functionality.

Supports local/remote logs, named event shortcuts, record ID resumes, parallel queries, and rich filtering (IDs, providers, keywords, levels, time windows, named data).

## EXAMPLES

### EXAMPLE 1
```powershell
Get-EVXEvent -LogName Security -EventId 4624 -StartTime (Get-Date).AddHours(-1)
```

Shows only successful logons from the last hour.

### EXAMPLE 2
```powershell
Get-EVXEvent -Path C:\Logs\App.evtx -EventId 1000,1001
```

Filters specific application error IDs from an offline log.

### EXAMPLE 3
```powershell
Get-EVXEvent -LogName Security -RecordIdFile C:\temp\resume.json -RecordIdKey Sec
```

Continues from the last processed record and updates the checkpoint file.

### EXAMPLE 4
```powershell
Get-EVXEvent -NamedEvents ADUserLogonFailed -StartTime (Get-Date).AddDays(-1)
```

Expands the named event definition to fetch all related logon failure IDs.

### EXAMPLE 5
```powershell
Get-EVXEvent -LogName Security -MachineName DC1,DC2 -EventId 4740 -MaxConcurrency 8
```

Retrieves account lockouts from multiple domain controllers with bounded concurrent source setup.

### EXAMPLE 6
```powershell
Get-EVXEvent -Path C:\Logs\Security.evtx -Oldest -ReadMode Metadata | Select-Object TimeCreated, RecordId, Id, ProviderName, MachineName | Export-Csv C:\Logs\Security-metadata.csv -NoTypeInformation
```

Skips provider message formatting, XML parsing, attachments, and bookmarks while streaming every record.

## PARAMETERS

### -AsArray
Returns results as an array instead of streaming them.

```yaml
Type: SwitchParameter
Parameter Sets: ProviderEvents, GenericEvents, NamedEvents, PathEvents, FilterHashtableEvents, FilterXmlEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Authentication
Authentication package used for remote Windows Event Log sessions.

```yaml
Type: EventLogAuthentication
Parameter Sets: ProviderEvents, GenericEvents, NamedEvents, FilterHashtableEvents, FilterXmlEvents
Aliases: None
Possible values: Default, Negotiate, Kerberos, Ntlm

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -BookmarkOffset
Record offset relative to BookmarkXml. The default of one resumes after the bookmarked event.

```yaml
Type: Int64
Parameter Sets: ProviderEvents, GenericEvents, PathEvents, FilterHashtableEvents, FilterXmlEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -BookmarkXml
Native bookmark XML used as the seek origin. A bookmark targets one source or one
structured QueryList session and cannot be fanned out across several independent sources.

```yaml
Type: String
Parameter Sets: ProviderEvents, GenericEvents, PathEvents, FilterHashtableEvents, FilterXmlEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -BufferCapacity
Maximum number of projected events buffered between parallel readers and the PowerShell pipeline. Zero selects a bounded default.

```yaml
Type: Int32
Parameter Sets: ProviderEvents, GenericEvents, NamedEvents, PathEvents, FilterHashtableEvents, FilterXmlEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ContinueOnError
Continues other independent sources when a channel, computer, or file query fails.
Each isolated failure is emitted as a non-terminating PowerShell error.

```yaml
Type: SwitchParameter
Parameter Sets: ProviderEvents, GenericEvents, NamedEvents, PathEvents, FilterHashtableEvents, FilterXmlEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Credential
Credentials used for remote channel or structured queries.

```yaml
Type: PSCredential
Parameter Sets: ProviderEvents, GenericEvents, NamedEvents, FilterHashtableEvents, FilterXmlEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DisableParallel
Disables parallel processing of queries.

```yaml
Type: SwitchParameter
Parameter Sets: ProviderEvents, GenericEvents, NamedEvents, PathEvents, FilterHashtableEvents, FilterXmlEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DnsMaxConcurrency
Maximum number of reverse-DNS requests that may overlap. Results and checkpoints remain in event order.

```yaml
Type: Int32
Parameter Sets: NamedEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DnsTimeoutMs
Whole-request timeout in milliseconds for each optional reverse-DNS request, including dependency retries.

```yaml
Type: Int32
Parameter Sets: NamedEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EndTime
End time for the event query.

```yaml
Type: DateTime
Parameter Sets: ProviderEvents, GenericEvents, NamedEvents, PathEvents
Aliases: DateTo
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EventId
Event identifiers used to filter results.

```yaml
Type: Int32[]
Parameter Sets: ProviderEvents, GenericEvents, NamedEvents, PathEvents
Aliases: Id
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EventRecordId
Specific event record identifiers to retrieve.

```yaml
Type: Int64[]
Parameter Sets: ProviderEvents, GenericEvents, PathEvents
Aliases: RecordId
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Expand
Expands event data into individual properties.

```yaml
Type: SwitchParameter
Parameter Sets: ProviderEvents, GenericEvents, NamedEvents, PathEvents, FilterHashtableEvents, FilterXmlEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FallbackMessageCulture
Culture used when provider resources do not contain MessageCulture.
Get-EVXEvent requests en-US by default, then falls back to the current UI culture
so deterministic English is preferred without discarding locally available messages.

```yaml
Type: CultureInfo
Parameter Sets: ProviderEvents, GenericEvents, NamedEvents, PathEvents, FilterHashtableEvents, FilterXmlEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FilterHashtable
One or more Get-WinEvent compatible hashtables containing LogName, Path, ProviderName, or combinations of them plus event predicates.
Arbitrary keys target named EventData fields. SuppressHashFilter adds native exclusions.

```yaml
Type: Hashtable[]
Parameter Sets: FilterHashtableEvents
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -FilterXml
A complete Windows Event Log QueryList XML document. This supports multi-channel Select
and Suppress expressions without translating or weakening the supplied query.

```yaml
Type: XmlDocument
Parameter Sets: FilterXmlEvents
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -FilterXPath
A native Windows Event Log XPath expression applied to every LogName or Path.
This cannot be combined with the high-level filter parameters.

```yaml
Type: String
Parameter Sets: GenericEvents, PathEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Force
Includes analytic and debug channels when LogName or ProviderName uses wildcard patterns.
An explicitly named analytic or debug channel never requires Force.

```yaml
Type: SwitchParameter
Parameter Sets: ProviderEvents, GenericEvents, FilterHashtableEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IgnoreStaleBookmark
Allows bookmark seek to continue when the exact bookmarked record is not in the result set.

```yaml
Type: SwitchParameter
Parameter Sets: ProviderEvents, GenericEvents, PathEvents, FilterHashtableEvents, FilterXmlEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IncludeBookmark
Materializes a native bookmark for each returned event. This is disabled by
default because bookmark creation adds native handle and render work per record.

```yaml
Type: SwitchParameter
Parameter Sets: ProviderEvents, GenericEvents, NamedEvents, PathEvents, FilterHashtableEvents, FilterXmlEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Keywords
Keywords used to filter events.

```yaml
Type: Int64[]
Parameter Sets: ProviderEvents, GenericEvents, PathEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Level
Event level (e.g. Error, Warning) used for filtering.

```yaml
Type: Int32[]
Parameter Sets: ProviderEvents, GenericEvents, PathEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LogName
Name of the log to query.

```yaml
Type: String[]
Parameter Sets: GenericEvents, NamedEvents
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue, ByPropertyName)
Accept wildcard characters: False
```

### -MachineName
Computer names against which to run the query.

```yaml
Type: List`1
Parameter Sets: ProviderEvents, GenericEvents, NamedEvents, FilterHashtableEvents, FilterXmlEvents
Aliases: ComputerName, ServerName
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxConcurrency
Maximum number of independent event sources opened concurrently.

```yaml
Type: Int32
Parameter Sets: ProviderEvents, GenericEvents, NamedEvents, PathEvents, FilterHashtableEvents, FilterXmlEvents
Aliases: NumberOfThreads
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxEvents
Maximum number of events to return.

```yaml
Type: Int64
Parameter Sets: ProviderEvents, GenericEvents, NamedEvents, PathEvents, FilterHashtableEvents, FilterXmlEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxEventsScanned
Maximum number of merged candidate events delivered for message and checkpoint filtering.
Zero continues until the output limit is satisfied or the query is exhausted. Native selection may perform
one initial lookahead per machine/XPath chunk plus bounded page prefetch; those rows are not evaluated by the cmdlet.

```yaml
Type: Int64
Parameter Sets: ProviderEvents, GenericEvents, NamedEvents, PathEvents, FilterHashtableEvents, FilterXmlEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MessageCulture
Culture used to format provider messages and display names for offline EVTX queries.
For example, use en-US for deterministic English output.

```yaml
Type: CultureInfo
Parameter Sets: ProviderEvents, GenericEvents, NamedEvents, PathEvents, FilterHashtableEvents, FilterXmlEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MessageRegex
Filters events by matching their formatted message against the provided regular expression.

```yaml
Type: Regex
Parameter Sets: ProviderEvents, GenericEvents, NamedEvents, PathEvents, FilterHashtableEvents, FilterXmlEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NamedDataExcludeFilter
Hashtable filter to exclude named event data when querying files.

```yaml
Type: Hashtable
Parameter Sets: ProviderEvents, GenericEvents, PathEvents, FilterHashtableEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NamedDataFilter
Hashtable filter for named event data when querying files.

```yaml
Type: Hashtable
Parameter Sets: ProviderEvents, GenericEvents, PathEvents, FilterHashtableEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Oldest
Reads events from oldest to newest when querying files.

```yaml
Type: SwitchParameter
Parameter Sets: ProviderEvents, GenericEvents, NamedEvents, PathEvents, FilterHashtableEvents, FilterXmlEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
Path to an event log file for offline analysis.

```yaml
Type: String[]
Parameter Sets: PathEvents
Aliases: PSPath
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -ProviderName
Event provider name to filter results.

```yaml
Type: String[]
Parameter Sets: ProviderEvents, GenericEvents, PathEvents
Aliases: Source, Provider
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ReadMode
Controls per-event materialization. Metadata skips provider messages, XML, attachments, and bookmarks;
Message formats the provider message; StructuredData parses XML without formatting the message; Full includes all data.
Named-event queries default to Full so rule projections receive their structured payload; other query sets default to Message.

```yaml
Type: EventReadMode
Parameter Sets: ProviderEvents, GenericEvents, NamedEvents, PathEvents, FilterHashtableEvents, FilterXmlEvents
Aliases: None
Possible values: Metadata, Message, StructuredData, RawXml, Full

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -RecordIdFile
Path to a file storing last processed record ID.

```yaml
Type: String
Parameter Sets: ProviderEvents, GenericEvents, NamedEvents, PathEvents, FilterHashtableEvents, FilterXmlEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -RecordIdKey
Identifier used when persisting record IDs to allow multiple jobs to share a file.

```yaml
Type: String
Parameter Sets: ProviderEvents, GenericEvents, NamedEvents, PathEvents, FilterHashtableEvents, FilterXmlEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ResolveDns
Resolves reverse-DNS names for supported named events after projection. DNS failures remain visible on the
event and never remove the event from the pipeline.

```yaml
Type: SwitchParameter
Parameter Sets: NamedEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -SessionTimeoutMs
Overrides both remote connection and no-progress read timeouts in milliseconds.
Zero uses Settings.SessionTimeoutMs for connection establishment and
Settings.QuerySessionTimeoutMs for reading.

```yaml
Type: Int32
Parameter Sets: ProviderEvents, GenericEvents, NamedEvents, FilterHashtableEvents, FilterXmlEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StartTime
Start time for the event query.

```yaml
Type: DateTime
Parameter Sets: ProviderEvents, GenericEvents, NamedEvents, PathEvents
Aliases: DateFrom
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TimePeriod
Relative time period for filtering events.

```yaml
Type: TimePeriod
Parameter Sets: ProviderEvents, GenericEvents, NamedEvents, PathEvents
Aliases: None
Possible values: PastHour, CurrentHour, PastDay, CurrentDay, PastMonth, CurrentMonth, PastQuarter, CurrentQuarter, Last3Days, Last7Days, Last14Days, Last1Hour, Last2Hours, Last3Hours, Last6Hours, Last12Hours, Last16Hours, Last24Hours, Today, Yesterday, Everything, TillLastMonday, TillLastTuesday, TillLastWednesday, TillLastThursday, TillLastFriday, TillLastSaturday, TillLastSunday

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TolerateQueryErrors
Allows a structured QueryList to continue when one path cannot be evaluated.

```yaml
Type: SwitchParameter
Parameter Sets: FilterHashtableEvents, FilterXmlEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Type
Predefined named events to query.

```yaml
Type: NamedEvents[]
Parameter Sets: NamedEvents
Aliases: NamedEvents
Possible values: ADComputerCreateChange, ADComputerDeleted, ADComputerChangeDetailed, ADGroupMembershipChange, ADGroupEnumeration, ADGroupChange, ADGroupCreateDelete, ADGroupChangeDetailed, ADGroupPolicyChanges, ADGroupPolicyEdits, ADGroupPolicyLinks, ADGroupPolicyChangesDetailed, GpoCreated, GpoDeleted, GpoModified, ADLdapBindingSummary, ADLdapBindingDetails, ADUserCreateChange, ADUserStatus, ADUserChangeDetailed, ADUserLockouts, ADUserLogon, ADUserLogonNTLMv1, ADUserLogonFailed, ADUserUnlocked, ADUserPrivilegeUse, ADUserRightsAssignment, KerberosTGTRequest, KerberosServiceTicket, KerberosTicketFailure, KerberosPolicyChange, ADOrganizationalUnitChangeDetailed, ADOtherChangeDetailed, ADSMBServerAuditV1, LogsClearedSecurity, LogsClearedOther, LogsFullSecurity, NetworkAccessAuthenticationPolicy, CertificateIssued, AuditPolicyChange, FirewallRuleChange, DhcpLeaseCreated, BitLockerKeyChange, BitLockerSuspended, DeviceRecognized, DeviceDisabled, ObjectDeletion, ScheduledTaskDeleted, ScheduledTaskCreated, OSCrash, OSBugCheck, OSStartup, OSShutdown, OSUncleanShutdown, OSStartupSecurity, OSCrashOnAuditFailRecovery, OSTimeChange, WindowsUpdateFailure, ClientGroupPoliciesApplication, ClientGroupPoliciesSystem, HyperVVirtualMachineShutdown, HyperVVirtualMachineStarted, IISSiteBindingFailure, HyperVCheckpointCreated, IISSiteStopped, ExchangeDatabaseMounted, DfsReplicationError, SqlDatabaseCreated, SyncCompleted, AADConnectStagingEnabled, AADConnectStagingDisabled, AADConnectPasswordSyncFailed, AADConnectRunProfile, AADSyncCycleStage, AADSyncProvisionCredentialsPing, AADSyncPasswordHashSyncStatus, AADSyncImportStatus, AADSyncFilterStatus, NetworkMonitorDriverLoaded, NetworkPromiscuousMode

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -UserId
User identifier used to filter events.

```yaml
Type: String[]
Parameter Sets: ProviderEvents, GenericEvents, PathEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.String[]`
- `System.Collections.Hashtable[]`
- `System.Xml.XmlDocument`

## OUTPUTS

- `EventViewerX.EventObject`
- `EventViewerX.EventObjectSlim`

## RELATED LINKS

- None
