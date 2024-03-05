using System;
using System.Runtime.InteropServices;
using System.Text;

namespace EventViewer {
    public partial class SearchEvents : Settings {

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint EventUnregister(IntPtr RegHandle);

        [StructLayout(LayoutKind.Sequential)]
        private struct EVENT_FILTER_DESCRIPTOR {
            public ulong Ptr;
            public uint Size;
            public uint Type;
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint EventRegister(
            ref Guid ProviderId,
            EtwEnableCallback EnableCallback,
            IntPtr CallbackContext,
            out IntPtr RegHandle);

        private unsafe delegate void EtwEnableCallback(
            ref Guid SourceId,
            int IsEnabled,
            byte Level,
            ulong MatchAnyKeyword,
            ulong MatchAllKeyword,
            EVENT_FILTER_DESCRIPTOR* FilterData,
            IntPtr CallbackContext);

        // Define the PInvoke signature
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint EventWriteEx(
            IntPtr RegHandle,
            ref EVENT_DESCRIPTOR EventDescriptor,
            ulong Filter,
            uint Flags,
            ref Guid ActivityId,
            ref Guid RelatedActivityId,
            uint UserDataCount,
            IntPtr UserData);

        [StructLayout(LayoutKind.Sequential)]
        private struct EVENT_DESCRIPTOR {
            public ushort Id;
            public byte Version;
            public byte Channel;
            public byte Level;
            public byte Opcode;
            public ushort Task;
            public ulong Keyword;
        }


        [StructLayout(LayoutKind.Sequential)]
        private struct EVENT_DATA_DESCRIPTOR {
            public ulong Ptr;
            public uint Size;
            public uint Reserved;
        }

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


        //public static void WriteEventEx(string log, string serviceName, string message) {

        //    string providerId = "dbe9b383-7cf3-4331-91cc-a3cb16a3b538";

        //    // Remove the curly braces from the provider ID
        //    providerId = providerId.Trim('{', '}');

        //    // Convert the provider ID to a GUID
        //    Guid providerGuid = new Guid(providerId);

        //    // Register the event provider
        //    IntPtr registrationHandle;
        //    uint result = EventRegister(ref providerGuid, null, IntPtr.Zero, out registrationHandle);
        //    if (result != 0) {
        //        Console.WriteLine("EventRegister failed with error: " + result);
        //        return;
        //    }

        //    // Initialize the other parameters

        //    // Initialize the other parameters
        //    EVENT_DESCRIPTOR eventDescriptor = new EVENT_DESCRIPTOR {
        //        Id = 100,
        //        Version = 0,
        //        Channel = 16,
        //        Level = 4,
        //        Opcode = 1,
        //        Task = 1,
        //        Keyword = 0x8000000000000001
        //    };



        //    message = "Retail Demo service has started.";

        //    // Convert the message to a byte array and pin it
        //    message = "Your message here";
        //    byte[] messageBytes = Encoding.Unicode.GetBytes(message);

        //    // Pin the byte array to prevent the garbage collector from moving it while we're using it
        //    GCHandle pinnedArray = GCHandle.Alloc(messageBytes, GCHandleType.Pinned);
        //    IntPtr userData = pinnedArray.AddrOfPinnedObject();

        //    try {
        //        Guid emptyGuid = Guid.Empty;

        //        // Create an EVENT_DATA_DESCRIPTOR for the message string
        //        EVENT_DATA_DESCRIPTOR dataDescriptor = new EVENT_DATA_DESCRIPTOR {
        //            Ptr = (ulong)userData,
        //            Size = (uint)messageBytes.Length,
        //            Reserved = 0
        //        };

        //        // Get the address of the dataDescriptor
        //        IntPtr dataDescriptorPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(EVENT_DATA_DESCRIPTOR)));
        //        Marshal.StructureToPtr(dataDescriptor, dataDescriptorPtr, false);

        //        // Write the event
        //        result = EventWriteEx(
        //            registrationHandle,
        //            ref eventDescriptor,
        //            0, // Filter
        //            0, // Flags
        //            ref emptyGuid, // ActivityId
        //            ref emptyGuid, // RelatedActivityId
        //            1, // UserDataCount
        //            dataDescriptorPtr); // UserData

        //        // Free the allocated memory
        //        Marshal.FreeHGlobal(dataDescriptorPtr);



        //        // Check the result
        //        if (result != 0) {
        //            Console.WriteLine("EventWriteEx failed with error: " + result);
        //        } else {
        //            Console.WriteLine("EventWriteEx succeeded");
        //        }
        //    } finally {
        //        // Unpin the byte array
        //        pinnedArray.Free();
        //    }

        //    // Unregister the event provider
        //    EventUnregister(registrationHandle);
        //}
    }
}