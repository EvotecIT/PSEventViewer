<#
    .SYNOPSIS
    This PowerShell module simplifies parsing Windows Event Log, has some problems thou... that will be addressed later
    .DESCRIPTION
    This PowerShell module simplifies parsing Windows Event Log, has some problems thou... that will be addressed later

    .NOTES
    Version:        0.32
    Author:         Przemyslaw Klys <przemyslaw.klys at evotec.pl>


    Get-WinEvent Help:
    - https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.diagnostics/get-winevent?view=powershell-6

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



Function ConvertTo-FlatObject {
    <#
      .SYNOPSIS
        Flatten an object to simplify discovery of data

      .DESCRIPTION
        Flatten an object.  This function will take an object, and flatten the properties using their full path into a single object with one layer of properties.

        You can use this to flatten XML, JSON, and other arbitrary objects.

        This can simplify initial exploration and discovery of data returned by APIs, interfaces, and other technologies.

        NOTE:
            Use tools like Get-Member, Select-Object, and Show-Object to further explore objects.
            This function does not handle certain data types well.  It was original designed to expand XML and JSON.

      .PARAMETER InputObject
        Object to flatten

      .PARAMETER Exclude
        Exclude any nodes in this list.  Accepts wildcards.

        Example:
            -Exclude price, title

      .PARAMETER ExcludeDefault
        Exclude default properties for sub objects.  True by default.

        This simplifies views of many objects (e.g. XML) but may exclude data for others (e.g. if flattening a process, ProcessThread properties will be excluded)

      .PARAMETER Include
        Include only leaves in this list.  Accepts wildcards.

        Example:
            -Include Author, Title

      .PARAMETER Value
        Include only leaves with values like these arguments.  Accepts wildcards.

      .PARAMETER MaxDepth
        Stop recursion at this depth.

      .INPUTS
        Any object

      .OUTPUTS
        System.Management.Automation.PSCustomObject

      .EXAMPLE

        #Pull unanswered PowerShell questions from StackExchange, Flatten the data to date a feel for the schema
        Invoke-RestMethod "https://api.stackexchange.com/2.0/questions/unanswered?order=desc&sort=activity&tagged=powershell&pagesize=10&site=stackoverflow" |
            ConvertTo-FlatObject -Include Title, Link, View_Count

            $object.items[0].owner.link : http://stackoverflow.com/users/1946412/julealgon
            $object.items[0].view_count : 7
            $object.items[0].link       : http://stackoverflow.com/questions/26910789/is-it-possible-to-reuse-a-param-block-across-multiple-functions
            $object.items[0].title      : Is it possible to reuse a &#39;param&#39; block across multiple functions?
            $object.items[1].owner.link : http://stackoverflow.com/users/4248278/nitin-tyagi
            $object.items[1].view_count : 8
            $object.items[1].link       : http://stackoverflow.com/questions/26909879/use-powershell-to-retreive-activated-features-for-sharepoint-2010
            $object.items[1].title      : Use powershell to retreive Activated features for sharepoint 2010
            ...

      .EXAMPLE

        #Set up some XML to work with
        $object = [xml]'
            <catalog>
               <book id="bk101">
                  <author>Gambardella, Matthew</author>
                  <title>XML Developers Guide</title>
                  <genre>Computer</genre>
                  <price>44.95</price>
               </book>
               <book id="bk102">
                  <author>Ralls, Kim</author>
                  <title>Midnight Rain</title>
                  <genre>Fantasy</genre>
                  <price>5.95</price>
               </book>
            </catalog>'

        #Call the flatten command against this XML
            ConvertTo-FlatObject $object -Include Author, Title, Price

            #Result is a flattened object with the full path to the node, using $object as the root.
            #Only leaf properties we specified are included (author,title,price)

                $object.catalog.book[0].author : Gambardella, Matthew
                $object.catalog.book[0].title  : XML Developers Guide
                $object.catalog.book[0].price  : 44.95
                $object.catalog.book[1].author : Ralls, Kim
                $object.catalog.book[1].title  : Midnight Rain
                $object.catalog.book[1].price  : 5.95

        #Invoking the property names should return their data if the orginal object is in $object:
            $object.catalog.book[1].price
                5.95

            $object.catalog.book[0].title
                XML Developers Guide

      .EXAMPLE

        #Set up some XML to work with
            [xml]'<catalog>
               <book id="bk101">
                  <author>Gambardella, Matthew</author>
                  <title>XML Developers Guide</title>
                  <genre>Computer</genre>
                  <price>44.95</price>
               </book>
               <book id="bk102">
                  <author>Ralls, Kim</author>
                  <title>Midnight Rain</title>
                  <genre>Fantasy</genre>
                  <price>5.95</price>
               </book>
            </catalog>' |
                ConvertTo-FlatObject -exclude price, title, id

        Result is a flattened object with the full path to the node, using XML as the root.  Price and title are excluded.

            $Object.catalog                : catalog
            $Object.catalog.book           : {book, book}
            $object.catalog.book[0].author : Gambardella, Matthew
            $object.catalog.book[0].genre  : Computer
            $object.catalog.book[1].author : Ralls, Kim
            $object.catalog.book[1].genre  : Fantasy

      .EXAMPLE
        #Set up some XML to work with
            [xml]'<catalog>
               <book id="bk101">
                  <author>Gambardella, Matthew</author>
                  <title>XML Developers Guide</title>
                  <genre>Computer</genre>
                  <price>44.95</price>
               </book>
               <book id="bk102">
                  <author>Ralls, Kim</author>
                  <title>Midnight Rain</title>
                  <genre>Fantasy</genre>
                  <price>5.95</price>
               </book>
            </catalog>' |
                ConvertTo-FlatObject -Value XML*, Fantasy

        Result is a flattened object filtered by leaves that matched XML* or Fantasy

            $Object.catalog.book[0].title : XML Developers Guide
            $Object.catalog.book[1].genre : Fantasy

      .EXAMPLE
        #Get a single process with all props, flatten this object.  Don't exclude default properties
        Get-Process | select -first 1 -skip 10 -Property * | ConvertTo-FlatObject -ExcludeDefault $false

        #NOTE - There will likely be bugs for certain complex objects like this.
                For example, $Object.StartInfo.Verbs.SyncRoot.SyncRoot... will loop until we hit MaxDepth. (Note: SyncRoot is now addressed individually)

      .NOTES
        I have trouble with algorithms.  If you have a better way to handle this, please let me know!

      .FUNCTIONALITY
        General Command
    #>
    [cmdletbinding()]
    param(

        [parameter( Mandatory = $True,
            ValueFromPipeline = $True)]
        [PSObject[]]$InputObject,
        [string[]]$Exclude = "",
        [bool]$ExcludeDefault = $True,
        [string[]]$Include = $null,
        [string[]]$Value = $null,
        [int]$MaxDepth = 10
    )
    Begin {
        #region FUNCTIONS

        #Before adding a property, verify that it matches a Like comparison to strings in $Include...
        Function IsIn-Include {
            param($prop)
            if (-not $Include) {$True}
            else {
                foreach ($Inc in $Include) {
                    if ($Prop -like $Inc) {
                        $True
                    }
                }
            }
        }

        #Before adding a value, verify that it matches a Like comparison to strings in $Value...
        Function IsIn-Value {
            param($val)
            if (-not $Value) {$True}
            else {
                foreach ($string in $Value) {
                    if ($val -like $string) {
                        $True
                    }
                }
            }
        }

        Function Get-Exclude {
            [cmdletbinding()]
            param($obj)

            #Exclude default props if specified, and anything the user specified.  Thanks to Jaykul for the hint on [type]!
            if ($ExcludeDefault) {
                Try {
                    $DefaultTypeProps = @( $obj.gettype().GetProperties() | Select-Object -ExpandProperty Name -ErrorAction Stop )
                    if ($DefaultTypeProps.count -gt 0) {
                        Write-Verbose "Excluding default properties for $($obj.gettype().Fullname):`n$($DefaultTypeProps | Out-String)"
                    }
                } Catch {
                    Write-Verbose "Failed to extract properties from $($obj.gettype().Fullname): $_"
                    $DefaultTypeProps = @()
                }
            }

            @( $Exclude + $DefaultTypeProps ) | Select-Object -Unique
        }

        #Function to recurse the Object, add properties to object
        Function Recurse-Object {
            [cmdletbinding()]
            param(
                $Object,
                [string[]]$path = '$Object',
                [psobject]$Output,
                $depth = 0
            )

            # Handle initial call
            Write-Verbose "Working in path $Path at depth $depth"
            Write-Debug "Recurse Object called with PSBoundParameters:`n$($PSBoundParameters | Out-String)"
            $Depth++

            #Exclude default props if specified, and anything the user specified.
            $ExcludeProps = @( Get-Exclude $object )

            #Get the children we care about, and their names
            $Children = $object.psobject.properties | Where-Object {$ExcludeProps -notcontains $_.Name }
            Write-Debug "Working on properties:`n$($Children | Select-Object -ExpandProperty Name | Out-String)"

            #Loop through the children properties.
            foreach ($Child in @($Children)) {
                $ChildName = $Child.Name
                $ChildValue = $Child.Value

                Write-Debug "Working on property $ChildName with value $($ChildValue | Out-String)"
                # Handle special characters...
                if ($ChildName -match '[^a-zA-Z0-9_]') {
                    $FriendlyChildName = "{$ChildName}"
                } else {
                    $FriendlyChildName = $ChildName
                }

                #Add the property.
                if ((IsIn-Include $ChildName) -and (IsIn-Value $ChildValue) -and $Depth -le $MaxDepth) {
                    $ThisPath = @( $Path + $FriendlyChildName ) -join "."
                    $Output | Add-Member -MemberType NoteProperty -Name $ThisPath -Value $ChildValue
                    Write-Verbose "Adding member '$ThisPath'"
                }

                #Handle null...
                if ($ChildValue -eq $null) {
                    Write-Verbose "Skipping NULL $ChildName"
                    continue
                }

                #Handle evil looping.  Will likely need to expand this.  Any thoughts on a better approach?
                if (
                    (
                        $ChildValue.GetType() -eq $Object.GetType() -and
                        $ChildValue -is [datetime]
                    ) -or
                    (
                        $ChildName -eq "SyncRoot" -and
                        -not $ChildValue
                    )
                ) {
                    Write-Verbose "Skipping $ChildName with type $($ChildValue.GetType().fullname)"
                    continue
                }

                #Check for arrays
                $IsArray = @($ChildValue).count -gt 1
                $count = 0

                #Set up the path to this node and the data...
                $CurrentPath = @( $Path + $FriendlyChildName ) -join "."

                #Exclude default props if specified, and anything the user specified.
                $ExcludeProps = @( Get-Exclude $ChildValue )

                #Get the children's children we care about, and their names.  Also look for signs of a hashtable like type
                $ChildrensChildren = $ChildValue.psobject.properties | Where-Object {$ExcludeProps -notcontains $_.Name }

                $HashKeys = if ($ChildValue.Keys -notlike $null -and $ChildValue.Values) {
                    $ChildValue.Keys
                } else {
                    $null
                }
                Write-Debug "Found children's children $($ChildrensChildren | Select-Object -ExpandProperty Name | Out-String)"
                #>
                #If we aren't at max depth or a leaf...
                if (
                    (@($ChildrensChildren).count -ne 0 -or $HashKeys) -and
                    $Depth -lt $MaxDepth
                ) {
                    #This handles hashtables.  But it won't recurse...
                    if ($HashKeys) {
                        Write-Verbose "Working on hashtable $CurrentPath"
                        foreach ($key in $HashKeys) {
                            Write-Verbose "Adding value from hashtable $CurrentPath['$key']"
                            $Output | Add-Member -MemberType NoteProperty -name "$CurrentPath['$key']" -value $ChildValue["$key"]
                            $Output = Recurse-Object -Object $ChildValue["$key"] -Path "$CurrentPath['$key']" -Output $Output -depth $depth
                        }
                    }
                    #Sub children?  Recurse!
                    else {
                        if ($IsArray) {
                            foreach ($item in @($ChildValue)) {
                                Write-Verbose "Recursing through array node '$CurrentPath'"
                                $Output = Recurse-Object -Object $item -Path "$CurrentPath[$count]" -Output $Output -depth $depth
                                $Count++
                            }
                        } else {
                            Write-Verbose "Recursing through node '$CurrentPath'"
                            $Output = Recurse-Object -Object $ChildValue -Path $CurrentPath -Output $Output -depth $depth
                        }
                    }
                }
            }

            $Output
        }

        #endregion FUNCTIONS
    }
    Process {
        Foreach ($Object in $InputObject) {
            #Flatten the XML and write it to the pipeline
            Recurse-Object -Object $Object -Output $( New-Object -TypeName PSObject )
        }
    }
}

Export-ModuleMember -function "Get-Events"