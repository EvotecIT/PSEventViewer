Import-Module $PSScriptRoot/../PSEventViewer.psd1 -Force

$action = {
    param($Event)
    Write-Host "Found event $($Event.Id) on $($Event.Computer)"
}

$watcher = Start-EVXWatcher -Name 'BasicWatcher' -MachineName $env:COMPUTERNAME -LogName 'Security' -EventId 4624,4625 -Action $action

# Wait for events to arrive
Start-Sleep -Seconds 30

Get-EVXWatcher -Id $watcher.Id | Format-List

Stop-EVXWatcher -Id $watcher.Id
