function Get-EventsSettings {
    [cmdletBinding()]
    param(
        [string] $LogName,
        [string] $ComputerName,
        [int] $MaximumSize
    )
    $Log = Get-PSRegistry -RegistryPath "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\EventLog\$LogName" -ComputerName $ComputerName
    if ($Log.PSError -eq $true) {
        $Log = Get-PSRegistry -RegistryPath "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\WINEVT\Channels\$LogName" -ComputerName $ComputerName
        $PSRegistryPath = "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\WINEVT\Channels\$LogName"
    } else {
        $PSRegistryPath = "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\EventLog\$LogName"
    }


    if ($Log.AutoBackupLogFiles -eq 1 -and $Log.Retention -eq 4294967295) {
        $EventAction = 'ArchiveTheLogWhenFullDoNotOverwrite'
    } elseif ($Log.AutoBackupLogFiles -eq 0 -and $Log.Retention -eq 4294967295) {
        $EventAction = 'DoNotOverwriteEventsClearLogManually'
    } else {
        $EventAction = 'OverwriteEventsAsNeededOldestFirst'
    }
    if ($Log.RestrictGuestAccess -eq 1) {
        $RestrictGuestAccess = $true
    } else {
        $RestrictGuestAccess = $false
    }
    #if ($Log.MaxSize) {
    $MaxSizeMB = Convert-Size -Value $Log.MaxSize -From Bytes -To MB -Precision 2
    #} else {
    #    $MaxSizeMB = Convert-Size -Value 1028 -From Bytes -To MB -Precision 2
    #}

    [PSCustomObject] @{
        PSError             = $Log.PSError
        PSRegistryPath      = $PSRegistryPath
        MaxSizeMB           = $MaxSizeMB
        EventAction         = $EventAction
        RestrictGuestAccess = $RestrictGuestAccess
    }
}