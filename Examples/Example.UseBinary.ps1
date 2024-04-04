Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force


$List = [PSEventViewer.SearchEvents]::GetProviders()

return
foreach ($L in $LIst) {
    if ($L.Events.Count -gt 0) {
        $L.Events | Format-Table
    break
    }

}