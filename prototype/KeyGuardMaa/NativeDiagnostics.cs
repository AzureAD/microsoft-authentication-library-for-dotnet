// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace KeyGuard.Attestation;

internal static class NativeDiagnostics
{
    private const string NativeDll = "AttestationClientLib.dll";

    internal static string? ProbeNativeDll()
    {
        string path = Path.Combine(AppContext.BaseDirectory, NativeDll);
        if (!File.Exists(path))
            return $"Native DLL not found at: {path}";

        IntPtr h = NativeLibrary.Load(path);
        if (h == IntPtr.Zero)
        {
            int err = Marshal.GetLastWin32Error();
            return err switch
            {
                193 or 216 => $"{NativeDll} is the wrong architecture for this process.",
                126 => $"{NativeDll} found but one of its dependencies is missing (libcurl, OpenSSL, or VC++ runtime).",
                _ => $"{NativeDll} could not be loaded (Win32 error 0x{err:X})."
            };
        }

        NativeLibrary.Free(h);
        return null;          // success
    }
}
