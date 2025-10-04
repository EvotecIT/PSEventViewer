Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

# Write a test entry to Application log
Write-EVXEntry -LogName 'Application' -ProviderName 'TestProvider' -Message 'Sample message' -EventId 1000

# Clear the Application log and set retention to seven days
Clear-EVXLog -LogName 'Application' -RetentionDays 7
