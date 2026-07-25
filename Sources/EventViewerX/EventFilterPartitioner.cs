namespace EventViewerX;

/// <summary>
/// Partitions large typed filters into an equivalent union of native XPath filters.
/// Windows permits at most 22 expressions in one event-log XPath.
/// </summary>
public static class EventFilterPartitioner {
    /// <summary>
    /// Splits OR-valued dimensions while preserving AND semantics between dimensions.
    /// </summary>
    public static IReadOnlyList<EventFilter> Partition(
        EventFilter filter) {

        if (filter == null) {
            throw new ArgumentNullException(nameof(filter));
        }
        var dimensions = new[] {
            new Dimension(
                FilterDimension.EventIds,
                filter.EventIds?.Count ?? 0),
            new Dimension(
                FilterDimension.RecordIds,
                filter.RecordIds?.Count ?? 0),
            new Dimension(
                FilterDimension.ProviderNames,
                filter.ProviderNames?.Count ?? 0),
            new Dimension(
                FilterDimension.Levels,
                filter.Levels?.Count ?? 0),
            new Dimension(
                FilterDimension.UserIds,
                filter.UserIds?.Count ?? 0),
            new Dimension(
                FilterDimension.Data,
                filter.Data?.Count ?? 0)
        }.Where(static dimension => dimension.Count > 0)
            .ToArray();
        int splittableExpressions =
            dimensions.Sum(static dimension => dimension.Count);
        int fixedExpressions =
            EventFilterCompiler.CountExpressions(filter) -
            splittableExpressions;
        int available =
            EventFilterCompiler.MaximumXPathExpressions -
            fixedExpressions;
        if (available < dimensions.Length) {
            throw new ArgumentException(
                $"The fixed filter requires {fixedExpressions} native XPath expressions, leaving {available} for {dimensions.Length} non-empty OR dimensions. Windows Event Log permits {EventFilterCompiler.MaximumXPathExpressions} expressions.");
        }

        AllocateCapacities(dimensions, available);
        var partitions = new List<EventFilter> {
            CopyWithoutSplittableDimensions(filter)
        };
        foreach (Dimension dimension in dimensions) {
            partitions = ApplyDimension(
                partitions,
                filter,
                dimension);
        }
        foreach (EventFilter partition in partitions) {
            int expressions =
                EventFilterCompiler.CountExpressions(partition);
            if (expressions >
                EventFilterCompiler.MaximumXPathExpressions) {
                throw new InvalidOperationException(
                    $"Internal filter partitioning produced {expressions} expressions.");
            }
        }
        return partitions;
    }

    internal static IReadOnlyList<EventFilter>
        PartitionNamedDataSuppression(
            EventFilter? suppression) {

        if (suppression?.NamedData == null ||
            suppression.NamedData.Count == 0) {
            return Array.Empty<EventFilter>();
        }
        var atoms = new List<NamedDataAtom>();
        foreach (KeyValuePair<string, IReadOnlyList<string>> entry in
                 suppression.NamedData) {
            if (entry.Value == null || entry.Value.Count == 0) {
                atoms.Add(new NamedDataAtom(
                    entry.Key,
                    value: null,
                    expressionCount: 1));
                continue;
            }
            foreach (string value in entry.Value) {
                atoms.Add(new NamedDataAtom(
                    entry.Key,
                    value,
                    expressionCount: 2));
            }
        }

        var partitions = new List<EventFilter>();
        var values =
            new Dictionary<string, List<string>>(
                StringComparer.OrdinalIgnoreCase);
        var existenceKeys = new List<string>();
        int expressions = 0;
        foreach (NamedDataAtom atom in atoms) {
            if (expressions > 0 &&
                expressions + atom.ExpressionCount >
                EventFilterCompiler.MaximumXPathExpressions) {
                partitions.Add(
                    CreateNamedDataPartition(
                        values,
                        existenceKeys));
                values = new Dictionary<string, List<string>>(
                    StringComparer.OrdinalIgnoreCase);
                existenceKeys = new List<string>();
                expressions = 0;
            }
            if (atom.Value == null) {
                existenceKeys.Add(atom.Key);
            } else {
                if (!values.TryGetValue(
                        atom.Key,
                        out List<string>? keyValues)) {
                    keyValues = new List<string>();
                    values.Add(atom.Key, keyValues);
                }
                keyValues.Add(atom.Value);
            }
            expressions += atom.ExpressionCount;
        }
        if (expressions > 0) {
            partitions.Add(
                CreateNamedDataPartition(
                    values,
                    existenceKeys));
        }
        return partitions;
    }

