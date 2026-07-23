using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace EventViewerX.Native;

internal sealed class WindowsEventPayloadRenderer : IDisposable {
    private const uint VariantArrayFlag = 0x80;
    private readonly NativeEventBuffer _xmlBuffer = new();
    private readonly NativeEventBuffer _valueBuffer = new();
    private readonly WindowsEventBookmarkRenderer _bookmarkRenderer = new();
    private readonly WindowsEventNativeMethods.EventHandle _userContext;

    internal WindowsEventPayloadRenderer() {
        _userContext = WindowsEventNativeMethods.EvtCreateRenderContext(
            0,
            null,
            WindowsEventNativeMethods.RenderContextFlags.User);
        if (_userContext.IsInvalid) {
            throw new System.ComponentModel.Win32Exception(
                Marshal.GetLastWin32Error(),
                "Failed to create the Windows event payload render context.");
        }
    }

    internal NativeEventStructured Render(IntPtr eventHandle, NativeEventMetadata metadata) {
        return new NativeEventStructured(
            metadata,
            RenderXml(eventHandle),
            RenderValues(eventHandle),
            _bookmarkRenderer.Render(eventHandle));
    }

    private string RenderXml(IntPtr eventHandle) {
        if (!WindowsEventNativeMethods.EvtRenderRaw(
                IntPtr.Zero,
                eventHandle,
                WindowsEventNativeMethods.RenderFlags.EventXml,
                _xmlBuffer.Capacity,
                _xmlBuffer.Pointer,
                out int bufferUsed,
                out _)) {

            int error = Marshal.GetLastWin32Error();
            if (error != WindowsEventNativeMethods.ErrorInsufficientBuffer) {
                return string.Empty;
            }

            _xmlBuffer.EnsureCapacity(bufferUsed);
            if (!WindowsEventNativeMethods.EvtRenderRaw(
                    IntPtr.Zero,
                    eventHandle,
                    WindowsEventNativeMethods.RenderFlags.EventXml,
                    _xmlBuffer.Capacity,
                    _xmlBuffer.Pointer,
                    out bufferUsed,
                    out _)) {
                return string.Empty;
            }
        }

        return Marshal.PtrToStringUni(_xmlBuffer.Pointer) ?? string.Empty;
    }

    private IReadOnlyList<EventPropertyValue> RenderValues(IntPtr eventHandle) {
        if (!WindowsEventNativeMethods.EvtRender(
                _userContext,
                eventHandle,
                WindowsEventNativeMethods.RenderFlags.EventValues,
                _valueBuffer.Capacity,
                _valueBuffer.Pointer,
                out int bufferUsed,
                out int propertyCount)) {

            int error = Marshal.GetLastWin32Error();
            if (error != WindowsEventNativeMethods.ErrorInsufficientBuffer) {
                return Array.Empty<EventPropertyValue>();
            }

            _valueBuffer.EnsureCapacity(bufferUsed);
            if (!WindowsEventNativeMethods.EvtRender(
                    _userContext,
                    eventHandle,
                    WindowsEventNativeMethods.RenderFlags.EventValues,
                    _valueBuffer.Capacity,
                    _valueBuffer.Pointer,
                    out bufferUsed,
                    out propertyCount)) {
                return Array.Empty<EventPropertyValue>();
            }
        }

        if (propertyCount == 0) {
            return Array.Empty<EventPropertyValue>();
        }

        int variantSize = Marshal.SizeOf<WindowsEventNativeMethods.EventVariant>();
        var values = new EventPropertyValue[propertyCount];
        for (int index = 0; index < propertyCount; index++) {
            WindowsEventNativeMethods.EventVariant variant =
                Marshal.PtrToStructure<WindowsEventNativeMethods.EventVariant>(
                    IntPtr.Add(_valueBuffer.Pointer, index * variantSize));
            values[index] = new EventPropertyValue(ReadValue(variant));
        }
        return values;
    }

    private static object? ReadValue(WindowsEventNativeMethods.EventVariant value) {
        if (value.ScalarType == WindowsEventNativeMethods.VariantType.Null) {
            return null;
        }
        if ((value.Type & VariantArrayFlag) != 0) {
            return ReadArray(value);
        }

