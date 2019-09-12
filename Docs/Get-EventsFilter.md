---
external help file: PSEventViewer-help.xml
Module Name: PSEventViewer
online version:
schema: 2.0.0
---

# Get-EventsFilter

## SYNOPSIS
This function generates an xpath filter that can be used with the -FilterXPath
parameter of Get-WinEvent. 
It may also be used inside the \<Select\>\</Select tags
of a Custom View in Event Viewer.

## SYNTAX

```
Get-EventsFilter [[-ID] <String[]>] [[-EventRecordID] <String[]>] [[-StartTime] <DateTime>]
 [[-EndTime] <DateTime>] [[-Data] <String[]>] [[-ProviderName] <String[]>] [[-Keywords] <Int64[]>]
 [[-Level] <String[]>] [[-UserID] <String[]>] [[-NamedDataFilter] <Hashtable[]>]
 [[-NamedDataExcludeFilter] <Hashtable[]>] [[-ExcludeID] <String[]>] [-LogName] <String> [-XPathOnly]
 [<CommonParameters>]
```

## DESCRIPTION
This function generates an xpath filter that can be used with the -FilterXPath
parameter of Get-WinEvent. 
It may also be used inside the \<Select\>\</Select tags
of a Custom View in Event Viewer.

This function allows for the create of xpath which can select events based on
many properties of the event including those of named data nodes in the event's
XML.

XPath is case sensetive and the data passed to the parameters here must
match the case of the data in the event's XML.

## EXAMPLES

### EXAMPLE 1
```
Get-EventsFilter -ID 4663 -NamedDataFilter @{'SubjectUserName'='john.doe'} -LogName 'ForwardedEvents'
```

This will return an XPath filter that will return any events with
the id of 4663 and has a SubjectUserName of 'john.doe'

Output:
\<QueryList\>
    \<Query Id="0" Path="ForwardedEvents"\>
        \<Select Path="ForwardedEvents"\>
                (*\[System\[EventID=4663\]\]) and (*\[EventData\[Data\[@Name='SubjectUserName'\] = 'john.doe'\]\])
        \</Select\>
    \</Query\>
\</QueryList\>

### EXAMPLE 2
```
Get-EventsFilter -StartTime '1/1/2015 01:30:00 PM' -EndTime '1/1/2015 02:00:00 PM' -LogName 'ForwardedEvents
```

This will return an XPath filter that will return events that occured between 1:30
2:00 PM on 1/1/2015. 
The filter will only be good if used immediately. 
XPath time
filters are based on the number of milliseconds that have occured since the event
and when the filter is used. 
StartTime and EndTime simply calculate the number of
milliseconds and use that for the filter.

Output:
\<QueryList\>
    \<Query Id="0" Path="ForwardedEvents"\>
        \<Select Path="ForwardedEvents"\>
                (*\[System\[TimeCreated\[timediff(@SystemTime) &lt;= 125812885399\]\]\]) and (*\[System\[TimeCreated\[timediff(@SystemTime)
&gt;= 125811085399\]\]\])
        \</Select\>
    \</Query\>
\</QueryList\>

### EXAMPLE 3
```
Get-EventsFilter -StartTime (Get-Date).AddDays(-1) -LogName System
```

This will return an XPath filter that will get events that occured within the last 24 hours.

Output:
\<QueryList\>
    \<Query Id="0" Path="System"\>
            \<Select Path="System"\>
                *\[System\[TimeCreated\[timediff(@SystemTime) &lt;= 86404194\]\]\]
        \</Select\>
    \</Query\>
\</QueryList\>

### EXAMPLE 4
```
Get-EventsFilter -ID 1105 -LogName 'ForwardedEvents' -RecordID '3512231','3512232'
```

This will return an XPath filter that will get events with EventRecordID 3512231 or 3512232 in Log ForwardedEvents with EventID 1105

Output:
\<QueryList\>
    \<Query Id="0" Path="ForwardedEvents"\>
            \<Select Path="ForwardedEvents"\>
                (*\[System\[EventID=1105\]\]) and (*\[System\[(EventRecordID=3512231) or (EventRecordID=3512232)\]\])
        \</Select\>
    \</Query\>
\</QueryList\>

## PARAMETERS

### -ID
This parameter accepts and array of event ids to include in the xpath filter.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EventRecordID
{{Fill EventRecordID Description}}

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: RecordID

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StartTime
This parameter sets the oldest event that may be returned by the xpath.

Please, note that the xpath time selector created here is based of of the
time the xpath is generated. 
XPath uses a time difference method to select
events by time; that time difference being the number of milliseconds between
the time and now.

```yaml
Type: DateTime
Parameter Sets: (All)
Aliases:

Required: False
Position: 3
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EndTime
This parameter sets the newest event that may be returned by the xpath.

Please, note that the xpath time selector created here is based of of the
time the xpath is generated. 
XPath uses a time difference method to select
events by time; that time difference being the number of milliseconds between
the time and now.

```yaml
Type: DateTime
Parameter Sets: (All)
Aliases:

Required: False
Position: 4
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Data
This parameter will accept an array of values that may be found in the data
section of the event's XML.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 5
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ProviderName
This parameter will accept an array of values that select events from event
providers.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 6
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Keywords
This parameter accepts and array of long integer keywords.
You must
pass this parameter the long integer value of the keywords you want
to search and not the keyword description.

```yaml
Type: Int64[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 7
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Level
This parameter will accept an array of values that specify the severity
rating of the events to be returned.

It accepts the following values.

'Critical',
'Error',
'Informational',
'LogAlways',
'Verbose',
'Warning'

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 8
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -UserID
This parameter will accept an array of SIDs or domain accounts.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 9
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NamedDataFilter
This parameter will accept and array of hashtables that define the key
value pairs for which you want to filter against the event's named data
fields.

Key values, as with XPath filters, are case sensetive.

You may assign an array as the value of any key.
This will search
for events where any of the values are present in that particular
data field.
If you wanted to define a filter that searches for a SubjectUserName
of either john.doe or jane.doe, pass the following

@{'SubjectUserName'=('john.doe','jane.doe')}

You may specify multiple data files and values.
Doing so will create
an XPath filter that will only return results where both values
are found.
If you only wanted to return events where both the
SubjectUserName is john.doe and the TargetUserName is jane.doe, then
pass the following

@{'SubjectUserName'='john.doe';'TargetUserName'='jane.doe'}

You may pass an array of hash tables to create an 'or' XPath filter
that will return objects where either key value set will be returned.
If you wanted to define a filter that searches for either a
SubjectUserName of john.doe or a TargetUserName of jane.doe then pass
the following

(@{'SubjectUserName'='john.doe'},@{'TargetUserName'='jane.doe'})

```yaml
Type: Hashtable[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 10
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NamedDataExcludeFilter
{{Fill NamedDataExcludeFilter Description}}

```yaml
Type: Hashtable[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 11
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExcludeID
{{Fill ExcludeID Description}}

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 12
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LogName
{{Fill LogName Description}}

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 13
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -XPathOnly
{{Fill XPathOnly Description}}

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

## NOTES
Original Code by https://community.spiceworks.com/scripts/show/3238-powershell-xpath-generator-for-windows-events
Extended by Justin Grote
Extended by Przemyslaw Klys

## RELATED LINKS
