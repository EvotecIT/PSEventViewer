---
external help file: PSEventViewer-help.xml
Module Name: PSEventViewer
online version: https://github.com/EvotecIT/PSEventViewer
schema: 2.0.0
---
# ConvertTo-EVXProviderDefinition
## SYNOPSIS
Converts a friendly hashtable or custom object into a validated provider definition.

Accepts concise PowerShell aliases such as ProviderName, ProviderGuid, Version, Message, and ordered field hashtables while retaining the complete typed EventViewerX provider schema for advanced channels, levels, tasks, opcodes, keywords, maps, localization, and versioned events.

## SYNTAX
### __AllParameterSets
```powershell
ConvertTo-EVXProviderDefinition [-InputObject] <Object> [<CommonParameters>]
```

## DESCRIPTION
Converts a friendly hashtable or custom object into a validated provider definition.

Accepts concise PowerShell aliases such as ProviderName, ProviderGuid, Version, Message, and ordered field hashtables while retaining the complete typed EventViewerX provider schema for advanced channels, levels, tasks, opcodes, keywords, maps, localization, and versioned events.

## EXAMPLES

### EXAMPLE 1
```powershell
PS> $definition = @{ ProviderName = 'Contoso.Scanner'; ProviderGuid = [guid]::NewGuid(); Version = '1.0.0'; Events = @{ Name = 'ScanCompleted'; Id = 1000; Message = 'Scan of {ComputerName} found {FindingCount} issues.'; Fields = [ordered]@{ ComputerName = 'String'; FindingCount = 'UInt32' } } } | ConvertTo-EVXProviderDefinition
```

Creates the default Contoso.Scanner/Operational channel and returns a strongly typed EventProviderDefinition.

## PARAMETERS

### -InputObject
Typed definition, hashtable, or custom object to convert.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Object`

## OUTPUTS

- `EventViewerX.Providers.EventProviderDefinition`

## RELATED LINKS

- None
