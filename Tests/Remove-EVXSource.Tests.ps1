describe 'Remove-EVXSource cmdlet' {
    BeforeAll {
        $script:source = 'TestEVXSource'
    }

    BeforeEach {
        if (-not [System.Diagnostics.EventLog]::SourceExists($script:source)) {
            Write-EVXEntry -LogName 'Application' -ProviderName $script:source -EventId 1 -Message 'test'
        }
    }

    AfterAll {
        if ([System.Diagnostics.EventLog]::SourceExists($script:source)) {
            [System.Diagnostics.EventLog]::DeleteEventSource($script:source)
        }
    }

    It 'removes existing source' {
        [System.Diagnostics.EventLog]::SourceExists($script:source) | Should -Be $true
        Remove-EVXSource -SourceName $script:source | Should -Be $true
        [System.Diagnostics.EventLog]::SourceExists($script:source) | Should -Be $false
    }

    It 'returns false when using -WhatIf' {
        [System.Diagnostics.EventLog]::SourceExists($script:source) | Should -Be $true
        Remove-EVXSource -SourceName $script:source -WhatIf | Should -Be $false
        [System.Diagnostics.EventLog]::SourceExists($script:source) | Should -Be $true
    }
}
