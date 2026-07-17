Describe 'Get-EVXPowerShellScript bounded query contract' {
    It 'Exposes output, scan, and bounded cache controls' {
        $parameters = (Get-Command -Name Get-EVXPowerShellScript -ErrorAction Stop).Parameters

        $parameters.ContainsKey('MaxScripts') | Should -BeTrue
        $parameters.ContainsKey('MaxEventsScanned') | Should -BeTrue
        $parameters.ContainsKey('MaxPendingScripts') | Should -BeTrue
        $parameters.ContainsKey('MaxCachedEvents') | Should -BeTrue
        $parameters['MaxScripts'].Aliases | Should -Contain 'MaxEvents'
    }

    It 'Rejects an unbounded incomplete-script cache configuration' {
        { Get-EVXPowerShellScript -Type WindowsPowerShell -MaxPendingScripts 0 } | Should -Throw
    }
}
