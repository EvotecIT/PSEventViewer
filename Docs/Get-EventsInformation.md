---
external help file: PSEventViewer-help.xml
Module Name: PSEventViewer
online version:
schema: 2.0.0
---

# Get-EventsInformation

## SYNOPSIS

Small wrapper against Get-WinEvent providing easy way to gather statistics for Event Logs.

## SYNTAX

```
Get-EventsInformation [[-Machine] <String[]>] [[-FilePath] <String[]>] [[-LogName] <String[]>]
 [[-MaxRunspaces] <Int32>] [-RunAgainstDC] [<CommonParameters>]
```

## DESCRIPTION

Small wrapper against Get-WinEvent providing easy way to gather statistics for Event Logs.
It provides option to ask for multiple machines, multiple files at the same time.
It runs on steroids (runspaces) which allows youto process everything at same time.
This basically allows you to query 50 servers at same time and do it in finite way.

## EXAMPLES

### EXAMPLE 1

```
$Computer = 'EVO1','AD1','AD2'
$LogName = 'Security'

$Size = Get-EventsInformation -Computer $Computer -LogName $LogName
$Size | ft -A

#Output:

EventNewest         EventOldest          FileSize FileSizeCurrentGB FileSizeMaximumGB IsClassicLog IsEnabled IsLogFull LastAccessTime      LastWriteTime
-----------         -----------          -------- ----------------- ----------------- ------------ --------- --------- --------------      -------------
28.12.2018 12:47:14 20.12.2018 19:29:57 110170112 0.1 GB            0.11 GB                   True      True     False 27.05.2018 14:18:36 28.12.2018 12:33:24
28.12.2018 12:46:51 26.12.2018 12:54:16  20975616 0.02 GB           0.02 GB                   True      True     False 28.12.2018 12:46:57 28.12.2018 12:46:57
```

### EXAMPLE 2

Due to AD2 being down time to run is 22 seconds. This is actual timeout before letting it go.

```
$Computers = 'EVO1', 'AD1', 'AD2'
$LogName = 'Security'

$EventLogsDirectory = Get-ChildItem -Path 'C:\MyEvents'

$Size = Get-EventsInformation -FilePath $EventLogsDirectory.FullName -Computer $Computers -LogName 'Security'
$Size | ft -a

#Output:

VERBOSE: Get-EventsInformation - processing start
VERBOSE: Get-EventsInformation - Setting up runspace for EVO1
VERBOSE: Get-EventsInformation - Setting up runspace for AD1
VERBOSE: Get-EventsInformation - Setting up runspace for AD2
VERBOSE: Get-EventsInformation - Setting up runspace for C:\MyEvents\Archive-Security-2018-08-21-23-49-19-424.evtx
VERBOSE: Get-EventsInformation - Setting up runspace for C:\MyEvents\Archive-Security-2018-09-08-02-53-53-711.evtx
VERBOSE: Get-EventsInformation - Setting up runspace for C:\MyEvents\Archive-Security-2018-09-14-22-13-07-710.evtx
VERBOSE: Get-EventsInformation - Setting up runspace for C:\MyEvents\Archive-Security-2018-09-15-09-27-52-679.evtx
VERBOSE: AD2 Reading Event Log (Security) size failed. Error occured: The RPC server is unavailable
VERBOSE: Get-EventsInformation - processing end - 0 days, 0 hours, 0 minutes, 22 seconds, 648 milliseconds

EventNewest         EventOldest          FileSize FileSizeCurrentGB FileSizeMaximumGB IsClassicLog IsEnabled IsLogFull LastAccessTime      LastWriteTime
-----------         -----------          -------- ----------------- ----------------- ------------ --------- --------- --------------      -------------
28.12.2018 15:56:54 20.12.2018 19:29:57 111218688 0.1 GB            0.11 GB                   True      True     False 27.05.2018 14:18:36 28.12.2018 14:18:24
22.08.2018 01:48:57 11.08.2018 09:28:06 115740672 0.11 GB           0.11 GB                  False     False     False 16.09.2018 09:27:04 22.08.2018 01:49:20
08.09.2018 04:53:52 03.09.2018 23:50:15 115740672 0.11 GB           0.11 GB                  False     False     False 12.09.2018 13:18:25 08.09.2018 04:53:53
15.09.2018 00:13:06 08.09.2018 04:53:53 115740672 0.11 GB           0.11 GB                  False     False     False 15.09.2018 00:13:26 15.09.2018 00:13:08
15.09.2018 11:27:51 22.08.2018 01:49:20 115740672 0.11 GB           0.11 GB                  False     False     False 15.09.2018 11:28:13 15.09.2018 11:27:55
28.12.2018 15:56:56 26.12.2018 15:56:31  20975616 0.02 GB           0.02 GB                   True      True     False 28.12.2018 15:56:47 28.12.2018 15:56:47
```

