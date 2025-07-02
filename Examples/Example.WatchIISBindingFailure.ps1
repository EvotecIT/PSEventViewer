Clear-Host
Import-Module $PSScriptRoot\..\PSEventViewer.psd1 -Force

$action = {
    param($Event)
    $site = $Event.GetValueFromDataDictionary('SiteName','Site')
    $binding = $Event.GetValueFromDataDictionary('BindingInfo','BindingInformation')
    if (-not $site -or -not $binding) {
        if ($Event.MessageSubject -match 'URL prefix\s+(?<binding>\S+).*?site\s+\"?(?<site>[^\"\s]+)') {
            if (-not $binding) { $binding = $Matches.binding }
            if (-not $site) { $site = $Matches.site }
        }
    }
    "Site: $site"
    "Binding: $binding"
}

Start-EVXWatcher -MachineName $env:COMPUTERNAME -LogName 'System' -EventId 1007 -Action $action
