Add-Type -TypeDefinition @"
using System;

namespace PSEventViewer
{
    public enum Keywords : long {
        AuditFailure     = (long) 4503599627370496,
        AuditSuccess     = (long) 9007199254740992,
        CorrelationHint2 = (long) 18014398509481984,
        EventLogClassic  = (long) 36028797018963968,
        Sqm              = (long) 2251799813685248,
        WdiDiagnostic    = (long) 1125899906842624,
        WdiContext       = (long) 562949953421312,
        ResponseTime     = (long) 281474976710656,
        None             = (long) 0
    }
}
"@