namespace PSEventViewer;

/// <summary>Converts PowerShell predicate inputs into the canonical EventViewerX model.</summary>
internal static class PowerShellEventPredicateAdapter {
    internal static EventPredicate? Resolve(object? value, string parameterName) {
        while (value is PSObject wrapper && wrapper.BaseObject != value) {
            value = wrapper.BaseObject;
        }
        if (value == null) {
            return null;
        }
        EventPredicate predicate = value switch {
            EventPredicate typed => typed,
            ScriptBlock scriptBlock => PowerShellEventPredicateAstParser.Parse(scriptBlock),
            string text when File.Exists(text) => EventPredicate.Load(text),
            string text => EventPredicate.ParseJson(text),
            _ => throw new PSArgumentException(
                $"{parameterName} must be an EventPredicate, a restricted ScriptBlock, predicate JSON, or a predicate JSON file path.",
                parameterName)
        };
        predicate.Validate();
        return predicate;
    }
}
