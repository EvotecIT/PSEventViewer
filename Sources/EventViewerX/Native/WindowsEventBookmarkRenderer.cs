using System;
using System.Diagnostics.Eventing.Reader;
using System.Reflection;
#if NET472
using System.Reflection.Emit;
#endif
using System.Runtime.InteropServices;

namespace EventViewerX.Native;

internal sealed class WindowsEventBookmarkRenderer : IDisposable {
#if NET472
    private static readonly Func<string, EventBookmark> CreateEventBookmark = CreateBookmarkFactory();
#endif
    private readonly NativeEventBuffer _buffer = new();

    internal EventBookmark? Render(IntPtr eventHandle) {
        using WindowsEventNativeMethods.EventHandle bookmark = WindowsEventNativeMethods.EvtCreateBookmark(null);
        if (bookmark.IsInvalid || !WindowsEventNativeMethods.EvtUpdateBookmark(bookmark, eventHandle)) {
            return null;
        }

        if (!WindowsEventNativeMethods.EvtRenderRaw(
                IntPtr.Zero,
                bookmark.DangerousGetHandle(),
                WindowsEventNativeMethods.RenderFlags.Bookmark,
                _buffer.Capacity,
                _buffer.Pointer,
                out int bufferUsed,
                out _)) {

            int error = Marshal.GetLastWin32Error();
            if (error != WindowsEventNativeMethods.ErrorInsufficientBuffer) {
                return null;
            }

            _buffer.EnsureCapacity(bufferUsed);
            if (!WindowsEventNativeMethods.EvtRenderRaw(
                    IntPtr.Zero,
                    bookmark.DangerousGetHandle(),
                    WindowsEventNativeMethods.RenderFlags.Bookmark,
                    _buffer.Capacity,
                    _buffer.Pointer,
                    out bufferUsed,
                    out _)) {
                return null;
            }
        }

        string bookmarkXml = Marshal.PtrToStringUni(_buffer.Pointer) ?? string.Empty;
        if (string.IsNullOrEmpty(bookmarkXml)) {
            return null;
        }
#if NET472
        return CreateEventBookmark(bookmarkXml);
#else
        return new EventBookmark(bookmarkXml);
#endif
    }

#if NET472
    private static Func<string, EventBookmark> CreateBookmarkFactory() {
        ConstructorInfo? constructor = typeof(EventBookmark).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            new[] { typeof(string) },
            modifiers: null);
        if (constructor == null) {
            throw new MissingMethodException(typeof(EventBookmark).FullName, ".ctor(string)");
        }

        var method = new DynamicMethod(
            "CreateEventBookmark",
            typeof(EventBookmark),
            new[] { typeof(string) },
            typeof(WindowsEventBookmarkRenderer),
            skipVisibility: true);
        ILGenerator generator = method.GetILGenerator();
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Newobj, constructor);
        generator.Emit(OpCodes.Ret);
        return (Func<string, EventBookmark>)method.CreateDelegate(typeof(Func<string, EventBookmark>));
    }
#endif

    public void Dispose() {
        _buffer.Dispose();
    }
}
