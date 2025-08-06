# Demonstrates using Get-EVXFilter with updated parameter names and aliases

# Using new parameter names
Get-EVXFilter -EventId 1 -EventRecordId 10 -LogName 'System' -XPathOnly

# Using existing aliases for backward compatibility
Get-EVXFilter -Id 1 -RecordId 10 -LogName 'System' -XPathOnly
