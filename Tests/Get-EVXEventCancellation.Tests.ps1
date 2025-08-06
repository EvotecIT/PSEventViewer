Describe 'Get-EVXEvent - Cancellation' {
    It 'Stops execution when pipeline is cancelled' {
        $filePath = [System.IO.Path]::Combine($PSScriptRoot, 'Logs', 'Active Directory Web Services.evtx')
        $modulePath = [System.IO.Path]::Combine($PSScriptRoot, '..', 'PSEventViewer.psm1')
        $ps = [System.Management.Automation.PowerShell]::Create()
        try {
            $null = $ps.AddScript({
                param(
                    [string] $modulePath,
                    [string] $eventPath
                )
                Import-Module -Name $modulePath -Force
                Get-EVXEvent -Path $eventPath -DisableParallel -MaxEvents 10000 | ForEach-Object {
                    Start-Sleep -Milliseconds 50
                }
            }).AddArgument($modulePath).AddArgument($filePath)
            $handle = $ps.BeginInvoke()
            Start-Sleep -Milliseconds 200
            $ps.Stop()
            { $ps.EndInvoke($handle) } | Should -Throw
        } finally {
            $ps.Dispose()
        }
    }
}
