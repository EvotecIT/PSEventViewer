Import-Module PSPublishModule -Force -ErrorAction Stop

$Path = "$PSScriptRoot\..\EventViewerX"

Get-ProjectVersion -Path "$Path" -ExcludeFolders @("$Path\Module\Artefacts") | Format-Table
Set-ProjectVersion -Path "$Path" -NewVersion "3.2.0" -WhatIf:$false -Verbose -ExcludeFolders @("$Path\Module\Artefacts") | Format-Table
