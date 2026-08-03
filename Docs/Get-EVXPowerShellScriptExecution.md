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
Get-EVXPowerShellScriptExecution [-Type] <PowerShellEdition> [-MaxEvents <int>] [-MachineName <string[]>] [-EventLogPath <string>] [-DateFrom <DateTime>] [-DateTo <DateTime>] [-MaxEventsScanned <int>] [-IncludeQueryInfo] [<CommonParameters>]
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
Only reads events logged after this date.

```yaml
Type: DateTime
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
Only reads events logged before this date.

```yaml
Type: DateTime
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
Exported EVTX file to query locally instead of a live operational log.
This cannot be combined with MachineName.

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
Emits a machine-readable completion record after each computer.

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
Computer names to query. When omitted, the local machine is used.
This cannot be combined with EventLogPath.

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
Maximum native records to scan per computer. Zero scans the complete query.

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
PowerShell edition whose operational log should be queried.

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
