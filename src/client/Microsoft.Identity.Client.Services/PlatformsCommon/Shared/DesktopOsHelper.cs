// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Identity.Client.Utils;

// TODO: bogavril - do we need this here? Can't it live exclusively on MSAL?

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    internal static class DesktopOsHelper
    {
        private static class Win32VersionApi
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
#if NET45
                var OsVersionInfo = new OSVERSIONINFOEXW { dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEXW)) };
#else
                    var OsVersionInfo = new OSVERSIONINFOEXW { dwOSVersionInfoSize = Marshal.SizeOf<OSVERSIONINFOEXW>() };
#endif
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

        private static class User32
        {
            private const string LibraryName = "user32.dll";

            public const int UOI_FLAGS = 1;
            public const int WSF_VISIBLE = 0x0001;

            [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
            public static extern IntPtr GetProcessWindowStation();

            [DllImport(LibraryName, EntryPoint = "GetUserObjectInformation", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
            public static extern unsafe bool GetUserObjectInformation(IntPtr hObj, int nIndex, void* pvBuffer, uint nLength, ref uint lpnLengthNeeded);
        }


        private static Lazy<bool> s_wamSupportedOSLazy = new Lazy<bool>(
           () => IsWamSupportedOSInternal());
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
        /// Checks if the OS supports WAM (Web Account Manager)
        /// WAM Supported OS's are Windows 10 and above for Client, Windows 2019 and above for Server
        /// </summary>
        /// <returns>Returns <c>true</c> if the Windows Version has WAM support</returns>
        private static bool IsWamSupportedOSInternal()
        {
#if WINDOWS_APP
            return true;
#elif SUPPORTS_WIN32
            if (IsWindows() && Win32VersionApi.IsWamSupportedOs())
            {
                return true;
            }

            return false;
#else
            return false;
#endif
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

    [StructLayout(LayoutKind.Sequential)]
    internal struct USEROBJECTFLAGS
    {
        public int fInherit;
        public int fReserved;
        public int dwFlags;
    }

    internal static class SecurityFramework
    {
        private const string SecurityFrameworkLib = "/System/Library/Frameworks/Security.framework/Security";

        public static readonly IntPtr Handle;
        public static readonly IntPtr kSecClass;
        public static readonly IntPtr kSecMatchLimit;
        public static readonly IntPtr kSecReturnAttributes;
        public static readonly IntPtr kSecReturnRef;
        public static readonly IntPtr kSecReturnPersistentRef;
        public static readonly IntPtr kSecClassGenericPassword;
        public static readonly IntPtr kSecMatchLimitOne;
        public static readonly IntPtr kSecMatchItemList;
        public static readonly IntPtr kSecAttrLabel;
        public static readonly IntPtr kSecAttrAccount;
        public static readonly IntPtr kSecAttrService;
        public static readonly IntPtr kSecValueRef;
        public static readonly IntPtr kSecValueData;
        public static readonly IntPtr kSecReturnData;

        static SecurityFramework()
        {
            Handle = LibSystem.dlopen(SecurityFrameworkLib, 0);

            kSecClass = LibSystem.GetGlobal(Handle, "kSecClass");
            kSecMatchLimit = LibSystem.GetGlobal(Handle, "kSecMatchLimit");
            kSecReturnAttributes = LibSystem.GetGlobal(Handle, "kSecReturnAttributes");
            kSecReturnRef = LibSystem.GetGlobal(Handle, "kSecReturnRef");
            kSecReturnPersistentRef = LibSystem.GetGlobal(Handle, "kSecReturnPersistentRef");
            kSecClassGenericPassword = LibSystem.GetGlobal(Handle, "kSecClassGenericPassword");
            kSecMatchLimitOne = LibSystem.GetGlobal(Handle, "kSecMatchLimitOne");
            kSecMatchItemList = LibSystem.GetGlobal(Handle, "kSecMatchItemList");
            kSecAttrLabel = LibSystem.GetGlobal(Handle, "kSecAttrLabel");
            kSecAttrAccount = LibSystem.GetGlobal(Handle, "kSecAttrAccount");
            kSecAttrService = LibSystem.GetGlobal(Handle, "kSecAttrService");
            kSecValueRef = LibSystem.GetGlobal(Handle, "kSecValueRef");
            kSecValueData = LibSystem.GetGlobal(Handle, "kSecValueData");
            kSecReturnData = LibSystem.GetGlobal(Handle, "kSecReturnData");
        }

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SessionGetInfo(int session, out int sessionId, out SessionAttributeBits attributes);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecAccessCreate(IntPtr descriptor, IntPtr trustedList, out IntPtr accessRef);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecKeychainItemCreateFromContent(IntPtr itemClass, IntPtr attrList, uint length,
            IntPtr data, IntPtr keychainRef, IntPtr initialAccess, out IntPtr itemRef);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecKeychainAddGenericPassword(
            IntPtr keychain,
            uint serviceNameLength,
            string serviceName,
            uint accountNameLength,
            string accountName,
            uint passwordLength,
            byte[] passwordData,
            out IntPtr itemRef);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecKeychainFindGenericPassword(
            IntPtr keychainOrArray,
            uint serviceNameLength,
            string serviceName,
            uint accountNameLength,
            string accountName,
            out uint passwordLength,
            out IntPtr passwordData,
            out IntPtr itemRef);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int SecKeychainItemCopyAttributesAndData(
            IntPtr itemRef,
            IntPtr info,
            IntPtr itemClass,
            SecKeychainAttributeList** attrList,
            uint* dataLength,
            void** data);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecKeychainItemModifyAttributesAndData(
            IntPtr itemRef,
            IntPtr attrList, // SecKeychainAttributeList*
            uint length,
            byte[] data);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecKeychainItemDelete(
            IntPtr itemRef);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecKeychainItemFreeContent(
            IntPtr attrList, // SecKeychainAttributeList*
            IntPtr data);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecKeychainItemFreeAttributesAndData(
            IntPtr attrList, // SecKeychainAttributeList*
            IntPtr data);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecItemCopyMatching(IntPtr query, out IntPtr result);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecKeychainItemCopyFromPersistentReference(IntPtr persistentItemRef, out IntPtr itemRef);

        [DllImport(SecurityFrameworkLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SecKeychainItemCopyContent(IntPtr itemRef, IntPtr itemClass, IntPtr attrList,
            out uint length, out IntPtr outData);

        public const int CallerSecuritySession = -1;

        // https://developer.apple.com/documentation/security/1542001-security_framework_result_codes
        public const int OK = 0;
        public const int ErrorSecNoSuchKeychain = -25294;
        public const int ErrorSecInvalidKeychain = -25295;
        public const int ErrorSecAuthFailed = -25293;
        public const int ErrorSecDuplicateItem = -25299;
        public const int ErrorSecItemNotFound = -25300;
        public const int ErrorSecInteractionNotAllowed = -25308;
        public const int ErrorSecInteractionRequired = -25315;
        public const int ErrorSecNoSuchAttr = -25303;

    }

    [Flags]
    internal enum SessionAttributeBits
    {
        SessionIsRoot = 0x0001,
        SessionHasGraphicAccess = 0x0010,
        SessionHasTty = 0x0020,
        SessionIsRemote = 0x1000,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SecKeychainAttributeInfo
    {
        public uint Count;
        public IntPtr Tag; // uint* (SecKeychainAttrType*)
        public IntPtr Format; // uint* (CssmDbAttributeFormat*)
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SecKeychainAttributeList
    {
        public uint Count;
        public IntPtr Attributes; // SecKeychainAttribute*
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SecKeychainAttribute
    {
        public SecKeychainAttrType Tag;
        public uint Length;
        public IntPtr Data;
    }

    internal enum CssmDbAttributeFormat : uint
    {
        String = 0,
        SInt32 = 1,
        UInt32 = 2,
        BigNum = 3,
        Real = 4,
        TimeDate = 5,
        Blob = 6,
        MultiUInt32 = 7,
        Complex = 8
    };

    internal enum SecKeychainAttrType : uint
    {
        // https://developer.apple.com/documentation/security/secitemattr/accountitemattr
        AccountItem = 1633903476,
    }

    internal static class LibSystem
    {
        private const string LibSystemLib = "/System/Library/Frameworks/System.framework/System";

        [DllImport(LibSystemLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr dlopen(string name, int flags);

        [DllImport(LibSystemLib, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr dlsym(IntPtr handle, string symbol);

        public static IntPtr GetGlobal(IntPtr handle, string symbol)
        {
            IntPtr ptr = dlsym(handle, symbol);
#if NET45
            var structure = Marshal.PtrToStructure(ptr, typeof(IntPtr));
#else
            var structure = Marshal.PtrToStructure<IntPtr>(ptr);
#endif
            return (IntPtr)structure;
        }
    }

    // TODO: bogavril - find a better place for these or remove completely?
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
}
