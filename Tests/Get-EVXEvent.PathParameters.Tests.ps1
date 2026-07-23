Describe 'Get-EVXEvent -Path parameter contract' {
    BeforeAll {
        $PathParameters = (Get-Command Get-EVXEvent).ParameterSets |
            Where-Object Name -EQ 'PathEvents' |
            Select-Object -ExpandProperty Parameters |
            Select-Object -ExpandProperty Name
    }

    It 'exposes every filter forwarded to the EVTX query engine' {
        foreach ($Name in 'EventId', 'EventRecordId', 'ProviderName', 'Keywords', 'Level', 'StartTime', 'EndTime', 'TimePeriod', 'UserId') {
            $PathParameters | Should -Contain $Name
        }
    }

    It 'does not advertise live-query parallel switches that EVTX reads do not use' {
        $PathParameters | Should -Not -Contain 'ParallelOption'
        $PathParameters | Should -Not -Contain 'DisableParallel'
    }

    It 'supports explicit provider-message culture for offline, live, and named queries' {
        $PathParameters | Should -Contain 'MessageCulture'
        $GenericParameters = (Get-Command Get-EVXEvent).ParameterSets |
            Where-Object Name -EQ 'GenericEvents' |
            Select-Object -ExpandProperty Parameters |
            Select-Object -ExpandProperty Name
        $GenericParameters | Should -Contain 'MessageCulture'
        $NamedParameters = (Get-Command Get-EVXEvent).ParameterSets |
            Where-Object Name -EQ 'NamedEvents' |
            Select-Object -ExpandProperty Parameters |
            Select-Object -ExpandProperty Name
        $NamedParameters | Should -Contain 'MessageCulture'

        $Event = Get-EVXEvent `
            -LogName System `
            -MaxEvents 1 `
            -ReadMode Message `
            -MessageCulture en-US
        $Event.MessageCulture | Should -Be 'en-US'
        $Event.MessageRenderStatus.ToString() | Should -Not -Be 'NotRequested'

        $NamedEvent = Get-EVXEvent `
            -Type OSStartup `
            -MaxEvents 1 `
            -MessageCulture en-US `
            -ErrorAction Stop |
            Select-Object -First 1
        if ($null -eq $NamedEvent) {
            Set-ItResult -Skipped -Because 'No OSStartup event was available.'
        } else {
            $NamedEvent.Event.MessageCulture | Should -Be 'en-US'
            $NamedEvent.Event.MessageRenderStatus.ToString() | Should -Not -Be 'NotRequested'
        }
    }
}
