// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client;

namespace Microsoft.Identity.Client.KeyAttestation
{
    /// <summary>
    /// Windows‑only helper that loads a native DLL from an absolute path.
    /// </summary>
    internal static class WindowsDllLoader
    {
        /// <summary>
        /// Load the DLL and throw when the OS loader fails.
        /// </summary>
        /// <param name="path">Absolute path to AttestationClientLib.dll</param>
        /// <returns>Module handle (never zero on success).</returns>
        /// <exception cref="Win32Exception">
        /// Thrown when <c>kernel32!LoadLibraryW</c> returns <c>NULL</c>.
        /// </exception>
        [DllImport("kernel32",
                   EntryPoint = "LoadLibraryW",
                   CharSet = CharSet.Unicode,
                   SetLastError = true,
                   ExactSpelling = true)]
        private static extern IntPtr LoadLibraryW(string path);

        internal static IntPtr Load(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            IntPtr h = LoadLibraryW(path);

            if (h == IntPtr.Zero)
            {
                // Preserve Win32 error code for diagnosis
                int err = Marshal.GetLastWin32Error();

                throw new MsalClientException(
                        "attestationmodule_load_failure",
                        $"Key Attestation Module load failed " +
                        $"(error={err}, " +
                        $"Unable to load {path}");
            }

            return h;
        }

        /// <summary>
        /// Optionally expose a Free helper so callers can unload if needed.
        /// </summary>
        [DllImport("kernel32", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        internal static void Free(IntPtr handle)
        {
            if (handle != IntPtr.Zero)
                FreeLibrary(handle);
        }
    }
}