### EXAMPLE 3

```
$Computers = 'EVO1', 'AD1','AD1'
$LogName = 'Security'

$EventLogsDirectory = Get-ChildItem -Path 'C:\MyEvents'

$Size = Get-EventsInformation -FilePath $EventLogsDirectory.FullName -Computer $Computers -LogName 'Security' -Verbose
$Size | ft -a Source, EventNewest, EventOldest,FileSize, FileSizeCurrentGB, FileSizeMaximumGB, IsEnabled, IsLogFull, LastAccessTime, LastWriteTime

#Output:

VERBOSE: Get-EventsInformation - processing start
VERBOSE: Get-EventsInformation - Setting up runspace for EVO1
VERBOSE: Get-EventsInformation - Setting up runspace for AD1
VERBOSE: Get-EventsInformation - Setting up runspace for AD1
VERBOSE: Get-EventsInformation - Setting up runspace for C:\MyEvents\Archive-Security-2018-08-21-23-49-19-424.evtx
VERBOSE: Get-EventsInformation - Setting up runspace for C:\MyEvents\Archive-Security-2018-09-08-02-53-53-711.evtx
VERBOSE: Get-EventsInformation - Setting up runspace for C:\MyEvents\Archive-Security-2018-09-14-22-13-07-710.evtx
VERBOSE: Get-EventsInformation - Setting up runspace for C:\MyEvents\Archive-Security-2018-09-15-09-27-52-679.evtx
VERBOSE: Get-EventsInformation - processing end - 0 days, 0 hours, 0 minutes, 1 seconds, 739 milliseconds

Source EventNewest         EventOldest          FileSize FileSizeCurrentGB FileSizeMaximumGB IsEnabled IsLogFull LastAccessTime      LastWriteTime
------ -----------         -----------          -------- ----------------- ----------------- --------- --------- --------------      -------------
AD1    28.12.2018 15:59:22 20.12.2018 19:29:57 111218688 0.1 GB            0.11 GB                True     False 27.05.2018 14:18:36 28.12.2018 14:18:24
AD1    28.12.2018 15:59:22 20.12.2018 19:29:57 111218688 0.1 GB            0.11 GB                True     False 27.05.2018 14:18:36 28.12.2018 14:18:24
File   22.08.2018 01:48:57 11.08.2018 09:28:06 115740672 0.11 GB           0.11 GB               False     False 16.09.2018 09:27:04 22.08.2018 01:49:20
File   08.09.2018 04:53:52 03.09.2018 23:50:15 115740672 0.11 GB           0.11 GB               False     False 12.09.2018 13:18:25 08.09.2018 04:53:53
File   15.09.2018 00:13:06 08.09.2018 04:53:53 115740672 0.11 GB           0.11 GB               False     False 15.09.2018 00:13:26 15.09.2018 00:13:08
File   15.09.2018 11:27:51 22.08.2018 01:49:20 115740672 0.11 GB           0.11 GB               False     False 15.09.2018 11:28:13 15.09.2018 11:27:55
EVO1   28.12.2018 15:59:22 26.12.2018 15:56:31  20975616 0.02 GB           0.02 GB                True     False 28.12.2018 15:58:47 28.12.2018 15:58:47
```

### EXAMPLE 4

