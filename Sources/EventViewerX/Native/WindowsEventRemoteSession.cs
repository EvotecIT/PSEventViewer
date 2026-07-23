using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace EventViewerX.Native;

internal static class WindowsEventRemoteSession {
    internal static WindowsEventNativeMethods.EventHandle OpenCore(
        string machineName,
        WindowsEventNativeMethods.RpcLogin login) {

        WindowsEventNativeMethods.EventHandle session =
            WindowsEventNativeMethods.EvtOpenSession(
                WindowsEventNativeMethods.LoginClass.RpcLogin,
                ref login,
                0,
                0);
        if (!session.IsInvalid) {
            return session;
        }

        int error = Marshal.GetLastWin32Error();
        session.Dispose();
        if (error == 5) {
            throw new UnauthorizedAccessException(
                $"Access was denied opening the Windows Event Log session to '{machineName}'.");
        }
        throw new Win32Exception(
            error,
            $"Failed to open the Windows Event Log session to '{machineName}'.");
    }
}
