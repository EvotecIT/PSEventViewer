---
external help file: PSEventViewer-help.xml
Module Name: PSEventViewer
online version:
schema: 2.0.0
---

# Get-Events

## SYNOPSIS
Get-Events is a wrapper function around Get-WinEvent providing additional features and options.

## SYNTAX

```
Get-Events [[-Machine] <String[]>] [[-DateFrom] <DateTime>] [[-DateTo] <DateTime>] [[-ID] <Int32[]>]
 [[-ExcludeID] <Int32[]>] [[-LogName] <String>] [[-ProviderName] <String[]>] [[-NamedDataFilter] <Hashtable>]
 [[-NamedDataExcludeFilter] <Hashtable>] [[-UserID] <String[]>] [[-Level] <Level[]>] [[-UserSID] <String>]
 [[-Data] <String[]>] [[-MaxEvents] <Int32>] [[-Credential] <PSCredential>] [[-Path] <String>]
 [[-Keywords] <Keywords[]>] [[-RecordID] <Int64>] [[-MaxRunspaces] <Int32>] [-Oldest] [-DisableParallel]
 [-ExtendedOutput] [[-ExtendedInput] <Array>] [<CommonParameters>]
```

## DESCRIPTION
Get-Events is a wrapper function around Get-WinEvent providing additional features and options exposing most of the Get-WinEvent functionality in easy to use manner.

## EXAMPLES

### EXAMPLE 1
```
Get-Events -LogName 'Application' -ID 1001 -MaxEvents 1 -Verbose -DisableParallel
```

### EXAMPLE 2
```
Get-Events -LogName 'Setup' -ID 2 -ComputerName 'AD1' -MaxEvents 1 -Verbose | Format-List *
```

### EXAMPLE 3
```
Get-Events -LogName 'Setup' -ID 2 -ComputerName 'AD1','AD2','AD3' -MaxEvents 1 -Verbose | Format-List *
```

### EXAMPLE 4
```
Get-Events -LogName 'Security' -ID 5379 -RecordID 19626 -Verbose
```

### EXAMPLE 5
```
Get-Events -LogName 'System' -ID 1001,1018 -ProviderName 'Microsoft-Windows-WER-SystemErrorReporting' -Verbose
```

Get-Events -LogName 'System' -ID 42,41,109 -ProviderName 'Microsoft-Windows-Kernel-Power' -Verbose
Get-Events -LogName 'System' -ID 1,12,13 -ProviderName 'Microsoft-Windows-Kernel-General' -Verbose
Get-Events -LogName 'System' -ID 6005,6006,6008,6013 -ProviderName 'EventLog' -Verbose

### EXAMPLE 6
```
$List = @(
@{ Server = 'AD1'; LogName = 'Security'; EventID = '5136', '5137'; Type = 'Computer' }
    @{ Server = 'AD2'; LogName = 'Security'; EventID = '5136', '5137'; Type = 'Computer' }
    @{ Server = 'C:\MyEvents\Archive-Security-2018-08-21-23-49-19-424.evtx'; LogName = 'Security'; EventID = '5136', '5137'; Type = 'File' }
    @{ Server = 'C:\MyEvents\Archive-Security-2018-09-15-09-27-52-679.evtx'; LogName = 'Security'; EventID = '5136', '5137'; Type = 'File' }
    @{ Server = 'Evo1'; LogName = 'Setup'; EventID = 2; Type = 'Computer'; }
    @{ Server = 'AD1.ad.evotec.xyz'; LogName = 'Security'; EventID = 4720, 4738, 5136, 5137, 5141, 4722, 4725, 4767, 4723, 4724, 4726, 4728, 4729, 4732, 4733, 4746, 4747, 4751, 4752, 4756, 4757, 4761, 4762, 4785, 4786, 4787, 4788, 5136, 5137, 5141, 5136, 5137, 5141, 5136, 5137, 5141; Type = 'Computer' }
    @{ Server = 'Evo1'; LogName = 'Security'; Type = 'Computer'; MaxEvents = 15; Keywords = 'AuditSuccess' }
    @{ Server = 'Evo1'; LogName = 'Security'; Type = 'Computer'; MaxEvents = 15; Level = 'Informational'; Keywords = 'AuditFailure' }
)
$Output = Get-Events -ExtendedInput $List -Verbose
$Output | Format-Table Computer, Date, LevelDisplayName

### EXAMPLE 7
```
Get-Events -MaxEvents 2 -LogName 'Security' -ComputerName 'AD1.AD.EVOTEC.XYZ','AD2' -ID 4720, 4738, 5136, 5137, 5141, 4722, 4725, 4767, 4723, 4724, 4726, 4728, 4729, 4732, 4733, 4746, 4747, 4751, 4752, 4756, 4757, 4761, 4762, 4785, 4786, 4787, 4788, 5136, 5137, 5141, 5136, 5137, 5141, 5136, 5137, 5141 -Verbose
```

## PARAMETERS

### -Machine
Specifies the name of the computer that this cmdlet gets events from the event logs.
Type the NetBIOS name, an IP address, or the fully qualified domain name (FQDN) of the computer.
The default value is the local computer, localhost.
This parameter accepts only one computer name at a time.

To get event logs from remote computers, configure the firewall port for the event log service to allow remote access.

This cmdlet does not rely on PowerShell remoting.
You can use the ComputerName parameter even if your computer is not configured to run remote commands.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: ADDomainControllers, DomainController, Server, Servers, Computer, Computers, ComputerName

Required: False
Position: 1
Default value: $Env:COMPUTERNAME
Accept pipeline input: False
Accept wildcard characters: False
```

