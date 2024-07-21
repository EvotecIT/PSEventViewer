Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

#$Events = Find-WinEvent -LogName 'Application' -Verbose -ParallelOption Parallel -EventID 1035,1042 -MaxEvents 2
#$Events | Format-Table

$Events = Find-WinEvent -LogName 'Application' -Verbose -ParallelOption Parallel -EventID 1035,258 -MaxEvents 10
$Events | Format-Table
<#
$Events = Find-WinEvent -LogName 'Application' -Verbose -ParallelOption Parallel -ProviderName "MsiInstaller" -MaxEvents 2
$Events | Format-Table
#>
$Events.Data | Format-Table
