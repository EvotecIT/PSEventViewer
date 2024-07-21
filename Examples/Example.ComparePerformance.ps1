Clear-Host
Import-Module .\PSEventViewer.psd1 -Force

Measure-Command {
    Find-WinEvent -LogName 'Security' -EventId 5136, 5137, 5168 -Verbose -MachineName 'AD1' -ParallelOption Disabled
}
Measure-Command {
    Get-WinEvent -FilterHashtable @{
        LogName = 'Security'
        ID      = 5136, 5137, 5168
    } -ComputerName AD1
}

Measure-Command {
    Find-WinEvent -LogName 'Security' -EventId 5136, 5137, 5168 -Verbose -MachineName 'AD1', 'AD2', 'AD0' -ParallelOption Parallel
}
Measure-Command {
    foreach ($Machine in 'AD1', 'AD2', 'AD0') {
        Get-WinEvent -FilterHashtable @{
            LogName = 'Security'
            ID      = 5136, 5137, 5168
        } -ComputerName $Machine
    }
}