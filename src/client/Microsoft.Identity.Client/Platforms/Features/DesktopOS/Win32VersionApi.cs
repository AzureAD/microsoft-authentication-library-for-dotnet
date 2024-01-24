// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{

    /// <summary>
    /// Windows OS Version checks
    /// </summary>
    /// <remarks>Do not include this code in UWP, it causes packaging errors</remarks>
    internal static class Win32VersionApi
    {

        #region ProductType    
        /// <summary>
        /// The operating system is Windows 10, Windows 8, Windows 7,...
        /// </summary>
        /// <remarks>VER_NT_WORKSTATION</remarks>
        private const byte VER_NT_WORKSTATION = 0x0000001;

        /// <summary>
        /// The system is a domain controller and the operating system is Windows Server.
        /// </summary>
        /// <remarks>VER_NT_DOMAIN_CONTROLLER</remarks>
        private const byte VER_NT_DOMAIN_CONTROLLER = 0x0000002;

        /// <summary>
        /// The operating system is Windows Server. Note that a server that is also a domain controller
        /// is reported as VER_NT_DOMAIN_CONTROLLER, not VER_NT_SERVER.
        /// </summary>
        /// <remarks>VER_NT_SERVER</remarks>
        private const byte VER_NT_SERVER = 0x0000003;
        #endregion ProductType

        /// <summary>
        /// RtlGetVersion returns STATUS_SUCCESS.
        /// </summary>
        /// <remarks>NT_STATUS</remarks>
        private const byte NT_STATUS_SUCCESS = 0x00000000;

        /// <summary>
        /// Microsoft 365 apps (for example, Office client apps) use Azure Active Directory Authentication Library (ADAL) 
        /// framework-based Modern Authentication by default. Starting with build 16.0.7967, Microsoft 365 apps use 
        /// Web Account Manager (WAM) for sign-in workflows on Windows builds that are later than 15000 
        /// (Windows 10, version 1703, build 15063.138).
        /// https://docs.microsoft.com/en-us/office365/troubleshoot/administration/disabling-adal-wam-not-recommended
        /// </summary>
        private const int WamSupportedWindows10BuildNumber = 15063;

        /// <summary>
        /// Windows Server 2019 (version 1809, Build Number 17763)
        /// Editions : Datacenter, Essentials, Standard
        /// https://docs.microsoft.com/en-us/windows-server/get-started/windows-server-release-info
        /// For MultiSession Window 10 Build Number is same as Windows 2019 Server Build Number
        /// MultiSession Windows 10 is supported from Windows 10 multi-session, version 1903
        /// https://docs.microsoft.com/en-us/mem/intune/fundamentals/azure-virtual-desktop-multi-session
        /// </summary>
        private const int Windows2019BuildNumber = 17763;

        /// <summary>
        /// RtlGetVersion is the kernel-mode equivalent of the user-mode GetVersionEx function in the Windows SDK
        /// The RtlGetVersion routine returns version information about the currently running operating system.
        /// https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/nf-wdm-rtlgetversion
        /// When using RtlGetVersion to determine whether a particular version of the operating system is running, 
        /// a caller should check for version numbers that are greater than or equal to the required version number. 
        /// This ensures that a version test succeeds for later versions of Windows.
        /// </summary>
        /// <param name="versionInformation">Pointer to either a RTL_OSVERSIONINFOW structure or a RTL_OSVERSIONINFOEXW 
        /// structure that contains the version information about the currently running operating system. A caller specifies 
        /// which input structure is used by setting the dwOSVersionInfoSize member of the structure to the size in bytes of 
        /// the structure that is used.</param>
        /// <returns>RtlGetVersion returns Status_Success.</returns>
        [DllImport("ntdll.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int RtlGetVersion(ref OSVERSIONINFOEXW versionInformation);

        /// <summary>
        /// Contains operating system version information. The information includes major and minor version numbers, 
        /// a build number, a platform identifier, and information about product suites and the latest Service Pack 
        /// installed on the system. 
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct OSVERSIONINFOEXW
        {
            /// <summary>
            /// The size, in bytes, of an RTL_OSVERSIONINFOEXW structure. 
            /// </summary>
            public int dwOSVersionInfoSize;

            /// <summary>
            /// he major version number of the operating system. 
            /// For example, for Windows 2000, the major version number is five.
            /// </summary>
            public int dwMajorVersion;

            /// <summary>
            /// The minor version number of the operating system. 
            /// For example, for Windows 2000, the minor version number is zero.
            /// </summary>
            public int dwMinorVersion;

            /// <summary>
            /// The build number of the operating system.
            /// </summary>
            public int dwBuildNumber;

            /// <summary>
            /// The operating system platform. This member can be VER_PLATFORM_WIN32_NT (2).
            /// </summary>
            public int dwPlatformId;

            /// <summary>
            /// A null-terminated string, such as "Service Pack 3", that indicates the latest Service Pack 
            /// installed on the system. If no Service Pack has been installed, the string is empty.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string scZSDVersion;

            /// <summary>
            /// The major version number of the latest Service Pack installed on the system. 
            /// For example, for Service Pack 3, the major version number is 3. 
            /// If no Service Pack has been installed, the value is zero.
            /// </summary>
            public ushort wServicePackMajor;

            /// <summary>
            /// The minor version number of the latest Service Pack installed on the system. 
            /// For example, for Service Pack 3, the minor version number is 0.
            /// </summary>
            public ushort wServicePackMinor;

            /// <summary>
            /// A bit mask that identifies the product suites available on the system. 
            /// This member can be a combination of the following values.
            /// </summary>
            public short wSuiteMask;

            /// <summary>
            /// The product type. This member contains additional information about the system. 
            /// This member can be one of the following values: VER_NT_WORKSTATION, VER_NT_DOMAIN_CONTROLLER, VER_NT_SERVER
            /// </summary>
            public byte wProductType;

            /// <summary>
            /// Reserved for future use.
            /// </summary>
            public byte wReserved;
        }

        /// <summary>
        /// Checks if the OS supports WAM (Web Account Manager)
        /// WAM Supported OS's are Windows 10 and above for Client, Windows 2019 and above for Server
        /// </summary>
        /// <returns>Returns <c>true</c> if the OS Version has WAM support</returns>
        public static bool IsWamSupportedOs()
        {
            try
            {
                var OsVersionInfo = new OSVERSIONINFOEXW { dwOSVersionInfoSize = Marshal.SizeOf<OSVERSIONINFOEXW>() };
                if (RtlGetVersion(ref OsVersionInfo) == NT_STATUS_SUCCESS)
                {
                    switch (OsVersionInfo.wProductType)
                    {
                        //To filter client operating systems only, such as Windows 10 or Windows 11 Clients, use ProductType="1". 
                        case VER_NT_WORKSTATION:
                            switch (OsVersionInfo.dwMajorVersion)
                            {
                                //https://docs.microsoft.com/en-us/windows/win32/sysinfo/operating-system-version
                                //For Client (Windows 10 and 11) and for Server (Windows 2016 and above) Major version is 10.*
                                //Windows 10 Build Number 15063 is the minimum version where WAM is supported
                                case 10:
                                    if (OsVersionInfo.dwBuildNumber >= WamSupportedWindows10BuildNumber)
                                        return true;
                                    else
                                        return false;

                                default:
                                    return false;
                            }

                        //For server operating systems that are not domain controllers and for Windows 10 and Windows 11 multi-session, use ProductType="3". 
                        //https://docs.microsoft.com/en-us/windows/security/threat-protection/windows-firewall/create-wmi-filters-for-the-gpo
                        case VER_NT_SERVER:
                        case VER_NT_DOMAIN_CONTROLLER:
                            switch (OsVersionInfo.dwMajorVersion)
                            {
                                //https://docs.microsoft.com/en-us/windows/win32/sysinfo/operating-system-version
                                //For Client (Windows 10 and 11) and for Server (Windows 2016 and above) Major version is 10.*
                                //Windows Server 2019 minimum build number is 17763
                                case 10:
                                    if (OsVersionInfo.dwBuildNumber >= Windows2019BuildNumber)
                                        return true;
                                    else
                                        return false;

                                default:
                                    return false;
                            }

                        default:
                            return false;

                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }

}