    private static EventFilter CreateNamedDataPartition(
        IReadOnlyDictionary<string, List<string>> values,
        IReadOnlyList<string> existenceKeys) {

        var namedData =
            new Dictionary<string, IReadOnlyList<string>>(
                StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, List<string>> entry in values) {
            namedData[entry.Key] = entry.Value.ToArray();
        }
        foreach (string key in existenceKeys) {
            namedData[key] = Array.Empty<string>();
        }
        return new EventFilter {
            NamedData = namedData
        };
    }

    private static void AllocateCapacities(
        IReadOnlyList<Dimension> dimensions,
        int available) {

        foreach (Dimension dimension in dimensions) {
            dimension.Capacity = 1;
        }
        int remaining = available - dimensions.Count;
        while (remaining > 0) {
            Dimension? selected = dimensions
                .Where(static dimension =>
                    dimension.Capacity < dimension.Count)
                .OrderByDescending(static dimension =>
                    (double)dimension.Count /
                    dimension.Capacity)
                .ThenBy(static dimension => dimension.Kind)
                .FirstOrDefault();
            if (selected == null) {
                break;
            }
            selected.Capacity++;
            remaining--;
        }
    }

    private static List<EventFilter> ApplyDimension(
        IReadOnlyList<EventFilter> current,
        EventFilter source,
        Dimension dimension) {

        int chunkCount =
            (dimension.Count + dimension.Capacity - 1) /
            dimension.Capacity;
        var output = new List<EventFilter>(
            checked(current.Count * chunkCount));
        foreach (EventFilter partial in current) {
            for (int offset = 0;
                 offset < dimension.Count;
                 offset += dimension.Capacity) {
                int count = Math.Min(
                    dimension.Capacity,
                    dimension.Count - offset);
                output.Add(WithDimension(
                    partial,
                    source,
                    dimension.Kind,
                    offset,
                    count));
            }
        }
        return output;
    }

    private static EventFilter WithDimension(
        EventFilter partial,
        EventFilter source,
        FilterDimension dimension,
        int offset,
        int count) {

        var result = Copy(partial);
        switch (dimension) {
            case FilterDimension.EventIds:
                result.EventIds = source.EventIds!
                    .Skip(offset)
                    .Take(count)
                    .ToArray();
                break;
            case FilterDimension.RecordIds:
                result.RecordIds = source.RecordIds!
                    .Skip(offset)
                    .Take(count)
                    .ToArray();
                break;
            case FilterDimension.ProviderNames:
                result.ProviderNames = source.ProviderNames!
                    .Skip(offset)
                    .Take(count)
                    .ToArray();
                break;
            case FilterDimension.Levels:
                result.Levels = source.Levels!
                    .Skip(offset)
                    .Take(count)
                    .ToArray();
                break;
            case FilterDimension.UserIds:
                result.UserIds = source.UserIds!
                    .Skip(offset)
                    .Take(count)
                    .ToArray();
                break;
            case FilterDimension.Data:
                result.Data = source.Data!
                    .Skip(offset)
                    .Take(count)
                    .ToArray();
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(dimension));
        }
        return result;
    }

    private static EventFilter CopyWithoutSplittableDimensions(
        EventFilter source) {

        EventFilter copy = Copy(source);
        copy.EventIds = null;
        copy.RecordIds = null;
        copy.ProviderNames = null;
        copy.Levels = null;
        copy.UserIds = null;
        copy.Data = null;
        return copy;
    }

    private static EventFilter Copy(EventFilter source) {
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
            ExcludedNamedData = source.ExcludedNamedData,
            ExcludedEventIds = source.ExcludedEventIds
        };
    }

    private enum FilterDimension {
        EventIds,
        RecordIds,
        ProviderNames,
        Levels,
        UserIds,
        Data
    }

    private sealed class Dimension {
        internal Dimension(
            FilterDimension kind,
            int count) {

            Kind = kind;
            Count = count;
        }

        internal FilterDimension Kind { get; }
        internal int Count { get; }
        internal int Capacity { get; set; }
    }

    private sealed class NamedDataAtom {
        internal NamedDataAtom(
            string key,
            string? value,
            int expressionCount) {

            Key = key;
            Value = value;
            ExpressionCount = expressionCount;
        }

        internal string Key { get; }
        internal string? Value { get; }
        internal int ExpressionCount { get; }
    }
}
