Describe 'Get-WinEventInformation - Basic Log Test' {
    $info = Get-WinEventInformation -Machine $Env:COMPUTERNAME -LogName 'Application' -MaxRunspaces 1

    It 'Should return some results' {
        $info.Count | Should -BeGreaterThan 0
    }
    It 'Should have Source equal to machine name' {
        $info[0].Source | Should -Be $Env:COMPUTERNAME
    }
    It 'Should include LogName' {
        $info[0].LogName | Should -Be 'Application'
    }
}

Describe 'Get-WinEventInformation - FilePath Test' {
    $filePath = Join-Path $PSScriptRoot 'Logs' 'Active Directory Web Services.evtx'
    $info = Get-WinEventInformation -FilePath $filePath -MaxRunspaces 1

    It 'Should return one object' {
        $info.Count | Should -Be 1
    }
    It 'Source should be File' {
        $info[0].Source | Should -Be 'File'
    }
    It 'LogName should be N/A' {
        $info[0].LogName | Should -Be 'N/A'
    }
}
