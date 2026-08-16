namespace EventViewerX.Reporting;

internal static class EventReportSectionBuilder {
    internal static IReadOnlyList<EventReportSection> Build(IReadOnlyList<EventReportProjection> projections) {
        var orderedKeys = new List<string>();
        var groups = new Dictionary<string, List<EventReportProjection>>(StringComparer.OrdinalIgnoreCase);
        foreach (EventReportProjection projection in projections) {
            if (!groups.TryGetValue(projection.Section.Key, out List<EventReportProjection>? group)) {
                group = new List<EventReportProjection>();
                groups.Add(projection.Section.Key, group);
                orderedKeys.Add(projection.Section.Key);
            }
            group.Add(projection);
        }

        var sections = new List<EventReportSection>(orderedKeys.Count);
        foreach (string key in orderedKeys) {
            List<EventReportProjection> group = groups[key];
            EventReportSectionDefinition definition = group[0].Section;
            IReadOnlyList<EventReportColumn> columns = definition.Kind == EventReportSectionKind.Generic
                ? EventReportTableProjection.BuildGenericColumns(group.Select(static item => item.Row))
                : definition.Columns;
            sections.Add(new EventReportSection(
                definition.Name,
                definition.DisplayName,
                definition.Description,
                definition.Kind,
                columns,
                group.Select(static item => item.Row).ToArray()));
        }
        return sections;
    }
}
