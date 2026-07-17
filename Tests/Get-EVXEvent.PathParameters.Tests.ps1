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
}
