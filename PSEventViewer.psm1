<#
    .SYNOPSIS
    This PowerShell module simplifies parsing Windows Event Log, has some problems thou... that will be addressed later
    .DESCRIPTION
    This PowerShell module simplifies parsing Windows Event Log, has some problems thou... that will be addressed later

    .NOTES
    Version:        0.28
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
Function Split-Every($list, $count = 4) {
    $aggregateList = @()

    $blocks = [Math]::Floor($list.Count / $count)
    $leftOver = $list.Count % $count
    $start = $end = 0
    for ($i = 0; $i -lt $blocks; $i++) {
        $end = $count * ($i + 1) - 1

        $aggregateList += @(, $list[$start..$end])
        $start = $end + 1
    }
    if ($leftOver -gt 0) {
        $aggregateList += @(, $list[$start..($end + $leftOver)])
    }

    $aggregateList
}
function Split-Array {
    <#
        .SYNOPSIS
        Split an array
        .NOTES
        Version : July 2, 2017 - implemented suggestions from ShadowSHarmon for performance
        .PARAMETER inArray
        A one dimensional array you want to split
        .EXAMPLE
        Split-array -inArray @(1,2,3,4,5,6,7,8,9,10) -parts 3
        .EXAMPLE
        Split-array -inArray @(1,2,3,4,5,6,7,8,9,10) -size 3
    #>

    param($inArray, [int]$parts, [int]$size)
    if ($parts) {
        $PartSize = [Math]::Ceiling($inArray.count / $parts)
    }
    if ($size) {
        $PartSize = $size
        $parts = [Math]::Ceiling($inArray.count / $size)
    }
    $outArray = New-Object 'System.Collections.Generic.List[psobject]'
    for ($i = 1; $i -le $parts; $i++) {
        $start = (($i - 1) * $PartSize)
        $end = (($i) * $PartSize) - 1
        if ($end -ge $inArray.count) {$end = $inArray.count - 1}
        $outArray.Add(@($inArray[$start..$end]))
    }
    return , $outArray
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
    Write-Verbose "Get-Events - Overall events processing start"
    $MeasureTotal = [System.Diagnostics.Stopwatch]::StartNew() # Timer Start
    $AllEvents = @()

    if ($ID -ne $null) {
        $ID = $ID | Sort-Object -Unique
        Write-Verbose "Get-Events - Events to process in Total: $($Id.Count)"
        Write-Verbose "Get-Events - Events to process in Total ID: $ID"
        if ($Id.Count -gt 22) {
            Write-Verbose "Get-Events - There are more events to process then 22, split will be required."
            Write-Verbose "Get-Events - This means it will take twice the time to make a scan."
        }
    }
    $SplitArrayID = Split-Array -inArray $ID -size 22  # Support for more ID's then 22 (limitation of Get-WinEvent)
    foreach ($ID in $SplitArrayID) {
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
            $Measure = [System.Diagnostics.Stopwatch]::StartNew() # Timer Start
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
            Write-Verbose "Get-Events - Processing computer $Comp for Events NaxEvents: $MaxEvents"
            Write-Verbose "Get-Events - Processing computer $Comp for Events Path: $Path"
            Write-Verbose "Get-Events - Processing computer $Comp for Events UserSID: $UserSID"
            Write-Verbose "Get-Events - Processing computer $Comp for Events Oldest: $Oldest"

            $Events = @()
            try {
                if ($MaxEvents -ne $null) {
                    $Events = Get-WinEvent -FilterHashtable $EventFilter -ComputerName $Comp -ErrorAction Stop -MaxEvents $MaxEvents
                } else {
                    $Events = Get-WinEvent -FilterHashtable $EventFilter -ComputerName $Comp -ErrorAction Stop
                }
                $EventsCount = ($Events | Measure-Object).Count
                Write-Verbose -Message "Get-Events - Events processed $EventsCount on computer $comp"
            } catch {
                if ($_.Exception -match "No events were found that match the specified selection criteria") {
                    Write-Verbose -Message "Get-Events - Processing computer $Comp - No events found."
                } elseif ($_.Exception -match "There are no more endpoints available from the endpoint") {
                    Write-Verbose -Message "Get-Events - Processing computer $Comp - Error connecting."
                    Write-Verbose -Message "Get-Events - Processing computer $Comp - Error $($_.Exception.Message)"
                } else {
                    Write-Verbose -Message "Get-Events - Processing computer $Comp - Error connecting."
                    Write-Verbose -Message "Get-Events - Processing computer $Comp - Error $($_.Exception.Message)"
                }
                Write-Verbose "Get-Events - Processing computer $Comp - Time to generate $($Measure.Elapsed.Hours) hours, $($Measure.Elapsed.Minutes) minutes, $($Measure.Elapsed.Seconds) seconds, $($Measure.Elapsed.Milliseconds) milliseconds"
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
            Write-Verbose "Get-Events - Processing computer $Comp - Time to generate $($Measure.Elapsed.Hours) hours, $($Measure.Elapsed.Minutes) minutes, $($Measure.Elapsed.Seconds) seconds, $($Measure.Elapsed.Milliseconds) milliseconds"
            $Measure.Stop()
        }
    }
    $EventsProcessed = ($Allevents | Measure-Object).Count
    Write-Verbose "Get-Events - Overall events processed in total for the report: $EventsProcessed"
    Write-Verbose "Get-Events - Overall time to generate $($MeasureTotal.Elapsed.Hours) hours, $($MeasureTotal.Elapsed.Minutes) minutes, $($MeasureTotal.Elapsed.Seconds) seconds, $($MeasureTotal.Elapsed.Milliseconds) milliseconds"
    $MeasureTotal.Stop()
    return $Allevents
}

Export-ModuleMember -function "Get-Events"