Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

try {
    Write-EVXEntry -LogName 'Application' -ProviderName 'TestProvider' -Message 'Test message' -EventId 1000 -Category 40000 -ErrorAction Stop
} catch {
    Write-Warning -Message "Write-EVXEntry failed: $($_.Exception.Message)"
}