```
$Computers = 'EVO1', 'AD1'
$EventLogsDirectory = Get-ChildItem -Path 'C:\MyEvents'

$Size = Get-EventsInformation -FilePath $EventLogsDirectory.FullName -Computer $Computers -LogName 'Security','System' -Verbose
$Size | ft -a Source, EventNewest, EventOldest,FileSize, FileSizeCurrentGB, FileSizeMaximumGB, IsEnabled, IsLogFull, LastAccessTime, LastWriteTime, LogFilePath, LOgName

#Output:

VERBOSE: Get-EventsInformation - processing start
VERBOSE: Get-EventsInformation - Setting up runspace for EVO1
VERBOSE: Get-EventsInformation - Setting up runspace for AD1
VERBOSE: Get-EventsInformation - Setting up runspace for C:\MyEvents\Archive-Security-2018-08-21-23-49-19-424.evtx
VERBOSE: Get-EventsInformation - Setting up runspace for C:\MyEvents\Archive-Security-2018-09-08-02-53-53-711.evtx
VERBOSE: Get-EventsInformation - Setting up runspace for C:\MyEvents\Archive-Security-2018-09-14-22-13-07-710.evtx
VERBOSE: Get-EventsInformation - Setting up runspace for C:\MyEvents\Archive-Security-2018-09-15-09-27-52-679.evtx
VERBOSE: Get-EventsInformation - processing end - 0 days, 0 hours, 0 minutes, 0 seconds, 137 milliseconds

Source EventNewest         EventOldest          FileSize FileSizeCurrentGB FileSizeMaximumGB IsEnabled IsLogFull LastAccessTime      LastWriteTime       LogFilePath                                               LogName
------ -----------         -----------          -------- ----------------- ----------------- --------- --------- --------------      -------------       -----------                                               -------
File   22.08.2018 01:48:57 11.08.2018 09:28:06 115740672 0.11 GB           0.11 GB               False     False 16.09.2018 09:27:04 22.08.2018 01:49:20 C:\MyEvents\Archive-Security-2018-08-21-23-49-19-424.evtx N/A
File   08.09.2018 04:53:52 03.09.2018 23:50:15 115740672 0.11 GB           0.11 GB               False     False 12.09.2018 13:18:25 08.09.2018 04:53:53 C:\MyEvents\Archive-Security-2018-09-08-02-53-53-711.evtx N/A
EVO1   28.12.2018 18:19:48 26.12.2018 17:27:30  20975616 0.02 GB           0.02 GB                True     False 28.12.2018 18:19:47 28.12.2018 18:19:47 %SystemRoot%\System32\Winevt\Logs\Security.evtx           Security
AD1    28.12.2018 18:20:01 20.12.2018 19:29:57 113315840 0.11 GB           0.11 GB                True     False 27.05.2018 14:18:36 28.12.2018 17:48:24 %SystemRoot%\System32\Winevt\Logs\Security.evtx           Security
File   15.09.2018 00:13:06 08.09.2018 04:53:53 115740672 0.11 GB           0.11 GB               False     False 15.09.2018 00:13:26 15.09.2018 00:13:08 C:\MyEvents\Archive-Security-2018-09-14-22-13-07-710.evtx N/A
EVO1   28.12.2018 18:20:01 05.10.2018 01:33:48  12652544 0.01 GB           0.02 GB                True     False 28.12.2018 18:18:01 28.12.2018 18:18:01 %SystemRoot%\System32\Winevt\Logs\System.evtx             System
AD1    28.12.2018 18:12:47 03.12.2018 17:20:48   2166784 0 GB              0.01 GB                True     False 19.05.2018 20:05:07 27.12.2018 12:00:32 %SystemRoot%\System32\Winevt\Logs\System.evtx             System
File   15.09.2018 11:27:51 22.08.2018 01:49:20 115740672 0.11 GB           0.11 GB               False     False 15.09.2018 11:28:13 15.09.2018 11:27:55 C:\MyEvents\Archive-Security-2018-09-15-09-27-52-679.evtx N/A
```

## PARAMETERS

### -Machine

ComputerName or Server you want to query. Takes an array of servers as well.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: ADDomainControllers, DomainController, Server, Servers, Computer, Computers, ComputerName

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FilePath

FilePath to Event Log file (with .EVTX). Takes an array of Event Log files.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LogName

LogName such as Security or System. Works in conjuction with Machine (s). Default is Security.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases: LogType, Log

Required: False
Position: 3
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxRunspaces

Maximum number of runspaces running at same time. For optimum performance decide on your own. Default is 50.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: 4
Default value: 10
Accept pipeline input: False
Accept wildcard characters: False
```

### -RunAgainstDC
{{Fill RunAgainstDC Description}}

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: AskDC, QueryDomainControllers, AskForest

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

General notes

## RELATED LINKS
