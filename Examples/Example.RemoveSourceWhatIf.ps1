Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

# Register a temporary source
Write-EVXEntry -LogName 'Application' -ProviderName 'TestWhatIfSource' -EventId 1 -Message 'Demo'

# Preview removal of the source without deleting it
Remove-EVXSource -SourceName 'TestWhatIfSource' -WhatIf
