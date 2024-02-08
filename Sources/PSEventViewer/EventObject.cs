using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Security.Principal;
using System.Xml.Linq;

namespace PSEventViewer {
    public enum Modes {
        Disabled,
        Parallel,
        ParallelForEachBuiltin,
        ParallelForEach,
    }

    public enum Level {
        Verbose = 5,
        Informational = 4,
        Warning = 3,
        Error = 2,
        Critical = 1,
        LogAlways = 0
    }

    public enum Keywords : long {
        AuditFailure = (long)4503599627370496,
        AuditSuccess = (long)9007199254740992,
        CorrelationHint2 = (long)18014398509481984,
        EventLogClassic = (long)36028797018963968,
        Sqm = (long)2251799813685248,
        WdiDiagnostic = (long)1125899906842624,
        WdiContext = (long)562949953421312,
        ResponseTime = (long)281474976710656,
        None = (long)0
    }

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
        public string ContainerLog => _eventRecord.ContainerLog;

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
        /// Original event record
        /// </summary>
        private readonly EventLogRecord _eventRecord;

        public EventObject(EventRecord eventRecord, string queriedMachine) {
            QueriedMachine = queriedMachine;
            _eventRecord = (EventLogRecord)eventRecord;

            XMLData = eventRecord.ToXml();
            // Create a dictionary to hold the xml data
            Data = ParseXML(XMLData);
            // Create a dictionary to hold the message data
            MessageData = ParseMessage(eventRecord.FormatDescription());
        }

        /// <summary>
        /// Parses the message of the event record into a dictionary converting it into a key value pair
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private Dictionary<string, string> ParseMessage(string message) {
            Dictionary<string, string> data = new Dictionary<string, string>();

            // Split the message into lines
            string[] lines = message.Split('\n');

            // Add the first line of the message to the dictionary with a default key of "Message"
            data["Message"] = lines[0].Trim();

            MessageSubject = data["Message"];

            // Process the remaining lines
            for (int i = 1; i < lines.Length; i++) {
                string line = lines[i];
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
        /// Parses the XML data of the event record into a dictionary converting it into a key value pair
        /// </summary>
        /// <param name="xmlData"></param>
        /// <returns></returns>
        private Dictionary<string, string> ParseXML(string xmlData) {
            Dictionary<string, string> data = new Dictionary<string, string>();

            // Parse the XML data into an XElement
            XElement root = XElement.Parse(xmlData);

            // Get the namespace of the root element
            XNamespace ns = root.GetDefaultNamespace();

            // Extract the EventData or UserData/EventXML element
            XElement eventData = root.Element(ns + "EventData");
            if (eventData == null) {
                eventData = root.Element(ns + "UserData")?.Elements().FirstOrDefault();
            }

            if (eventData != null) {
                // Add each data element to the dictionary
                int noNameIndex = 1;
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
                }
            }
            return data;
        }

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
    }
}