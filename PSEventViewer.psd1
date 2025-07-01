@{
    AliasesToExport      = @('Get-EventViewerXEvent', 'Find-WinEvent', 'Get-Events', 'Get-EventViewerXFilter', 'Get-WinEventFilter', 'Get-EventsFilter', 'Get-EventViewerXInfo', 'Get-EventsSettings', 'Get-EventsInformation', 'Restore-EVXPowerShellScript', 'Get-PowerShellScriptExecution', 'Restore-PowerShellScript', 'Get-EventViewerXProviderList', 'Remove-EventViewerXSource', 'Remove-WinEventSource', 'Set-EventViewerXInfo', 'Set-EventsInformation', 'Set-EventsSettings', 'Start-EventViewerXWatcher', 'Start-EventWatching', 'Write-EventViewerXEntry', 'Write-WinEvent')
    Author               = 'Przemyslaw Klys'
    CmdletsToExport      = @('Get-EVXEvent', 'Get-EVXFilter', 'Get-EVXInfo', 'Get-EVXPowerShellScript', 'Get-EVXProviderList', 'Remove-EVXSource', 'Set-EVXInfo', 'Start-EVXWatcher', 'Write-EVXEntry')
    CompanyName          = 'Evotec'
    CompatiblePSEditions = @('Desktop', 'Core')
    Copyright            = '(c) 2011 - 2025 Przemyslaw Klys @ Evotec. All rights reserved.'
    Description          = 'Simple module allowing parsing of event logs. Has its own quirks...'
    FunctionsToExport    = @()
    GUID                 = '5df72a79-cdf6-4add-b38d-bcacf26fb7bc'
    ModuleVersion        = '3.0.0'
    PowerShellVersion    = '5.1'
    PrivateData          = @{
        PSData = @{
            ExternalModuleDependencies = @('Microsoft.PowerShell.Utility', 'Microsoft.PowerShell.Management', 'Microsoft.PowerShell.Diagnostics')
            IconUri                    = 'https://evotec.xyz/wp-content/uploads/2018/10/PSEventViewer.png'
            ProjectUri                 = 'https://github.com/EvotecIT/PSEventViewer'
            Tags                       = @('Events', 'Viewer', 'Windows', 'XML', 'XPATH', 'EVTX')
        }
    }
    RequiredModules      = @(@{
            Guid          = 'ee272aa8-baaa-4edf-9f45-b6d6f7d844fe'
            ModuleName    = 'PSSharedGoods'
            ModuleVersion = '0.0.309'
        }, 'Microsoft.PowerShell.Utility', 'Microsoft.PowerShell.Management', 'Microsoft.PowerShell.Diagnostics')
    RootModule           = 'PSEventViewer.psm1'
}