Describe 'Clear-EVXLog cmdlet' {
    BeforeAll {
        $script:log = 'EVXClearTestLog'
        $script:provider = 'EVXClearTestSource'
        if ([System.Diagnostics.EventLog]::SourceExists($script:provider)) {
            [System.Diagnostics.EventLog]::DeleteEventSource($script:provider)
        }
        if ([System.Diagnostics.EventLog]::Exists($script:log)) {
            [System.Diagnostics.EventLog]::Delete($script:log)
        }
        New-EVXLog -LogName $script:log -ProviderName $script:provider | Out-Null
        Write-EVXEntry -LogName $script:log -ProviderName $script:provider -Message 'test' -EventId 1000
    }
    AfterAll {
        if ([System.Diagnostics.EventLog]::Exists($script:log)) {
            [System.Diagnostics.EventLog]::Delete($script:log)
        }
        if ([System.Diagnostics.EventLog]::SourceExists($script:provider)) {
            [System.Diagnostics.EventLog]::DeleteEventSource($script:provider)
        }
    }
    It 'clears the log and sets retention' {
        Clear-EVXLog -LogName $script:log -RetentionDays 2 | Should -Be $true
        $eventLog = New-Object System.Diagnostics.EventLog $script:log
        $eventLog.Entries.Count | Should -Be 0
        $eventLog.MinimumRetentionDays | Should -Be 2
    }
}
