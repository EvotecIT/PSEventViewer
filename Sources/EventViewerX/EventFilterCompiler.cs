using System.Collections;
using System.Globalization;
using System.Xml;

namespace EventViewerX;

/// <summary>
/// Compiles typed event filters into the Windows Event Log XPath and structured XML query formats.
/// </summary>
public static class EventFilterCompiler {
    /// <summary>Maximum predicate expressions accepted by the Windows Event Log XPath engine.</summary>
    public const int MaximumXPathExpressions = 22;

    /// <summary>Counts the native XPath expressions required by a typed filter.</summary>
    public static int CountExpressions(EventFilter? filter) {
        if (filter == null) {
            return 0;
        }
        return WindowsEventFilterBuilder.CountWinEventFilterExpressions(
            ToInvariantStrings(filter.EventIds),
            ToInvariantStrings(filter.RecordIds),
            filter.StartTime,
            filter.EndTime,
            NormalizeStrings(filter.Data),
            NormalizeStrings(filter.ProviderNames),
            filter.Keywords?.ToArray(),
            filter.Levels?
                .Select(static value =>
                    ((System.Diagnostics.Tracing.EventLevel)value).ToString())
                .ToArray(),
            NormalizeStrings(filter.UserIds),
            ToHashtables(filter.NamedData),
            ToHashtables(filter.ExcludedNamedData),
            ToInvariantStrings(filter.ExcludedEventIds),
            filter.MinimumRecordIdExclusive,
            filter.MaximumRecordIdExclusive);
    }

    /// <summary>Builds a native Windows Event Log XPath expression.</summary>
    public static string BuildXPath(EventFilter? filter) {
        if (filter == null || !filter.HasAny) {
            return "*";
        }
        if (HasExcludedNamedData(filter)) {
            throw new ArgumentException(
                "ExcludedNamedData requires a structured QueryList Suppress clause and cannot be compiled as raw XPath. Use BuildChannelQueryXml or BuildFileQueryXml.",
                nameof(filter));
        }
        if (filter.StartTime.HasValue &&
            filter.EndTime.HasValue &&
            filter.StartTime.Value > filter.EndTime.Value) {
            throw new ArgumentException(
                "StartTime must be less than or equal to EndTime.",
                nameof(filter));
        }

        string[]? levels = filter.Levels?
            .Select(static value => {
                var level = (System.Diagnostics.Tracing.EventLevel)value;
                if (!Enum.IsDefined(typeof(System.Diagnostics.Tracing.EventLevel), level)) {
                    throw new ArgumentOutOfRangeException(
                        nameof(filter),
                        $"Event level '{value}' is not supported by Windows Event Log.");
                }
                return level.ToString();
            })
            .ToArray();
        return WindowsEventFilterBuilder.BuildWinEventFilter(
            id: ToInvariantStrings(filter.EventIds),
            eventRecordId: ToInvariantStrings(filter.RecordIds),
            startTime: filter.StartTime,
            endTime: filter.EndTime,
            data: NormalizeStrings(filter.Data),
            providerName: NormalizeStrings(filter.ProviderNames),
            keywords: filter.Keywords?.ToArray(),
            level: levels,
            userId: NormalizeStrings(filter.UserIds),
            namedDataFilter: ToHashtables(filter.NamedData),
            namedDataExcludeFilter: ToHashtables(filter.ExcludedNamedData),
            excludeId: ToInvariantStrings(filter.ExcludedEventIds),
            xpathOnly: true,
            minimumEventRecordIdExclusive: filter.MinimumRecordIdExclusive,
            maximumEventRecordIdExclusive: filter.MaximumRecordIdExclusive);
    }

    /// <summary>
    /// Builds a structured XML query that can select and suppress records across several channels.
    /// </summary>
    public static string BuildChannelQueryXml(
        IEnumerable<string> logNames,
        EventFilter? select = null,
        EventFilter? suppress = null) {

        return BuildQueryXml(
            logNames,
            filePaths: false,
            select,
            suppress?.HasAny == true
                ? new[] { suppress }
                : Array.Empty<EventFilter>());
    }

    /// <summary>
    /// Builds a structured channel query with several suppression expressions.
    /// Multiple suppressions are combined as a union, which permits an equivalent
    /// partitioned suppression filter to exceed the native per-XPath expression limit.
    /// </summary>
    public static string BuildChannelQueryXmlWithSuppressions(
        IEnumerable<string> logNames,
        EventFilter? select,
        IEnumerable<EventFilter> suppressions) {

        return BuildQueryXml(
            logNames,
            filePaths: false,
            select,
            NormalizeSuppressions(suppressions));
    }

    /// <summary>
    /// Builds a structured XML query that can select and suppress records across several event-log files.
    /// </summary>
    public static string BuildFileQueryXml(
        IEnumerable<string> paths,
        EventFilter? select = null,
        EventFilter? suppress = null) {

        return BuildQueryXml(
            paths,
            filePaths: true,
            select,
            suppress?.HasAny == true
                ? new[] { suppress }
                : Array.Empty<EventFilter>());
    }

    /// <summary>
    /// Builds a structured file query with several suppression expressions.
    /// Multiple suppressions are combined as a union.
    /// </summary>
    public static string BuildFileQueryXmlWithSuppressions(
        IEnumerable<string> paths,
        EventFilter? select,
        IEnumerable<EventFilter> suppressions) {

        return BuildQueryXml(
            paths,
            filePaths: true,
            select,
            NormalizeSuppressions(suppressions));
    }

