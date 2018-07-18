function New-Runspace {
    [cmdletbinding()]
    param (
        [int] $minRunspaces = 1,
        [int] $maxRunspaces = [int]$env:NUMBER_OF_PROCESSORS + 1
    )
    $RunspacePool = [RunspaceFactory]::CreateRunspacePool($minRunspaces, $maxRunspaces)
    $RunspacePool.ApartmentState = "MTA"
    $RunspacePool.Open()
    return $RunspacePool
}
function Start-Runspace {
    [cmdletbinding()]
    param (
        $ScriptBlock,
        [hashtable] $Parameters,
        [System.Management.Automation.Runspaces.RunspacePool] $RunspacePool
    )
    #Write-Verbose "Start-Runspace - Starting"
    $runspace = [PowerShell]::Create()
    $null = $runspace.AddScript($ScriptBlock)
    $null = $runspace.AddParameters($Parameters)
    $runspace.RunspacePool = $RunspacePool
    #Write-Verbose "Start-Runspace - Ending soon"
    $Data = [PSCustomObject]@{ Pipe = $runspace; Status = $runspace.BeginInvoke() }
    #Write-Verbose "Start-Runspace - Ending done"
    return $Data
}

function Stop-Runspace {
    [cmdletbinding()]
    param(
        [System.Object[]]$Runspaces,
        [string] $FunctionName,
        [System.Management.Automation.Runspaces.RunspacePool] $RunspacePool
    )
    $List = @()
    while ($Runspaces.Status -ne $null) {
        #foreach ($v in $($runspaces.Pipe.Streams.Verbose)) {
        #    Write-Verbose "$FunctionName - 1Verbose from runspace: $v"
        #}
        $completed = $runspaces | Where-Object { $_.Status.IsCompleted -eq $true }

        foreach ($runspace in $completed) {
            #write-verbose 'Stop-runspace - Hello 2'
            foreach ($e in $($runspace.Pipe.Streams.Error)) {
                Write-Verbose "$FunctionName - Error from runspace: $e"
            }
            foreach ($v in $($runspace.Pipe.Streams.Verbose)) {
                Write-Verbose "$FunctionName - Verbose from runspace: $v"
            }
            #write-verbose 'Stop-runspace - Hello 3'
            $List += $runspace.Pipe.EndInvoke($runspace.Status)
            #write-verbose 'Stop-runspace - Hello 4'
            $runspace.Status = $null
            #write-verbose 'Stop-runspace - Hello 5'
        }
    }
    #write-verbose 'Stop-runspace - Hello 6'
    $RunspacePool.Close()
    #write-verbose 'Stop-runspace - Hello 7'
    $RunspacePool.Dispose()
    #write-verbose 'Stop-runspace - Hello 8'
    return $List
}