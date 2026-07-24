Describe 'New-EVXLog cmdlet' {
    BeforeAll {
        $script:log = 'EVXTestLog'
        $script:provider = 'EVXTestSource'
        $script:isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
        $script:skip = -not $script:isAdmin

        if (-not $script:skip) {
            Remove-EVXSource -SourceName $script:provider -LogName $script:log -ErrorAction SilentlyContinue
            Remove-EVXLog -LogName $script:log -ErrorAction SilentlyContinue
        }
    }
    AfterAll {
        if (-not $script:skip) {
            Remove-EVXSource -SourceName $script:provider -LogName $script:log -ErrorAction SilentlyContinue
            Remove-EVXLog -LogName $script:log -ErrorAction SilentlyContinue
        }
    }
    It 'creates new log with provider' -Skip:$script:skip {
        $result = New-EVXLog -LogName $script:log -ProviderName $script:provider -MaximumKilobytes 1024 -OverflowAction OverwriteAsNeeded
        if ($script:isAdmin) {
            $result.CreatedLog | Should -BeTrue
            $result.CreatedSource | Should -BeTrue
            $result.After.LogExists | Should -BeTrue
            $result.After.SourceExists | Should -BeTrue
            [System.Diagnostics.EventLog]::Exists($script:log) | Should -Be $true
            $info = Get-EVXLog -LogName $script:log
            $info.LogName | Should -Be $script:log
            Remove-EVXLog -LogName $script:log | Should -Be $true
            [System.Diagnostics.EventLog]::Exists($script:log) | Should -Be $false
        }
        else {
            $result | Should -Be $false
        }
    }
}
