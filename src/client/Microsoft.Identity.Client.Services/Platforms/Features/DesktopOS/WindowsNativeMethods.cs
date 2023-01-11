// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Identity.Client.Platforms.Features.DesktopOs
{
    internal static class WindowsNativeMethods
    {
        public enum NetJoinStatus
        {
            NetSetupUnknownStatus = 0,
            NetSetupUnjoined,
            NetSetupWorkgroupName,
            NetSetupDomainName
        }

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentProcessId();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private const int PROCESSOR_ARCHITECTURE_AMD64 = 9;
        private const int PROCESSOR_ARCHITECTURE_ARM = 5;
        private const int PROCESSOR_ARCHITECTURE_IA64 = 6;
        private const int PROCESSOR_ARCHITECTURE_INTEL = 0;
        public const int ErrorSuccess = 0;

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
                // todo(migration): look at way to get logger into servicebundle-specific platformproxy -> MsalLogger.Default.Warning(ex.Message);
                return "Unknown";
            }
        }

        [DllImport("secur32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool GetUserNameEx(int nameFormat, StringBuilder userName, ref uint userNameSize);

        [DllImport("Netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int NetGetJoinInformation(string server, out IntPtr domain, out NetJoinStatus status);

        [DllImport("Netapi32.dll")]
        public static extern int NetApiBufferFree(IntPtr Buffer);

        [DllImport("user32.dll", EntryPoint = "GetDesktopWindow")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

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

    internal static class User32
    {
        private const string LibraryName = "user32.dll";

        public const int UOI_FLAGS = 1;
        public const int WSF_VISIBLE = 0x0001;

        [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern IntPtr GetProcessWindowStation();

        [DllImport(LibraryName, EntryPoint = "GetUserObjectInformation", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern unsafe bool GetUserObjectInformation(IntPtr hObj, int nIndex, void* pvBuffer, uint nLength, ref uint lpnLengthNeeded);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct USEROBJECTFLAGS
    {
        public int fInherit;
        public int fReserved;
        public int dwFlags;
    }
}
