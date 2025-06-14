Describe 'Set-WinEventSettings - command metadata' {
    BeforeAll {
        Import-Module -Force $PSScriptRoot/..\PSEventViewer.psd1
    }

    It 'exports Set-EventsSettings alias' {
        (Get-Alias Set-EventsSettings).Definition | Should -Be 'Set-WinEventSettings'
    }

    It 'supports ShouldProcess' {
        (Get-Command Set-WinEventSettings).CmdletBinding.SupportsShouldProcess | Should -Be $true
    }
}
