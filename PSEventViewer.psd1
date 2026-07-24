@{
    AliasesToExport      = @('Find-WinEvent')
    Author               = 'Przemyslaw Klys'
    CmdletsToExport      = @('Clear-EVXLog', 'ConvertTo-EVXProviderDefinition', 'Export-EVXEvent', 'Get-EVXCollectorSubscription', 'Get-EVXEvent', 'Get-EVXEventStatistics', 'Get-EVXFilter', 'Get-EVXLog', 'Get-EVXPowerShellScript', 'Get-EVXPowerShellScriptExecution', 'Get-EVXProvider', 'Get-EVXProviderPackage', 'Get-EVXWatcher', 'Install-EVXProviderPackage', 'New-EVXLog', 'New-EVXProviderPackage', 'New-EVXSource', 'Remove-EVXLog', 'Remove-EVXSource', 'Reset-EVXEventCheckpoint', 'Set-EVXCollectorSubscription', 'Set-EVXLog', 'Start-EVXWatcher', 'Stop-EVXWatcher', 'Test-EVXLog', 'Test-EVXProviderDefinition', 'Uninstall-EVXProviderPackage', 'Update-EVXLogArchive', 'Write-EVXEntry', 'Write-EVXEvent')
    CompanyName          = 'Evotec'
    CompatiblePSEditions = @('Desktop', 'Core')
    Copyright            = '(c) 2011 - 2026 Przemyslaw Klys @ Evotec. All rights reserved.'
    Description          = 'High-performance Windows Event Log queries, streaming exports, subscriptions, diagnostics, and administration for PowerShell.'
    FunctionsToExport    = @()
    GUID                 = '5df72a79-cdf6-4add-b38d-bcacf26fb7bc'
    ModuleVersion        = '4.0.0'
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
