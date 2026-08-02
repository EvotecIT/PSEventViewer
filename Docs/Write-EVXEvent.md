---
external help file: PSEventViewer-help.xml
Module Name: PSEventViewer
online version: https://github.com/EvotecIT/PSEventViewer
schema: 2.0.0
---
# Write-EVXEvent
## SYNOPSIS
Writes a registered manifest/ETW event using positional, named, or typed schema values.

Resolves and caches the exact registered event schema, validates every value, converts values according to native Windows types, and writes through the dependency-free EventViewerX engine. Named hashtable order does not matter.

EventName is available for providers installed through an EventViewerX .evxprovider package. ProviderName plus Id works with any registered manifest provider. Use Write-EVXEntry for classic Event Log sources.

## SYNTAX
### ByIdPayload (Default)
```powershell
Write-EVXEvent [-ProviderName] <string> [-Id] <int> [[-Payload] <Object[]>] [-Version <byte>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### ByIdData
```powershell
Write-EVXEvent [-ProviderName] <string> [-Id] <int> [-Data] <IDictionary> [-Version <byte>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### ByNameData
```powershell
Write-EVXEvent [-ProviderName] <string> [-EventName] <string> [-Data] <IDictionary> [-Version <byte>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Writes a registered manifest/ETW event using positional, named, or typed schema values.

Resolves and caches the exact registered event schema, validates every value, converts values according to native Windows types, and writes through the dependency-free EventViewerX engine. Named hashtable order does not matter.

EventName is available for providers installed through an EventViewerX .evxprovider package. ProviderName plus Id works with any registered manifest provider. Use Write-EVXEntry for classic Event Log sources.

## EXAMPLES

### EXAMPLE 1
```powershell
PS> Write-EVXEvent -ProviderName Contoso.Scanner -EventName ScanCompleted -Data @{ FindingCount = 7; ComputerName = $env:COMPUTERNAME }
```

Maps values to the manifest's canonical order by field name and writes the event.

### EXAMPLE 2
```powershell
PS> Write-EVXEvent -ProviderName Microsoft-Windows-PowerShell -Id 45090 -Payload Workflow, Running
```

Uses the positional compatibility surface for an existing Windows provider.

## PARAMETERS

### -Data
Hashtable of values keyed by manifest field name. Key order is ignored. Accepts pipeline input for efficient repeated writes with one cached native registration.

```yaml
Type: IDictionary
Parameter Sets: ByIdData, ByNameData
Aliases: None
Possible values:

Required: True
Position: 2
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -EventName
Friendly event name from an EventViewerX-managed provider package.

```yaml
Type: String
Parameter Sets: ByNameData
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Id
Event identifier declared by the provider manifest.

```yaml
Type: Int32
Parameter Sets: ByIdPayload, ByIdData
Aliases: EventId
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Payload
Ordered values for the compatibility surface. Prefer Data for custom providers.

```yaml
Type: Object[]
Parameter Sets: ByIdPayload
Aliases: None
Possible values:

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProviderName
Name of a registered local manifest event provider.

```yaml
Type: String
Parameter Sets: ByIdPayload, ByIdData, ByNameData
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Version
Event version. Required when the selected identity has multiple schema versions.

```yaml
Type: Nullable`1
Parameter Sets: ByIdPayload, ByIdData, ByNameData
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

- `System.Collections.IDictionary`

## OUTPUTS

- `EventViewerX.ManifestEventWriteResult`

## RELATED LINKS

- None
