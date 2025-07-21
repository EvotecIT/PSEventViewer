using System.Runtime.InteropServices;
using System.Text;

namespace EventViewerX {
    public partial class SearchEvents : Settings {
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
                    0,
                    0,
                    ref emptyGuid,
                    ref emptyGuid,
                    1,
                    dataDescriptorPtr);

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
