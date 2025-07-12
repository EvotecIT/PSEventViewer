Import-Module $PSScriptRoot/../PSEventViewer.psd1 -Force

$action = {
    param($Event)
    Write-Host "Advanced watcher saw event $($Event.Id)" -ForegroundColor Cyan
}

Start-EVXWatcher -Name 'AdvancedWatcher' -MachineName $env:COMPUTERNAME -LogName 'Application' -EventId 1000 -Action $action -Staging -StopOnMatch -TimeOut (New-TimeSpan -Minutes 1)

Start-Sleep -Seconds 65

Get-EVXWatcher -Name 'AdvancedWatcher'

