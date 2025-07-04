Import-Module $PSScriptRoot/../PSEventViewer.psd1 -Force

$action = {
    param($Event)
    Write-Host "Timeout watcher saw $($Event.Id)"
}

Start-EVXWatcher -Name 'TimeoutWatcher' -MachineName $env:COMPUTERNAME -LogName 'Application' -EventId 1000 -Action $action -TimeOut (New-TimeSpan -Seconds 30)

Start-Sleep -Seconds 40

Get-EVXWatcher
