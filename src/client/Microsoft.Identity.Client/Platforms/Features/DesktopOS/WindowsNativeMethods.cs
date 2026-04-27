// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// P/Invoke signatures must match native API exactly; parameters may be unused by managed callers.
#pragma warning disable IDE0060

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Identity.Client.Platforms.Features.DesktopOs
{
    internal static partial class WindowsNativeMethods
    {
        public enum NetJoinStatus
        {
            NetSetupUnknownStatus = 0,
            NetSetupUnjoined,
            NetSetupWorkgroupName,
            NetSetupDomainName
        }

        [LibraryImport("kernel32.dll")]
        public static partial uint GetCurrentProcessId();

        [LibraryImport("user32.dll", SetLastError = true)]
        public static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private const int PROCESSOR_ARCHITECTURE_AMD64 = 9;
        private const int PROCESSOR_ARCHITECTURE_ARM = 5;
        private const int PROCESSOR_ARCHITECTURE_IA64 = 6;
        private const int PROCESSOR_ARCHITECTURE_INTEL = 0;
        public const int ErrorSuccess = 0;

        [LibraryImport("kernel32.dll")]
        private static partial void GetNativeSystemInfo(ref SYSTEM_INFO lpSystemInfo);

        public static string GetProcessorArchitecture()
        {
            try
            {
                var systemInfo = new SYSTEM_INFO();
                GetNativeSystemInfo(ref systemInfo);
                return systemInfo.wProcessorArchitecture switch
                {
                    PROCESSOR_ARCHITECTURE_AMD64 or PROCESSOR_ARCHITECTURE_IA64 => "x64",
                    PROCESSOR_ARCHITECTURE_ARM => "ARM",
                    PROCESSOR_ARCHITECTURE_INTEL => "x86",
                    _ => "Unknown",
                };
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

        [LibraryImport("Netapi32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        public static partial int NetGetJoinInformation(string server, out IntPtr domain, out NetJoinStatus status);

        [LibraryImport("Netapi32.dll")]
        public static partial int NetApiBufferFree(IntPtr Buffer);

        [LibraryImport("user32.dll", EntryPoint = "GetDesktopWindow")]
        public static partial IntPtr GetDesktopWindow();

        [LibraryImport("kernel32.dll")]
        public static partial IntPtr GetConsoleWindow();

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct SYSTEM_INFO
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

    internal static partial class User32
    {
        private const string LibraryName = "user32.dll";

        public const int UOI_FLAGS = 1;
        public const int WSF_VISIBLE = 0x0001;

        [LibraryImport(LibraryName, SetLastError = true)]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
        public static partial IntPtr GetProcessWindowStation();

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
