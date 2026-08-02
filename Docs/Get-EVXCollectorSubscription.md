---
external help file: PSEventViewer-help.xml
Module Name: PSEventViewer
online version: https://github.com/EvotecIT/PSEventViewer
schema: 2.0.0
---
# Get-EVXCollectorSubscription
## SYNOPSIS
Returns normalized Windows Event Collector subscription configuration.

Reads local or remote WEC subscription inventory and returns detached snapshots with normalized XML details and query definitions. Remote access uses the caller's Windows identity.

## SYNTAX
### __AllParameterSets
```powershell
Get-EVXCollectorSubscription [[-Name] <string[]>] [-MachineName <string[]>] [-EnabledOnly] [<CommonParameters>]
```

## DESCRIPTION
Returns normalized Windows Event Collector subscription configuration.

Reads local or remote WEC subscription inventory and returns detached snapshots with normalized XML details and query definitions. Remote access uses the caller's Windows identity.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-EVXCollectorSubscription -EnabledOnly
```

Returns only enabled subscriptions from the local collector.

### EXAMPLE 2
```powershell
Get-EVXCollectorSubscription -Name '*Domain Controllers*' -MachineName WEC01
```

Uses Remote Registry access under the current Windows identity and applies wildcard matching to detached snapshots.

## PARAMETERS

### -EnabledOnly
Returns only enabled subscriptions.

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
Collector computers. Omit for the local computer.

```yaml
Type: String[]
Parameter Sets: __AllParameterSets
Aliases: ComputerName, ServerName
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -Name
Subscription names or wildcard patterns.

```yaml
Type: String[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.String[]`

## OUTPUTS

- `EventViewerX.CollectorSubscriptionSnapshot`

## RELATED LINKS

- None
