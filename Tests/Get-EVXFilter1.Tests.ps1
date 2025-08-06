Describe 'Get-EVXFilter' {
    It '-NamedDataFilter single value should return single named filter query "="' {
        $XPath = Get-EVXFilter -NamedDataFilter @{ FieldName = 'Value1' } -LogName 'xx' -XPathOnly
        $Xpath | Should -Be '*[EventData[Data[@Name=''FieldName''] = ''Value1'']]'
    }
    It '-NamedDataFilter two-value array should return "or"ed named filter query "="' {
        $XPath = Get-EVXFilter -NamedDataFilter @{ FieldName = ('Value1','Value2') } -LogName 'xx' -XPathOnly
        $Xpath | Should -Be '*[EventData[Data[@Name=''FieldName''] = ''Value1'' or Data[@Name=''FieldName''] = ''Value2'']]'
    }
    It '-NamedDataExcludeFilter should return single named filter query "!="' {
        $XPath = Get-EVXFilter -NamedDataExcludeFilter @{ FieldName = 'Value1' } -LogName 'xx' -XPathOnly
        $Xpath | Should -Be '*[EventData[Data[@Name=''FieldName''] != ''Value1'']]'
    }
    It '-NamedDataExcludeFilter two-value array should return "and"ed named filter query "!="' {
        $XPath = Get-EVXFilter -NamedDataExcludeFilter @{ FieldName = ('Value1','Value2') } -LogName 'xx' -XPathOnly
        $Xpath | Should -Be '*[EventData[Data[@Name=''FieldName''] != ''Value1'' and Data[@Name=''FieldName''] != ''Value2'']]'
    }
}

Describe "Get-EventFilters using Path and NamendDataFilter" {
    It "basic syntax check for queries of eventlogs" {
        $XML = Get-EVXFilter -LogName System -EventId 7040 -NamedDataExcludeFilter @{ param4 = 'BITS' }
        $XML | Should -BeLike '*Query Id="0" Path="system"*'    -Because 'We wanted to query a filepath'
        $XML | Should -BeLike '*Select Path="system"*'          -Because 'Queries using eventfiles do not have a Channel'
    }
    It "basic syntax check for queries of saved eventlog files" {
        $FilePath = [System.IO.Path]::Combine($PSScriptRoot, 'Logs', 'NamedFilterExamples.evtx')
        $XML = Get-EVXFilter -Path $FilePath -EventId 7040 -NamedDataExcludeFilter @{ param4 = 'BITS' }
        $XML | Should -BeLike '*Query Id="0" Path="file://*'    -Because 'We wanted to query a filepath'
        $XML | Should -Not -Belike '*Select Path*'              -Because 'Queries using eventfiles do not have a Channel'
    }

    It '-Keywords single value should produce band filter' {
        $XPath = Get-EVXFilter -Keywords 1125899906842624 -LogName 'xx' -XPathOnly
        $XPath | Should -Be '*[System[band(Keywords,1125899906842624)]]'
    }

    It '-Keywords multiple values should OR them in band filter' {
        $XPath = Get-EVXFilter -Keywords 1125899906842624,281474976710656 -LogName 'xx' -XPathOnly
        $XPath | Should -Be '*[System[band(Keywords,1407374883553280)]]'
    }
}

Describe 'Get-EVXFilter parameter binding' {
    It 'binds EventId by new name' {
        $XPath = Get-EVXFilter -EventId 1 -LogName 'xx' -XPathOnly
        $XPath | Should -Be '*[System[(EventID=1)]]'
    }
    It 'binds EventId via ID alias' {
        $XPath = Get-EVXFilter -Id 1 -LogName 'xx' -XPathOnly
        $XPath | Should -Be '*[System[(EventID=1)]]'
    }
    It 'binds EventRecordId and RecordId alias' {
        $Name = Get-EVXFilter -EventRecordId 10 -LogName 'xx' -XPathOnly
        $Alias = Get-EVXFilter -RecordId 10 -LogName 'xx' -XPathOnly
        $Name | Should -Be '*[System[(EventRecordID=10)]]'
        $Alias | Should -Be $Name
    }
}