---
external help file: PSEventViewer-help.xml
Module Name: PSEventViewer
online version:
schema: 2.0.0
---

# Set-WinEventSettings

## SYNOPSIS
Updates event log settings such as size and log mode.

## SYNTAX

```
Set-WinEventSettings -LogName <String> [-ComputerName <String>] [-MaximumSizeMB <Int32>] [-MaximumSizeInBytes <Int64>]
 [-EventAction <String>] [-Mode <EventLogMode>] [<CommonParameters>]
```

## DESCRIPTION
`Set-WinEventSettings` modifies properties of an event log using the native
.NET `EventLogConfiguration` API.

## EXAMPLES

### Example 1
```powershell
Set-WinEventSettings -LogName 'Application' -MaximumSizeMB 20 -Mode Circular
```

Sets the Application log to a maximum size of 20 MB and circular logging.

## PARAMETERS

### -ComputerName
Specifies a remote computer. If omitted the local machine is used.

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

### -EventAction
Indicates how the log behaves when it reaches its maximum size.

```yaml
Type: String
Parameter Sets: (All)
Aliases:
Accepted values: OverwriteEventsAsNeededOldestFirst, ArchiveTheLogWhenFullDoNotOverwrite, DoNotOverwriteEventsClearLogManually, None

Required: False
Position: 3
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LogName
Name of the event log to configure (for example `Application`).

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

### -MaximumSizeMB
Desired maximum log size in megabytes.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None

## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
