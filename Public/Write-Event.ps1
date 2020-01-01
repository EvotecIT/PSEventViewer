function Write-Event {
    [alias('Write-WinEvent', 'Write-Events')]
    [cmdletBinding()]
    param(
        [string[]] $Computer,
        [Parameter(Mandatory)][alias('EventLog')][string] $LogName,
        [Parameter(Mandatory)][alias('Provider', 'ProviderName')][string] $Source,
        [int] $Category,
        [alias('Level')][System.Diagnostics.EventLogEntryType] $EntryType = [System.Diagnostics.EventLogEntryType]::Information,
        [Parameter(Mandatory)][alias('EventID')][int] $ID,
        [Parameter(Mandatory)][string] $Message,
        [Array] $AdditionalFields
    )
    Begin {
        #Load the event source to the log if not already loaded.  This will fail if the event source is already assigned to a different log.
        <# This errors out when run not as Administrator on Security log, even thou was
        if ([System.Diagnostics.EventLog]::SourceExists($Source) -eq $false) {
            try {
                [System.Diagnostics.EventLog]::CreateEventSource($source, $evtlog)
            } catch {
                Write-Warning "New-WinEvent - Couldn't create new event log source - $($_.ExceptionMessage)"
                return
            }
        }
        #>
    }
    Process {
        if (-not $Computer) {
            # Funnily enough checking for Source existance is only required for local computer. It works fine for remote computers for any Source
            $SourceExists = Get-WinEvent -ListProvider $Source -ErrorAction SilentlyContinue
            if ($null -eq $SourceExists -or $SourceExists.LogLinks.LogName -notcontains $LogName) {
                try {
                    New-EventLog -LogName $LogName -Source $Source -ErrorAction Stop
                } catch {
                    Write-Warning "New-WinEvent - Couldn't create new event log source - $($_.Exception.Message)"
                    return
                }
            }
            # We need to set $Computer to LocalComputer if user didn't fill it in.
            $Computer = $Env:COMPUTERNAME
        }
        foreach ($Machine in $Computer) {
            <#
            System.Diagnostics.EventInstance new(long instanceId, int categoryId)
            System.Diagnostics.EventInstance new(long instanceId, int categoryId, System.Diagnostics.EventLogEntryType entryType)
            #>
            $EventInstance = [System.Diagnostics.EventInstance]::new($ID, $Category, $EntryType)
            $Event = [System.Diagnostics.EventLog]::new()
            $Event.Log = $LogName
            $Event.Source = $Source
            if ($Machine -ne $Env:COMPUTERNAME) {
                $Event.MachineName = $Machine
            }
            [Array] $JoinedMessage = @(
                $Message
                $AdditionalFields | ForEach-Object { $_ }
            )
            try {
                $Event.WriteEvent($EventInstance, $JoinedMessage)
            } catch {
                Write-Warning "Write-Event - Couldn't create new event - $($_.Exception.Message)"
            }
        }
    }
}