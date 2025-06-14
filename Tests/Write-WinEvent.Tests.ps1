Describe 'Write-WinEvent cmdlet' {
    BeforeAll {
        Import-Module -Force $PSScriptRoot/..\PSEventViewer.psd1
    }

    It 'exports Write-Event alias' {
        (Get-Alias Write-Event).Definition | Should -Be 'Write-WinEvent'
    }

    It 'allows null ComputerName parameter' {
        if (-not $IsWindows) {
            return
        }
        $err = $null
        Write-Event -LogName 'Application' -Source 'Windows PowerShell' -ID 1 -Message 'Codex test message' -Computer $null -ErrorAction SilentlyContinue -ErrorVariable err
        $err | Should -BeNullOrEmpty
    }
}
