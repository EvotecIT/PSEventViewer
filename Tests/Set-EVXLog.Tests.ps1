Describe 'Set-EVXLog channel policy' {
    BeforeAll {
        $suffix = [Guid]::NewGuid().ToString('N')
        $script:log = 'EVX' + $suffix + 'Limit'
        $script:provider = 'EVXLimitSource' + $suffix
        $script:isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
        $script:skip = -not $script:isAdmin

        if (-not $script:skip) {
            Remove-EVXSource -SourceName $script:provider -LogName $script:log -ErrorAction SilentlyContinue
            Remove-EVXLog -LogName $script:log -ErrorAction SilentlyContinue
            New-EVXLog -LogName $script:log -ProviderName $script:provider | Out-Null
        }
    }
    AfterAll {
        if (-not $script:skip) {
            Remove-EVXLog -LogName $script:log -ErrorAction SilentlyContinue
            Remove-EVXSource -SourceName $script:provider -LogName $script:log -ErrorAction SilentlyContinue
        }
    }
    It 'applies size and retention mode through one typed result' -Skip:$script:skip {
        $result = Set-EVXLog -LogName $script:log -MaximumSizeMB 2 -Mode Retain
        $result.Success | Should -BeTrue

        $details = Get-EVXLog -LogName $script:log
        $details.MaximumSizeInBytes | Should -Be (2MB)
        $details.LogMode | Should -Be 'Retain'
    }
}
