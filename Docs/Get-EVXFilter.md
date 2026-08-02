---
external help file: PSEventViewer-help.xml
Module Name: PSEventViewer
online version: https://github.com/EvotecIT/PSEventViewer
schema: 2.0.0
---
# Get-EVXFilter
## SYNOPSIS
Generates XPath filters for Windows Event Log queries.

Produces filter strings compatible with Get-WinEvent -FilterXPath and Event Viewer Custom Views, supporting include/exclude IDs, time windows, providers, users, keywords, levels, and named data.

## SYNTAX
### __AllParameterSets
```powershell
Get-EVXFilter [-ID <string[]>] [-EventRecordID <string[]>] [-StartTime <datetime>] [-EndTime <datetime>] [-Data <string[]>] [-ProviderName <string[]>] [-Keywords <long[]>] [-Level <string[]>] [-UserID <string[]>] [-NamedDataFilter <hashtable[]>] [-NamedDataExcludeFilter <hashtable[]>] [-ExcludeID <string[]>] [-LogName <string>] [-Path <string>] [-XPathOnly] [<CommonParameters>]
```

## DESCRIPTION
Generates XPath filters for Windows Event Log queries.

Produces filter strings compatible with Get-WinEvent -FilterXPath and Event Viewer Custom Views, supporting include/exclude IDs, time windows, providers, users, keywords, levels, and named data.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-EVXFilter -ID 4624,4625 -LogName Security
```

Returns an XPath that matches successful and failed logons.

### EXAMPLE 2
```powershell
Get-EVXFilter -ProviderName "Microsoft-Windows-Security-Auditing" -StartTime (Get-Date).AddHours(-4)
```

Limits results to the auditing provider over the last four hours.

### EXAMPLE 3
```powershell
Get-EVXFilter -NamedDataFilter @{ TargetUserName='alice' } -ExcludeID 4723
```

Matches events where TargetUserName is alice while excluding password-change attempts.

### EXAMPLE 4
```powershell
Get-EVXFilter -ID 1102 -LogName Security -XPathOnly
```

Emits only the XPath expression for use with custom tooling.

## PARAMETERS

### -Data
Specific event data values to filter on.

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

### -EndTime
End time for the filter range.

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

### -EventRecordID
Event record identifiers to include in the filter.

```yaml
Type: String[]
Parameter Sets: __AllParameterSets
Aliases: RecordID
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExcludeID
Event identifiers to exclude from the filter.

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

### -ID
Event identifiers to include in the filter.

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

### -Keywords
Keywords to include in the filter.

```yaml
Type: Int64[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Level
Event levels to include in the filter.

```yaml
Type: String[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Critical, Error, Informational, LogAlways, Verbose, Warning

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LogName
Name of the log associated with the filter.

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

### -NamedDataExcludeFilter
Hashtable specifying named data to exclude from the filter.

```yaml
Type: Hashtable[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NamedDataFilter
Hashtable specifying named data filters.

```yaml
Type: Hashtable[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
Path of the log file to generate the filter for.

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

### -ProviderName
Provider names to include in the filter.

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

### -StartTime
Start time for the filter range.

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

### -UserID
User identifiers to include in the filter.

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

### -XPathOnly
When set, outputs only the XPath expression without formatting.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `System.String`

## RELATED LINKS

- None
