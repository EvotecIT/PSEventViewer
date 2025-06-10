Describe 'Get-WinEventSettings cmdlet' {
    It 'Returns log information' {
        $result = Get-WinEventSettings -LogName 'Application'
        $result.LogName | Should -Be 'Application'
        $result.LogMode  | Should -Not -BeNullOrEmpty
        $result.MaximumSizeInBytes | Should -BeGreaterThan 0
    }
}
