Describe 'Clear-EVXLog cmdlet' {
    BeforeAll {
        $suffix = [Guid]::NewGuid().ToString('N')
        $script:log = 'EVX' + $suffix + 'Clear'
        $script:provider = 'EVXClearSource' + $suffix
        $script:isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)
        $script:skip = -not $script:isAdmin

        if (-not $script:skip) {
            Remove-EVXSource -SourceName $script:provider -LogName $script:log -ErrorAction SilentlyContinue
            Remove-EVXLog -LogName $script:log -ErrorAction SilentlyContinue
            New-EVXLog -LogName $script:log -ProviderName $script:provider | Out-Null
            Write-EVXEvent -LogName $script:log -ProviderName $script:provider -Message 'test' -Id 1000
        }
    }
    AfterAll {
        if (-not $script:skip) {
            Remove-EVXLog -LogName $script:log -ErrorAction SilentlyContinue
            Remove-EVXSource -SourceName $script:provider -LogName $script:log -ErrorAction SilentlyContinue
        }
    }
    It 'exposes bounded remote session controls' {
        $command = Get-Command Clear-EVXLog
        $command.Parameters.Keys | Should -Contain 'Credential'
        $command.Parameters.Keys | Should -Contain 'Authentication'
        $command.Parameters.Keys | Should -Contain 'TimeoutMs'
    }
    It 'atomically backs up and clears the log through wevtapi' -Skip:$script:skip {
        $backup = Join-Path $TestDrive 'EVXClearTestLog.evtx'
        $result = Clear-EVXLog -LogName $script:log -BackupPath $backup -Confirm:$false

        $result.LogName | Should -Be $script:log
        $result.BackupPath | Should -Be ([IO.Path]::GetFullPath($backup))
        Test-Path -LiteralPath $backup | Should -BeTrue
        (Get-EVXLog -Path $backup).RecordCount | Should -BeGreaterThan 0
        (Get-EVXLog -LogName $script:log).RecordCount | Should -Be 0
    }
}
