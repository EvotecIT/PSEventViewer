describe 'Remove-EVXSource cmdlet' {
    BeforeAll {
        # Register a temporary source for removal
        $script:source = 'TestEVXSource'
        Write-EVXEntry -LogName 'Application' -ProviderName $script:source -EventId 1 -Message 'test'
    }

    It 'removes existing source' {
        [System.Diagnostics.EventLog]::SourceExists($script:source) | Should -Be $true
        Remove-EVXSource -SourceName $script:source | Should -Be $true
        [System.Diagnostics.EventLog]::SourceExists($script:source) | Should -Be $false
    }
}
