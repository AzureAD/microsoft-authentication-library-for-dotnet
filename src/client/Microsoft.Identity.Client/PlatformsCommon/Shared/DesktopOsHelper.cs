// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client.Utils;
using System.Security;

#if SUPPORTS_WIN32 && !WINDOWS_APP
using Microsoft.Identity.Client.Platforms.Features.DesktopOs;
#endif

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    internal static class DesktopOsHelper
    {
        #region ProductType    
        /// <summary>
        /// The system is a domain controller and the operating system is Windows Server.
        /// </summary>
        /// <remarks>VER_NT_DOMAIN_CONTROLLER</remarks>
        private const byte VerNtDomainController = 0x0000002;

        /// <summary>
        /// The operating system is Windows Server. Note that a server that is also a domain controller
        /// is reported as VER_NT_DOMAIN_CONTROLLER, not VER_NT_SERVER.
        /// </summary>
        /// <remarks>VER_NT_SERVER</remarks>
        private const byte VerNtServer = 0x0000003;

        /// <summary>
        /// The operating system is Windows 10, Windows 8, Windows 7,...
        /// </summary>
        /// <remarks>VER_NT_WORKSTATION</remarks>
        private const byte VerNtWorkstation = 0x0000001;
        #endregion ProductType

        /// <summary>
        /// RtlGetVersion returns STATUS_SUCCESS.
        /// </summary>
        /// <remarks>NT_STATUS</remarks>
        private const byte NtStatusSuccess = 0x00000000;

        /// <summary>
        /// Microsoft 365 apps (for example, Office client apps) use Azure Active Directory Authentication Library (ADAL) 
        /// framework-based Modern Authentication by default. Starting with build 16.0.7967, Microsoft 365 apps use 
        /// Web Account Manager (WAM) for sign-in workflows on Windows builds that are later than 15000 
        /// (Windows 10, version 1703, build 15063.138).
        /// https://docs.microsoft.com/en-us/office365/troubleshoot/administration/disabling-adal-wam-not-recommended
        /// </summary>
        private static readonly int s_windows10MinimumSupportedBuildNumber = 15063;

        /// <summary>
        /// Windows Server 2019 (version 1809)
        /// Editions : Datacenter, Essentials, Standard
        /// https://docs.microsoft.com/en-us/windows-server/get-started/windows-server-release-info
        /// </summary>
        private static readonly int s_windows2019MinimumSupportedBuildNumber = 17763;

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
        [SecurityCritical]
        [DllImport("ntdll.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int RtlGetVersion(ref OSVERSIONINFOEXW versionInformation); // return type should be the NtStatus enum

        /// <summary>
        /// Contains operating system version information. The information includes major and minor version numbers, 
        /// a build number, a platform identifier, and information about product suites and the latest Service Pack 
        /// installed on the system. This structure is used with the GetVersionEx and VerifyVersionInfo functions.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct OSVERSIONINFOEXW
        {
            /// <summary>
            /// The size, in bytes, of an RTL_OSVERSIONINFOEXW structure. 
            /// </summary>
            public int OSVersionInfoSize;

            /// <summary>
            /// The major version number of the operating system. 
            /// For example, for Windows 2000, the major version number is five.
            /// </summary>
            public int MajorVersion;

            /// <summary>
            /// The minor version number of the operating system. 
            /// For example, for Windows 2000, the minor version number is zero.
            /// </summary>
            public int MinorVersion;

            /// <summary>
            /// The build number of the operating system.
            /// </summary>
            public int BuildNumber;

            /// <summary>
            /// The operating system platform. This member can be VER_PLATFORM_WIN32_NT (2).
            /// </summary>
            public int PlatformId;

            /// <summary>
            /// A null-terminated string, such as "Service Pack 3", that indicates the latest Service Pack 
            /// installed on the system. If no Service Pack has been installed, the string is empty.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string SDVersion;

            /// <summary>
            /// The major version number of the latest Service Pack installed on the system. 
            /// For example, for Service Pack 3, the major version number is 3. 
            /// If no Service Pack has been installed, the value is zero.
            /// </summary>
            public ushort ServicePackMajor;

            /// <summary>
            /// The minor version number of the latest Service Pack installed on the system. 
            /// For example, for Service Pack 3, the minor version number is 0.
            /// </summary>
            public ushort ServicePackMinor;

            /// <summary>
            /// A bit mask that identifies the product suites available on the system. 
            /// This member can be a combination of the following values.
            /// </summary>
            public short SuiteMask;

            /// <summary>
            /// The product type. This member contains additional information about the system. 
            /// This member can be one of the following values: VER_NT_WORKSTATION, VER_NT_DOMAIN_CONTROLLER, VER_NT_SERVER
            /// </summary>
            public byte ProductType;

            /// <summary>
            /// Reserved for future use.
            /// </summary>
            public byte Reserved;
        }

        private static Lazy<bool> s_wamSupportedOSLazy = new Lazy<bool>(
           () => IsWamSuportedOSInternal());
        private static Lazy<string> s_winVersionLazy = new Lazy<string>(
            () => GetWindowsVersionStringInternal());

        public static bool IsWindows()
        {
#if WINDOWS_APP
        return true;
#else


#if DESKTOP
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
#elif SUPPORTS_WIN32
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
            return false;
#endif

#endif
        }

        public static bool IsWin32()
        {
#if WINDOWS_APP
            return false;
#else

            return IsWindows();
#endif
        }

        public static bool IsXamarinOrUwp()
        {
#if IS_XAMARIN_OR_UWP
            return true;
#else
            return false;
#endif
        }

        public static bool IsLinux()
        {
#if IS_XAMARIN_OR_UWP
            return false;
#elif DESKTOP
            return Environment.OSVersion.Platform == PlatformID.Unix;
#else
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
#endif
        }

        public static bool IsMac()
        {
#if MAC
            return true;
#elif DESKTOP
            return Environment.OSVersion.Platform == PlatformID.MacOSX;
#elif !IS_XAMARIN_OR_UWP
            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
#else
            return false;
#endif
        }

        /// <summary>
        /// Returns true if the OS supports WAM (Web Account Manager)
        /// WAM Supported OS's are Windows 10 and above for Client, Windows 2019 and above for Server
        /// </summary>
        /// <returns>Returns <c>true</c> if the OS Version has WAM support</returns>
        private static bool IsWamSupportedOs()
        {
            var OsVersionInfo = new OSVERSIONINFOEXW { OSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEXW)) };

            if (RtlGetVersion(ref OsVersionInfo) == NtStatusSuccess)
            {
                switch (OsVersionInfo.ProductType)
                {
                    //When OS Installation Type is Client 
                    case VerNtWorkstation:
                        switch (OsVersionInfo.MajorVersion)
                        {
                            case 10:
                                if (OsVersionInfo.BuildNumber >= s_windows10MinimumSupportedBuildNumber)
                                    return true;
                                else
                                    return false;

                            default:
                                return false;
                        }

                    //When OS Installation Type is Server or Multi Session Client OS's
                    //https://docs.microsoft.com/en-us/windows/security/threat-protection/windows-firewall/create-wmi-filters-for-the-gpo
                    //For server operating systems that are not domain controllers and for Windows 10 and Windows 11 multi-session, use ProductType="3".
                    //For domain controllers only, use ProductType="2". 
                    case VerNtServer:
                    case VerNtDomainController:
                        switch (OsVersionInfo.MajorVersion)
                        {
                            case 10:
                                if (OsVersionInfo.BuildNumber >= s_windows2019MinimumSupportedBuildNumber)
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Returns <c>true</c> if the Windows Version has WAM support</returns>
        private static bool IsWamSuportedOSInternal()
        {
            if (IsWindows())
            {

                if (IsWamSupportedOs())
                {
                    return true;
                }
            }

            return false;
        }
        private static string GetWindowsVersionStringInternal()
        {
            //Environment.OSVersion as it will return incorrect information on some operating systems
            //For more information on how to acquire the current OS version from the registry
            //See (https://stackoverflow.com/a/61914068)
#if DESKTOP
            var reg = Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");

            string OSInfo = (string)reg.GetValue("ProductName");

            if (string.IsNullOrEmpty(OSInfo))
            {
                return Environment.OSVersion.ToString();
            }

            return OSInfo;
#else
            return RuntimeInformation.OSDescription;
#endif
        }

        public static string GetWindowsVersionString()
        {
            return s_winVersionLazy.Value;
        }

        public static bool IsWin10OrServerEquivalent()
        {
            return s_wamSupportedOSLazy.Value;
        }

        public static bool IsUserInteractive()
        {

#if SUPPORTS_WIN32
            if (IsWindows())
            {
                return IsInteractiveSessionWindows();
            }
            if (IsMac())
            {
                return IsInteractiveSessionMac();
            }
            if (IsLinux())
            {
                return IsInteractiveSessionLinux();
            }

#endif
            throw new PlatformNotSupportedException();
        }

#if SUPPORTS_WIN32

        private static unsafe bool IsInteractiveSessionWindows()
        {

            // Environment.UserInteractive is hard-coded to return true for POSIX and Windows platforms on .NET Core 2.x and 3.x.
            // In .NET 5 the implementation on Windows has been 'fixed', but still POSIX versions always return true.
            //
            // This code is lifted from the .NET 5 targeting dotnet/runtime implementation for Windows:
            // https://github.com/dotnet/runtime/blob/cf654f08fb0078a96a4e414a0d2eab5e6c069387/src/libraries/System.Private.CoreLib/src/System/Environment.Windows.cs#L125-L145

            // Per documentation of GetProcessWindowStation, this handle should not be closed
            IntPtr handle = User32.GetProcessWindowStation();
            if (handle != IntPtr.Zero)
            {
                USEROBJECTFLAGS flags = default;
                uint dummy = 0;
                if (User32.GetUserObjectInformation(handle, User32.UOI_FLAGS, &flags,
                    (uint)sizeof(USEROBJECTFLAGS), ref dummy))
                {
                    return (flags.dwFlags & User32.WSF_VISIBLE) != 0;
                }
            }

            // If we can't determine, return true optimistically
            // This will include cases like Windows Nano which do not expose WindowStations
            return true;

        }

        private static bool IsInteractiveSessionMac()
        {

            // Get information about the current session
            int error = SecurityFramework.SessionGetInfo(SecurityFramework.CallerSecuritySession, out int id, out var sessionFlags);

            // Check if the session supports Quartz
            if (error == 0 && (sessionFlags & SessionAttributeBits.SessionHasGraphicAccess) != 0)
            {
                return true;
            }

            // Fall-through and check if X11 is available on macOS
            return IsInteractiveSessionLinux();

        }

        private static bool IsInteractiveSessionLinux()
        {
            return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DISPLAY"));
        }
#endif
    }
}
