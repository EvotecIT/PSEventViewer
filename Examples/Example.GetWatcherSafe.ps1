# Demonstrates safe retrieval of watcher information
$watcher = Get-EVXWatcher -Name 'DemoWatcher'
if ($watcher) {
    $watcher | Format-List
} else {
    Write-Host "Watcher 'DemoWatcher' not found."
}
