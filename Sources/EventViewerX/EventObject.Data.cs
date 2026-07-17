using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace EventViewerX {
    public partial class EventObject {
        private static readonly string[] NewLineSeparators = { "\r\n", "\n" };

        private static string[] SplitMessageLines(string message) {
            if (string.IsNullOrEmpty(message)) {
                return Array.Empty<string>();
            }

            return message.Split(NewLineSeparators, StringSplitOptions.None);
        }

        /// <summary>
        /// Parses the message of the event record into a dictionary converting it into a key value pair
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private T ParseMessage<T>(string message) where T : IDictionary<string, string>, new() {
            message ??= string.Empty;
            T data = typeof(T) == typeof(Dictionary<string, string>)
                ? (T)(object)new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new();

            string[] lines = SplitMessageLines(message);
            int firstNonEmptyLineIndex = Array.FindIndex(lines, line => !string.IsNullOrWhiteSpace(line));
            if (firstNonEmptyLineIndex >= 0) {
                string firstLine = lines[firstNonEmptyLineIndex].Trim();
                data["Message"] = firstLine;
                MessageSubject = firstLine;
            }

            // Process remaining lines (after the subject) into key:value pairs.
            for (int i = firstNonEmptyLineIndex + 1; i < lines.Length; i++) {
                string line = lines[i].Trim();

                // Skip empty lines
                if (string.IsNullOrEmpty(line)) {
                    continue;
                }

                int colonIndex = line.IndexOf(':');
                if (colonIndex > 0) {
                    string key = line.Substring(0, colonIndex).Trim();
                    string value = line.Substring(colonIndex + 1).Trim();
                    if (!string.IsNullOrEmpty(key)) {
                        data[key] = value;
                    }
                }
            }

            return data;
        }

        /// <summary>
        /// Parses lines containing colon separated key/value pairs.
        /// </summary>
        /// <param name="text">Text to parse</param>
        /// <returns>Dictionary with parsed key value pairs</returns>
        private static Dictionary<string, string> ParseColonSeparatedLines(string text) {
            Dictionary<string, string> data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(text)) {
                return data;
            }

            string[] lines = text.Split(NewLineSeparators, StringSplitOptions.None);
            foreach (string rawLine in lines) {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) {
                    continue;
                }

                int index = line.IndexOf(':');
                if (index > -1) {
                    string key = line.Substring(0, index).Trim();
                    string value = line.Substring(index + 1).Trim();
                    if (!string.IsNullOrEmpty(key)) {
                        data[key] = value;
                    }
                }
            }

            return data;
        }

        /// <summary>
        /// Parses structured event data and binary attachments in one XML pass.
        /// </summary>
        /// <param name="xmlData">The XML data.</param>
        /// <param name="data">Parsed named event data.</param>
        /// <param name="attachments">Decoded binary attachments.</param>
        private static void ParseXmlPayload(
            string xmlData,
            out Dictionary<string, string> data,
            out List<byte[]> attachments) {

            data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            attachments = new List<byte[]>();

            XElement root;
            try {
                root = XElement.Parse(xmlData);
            } catch (Exception ex) {
                Settings._logger.WriteWarning($"Failed to parse event XML. Error: {ex.Message}");
                return;
            }

            XNamespace ns = root.GetDefaultNamespace();

            XElement? eventData = root.Element(ns + "EventData");
            if (eventData == null) {
                eventData = root.Element(ns + "UserData")?.Elements().FirstOrDefault();
            }

            if (eventData != null) {
                int noNameIndex = 0;
                foreach (XElement dataElement in eventData.Elements()) {
                    string value = dataElement.Value;
                    string? name = dataElement.Attribute("Name")?.Value;
                    if (string.IsNullOrEmpty(name)) {
                        if (dataElement.Name.LocalName == "Data") {
                            if (string.IsNullOrEmpty(value)) {
                                continue;
                            }
                            name = $"NoNameA{noNameIndex++}";
                        } else {
                            name = dataElement.Name.LocalName;
                        }
                    }
                    if (string.IsNullOrEmpty(name)) {
                        continue;
                    }
                    data[name!] = value;
                    foreach (var kv in ParseColonSeparatedLines(value)) {
                        if (!data.ContainsKey(kv.Key)) {
                            data[kv.Key] = kv.Value;
                        }
                    }

                    bool isBinaryElement = string.Equals(dataElement.Name.LocalName, "Binary", StringComparison.OrdinalIgnoreCase);
                    bool isBinaryData = string.Equals(dataElement.Attribute("Type")?.Value, "Binary", StringComparison.OrdinalIgnoreCase);
                    if ((isBinaryElement || isBinaryData) && TryDecodeBinary(value, out byte[] bytes)) {
                        attachments.Add(bytes);
                    }
                }
            }
        }

        private T ParseXML<T>(string xmlData) where T : IDictionary<string, string>, new() {
            ParseXmlPayload(xmlData, out Dictionary<string, string> parsed, out _);
            if (typeof(T) == typeof(Dictionary<string, string>)) {
                return (T)(object)parsed;
            }

            var result = new T();
            foreach (KeyValuePair<string, string> item in parsed) {
                result[item.Key] = item.Value;
            }
            return result;
        }

        private List<string> ExtractNicIdentifiers() {
            var nics = new List<string>();
            foreach (var kvp in Data) {
                var key = kvp.Key;
                if (key.IndexOf("nic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    key.IndexOf("nasidentifier", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    key.IndexOf("calledstationid", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    key.IndexOf("callingstationid", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    key.IndexOf("mac", StringComparison.OrdinalIgnoreCase) >= 0) {
                    if (!string.IsNullOrEmpty(kvp.Value)) {
                        nics.Add(kvp.Value);
                    }
                }
            }
            return nics;
        }

        private static bool TryDecodeBinary(string value, out byte[] bytes) {
            bytes = Array.Empty<byte>();
            if (string.IsNullOrWhiteSpace(value)) {
                return false;
            }

            value = value.Trim();
            value = value.Replace(" ", string.Empty);

            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
                value = value.Substring(2);
            }

            if (value.Length > 0 && value.Length % 2 == 0) {
                bytes = new byte[value.Length / 2];
                for (int i = 0; i < bytes.Length; i++) {
                    int high = HexValue(value[i * 2]);
                    int low = HexValue(value[(i * 2) + 1]);
                    if (high < 0 || low < 0) {
                        bytes = Array.Empty<byte>();
                        break;
                    }
                    bytes[i] = (byte)((high << 4) | low);
                }
                if (bytes.Length > 0) {
                    return true;
                }
            }

            try {
                bytes = Convert.FromBase64String(value);
                return true;
            } catch {
                return false;
            }
        }

        private static int HexValue(char value) {
            if (value >= '0' && value <= '9') {
                return value - '0';
            }
            if (value >= 'a' && value <= 'f') {
                return value - 'a' + 10;
            }
            if (value >= 'A' && value <= 'F') {
                return value - 'A' + 10;
            }
            return -1;
        }
    }
}

