---
external help file: PSEventViewer-help.xml
Module Name: PSEventViewer
online version:
schema: 2.0.0
---

# Get-Events

## SYNOPSIS

## SYNTAX

```
Get-Events [[-Machine] <String[]>] [[-DateFrom] <DateTime>] [[-DateTo] <DateTime>] [[-ID] <Int32[]>]
 [[-ExcludeID] <Int32[]>] [[-LogName] <String>] [[-ProviderName] <String>] [[-NamedDataFilter] <Hashtable>]
 [[-Level] <Int32>] [[-UserSID] <String>] [[-Data] <String[]>] [[-MaxEvents] <Int32>]
 [[-Credentials] <PSCredential>] [[-Path] <String>] [[-Keywords] <Int64[]>] [[-RecordID] <Int64>]
 [[-MaxRunspaces] <Int32>] [-Oldest] [-DisableParallel] [<CommonParameters>]
```

## DESCRIPTION
{{Fill in the Description}}

## EXAMPLES

### EXAMPLE 1
```
$DateFrom = (get-date).AddHours(-5)
```

$DateTo = (get-date).AddHours(1)
get-events -Computer "Evo1" -DateFrom $DateFrom -DateTo $DateTo -EventId 916 -LogType "Application"

## PARAMETERS

### -Machine
{{Fill Machine Description}}

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
{{Fill DateFrom Description}}

```yaml
Type: DateTime
Parameter Sets: (All)
Aliases: From

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DateTo
{{Fill DateTo Description}}

```yaml
Type: DateTime
Parameter Sets: (All)
Aliases: To

Required: False
Position: 3
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ID
{{Fill ID Description}}

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
{{Fill ExcludeID Description}}

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
{{Fill LogName Description}}

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
{{Fill ProviderName Description}}

```yaml
Type: String
Parameter Sets: (All)
Aliases: Provider

Required: False
Position: 7
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NamedDataFilter
{{Fill NamedDataFilter Description}}

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

### -Level
{{Fill Level Description}}

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: 9
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -UserSID
{{Fill UserSID Description}}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 10
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Data
{{Fill Data Description}}

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 11
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxEvents
{{Fill MaxEvents Description}}

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: 12
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Credentials
{{Fill Credentials Description}}

```yaml
Type: PSCredential
Parameter Sets: (All)
Aliases:

Required: False
Position: 13
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
{{Fill Path Description}}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 14
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Keywords
{{Fill Keywords Description}}

```yaml
Type: Int64[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 15
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -RecordID
{{Fill RecordID Description}}

```yaml
Type: Int64
Parameter Sets: (All)
Aliases: EventRecordID

Required: False
Position: 16
Default value: 0
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxRunspaces
{{Fill MaxRunspaces Description}}

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: 17
Default value: [int]$env:NUMBER_OF_PROCESSORS + 1
Accept pipeline input: False
Accept wildcard characters: False
```

### -Oldest
{{Fill Oldest Description}}

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
{{Fill DisableParallel Description}}

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable.
For more information, see about_CommonParameters (http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

## NOTES

## RELATED LINKS
