# Example: Monitor password synchronization failures (event ID 611)
# Displays the number of affected users in the specified time range.

Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

$events = Get-EVXEvent -Machine $env:COMPUTERNAME -LogName 'Application' -ID 611 -Expand

$users = $events | ForEach-Object { $_.GetValueFromDataDictionary('User', 'AccountName') }
$uniqueUsers = $users | Where-Object { $_ } | Sort-Object -Unique
"Failed password sync user count: $($uniqueUsers.Count)"
