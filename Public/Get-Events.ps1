function Get-Events {
    <#
    .SYNOPSIS
    Get-Events is a wrapper function around Get-WinEvent providing additional features and options.

    .DESCRIPTION
    Long description

    .PARAMETER Machine
    ComputerName or Server you want to query. Takes an array of servers as well.
    
    .PARAMETER DateFrom
    Parameter description
    
    .PARAMETER DateTo
    Parameter description
    
    .PARAMETER ID
    Parameter description
    
    .PARAMETER ExcludeID
    Parameter description
    
    .PARAMETER LogName
    Parameter description
    
    .PARAMETER ProviderName
    Parameter description
    
    .PARAMETER NamedDataFilter
    Parameter description
    
    .PARAMETER NamedDataExcludeFilter
    Parameter description
    
    .PARAMETER UserID
    Parameter description
    
    .PARAMETER Level
    Parameter description
    
    .PARAMETER UserSID
    Parameter description
    
    .PARAMETER Data
    Parameter description
    
    .PARAMETER MaxEvents
    Parameter description
    
    .PARAMETER Credentials
    Parameter description
    
    .PARAMETER Path
    Parameter description
    
    .PARAMETER Keywords
    Parameter description
    
    .PARAMETER RecordID
    Parameter description
    
    .PARAMETER MaxRunspaces
    Parameter description
    
    .PARAMETER Oldest
    Parameter description
    
    .PARAMETER DisableParallel
    Parameter description
    
    .PARAMETER ExtendedOutput
    Parameter description
    
    .PARAMETER ExtendedInput
    Parameter description
    
    .EXAMPLE
    An example
    
    .NOTES
    General notes
    #>
    
    [CmdLetBinding()]
    param (
        [alias ("ADDomainControllers", "DomainController", "Server", "Servers", "Computer", "Computers", "ComputerName")] [string[]] $Machine = $Env:COMPUTERNAME,
        [alias ("From")][nullable[DateTime]] $DateFrom = $null,
        [alias ("To")][nullable[DateTime]] $DateTo = $null,
        [alias ("Ids", "EventID", "EventIds")] [int[]] $ID = $null,
        [alias ("ExludeEventID")][int[]] $ExcludeID = $null,
        [alias ("LogType", "Log")][string] $LogName = $null,
        [alias ("Provider")] [string] $ProviderName = '',
        [hashtable] $NamedDataFilter,
        [hashtable] $NamedDataExcludeFilter,
        [string[]] $UserID,
        [PSEventViewer.Level[]] $Level = $null,
        [string] $UserSID = $null,
        [string[]]$Data = $null,
        [int] $MaxEvents = $null,
        [PSCredential] $Credentials = $null,
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
        [Array] $Param = foreach ($Input in $ExtendedInput) {
            $EventFilter = @{}
            if ($Input.Type -eq 'File') {
                Write-Verbose "Get-Events - Preparing data to scan file $($Input.Server)"
                Add-ToHashTable -Hashtable $EventFilter -Key "LogName" -Value $Input.LogName # Accepts Wildcard
                Add-ToHashTable -Hashtable $EventFilter -Key "Path" -Value $Input.Server
                Add-ToHashTable -Hashtable $EventFilter -Key "Id" -Value $Input.EventID
                $Comp = $Env:COMPUTERNAME
            } else {
                Write-Verbose "Get-Events - Preparing data to scan computer $($Input.Server)"
                Add-ToHashTable -Hashtable $EventFilter -Key "LogName" -Value $Input.LogName
                Add-ToHashTable -Hashtable $EventFilter -Key "Id" -Value $Input.EventID
                $Comp = $Input.Server
            }             
            if ($Verbose) {
                foreach ($Key in $EventFilter.Keys) {
                    Write-Verbose "Get-Events - Filter parameters provided $Key = $($EventFilter.$Key)"
                }
            }
            @{
                Comp        = $Comp
                EventFilter = $EventFilter
                MaxEvents   = $MaxEvents
                Oldest      = $Oldest
                Verbose     = $Verbose
            }
        }
        $null = $ParametersList.AddRange($Param)
    } else {

        if ($null -ne $ID) {
            $ID = $ID | Sort-Object -Unique
            Write-Verbose "Get-Events - Events to process in Total: $($Id.Count)"
            Write-Verbose "Get-Events - Events to process in Total ID: $ID"
            if ($Id.Count -gt 22) {
                Write-Verbose "Get-Events - There are more events to process then 22, split will be required."
            }
            $SplitArrayID = Split-Array -inArray $ID -size 22  # Support for more ID's then 22 (limitation of Get-WinEvent)
            [Array] $Param = foreach ($ID in $SplitArrayID) {
                $EventFilter = @{}
                Add-ToHashTable -Hashtable $EventFilter -Key "LogName" -Value $LogName # Accepts Wildcard
                Add-ToHashTable -Hashtable $EventFilter -Key "ProviderName" -Value $ProviderName # Accepts Wildcard
                Add-ToHashTable -Hashtable $EventFilter -Key "Path" -Value $Path # https://blogs.technet.microsoft.com/heyscriptingguy/2011/01/25/use-powershell-to-parse-saved-event-logs-for-errors/
                Add-ToHashTable -Hashtable $EventFilter -Key "Keywords" -Value $Keywords.value__
                Add-ToHashTable -Hashtable $EventFilter -Key "Id" -Value $ID
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

                foreach ($Comp in $Machine) {
                    Write-Verbose "Get-Events - Preparing data to scan computer $Comp"
                    foreach ($Key in $EventFilter.Keys) {
                        Write-Verbose "Get-Events - Filter parameters provided $Key = $($EventFilter.$Key)"
                    }
                    @{
                        Comp        = $Comp
                        EventFilter = $EventFilter
                        MaxEvents   = $MaxEvents
                        Oldest      = $Oldest
                        Verbose     = $Verbose
                    }
                }
            }
            $null = $ParametersList.AddRange($param)
        } else {
            # No EventID was given
            $EventFilter = @{}
            Add-ToHashTable -Hashtable $EventFilter -Key "LogName" -Value $LogName # Accepts Wildcard
            Add-ToHashTable -Hashtable $EventFilter -Key "ProviderName" -Value $ProviderName # Accepts Wildcard
            Add-ToHashTable -Hashtable $EventFilter -Key "Path" -Value $Path # https://blogs.technet.microsoft.com/heyscriptingguy/2011/01/25/use-powershell-to-parse-saved-event-logs-for-errors/
            Add-ToHashTable -Hashtable $EventFilter -Key "Keywords" -Value $Keywords.value__
            Add-ToHashTable -Hashtable $EventFilter -Key "Id" -Value $ID
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
                Write-Verbose "Get-Events - Preparing data to scan computer $Comp"
                foreach ($Key in $EventFilter.Keys) {
                    Write-Verbose "Get-Events - Filter parameters provided $Key = $($EventFilter.$Key)"
                }
                @{
                    Comp        = $Comp
                    EventFilter = $EventFilter
                    MaxEvents   = $MaxEvents
                    Oldest      = $Oldest
                    Verbose     = $Verbose
                }
            }
            $null = $ParametersList.AddRange($Param)
        }
    }

    $AllErrors = @()
    if ($DisableParallel) {
        Write-Verbose 'Get-Events - Running query with parallel disabled...'
        [Array] $AllEvents = foreach ($Param in $ParametersList) {  
            Invoke-Command -ScriptBlock $Script:ScriptBlock -ArgumentList $Param.Comp, $Param.EventFilter, $Param.MaxEvents, $Param.Oldest, $Param.Verbose
        }
    } else {
        Write-Verbose 'Get-Events - Running query with parallel enabled...'
        $RunspacePool = New-Runspace -MaxRunspaces $maxRunspaces -Verbose:$Verbose
        $Runspaces = foreach ($Parameter in $ParametersList) {    
            Start-Runspace -ScriptBlock $Script:ScriptBlock -Parameters $Parameter -RunspacePool $RunspacePool -Verbose:$Verbose
        }        
        [Array] $AllEvents = Stop-Runspace -Runspaces $Runspaces -FunctionName "Get-Events" -RunspacePool $RunspacePool -Verbose:$Verbose -ErrorAction SilentlyContinue -ErrorVariable +AllErrors -ExtendedOutput:$ExtendedOutput
    }
    if ($ExtendedOutput) {
        return , $AllEvents # returns @{ Output and Errors }
    }
    $EventsProcessed = ($AllEvents | Measure-Object).Count
    Write-Verbose "Get-Events - Overall errors: $($AllErrors.Count)"
    Write-Verbose "Get-Events - Overall events processed in total for the report: $EventsProcessed"
    Write-Verbose "Get-Events - Overall time to generate $($MeasureTotal.Elapsed.Hours) hours, $($MeasureTotal.Elapsed.Minutes) minutes, $($MeasureTotal.Elapsed.Seconds) seconds, $($MeasureTotal.Elapsed.Milliseconds) milliseconds"
    $MeasureTotal.Stop()
    Write-Verbose "Get-Events - Overall events processing end"
    return , $AllEvents
}