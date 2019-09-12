$Script:ScriptBlockEventsInformation = {
    Param (
        [string]$Computer,
        [string]$Path,
        [string]$LogName,
        [bool] $Verbose
    )
    if ($Verbose) {
        $VerbosePreference = 'continue'
    }
    function Convert-Size {
        # Original - https://techibee.com/powershell/convert-from-any-to-any-bytes-kb-mb-gb-tb-using-powershell/2376
        #
        # Changelog - Modified 30.03.2018 - przemyslaw.klys at evotec.pl
        # - Added $Display Switch
        [cmdletbinding()]
        param(
            [validateset("Bytes", "KB", "MB", "GB", "TB")]
            [string]$From,
            [validateset("Bytes", "KB", "MB", "GB", "TB")]
            [string]$To,
            [Parameter(Mandatory = $true)]
            [double]$Value,
            [int]$Precision = 4,
            [switch]$Display
        )
        switch ($From) {
            "Bytes" { $value = $Value }
            "KB" { $value = $Value * 1024 }
            "MB" { $value = $Value * 1024 * 1024 }
            "GB" { $value = $Value * 1024 * 1024 * 1024 }
            "TB" { $value = $Value * 1024 * 1024 * 1024 * 1024 }
        }

        switch ($To) {
            "Bytes" { return $value }
            "KB" { $Value = $Value / 1KB }
            "MB" { $Value = $Value / 1MB }
            "GB" { $Value = $Value / 1GB }
            "TB" { $Value = $Value / 1TB }

        }
        if ($Display) {
            return "$([Math]::Round($value,$Precision,[MidPointRounding]::AwayFromZero)) $To"
        } else {
            return [Math]::Round($value, $Precision, [MidPointRounding]::AwayFromZero)
        }

    }

    try {
        if ($Computer -eq '') {

            $FileInformation = Get-ChildItem -File $Path
            $EventsOldest = Get-WinEvent -MaxEvents 1 -Oldest -Path $Path -Verbose:$false
            $EventsNewest = Get-WinEvent -MaxEvents 1 -Path $Path -Verbose:$false

            $RecordCount = $EventsNewest.RecordID - $EventsOldest.RecordID

            $EventsInfo = [PSCustomObject]@{
                EventNewest                        = $EventsNewest.TimeCreated
                EventOldest                        = $EventsOldest.TimeCreated
                FileSize                           = $FileInformation.Length
                FileSizeMaximum                    = $null
                FileSizeCurrentMB                  = Convert-Size -Value $FileInformation.Length -From Bytes -To MB -Precision 2 #-Display
                FileSizeMaximumMB                  = Convert-Size -Value $FileInformation.Length -From Bytes -To MB -Precision 2 #-Display
                IsClassicLog                       = $false
                IsEnabled                          = $false
                IsLogFull                          = $false
                LastAccessTime                     = $FileInformation.LastAccessTime
                LastWriteTime                      = $FileInformation.LastWriteTime
                LogFilePath                        = $Path
                LogIsolation                       = $false
                LogMode                            = 'N/A'
                LogName                            = 'N/A'
                LogType                            = 'N/A'
                MaximumSizeInBytes                 = $FileInformation.Length
                MachineName                        = (@($EventsOldest.MachineName) + @($EventsNewest.MachineName) | Sort-Object -Unique) -join ', '
                OldestRecordNumber                 = $EventsOldest.RecordID
                OwningProviderName                 = ''
                ProviderBufferSize                 = 0
                ProviderControlGuid                = ''
                ProviderKeywords                   = ''
                ProviderLatency                    = 1000
                ProviderLevel                      = ''
                ProviderMaximumNumberOfBuffers     = 16
                ProviderMinimumNumberOfBuffers     = 0
                ProviderNames                      = ''
                ProviderNamesExpanded              = ''
                RecordCount                        = $RecordCount
                SecurityDescriptor                 = $null
                SecurityDescriptorOwner            = $null
                SecurityDescriptorGroup            = $null
                SecurityDescriptorDiscretionaryAcl = $null
                SecurityDescriptorSystemAcl        = $null
                Source                             = 'File'
            }

        } else {
            $EventsInfo = Get-WinEvent -ListLog $LogName -ComputerName $Computer

            $FileSizeCurrentMB = Convert-Size -Value $EventsInfo.FileSize -From Bytes -To MB -Precision 2 #-Display
            $FileSizeMaximumMB = Convert-Size -Value $EventsInfo.MaximumSizeInBytes -From Bytes -To MB -Precision 2 #-Display
            $EventOldest = (Get-WinEvent -MaxEvents 1 -LogName $LogName -Oldest -ComputerName $Computer).TimeCreated
            $EventNewest = (Get-WinEvent -MaxEvents 1 -LogName $LogName -ComputerName $Computer).TimeCreated
            $ProviderNamesExpanded = $EventsInfo.ProviderNames -join ', '

            $SecurityDescriptorTranslated = ConvertFrom-SddlString -Sddl $EventsInfo.SecurityDescriptor

            Add-Member -InputObject $EventsInfo -MemberType NoteProperty -Name "FileSizeCurrentMB" -Value $FileSizeCurrentMB -Force
            Add-Member -InputObject $EventsInfo -MemberType NoteProperty -Name "FileSizeMaximumMB" -Value $FileSizeMaximumMB -Force
            Add-Member -InputObject $EventsInfo -MemberType NoteProperty -Name "EventOldest" -Value $EventOldest -Force
            Add-Member -InputObject $EventsInfo -MemberType NoteProperty -Name "EventNewest" -Value $EventNewest -Force
            Add-Member -InputObject $EventsInfo -MemberType NoteProperty -Name "ProviderNamesExpanded" -Value $ProviderNamesExpanded -Force
            Add-Member -InputObject $EventsInfo -MemberType NoteProperty -Name "MachineName" -Value $Computer -Force
            Add-Member -InputObject $EventsInfo -MemberType NoteProperty -Name "Source" -Value $Computer -Force
            Add-Member -InputObject $EventsInfo -MemberType NoteProperty -Name 'SecurityDescriptorOwner' -Value $SecurityDescriptorTranslated.Owner -Force
            Add-Member -InputObject $EventsInfo -MemberType NoteProperty -Name 'SecurityDescriptorGroup' -Value $SecurityDescriptorTranslated.Group -Force
            Add-Member -InputObject $EventsInfo -MemberType NoteProperty -Name 'SecurityDescriptorDiscretionaryAcl' -Value $SecurityDescriptorTranslated.DiscretionaryAcl -Force
            Add-Member -InputObject $EventsInfo -MemberType NoteProperty -Name 'SecurityDescriptorSystemAcl' -Value $SecurityDescriptorTranslated.SystemACL -Force
        }
    } catch {
        $ErrorMessage = $_.Exception.Message -replace "`n", " " -replace "`r", " "
        switch ($ErrorMessage) {
            { $_ -match 'No events were found' } {
                Write-Verbose -Message "$Computer Reading Event Log ($LogName) size failed. No events found."
            }
            { $_ -match 'Attempted to perform an unauthorized operation' } {
                Write-Verbose -Message "$Computer Reading Event Log ($LogName) size failed. Unauthorized operation."
                Write-Error -Message "$Computer`: $_"
            }
            default {
                Write-Verbose -Message "$Computer Reading Event Log ($LogName) size failed. Error occured: $ErrorMessage"
                Write-Error -Message "$Computer`: $_"
            }
        }
    }
    $Properties = $EventsInfo.PSObject.Properties.Name | Sort-Object
    $EventsInfo | Select-Object $Properties
}