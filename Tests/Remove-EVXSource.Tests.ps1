describe 'Remove-EVXSource cmdlet' {
    $source = 'TestEVXSource'

    BeforeAll {
        Write-EVXEntry -LogName 'Application' -ProviderName $source -EventId 1 -Message 'test'
    }

    It 'removes existing source' {
        [System.Diagnostics.EventLog]::SourceExists($source) | Should -Be $true
        Remove-EVXSource -SourceName $source | Should -Be $true
        [System.Diagnostics.EventLog]::SourceExists($source) | Should -Be $false
    }
}
