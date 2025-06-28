Describe 'Get-EVXProviderList' {
    It 'Should return provider names' {
        $providers = Get-EVXProviderList
        $providers | Should -Not -BeNullOrEmpty
    }
}
