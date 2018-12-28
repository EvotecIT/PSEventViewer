function Get-EventsInformation {
    <#
    .SYNOPSIS
    Short description

    .DESCRIPTION
    Long description

    .PARAMETER Machine
    Parameter description

    .PARAMETER LogName
    Parameter description

    .PARAMETER MaxRunspaces
    Parameter description

    .EXAMPLE

    $Computer = 'EVO1','AD1','AD2'
    $LogName = 'Security'

    $Size = Get-EventsInformation -Computer $Computer -LogName $LogName -Verbose
    $Size | ft -A

    Output:

    EventNewest         EventOldest          FileSize FileSizeCurrentGB FileSizeMaximumGB IsClassicLog IsEnabled IsLogFull LastAccessTime      LastWriteTime
    -----------         -----------          -------- ----------------- ----------------- ------------ --------- --------- --------------      -------------
    28.12.2018 12:47:14 20.12.2018 19:29:57 110170112 0.1 GB            0.11 GB                   True      True     False 27.05.2018 14:18:36 28.12.2018 12:33:24
    28.12.2018 12:46:51 26.12.2018 12:54:16  20975616 0.02 GB           0.02 GB                   True      True     False 28.12.2018 12:46:57 28.12.2018 12:46:57


    .NOTES
    General notes
    #>

    [CmdLetBinding()]
    param(
        [alias ("ADDomainControllers", "DomainController", "Server", "Servers", "Computer", "Computers", "ComputerName")]
        [string[]] $Machine,
        [string[]] $FilePath,
        [alias ("LogType", "Log")][string] $LogName = $null,
        [int] $MaxRunspaces = 10
    )
    Write-Verbose "Get-EventsInformation - processing start"
    if ($PSCmdlet.MyInvocation.BoundParameters["Verbose"].IsPresent) { $Verbose = $true } else { $Verbose = $false }

    $Time = Start-TimeLog
    $Pool = New-Runspace -MaxRunspaces $maxRunspaces -Verbose:$Verbose

    $RunSpaces = @()
    $RunSpaces += foreach ($Computer in $Machine) {
        Write-Verbose "Get-EventsInformation - Setting up runspace for $Computer"
        $Parameters = [ordered] @{
            Computer = $Computer
            #Path     = ''
            LogName  = $LogName
            Verbose  = $Verbose
        }
        # returns values
        Start-Runspace -ScriptBlock $ScriptBlockEventsInformation -Parameters $Parameters -RunspacePool $Pool -Verbose:$Verbose
    }

    $RunSpaces += foreach ($Path in $FilePath) {
        Write-Verbose "Get-EventsInformation - Setting up runspace for $Path"
        $Parameters = [ordered] @{
               Path     = $Path
            Verbose  = $Verbose
        }
        # returns values
        Start-Runspace -ScriptBlock $ScriptBlockEventsInformation -Parameters $Parameters -RunspacePool $Pool -Verbose:$Verbose
    }


    ### End Runspaces START
    $AllEvents = Stop-Runspace -Runspaces $RunSpaces `
        -FunctionName "Get-EventsInformation" `
        -RunspacePool $pool -Verbose:$Verbose `
        -ErrorAction SilentlyContinue `
        -ErrorVariable +AllErrors

    $Elapsed = Stop-TimeLog -Time $Time -Option OneLiner
    Write-Verbose -Message "Get-EventsInformation - processing end - $Elapsed"
    ### End Runspaces END
    return $AllEvents
}