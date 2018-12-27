function Get-Events {
    <#
    .SYNOPSIS

    .DESCRIPTION

    .NOTES

    .EXAMPLE
        $DateFrom = (get-date).AddHours(-5)
        $DateTo = (get-date).AddHours(1)
        get-events -Computer "Evo1" -DateFrom $DateFrom -DateTo $DateTo -EventId 916 -LogType "Application"
    #>
    [CmdLetBinding()]
    param (
        [alias ("ADDomainControllers", "DomainController", "Server", "Servers", "Computer", "Computers", "ComputerName")] [string[]] $Machine = $Env:COMPUTERNAME,
        [alias ("From")][nullable[DateTime]] $DateFrom = $null,
        [alias ("To")][nullable[DateTime]] $DateTo = $null,
        [alias ("Ids", "EventID", "EventIds")] [int[]] $ID = $null,
        [alias ("LogType", "Log")][string] $LogName = $null,
        [alias ("Provider")] [string] $ProviderName = '',
        [int] $Level = $null,
        [string] $UserSID = $null,
        [string[]]$Data = $null,
        [int] $MaxEvents = $null,
        $Credentials = $null,
        [string] $Path = $null,
        [long[]] $Keywords = $null,
        [int64] $RecordID,
        [int] $MaxRunspaces = [int]$env:NUMBER_OF_PROCESSORS + 1,
        [switch] $Oldest,
        [switch] $DisableParallel
    )

    Write-Verbose "Get-Events - Overall events processing start"
    $MeasureTotal = [System.Diagnostics.Stopwatch]::StartNew() # Timer Start

    if ($PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent) { $Verbose = $true } else { $Verbose = $false }

    ### Define Runspace START
    $pool = New-Runspace -MaxRunspaces $maxRunspaces -Verbose:$Verbose
    ### Define Runspace END

    $AllEvents = @()
    $AllErrors = @()

    if ($null -ne $ID) {
        $ID = $ID | Sort-Object -Unique
        Write-Verbose "Get-Events - Events to process in Total: $($Id.Count)"
        Write-Verbose "Get-Events - Events to process in Total ID: $ID"
        if ($Id.Count -gt 22) {
            Write-Verbose "Get-Events - There are more events to process then 22, split will be required."
            Write-Verbose "Get-Events - This means it will take twice the time to make a scan."
        }
    }

    $SplitArrayID = Split-Array -inArray $ID -size 22  # Support for more ID's then 22 (limitation of Get-WinEvent)
    $Runspaces = foreach ($ID in $SplitArrayID) {
        $EventFilter = @{}
        Add-ToHashTable -Hashtable $EventFilter -Key "LogName" -Value $LogName # Accepts Wildcard
        Add-ToHashTable -Hashtable $EventFilter -Key "ProviderName" -Value $ProviderName # Accepts Wildcard
        Add-ToHashTable -Hashtable $EventFilter -Key "Path" -Value $Path # https://blogs.technet.microsoft.com/heyscriptingguy/2011/01/25/use-powershell-to-parse-saved-event-logs-for-errors/
        Add-ToHashTable -Hashtable $EventFilter -Key "Keywords" -Value $Keywords
        Add-ToHashTable -Hashtable $EventFilter -Key "Id" -Value $ID
        Add-ToHashTable -Hashtable $EventFilter -Key "Level" -Value $Level
        Add-ToHashTable -Hashtable $EventFilter -Key "StartTime" -Value $DateFrom
        Add-ToHashTable -Hashtable $EventFilter -Key "EndTime" -Value $DateTo
        Add-ToHashTable -Hashtable $EventFilter -Key "UserID" -Value $UserSID
        Add-ToHashTable -Hashtable $EventFilter -Key "Data" -Value $Data

        foreach ($Comp in $Machine) {
            Write-Verbose "Get-Events - Processing computer $Comp for Events ID: $ID"
            Write-Verbose "Get-Events - Processing computer $Comp for Events ID Count: $($ID.Count)"
            Write-Verbose "Get-Events - Processing computer $Comp for Events LogName: $LogName"
            Write-Verbose "Get-Events - Processing computer $Comp for Events ProviderName: $ProviderName"
            Write-Verbose "Get-Events - Processing computer $Comp for Events Keywords: $Keywords"
            Write-Verbose "Get-Events - Processing computer $Comp for Events StartTime: $DateFrom"
            Write-Verbose "Get-Events - Processing computer $Comp for Events EndTime: $DateTo"
            Write-Verbose "Get-Events - Processing computer $Comp for Events Path: $Path"
            Write-Verbose "Get-Events - Processing computer $Comp for Events Level: $Level"
            Write-Verbose "Get-Events - Processing computer $Comp for Events UserID: $UserID"
            Write-Verbose "Get-Events - Processing computer $Comp for Events Data: $Data"
            Write-Verbose "Get-Events - Processing computer $Comp for Events MaxEvents: $MaxEvents"
            Write-Verbose "Get-Events - Processing computer $Comp for Events Path: $Path"
            Write-Verbose "Get-Events - Processing computer $Comp for Events UserSID: $UserSID"
            Write-Verbose "Get-Events - Processing computer $Comp for Events Oldest: $Oldest"
            Write-Verbose "Get-Events - Processing computer $Comp for Events RecordID: $RecordID"

            if ($DisableParallel) {
                Write-Verbose 'Get-Events - Running query with parallel disabled...'
                Invoke-Command -ScriptBlock $ScriptBlock -ArgumentList $Comp, $EventFilter, $MaxEvents, $Oldest, $Verbose
            } else {
                Write-Verbose 'Get-Events - Running query with parallel enabled...'
                $Parameters = [ordered] @{
                    Comp        = $Comp
                    EventFilter = $EventFilter
                    RecordID    = $RecordID
                    MaxEvents   = $MaxEvents
                    Oldest      = $Oldest
                    Verbose     = $Verbose
                }
                # returns values
                Start-Runspace -ScriptBlock $ScriptBlock -Parameters $Parameters -RunspacePool $pool -Verbose:$Verbose
            }
        }
    }
    ### End Runspaces START
    $AllEvents = Stop-Runspace -Runspaces $Runspaces -FunctionName "Get-Events" -RunspacePool $pool -Verbose:$Verbose -ErrorAction SilentlyContinue -ErrorVariable +AllErrors
    ### End Runspaces END

    $EventsProcessed = ($AllEvents | Measure-Object).Count
    Write-Verbose "Get-Events - Overall errors: $($AllErrors.Count)"
    Write-Verbose "Get-Events - Overall events processed in total for the report: $EventsProcessed"
    Write-Verbose "Get-Events - Overall time to generate $($MeasureTotal.Elapsed.Hours) hours, $($MeasureTotal.Elapsed.Minutes) minutes, $($MeasureTotal.Elapsed.Seconds) seconds, $($MeasureTotal.Elapsed.Milliseconds) milliseconds"
    $MeasureTotal.Stop()
    Write-Verbose "Get-Events - Overall events processing end"
    return $AllEvents
}