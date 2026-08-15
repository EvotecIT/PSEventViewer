Describe 'Get-EVXEvent - Named Event' {
    It 'uses NamedEvent as the canonical parameter and preserves migration aliases' {
        $Parameter = (Get-Command Get-EVXEvent).Parameters['NamedEvent']
        $Parameter.Aliases | Should -Contain 'NamedEvents'
        $Parameter.Aliases | Should -Contain 'Type'
    }

    It 'exposes opt-in DNS enrichment only on the NamedEvents parameter set' {
        $Command = Get-Command Get-EVXEvent
        $Command.Parameters.Keys | Should -Contain 'ResolveDns'
        $Command.Parameters.Keys | Should -Contain 'DnsTimeoutMs'
        $Command.Parameters.Keys | Should -Contain 'DnsMaxConcurrency'

        $ResolveDnsSets = @($Command.Parameters['ResolveDns'].ParameterSets.Keys)
        $DnsTimeoutSets = @($Command.Parameters['DnsTimeoutMs'].ParameterSets.Keys)
        $DnsConcurrencySets = @($Command.Parameters['DnsMaxConcurrency'].ParameterSets.Keys)
        $ResolveDnsSets | Should -Be @('NamedEvent')
        $DnsTimeoutSets | Should -Be @('NamedEvent')
        $DnsConcurrencySets | Should -Be @('NamedEvent')
    }

    It 'exposes native remote, failure, bookmark, and Int64 scan controls without no-op filters' {
        $Command = Get-Command Get-EVXEvent
        $NamedSet = $Command.ParameterSets |
            Where-Object Name -EQ 'NamedEvent'

        $NamedSet.Parameters.Name | Should -Contain 'Credential'
        $NamedSet.Parameters.Name | Should -Contain 'Authentication'
        $NamedSet.Parameters.Name | Should -Contain 'ContinueOnError'
        $NamedSet.Parameters.Name | Should -Contain 'SessionTimeoutMs'
        $NamedSet.Parameters.Name | Should -Contain 'BufferCapacity'
        $NamedSet.Parameters.Name | Should -Contain 'IncludeBookmark'
        $NamedSet.Parameters.Name | Should -Not -Contain 'NamedDataFilter'
        $Command.Parameters.MaxEventsScanned.ParameterType |
            Should -Be ([long])
    }

    It 'rejects credentials when any named-event target is local' {
        $SecurePassword = ConvertTo-SecureString 'not-used' -AsPlainText -Force
        $Credential = [pscredential]::new('reader', $SecurePassword)

        {
            Get-EVXEvent `
                -NamedEvent OSStartup `
                -Credential $Credential `
                -ErrorAction Stop
        } | Should -Throw '*every named-event target is a remote computer*'
        {
            Get-EVXEvent `
                -NamedEvent OSStartup `
                -MachineName $env:COMPUTERNAME, 'remote.contoso.test' `
                -Credential $Credential `
                -ErrorAction Stop
        } | Should -Throw '*every named-event target is a remote computer*'
    }


    It 'Returns ADUserLogon events when available' -Tag 'RequiresEvents' {
        $events = Get-EVXEvent -NamedEvent ADUserLogon -MaxEvents 1 -ErrorAction SilentlyContinue
        if ($events) {
            $events.Count | Should -BeGreaterThan 0
        } else {
            Write-Warning 'No ADUserLogon events found on this system.'
        }
    }

    It 'expands structured payload fields on named event projections' {
        $Plain = Get-EVXEvent -NamedEvent OSStartup -MaxEvents 1 -ErrorAction SilentlyContinue | Select-Object -First 1
        if (-not $Plain) {
            Set-ItResult -Skipped -Because 'No OSStartup event was available.'
            return
        }

        $Plain.SourceEvent.ReadMode | Should -Be ([EventViewerX.EventReadMode]::Full)
        $ExistingProperties = @($Plain.PSObject.Properties.Name)
        $PayloadKey = @($Plain.SourceEvent.Data.Keys | Where-Object { $_ -notin $ExistingProperties } | Select-Object -First 1)
        if (-not $PayloadKey) {
            Set-ItResult -Skipped -Because 'The OSStartup payload had no distinct field to expand.'
            return
        }

        $Expanded = Get-EVXEvent -NamedEvent OSStartup -MaxEvents 1 -ReadMode Full -ExpandData -ErrorAction Stop | Select-Object -First 1

        $Expanded.PSObject.Properties.Name | Should -Contain $PayloadKey[0]
        $Expanded.PSObject.BaseObject | Should -BeOfType $Plain.GetType()
    }

    It 'applies MessageRegex before the global named-event result limit' {
        $Types = @('OSStartup', 'OSShutdown', 'OSStartupSecurity')
        $Expected = @(Get-EVXEvent -NamedEvent $Types -MaxEvents 5 -ErrorAction SilentlyContinue)
        if ($Expected.Count -lt 2 -or @($Expected.SourceEvent.ContainerLog | Sort-Object -Unique).Count -lt 2) {
            Set-ItResult -Skipped -Because 'Two named-event logs were not available for a global-order comparison.'
            return
        }

        $Actual = @(Get-EVXEvent -NamedEvent $Types -MessageRegex '(?s).*' -MaxEvents 5 -ErrorAction Stop)
        $ExpectedKeys = @($Expected | ForEach-Object { '{0}|{1}|{2}' -f $_.SourceEvent.QueriedMachine, $_.SourceEvent.ContainerLog, $_.SourceEvent.RecordId })
        $ActualKeys = @($Actual | ForEach-Object { '{0}|{1}|{2}' -f $_.SourceEvent.QueriedMachine, $_.SourceEvent.ContainerLog, $_.SourceEvent.RecordId })

        ($ActualKeys -join "`n") | Should -Be ($ExpectedKeys -join "`n")
    }

    It 'applies an unlimited timeline scan to a globally merged output cap' {
        $Types = @(
            [EventViewerX.NamedEvents]::OSStartup,
            [EventViewerX.NamedEvents]::OSShutdown,
            [EventViewerX.NamedEvents]::OSStartupSecurity
        )
        $Expected = @(Get-EVXEvent -NamedEvent $Types -MaxEvents 5 -ErrorAction SilentlyContinue)
        if ($Expected.Count -lt 2 -or @($Expected.SourceEvent.ContainerLog | Sort-Object -Unique).Count -lt 2) {
            Set-ItResult -Skipped -Because 'Two named-event logs were not available for a global timeline comparison.'
            return
        }

        $Request = [EventViewerX.Reports.Correlation.NamedEventsTimelineQueryRequest]::new()
        $Request.NamedEvents = [EventViewerX.NamedEvents[]] $Types
        $Request.MaxEvents = 5
        $Request.MaxEventsScanned = 0
        $Request.IncludeUncorrelated = $true
        $Response = [EventViewerX.Reports.Correlation.NamedEventsTimelineQueryExecutor]::TryBuildAsync(
            $Request,
            [Threading.CancellationToken]::None).GetAwaiter().GetResult()

        $Response.Item2 | Should -BeNullOrEmpty
        $ExpectedKeys = @($Expected | ForEach-Object { '{0}|{1}|{2}' -f $_.SourceEvent.QueriedMachine, $_.SourceEvent.ContainerLog, $_.SourceEvent.RecordId } | Sort-Object)
        $ActualKeys = @($Response.Item1.Timeline | ForEach-Object { '{0}|{1}|{2}' -f $_.GatheredFrom, $_.GatheredLogName, $_.RecordId } | Sort-Object)
        ($ActualKeys -join "`n") | Should -Be ($ExpectedKeys -join "`n")
    }

    It 'reports an isolated remote named-event failure instead of silently returning partial results' {
        $Errors = @()
        $null = @(
            Get-EVXEvent `
                -NamedEvent OSStartup `
                -MachineName $env:COMPUTERNAME, '192.0.2.1' `
                -SessionTimeoutMs 500 `
                -MaxEvents 0 `
                -ErrorAction SilentlyContinue `
                -ErrorVariable +Errors
        )

        @($Errors.FullyQualifiedErrorId) |
            Should -Contain 'EVXNamedEventTargetFailed,PSEventViewer.CmdletGetEVXEvent'
    }
}
