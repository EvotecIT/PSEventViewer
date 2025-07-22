Describe 'Set-EVXInfo cmdlet' {
    BeforeAll {
        $script:log = 'EVXInfoTestLog'
        $script:provider = 'EVXInfoTestSource'
        if ([System.Diagnostics.EventLog]::SourceExists($script:provider)) {
            [System.Diagnostics.EventLog]::DeleteEventSource($script:provider)
        }
        if ([System.Diagnostics.EventLog]::Exists($script:log)) {
            [System.Diagnostics.EventLog]::Delete($script:log)
        }
        New-EVXLog -LogName $script:log -ProviderName $script:provider | Out-Null
    }
    AfterAll {
        if ([System.Diagnostics.EventLog]::Exists($script:log)) {
            [System.Diagnostics.EventLog]::Delete($script:log)
        }
        if ([System.Diagnostics.EventLog]::SourceExists($script:provider)) {
            [System.Diagnostics.EventLog]::DeleteEventSource($script:provider)
        }
    }
    It 'tolerates null optional parameters' {
        { Set-EVXInfo -LogName $script:log -MaximumSizeMB $null -MaximumSizeInBytes $null -EventAction $null -Mode $null -ComputerName $null } | Should -Not -Throw
    }
}
