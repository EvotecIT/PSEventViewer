function Get-EventsSettings {
    [cmdletBinding()]
    param(
        [parameter(Mandatory)][string] $LogName,
        [string] $ComputerName
    )
    try {
        if ($ComputerName) {
            $Log = Get-WinEvent -ListLog $LogName -ErrorAction Stop
        } else {
            $Log = Get-WinEvent -ListLog $LogName -ComputerName $ComputerName -ErrorAction Stop
        }
    } catch {
        if ($ErrorActionPreference -eq 'Stop') {
            throw
        } else {
            Write-Warning -Message "Get-EventsSettings - Error occured during reading of event log $LogName - $($_.Exception.Message)"
            return
        }
    }
    if ($Log.LogMode -eq 'AutoBackup') {
        $EventAction = 'ArchiveTheLogWhenFullDoNotOverwrite'
    } elseif ($Log.LogMode -eq 'Circular') {
        $EventAction = 'OverwriteEventsAsNeededOldestFirst'
    } elseif ($Log.LogMode -eq 'Retain') {
        $EventAction = 'DoNotOverwriteEventsClearLogManually'
    } else {
        $EventAction = 'Unknown'
    }
    [PSCustomObject] @{
        EventAction                    = $EventAction
        LogName                        = $Log.LogName                         # #: Application
        LogType                        = $Log.LogType                         # #: Administrative
        LogMode                        = $Log.LogMode                         # #: Circular
        FileSize                       = $Log.FileSize                        # #: 69632
        FileSizeMB                     = Convert-Size -Value $Log.FileSize -From Bytes -To MB -Precision 2
        MaximumSizeInBytes             = $Log.MaximumSizeInBytes              # #: 2105344
        MaximumSizeinMB                = Convert-Size -Value $Log.MaximumSizeInBytes -From Bytes -To MB -Precision 2
        IsLogFull                      = $Log.IsLogFull                       # #: False
        LogFilePath                    = $Log.LogFilePath                     # #: % SystemRoot % \System32\Winevt\Logs\Application.evtx
        LastAccessTime                 = $Log.LastAccessTime                  # #: 25.05.2022 12:32:29
        LastWriteTime                  = $Log.LastWriteTime                   # #: 25.05.2022 12:22:33
        OldestRecordNumber             = $Log.OldestRecordNumber              # #: 1
        RecordCount                    = $Log.RecordCount                     # #: 11
        LogIsolation                   = $Log.LogIsolation                    # #: Application
        IsEnabled                      = $Log.IsEnabled                       # #: True
        IsClassicLog                   = $Log.IsClassicLog                    # #: True
        SecurityDescriptor             = $Log.SecurityDescriptor              # #: O:BAG:SYD:(A; ; 0x2; ; ; S - 1 - 15 - 2 - 1)(A; ; 0x2; ; ; S - 1 - 15 - 3 - 1024 - 3153509613 - 960666767 - 3724611135 - 2725662640 - 12138253 - 543910227 - 1950414635 - 4190290187)(A; ; 0xf0007; ; ; SY)(A; ; 0x7; ; ; BA)(A; ; 0x7; ; ; SO)(A; ; 0x3; ; ; IU)(A; ; 0x3; ; ; SU)(A; ; 0x3; ; ; S - 1 - 5 - 3)(A; ; 0x3; ; ; S - 1 - 5 - 33)(A; ; 0x1; ; ; S - 1 - 5 - 32$Log. - 57
        OwningProviderName             = $Log.OwningProviderName              # #:
        ProviderNames                  = $Log.ProviderNames                   # #: { .NET Runtime, .NET Runtime Optimization Service, Application, Application Error… }
        ProviderLevel                  = $Log.ProviderLevel                   # #:
        ProviderKeywords               = $Log.ProviderKeywords                # #:
        ProviderBufferSize             = $Log.ProviderBufferSize              # #: 64
        ProviderMinimumNumberOfBuffers = $Log.ProviderMinimumNumberOfBuffers  # #: 0
        ProviderMaximumNumberOfBuffers = $Log.ProviderMaximumNumberOfBuffers  # #: 64
        ProviderLatency                = $Log.ProviderLatency                 # #: 1000
        ProviderControlGuid            = $Log.ProviderControlGuid             # #:
    }
}