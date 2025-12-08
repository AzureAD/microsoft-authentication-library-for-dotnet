// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.IO;

namespace Msal.KeyAttestation
{
    internal static class NativeDiagnostics
    {
        private const string NativeDll = "AttestationClientLib.dll";

        internal static string ProbeNativeDll()
        {
            string path = Path.Combine(AppContext.BaseDirectory, NativeDll);

            if (!File.Exists(path))
                return $"Native DLL not found at: {path}";

            IntPtr h;

            try
            {
                h = WindowsDllLoader.Load(path);
            }
            catch (Win32Exception w32)
            {
                return w32.NativeErrorCode switch
                {
                    193 or 216 => $"{NativeDll} is the wrong architecture for this process.",
                    126 => $"{NativeDll} found but one of its dependencies is missing (libcurl, OpenSSL, or VC++ runtime).",
                    _ => $"{NativeDll} could not be loaded (Win32 error 0x{w32.NativeErrorCode:X})."
                };
            }
            catch (Exception ex)
            {
                return $"Unable to load {NativeDll}: {ex.Message}";
            }

            // success – unload and return null (meaning "no error")
            WindowsDllLoader.Free(h);
            return null;
        }
    }
}
