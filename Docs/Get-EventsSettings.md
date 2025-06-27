---
external help file: PSEventViewer-help.xml
Module Name: PSEventViewer
online version:
schema: 2.0.0
---

# Get-WinEventSettings

## SYNOPSIS
Retrieves settings for a Windows event log.

## SYNTAX

```
Get-WinEventSettings [-LogName] <String> [[-ComputerName] <String>]
 [<CommonParameters>]
```

## DESCRIPTION
Get-WinEventSettings uses the underlying EventViewerX library to read
configuration information for a given Windows event log. It exposes
properties such as log mode and maximum size.

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

### -ComputerName
Remote computer name. If not specified, the local computer is used.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LogName
Name of the event log to query. This parameter is mandatory.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None

## OUTPUTS

### EventViewerX.EventLogDetails
## NOTES

## RELATED LINKS
