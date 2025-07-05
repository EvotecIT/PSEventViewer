# Create a basic log
New-EVXLog -LogName 'MyLog'

# Create log with custom provider and size
New-EVXLog -LogName 'MyAdvancedLog' -ProviderName 'MyProvider' -MaximumKilobytes 1024 -OverflowAction OverwriteOlder -RetentionDays 30

# Display logs matching pattern
Get-EVXLog -LogName 'My*' | Format-Table LogName, MachineName, MaximumSizeInBytes

# Remove logs
Remove-EVXLog -LogName 'MyLog'
Remove-EVXLog -LogName 'MyAdvancedLog'
