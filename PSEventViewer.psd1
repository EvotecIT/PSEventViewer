@{
    AliasesToExport      = @('Clear-EventViewerXLog', 'Clear-WinEventLog', 'Find-WinEvent', 'Get-Events', 'Get-EventsFilter', 'Get-EventsInformation', 'Get-EventsSettings', 'Get-EventViewerXEvent', 'Get-EventViewerXFilter', 'Get-EventViewerXInfo', 'Get-EventViewerXLog', 'Get-EventViewerXProviderList', 'Get-EventViewerXWatcher', 'Get-PowerShellScriptExecution', 'Get-WinEventFilter', 'Get-WinEventLog', 'Limit-EventViewerXLog', 'Limit-EVXLog', 'Limit-WinEventLog', 'New-EventViewerXLog', 'New-WinEventLog', 'Remove-EventViewerXLog', 'Remove-EventViewerXSource', 'Remove-WinEventLog', 'Remove-WinEventSource', 'Restore-EVXPowerShellScript', 'Restore-PowerShellScript', 'Set-EventsInformation', 'Set-EventsSettings', 'Set-EventViewerXInfo', 'Start-EventViewerXWatcher', 'Start-EventWatching', 'Stop-EventViewerXWatcher', 'Write-Event', 'Write-EventViewerXEntry', 'Write-WinEvent')
    Author               = 'Przemyslaw Klys'
    CmdletsToExport      = @('Clear-EVXLog', 'Get-EVXEvent', 'Get-EVXFilter', 'Get-EVXInfo', 'Get-EVXLog', 'Get-EVXPowerShellScript', 'Get-EVXProviderList', 'Get-EVXWatcher', 'New-EVXLog', 'Remove-EVXLog', 'Remove-EVXSource', 'Set-EVXInfo', 'Set-EVXLogLimit', 'Start-EVXWatcher', 'Stop-EVXWatcher', 'Write-EVXEntry')
    CompanyName          = 'Evotec'
    CompatiblePSEditions = @('Desktop', 'Core')
    Copyright            = '(c) 2011 - 2026 Przemyslaw Klys @ Evotec. All rights reserved.'
    Description          = 'Simple module allowing parsing of event logs. Has its own quirks...'
    FunctionsToExport    = @()
    GUID                 = '5df72a79-cdf6-4add-b38d-bcacf26fb7bc'
    ModuleVersion        = '3.4.0'
    PowerShellVersion    = '5.1'
    PrivateData          = @{
        PSData = @{
            ExternalModuleDependencies = @()
            IconUri                    = 'https://evotec.xyz/wp-content/uploads/2018/10/PSEventViewer.png'
            ProjectUri                 = 'https://github.com/EvotecIT/PSEventViewer'
            RequireLicenseAcceptance   = $false
            Tags                       = @('Events', 'Viewer', 'Windows', 'XML', 'XPATH', 'EVTX')
        }
    }
    RequiredModules      = @()
    RootModule           = 'PSEventViewer.psm1'
    ScriptsToProcess     = @()
}