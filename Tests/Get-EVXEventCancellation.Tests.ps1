Describe 'Get-EVXEvent - Cancellation' {
    It 'Stops execution when pipeline is cancelled' {
        $modulePath = [System.IO.Path]::Combine($PSScriptRoot, '..', 'PSEventViewer.psm1')
        $ps = [System.Management.Automation.PowerShell]::Create()
        try {
            $null = $ps.AddScript({
                param(
                    [string] $modulePath,
                    [string] $logName
                )
                Import-Module -Name $modulePath -Force
                Get-EVXEvent -LogName $logName -DisableParallel -MaxEvents 100000 | ForEach-Object {
                    Start-Sleep -Milliseconds 50
                }
            }).AddArgument($modulePath).AddArgument('Application')
            $handle = $ps.BeginInvoke()
            Start-Sleep -Milliseconds 200
            $ps.Stop()
            { $ps.EndInvoke($handle) } | Should -Throw
        } finally {
            $ps.Dispose()
        }
    }
}
