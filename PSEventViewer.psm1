<#
    .SYNOPSIS
    This PowerShell module simplifies parsing Windows Event Log
    .DESCRIPTION
    This PowerShell module simplifies parsing Windows Event Log

    .NOTES
    Version:        0.5
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
        $Path = $null,
        [long[]] $Keywords = $null,
        [switch] $Oldest,
        [switch] $DisableParallel
    )

    Write-Verbose "Get-Events - Overall events processing start"
    $MeasureTotal = [System.Diagnostics.Stopwatch]::StartNew() # Timer Start

    if ($PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent) { $Verbose = $true } else { $Verbose = $false }

    ### Define Runspace
    $pool = [RunspaceFactory]::CreateRunspacePool(1, [int]$env:NUMBER_OF_PROCESSORS + 1)
    $pool.ApartmentState = "MTA"
    $pool.Open()
    $runspaces = @()
    ### Define Runspace

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



            $ScriptBlock = {
                #[cmdletbinding()]
                Param (
                    [string]$Comp,
                    [hashtable]$EventFilter,
                    [int]$MaxEvents,
                    [bool] $Oldest,
                    [bool] $Verbose
                )
                if ($Verbose) {
                    $verbosepreference = 'continue'
                }
                function Get-EventsInternal () {
                    #[cmdletbinding()]
                    param (
                        [string]$Comp,
                        [hashtable]$EventFilter,
                        [int]$MaxEvents,
                        [bool] $Oldest,
                        [bool] $Verbose
                    )

                    if ($Verbose) {
                        $verbosepreference = 'continue'
                    }
                    Write-Verbose "Get-Events - Inside $Comp executing on: $($Env:COMPUTERNAME)"
                    Write-Verbose "Get-Events - Inside $Comp for Events ID: $($EventFilter.ID)"
                    Write-Verbose "Get-Events - Inside $Comp for Events ID: $($EventFilter.LogName)"
                    Write-Verbose "Get-Events - Inside $Comp for Events Oldest: $Oldest"
                    Write-Verbose "Get-Events - Inside $Comp for Events Max Events: $MaxEvents"
                    Write-Verbose "Get-Events - Inside $Comp for Events Verbose: $Verbose"

                    $Measure = [System.Diagnostics.Stopwatch]::StartNew() # Timer Start
                    $Events = @()

                    try {
                        if ($MaxEvents -ne $null -and $MaxEvents -ne 0) {
                            $Events = Get-WinEvent -FilterHashtable $EventFilter -ComputerName $Comp -MaxEvents $MaxEvents -Oldest:$Oldest -ErrorAction Stop
                        } else {
                            $Events = Get-WinEvent -FilterHashtable $EventFilter -ComputerName $Comp -Oldest:$Oldest -ErrorAction Stop
                        }
                        $EventsCount = ($Events | Measure-Object).Count
                        Write-Verbose -Message "Get-Events - Inside $Comp Events founds $EventsCount"
                    } catch {
                        if ($_.Exception -match "No events were found that match the specified selection criteria") {
                            Write-Verbose -Message "Get-Events - Inside $Comp - No events found."
                        } elseif ($_.Exception -match "There are no more endpoints available from the endpoint") {
                            Write-Verbose -Message "Get-Events - Inside $Comp - Error connecting."
                            Write-Verbose -Message "Get-Events - Inside $Comp - Error $($_.Exception.Message)"
                        } else {
                            Write-Verbose -Message "Get-Events - Inside $Comp - Error connecting."
                            Write-Verbose -Message "Get-Events - Inside $Comp - Error $($_.Exception.Message)"
                        }
                        Write-Verbose "Get-Events - Inside $Comp - Time to generate $($Measure.Elapsed.Hours) hours, $($Measure.Elapsed.Minutes) minutes, $($Measure.Elapsed.Seconds) seconds, $($Measure.Elapsed.Milliseconds) milliseconds"
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

                        $EventTopNodes = Get-Member -inputobject $eventXML.Event -MemberType Properties | Where-Object { $_.Name -ne 'System' -and $_.Name -ne 'xmlns'}
                        foreach ($EventTopNode in $EventTopNodes) {
                            $TopNode = $EventTopNode.Name

                            $EventSubsSubs = Get-Member -inputobject $eventXML.Event.$TopNode -Membertype Properties
                            $h = 0
                            foreach ($EventSubSub in $EventSubsSubs) {
                                $SubNode = $EventSubSub.Name
                                #$EventSubSub | ft -a
                                if ($EventSubSub.Definition -like "System.Object*") {
                                    if (Get-Member -inputobject $eventXML.Event.$TopNode -name "$SubNode" -Membertype Properties) {

                                        # Case 1
                                        $SubSubNode = Get-Member -inputobject $eventXML.Event.$TopNode.$SubNode -MemberType Properties | Where-Object { $_.Name -ne 'xmlns' -and $_.Definition -like "string*" }
                                        foreach ($Name in $SubSubNode.Name) {
                                            $fieldName = $Name
                                            $fieldValue = $eventXML.Event.$TopNode.$SubNode.$Name
                                            Add-Member -InputObject $Event -MemberType NoteProperty -Name $fieldName -Value $fieldValue -Force
                                        }
                                        # Case 1

                                        For ($i = 0; $i -lt $eventXML.Event.$TopNode.$SubNode.Count; $i++) {
                                            if (Get-Member -inputobject $eventXML.Event.$TopNode.$SubNode[$i] -name "Name" -Membertype Properties) {
                                                # Case 2
                                                $fieldName = $eventXML.Event.$TopNode.$SubNode[$i].Name
                                                if (Get-Member -inputobject $eventXML.Event.$TopNode.$SubNode[$i] -name "#text" -Membertype Properties) {
                                                    $fieldValue = $eventXML.Event.$TopNode.$SubNode[$i]."#text"
                                                    if ($fieldValue -eq "-".Trim()) { $fieldValue = $fieldValue -replace "-" }
                                                } else {
                                                    $fieldValue = ""
                                                }
                                                if ($fieldName -ne "") {
                                                    Add-Member -InputObject $Event -MemberType NoteProperty -Name $fieldName -Value $fieldValue -Force
                                                }
                                                # Case 2
                                            } else {
                                                # Case 3
                                                $Value = $eventXML.Event.$TopNode.$SubNode[$i]
                                                if ($Value.Name -ne 'Name' -and $Value.Name -ne '#text') {
                                                    $fieldName = "NoNameA$i"
                                                    $fieldValue = $Value
                                                    Add-Member -InputObject $Event -MemberType NoteProperty -Name $fieldName -Value $fieldValue -Force
                                                }
                                                # Case 3
                                            }

                                        }
                                        # }
                                    }
                                } elseif ($EventSubSub.Definition -like "System.Xml.XmlElement*") {
                                    # Case 1
                                    $SubSubNode = Get-Member -inputobject $eventXML.Event.$TopNode.$SubNode -MemberType Properties | Where-Object { $_.Name -ne 'xmlns' -and $_.Definition -like "string*" }
                                    foreach ($Name in $SubSubNode.Name) {
                                        $fieldName = $Name
                                        $fieldValue = $eventXML.Event.$TopNode.$SubNode.$Name
                                        Add-Member -InputObject $Event -MemberType NoteProperty -Name $fieldName -Value $fieldValue -Force
                                    }
                                    # Case 1
                                } else {
                                    # Case 4
                                    $h++
                                    $fieldName = "NoNameB$h"
                                    $fieldValue = $eventXML.Event.$TopNode.$SubNode
                                    Add-Member -InputObject $Event -MemberType NoteProperty -Name $fieldName -Value $fieldValue -Force
                                    # Case 4
                                }
                            }
                        }
                    }
                    Write-Verbose "Get-Events - Inside $Comp - Time to generate $($Measure.Elapsed.Hours) hours, $($Measure.Elapsed.Minutes) minutes, $($Measure.Elapsed.Seconds) seconds, $($Measure.Elapsed.Milliseconds) milliseconds"
                    $Measure.Stop()
                    return $Events
                }
                return Get-EventsInternal -Comp $Comp -EventFilter $EventFilter -MaxEvents $MaxEvents -Oldest:$Oldest -Verbose $Verbose
            }
            if ($DisableParallel) {
                Write-Verbose 'Get-Events - Running query with parallel disabled...'
                Invoke-Command -ScriptBlock $ScriptBlock -ArgumentList $Comp, $EventFilter, $MaxEvents, $Oldest, $Verbose
            } else {
                Write-Verbose 'Get-Events - Running query with parallel enabled...'
                $runspace = [PowerShell]::Create()
                $null = $runspace.AddScript($ScriptBlock)
                $null = $runspace.AddParameter('Comp', $Comp)
                $null = $runspace.AddParameter('EventFilter', $EventFilter)
                $null = $runspace.AddParameter('MaxEvents', $MaxEvents)
                $null = $runspace.AddParameter('Oldest', $Oldest)
                $null = $runspace.AddParameter('Verbose', $Verbose)
                $runspace.RunspacePool = $pool
                $runspaces += [PSCustomObject]@{ Pipe = $runspace; Status = $runspace.BeginInvoke() }
            }
        }
    }
    ### End Runspaces
    while ($runspaces.Status -ne $null) {
        $completed = $runspaces | Where-Object { $_.Status.IsCompleted -eq $true }
        foreach ($runspace in $completed) {
            foreach ($e in $($runspace.Pipe.Streams.Error)) {
                Write-Verbose "Get-Events - Error from runspace: $e"
            }
            foreach ($v in $($runspace.Pipe.Streams.Verbose)) {
                Write-Verbose "Get-Events - Verbose from runspace: $v"
            }
            $AllEvents += $runspace.Pipe.EndInvoke($runspace.Status)
            $runspace.Status = $null
        }
    }
    $pool.Close()
    $pool.Dispose()
    ### End Runspaces


    $EventsProcessed = ($AllEvents | Measure-Object).Count
    Write-Verbose "Get-Events - Overall events processed in total for the report: $EventsProcessed"
    Write-Verbose "Get-Events - Overall time to generate $($MeasureTotal.Elapsed.Hours) hours, $($MeasureTotal.Elapsed.Minutes) minutes, $($MeasureTotal.Elapsed.Seconds) seconds, $($MeasureTotal.Elapsed.Milliseconds) milliseconds"
    $MeasureTotal.Stop()
    Write-Verbose "Get-Events - Overall events processing end"
    return $AllEvents
}

Export-ModuleMember -function "Get-Events"