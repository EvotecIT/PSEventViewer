---
external help file: PSEventViewer-help.xml
Module Name: PSEventViewer
online version: https://github.com/EvotecIT/PSEventViewer
schema: 2.0.0
---
# Set-EVXCollectorSubscription
## SYNOPSIS
Enables or disables an existing local Windows Event Collector subscription.

Uses the supported Windows Event Collector service API, saves the subscription, and verifies the persisted value. Remote registry mutation and wholesale XML replacement are intentionally not exposed.

## SYNTAX
### __AllParameterSets
```powershell
Set-EVXCollectorSubscription [-Name] <string> -Enabled <bool> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Enables or disables an existing local Windows Event Collector subscription.

Uses the supported Windows Event Collector service API, saves the subscription, and verifies the persisted value. Remote registry mutation and wholesale XML replacement are intentionally not exposed.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-EVXCollectorSubscription -Name 'Domain Controllers' -Enabled $false
```

Returns before and after snapshots plus whether the persisted state changed.

## PARAMETERS

### -Enabled
Desired enabled state.

```yaml
Type: Boolean
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
Exact local collector subscription name.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: SubscriptionName
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.String`

## OUTPUTS

- `EventViewerX.CollectorSubscriptionUpdateResult`

## RELATED LINKS

- None
