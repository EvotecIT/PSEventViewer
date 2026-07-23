using System;
using System.Collections.Generic;
using System.Xml;

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
        /// <param name="lines">Provider-formatted message lines.</param>
        /// <returns></returns>
        private Dictionary<string, string> ParseMessage(IReadOnlyList<string> lines) {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int firstNonEmptyLineIndex = FindFirstMessageLine(lines);
            if (firstNonEmptyLineIndex >= 0) {
                string firstLine = lines[firstNonEmptyLineIndex].Trim();
                data["Message"] = firstLine;
            }

            // Process remaining lines (after the subject) into key:value pairs.
            for (int i = firstNonEmptyLineIndex + 1; i < lines.Count; i++) {
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

        private static string GetMessageSubject(IReadOnlyList<string> lines) {
            int index = FindFirstMessageLine(lines);
            return index >= 0 ? lines[index].Trim() : string.Empty;
        }

        private static int FindFirstMessageLine(IReadOnlyList<string> lines) {
            for (int i = 0; i < lines.Count; i++) {
                if (!string.IsNullOrWhiteSpace(lines[i])) {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Parses lines containing colon separated key/value pairs.
        /// </summary>
        /// <param name="data">Dictionary that receives keys that are not already present.</param>
        /// <param name="text">Text to parse</param>
        private static void AddColonSeparatedLines(Dictionary<string, string> data, string text) {
            if (string.IsNullOrEmpty(text)) {
                return;
            }
            if (text.IndexOf(':') < 0) {
                return;
            }

            Dictionary<string, string>? parsed = null;
            int lineStart = 0;
            while (lineStart < text.Length) {
                int newLineIndex = text.IndexOf('\n', lineStart);
                int lineEnd = newLineIndex >= 0 ? newLineIndex : text.Length;
                if (lineEnd > lineStart && text[lineEnd - 1] == '\r') {
                    lineEnd--;
                }

                int contentStart = lineStart;
                while (contentStart < lineEnd && char.IsWhiteSpace(text[contentStart])) {
                    contentStart++;
                }
                while (lineEnd > contentStart && char.IsWhiteSpace(text[lineEnd - 1])) {
                    lineEnd--;
                }

                int colonIndex = contentStart < lineEnd
                    ? text.IndexOf(':', contentStart, lineEnd - contentStart)
                    : -1;
                if (colonIndex >= 0) {
                    int keyEnd = colonIndex;
                    while (keyEnd > contentStart && char.IsWhiteSpace(text[keyEnd - 1])) {
                        keyEnd--;
                    }

                    int valueStart = colonIndex + 1;
                    while (valueStart < lineEnd && char.IsWhiteSpace(text[valueStart])) {
                        valueStart++;
                    }

                    if (keyEnd > contentStart) {
                        string key = text.Substring(contentStart, keyEnd - contentStart);
                        parsed ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        parsed[key] = text.Substring(valueStart, lineEnd - valueStart);
                    }
                }

                if (newLineIndex < 0) {
                    break;
                }
                lineStart = newLineIndex + 1;
            }

            if (parsed == null) {
                return;
            }
            foreach (KeyValuePair<string, string> field in parsed) {
                if (!data.ContainsKey(field.Key)) {
                    data[field.Key] = field.Value;
                }
            }
        }

        /// <summary>
        /// Parses structured event data and, when requested, binary attachments in one XML pass.
        /// </summary>
        /// <param name="xmlData">The XML data.</param>
        /// <param name="data">Parsed named event data.</param>
        /// <param name="attachments">Decoded binary attachments.</param>
        /// <param name="includeAttachments">Whether binary payloads should be decoded and retained.</param>
        private static void ParseXmlPayload(
            string xmlData,
            out Dictionary<string, string> data,
            out IReadOnlyList<byte[]> attachments,
            bool includeAttachments) {

            data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            attachments = Array.Empty<byte[]>();
            if (string.IsNullOrEmpty(xmlData)) {
                return;
            }

            try {
                using var stringReader = new StringReader(xmlData);
                using XmlReader reader = XmlReader.Create(stringReader, new XmlReaderSettings {
                    DtdProcessing = DtdProcessing.Prohibit,
                    IgnoreComments = true,
                    XmlResolver = null
                });

                List<byte[]>? decodedAttachments = null;
                while (reader.Read()) {
                    if (reader.NodeType != XmlNodeType.Element) {
                        continue;
                    }
                    if (string.Equals(reader.LocalName, "EventData", StringComparison.Ordinal)) {
                        ParsePayloadContainer(reader, data, ref decodedAttachments, includeAttachments);
                        break;
                    }
                    if (string.Equals(reader.LocalName, "UserData", StringComparison.Ordinal)) {
                        ParseUserData(reader, data, ref decodedAttachments, includeAttachments);
                        break;
                    }
                }
                if (decodedAttachments != null) {
                    attachments = decodedAttachments;
                }
            } catch (Exception ex) {
                Settings._logger.WriteWarning($"Failed to parse event XML. Error: {ex.Message}");
            }
        }

        private static void ParseUserData(
            XmlReader reader,
            Dictionary<string, string> data,
            ref List<byte[]>? attachments,
            bool includeAttachments) {

            int userDataDepth = reader.Depth;
            if (reader.IsEmptyElement) {
                return;
            }

            while (reader.Read()) {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == userDataDepth) {
                    break;
                }
                if (reader.NodeType == XmlNodeType.Element && reader.Depth == userDataDepth + 1) {
                    ParsePayloadContainer(reader, data, ref attachments, includeAttachments);
                    break;
                }
            }
        }

        private static void ParsePayloadContainer(
            XmlReader reader,
            Dictionary<string, string> data,
            ref List<byte[]>? attachments,
            bool includeAttachments) {

            int containerDepth = reader.Depth;
            int noNameIndex = 0;
            if (reader.IsEmptyElement) {
                return;
            }

            while (reader.Read()) {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == containerDepth) {
                    break;
                }
                if (reader.NodeType != XmlNodeType.Element || reader.Depth != containerDepth + 1) {
                    continue;
                }

                string localName = reader.LocalName;
                string? name = reader.GetAttribute("Name");
                string? type = reader.GetAttribute("Type");
                string value = ReadElementText(reader);
                if (string.IsNullOrEmpty(name)) {
                    if (string.Equals(localName, "Data", StringComparison.Ordinal)) {
                        if (string.IsNullOrEmpty(value)) {
                            continue;
                        }
                        name = $"NoNameA{noNameIndex++}";
                    } else {
                        name = localName;
                    }
                }
                if (string.IsNullOrEmpty(name)) {
                    continue;
                }

                data[name] = value;
                AddColonSeparatedLines(data, value);

                bool isBinaryElement = string.Equals(localName, "Binary", StringComparison.OrdinalIgnoreCase);
                bool isBinaryData = string.Equals(type, "Binary", StringComparison.OrdinalIgnoreCase);
                if (includeAttachments && (isBinaryElement || isBinaryData) && TryDecodeBinary(value, out byte[] bytes)) {
                    attachments ??= new List<byte[]>();
                    attachments.Add(bytes);
                }
            }
        }

        private static string ReadElementText(XmlReader reader) {
            if (reader.IsEmptyElement) {
                return string.Empty;
            }

            int elementDepth = reader.Depth;
            string? singleValue = null;
            StringBuilder? builder = null;
            while (reader.Read()) {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == elementDepth) {
                    break;
                }
                if (reader.NodeType != XmlNodeType.Text &&
                    reader.NodeType != XmlNodeType.CDATA &&
                    reader.NodeType != XmlNodeType.Whitespace &&
                    reader.NodeType != XmlNodeType.SignificantWhitespace) {
                    continue;
                }

                if (singleValue == null && builder == null) {
                    singleValue = reader.Value;
                } else {
                    builder ??= new StringBuilder(singleValue);
                    builder.Append(reader.Value);
                }
            }
            return builder?.ToString() ?? singleValue ?? string.Empty;
        }

        private T ParseXML<T>(string xmlData) where T : IDictionary<string, string>, new() {
            ParseXmlPayload(xmlData, out Dictionary<string, string> parsed, out _, includeAttachments: false);
            if (typeof(T) == typeof(Dictionary<string, string>)) {
                return (T)(object)parsed;
            }

            var result = new T();
            foreach (KeyValuePair<string, string> item in parsed) {
                result[item.Key] = item.Value;
            }
            return result;
        }

        private static List<string>? ExtractNicIdentifiers(IReadOnlyDictionary<string, string> data) {
            List<string>? nics = null;
            foreach (KeyValuePair<string, string> kvp in data) {
                var key = kvp.Key;
                if (key.IndexOf("nic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    key.IndexOf("nasidentifier", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    key.IndexOf("calledstationid", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    key.IndexOf("callingstationid", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    key.IndexOf("mac", StringComparison.OrdinalIgnoreCase) >= 0) {
                    if (!string.IsNullOrEmpty(kvp.Value)) {
                        nics ??= new List<string>();
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
