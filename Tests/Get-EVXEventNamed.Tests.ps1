Describe 'Get-EVXEvent - Named Event' {
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
}
