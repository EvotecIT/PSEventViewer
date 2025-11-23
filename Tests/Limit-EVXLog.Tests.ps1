Describe 'Limit-EVXLog cmdlet' {
    BeforeAll {
        $script:log = 'EVXLimitTestLog'
        $script:provider = 'EVXLimitTestSource'
        $script:isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
        $script:skip = -not $script:isAdmin

        if (-not $script:skip) {
            Remove-EVXSource -SourceName $script:provider -LogName $script:log -ErrorAction SilentlyContinue
            Remove-EVXLog -LogName $script:log -ErrorAction SilentlyContinue
            New-EVXLog -LogName $script:log -ProviderName $script:provider -SourceLogName $script:log | Out-Null
        }
    }
    AfterAll {
        if (-not $script:skip) {
            Remove-EVXLog -LogName $script:log -ErrorAction SilentlyContinue
            Remove-EVXSource -SourceName $script:provider -LogName $script:log -ErrorAction SilentlyContinue
        }
    }
    It 'limits log settings' -Skip:$script:skip {
        $result = Limit-EVXLog -LogName $script:log -MaximumKilobytes 2048 -OverflowAction OverwriteOlder -RetentionDays 2 -SourceLogName $script:log
        if ($script:isAdmin) {
            $result | Should -Be $true
            $eventLog = New-Object System.Diagnostics.EventLog $script:log
            $eventLog.MaximumKilobytes | Should -Be 2048
            $eventLog.OverflowAction | Should -Be 'OverwriteOlder'
            $eventLog.MinimumRetentionDays | Should -Be 2
        }
        else {
            $result | Should -Be $false
        }
    }
    It 'supports overwrite as needed' -Skip:$script:skip {
        $result = Limit-EVXLog -LogName $script:log -MaximumKilobytes 4096 -OverflowAction OverwriteAsNeeded -SourceLogName $script:log
        if ($script:isAdmin) {
            $result | Should -Be $true
            $eventLog = New-Object System.Diagnostics.EventLog $script:log
            $eventLog.MaximumKilobytes | Should -Be 4096
            $eventLog.OverflowAction | Should -Be 'OverwriteAsNeeded'
        }
        else {
            $result | Should -Be $false
        }
    }
}
