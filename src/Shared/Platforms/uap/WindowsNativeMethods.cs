// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Platforms.uap
{
    internal static class WindowsNativeMethods
    {
        private const int PROCESSOR_ARCHITECTURE_AMD64 = 9;
        private const int PROCESSOR_ARCHITECTURE_ARM = 5;
        private const int PROCESSOR_ARCHITECTURE_IA64 = 6;
        private const int PROCESSOR_ARCHITECTURE_INTEL = 0;

        [DllImport("kernel32.dll")]
        private static extern void GetNativeSystemInfo(ref SYSTEM_INFO lpSystemInfo);

        public static string GetProcessorArchitecture()
        {
            try
            {
                var systemInfo = new SYSTEM_INFO();
                GetNativeSystemInfo(ref systemInfo);
                switch (systemInfo.wProcessorArchitecture)
                {
                case PROCESSOR_ARCHITECTURE_AMD64:
                case PROCESSOR_ARCHITECTURE_IA64:
                    return "x64";

                case PROCESSOR_ARCHITECTURE_ARM:
                    return "ARM";

                case PROCESSOR_ARCHITECTURE_INTEL:
                    return "x86";

                default:
                    return "Unknown";
                }
            }
            catch (Exception)
            {
                // todo(migration): look at way to get logger into servicebundle-specific platformproxy -> MsalLogger.Default.WarningPii(ex);
                return "Unknown";
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEM_INFO
        {
            public readonly short wProcessorArchitecture;
            public readonly short wReserved;
            public readonly int dwPageSize;
            public readonly IntPtr lpMinimumApplicationAddress;
            public readonly IntPtr lpMaximumApplicationAddress;
            public readonly IntPtr dwActiveProcessorMask;
            public readonly int dwNumberOfProcessors;
            public readonly int dwProcessorType;
            public readonly int dwAllocationGranularity;
            public readonly short wProcessorLevel;
            public readonly short wProcessorRevision;
        }
    }
}
