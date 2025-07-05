Describe 'New-EVXLog cmdlet' {
    BeforeAll {
        $script:log = 'EVXTestLog'
        $script:provider = 'EVXTestSource'
        if ([System.Diagnostics.EventLog]::SourceExists($script:provider)) {
            [System.Diagnostics.EventLog]::DeleteEventSource($script:provider)
        }
        if ([System.Diagnostics.EventLog]::Exists($script:log)) {
            [System.Diagnostics.EventLog]::Delete($script:log)
        }
    }
    AfterAll {
        if ([System.Diagnostics.EventLog]::SourceExists($script:provider)) {
            [System.Diagnostics.EventLog]::DeleteEventSource($script:provider)
        }
        if ([System.Diagnostics.EventLog]::Exists($script:log)) {
            [System.Diagnostics.EventLog]::Delete($script:log)
        }
    }
    It 'creates new log with provider' {
        New-EVXLog -LogName $script:log -ProviderName $script:provider -MaximumKilobytes 1024 -OverflowAction OverwriteAsNeeded | Should -Be $true
        [System.Diagnostics.EventLog]::Exists($script:log) | Should -Be $true
        $info = Get-EVXLog -LogName $script:log
        $info.LogName | Should -Be $script:log
        Remove-EVXLog -LogName $script:log | Should -Be $true
        [System.Diagnostics.EventLog]::Exists($script:log) | Should -Be $false
    }
}
