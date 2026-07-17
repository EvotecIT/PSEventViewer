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

    It 'Exposes execution records through a dedicated cmdlet and compatibility alias' {
        $command = Get-Command -Name Get-EVXPowerShellScriptExecution -ErrorAction Stop

        $command.Parameters.ContainsKey('MaxEvents') | Should -BeTrue
        $command.Parameters.ContainsKey('MaxEventsScanned') | Should -BeTrue
        $command.Parameters.ContainsKey('IncludeQueryInfo') | Should -BeTrue
        (Get-Alias -Name Get-PowerShellScriptExecution -ErrorAction Stop).ResolvedCommandName |
            Should -Be 'Get-EVXPowerShellScriptExecution'
        (Get-Alias -Name Restore-PowerShellScript -ErrorAction Stop).ResolvedCommandName |
            Should -Be 'Get-EVXPowerShellScript'
    }
}
