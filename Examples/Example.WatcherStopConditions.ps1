Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

# Start watcher directly with multiple stop options
Start-EVXWatcher -MachineName $env:COMPUTERNAME -LogName 'Application' -EventId 1000 -TimeoutSeconds 300 -StopAfter 2 -Until { $_.Source -eq 'Application Error' } -Action {
    Write-Host "Error event: $($_.Message)"
}
