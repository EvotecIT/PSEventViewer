
Import-Module .\PSEventViewer.psd1 -Force

$OK = Get-Events -LogName 'Security' -EventID 4722, 4725, 4740, 4724 -Computer ad1 -DisableParallel
$OK[1] | Format-List *