Import-Module PSEventViewer -Force

$AllEvents = Get-Events -LogName 'Application' -ID 1001 -MaxEvents 1 -Verbose -DisableParallel
$Message = $AllEvents[0].Message

$M = [ordered] @{}
foreach ($SubMessage in $Message.Split([Environment]::NewLine)) {
    if ($SubMessage -like '*:*') {
        $T = $SubMessage.Split(':')
        $M[($T[0])] = $T[1]
    } else {
        if ($SubMessage.Trim() -ne '') {

        }
    }
}



<#
Fault bucket 2058674454742653114, type 5
Event Name: RADAR_PRE_LEAK_64
Response: Not available
Cab Id: 0

Problem signature:
P1: Code.exe
P2: 1.36.0.0
P3: 10.0.18362.2.0.0
P4:
P5:
P6:
P7:
P8:
P9:
P10:

Attached files:
\\?\C:\Users\PRZEMY~1.KLY\AppData\Local\Temp\RDR7D2A.tmp\empty.txt
\\?\C:\ProgramData\Microsoft\Windows\WER\Temp\WER7D2B.tmp.WERInternalMetadata.xml
\\?\C:\ProgramData\Microsoft\Windows\WER\Temp\WER7D3C.tmp.xml
\\?\C:\ProgramData\Microsoft\Windows\WER\Temp\WER7D98.tmp.csv
\\?\C:\ProgramData\Microsoft\Windows\WER\Temp\WER7DD7.tmp.txt

These files may be available here:


Analysis symbol:
Rechecking for solution: 0
Report Id: a0709c03-7dbf-42e3-977b-9b53a404444a
Report Status: 268435456
Hashed bucket: 74775b34cbd39705dc91e1825f1b58ba
Cab Guid: 0

#>