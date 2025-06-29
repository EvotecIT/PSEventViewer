using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
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

            EventLogQuery query = string.IsNullOrEmpty(eventLogPath)
                ? new EventLogQuery(logName, PathType.LogName, queryString)
                : new EventLogQuery(null, PathType.LogName, queryString);
            if (!string.IsNullOrEmpty(machineName)) {
                query.Session = new EventLogSession(machineName);
            }

            using EventLogReader reader = CreateEventLogReader(query, machineName);
            if (reader == null) {
                yield break;
            }

            EventRecord record;
            while ((record = reader.ReadEvent()) != null) {
                string contextInfo = ExtractData(record, "ContextInfo");
                var data = ParseContextInfo(contextInfo);
                yield return new PowerShellScriptExecutionInfo(record, data);
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

            EventLogQuery query = string.IsNullOrEmpty(eventLogPath)
                ? new EventLogQuery(logName, PathType.LogName, queryString)
                : new EventLogQuery(null, PathType.LogName, queryString);
            if (!string.IsNullOrEmpty(machineName)) {
                query.Session = new EventLogSession(machineName);
            }

            using EventLogReader reader = CreateEventLogReader(query, machineName);
            if (reader == null) {
                yield break;
            }

            var cache = new Dictionary<string, Dictionary<string, object>>();
            EventRecord record;
            while ((record = reader.ReadEvent()) != null) {
                string scriptText = ExtractData(record, "ScriptBlockText");
                if (string.IsNullOrEmpty(scriptText) || scriptText == "0") {
                    continue;
                }
                string scriptId = ExtractData(record, "ScriptBlockId");
                if (scriptId == null) {
                    continue;
                }
                string messageNumber = ExtractData(record, "MessageNumber") ?? "0";
                if (!cache.TryGetValue(scriptId, out var inner)) {
                    inner = new Dictionary<string, object>();
                    cache[scriptId] = inner;
                }
                inner["0"] = record;
                inner[messageNumber] = scriptText;
            }

            foreach (var kv in cache) {
                var metaRecord = (EventRecord)kv.Value["0"];
                string totalStr = ExtractData(metaRecord, "MessageTotal") ?? "0";
                if (!int.TryParse(totalStr, out int total)) total = 0;
                var sb = new StringBuilder();
                for (int i = 1; i <= total; i++) {
                    if (kv.Value.TryGetValue(i.ToString(), out var partObj) && partObj is string part) {
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
                    EventRecord = metaRecord,
                    Data = GetAllData(metaRecord)
                };
            }
        }

        private static Dictionary<string, string?> ParseContextInfo(string context) {
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(context)) {
                return result;
            }
            var lines = context.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines) {
                var parts = line.Split(new[] { '=' }, 2);
                var key = parts[0].Trim().Replace(" ", string.Empty);
                var value = parts.Length > 1 ? parts[1].Trim() : null;
                result[key] = value;
            }
            return result;
        }

        private static string ExtractData(EventRecord record, string name) {
            try {
                var element = XElement.Parse(record.ToXml());
                XNamespace ns = element.GetDefaultNamespace();
                return element.Descendants(ns + "Data")
                    .FirstOrDefault(e => (string)e.Attribute("Name") == name)?.Value;
            } catch {
                return null;
            }
        }

        private static Dictionary<string, string?> GetAllData(EventRecord record) {
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            try {
                var element = XElement.Parse(record.ToXml());
                XNamespace ns = element.GetDefaultNamespace();
                foreach (var data in element.Descendants(ns + "Data")) {
                    var key = (string)data.Attribute("Name");
                    if (key != null) {
                        result[key] = data.Value;
                    }
                }
            } catch {
            }
            return result;
        }

        private static string FormatScript(string script) {
            var sb = new StringBuilder();
            int indent = 0;
            var lines = script.Replace("\r", string.Empty).Split('\n');
            foreach (var raw in lines) {
                var line = raw.Trim();
                if (line.StartsWith("}")) {
                    indent = Math.Max(0, indent - 4);
                }
                sb.Append(' ', indent);
                sb.AppendLine(line);
                if (line.EndsWith("{")) {
                    indent += 4;
                }
            }
            return sb.ToString();
        }
    }
}
