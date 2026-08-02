---
external help file: PSEventViewer-help.xml
Module Name: PSEventViewer
online version: https://github.com/EvotecIT/PSEventViewer
schema: 2.0.0
---
# Get-EVXPowerShellScriptExecution
## SYNOPSIS
Retrieves PowerShell execution-context events from live operational logs or exported EVTX files.

## SYNTAX
### __AllParameterSets
```powershell
Get-EVXPowerShellScriptExecution [-Type] <PowerShellEdition> [-MaxEvents <int>] [-MachineName <string[]>] [-EventLogPath <string>] [-DateFrom <datetime>] [-DateTo <datetime>] [-MaxEventsScanned <int>] [-IncludeQueryInfo] [<CommonParameters>]
```

## DESCRIPTION
Retrieves PowerShell execution-context events from live operational logs or exported EVTX files.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-EVXPowerShellScriptExecution -EventLogPath 'C:\Path'
```


## PARAMETERS

### -DateFrom
{{ Fill DateFrom Description }}

```yaml
Type: Nullable`1
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DateTo
{{ Fill DateTo Description }}

```yaml
Type: Nullable`1
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EventLogPath
{{ Fill EventLogPath Description }}

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IncludeQueryInfo
{{ Fill IncludeQueryInfo Description }}

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MachineName
{{ Fill MachineName Description }}

```yaml
Type: String[]
Parameter Sets: __AllParameterSets
Aliases: ComputerName
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxEvents
Maximum execution records to return per computer. Zero returns every matching record.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxEventsScanned
{{ Fill MaxEventsScanned Description }}

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Type
{{ Fill Type Description }}

```yaml
Type: PowerShellEdition
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: PowerShell, WindowsPowerShell

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `EventViewerX.PowerShellScriptExecutionInfo`
- `EventViewerX.PowerShellScriptQueryExecutionInfo`

## RELATED LINKS

- None
