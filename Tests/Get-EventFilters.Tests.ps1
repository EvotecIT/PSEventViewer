Describe 'Get-EventsFilters' {
    It '-NamedDataFilter single value should return single named filter query "="' {
        $XPath = Get-EventsFilter -NamedDataFilter @{ FieldName = 'Value1' } -LogName 'xx' -XPathOnly
        $Xpath | Should -Be '*[EventData[Data[@Name=''FieldName''] = ''Value1'']]'
    }
    It '-NamedDataFilter two-value array should return "or"ed named filter query "="' {
        $XPath = Get-EventsFilter -NamedDataFilter @{ FieldName = ('Value1','Value2') } -LogName 'xx' -XPathOnly
        $Xpath | Should -Be '*[EventData[Data[@Name=''FieldName''] = ''Value1'' or Data[@Name=''FieldName''] = ''Value2'']]'
    }
    It '-NamedDataExcludeFilter should return single named filter query "!="' {
        $XPath = Get-EventsFilter -NamedDataExcludeFilter @{ FieldName = 'Value1' } -LogName 'xx' -XPathOnly
        $Xpath | Should -Be '*[EventData[Data[@Name=''FieldName''] != ''Value1'']]'
    }
    It '-NamedDataExcludeFilter two-value array should return "and"ed named filter query "!="' {
        $XPath = Get-EventsFilter -NamedDataExcludeFilter @{ FieldName = ('Value1','Value2') } -LogName 'xx' -XPathOnly
        $Xpath | Should -Be '*[EventData[Data[@Name=''FieldName''] != ''Value1'' and Data[@Name=''FieldName''] != ''Value2'']]'
    }
}
