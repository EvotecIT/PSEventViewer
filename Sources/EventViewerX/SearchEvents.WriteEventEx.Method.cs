using System;
using System.Runtime.InteropServices;
using System.Text;

namespace EventViewerX {
    public partial class SearchEvents {
        /// <summary>
        /// Writes a structured ETW event using EventWriteEx for advanced scenarios (e.g., custom provider payloads).
        /// </summary>
        /// <param name="log">Target log/channel name.</param>
        /// <param name="serviceName">Service or provider name associated with the event.</param>
        /// <param name="message">Event message text (Unicode).</param>
        /// <param name="eventId">Event identifier.</param>
        /// <param name="version">Event version.</param>
        /// <param name="opcode">Event opcode.</param>
        /// <param name="channel">Channel identifier.</param>
        /// <param name="level">Event level.</param>
        /// <param name="task">Task identifier.</param>
        /// <param name="keyword">Keyword bitmask.</param>
        public static void WriteEventEx(string log, string serviceName, string message, ushort eventId, byte version, byte opcode, byte channel, byte level, ushort task, ulong keyword) {
            string providerId = "dbe9b383-7cf3-4331-91cc-a3cb16a3b538";
            providerId = providerId.Trim('{', '}');
            Guid providerGuid = new Guid(providerId);

            IntPtr registrationHandle;
            uint result = EventRegister(ref providerGuid, null, IntPtr.Zero, out registrationHandle);
            if (result != 0) {
                Console.WriteLine("EventRegister failed with error: " + result);
                return;
            }

            EVENT_DESCRIPTOR eventDescriptor = new EVENT_DESCRIPTOR {
                Id = eventId,
                Version = version,
                Channel = channel,
                Level = level,
                Opcode = opcode,
                Task = task,
                Keyword = keyword
            };

            byte[] messageBytes = Encoding.Unicode.GetBytes(message);
            GCHandle pinnedArray = GCHandle.Alloc(messageBytes, GCHandleType.Pinned);
            IntPtr userData = pinnedArray.AddrOfPinnedObject();

            try {
                Guid emptyGuid = Guid.Empty;

                EVENT_DATA_DESCRIPTOR dataDescriptor = new EVENT_DATA_DESCRIPTOR {
                    Ptr = (ulong)userData,
                    Size = (uint)messageBytes.Length,
                    Reserved = 0
                };

                IntPtr dataDescriptorPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(EVENT_DATA_DESCRIPTOR)));
                Marshal.StructureToPtr(dataDescriptor, dataDescriptorPtr, false);

                result = EventWriteEx(
                    registrationHandle,
                    ref eventDescriptor,
                    0, // Filter
                    0, // Flags
                    ref emptyGuid, // ActivityId
                    ref emptyGuid, // RelatedActivityId
                    1, // UserDataCount
                    dataDescriptorPtr); // UserData

                Marshal.FreeHGlobal(dataDescriptorPtr);

                if (result != 0) {
                    Console.WriteLine("EventWriteEx failed with error: " + result);
                    _logger.WriteWarning("EventWriteEx failed with error: " + result);
                } else {
                    _logger.WriteVerbose("EventWriteEx succeeded");
                }
            } finally {
                pinnedArray.Free();
            }

            EventUnregister(registrationHandle);
        }
    }
}
