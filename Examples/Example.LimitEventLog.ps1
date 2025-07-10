Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

# Adjust log settings for the Application log using the built-in cmdlet
Limit-EventLog -LogName 'Application' -MaximumSize 10240 -OverflowAction OverwriteOlder -RetentionDays 7

# The EVX wrapper provides the same functionality and works on PowerShell Core
Limit-EVXLog -LogName 'Application' -MaximumKilobytes 10240 -OverflowAction OverwriteOlder -RetentionDays 7
