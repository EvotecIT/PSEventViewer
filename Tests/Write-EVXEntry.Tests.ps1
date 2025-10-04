Describe 'Write-EVXEntry cmdlet' {
    It 'throws terminating error with -ErrorAction Stop when write fails' {
        { Write-EVXEntry -LogName 'Application' -ProviderName 'TestProvider' -Message 'Test message' -EventId 1000 -Category 40000 -ErrorAction Stop } | Should -Throw
    }
}
