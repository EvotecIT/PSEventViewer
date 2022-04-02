function Get-Events {
    <#
    .SYNOPSIS
    Get-Events is a wrapper function around Get-WinEvent providing additional features and options.

    .DESCRIPTION
    Get-Events is a wrapper function around Get-WinEvent providing additional features and options exposing most of the Get-WinEvent functionality in easy to use manner.

    .PARAMETER Machine
    Specifies the name of the computer that this cmdlet gets events from the event logs. Type the NetBIOS name, an IP address, or the fully qualified domain name (FQDN) of the computer. The default value is the local computer, localhost. This parameter accepts only one computer name at a time.

    To get event logs from remote computers, configure the firewall port for the event log service to allow remote access.

    This cmdlet does not rely on PowerShell remoting. You can use the ComputerName parameter even if your computer is not configured to run remote commands.

    .PARAMETER DateFrom
    Specifies the date and time of the earliest event in the event log you want to search for.

    .PARAMETER DateTo
    Specifies the date and time of the latest event in the event log you want to search for.

    .PARAMETER ID
    Specifies the event ID (or events) of the event you want to search for. If provided more than 23 the cmdlet will split the events into multiple queries automatically.

    .PARAMETER ExcludeID
    Specifies the event ID (or events) of the event you want to exclude from the search. If provided more than 23 the cmdlet will split the events into multiple queries automatically.

    .PARAMETER LogName
    Specifies the event logs that this cmdlet get events from. Enter the event log names in a comma-separated list. Wildcards are permitted.

    .PARAMETER ProviderName
    Specifies, as a string array, the event log providers from which this cmdlet gets events. Enter the provider names in a comma-separated list, or use wildcard characters to create provider name patterns.

    An event log provider is a program or service that writes events to the event log. It is not a PowerShell provider.

    .PARAMETER NamedDataFilter
    Provide NamedDataFilter in specific form to optimize search performance looking for specific events.

    .PARAMETER NamedDataExcludeFilter
    Provide NamedDataExcludeFilter in specific form to optimize search performance looking for specific events.

    .PARAMETER UserID
    The UserID key can take a valid security identifier (SID) or a domain account name that can be used to construct a valid System.Security.Principal.NTAccount object.

    .PARAMETER Level
    Define the event level that this cmdlet gets events from. Options are Verbose, Informational, Warning, Error, Critical, LogAlways

    .PARAMETER UserSID
    Search events by UserSID

    .PARAMETER Data
    The Data value takes event data in an unnamed field. For example, events in classic event logs.

    .PARAMETER MaxEvents
    Specifies the maximum number of events that are returned. Enter an integer such as 100. The default is to return all the events in the logs or files.

    .PARAMETER Credential
    Specifies the name of the computer that this cmdlet gets events from the event logs. Type the NetBIOS name, an IP address, or the fully qualified domain name (FQDN) of the computer. The default value is the local computer, localhost. This parameter accepts only one computer name at a time.

    To get event logs from remote computers, configure the firewall port for the event log service to allow remote access.

    This cmdlet does not rely on PowerShell remoting. You can use the ComputerName parameter even if your computer is not configured to run remote commands.

    .PARAMETER Path
    Specifies the path to the event log files that this cmdlet get events from. Enter the paths to the log files in a comma-separated list, or use wildcard characters to create file path patterns.

    .PARAMETER Keywords
    Define keywords to search for by their name. Available keywords are: AuditFailure, AuditSuccess, CorrelationHint2, EventLogClassic, Sqm, WdiDiagnostic, WdiContext, ResponseTime, None

    .PARAMETER RecordID
    Find a single event in the event log using it's RecordId

    .PARAMETER MaxRunspaces
    Limit the number of concurrent runspaces that can be used to process the events. By default it uses $env:NUMBER_OF_PROCESSORS + 1

    .PARAMETER Oldest
    Indicate that this cmdlet gets the events in oldest-first order. By default, events are returned in newest-first order.

    .PARAMETER DisableParallel
    Disables parallel processing of the events. By default, events are processed in parallel.

    .PARAMETER ExtendedOutput
    Indicates that this cmdlet returns an extended set of output parameters. By default, this cmdlet does not generate any extended output.

    .PARAMETER ExtendedInput
    Indicates that this cmdlet takes an extended set of input parameters. Extended input is used by PSWinReportingV2 to provide special input parameters.

    .EXAMPLE
    Get-Events -LogName 'Application' -ID 1001 -MaxEvents 1 -Verbose -DisableParallel

    .EXAMPLE
    Get-Events -LogName 'Setup' -ID 2 -ComputerName 'AD1' -MaxEvents 1 -Verbose | Format-List *

    .EXAMPLE
    Get-Events -LogName 'Setup' -ID 2 -ComputerName 'AD1','AD2','AD3' -MaxEvents 1 -Verbose | Format-List *

    .EXAMPLE
    Get-Events -LogName 'Security' -ID 5379 -RecordID 19626 -Verbose

    .EXAMPLE
    Get-Events -LogName 'System' -ID 1001,1018 -ProviderName 'Microsoft-Windows-WER-SystemErrorReporting' -Verbose
    Get-Events -LogName 'System' -ID 42,41,109 -ProviderName 'Microsoft-Windows-Kernel-Power' -Verbose
    Get-Events -LogName 'System' -ID 1,12,13 -ProviderName 'Microsoft-Windows-Kernel-General' -Verbose
    Get-Events -LogName 'System' -ID 6005,6006,6008,6013 -ProviderName 'EventLog' -Verbose

    .EXAMPLE
    $List = @(
        @{ Server = 'AD1'; LogName = 'Security'; EventID = '5136', '5137'; Type = 'Computer' }
        @{ Server = 'AD2'; LogName = 'Security'; EventID = '5136', '5137'; Type = 'Computer' }
        @{ Server = 'C:\MyEvents\Archive-Security-2018-08-21-23-49-19-424.evtx'; LogName = 'Security'; EventID = '5136', '5137'; Type = 'File' }
        @{ Server = 'C:\MyEvents\Archive-Security-2018-09-15-09-27-52-679.evtx'; LogName = 'Security'; EventID = '5136', '5137'; Type = 'File' }
        @{ Server = 'Evo1'; LogName = 'Setup'; EventID = 2; Type = 'Computer'; }
        @{ Server = 'AD1.ad.evotec.xyz'; LogName = 'Security'; EventID = 4720, 4738, 5136, 5137, 5141, 4722, 4725, 4767, 4723, 4724, 4726, 4728, 4729, 4732, 4733, 4746, 4747, 4751, 4752, 4756, 4757, 4761, 4762, 4785, 4786, 4787, 4788, 5136, 5137, 5141, 5136, 5137, 5141, 5136, 5137, 5141; Type = 'Computer' }
        @{ Server = 'Evo1'; LogName = 'Security'; Type = 'Computer'; MaxEvents = 15; Keywords = 'AuditSuccess' }
        @{ Server = 'Evo1'; LogName = 'Security'; Type = 'Computer'; MaxEvents = 15; Level = 'Informational'; Keywords = 'AuditFailure' }
    )
    $Output = Get-Events -ExtendedInput $List -Verbose
    $Output | Format-Table Computer, Date, LevelDisplayName

    .EXAMPLE
    Get-Events -MaxEvents 2 -LogName 'Security' -ComputerName 'AD1.AD.EVOTEC.XYZ','AD2' -ID 4720, 4738, 5136, 5137, 5141, 4722, 4725, 4767, 4723, 4724, 4726, 4728, 4729, 4732, 4733, 4746, 4747, 4751, 4752, 4756, 4757, 4761, 4762, 4785, 4786, 4787, 4788, 5136, 5137, 5141, 5136, 5137, 5141, 5136, 5137, 5141 -Verbose

    .NOTES
    General notes
    #>
    [CmdLetBinding()]
    param (
        [alias ("ADDomainControllers", "DomainController", "Server", "Servers", "Computer", "Computers", "ComputerName")] [string[]] $Machine = $Env:COMPUTERNAME,
        [alias ("StartTime", "From")][nullable[DateTime]] $DateFrom = $null,
        [alias ("EndTime", "To")][nullable[DateTime]] $DateTo = $null,
        [alias ("Ids", "EventID", "EventIds")] [int[]] $ID = $null,
        [alias ("ExludeEventID")][int[]] $ExcludeID = $null,
        [alias ("LogType", "Log")][string] $LogName = $null,
        [alias ("Provider", "Source")] [string[]] $ProviderName,
        [hashtable] $NamedDataFilter,
        [hashtable] $NamedDataExcludeFilter,
        [string[]] $UserID,
        [PSEventViewer.Level[]] $Level = $null,
        [string] $UserSID = $null,
        [string[]]$Data = $null,
        [int] $MaxEvents = $null,

        [ValidateNotNull()]
        [alias('Credentials')][System.Management.Automation.PSCredential]
        [System.Management.Automation.Credential()]$Credential = [System.Management.Automation.PSCredential]::Empty,

        [string] $Path = $null,
        [PSEventViewer.Keywords[]] $Keywords = $null,
        [alias("EventRecordID")][int64] $RecordID,
        [int] $MaxRunspaces = [int]$env:NUMBER_OF_PROCESSORS + 1,
        [switch] $Oldest,
        [switch] $DisableParallel,
        [switch] $ExtendedOutput,
        [Array] $ExtendedInput
    )
    if ($PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent) { $Verbose = $true } else { $Verbose = $false }

    $MeasureTotal = [System.Diagnostics.Stopwatch]::StartNew() # Timer Start
    $ParametersList = [System.Collections.Generic.List[Object]]::new()

    if ($ExtendedInput.Count -gt 0) {
        # Special input - PSWinReporting requires it
        [Array] $Param = foreach ($EventEntry in $ExtendedInput) {
            $EventFilter = @{ }
            if ($EventEntry.Type -eq 'File') {
                Write-Verbose "Get-Events - Preparing data to scan file $($EventEntry.Server)"
                Add-ToHashTable -Hashtable $EventFilter -Key "Path" -Value $EventEntry.Server
                $Comp = $Env:COMPUTERNAME
            } else {
                Write-Verbose "Get-Events - Preparing data to scan computer $($EventEntry.Server)"
                $Comp = $EventEntry.Server
            }
            $ConvertedLevels = foreach ($DataLevel in $EventEntry.Level) {
                ([PSEventViewer.Level]::$DataLevel).value__
            }
            $ConvertedKeywords = foreach ($DataKeyword in $EventEntry.Keywords) {
                ([PSEventViewer.Keywords]::$DataKeyword).value__
            }
            Add-ToHashTable -Hashtable $EventFilter -Key "LogName" -Value $EventEntry.LogName
            Add-ToHashTable -Hashtable $EventFilter -Key "StartTime" -Value $EventEntry.DateFrom
            Add-ToHashTable -Hashtable $EventFilter -Key "EndTime" -Value $EventEntry.DateTo
            Add-ToHashTable -Hashtable $EventFilter -Key "Keywords" -Value $ConvertedKeywords
            Add-ToHashTable -Hashtable $EventFilter -Key "Level" -Value $ConvertedLevels
            Add-ToHashTable -Hashtable $EventFilter -Key "UserID" -Value $EventEntry.UserSID
            Add-ToHashTable -Hashtable $EventFilter -Key "Data" -Value $EventEntry.Data
            Add-ToHashTable -Hashtable $EventFilter -Key "RecordID" -Value $EventEntry.RecordID
            Add-ToHashTable -Hashtable $EventFilter -Key "NamedDataFilter" -Value $EventEntry.NamedDataFilter
            Add-ToHashTable -Hashtable $EventFilter -Key "NamedDataExcludeFilter" -Value $EventEntry.NamedDataExcludeFilter
            Add-ToHashTable -Hashtable $EventFilter -Key "UserID" -Value $EventEntry.UserID
            Add-ToHashTable -Hashtable $EventFilter -Key "ExcludeID" -Value $EventEntry.ExcludeID

            if ($Verbose) {
                foreach ($Key in $EventFilter.Keys) {
                    if ($Key -eq 'NamedDataFilter' -or $Key -eq 'NamedDataExcludeFilter') {
                        foreach ($SubKey in $($EventFilter.$Key).Keys) {
                            Write-Verbose "Get-Events - Filter parameters provided $Key with SubKey $SubKey = $(($EventFilter.$Key.$SubKey) -join ', ')"
                        }
                    } else {
                        Write-Verbose "Get-Events - Filter parameters provided $Key = $(($EventFilter.$Key) -join ', ')"
                    }
                }
            }
            if ($null -ne $EventEntry.EventID) {
                $ID = $EventEntry.EventID | Sort-Object -Unique
                Write-Verbose "Get-Events - Events to process in Total (unique): $($Id.Count)"
                Write-Verbose "Get-Events - Events to process in Total ID: $($ID -join ', ')"
                if ($Id.Count -gt 22) {
                    Write-Verbose "Get-Events - There are more events to process then 22, split will be required."
                }
                $SplitArrayID = Split-Array -inArray $ID -size 22  # Support for more ID's then 22 (limitation of Get-WinEvent)
                foreach ($EventIdGroup in $SplitArrayID) {
                    $EventFilter.Id = @($EventIdGroup)
                    @{
                        Comp        = $Comp
                        Credential  = $Credential
                        EventFilter = $EventFilter.Clone()
                        MaxEvents   = $EventEntry.MaxEvents
                        Oldest      = $Oldest
                        Verbose     = $Verbose
                    }
                }
            } else {
                @{
                    Comp        = $Comp
                    Credential  = $Credential
                    EventFilter = $EventFilter
                    MaxEvents   = $EventEntry.MaxEvents
                    Oldest      = $Oldest
                    Verbose     = $Verbose
                }
            }
        }
        if ($null -ne $Param) {
            $null = $ParametersList.AddRange($Param)
        }
    } else {
        # Standard input
        $EventFilter = @{ }
        Add-ToHashTable -Hashtable $EventFilter -Key "LogName" -Value $LogName # Accepts Wildcard
        Add-ToHashTable -Hashtable $EventFilter -Key "ProviderName" -Value $ProviderName # Accepts Wildcard
        Add-ToHashTable -Hashtable $EventFilter -Key "Path" -Value $Path # https://blogs.technet.microsoft.com/heyscriptingguy/2011/01/25/use-powershell-to-parse-saved-event-logs-for-errors/
        Add-ToHashTable -Hashtable $EventFilter -Key "Keywords" -Value $Keywords.value__
        Add-ToHashTable -Hashtable $EventFilter -Key "Level" -Value $Level.value__
        Add-ToHashTable -Hashtable $EventFilter -Key "StartTime" -Value $DateFrom
        Add-ToHashTable -Hashtable $EventFilter -Key "EndTime" -Value $DateTo
        Add-ToHashTable -Hashtable $EventFilter -Key "UserID" -Value $UserSID
        Add-ToHashTable -Hashtable $EventFilter -Key "Data" -Value $Data
        Add-ToHashTable -Hashtable $EventFilter -Key "RecordID" -Value $RecordID
        Add-ToHashTable -Hashtable $EventFilter -Key "NamedDataFilter" -Value $NamedDataFilter
        Add-ToHashTable -Hashtable $EventFilter -Key "NamedDataExcludeFilter" -Value $NamedDataExcludeFilter
        Add-ToHashTable -Hashtable $EventFilter -Key "UserID" -Value $UserID
        Add-ToHashTable -Hashtable $EventFilter -Key "ExcludeID" -Value $ExcludeID

        [Array] $Param = foreach ($Comp in $Machine) {
            if ($Verbose) {
                Write-Verbose "Get-Events - Preparing data to scan computer $Comp"
                foreach ($Key in $EventFilter.Keys) {
                    if ($Key -eq 'NamedDataFilter' -or $Key -eq 'NamedDataExcludeFilter') {
                        foreach ($SubKey in $($EventFilter.$Key).Keys) {
                            Write-Verbose "Get-Events - Filter parameters provided $Key with SubKey $SubKey = $(($EventFilter.$Key.$SubKey) -join ', ')"
                        }
                    } else {
                        Write-Verbose "Get-Events - Filter parameters provided $Key = $(($EventFilter.$Key) -join ', ')"
                    }
                }
            }
            if ($null -ne $ID) {
                # EventID needed
                $ID = $ID | Sort-Object -Unique
                Write-Verbose "Get-Events - Events to process in Total (unique): $($Id.Count)"
                Write-Verbose "Get-Events - Events to process in Total ID: $($ID -join ', ')"
                if ($Id.Count -gt 22) {
                    Write-Verbose "Get-Events - There are more events to process then 22, split will be required."
                }
                $SplitArrayID = Split-Array -inArray $ID -size 22  # Support for more ID's then 22 (limitation of Get-WinEvent)
                foreach ($EventIdGroup in $SplitArrayID) {
                    $EventFilter.Id = @($EventIdGroup)
                    @{
                        Comp        = $Comp
                        Credential  = $Credential
                        EventFilter = $EventFilter.Clone()
                        MaxEvents   = $MaxEvents
                        Oldest      = $Oldest
                        Verbose     = $Verbose
                    }
                }
            } else {
                # No EventID given
                @{
                    Comp        = $Comp
                    Credential  = $Credential
                    EventFilter = $EventFilter
                    MaxEvents   = $MaxEvents
                    Oldest      = $Oldest
                    Verbose     = $Verbose
                }
            }
        }
        if ($null -ne $Param) {
            $null = $ParametersList.AddRange($Param)
        }
    }
    $AllErrors = @()
    if ($DisableParallel) {
        Write-Verbose 'Get-Events - Running query with parallel disabled...'
        [Array] $AllEvents = foreach ($Parameter in $ParametersList) {
            Invoke-Command -ScriptBlock $Script:ScriptBlock -ArgumentList $Parameter.Comp, $Parameter.Credential, $Parameter.EventFilter, $Parameter.MaxEvents, $Parameter.Oldest, $Parameter.Verbose
        }
    } else {
        Write-Verbose 'Get-Events - Running query with parallel enabled...'
        $RunspacePool = New-Runspace -maxRunspaces $maxRunspaces -Verbose:$Verbose
        $Runspaces = foreach ($Parameter in $ParametersList) {
            Start-Runspace -ScriptBlock $Script:ScriptBlock -Parameters $Parameter -RunspacePool $RunspacePool -Verbose:$Verbose
        }
        [Array] $AllEvents = Stop-Runspace -Runspaces $Runspaces -FunctionName "Get-Events" -RunspacePool $RunspacePool -Verbose:$Verbose -ErrorAction SilentlyContinue -ErrorVariable +AllErrors -ExtendedOutput:$ExtendedOutput
    }
    Write-Verbose "Get-Events - Overall errors: $($AllErrors.Count)"
    Write-Verbose "Get-Events - Overall events processed in total for the report: $($AllEvents.Count)"
    Write-Verbose "Get-Events - Overall time to generate $($MeasureTotal.Elapsed.Hours) hours, $($MeasureTotal.Elapsed.Minutes) minutes, $($MeasureTotal.Elapsed.Seconds) seconds, $($MeasureTotal.Elapsed.Milliseconds) milliseconds"
    $MeasureTotal.Stop()
    Write-Verbose "Get-Events - Overall events processing end"
    if ($AllEvents.Count -eq 1) {
        return , $AllEvents
    } else {
        return $AllEvents
    }
}