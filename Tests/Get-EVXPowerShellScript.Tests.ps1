Describe 'Get-EVXPowerShellScript bounded query contract' {
    It 'Exposes output, scan, and bounded cache controls' {
        $parameters = (Get-Command -Name Get-EVXPowerShellScript -ErrorAction Stop).Parameters

        $parameters.ContainsKey('MaxScripts') | Should -BeTrue
        $parameters.ContainsKey('MaxEventsScanned') | Should -BeTrue
        $parameters.ContainsKey('MaxPendingScripts') | Should -BeTrue
        $parameters.ContainsKey('MaxCachedEvents') | Should -BeTrue
        $parameters.ContainsKey('IncludeQueryInfo') | Should -BeTrue
        $parameters['MaxScripts'].Aliases | Should -Contain 'MaxEvents'
    }

    It 'Rejects an unbounded incomplete-script cache configuration' {
        { Get-EVXPowerShellScript -Type WindowsPowerShell -MaxPendingScripts 0 } | Should -Throw
    }

    It 'exposes execution records through a dedicated canonical cmdlet' {
        $command = Get-Command -Name Get-EVXPowerShellScriptExecution -ErrorAction Stop

        $command.Parameters.ContainsKey('MaxEvents') | Should -BeTrue
        $command.Parameters.ContainsKey('MaxEventsScanned') | Should -BeTrue
        $command.Parameters.ContainsKey('IncludeQueryInfo') | Should -BeTrue
    }
}
