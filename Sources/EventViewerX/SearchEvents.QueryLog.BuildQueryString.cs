using System.Text;

namespace EventViewerX;

public partial class SearchEvents : Settings {
    private static string BuildQueryString(List<long> eventRecordIds) {
        if (eventRecordIds != null) {
            var validIds = eventRecordIds.Where(id => id > 0).ToList();
            if (validIds.Any()) {
                return $"*[System[{string.Join(" or ", validIds.Select(id => $"EventRecordID={id}"))}]]";
            }
        }
        return "*";
    }

    private static string BuildQueryString(string logName, List<int> eventIds = null, string providerName = null, Keywords? keywords = null, Level? level = null, DateTime? startTime = null, DateTime? endTime = null, string userId = null, List<int> tasks = null, List<int> opcodes = null, TimePeriod? timePeriod = null) {
        TimeSpan? lastPeriod = null;
        if (timePeriod.HasValue) {
            var times = TimeHelper.GetTimePeriod(timePeriod.Value);
            startTime = times.StartTime;
            endTime = times.EndTime;
            lastPeriod = times.LastPeriod;
            _logger.WriteVerbose($"Time period: {timePeriod}, time start: {startTime}, time end: {endTime}, lastPeriod: {lastPeriod}");
        }

        StringBuilder queryString = new StringBuilder($"<QueryList><Query Id='0' Path='{logName}'><Select Path='{logName}'>*[System[");

        if (eventIds != null) {
            var validIds = eventIds.Where(id => id > 0).ToList();
            if (validIds.Any()) {
                AddCondition(queryString, "(" + string.Join(" or ", validIds.Select(id => $"EventID={id}")) + ")");
            }
        }

        if (!string.IsNullOrEmpty(providerName)) {
            var escaped = EscapeXPathValue(providerName);
            AddCondition(queryString, $"Provider[@Name='{escaped}']");
        }

        if (keywords.HasValue) {
            AddCondition(queryString, $"band(Keywords,{(long)keywords.Value})");
        }

        if (level.HasValue) {
            AddCondition(queryString, $"Level={(int)level.Value}");
        }

        if (tasks != null && tasks.Any()) {
            AddCondition(queryString, "(" + string.Join(" or ", tasks.Select(task => $"Task={task}")) + ")");
        }

        if (opcodes != null && opcodes.Any()) {
            AddCondition(queryString, "(" + string.Join(" or ", opcodes.Select(opcode => $"Opcode={opcode}")) + ")");
        }

        if (lastPeriod != null) {
            AddCondition(queryString, $"TimeCreated[timediff(@SystemTime) <= {lastPeriod.Value.TotalMilliseconds}]");
        } else {
            if (startTime.HasValue && endTime.HasValue) {
                AddCondition(queryString, $"TimeCreated[@SystemTime>'{startTime.Value:s}Z' and @SystemTime<='{endTime.Value:s}Z']");
            } else if (startTime.HasValue) {
                AddCondition(queryString, $"TimeCreated[@SystemTime>'{startTime.Value:s}Z']");
            } else if (endTime.HasValue) {
                AddCondition(queryString, $"TimeCreated[@SystemTime<='{endTime.Value:s}Z']");
            }
        }

        if (!string.IsNullOrEmpty(userId)) {
            AddCondition(queryString, $"Security[@UserID='{userId}']");
        }

        if (queryString.ToString().EndsWith("[System[")) {
            queryString.Append("*");
        }

        queryString.Append("]]</Select></Query></QueryList>");

        return queryString.ToString();
    }
}
