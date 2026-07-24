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

    It 'does not register a missing source during an ordinary write' {
        $Source = "EVXMissing$([guid]::NewGuid().ToString('N'))"

        {
            Write-EVXEntry `
                -LogName Application `
                -ProviderName $Source `
                -Message 'No implicit registration' `
                -EventId 1000 `
                -ErrorAction Stop
        } | Should -Throw '*not registered*'
        [EventViewerX.ClassicEventLogManager]::SourceExists(
            $Source,
            'Application') | Should -BeFalse
    }

    It 'does not write or register under WhatIf' {
        $Source = "EVXWhatIf$([guid]::NewGuid().ToString('N'))"

        Write-EVXEntry `
            -LogName Application `
            -ProviderName $Source `
            -Message 'Preview only' `
            -EventId 1000 `
            -CreateSource `
            -WhatIf

        [EventViewerX.ClassicEventLogManager]::SourceExists(
            $Source,
            'Application') | Should -BeFalse
    }
}
