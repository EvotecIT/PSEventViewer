$ScriptBlock = {
    Param (
        [string]$Comp,
        [hashtable]$EventFilter,
        [int64]$RecordID,
        [int]$MaxEvents,
        [bool] $Oldest,
        [bool] $Verbose
    )
    if ($Verbose) {
        $VerbosePreference = 'continue'
    }
    #$WarningPreference = 'continue'
    function Get-EventsInternal () {
        [CmdLetBinding()]
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
                foreach ($k in $EventFilter.Keys) {
                    Write-Verbose "Get-Events - Inside $Comp Data in FilterHashTable $k $($EventFilter[$k])"
                }
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
                #Write-Verbose -Message "Get-Events - Inside $Comp - Error connecting."
                Write-Verbose -Message "Get-Events - Inside $Comp - Error $($_.Exception.Message)"
                Write-Error -Message "(ComputerName: $Comp) $_"
            } else {
                #Write-Verbose -Message "Get-Events - Inside $Comp - Error connecting."
                Write-Verbose -Message "Get-Events - Inside $Comp - Error $($_.Exception.Message)"
                Write-Error -Message "(ComputerName: $Comp) $_"
            }
            Write-Verbose "Get-Events - Inside $Comp - Time to generate $($Measure.Elapsed.Hours) hours, $($Measure.Elapsed.Minutes) minutes, $($Measure.Elapsed.Seconds) seconds, $($Measure.Elapsed.Milliseconds) milliseconds"
            $Measure.Stop()

            #Write-Warning -Message 'My Test'
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

            $EventTopNodes = Get-Member -InputObject $eventXML.Event -MemberType Properties | Where-Object { $_.Name -ne 'System' -and $_.Name -ne 'xmlns'}
            foreach ($EventTopNode in $EventTopNodes) {
                $TopNode = $EventTopNode.Name

                $EventSubsSubs = Get-Member -InputObject $eventXML.Event.$TopNode -Membertype Properties
                $h = 0
                foreach ($EventSubSub in $EventSubsSubs) {
                    $SubNode = $EventSubSub.Name
                    #$EventSubSub | ft -a
                    if ($EventSubSub.Definition -like "System.Object*") {
                        if (Get-Member -InputObject $eventXML.Event.$TopNode -name "$SubNode" -Membertype Properties) {

                            # Case 1
                            $SubSubNode = Get-Member -InputObject $eventXML.Event.$TopNode.$SubNode -MemberType Properties | Where-Object { $_.Name -ne 'xmlns' -and $_.Definition -like "string*" }
                            foreach ($Name in $SubSubNode.Name) {
                                $fieldName = $Name
                                $fieldValue = $eventXML.Event.$TopNode.$SubNode.$Name
                                Add-Member -InputObject $Event -MemberType NoteProperty -Name $fieldName -Value $fieldValue -Force
                            }
                            # Case 1

                            For ($i = 0; $i -lt $eventXML.Event.$TopNode.$SubNode.Count; $i++) {
                                if (Get-Member -InputObject $eventXML.Event.$TopNode.$SubNode[$i] -name "Name" -Membertype Properties) {
                                    # Case 2
                                    $fieldName = $eventXML.Event.$TopNode.$SubNode[$i].Name
                                    if (Get-Member -InputObject $eventXML.Event.$TopNode.$SubNode[$i] -name "#text" -Membertype Properties) {
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
                        $SubSubNode = Get-Member -InputObject $eventXML.Event.$TopNode.$SubNode -MemberType Properties | Where-Object { $_.Name -ne 'xmlns' -and $_.Definition -like "string*" }
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
            [string] $MessageSubject = ($Event.Message -split '\n')[0]
            Add-Member -InputObject $Event -MemberType NoteProperty -Name 'MessageSubject' -Value $MessageSubject -Force
            Add-Member -InputObject $Event -MemberType NoteProperty -Name 'Action' -Value $MessageSubject -Force

            if ($Event.SubjectDomainName -and $Event.SubjectUserName) {
                Add-Member -InputObject $Event -MemberType NoteProperty -Name 'Who' -Value "$($Event.SubjectDomainName)\$($Event.SubjectUserName)" -Force
            }
            if ($Event.TargetDomainName -and $Event.TargetUserName) {
                Add-Member -InputObject $Event -MemberType NoteProperty -Name 'ObjectAffected' -Value "$($Event.TargetDomainName)\$($Event.TargetUserName)" -Force
            }
            if ($Event.MemberName) {
                [string] $MemberNameWithoutCN = $Event.MemberName -replace '^CN=|,.*$'
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
    return @($Data)
}