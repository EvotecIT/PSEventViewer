function Set-EventsSettings {
    <#
    .SYNOPSIS
    Set-EventsSettings updates the settings of a specified event log.

    .DESCRIPTION
    Set-EventsSettings allows you to modify various settings of a specified event log, such as log mode, maximum size, and event action.

    .EXAMPLE
    Set-EventsSettings -LogName 'Application' -EventAction 'ArchiveTheLogWhenFullDoNotOverwrite'
    Updates the 'Application' event log to archive the log when full without overwriting.

    .EXAMPLE
    Set-EventsSettings -LogName 'Security' -MaximumSizeMB 100 -Mode Circular
    Sets the 'Security' event log to have a maximum size of 100 MB and log mode as Circular.
    #>
    [cmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)][string] $LogName,
        [string] $ComputerName,
        [int] $MaximumSizeMB,
        [int] $MaximumSizeInBytes,
        [ValidateSet('OverwriteEventsAsNeededOldestFirst', 'ArchiveTheLogWhenFullDoNotOverwrite', 'DoNotOverwriteEventsClearLogManually')][string] $EventAction,
        [alias('LogMode')][System.Diagnostics.Eventing.Reader.EventLogMode] $Mode
    )

    $TranslateEventAction = @{
        'OverwriteEventsAsNeededOldestFirst'   = [System.Diagnostics.Eventing.Reader.EventLogMode]::Circular
        'ArchiveTheLogWhenFullDoNotOverwrite'  = [System.Diagnostics.Eventing.Reader.EventLogMode]::AutoBackup
        'DoNotOverwriteEventsClearLogManually' = [System.Diagnostics.Eventing.Reader.EventLogMode]::Retain
    }

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
            Write-Warning -Message "Set-EventsSettings - Error occured during reading $LogName log - $($_.Exception.Message)"
            return
        }
    }
    if ($PSBoundParameters.ContainsKey('EventAction')) {
        $Log.LogMode = $TranslateEventAction[$EventAction]
    }
    if ($PSBoundParameters.ContainsKey('Mode')) {
        $Log.LogMode = $Mode
    }
    if ($PSBoundParameters.ContainsKey('MaximumSizeMB')) {
        $MaxSize = $MaximumSizeMB * 1MB
        $Log.MaximumSizeInBytes = $MaxSize
    }
    if ($PSBoundParameters.ContainsKey('MaximumSizeInBytes')) {
        $Log.MaximumSizeInBytes = $MaximumSizeInBytes
    }
    if ($PSCmdlet.ShouldProcess($LogName, "Saving event log settings")) {
        try {
            $Log.SaveChanges()
        } catch {
            if ($ErrorActionPreference -eq 'Stop') {
                throw
            } else {
                Write-Warning -Message "Set-EventsSettings - Error occured during saving of changes for $LogName log - $($_.Exception.Message)"
                return
            }
        }
    }
}