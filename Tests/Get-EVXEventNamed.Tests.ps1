Describe 'Get-EVXEvent - Named Event' {
    It 'exposes NamedEvents as the documented alias for Type' {
        (Get-Command Get-EVXEvent).Parameters['Type'].Aliases | Should -Contain 'NamedEvents'
    }

    It 'Returns ADUserLogon events when available' -Tag 'RequiresEvents' {
        $events = Get-EVXEvent -Type ADUserLogon -MaxEvents 1 -ErrorAction SilentlyContinue
        if ($events) {
            $events.Count | Should -BeGreaterThan 0
        } else {
            Write-Warning 'No ADUserLogon events found on this system.'
        }
    }

    It 'expands structured payload fields on named event projections' {
        $Plain = Get-EVXEvent -Type OSStartup -MaxEvents 1 -ErrorAction SilentlyContinue | Select-Object -First 1
        if (-not $Plain) {
            Set-ItResult -Skipped -Because 'No OSStartup event was available.'
            return
        }

        $ExistingProperties = @($Plain.PSObject.Properties.Name)
        $PayloadKey = @($Plain.Event.Data.Keys | Where-Object { $_ -notin $ExistingProperties } | Select-Object -First 1)
        if (-not $PayloadKey) {
            Set-ItResult -Skipped -Because 'The OSStartup payload had no distinct field to expand.'
            return
        }

        $Expanded = Get-EVXEvent -Type OSStartup -MaxEvents 1 -Expand -ErrorAction Stop | Select-Object -First 1

        $Expanded.PSObject.Properties.Name | Should -Contain $PayloadKey[0]
        $Expanded.PSObject.BaseObject | Should -BeOfType $Plain.GetType()
    }

    It 'applies MessageRegex before the global named-event result limit' {
        $Types = @('OSStartup', 'OSShutdown', 'OSStartupSecurity')
        $Expected = @(Get-EVXEvent -Type $Types -MaxEvents 5 -ErrorAction SilentlyContinue)
        if ($Expected.Count -lt 2 -or @($Expected.Event.ContainerLog | Sort-Object -Unique).Count -lt 2) {
            Set-ItResult -Skipped -Because 'Two named-event logs were not available for a global-order comparison.'
            return
        }

        $Actual = @(Get-EVXEvent -Type $Types -MessageRegex '(?s).*' -MaxEvents 5 -ErrorAction Stop)
        $ExpectedKeys = @($Expected | ForEach-Object { '{0}|{1}|{2}' -f $_.Event.QueriedMachine, $_.Event.ContainerLog, $_.Event.RecordId })
        $ActualKeys = @($Actual | ForEach-Object { '{0}|{1}|{2}' -f $_.Event.QueriedMachine, $_.Event.ContainerLog, $_.Event.RecordId })

        ($ActualKeys -join "`n") | Should -Be ($ExpectedKeys -join "`n")
    }
}
