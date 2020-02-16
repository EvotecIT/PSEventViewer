function Set-EventsSettings {
    [cmdletBinding()]
    param(
        [string] $LogName,
        [string] $ComputerName,
        [int] $MaximumSizeMB,
        [ValidateSet('OverwriteEventsAsNeededOldestFirst', 'ArchiveTheLogWhenFullDoNotOverwrite', 'DoNotOverwriteEventsClearLogManually', 'None')][string] $EventAction
    )
    if ($MaximumSizeMB) {
        $MaxSize = $MaximumSizeMB * 1MB
        $Log = Get-EventsSettings -LogName $LogName
        if ($Log.PSError -eq $false) {
            if ($MaximumSizeMB -ne 0) {
                Set-PSRegistry -RegistryPath $Log.PSRegistryPath -ComputerName $ComputerName -Key 'MaxSize' -Value $MaxSize -Type REG_DWORD
            }
            if ($EventAction) {
                if ($EventAction -eq 'ArchiveTheLogWhenFullDoNotOverwrite') {
                    Set-PSRegistry -RegistryPath $Log.PSRegistryPath -ComputerName $ComputerName -Key 'AutoBackupLogFiles' -Value 1 -Type REG_DWORD
                    Set-PSRegistry -RegistryPath $Log.PSRegistryPath -ComputerName $ComputerName -Key 'Retention' -Value 4294967295 -Type REG_DWORD
                } elseif ($EventAction -eq 'DoNotOverwriteEventsClearLogManually') {
                    Set-PSRegistry -RegistryPath $Log.PSRegistryPath -ComputerName $ComputerName -Key 'AutoBackupLogFiles' -Value 0 -Type REG_DWORD
                    Set-PSRegistry -RegistryPath $Log.PSRegistryPath -ComputerName $ComputerName -Key 'Retention' -Value 4294967295 -Type REG_DWORD
                } elseif ( $EventAction -eq 'OverwriteEventsAsNeededOldestFirst') {
                    Set-PSRegistry -RegistryPath $Log.PSRegistryPath -ComputerName $ComputerName -Key 'AutoBackupLogFiles' -Value 0 -Type REG_DWORD
                    Set-PSRegistry -RegistryPath $Log.PSRegistryPath -ComputerName $ComputerName -Key 'Retention' -Value 0 -Type REG_DWORD
                } else {
                    # No choice
                }

            }
        }
    }
}