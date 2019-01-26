Add-Type -TypeDefinition @"
using System;

namespace PSEventViewer
{
    public enum Level {
        Verbose       = 5,
        Informational = 4,
        Warning       = 3,
        Error         = 2,
        Critical      = 1,
        LogAlways     = 0
    }
}
"@