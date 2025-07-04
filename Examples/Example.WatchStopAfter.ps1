Import-Module $PSScriptRoot/../PSEventViewer.psd1 -Force

$action = {
    param($Event)
    Write-Host "Event $($Event.Id) captured"
}

Start-EVXWatcher -Name 'StopAfterWatcher' -MachineName $env:COMPUTERNAME -LogName 'Security' -EventId 4625 -Action $action -StopAfter 2

# Wait for watcher to stop itself after two events
Start-Sleep -Seconds 60

Get-EVXWatcher