        return value.ScalarType switch {
            WindowsEventNativeMethods.VariantType.String =>
                Marshal.PtrToStringUni(value.PointerValue),
            WindowsEventNativeMethods.VariantType.Xml =>
                Marshal.PtrToStringUni(value.PointerValue),
            WindowsEventNativeMethods.VariantType.AnsiString =>
                Marshal.PtrToStringAnsi(value.PointerValue),
            WindowsEventNativeMethods.VariantType.SByte => value.SByteValue,
            WindowsEventNativeMethods.VariantType.Byte => value.ByteValue,
            WindowsEventNativeMethods.VariantType.Int16 => value.Int16Value,
            WindowsEventNativeMethods.VariantType.UInt16 => value.UInt16Value,
            WindowsEventNativeMethods.VariantType.Int32 or WindowsEventNativeMethods.VariantType.HexInt32 =>
                value.Int32Value,
            WindowsEventNativeMethods.VariantType.UInt32 => value.UInt32Value,
            WindowsEventNativeMethods.VariantType.Int64 or WindowsEventNativeMethods.VariantType.HexInt64 =>
                value.Int64Value,
            WindowsEventNativeMethods.VariantType.UInt64 => value.UInt64Value,
            WindowsEventNativeMethods.VariantType.Single => value.SingleValue,
            WindowsEventNativeMethods.VariantType.Double => value.DoubleValue,
            WindowsEventNativeMethods.VariantType.Boolean => value.UInt32Value != 0,
            WindowsEventNativeMethods.VariantType.Binary => CopyBytes(value.PointerValue, value.Count),
            WindowsEventNativeMethods.VariantType.Guid =>
                value.PointerValue == IntPtr.Zero ? null : Marshal.PtrToStructure<Guid>(value.PointerValue),
            WindowsEventNativeMethods.VariantType.SizeT => value.PointerValue,
            WindowsEventNativeMethods.VariantType.FileTime =>
                DateTime.FromFileTimeUtc(unchecked((long)value.UInt64Value)).ToLocalTime(),
            WindowsEventNativeMethods.VariantType.Sid =>
                value.PointerValue == IntPtr.Zero ? null : new SecurityIdentifier(value.PointerValue),
            _ => value.PointerValue
        };
    }

    private static object ReadArray(WindowsEventNativeMethods.EventVariant value) {
        int count = checked((int)value.Count);
        if (count == 0 || value.PointerValue == IntPtr.Zero) {
            return Array.Empty<object>();
        }

        return value.ScalarType switch {
            WindowsEventNativeMethods.VariantType.String or WindowsEventNativeMethods.VariantType.Xml =>
                ReadStringArray(value.PointerValue, count),
            WindowsEventNativeMethods.VariantType.Byte or WindowsEventNativeMethods.VariantType.Binary =>
                CopyBytes(value.PointerValue, value.Count),
            WindowsEventNativeMethods.VariantType.UInt16 => ReadUInt16Array(value.PointerValue, count),
            WindowsEventNativeMethods.VariantType.UInt32 => ReadUInt32Array(value.PointerValue, count),
            WindowsEventNativeMethods.VariantType.Int64 => ReadInt64Array(value.PointerValue, count),
            WindowsEventNativeMethods.VariantType.UInt64 => ReadUInt64Array(value.PointerValue, count),
            WindowsEventNativeMethods.VariantType.Guid => ReadGuidArray(value.PointerValue, count),
            _ => Array.Empty<object>()
        };
    }

    private static byte[] CopyBytes(IntPtr source, uint count) {
        if (source == IntPtr.Zero || count == 0) {
            return Array.Empty<byte>();
        }
        var result = new byte[checked((int)count)];
        Marshal.Copy(source, result, 0, result.Length);
        return result;
    }

    private static string?[] ReadStringArray(IntPtr source, int count) {
        var result = new string?[count];
        for (int index = 0; index < count; index++) {
            IntPtr item = Marshal.ReadIntPtr(source, index * IntPtr.Size);
            result[index] = item == IntPtr.Zero ? null : Marshal.PtrToStringUni(item);
        }
        return result;
    }

    private static ushort[] ReadUInt16Array(IntPtr source, int count) {
        var result = new ushort[count];
        for (int index = 0; index < count; index++) {
            result[index] = unchecked((ushort)Marshal.ReadInt16(source, index * sizeof(ushort)));
        }
        return result;
    }

    private static uint[] ReadUInt32Array(IntPtr source, int count) {
        var result = new uint[count];
        for (int index = 0; index < count; index++) {
            result[index] = unchecked((uint)Marshal.ReadInt32(source, index * sizeof(uint)));
        }
        return result;
    }

    private static long[] ReadInt64Array(IntPtr source, int count) {
        var result = new long[count];
        for (int index = 0; index < count; index++) {
            result[index] = Marshal.ReadInt64(source, index * sizeof(long));
        }
        return result;
    }

    private static ulong[] ReadUInt64Array(IntPtr source, int count) {
        var result = new ulong[count];
        for (int index = 0; index < count; index++) {
            result[index] = unchecked((ulong)Marshal.ReadInt64(source, index * sizeof(ulong)));
        }
        return result;
    }

    private static Guid[] ReadGuidArray(IntPtr source, int count) {
        var result = new Guid[count];
        int size = Marshal.SizeOf<Guid>();
        for (int index = 0; index < count; index++) {
            result[index] = Marshal.PtrToStructure<Guid>(IntPtr.Add(source, index * size));
        }
        return result;
    }

    public void Dispose() {
        _userContext.Dispose();
        _bookmarkRenderer.Dispose();
        _xmlBuffer.Dispose();
        _valueBuffer.Dispose();
    }
}
