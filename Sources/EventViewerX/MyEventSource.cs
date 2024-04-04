using System.Diagnostics.Tracing;

namespace EventViewerX {

    [EventSource(Name = "MyCompany-MyApplication-MyEventSource")]
    public sealed class MyEventSource : EventSource {
        public static readonly MyEventSource Log = new MyEventSource();

        [Event(1, Level = EventLevel.Informational)]
        public void MyEvent(int eventId, string param1, string param2, string binaryData) {
            if (eventId > 0) {
                WriteEvent(eventId, param1, param2, binaryData);
            }
        }
    }

    public class EventLogger {
        public static void WriteToEventLog(string eventSourceName, int eventId, EventLevel level, string param1, string param2, string binaryData) {
            MyEventSource.Log.MyEvent(eventId, param1, param2, binaryData);
        }
    }
}