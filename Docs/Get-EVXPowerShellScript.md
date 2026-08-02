---
external help file: PSEventViewer-help.xml
Module Name: PSEventViewer
online version: https://github.com/EvotecIT/PSEventViewer
schema: 2.0.0
---
# Get-EVXPowerShellScript
## SYNOPSIS
Retrieves PowerShell scripts from event logs and optionally saves them.

## SYNTAX
### __AllParameterSets
```powershell
Get-EVXPowerShellScript [-Type] <PowerShellEdition> [-Path <string>] [-Format] [-ContainsText <string[]>] [-MaxScripts <int>] [-MaxPendingScripts <int>] [-MaxCachedEvents <int>] [-MachineName <string[]>] [-EventLogPath <string>] [-DateFrom <datetime>] [-DateTo <datetime>] [-MaxEventsScanned <int>] [-IncludeQueryInfo] [<CommonParameters>]
```

## DESCRIPTION
Retrieves PowerShell scripts from event logs and optionally saves them.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-EVXPowerShellScript -Path 'C:\Path'
```


## PARAMETERS

### -ContainsText
Filters scripts to those containing the specified text.

```yaml
Type: String[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

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

### -Format
When set, converts scripts back to their original formatting.

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

### -MaxCachedEvents
Maximum event snapshots retained across incomplete script groups.

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

### -MaxPendingScripts
Maximum incomplete script groups retained while scanning.

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

### -MaxScripts
Maximum reconstructed scripts to return per computer. Zero returns every matching script.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: MaxEvents
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
Destination directory where retrieved scripts should be saved.

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

- `EventViewerX.RestoredPowerShellScript`
- `EventViewerX.PowerShellScriptQueryExecutionInfo`
- `System.String`

## RELATED LINKS

- None
