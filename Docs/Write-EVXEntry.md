---
external help file: PSEventViewer-help.xml
Module Name: PSEventViewer
online version: https://github.com/EvotecIT/PSEventViewer
schema: 2.0.0
---
# Write-EVXEntry
## SYNOPSIS
Writes custom events to Windows Event Logs for testing, debugging, or application logging.

Writes through ClassicEventLogManager. A normal write never performs an implicit administrative source registration; use CreateSource explicitly when that behavior is intended.

## SYNTAX
### GenericEvents
```powershell
Write-EVXEntry [-LogName] <string> -ProviderName <string> -EventId <int> -Message <string> [-MachineName <string>] [-Category <int>] [-EventLogEntryType <EventLogEntryType>] [-AdditionalFields <string[]>] [-CreateSource] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### RecordId
```powershell
Write-EVXEntry [-LogName] <string> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Writes custom events to Windows Event Logs for testing, debugging, or application logging.

Writes through ClassicEventLogManager. A normal write never performs an implicit administrative source registration; use CreateSource explicitly when that behavior is intended.

## EXAMPLES

### EXAMPLE 1
```powershell
Write-EVXEntry -LogName Application -ProviderName MyApp -EventId 1000 -Message "Startup complete"
```

Creates an information entry in Application using provider MyApp.

### EXAMPLE 2
```powershell
Write-EVXEntry -MachineName SRV01 -LogName Application -ProviderName MyApp -EventId 2001 -EventLogEntryType Warning -Message "Cache warming delayed"
```

Targets a remote machine and sets the entry type to Warning.

### EXAMPLE 3
```powershell
Write-EVXEntry -LogName Application -ProviderName MyApp -EventId 3001 -Message "User action" -AdditionalFields User:alice Action:Delete
```

Stores extra key/value data alongside the event for later filtering.

### EXAMPLE 4
```powershell
Write-EVXEntry -LogName Application -ProviderName MyApp -EventId 4001 -Category 42 -EventLogEntryType Error -Message "Unhandled exception"
```

Records an error and sets a custom category value.

## PARAMETERS

### -AdditionalFields
Additional custom fields to include with the event.

```yaml
Type: String[]
Parameter Sets: GenericEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Category
Category for the event entry.

```yaml
Type: Int32
Parameter Sets: GenericEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -CreateSource
Explicitly registers a missing source before writing. Source registration normally requires administrative rights.

```yaml
Type: SwitchParameter
Parameter Sets: GenericEvents
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EventId
Identifier for the event entry.

```yaml
Type: Int32
Parameter Sets: GenericEvents
Aliases: Id
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EventLogEntryType
Type of the event log entry.

```yaml
Type: EventLogEntryType
Parameter Sets: GenericEvents
Aliases: EntryType
Possible values: Error, Warning, Information, SuccessAudit, FailureAudit

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LogName
Name of the event log where the entry will be written.

```yaml
Type: String
Parameter Sets: GenericEvents, RecordId
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MachineName
Target computer to write the event to.

```yaml
Type: String
Parameter Sets: GenericEvents
Aliases: ComputerName, ServerName
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Message
Message for the event entry.

```yaml
Type: String
Parameter Sets: GenericEvents
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProviderName
Name of the provider that writes the entry.

```yaml
Type: String
Parameter Sets: GenericEvents
Aliases: Source, Provider
Possible values:

Required: True
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

- `None`

## RELATED LINKS

- None
