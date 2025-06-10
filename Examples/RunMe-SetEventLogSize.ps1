Import-Module ..\PSEventViewer.psd1 -Force

# proper registry key
Get-PSRegistry -RegistryPath 'HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\EventLog\Application' | Format-List
# probably via gpo, but don't use
Get-PSRegistry -RegistryPath 'HKEY_LOCAL_MACHINE\\Software\\Policies\\Microsoft\\Windows\\EventLog\\Application' | Format-List
# optional, but usually not existing
Get-PSRegistry -RegistryPath 'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\WINEVT\Channels' | Format-Table -AutoSize

Get-EventsSettings -LogName 'Application' | Format-Table

Set-WinEventSettings -LogName 'Application' -MaximumSizeMB 15 -Mode Circular -WhatIf

Get-EventsSettings -LogName 'Application' | Format-Table