<#
    .SYNOPSIS
    This PowerShell module simplifies parsing Windows Event Log, has some problems thou... that will be addressed later
    .DESCRIPTION
    This PowerShell module simplifies parsing Windows Event Log, has some problems thou... that will be addressed later

    .NOTES
    Version:        0.1
    Author:         Przemyslaw Klys <przemyslaw.klys at evotec.pl>

    .EXAMPLE
        $DateFrom = (get-date).AddHours(-5)
        $DateTo = (get-date).AddHours(1)
        get-events -Computer "Evo1" -DateFrom $DateFrom -DateTo $DateTo -EventId 916 -LogType "Application"
#>
function Add-ToHashTable($Hashtable, $Key, $Value) {
    if ($Value -ne $null -and $Value -ne '') {
        $Hashtable.Add($Key, $Value)
    }
}
function Get-Events {
    [cmdletbinding()]
    param (
        [alias ("ADDomainControllers", "DomainController", "Server", "Servers", "Computer", "Computers")] [string[]] $Machine = $Env:COMPUTERNAME,
        [alias ("From")][nullable[DateTime]] $DateFrom = $null,
        [alias ("To")][nullable[DateTime]] $DateTo = $null,
        [alias ("Ids", "EventID", "EventIds")] [int[]] $ID = $null,
        [alias ("LogType", "Log")][string] $LogName = $null,
        [alias ("Provider")] [string] $ProviderName = '',
        $Level = $null,
        $UserSID = $null,
        $Data = $null,
        $MaxEvents = $null,
        $Credentials = $null,
        $Path = $null,
        $Keywords = $null,
        [switch] $Oldest

    )
    $MeasureTotal = [System.Diagnostics.Stopwatch]::StartNew() # Timer Start
    $AllEvents = @()
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

    #$EventFilter | ForEach-Object { new-object PSObject -Property $_} | Format-List # Format-Table -AutoSize

    foreach ($Comp in $Machine) {
        $Measure = [System.Diagnostics.Stopwatch]::StartNew() # Timer Start

        Write-Verbose "Processing computer $Comp for Events ID: $Id"
        Write-Verbose "Processing computer $Comp for Events LogName: $LogName"
        Write-Verbose "Processing computer $Comp for Events ProviderName: $ProviderName"
        Write-Verbose "Processing computer $Comp for Events Keywords: $Keywords"
        Write-Verbose "Processing computer $Comp for Events StartTime: $DateFrom"
        Write-Verbose "Processing computer $Comp for Events EndTime: $DateTo"
        Write-Verbose "Processing computer $Comp for Events Path: $Path"
        Write-Verbose "Processing computer $Comp for Events Level: $Level"
        Write-Verbose "Processing computer $Comp for Events DateTo: $DateTo"
        Write-Verbose "Processing computer $Comp for Events UserID: $UserID"
        Write-Verbose "Processing computer $Comp for Events Data: $Data"

        $Events = @()
        try {
            if ($MaxEvents -ne $null) {
                $Events = Get-WinEvent -FilterHashtable $EventFilter -ComputerName $Comp -ErrorAction Stop -MaxEvents $MaxEvents
            } else {
                $Events = Get-WinEvent -FilterHashtable $EventFilter -ComputerName $Comp -ErrorAction Stop
            }
            $EventsCount = ($Events | Measure-Object).Count
            Write-Verbose -Message "Events processed $EventsCount on computer $comp"
        } catch {
            if ($_.Exception -match "No events were found that match the specified selection criteria") {
                Write-Verbose -Message "No events found on computer: $comp"
            } elseif ($_.Exception -match "There are no more endpoints available from the endpoint") {
                Write-Verbose -Message "Error connecting to computer $($Comp)"
                Write-Verbose -Message "Error $($_.Exception.Message)"
            } else {
                Write-Verbose -Message "Error connecting to computer $($Comp)"
                Write-Verbose -Message "Error $($_.Exception.Message)"
            }
            Write-Verbose "Time to generate $($Measure.Elapsed.Hours) hours, $($Measure.Elapsed.Minutes) minutes, $($Measure.Elapsed.Seconds) seconds, $($Measure.Elapsed.Milliseconds) milliseconds"
            $Measure.Stop()
            continue
        }
        # Parse out the event message data
        ForEach ($Event in $Events) {
            # Convert the event to XML
            $eventXML = [xml]$Event.ToXml()
            # Iterate through each one of the XML message properties
            Add-Member -InputObject $Event -MemberType NoteProperty -Name "Computer" -Value $event.MachineName.ToString() -Force
            Add-Member -InputObject $Event -MemberType NoteProperty -Name "Date" -Value $Event.TimeCreated -Force

            # Get-Member -inputobject $eventXML.Event

            if (Get-Member -inputobject $eventXML.Event.EventData -name "Data" -Membertype Properties) {
                if (Get-Member -inputobject $eventXML.Event.EventData.Data -name "Count" -Membertype Properties) {
                    For ($i = 0; $i -lt $eventXML.Event.EventData.Data.Count; $i++) {
                        if (Get-Member -inputobject $eventXML.Event.EventData.Data[$i] -name "Name" -Membertype Properties) {
                            $fieldName = $eventXML.Event.EventData.Data[$i].Name
                        } else {
                            $fieldName = ""
                        }
                        if (Get-Member -inputobject $eventXML.Event.EventData.Data[$i] -name "#text" -Membertype Properties) {
                            $fieldValue = $eventXML.Event.EventData.Data[$i]."#text"
                            if ($fieldValue -eq "-".Trim()) { $fieldValue = $fieldValue -replace "-" }
                        } else {
                            $fieldValue = ""
                        }
                        # Append these as object properties
                        if ($fieldName -ne "") {
                            Add-Member -InputObject $Event -MemberType NoteProperty -Name $fieldName -Value $fieldValue -Force
                        }
                    }
                }
            }
        }
        $Allevents += $events
        Write-Verbose "Time to generate $($Measure.Elapsed.Hours) hours, $($Measure.Elapsed.Minutes) minutes, $($Measure.Elapsed.Seconds) seconds, $($Measure.Elapsed.Milliseconds) milliseconds"
        $Measure.Stop()
    }
    $EventsProcessed = ($Allevents | Measure-Object).Count
    Write-Verbose "Total events processed in total for the report: $EventsProcessed"
    Write-Verbose "Total time to generate $($MeasureTotal.Elapsed.Hours) hours, $($MeasureTotal.Elapsed.Minutes) minutes, $($MeasureTotal.Elapsed.Seconds) seconds, $($MeasureTotal.Elapsed.Milliseconds) milliseconds"
    $MeasureTotal.Stop()
    return $Allevents
}

Export-ModuleMember -function "Get-Events"