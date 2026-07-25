namespace EventViewerX;

internal static class EventIdValidation {
    internal static List<int> Normalize(
        IEnumerable<int> eventIds,
        string parameterName) {

        if (eventIds == null) {
            throw new ArgumentNullException(parameterName);
        }

        var normalized = new HashSet<int>();
        foreach (int eventId in eventIds) {
            if (eventId < 0 ||
                eventId > ushort.MaxValue) {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    eventId,
                    $"Event IDs must be between 0 and {ushort.MaxValue}.");
            }
            normalized.Add(eventId);
        }
        return normalized
            .OrderBy(static eventId => eventId)
            .ToList();
    }
}
