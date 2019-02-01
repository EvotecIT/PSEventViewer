Import-Module ..\PSEventViewer.psd1 -Force
$List = @()
#$List += @{ Server = 'AD1'; LogName = 'Security'; EventID = '5136', '5137'; Type = 'Computer' }
#$List += @{ Server = 'AD2'; LogName = 'Security'; EventID = '5136', '5137'; Type = 'Computer' }
#$List += @{ Server = 'C:\MyEvents\Archive-Security-2018-08-21-23-49-19-424.evtx'; LogName = 'Security'; EventID = '5136', '5137'; Type = 'File' }
#$List += @{ Server = 'C:\MyEvents\Archive-Security-2018-09-15-09-27-52-679.evtx'; LogName = 'Security'; EventID = '5136', '5137'; Type = 'File' }
#$List += @{ Server = 'Evo1'; LogName = 'Setup'; EventID = 2; Type = 'Computer'; }
#$List += @{ Server = 'AD1.ad.evotec.xyz'; LogName = 'Security'; EventID = 4720, 4738, 5136, 5137, 5141, 4722, 4725, 4767, 4723, 4724, 4726, 4728, 4729, 4732, 4733, 4746, 4747, 4751, 4752, 4756, 4757, 4761, 4762, 4785, 4786, 4787, 4788, 5136, 5137, 5141, 5136, 5137, 5141, 5136, 5137, 5141; Type = 'Computer' }
$List += @{ Server = 'Evo1'; LogName = 'Security'; Type = 'Computer'; MaxEvents = 15; Keywords = [PSEventViewer.Keywords]::AuditSuccess }
$List += @{ Server = 'Evo1'; LogName = 'Security'; Type = 'Computer'; MaxEvents = 15; Level = [PSEventViewer.Level]::Informational; Keywords = [PSEventViewer.Keywords]::AuditFailure }
$Output4 = Get-Events -ExtendedInput $List -Verbose
$Output4 | ft Computer, Date, LevelDisplayName