Describe 'Set-EVXInfo warning message' {
    It 'Outputs Set-EVXInfo prefix in warnings' {
        $warning = $null
        Set-EVXInfo -LogName 'Nonexistent-Log-Name' -WarningVariable warning -ErrorAction SilentlyContinue
        $warning | Should -Match '^Set\xE2\x80\x91EVXInfo - Error occured'
    }
}
