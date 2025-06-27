Describe 'Get-EVXInfo - Basic Log Test' {
    It 'Should return some results' {
        $info = Get-EVXInfo -Machine $Env:COMPUTERNAME -LogName 'Application'
        $info | Should -Not -BeNullOrEmpty
        $info.Source | Should -Be $Env:COMPUTERNAME
        $info.LogName | Should -Be 'Application'
    }
}

Describe 'Get-EVXInfo - FilePath Test' {
    It 'Should be consistent' {
        $filePath = [io.path]::Combine($PSScriptRoot, 'Logs', 'Active Directory Web Services.evtx')
        $info1 = Get-EVXInfo -FilePath $filePath
        Test-Path $filePath | Should -Be $true
        $info1 | Should -BeNullOrEmpty -Not
        $info1.Source | Should -Be 'File'
        $info1.LogName | Should -Be 'N/A'
    }
}

Describe 'Get-EVXInfo cmdlet' {
    It 'Returns log information' {
        $result = Get-EVXInfo -LogName 'Application'
        $result.LogName | Should -Be 'Application'
        $result.LogMode | Should -Not -BeNullOrEmpty
        $result.MaximumSizeInBytes | Should -BeGreaterThan 0
    }
}
