using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Security.Principal;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace EventViewerX {
    public class EventObject {
        /// <summary>
        /// Time and date when the event was created
        /// </summary>
        public DateTime TimeCreated => _eventRecord.TimeCreated.Value;

        /// <summary>
        /// Event ID
        /// </summary>
        public int Id => _eventRecord.Id;

        /// <summary>
        /// Record ID
        /// </summary>
        public long? RecordId => _eventRecord.RecordId;

        /// <summary>
        /// Log name where the event was logged
        /// </summary>
        public string LogName => _eventRecord.LogName;

        /// <summary>
        /// Log name where the event was queried from
        /// </summary>
        public string ContainerLog;

        /// <summary>
        /// Computer name where the event was logged
        /// </summary>
        public string ComputerName => _eventRecord.MachineName;

        public string LevelDisplayName => _eventRecord.LevelDisplayName;

        public string ProviderName => _eventRecord.ProviderName;

        public string Qualifiers => _eventRecord.Qualifiers?.ToString();

        public short? Opcode => _eventRecord.Opcode;

        public Guid? ProviderId => _eventRecord.ProviderId;

        public Guid? RelatedActivityId => _eventRecord.RelatedActivityId;

        public Guid? ActivityId => _eventRecord.ActivityId;

        public SecurityIdentifier UserId => _eventRecord.UserId;

        public EventBookmark Bookmark => _eventRecord.Bookmark;

        public string Message => _eventRecord.FormatDescription();

        public string TaskDisplayName => _eventRecord.TaskDisplayName;

        public string OpcodeDisplayName => _eventRecord.OpcodeDisplayName;

        public IEnumerable<string> KeywordsDisplayNames => _eventRecord.KeywordsDisplayNames;

        public long? Keywords => _eventRecord.Keywords;

        public byte? Level => _eventRecord.Level;

        //public Level? Level => (Level?)_eventRecord.Level;

        //public Keywords? Keywords => (Keywords?)_eventRecord.Keywords;

        public byte? Version => _eventRecord.Version;

        public int? Task => _eventRecord.Task;

        public int? ProcessId => _eventRecord.ProcessId;

        public int? ThreadId => _eventRecord.ThreadId;

        /// <summary>
        /// Computer name where the event was logged
        /// </summary>
        public string MachineName => _eventRecord.MachineName;

        /// <summary>
        /// Properties available in the event record
        /// </summary>
        public IList<EventProperty> Properties => _eventRecord.Properties;
        //public IEnumerable<EventProperty> Properties => _eventRecord.Properties;

        /// <summary>
        /// Data available in XML converted to a dictionary
        /// </summary>
        public Dictionary<string, string> Data { get; private set; }

        /// <summary>
        /// Data available in the message converted to a dictionary
        /// </summary>
        public Dictionary<string, string> MessageData { get; private set; }

        public string MessageSubject;

        /// <summary>
        /// Data available in XML format
        /// </summary>
        public string XMLData;

        /// <summary>
        /// Machine where the event was queried from
        /// </summary>
        public string QueriedMachine;

        /// <summary>
        /// Source where the event was gathered from (computer name or file path)
        /// </summary>
        public string GatheredFrom;

        /// <summary>
        /// Log name where the event was gathered from
        /// </summary>
        public string GatheredLogName;

        /// <summary>
        /// Original event record
        /// </summary>
        public readonly EventRecord _eventRecord;

        public EventObject(EventRecord eventRecord, string queriedMachine) {


            QueriedMachine = queriedMachine;
            _eventRecord = eventRecord;

            ContainerLog = ((EventLogRecord)_eventRecord).ContainerLog;

            // GatheredFrom is the file path if querying from file, otherwise computer name
            if (queriedMachine != null && (queriedMachine.EndsWith(".evtx", StringComparison.OrdinalIgnoreCase) || queriedMachine.Contains("\\"))) {
                // This looks like a file path
                GatheredFrom = queriedMachine;
            } else {
                // This is a computer name or null (local machine)
                GatheredFrom = queriedMachine ?? Environment.MachineName;
            }
            GatheredLogName = eventRecord.LogName;

            XMLData = eventRecord.ToXml();
            // Create a dictionary to hold the xml data
            Data = ParseXML(XMLData);
            // Create a dictionary to hold the message data
            try {
                MessageData = ParseMessage(eventRecord.FormatDescription());
            } catch {
                MessageData = new Dictionary<string, string>();
                //_logger.WriteError("Error parsing message");
            }
        }

        /// <summary>
        /// Parses the message of the event record into a dictionary converting it into a key value pair
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private Dictionary<string, string> ParseMessage(string message) {
            Dictionary<string, string> data = new Dictionary<string, string>();

            // Split the message into lines
            string[] lines = Regex.Split(message, "\r?\n");

            // Find the first non-empty line and add it to the dictionary with a default key of "Message"
            string firstLine = lines.FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));
            if (firstLine != null) {
                data["Message"] = firstLine.Trim();
                MessageSubject = firstLine.Trim();
            }

            // Process the remaining lines
            for (int i = 1; i < lines.Length; i++) {
                string line = lines[i].Trim(); // Trim the line to remove leading and trailing whitespace

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
        private Dictionary<string, string> ParseXML(string xmlData) {
            Dictionary<string, string> data = new Dictionary<string, string>();

            // Parse the XML data into an XElement
            XElement root;
            try {
                root = XElement.Parse(xmlData);
            } catch (Exception ex) {
                Settings._logger.WriteWarning($"Failed to parse event XML. Error: {ex.Message}");
                return data;
            }

            // Get the namespace of the root element
            XNamespace ns = root.GetDefaultNamespace();

            // Extract the EventData or UserData/EventXML element
            XElement eventData = root.Element(ns + "EventData");
            if (eventData == null) {
                eventData = root.Element(ns + "UserData")?.Elements().FirstOrDefault();
            }

            if (eventData != null) {
                // Add each data element to the dictionary
                int noNameIndex = 0;
                foreach (XElement dataElement in eventData.Elements()) {
                    string value = dataElement.Value;
                    string name = dataElement.Attribute("Name")?.Value;
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
                    data[name] = value;
                    foreach (var kv in ParseColonSeparatedLines(value)) {
                        if (!data.ContainsKey(kv.Key)) {
                            data[kv.Key] = kv.Value;
                        }
                    }
                }
            }
            return data;
        }

        /// <summary>
        /// Gets the value from data dictionary.
        /// </summary>
        /// <param name="key1">The key1.</param>
        /// <param name="key2">The key2.</param>
        /// <param name="splitter">The splitter.</param>
        /// <param name="reverseOrder">if set to <c>true</c> [reverse order].</param>
        /// <returns></returns>
        public string GetValueFromDataDictionary(string key1, string key2 = null, string splitter = "\\", bool reverseOrder = false) {
            if (key1 != null && key2 != null && Data.ContainsKey(key1) && Data.ContainsKey(key2)) {
                if (reverseOrder) {
                    return Data[key2] + splitter + Data[key1];
                } else {
                    return Data[key1] + splitter + Data[key2];
                }
            } else if (key1 != null && Data.ContainsKey(key1)) {
                return Data[key1];
            } else if (key2 != null && Data.ContainsKey(key2)) {
                return Data[key2];
            } else {
                return "";
            }
        }

        internal bool ValueMatches(string key, string expectedValue) {
            if (key != null && Data.ContainsKey(key)) {
                if (Data[key].Equals(expectedValue, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                } else {
                    return false;
                }
            }

            return false;
        }
    }
}