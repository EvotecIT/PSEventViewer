using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace EventViewerX {
    public partial class EventObject {
        /// <summary>
        /// Parses the message of the event record into a dictionary converting it into a key value pair
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private T ParseMessage<T>(string message) where T : IDictionary<string, string>, new() {
            T data = new();

            // Split the message into lines
            string[] lines = Regex.Split(message, "\r?\n");

            // Find the first non-empty line and add it to the dictionary with a default key of "Message"
            string? firstLine = lines.FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));
            if (firstLine != null) {
                data["Message"] = firstLine.Trim();
                MessageSubject = firstLine.Trim();
            }

            // Process the remaining lines
            for (int i = 1; i < lines.Length; i++) {
                string line = lines[i].Trim();

                // Skip empty lines
                if (string.IsNullOrEmpty(line)) {
                    continue;
                }

                // Check if the line contains a colon
                if (line.Contains(':')) {
                    // Split the line into a key and a value
                    string[] parts = line.Split(':');
                    if (parts.Length == 2) {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();
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
            Dictionary<string, string> data = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(text)) {
                return data;
            }

            string[] lines = Regex.Split(text, "\r?\n");
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
        /// Parses the XML data of the event record into a dictionary converting it into a key value pair
        /// </summary>
        /// <param name="xmlData">The XML data.</param>
        /// <returns></returns>
        private T ParseXML<T>(string xmlData) where T : IDictionary<string, string>, new() {
            T data = typeof(T) == typeof(Dictionary<string, string>)
                ? (T)(object)new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new();

            XElement root;
            try {
                root = XElement.Parse(xmlData);
            } catch (Exception ex) {
                Settings._logger.WriteWarning($"Failed to parse event XML. Error: {ex.Message}");
                return data;
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
                }
            }
            return data;
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

        private static List<byte[]> ExtractAttachments(string xmlData) {
            var attachments = new List<byte[]>();
            try {
                var root = XElement.Parse(xmlData);
                XNamespace ns = root.GetDefaultNamespace();
                var eventData = root.Element(ns + "EventData");
                if (eventData == null) {
                    eventData = root.Element(ns + "UserData")?.Elements().FirstOrDefault();
                }

                if (eventData != null) {
                    foreach (var binary in eventData.Elements(ns + "Binary")) {
                        var value = binary.Value;
                        if (TryDecodeBinary(value, out var bytes)) {
                            attachments.Add(bytes);
                        }
                    }

                    foreach (var dataElement in eventData.Elements(ns + "Data")) {
                        if (string.Equals(dataElement.Attribute("Type")?.Value, "Binary", StringComparison.OrdinalIgnoreCase)) {
                            var value = dataElement.Value;
                            if (TryDecodeBinary(value, out var bytes)) {
                                attachments.Add(bytes);
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                Settings._logger.WriteWarning($"Failed to parse attachments. Error: {ex.Message}");
            }

            return attachments;
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

            if (Regex.IsMatch(value, "^([0-9a-fA-F]{2})+$")) {
                try {
                    bytes = new byte[value.Length / 2];
                    for (int i = 0; i < bytes.Length; i++) {
                        bytes[i] = Convert.ToByte(value.Substring(i * 2, 2), 16);
                    }
                    return true;
                } catch {
                    return false;
                }
            }

            try {
                bytes = Convert.FromBase64String(value);
                return true;
            } catch {
                return false;
            }
        }
    }
}

