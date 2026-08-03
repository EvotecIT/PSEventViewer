---
external help file: PSEventViewer-help.xml
Module Name: PSEventViewer
online version: https://github.com/EvotecIT/PSEventViewer
schema: 2.0.0
---
# Get-EVXProvider
## SYNOPSIS
Returns detached Windows Event Log provider metadata.

Supports local and remote provider discovery, wildcard names, deterministic culture, linked channels, levels, tasks, opcodes, keywords, and optional event definitions.

## SYNTAX
### __AllParameterSets
```powershell
Get-EVXProvider [[-Name] <string[]>] [-MachineName <string>] [-Credential <pscredential>] [-Authentication <EventLogAuthentication>] [-TimeoutMs <int>] [-Culture <cultureinfo>] [-IncludeEvents] [-NameOnly] [-AsResult] [<CommonParameters>]
```

## DESCRIPTION
Returns detached Windows Event Log provider metadata.

Supports local and remote provider discovery, wildcard names, deterministic culture, linked channels, levels, tasks, opcodes, keywords, and optional event definitions.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-EVXProvider -Name '*Security*' | Select-Object Name, LogLinks
```

Returns reusable detached metadata rather than disposable ProviderMetadata handles.

### EXAMPLE 2
```powershell
Get-EVXProvider -Name '*IIS*' -NameOnly
```

Outputs only provider names for scripts that need strings.

## PARAMETERS

### -AsResult
Returns one success/failure result for every matching provider.

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

### -Authentication
Authentication package for the remote session.

```yaml
Type: EventLogAuthentication
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Default, Negotiate, Kerberos, Ntlm

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Credential
Credentials for a remote provider catalog.

```yaml
Type: PSCredential
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Culture
Culture used for provider display metadata.

```yaml
Type: CultureInfo
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IncludeEvents
Includes all provider event definitions and templates.

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
Remote computer name. Omit for the local computer.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: ComputerName, ServerName
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
Provider names or wildcard patterns.

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

### -NameOnly
Returns provider names instead of metadata snapshots.

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

### -TimeoutMs
Maximum time for remote RPC preflight and session establishment.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `EventViewerX.EventProviderMetadataSnapshot`
- `EventViewerX.EventProviderCatalogResult`
- `System.String`

## RELATED LINKS

- None
