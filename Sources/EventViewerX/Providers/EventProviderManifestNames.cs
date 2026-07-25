using System.Globalization;

namespace EventViewerX.Providers;

internal static class EventProviderManifestNames {
    internal static string SafeId(string value) {
        return Symbol(value, "Value");
    }

    internal static string EventSymbol(
        EventProviderEventDefinition eventDefinition) {

        return Symbol(
                   eventDefinition.Name,
                   "Event_" + eventDefinition.Id) +
               "_V" +
               eventDefinition.Version.ToString(
                   CultureInfo.InvariantCulture);
    }

    internal static string Symbol(string value, string fallback) {
        string source = string.IsNullOrWhiteSpace(value)
            ? fallback
            : value;
        source ??= "Value";
        var builder = new StringBuilder(source.Length + 1);
        foreach (char character in source) {
            builder.Append(
                IsAsciiLetterOrDigit(character)
                    ? char.ToUpperInvariant(character)
                    : '_');
        }
        if (builder.Length == 0) {
            builder.Append("EVX");
        }
        if (char.IsDigit(builder[0])) {
            builder.Insert(0, '_');
        }
        return builder.ToString();
    }

    private static bool IsAsciiLetterOrDigit(char value) {
        return value is >= 'A' and <= 'Z' or
            >= 'a' and <= 'z' or
            >= '0' and <= '9';
    }

    internal static string TypeName(EventProviderFieldType type) {
        return type switch {
            EventProviderFieldType.Guid => "win:GUID",
            EventProviderFieldType.FileTime => "win:FILETIME",
            EventProviderFieldType.SystemTime => "win:SYSTEMTIME",
            EventProviderFieldType.Sid => "win:SID",
            _ => "win:" + type
        };
    }

    internal static string OutputTypeName(
        EventProviderFieldDefinition field) {

        if (!string.IsNullOrWhiteSpace(field.CustomOutputType)) {
            return field.CustomOutputType.Trim();
        }
        return field.OutputType switch {
            EventProviderFieldOutputType.Default => string.Empty,
            EventProviderFieldOutputType.String => "xs:string",
            EventProviderFieldOutputType.DateTime => "xs:dateTime",
            EventProviderFieldOutputType.CultureInsensitiveDateTime =>
                "win:DateTimeCultureInsensitive",
            EventProviderFieldOutputType.Xml => "win:Xml",
            EventProviderFieldOutputType.Json => "win:Json",
            EventProviderFieldOutputType.Utf8 => "win:Utf8",
            EventProviderFieldOutputType.HResult => "win:HResult",
            EventProviderFieldOutputType.ErrorCode => "win:ErrorCode",
            EventProviderFieldOutputType.NtStatus => "win:NTSTATUS",
            EventProviderFieldOutputType.Pid => "win:PID",
            EventProviderFieldOutputType.Tid => "win:TID",
            EventProviderFieldOutputType.Port => "win:Port",
            EventProviderFieldOutputType.IPv4 => "win:IPv4",
            EventProviderFieldOutputType.IPv6 => "win:IPv6",
            EventProviderFieldOutputType.SocketAddress =>
                "win:SocketAddress",
            EventProviderFieldOutputType.CodePointer => "win:CodePointer",
            _ => throw new ArgumentOutOfRangeException(
                nameof(field),
                field.OutputType,
                "Unsupported event field output type.")
        };
    }

    internal static string Hex(ulong value) {
        return "0x" + value.ToString("X", CultureInfo.InvariantCulture);
    }
}
