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
        [string] $Path = $null,
        [long[]] $Keywords = $null,
        [int] $RecordID,
        [int] $MaxRunspaces = [int]$env:NUMBER_OF_PROCESSORS + 1,
        [switch] $Oldest,
        [switch] $DisableParallel
    )

    Write-Verbose "Get-Events - Overall events processing start"
    $MeasureTotal = [System.Diagnostics.Stopwatch]::StartNew() # Timer Start

    if ($PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent) { $Verbose = $true } else { $Verbose = $false }

    ### Define Runspace START
    $runspaces = @()
    $pool = New-Runspace -MaxRunspaces $maxRunspaces -Verbose:$Verbose
    ### Define Runspace END

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
                $runspaces += Start-Runspace -ScriptBlock $ScriptBlock -Parameters $Parameters -RunspacePool $pool -Verbose:$Verbose
            }
        }
    }
    ### End Runspaces START
    $AllEvents = Stop-Runspace -Runspaces $runspaces -FunctionName 'Get-Events' -RunspacePool $pool -Verbose:$Verbose
    ### End Runspaces END

    $EventsProcessed = ($AllEvents | Measure-Object).Count
    Write-Verbose "Get-Events - Overall events processed in total for the report: $EventsProcessed"
    Write-Verbose "Get-Events - Overall time to generate $($MeasureTotal.Elapsed.Hours) hours, $($MeasureTotal.Elapsed.Minutes) minutes, $($MeasureTotal.Elapsed.Seconds) seconds, $($MeasureTotal.Elapsed.Milliseconds) milliseconds"
    $MeasureTotal.Stop()
    Write-Verbose "Get-Events - Overall events processing end"
    return $AllEvents
}

$ScriptBlock = {
    Param (
        [string]$Comp,
        [hashtable]$EventFilter,
        [int]$RecordID,
        [int]$MaxEvents,
        [bool] $Oldest,
        [bool] $Verbose
    )
    if ($Verbose) {
        $verbosepreference = 'continue'
    }
    function Get-EventsInternal () {
        [cmdletbinding()]
        param (
            [string]$Comp,
            [hashtable]$EventFilter,
            [int]$MaxEvents,
            [switch] $Oldest
        )
        Write-Verbose "Get-Events - Inside $Comp executing on: $($Env:COMPUTERNAME)"
        Write-Verbose "Get-Events - Inside $Comp for Events ID: $($EventFilter.ID)"
        Write-Verbose "Get-Events - Inside $Comp for Events ID: $($EventFilter.LogName)"
        Write-Verbose "Get-Events - Inside $Comp for Events RecordID: $RecordID"
        Write-Verbose "Get-Events - Inside $Comp for Events Oldest: $Oldest"
        Write-Verbose "Get-Events - Inside $Comp for Events Max Events: $MaxEvents"
        $Measure = [System.Diagnostics.Stopwatch]::StartNew() # Timer Start
        $Events = @()

        try {
            if ($RecordID -ne 0) {
                $FilterXML = @"

                <QueryList>
                    <Query Id="0" Path="$($EventFilter.LogName)">
                        <Select Path="$($EventFilter.LogName)">
                        *[
                            (System/EventID=$($EventFilter.ID))
                            and
                            (System/EventRecordID=$RecordID)
                         ]
                        </Select>
                    </Query>
                </QueryList>
"@
                Write-Verbose "Get-Events - Inside $Comp - FilterXML: $FilterXML"
                if ($MaxEvents -ne $null -and $MaxEvents -ne 0) {
                    $Events = Get-WinEvent -FilterXml $FilterXML -ComputerName $Comp -MaxEvents $MaxEvents -Oldest:$Oldest -ErrorAction Stop
                } else {
                    $Events = Get-WinEvent -FilterXml $FilterXML -ComputerName $Comp -Oldest:$Oldest -ErrorAction Stop
                }
            } else {
                foreach ($k in $EventFilter.Keys) {Write-Verbose "Get-Events - Inside $Comp Data in FilterHashTable $k $($EventFilter[$k])"}
                if ($MaxEvents -ne 0) {
                    #Write-Verbose "Get-Events - Inside $Comp - Running (1-1) with MaxEvents: $Maxevents"
                    $Events = Get-WinEvent -FilterHashtable $EventFilter -ComputerName $Comp -MaxEvents $MaxEvents -Oldest:$Oldest -ErrorAction Stop
                    #Write-Verbose "Get-Events - Inside $Comp - Running (1-2) with MaxEvents: $Maxevents"
                } else {
                    #Write-Verbose "Get-Events - Inside $Comp - Running (2-1) with MaxEvents: $Maxevents"
                    $Events = Get-WinEvent -FilterHashtable $EventFilter -ComputerName $Comp -Oldest:$Oldest -ErrorAction Stop
                    #Write-Verbose "Get-Events - Inside $Comp - Running (2-2) with MaxEvents: $Maxevents"
                }
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
        Write-Verbose "Get-Events - Inside $Comp - Processing events..."

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
            # This adds some fields specific to PSWinReporting
            $MessageSubject = ($Event.Message -split '\n')[0]
            Add-Member -InputObject $Event -MemberType NoteProperty -Name 'MessageSubject' -Value $MessageSubject -Force
            Add-Member -InputObject $Event -MemberType NoteProperty -Name 'Action' -Value $MessageSubject -Force

            if ($Event.SubjectDomainName -and $Event.SubjectUserName) {
                Add-Member -InputObject $Event -MemberType NoteProperty -Name 'Who' -Value "$($Event.SubjectDomainName)\$($Event.SubjectUserName)" -Force
            }
            if ($Event.TargetDomainName -and $Event.TargetUserName) {
                Add-Member -InputObject $Event -MemberType NoteProperty -Name 'ObjectAffected' -Value "$($Event.TargetDomainName)\$($Event.TargetUserName)" -Force
            }
            if ($Event.MemberName) {
                $MemberNameWithoutCN = $Event.MemberName -replace '^CN=|,.*$'
                Add-Member -InputObject $Event -MemberType NoteProperty -Name 'MemberNameWithoutCN' -Value $MemberNameWithoutCN -Force
            }
        }
        Write-Verbose "Get-Events - Inside $Comp - Time to generate $($Measure.Elapsed.Hours) hours, $($Measure.Elapsed.Minutes) minutes, $($Measure.Elapsed.Seconds) seconds, $($Measure.Elapsed.Milliseconds) milliseconds"
        $Measure.Stop()
        return $Events
    }
    Write-Verbose 'Get-Events - preparing to run'
    $Data = Get-EventsInternal -Comp $Comp -EventFilter $EventFilter -MaxEvents $MaxEvents -Oldest:$Oldest -Verbose:$Verbose
    if ($EventFilter.Path) {
        $Data | Add-Member -MemberType NoteProperty -Name "GatheredFrom" -Value $EventFilter.Path -Force
    } else {
        $Data | Add-Member -MemberType NoteProperty -Name "GatheredFrom" -Value $Comp -Force
    }
    $Data | Add-Member -MemberType NoteProperty -Name "GatheredLogName" -Value $EventFilter.LogName -Force
    Write-Verbose 'Get-Events - finished run'
    return $Data
}