    private static string BuildQueryXml(
        IEnumerable<string> sources,
        bool filePaths,
        EventFilter? select,
        IReadOnlyList<EventFilter> suppressions) {

        if (sources == null) {
            throw new ArgumentNullException(nameof(sources));
        }
        string[] normalizedSources = sources
            .Select(source => source?.Trim() ?? string.Empty)
            .Where(static source => source.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (normalizedSources.Length == 0) {
            throw new ArgumentException(
                "At least one event source is required.",
                nameof(sources));
        }

        EventFilter? namedDataSuppression =
            CreateExcludedNamedDataSuppression(select);
        EventFilter? normalizedSelect =
            WithoutExcludedNamedData(select);
        var combinedSuppressions =
            new List<EventFilter>(suppressions);
        if (namedDataSuppression != null) {
            combinedSuppressions.Add(
                namedDataSuppression);
        }
        IReadOnlyList<EventFilter> normalizedSuppressions =
            NormalizeSuppressions(combinedSuppressions);
        string selectXPath = BuildXPath(normalizedSelect);
        string[] suppressXPaths = normalizedSuppressions
            .Select(BuildXPath)
            .Where(static xpath => xpath != "*")
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var builder = new StringBuilder();
        var settings = new XmlWriterSettings {
            OmitXmlDeclaration = true,
            Indent = false
        };
        using (XmlWriter writer = XmlWriter.Create(builder, settings)) {
            writer.WriteStartElement("QueryList");
            for (int index = 0; index < normalizedSources.Length; index++) {
                string source = filePaths
                    ? "file://" + Path.GetFullPath(normalizedSources[index])
                    : normalizedSources[index];
                writer.WriteStartElement("Query");
                writer.WriteAttributeString(
                    "Id",
                    index.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("Path", source);
                writer.WriteStartElement("Select");
                writer.WriteAttributeString("Path", source);
                writer.WriteString(selectXPath);
                writer.WriteEndElement();
                foreach (string suppressXPath in suppressXPaths) {
                    writer.WriteStartElement("Suppress");
                    writer.WriteAttributeString("Path", source);
                    writer.WriteString(suppressXPath);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
        return builder.ToString();
    }

    private static IReadOnlyList<EventFilter> NormalizeSuppressions(
        IEnumerable<EventFilter> suppressions) {

        if (suppressions == null) {
            throw new ArgumentNullException(nameof(suppressions));
        }
        EventFilter[] normalized = suppressions
            .Where(static filter => filter != null && filter.HasAny)
            .ToArray();
        if (normalized.Any(HasExcludedNamedData)) {
            throw new ArgumentException(
                "A suppression filter cannot itself contain ExcludedNamedData. Put excluded named values on the select filter so they can be translated into a positive Suppress clause.",
                nameof(suppressions));
        }
        if (normalized.Any(static filter =>
                CountExpressions(filter) > MaximumXPathExpressions)) {
            throw new ArgumentException(
                $"Every suppression partition must fit within {MaximumXPathExpressions} native XPath expressions.",
                nameof(suppressions));
        }
        return normalized;
    }

    internal static bool HasExcludedNamedData(
        EventFilter? filter) {

        return (filter?.ExcludedNamedData?.Count ?? 0) > 0;
    }

    /// <summary>
    /// Separates named-data exclusions from a typed selection filter.
    /// Windows represents the exclusions as a positive QueryList Suppress
    /// filter because raw XPath inequality drops events where the field is absent.
    /// </summary>
    /// <param name="filter">Typed source filter.</param>
    /// <param name="select">Equivalent selection filter without named-data exclusions.</param>
    /// <param name="suppress">Positive named-data filter to emit as a Suppress clause, or null.</param>
    public static void SplitNamedDataExclusions(
        EventFilter? filter,
        out EventFilter? select,
        out EventFilter? suppress) {

        suppress =
            CreateExcludedNamedDataSuppression(
                filter);
        select =
            WithoutExcludedNamedData(
                filter);
    }

    internal static EventFilter? CreateExcludedNamedDataSuppression(
        EventFilter? filter) {

        return HasExcludedNamedData(filter)
            ? new EventFilter {
                NamedData = filter!.ExcludedNamedData
            }
            : null;
    }

    internal static EventFilter? WithoutExcludedNamedData(
        EventFilter? source) {

        if (source == null ||
            !HasExcludedNamedData(source)) {
            return source;
        }
        return new EventFilter {
            EventIds = source.EventIds,
            RecordIds = source.RecordIds,
            MinimumRecordIdExclusive =
                source.MinimumRecordIdExclusive,
            MaximumRecordIdExclusive =
                source.MaximumRecordIdExclusive,
            ProviderNames = source.ProviderNames,
            Levels = source.Levels,
            Keywords = source.Keywords,
            StartTime = source.StartTime,
            EndTime = source.EndTime,
            UserIds = source.UserIds,
            Data = source.Data,
            NamedData = source.NamedData,
            ExcludedEventIds = source.ExcludedEventIds
        };
    }

    private static string[]? ToInvariantStrings<T>(
        IReadOnlyList<T>? values) where T : struct, IFormattable {

        return values?.Count > 0
            ? values.Select(static value =>
                value.ToString(null, CultureInfo.InvariantCulture)).ToArray()
            : null;
    }

    private static string[]? NormalizeStrings(
        IReadOnlyList<string>? values) {

        if (values == null || values.Count == 0) {
            return null;
        }
        string[] normalized = values
            .Select(static value => value?.Trim() ?? string.Empty)
            .Where(static value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return normalized.Length == 0 ? null : normalized;
    }

    private static Hashtable[]? ToHashtables(
        IReadOnlyDictionary<string, IReadOnlyList<string>>? values) {

        if (values == null || values.Count == 0) {
            return null;
        }
        var table = new Hashtable(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, IReadOnlyList<string>> entry in values) {
            table[entry.Key] = entry.Value?.ToArray() ?? Array.Empty<string>();
        }
        return new[] { table };
    }
}
