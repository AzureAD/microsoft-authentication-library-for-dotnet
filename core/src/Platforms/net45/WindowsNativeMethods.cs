using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Identity.Core.Platforms
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
                SYSTEM_INFO systemInfo = new SYSTEM_INFO();
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
            catch (Exception ex)
            {
                CoreLoggerBase.Default.Warning(ex.Message);
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
