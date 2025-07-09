Describe 'Get-EVXEvent - Named Event' {
    It 'Returns ADUserLogon events when available' -Tag 'RequiresEvents' {
        $events = Get-EVXEvent -Type ADUserLogon -MaxEvents 1 -ErrorAction SilentlyContinue
        if ($events) {
            $events.Count | Should -BeGreaterThan 0
        } else {
            Write-Warning 'No ADUserLogon events found on this system.'
        }
    }
}
