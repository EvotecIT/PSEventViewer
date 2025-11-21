using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Xml.Linq;

namespace EventViewerX {
    public partial class SearchEvents {
        public static IEnumerable<PowerShellScriptExecutionInfo> GetPowerShellScriptExecution(
            PowerShellEdition type,
            string machineName = null,
            string eventLogPath = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null) {
            string logName = type == PowerShellEdition.WindowsPowerShell
                ? "Microsoft-Windows-PowerShell/Operational"
                : "PowerShellCore/Operational";

            string queryString = BuildWinEventFilter(
                id: new[] { "4100" },
                startTime: dateFrom,
                endTime: dateTo,
                logName: logName,
                path: eventLogPath,
                xpathOnly: false);

            EventLogSession? session = null;
            EventLogQuery query = string.IsNullOrEmpty(eventLogPath)
                ? new EventLogQuery(logName, PathType.LogName, queryString)
                : new EventLogQuery(null, PathType.LogName, queryString);
            if (!string.IsNullOrEmpty(machineName)) {
                session = CreateSession(machineName, "PowerShellScripts", logName, DefaultSessionTimeoutMs);
                if (session == null) yield break;
                query.Session = session;
            }

            try {
                using EventLogReader reader = CreateEventLogReader(query, machineName, DefaultSessionTimeoutMs);
                if (reader == null) {
                    yield break;
                }

                while (true) {
                    EventRecord? record = null;
                    try {
                        var readTask = Task.Run(() => reader.ReadEvent(TimeSpan.FromMilliseconds(750)));
                        var completed = Task.WhenAny(readTask, Task.Delay(DefaultSessionTimeoutMs)).GetAwaiter().GetResult();
                        if (completed != readTask) break;
                        record = readTask.GetAwaiter().GetResult();
                    } catch (EventLogException ex) {
                        _logger.WriteWarning($"PowerShellScripts: error reading log on {machineName ?? GetFQDN()}: {ex.Message}");
                        break;
                    }

                    if (record == null) break;

                    var element = XElement.Parse(record.ToXml());
                    string contextInfo = ExtractData(element, "ContextInfo");
                    var data = ParseContextInfo(contextInfo);
                    yield return new PowerShellScriptExecutionInfo(record, data);
                }
            }
            finally {
                session?.Dispose();
            }
        }

        public static IEnumerable<RestoredPowerShellScript> RestorePowerShellScripts(
            PowerShellEdition type,
            string machineName = null,
            string eventLogPath = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            bool format = false,
            IEnumerable<string> containsText = null) {
            string logName = type == PowerShellEdition.WindowsPowerShell
                ? "Microsoft-Windows-PowerShell/Operational"
                : "PowerShellCore/Operational";

            string queryString = BuildWinEventFilter(
                id: new[] { "4103", "4104" },
                startTime: dateFrom,
                endTime: dateTo,
                logName: logName,
                path: eventLogPath,
                xpathOnly: false);

            EventLogSession? session = null;
            EventLogQuery query = string.IsNullOrEmpty(eventLogPath)
                ? new EventLogQuery(logName, PathType.LogName, queryString)
                : new EventLogQuery(null, PathType.LogName, queryString);
            if (!string.IsNullOrEmpty(machineName)) {
                session = CreateSession(machineName, "PowerShellScripts", logName, DefaultSessionTimeoutMs);
                if (session == null) yield break;
                query.Session = session;
            }

            try {
                using EventLogReader reader = CreateEventLogReader(query, machineName, DefaultSessionTimeoutMs);
                if (reader == null) {
                    yield break;
                }

                var cache = new Dictionary<string, ScriptCache>();
                while (true) {
                    EventRecord? record = null;
                    try {
                        var readTask = Task.Run(() => reader.ReadEvent(TimeSpan.FromMilliseconds(750)));
                        var completed = Task.WhenAny(readTask, Task.Delay(DefaultSessionTimeoutMs)).GetAwaiter().GetResult();
                        if (completed != readTask) break;
                        record = readTask.GetAwaiter().GetResult();
                    } catch (EventLogException ex) {
                        _logger.WriteWarning($"PowerShellScripts: error reading log on {machineName ?? GetFQDN()}: {ex.Message}");
                        break;
                    }

                    if (record == null) break;

                    var element = XElement.Parse(record.ToXml());
                    string scriptText = ExtractData(element, "ScriptBlockText");
                    if (string.IsNullOrEmpty(scriptText) || scriptText == "0") {
                        continue;
                    }
                    string scriptId = ExtractData(element, "ScriptBlockId");
                    if (scriptId == null) {
                        continue;
                    }
                    string messageNumber = ExtractData(element, "MessageNumber") ?? "0";
                    if (!cache.TryGetValue(scriptId, out var inner)) {
                        inner = new ScriptCache();
                        cache[scriptId] = inner;
                    }
                    inner.Events.Add(record);
                    if (messageNumber == "0") {
                        inner.MetaRecord = record;
                    } else if (int.TryParse(messageNumber, out int num)) {
                        inner.Parts[num] = scriptText;
                    }
                }

                foreach (var kv in cache) {
                    var metaRecord = kv.Value.MetaRecord ?? kv.Value.Events[0];
                    var metaElement = XElement.Parse(metaRecord.ToXml());
                    string totalStr = ExtractData(metaElement, "MessageTotal") ?? "0";
                    if (!int.TryParse(totalStr, out int total)) total = 0;
                    var sb = new StringBuilder();
                    for (int i = 1; i <= total; i++) {
                        if (kv.Value.Parts.TryGetValue(i, out var part)) {
                            sb.Append(part);
                        }
                    }
                    string script = sb.ToString();
                    if (containsText != null && containsText.Any()) {
                        bool all = true;
                        foreach (var term in containsText) {
                            if (term == null) continue;
                            if (script.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0) {
                                all = false;
                                break;
                            }
                        }
                        if (!all) {
                            continue;
                        }
                    }
                    if (format) {
                        script = FormatScript(script);
                    }
                    yield return new RestoredPowerShellScript {
                        ScriptBlockId = kv.Key,
                        Script = script,
                        Events = kv.Value.Events.AsReadOnly(),
                        Data = GetAllData(metaElement)
                    };
                }
            }
            finally {
                session?.Dispose();
            }
        }

        private sealed class ScriptCache {
            public EventRecord MetaRecord { get; set; }
            public List<EventRecord> Events { get; } = new List<EventRecord>();
            public Dictionary<int, string> Parts { get; } = new Dictionary<int, string>();
        }
    }
}
