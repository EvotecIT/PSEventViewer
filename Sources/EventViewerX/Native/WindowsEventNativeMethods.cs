using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace EventViewerX.Native;

internal static class WindowsEventNativeMethods {
    internal const int ErrorInsufficientBuffer = 122;
    internal const int ErrorNoMoreItems = 259;
    internal const int SystemPropertyCount = 18;

    [Flags]
    internal enum QueryFlags : uint {
        ChannelPath = 0x1,
        FilePath = 0x2,
        ForwardDirection = 0x100,
        ReverseDirection = 0x200
    }

    internal enum RenderContextFlags : uint {
        System = 1,
        User = 2
    }

    internal enum RenderFlags : uint {
        EventValues = 0,
        EventXml = 1,
        Bookmark = 2
    }

    internal enum FormatMessageFlags : uint {
        Event = 1,
        Level = 2,
        Task = 3,
        Opcode = 4,
        Keyword = 5
    }

    internal enum VariantType : uint {
        Null = 0,
        String = 1,
        AnsiString = 2,
        SByte = 3,
        Byte = 4,
        Int16 = 5,
        UInt16 = 6,
        Int32 = 7,
        UInt32 = 8,
        Int64 = 9,
        UInt64 = 10,
        Single = 11,
        Double = 12,
        Boolean = 13,
        Binary = 14,
        Guid = 15,
        SizeT = 16,
        FileTime = 17,
        SystemTime = 18,
        Sid = 19,
        HexInt32 = 20,
        HexInt64 = 21,
        EventHandle = 32,
        Xml = 35
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal struct EventVariant {
        [FieldOffset(0)]
        internal IntPtr PointerValue;

        [FieldOffset(0)]
        internal byte ByteValue;

        [FieldOffset(0)]
        internal sbyte SByteValue;

        [FieldOffset(0)]
        internal short Int16Value;

        [FieldOffset(0)]
        internal ushort UInt16Value;

        [FieldOffset(0)]
        internal int Int32Value;

        [FieldOffset(0)]
        internal uint UInt32Value;

        [FieldOffset(0)]
        internal long Int64Value;

        [FieldOffset(0)]
        internal ulong UInt64Value;

        [FieldOffset(0)]
        internal float SingleValue;

        [FieldOffset(0)]
        internal double DoubleValue;

        [FieldOffset(8)]
        internal uint Count;

        [FieldOffset(12)]
        internal uint Type;

        internal VariantType ScalarType => (VariantType)(Type & 0x7f);
    }

    internal sealed class EventHandle : SafeHandleZeroOrMinusOneIsInvalid {
        internal EventHandle()
            : base(true) {
        }

        protected override bool ReleaseHandle() {
            return EvtClose(handle);
        }
    }

    [DllImport("wevtapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern EventHandle EvtQuery(
        IntPtr session,
        string path,
        string query,
        QueryFlags flags);

    [DllImport("wevtapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EvtNext(
        EventHandle resultSet,
        int eventArraySize,
        [Out] IntPtr[] eventArray,
        int timeout,
        int flags,
        out int returned);

    [DllImport("wevtapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern EventHandle EvtCreateRenderContext(
        int valuePathsCount,
        string[]? valuePaths,
        RenderContextFlags flags);

    [DllImport("wevtapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EvtRender(
        EventHandle? context,
        IntPtr fragment,
        RenderFlags flags,
        int bufferSize,
        IntPtr buffer,
        out int bufferUsed,
        out int propertyCount);

    [DllImport("wevtapi.dll", EntryPoint = "EvtRender", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EvtRenderRaw(
        IntPtr context,
        IntPtr fragment,
        RenderFlags flags,
        int bufferSize,
        IntPtr buffer,
        out int bufferUsed,
        out int propertyCount);

    [DllImport("wevtapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern EventHandle EvtOpenPublisherMetadata(
        IntPtr session,
        string publisherIdentity,
        string? logFilePath,
        int locale,
        int flags);

    [DllImport("wevtapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EvtFormatMessage(
        IntPtr publisherMetadata,
        IntPtr eventHandle,
        int messageId,
        int valueCount,
        IntPtr values,
        FormatMessageFlags flags,
        int bufferSize,
        IntPtr buffer,
        out int bufferUsed);

    [DllImport("wevtapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern EventHandle EvtCreateBookmark(string? bookmarkXml);

    [DllImport("wevtapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EvtUpdateBookmark(
        EventHandle bookmark,
        IntPtr eventHandle);

    [DllImport("wevtapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EvtClose(IntPtr handle);
}
