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
 [[-ExcludeID] <Int32[]>] [[-LogName] <String>] [[-ProviderName] <String>] [[-NamedDataFilter] <Hashtable>]
 [[-NamedDataExcludeFilter] <Hashtable>] [[-UserID] <String[]>] [[-Level] <Level[]>] [[-UserSID] <String>]
 [[-Data] <String[]>] [[-MaxEvents] <Int32>] [[-Credential] <PSCredential>] [[-Path] <String>]
 [[-Keywords] <Keywords[]>] [[-RecordID] <Int64>] [[-MaxRunspaces] <Int32>] [-Oldest] [-DisableParallel]
 [-ExtendedOutput] [[-ExtendedInput] <Array>] [<CommonParameters>]
```

## DESCRIPTION
Long description

## EXAMPLES

### EXAMPLE 1
```
$DateFrom = (get-date).AddHours(-5)
```

$DateTo = (get-date).AddHours(1)
get-events -Computer "Evo1" -DateFrom $DateFrom -DateTo $DateTo -EventId 916 -LogType "Application"

## PARAMETERS

### -Machine
Parameter description

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
Parameter description

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
Parameter description

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
Parameter description

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
Parameter description

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
Parameter description

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
Parameter description

```yaml
Type: String
Parameter Sets: (All)
Aliases: Provider, Source

Required: False
Position: 7
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NamedDataFilter
Parameter description

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
Parameter description

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
Parameter description

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
Parameter description

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
Parameter description

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
Parameter description

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
Parameter description

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
{{ Fill Credential Description }}

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
Parameter description

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
Parameter description

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
Parameter description

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
Parameter description

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
Parameter description

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
Parameter description

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
{{Fill ExtendedOutput Description}}

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
Parameter description

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
