Import-Module $PSScriptRoot/../PSEventViewer.psd1 -Force

$includeFilter = @{
    SubjectUserName = 'user1','user2'
    TargetUserName  = 'server$'
}

$excludeFilter = @{
    ProcessName = 'cmd.exe','powershell.exe'
}

Get-EVXEvent -LogName 'Security' -NamedDataFilter $includeFilter -NamedDataExcludeFilter $excludeFilter -Oldest -MaxEvents 10 |
    Format-Table TimeCreated, Id, SubjectUserName, TargetUserName, ProcessName

