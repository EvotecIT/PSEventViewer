function New-Runspace {
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
    param (
        $ScriptBlock,
        [hashtable] $Parameters,
        [System.Management.Automation.Runspaces.RunspacePool] $RunspacePool
    )
    $runspace = [PowerShell]::Create()
    $null = $runspace.AddScript($ScriptBlock)
    $null = $runspace.AddParameters($Parameters)
    $runspace.RunspacePool = $RunspacePool
    return [PSCustomObject]@{ Pipe = $runspace; Status = $runspace.BeginInvoke() }
}

function Stop-Runspace {
    param(
        $Runspaces,
        [string] $FunctionName,
        [System.Management.Automation.Runspaces.RunspacePool] $RunspacePool
    )
    $List = @()
    while ($Runspaces.Status -ne $null) {
        $completed = $runspaces | Where-Object { $_.Status.IsCompleted -eq $true }
        foreach ($runspace in $completed) {
            foreach ($e in $($runspace.Pipe.Streams.Error)) {
                Write-Verbose "$FunctionName - Error from runspace: $e"
            }
            foreach ($v in $($runspace.Pipe.Streams.Verbose)) {
                Write-Verbose "$FunctionName - Verbose from runspace: $v"
            }
            $List += $runspace.Pipe.EndInvoke($runspace.Status)
            $runspace.Status = $null
        }
    }
    $RunspacePool.Close()
    $RunspacePool.Dispose()
    return $List
}