### -DateFrom
Specifies the date and time of the earliest event in the event log you want to search for.

```yaml
Type: DateTime
Parameter Sets: (All)
Aliases: StartTime, From

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DateTo
Specifies the date and time of the latest event in the event log you want to search for.

```yaml
Type: DateTime
Parameter Sets: (All)
Aliases: EndTime, To

Required: False
Position: 3
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ID
Specifies the event ID (or events) of the event you want to search for.
If provided more than 23 the cmdlet will split the events into multiple queries automatically.

```yaml
Type: Int32[]
Parameter Sets: (All)
Aliases: Ids, EventID, EventIds

Required: False
Position: 4
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExcludeID
Specifies the event ID (or events) of the event you want to exclude from the search.
If provided more than 23 the cmdlet will split the events into multiple queries automatically.

```yaml
Type: Int32[]
Parameter Sets: (All)
Aliases: ExludeEventID

Required: False
Position: 5
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LogName
Specifies the event logs that this cmdlet get events from.
Enter the event log names in a comma-separated list.
Wildcards are permitted.

```yaml
Type: String
Parameter Sets: (All)
Aliases: LogType, Log

Required: False
Position: 6
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProviderName
Specifies, as a string array, the event log providers from which this cmdlet gets events.
Enter the provider names in a comma-separated list, or use wildcard characters to create provider name patterns.

An event log provider is a program or service that writes events to the event log.
It is not a PowerShell provider.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: Provider, Source

Required: False
Position: 7
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NamedDataFilter
Provide NamedDataFilter in specific form to optimize search performance looking for specific events.

```yaml
Type: Hashtable
Parameter Sets: (All)
Aliases:

Required: False
Position: 8
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NamedDataExcludeFilter
Provide NamedDataExcludeFilter in specific form to optimize search performance looking for specific events.

```yaml
Type: Hashtable
Parameter Sets: (All)
Aliases:

Required: False
Position: 9
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -UserID
The UserID key can take a valid security identifier (SID) or a domain account name that can be used to construct a valid System.Security.Principal.NTAccount object.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 10
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Level
Define the event level that this cmdlet gets events from.
Options are Verbose, Informational, Warning, Error, Critical, LogAlways

```yaml
Type: Level[]
Parameter Sets: (All)
Aliases:
Accepted values: LogAlways, Critical, Error, Warning, Informational, Verbose

Required: False
Position: 11
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -UserSID
Search events by UserSID

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 12
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Data
The Data value takes event data in an unnamed field.
For example, events in classic event logs.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 13
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxEvents
Specifies the maximum number of events that are returned.
Enter an integer such as 100.
The default is to return all the events in the logs or files.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: 14
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Credential
Specifies the name of the computer that this cmdlet gets events from the event logs.
Type the NetBIOS name, an IP address, or the fully qualified domain name (FQDN) of the computer.
The default value is the local computer, localhost.
This parameter accepts only one computer name at a time.

To get event logs from remote computers, configure the firewall port for the event log service to allow remote access.

This cmdlet does not rely on PowerShell remoting.
You can use the ComputerName parameter even if your computer is not configured to run remote commands.

```yaml
Type: PSCredential
Parameter Sets: (All)
Aliases: Credentials

Required: False
Position: 15
Default value: [System.Management.Automation.PSCredential]::Empty
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
Specifies the path to the event log files that this cmdlet get events from.
Enter the paths to the log files in a comma-separated list, or use wildcard characters to create file path patterns.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 16
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Keywords
Define keywords to search for by their name.
Available keywords are: AuditFailure, AuditSuccess, CorrelationHint2, EventLogClassic, Sqm, WdiDiagnostic, WdiContext, ResponseTime, None

```yaml
Type: Keywords[]
Parameter Sets: (All)
Aliases:
Accepted values: None, ResponseTime, WdiContext, WdiDiagnostic, Sqm, AuditFailure, AuditSuccess, CorrelationHint2, EventLogClassic

Required: False
Position: 17
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -RecordID
Find a single event in the event log using it's RecordId

```yaml
Type: Int64
Parameter Sets: (All)
Aliases: EventRecordID

Required: False
Position: 18
Default value: 0
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxRunspaces
Limit the number of concurrent runspaces that can be used to process the events.
By default it uses $env:NUMBER_OF_PROCESSORS + 1

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: 19
Default value: [int]$env:NUMBER_OF_PROCESSORS + 1
Accept pipeline input: False
Accept wildcard characters: False
```

### -Oldest
Indicate that this cmdlet gets the events in oldest-first order.
By default, events are returned in newest-first order.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -DisableParallel
Disables parallel processing of the events.
By default, events are processed in parallel.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExtendedOutput
Indicates that this cmdlet returns an extended set of output parameters.
By default, this cmdlet does not generate any extended output.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExtendedInput
Indicates that this cmdlet takes an extended set of input parameters.
Extended input is used by PSWinReportingV2 to provide special input parameters.

```yaml
Type: Array
Parameter Sets: (All)
Aliases:

Required: False
Position: 20
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

## NOTES
General notes

## RELATED LINKS
