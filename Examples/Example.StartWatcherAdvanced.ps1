Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

# Start watcher for application errors
Start-EVXWatcher -MachineName $env:COMPUTERNAME -LogName 'Application' -EventId 1000 -Action {
    Write-Host "Application error: $($_.Message)"
}

# Start watcher for service crashes with additional stop conditions
Start-EVXWatcher -MachineName $env:COMPUTERNAME -LogName 'System' -EventId 7031 -StopAfter 5 -Until { $_.Id -eq 7031 } -Action {
    Write-Host "Service crashed: $($_.Properties[0].Value)"
}
