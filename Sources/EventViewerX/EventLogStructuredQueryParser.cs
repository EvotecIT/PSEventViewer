using System.Xml.Linq;

namespace EventViewerX;

internal static class EventLogStructuredQueryParser {
    internal static XElement[] ParseQueries(string queryXml) {
        XDocument document = XDocument.Parse(
            queryXml,
            LoadOptions.PreserveWhitespace);
        XElement[] queries = document.Root?
            .Elements()
            .Where(static element =>
                string.Equals(
                    element.Name.LocalName,
                    "Query",
                    StringComparison.Ordinal))
            .Select(static element => new XElement(element))
            .ToArray() ??
            Array.Empty<XElement>();
        if (queries.Length == 0) {
            throw new ArgumentException(
                "A structured query must contain at least one Query element.",
                nameof(queryXml));
        }
        return queries;
    }

    internal static EventLogQuerySourceKind ResolveSourceKind(
        XElement query,
        EventLogQuerySourceKind declaredKind) {

        if (!Enum.IsDefined(
                typeof(EventLogQuerySourceKind),
                declaredKind)) {
            throw new ArgumentOutOfRangeException(
                nameof(declaredKind),
                "The structured query source kind is not supported.");
        }
        string[] paths = GetPaths(query);
        bool hasFile = paths.Any(IsFileSource);
        bool hasChannel = paths.Any(static path =>
            !IsFileSource(path));
        if (hasFile && hasChannel) {
            throw new ArgumentException(
                "One Query element cannot mix channel and offline-file paths because Windows requires one native source kind per query handle.");
        }
        EventLogQuerySourceKind inferredKind = hasFile
            ? EventLogQuerySourceKind.File
            : EventLogQuerySourceKind.Channel;
        if (declaredKind != EventLogQuerySourceKind.Auto &&
            declaredKind != inferredKind) {
            throw new ArgumentException(
                $"Structured query paths are {inferredKind} sources but SourceKind is {declaredKind}.");
        }
        return declaredKind == EventLogQuerySourceKind.Auto
            ? inferredKind
            : declaredKind;
    }

    internal static EventLogQuerySourceKind[] ResolveSourceKinds(
        string queryXml,
        EventLogQuerySourceKind declaredKind) {

        return ParseQueries(queryXml)
            .Select(query =>
                ResolveSourceKind(
                    query,
                    declaredKind))
            .Distinct()
            .ToArray();
    }

    internal static string GetFileSourceIdentity(
        XElement query) {

        string[] sources = GetPaths(query)
            .Where(IsFileSource)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (sources.Length != 1) {
            throw new ArgumentException(
                "Each offline-file Query element must reference exactly one file path.");
        }
        return sources[0];
    }

    internal static string GetFilePath(
        string fileSourceIdentity) {

        if (!IsFileSource(fileSourceIdentity)) {
            throw new ArgumentException(
                "The structured query source is not a file URI.",
                nameof(fileSourceIdentity));
        }
        if (Uri.TryCreate(
                fileSourceIdentity,
                UriKind.Absolute,
                out Uri? uri) &&
            uri.IsFile) {
            return Path.GetFullPath(uri.LocalPath);
        }
        string path = fileSourceIdentity.Substring(
            "file://".Length);
        if (path.Length >= 3 &&
            path[0] == '/' &&
            char.IsLetter(path[1]) &&
            path[2] == ':') {
            path = path.Substring(1);
        }
        return Path.GetFullPath(
            Uri.UnescapeDataString(path)
                .Replace(
                    '/',
                    Path.DirectorySeparatorChar));
    }

    private static string[] GetPaths(XElement query) {
        return query
            .DescendantsAndSelf()
            .Attributes()
            .Where(static attribute =>
                string.Equals(
                    attribute.Name.LocalName,
                    "Path",
                    StringComparison.Ordinal))
            .Select(static attribute =>
                attribute.Value.Trim())
            .Where(static path => path.Length > 0)
            .ToArray();
    }

    private static bool IsFileSource(string path) {
        return path.StartsWith(
            "file://",
            StringComparison.OrdinalIgnoreCase);
    }
}
