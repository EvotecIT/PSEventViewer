Describe 'Limit-EVXLog cmdlet' {
    BeforeAll {
        $script:log = 'EVXLimitTestLog'
        $script:provider = 'EVXLimitTestSource'
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
    It 'limits log settings' {
        Limit-EVXLog -LogName $script:log -MaximumKilobytes 2048 -OverflowAction OverwriteOlder -RetentionDays 2 | Should -Be $true
        $eventLog = New-Object System.Diagnostics.EventLog $script:log
        $eventLog.MaximumKilobytes | Should -Be 2048
        $eventLog.OverflowAction | Should -Be 'OverwriteOlder'
        $eventLog.MinimumRetentionDays | Should -Be 2
    }
    It 'supports overwrite as needed' {
        Limit-EVXLog -LogName $script:log -MaximumKilobytes 4096 -OverflowAction OverwriteAsNeeded | Should -Be $true
        $eventLog = New-Object System.Diagnostics.EventLog $script:log
        $eventLog.MaximumKilobytes | Should -Be 4096
        $eventLog.OverflowAction | Should -Be 'OverwriteAsNeeded'
    }
}
