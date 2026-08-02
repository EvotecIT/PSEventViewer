---
external help file: PSEventViewer-help.xml
Module Name: PSEventViewer
online version: https://github.com/EvotecIT/PSEventViewer
schema: 2.0.0
---
# Get-EVXEventStatistics
## SYNOPSIS
Builds bounded statistics from a live Windows event log.

Scans event metadata without formatting messages or parsing XML and reports top event IDs, providers, levels, computers, and the observed time range.

## SYNTAX
### __AllParameterSets
```powershell
Get-EVXEventStatistics [-LogName] <string> [-MachineName <string>] [-XPath <string>] [-MaxEvents <long>] [-Oldest] [-StartTime <datetime>] [-EndTime <datetime>] [-Top <int>] [-SessionTimeoutMs <int>] [<CommonParameters>]
```

## DESCRIPTION
Builds bounded statistics from a live Windows event log.

Scans event metadata without formatting messages or parsing XML and reports top event IDs, providers, levels, computers, and the observed time range.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-EVXEventStatistics -LogName Security -MaxEvents 50000
```

Scans up to 50,000 events using the metadata-only projection.

### EXAMPLE 2
```powershell
Get-EVXEventStatistics -LogName System -MachineName AD1.ad.evotec.xyz -MaxEvents 10000
```

Returns a typed statistics result or a PowerShell error with the underlying failure category.

## PARAMETERS

### -EndTime
Optional inclusive upper time bound.

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

### -LogName
Event log channel to scan.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MachineName
Optional target computer. The local computer is used by default.

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

### -MaxEvents
Maximum number of events to scan. Zero removes the limit.

```yaml
Type: Int64
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Oldest
Reads from oldest to newest.

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

### -SessionTimeoutMs
Session and per-read timeout in milliseconds.

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

### -StartTime
Optional inclusive lower time bound.

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

### -Top
Number of entries retained for each top-N group.

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

### -XPath
Optional XPath filter. All events are selected by default.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `EventViewerX.Reports.Live.LiveStatsQueryResult`

## RELATED LINKS

- None
