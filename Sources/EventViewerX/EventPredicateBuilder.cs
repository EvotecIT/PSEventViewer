namespace EventViewerX;

/// <summary>
/// Type-aware builder that exposes the fields available to one built-in or custom event definition.
/// </summary>
public sealed class EventPredicateBuilder {
    private readonly IReadOnlyDictionary<string, EventPredicateField> _fields;

    private EventPredicateBuilder(
        string definitionName,
        string displayName,
        IEnumerable<EventFieldDefinition> fields) {

        DefinitionName = definitionName;
        DisplayName = displayName;
        EventPredicateField[] values = fields
            .Where(static field => field.IsFilterable)
            .Select(static field => new EventPredicateField(field))
            .ToArray();
        Fields = values;
        var fieldLookup = new Dictionary<string, EventPredicateField>(StringComparer.OrdinalIgnoreCase);
        foreach (EventPredicateField field in values) {
            fieldLookup[field.Name] = field;
            foreach (string alias in field.Definition.Aliases) {
                if (!string.IsNullOrWhiteSpace(alias)) {
                    string normalizedAlias = alias.Trim();
                    if (!fieldLookup.ContainsKey(normalizedAlias)) {
                        fieldLookup[normalizedAlias] = field;
                    }
                }
            }
        }
        _fields = fieldLookup;
    }

    /// <summary>Stable definition name.</summary>
    public string DefinitionName { get; }

    /// <summary>Human-friendly definition label.</summary>
    public string DisplayName { get; }

    /// <summary>Filterable fields in stable projection order.</summary>
    public IReadOnlyList<EventPredicateField> Fields { get; }

    /// <summary>Creates a builder for one built-in event type.</summary>
    public static EventPredicateBuilder ForType(EventType type) {
        EventTypeDefinition definition = EventTypeCatalog.GetDefinition(type);
        if (definition.IsComposite) {
            IReadOnlyList<EventFieldDefinition> compositeFields = EventTypeCatalog
                .Expand(new[] { type })
                .Select(EventTypeCatalog.GetDefinition)
                .SelectMany(static item => item.Fields)
                .GroupBy(static field => field.Name, StringComparer.OrdinalIgnoreCase)
                .Select(static group => group.First())
                .ToArray();
            return new EventPredicateBuilder(definition.Name, definition.DisplayName, compositeFields);
        }
        return new EventPredicateBuilder(definition.Name, definition.DisplayName, definition.Fields);
    }

    /// <summary>Creates a builder for one declarative custom definition.</summary>
    public static EventPredicateBuilder ForDefinition(EventDefinition definition) {
        if (definition == null) {
            throw new ArgumentNullException(nameof(definition));
        }
        definition.Validate();
        EventFieldDefinition[] customFields = definition.Fields.Select(
            static field => new EventFieldDefinition(
                field.Name,
                string.IsNullOrWhiteSpace(field.DisplayName) ? field.Name : field.DisplayName,
                ResolveType(field.ValueKind),
                isCommon: false,
                field.Description,
                field.Aliases,
                field.Source == EventFieldSource.Metadata &&
                field.DefaultValue == null &&
                EventFieldDefinition.IsNativeField(field.SourceName)
                    ? EventFieldFilterStage.Native
                    : EventFieldFilterStage.Managed)).ToArray();
        var claimedCommonNames = new HashSet<string>(
            customFields.SelectMany(static field => field.Aliases.Prepend(field.Name)),
            StringComparer.OrdinalIgnoreCase);
        IEnumerable<EventFieldDefinition> fields = customFields
            .Concat(EventTypeCatalog.GetCommonFields().Where(field => !claimedCommonNames.Contains(field.Name)))
            .GroupBy(static field => field.Name, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First());
        return new EventPredicateBuilder(definition.Name, definition.DisplayName, fields);
    }

    /// <summary>Gets one field by stable name, ignoring case.</summary>
    public EventPredicateField Field(string name) {
        if (string.IsNullOrWhiteSpace(name) || !_fields.TryGetValue(name.Trim(), out EventPredicateField? field)) {
            throw new ArgumentException(
                $"Field '{name}' is not available for definition '{DefinitionName}'. " +
                $"Available fields: {string.Join(", ", Fields.Select(static item => item.Name))}.",
                nameof(name));
        }
        return field;
    }

    /// <summary>Requires every predicate to match.</summary>
    public EventPredicate AllOf(params EventPredicate[] predicates) =>
        EventPredicate.AllOf(predicates);

    /// <summary>Requires at least one predicate to match.</summary>
    public EventPredicate AnyOf(params EventPredicate[] predicates) =>
        EventPredicate.AnyOf(predicates);

    /// <summary>Negates one predicate.</summary>
    public EventPredicate Not(EventPredicate predicate) => EventPredicate.Not(predicate);

    private static Type ResolveType(EventFieldValueKind kind) {
        return kind switch {
            EventFieldValueKind.Int32 => typeof(int),
            EventFieldValueKind.Int64 => typeof(long),
            EventFieldValueKind.Boolean => typeof(bool),
            EventFieldValueKind.DateTime => typeof(DateTime),
            EventFieldValueKind.Guid => typeof(Guid),
            EventFieldValueKind.IpAddress => typeof(System.Net.IPAddress),
            _ => typeof(string)
        };
    }
}
