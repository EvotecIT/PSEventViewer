Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

Start-EVXWatcher -MachineName $env:COMPUTERNAME -LogName 'Application' -EventId 1 -TimeoutSeconds 30 -StopOnMatch -Action {
    Write-Host "Application event triggered: $($_.Id)"
}
