Describe 'Write-EVXEntry cmdlet' {
    It 'throws terminating error with -ErrorAction Stop when write fails' {
        { Write-EVXEntry -LogName 'Application' -ProviderName 'TestProvider' -Message 'Test message' -EventId 1000 -Category 40000 -ErrorAction Stop } | Should -Throw
    }

    It 'writes a nonterminating ErrorRecord that honors ErrorVariable' {
        $CapturedErrors = @()
        Write-EVXEntry -LogName 'Application' -ProviderName 'TestProvider' -Message 'Test message' -EventId 1000 -Category 40000 -ErrorAction SilentlyContinue -ErrorVariable +CapturedErrors

        $CapturedErrors.Count | Should -Be 1
        $CapturedErrors[0].FullyQualifiedErrorId | Should -Match 'WriteEventFailed'
    }
}
