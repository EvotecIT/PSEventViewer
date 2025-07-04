Import-Module $PSScriptRoot/../PSEventViewer.psd1 -Force

$action = {
    param($Event)
    Write-Host "Named event $($Event.Type) detected"
}

$watcher = Start-EVXWatcher -Name 'NamedWatcher' -MachineName $env:COMPUTERNAME -LogName 'System' -NamedEvent OSCrash,OSBugCheck -Action $action

Start-Sleep -Seconds 30

Get-EVXWatcher -Name 'NamedWatcher'

Stop-EVXWatcher -Name 'NamedWatcher'
