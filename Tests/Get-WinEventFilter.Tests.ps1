Describe 'Get-WinEventFilters' {
    It '-NamedDataFilter single value should return single named filter query "="' {
        $XPath = Get-WinEventFilter -NamedDataFilter @{ FieldName = 'Value1' } -LogName 'xx' -XPathOnly
        $Xpath | Should -Be '*[EventData[Data[@Name=''FieldName''] = ''Value1'']]'
    }
    It '-NamedDataFilter two-value array should return "or"ed named filter query "="' {
        $XPath = Get-WinEventFilter -NamedDataFilter @{ FieldName = ('Value1','Value2') } -LogName 'xx' -XPathOnly
        $Xpath | Should -Be '*[EventData[Data[@Name=''FieldName''] = ''Value1'' or Data[@Name=''FieldName''] = ''Value2'']]'
    }
    It '-NamedDataExcludeFilter should return single named filter query "!="' {
        $XPath = Get-WinEventFilter -NamedDataExcludeFilter @{ FieldName = 'Value1' } -LogName 'xx' -XPathOnly
        $Xpath | Should -Be '*[EventData[Data[@Name=''FieldName''] != ''Value1'']]'
    }
    It '-NamedDataExcludeFilter two-value array should return "and"ed named filter query "!="' {
        $XPath = Get-WinEventFilter -NamedDataExcludeFilter @{ FieldName = ('Value1','Value2') } -LogName 'xx' -XPathOnly
        $Xpath | Should -Be '*[EventData[Data[@Name=''FieldName''] != ''Value1'' and Data[@Name=''FieldName''] != ''Value2'']]'
    }
}

Describe "Get-EventFilters using Path and NamendDataFilter" {
    It "basic syntax check for queries of eventlogs" {
        $XML = Get-WinEventFilter -logname System -Id 7040 -NamedDataExcludeFilter @{ param4 = 'BITS' }
        $XML | Should -BeLike '*Query Id="0" Path="system"*'    -Because 'We wanted to query a filepath'
        $XML | Should -BeLike '*Select Path="system"*'          -Because 'Queries using eventfiles do not have a Channel'
    }
    It "basic syntax check for queries of saved eventlog files" {
        $FilePath = [System.IO.Path]::Combine($PSScriptRoot, 'Logs', 'NamedFilterExamples.evtx')
        $XML = Get-WinEventFilter -Path $FilePath -Id 7040 -NamedDataExcludeFilter @{ param4 = 'BITS' }
        $XML | Should -BeLike '*Query Id="0" Path="file://*'    -Because 'We wanted to query a filepath'
        $XML | Should -Not -Belike '*Select Path*'              -Because 'Queries using eventfiles do not have a Channel'
    }
}
Describe 'Additional Get-WinEventFilter cases' {
    It '-ID multiple values produce "or" xpath filter' {
        $XPath = Get-WinEventFilter -ID 1,2 -LogName 'xx' -XPathOnly
        $XPath | Should -Be '*[System[(EventID=1) or (EventID=2)]]'
    }
    It '-ExcludeID uses inequality syntax' {
        $XPath = Get-WinEventFilter -ExcludeID 1,2 -LogName 'xx' -XPathOnly
        $XPath | Should -Be '*[System[(EventID!=1) or (EventID!=2)]]'
    }
}
