// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Microsoft.Identity.Client.MtlsPop.Attestation
{
    /// <summary>
    /// Ensures AttestationClientLib.dll is resolved from an override path, the app folder,
    /// the system directories (System32/SysWOW64), or the default DLL search order (PATH).
    /// </summary>
    internal static class NativeDllResolver
    {
        private const string NativeDll = "AttestationClientLib.dll";
        private static IntPtr s_module;

        static NativeDllResolver()
        {
            // 1) Env override (per-job / per-process)
            if (TryLoadFromEnv())
                return;

            // 2) App base directory
            if (TryLoadFromAppBase())
                return;

            // 3) System directory (System32 for x64 process, SysWOW64 for x86 process)
            if (TryLoadFromSystemDir())
                return;

            // 4) Let Windows search PATH / SxS / Known DLL dirs
            s_module = WindowsDllLoader.Load(NativeDll);
        }

        /// <summary>Touch this method from startup code to trigger the static ctor.</summary>
        internal static void EnsureLoaded() { }

        private static bool TryLoadFromEnv()
        {
            var overrideDir = Environment.GetEnvironmentVariable("MSAL_MTLSPOP_NATIVE_PATH");
            if (string.IsNullOrWhiteSpace(overrideDir))
            {
                return false;
            }

            var candidate = Path.Combine(overrideDir, NativeDll);
            if (!File.Exists(candidate))
            {
                return false;
            }

            s_module = WindowsDllLoader.Load(candidate);
            return s_module != IntPtr.Zero;
        }

        private static bool TryLoadFromAppBase()
        {
            var exePath = Path.Combine(AppContext.BaseDirectory, NativeDll);
            if (!File.Exists(exePath))
            {
                return false;
            }

            s_module = WindowsDllLoader.Load(exePath);
            return s_module != IntPtr.Zero;
        }

        private static bool TryLoadFromSystemDir()
        {
            var windowsRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            if (string.IsNullOrEmpty(windowsRoot))
            {
                return false;
            }

            // x64 process -> System32, x86 process -> SysWOW64
            var sysDir = Path.Combine(
                windowsRoot,
                Environment.Is64BitProcess ? "System32" : "SysWOW64");

            var sysPath = Path.Combine(sysDir, NativeDll);
            if (!File.Exists(sysPath))
            {
                return false;
            }

            s_module = WindowsDllLoader.Load(sysPath);
            return s_module != IntPtr.Zero;
        }
    }
}
