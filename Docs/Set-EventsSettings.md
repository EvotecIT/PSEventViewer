---
external help file: PSEventViewer-help.xml
Module Name: PSEventViewer
online version:
schema: 2.0.0
---

# Set-EventsSettings

## SYNOPSIS
{{ Fill in the Synopsis }}

## SYNTAX

```
Set-EventsSettings [[-LogName] <String>] [[-ComputerName] <String>] [[-MaximumSizeMB] <Int32>]
 [[-EventAction] <String>] [<CommonParameters>]
```

## DESCRIPTION
{{ Fill in the Description }}

## EXAMPLES

### Example 1
```powershell
PS C:\> {{ Add example code here }}
```

{{ Add example description here }}

## PARAMETERS

### -ComputerName
{{ Fill ComputerName Description }}

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
{{ Fill EventAction Description }}

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
{{ Fill LogName Description }}

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
{{ Fill MaximumSizeMB Description }}

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
