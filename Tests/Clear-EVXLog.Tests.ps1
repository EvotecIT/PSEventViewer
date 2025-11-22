Describe 'Clear-EVXLog cmdlet' {
    BeforeAll {
        $script:log = 'EVXClearTestLog'
        $script:provider = 'EVXClearTestSource'
        $script:isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
        $script:skip = -not $script:isAdmin

        if (-not $script:skip) {
            Remove-EVXSource -SourceName $script:provider -LogName $script:log -ErrorAction SilentlyContinue
            Remove-EVXLog -LogName $script:log -ErrorAction SilentlyContinue
            New-EVXLog -LogName $script:log -ProviderName $script:provider -SourceLogName $script:log | Out-Null
            Write-EVXEntry -LogName $script:log -ProviderName $script:provider -Message 'test' -EventId 1000
        }
    }
    AfterAll {
        if (-not $script:skip) {
            Remove-EVXLog -LogName $script:log -ErrorAction SilentlyContinue
            Remove-EVXSource -SourceName $script:provider -LogName $script:log -ErrorAction SilentlyContinue
        }
    }
    It 'clears the log and sets retention' -Skip:$script:skip {
        $result = Clear-EVXLog -LogName $script:log -RetentionDays 2
        if ($script:isAdmin) {
            $result | Should -Be $true
            $eventLog = New-Object System.Diagnostics.EventLog $script:log
            $eventLog.Entries.Count | Should -Be 0
            $eventLog.MinimumRetentionDays | Should -Be 2
        }
        else {
            $result | Should -Be $false
        }
    }
}
