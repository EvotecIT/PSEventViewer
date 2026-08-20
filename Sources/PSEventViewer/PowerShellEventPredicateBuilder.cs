namespace PSEventViewer;

/// <summary>PowerShell-friendly view over the canonical EventViewerX typed predicate builder.</summary>
public sealed class PowerShellEventPredicateBuilder {
    private readonly EventPredicateBuilder _builder;

    internal PowerShellEventPredicateBuilder(EventPredicateBuilder builder) {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        var fields = new PSObject();
        foreach (EventPredicateField field in builder.Fields) {
            fields.Properties.Add(new PSNoteProperty(field.Name, field));
            foreach (string alias in field.Definition.Aliases) {
                if (fields.Properties[alias] == null) {
                    fields.Properties.Add(new PSAliasProperty(alias, field.Name));
                }
            }
        }
        Fields = fields;
    }

    /// <summary>Stable definition name.</summary>
    public string DefinitionName => _builder.DefinitionName;

    /// <summary>Human-friendly definition label.</summary>
    public string DisplayName => _builder.DisplayName;

    /// <summary>Tab-discoverable typed field builders.</summary>
    public PSObject Fields { get; }

    /// <summary>Gets one field by stable name or alias.</summary>
    public EventPredicateField Field(string name) => _builder.Field(name);

    /// <summary>Requires every predicate to match.</summary>
    public EventPredicate AllOf(params EventPredicate[] predicates) => _builder.AllOf(predicates);

    /// <summary>Requires at least one predicate to match.</summary>
    public EventPredicate AnyOf(params EventPredicate[] predicates) => _builder.AnyOf(predicates);

    /// <summary>Negates one predicate.</summary>
    public EventPredicate Not(EventPredicate predicate) => _builder.Not(predicate);
}
