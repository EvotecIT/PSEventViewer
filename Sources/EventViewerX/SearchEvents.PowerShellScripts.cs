using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace EventViewerX {
    /// <summary>
    /// Methods for working with PowerShell script execution events.
    /// </summary>
    public partial class SearchEvents {

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
                if (record == null) {
                    throw new ArgumentNullException(nameof(record));
                }
                var element = XElement.Parse(record.ToXml());
                return ExtractData(element, name);
            } catch (Exception ex) {
                Settings._logger.WriteWarning($"Failed extracting '{name}' data. Exception: {ex}");
                return null;
            }
        }

        private static string ExtractData(XElement element, string name) {
            XNamespace ns = element.GetDefaultNamespace();
            return element.Descendants(ns + "Data")
                .FirstOrDefault(e => (string)e.Attribute("Name") == name)?.Value;
        }

        private static Dictionary<string, string?> GetAllData(EventRecord record) {
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            if (record == null) {
                Settings._logger.WriteWarning("Failed parsing event data. EventRecord is null.");
                return result;
            }

            try {
                var element = XElement.Parse(record.ToXml());
                return GetAllData(element);
            } catch (Exception ex) {
                Settings._logger.WriteWarning($"Failed parsing event data. Error: {ex.Message}");
                return result;
            }
        }

        private static Dictionary<string, string?> GetAllData(XElement element) {
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            XNamespace ns = element.GetDefaultNamespace();
            foreach (var data in element.Descendants(ns + "Data")) {
                var key = (string)data.Attribute("Name");
                if (key != null) {
                    result[key] = data.Value;
                }
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

        public static IEnumerable<RestoredPowerShellScript> GetPowerShellScripts(
            PowerShellEdition type,
            string machineName = null,
            string eventLogPath = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            bool format = false,
            IEnumerable<string> containsText = null) {
            return RestorePowerShellScripts(type, machineName, eventLogPath, dateFrom, dateTo, format, containsText);
        }
    }
}
