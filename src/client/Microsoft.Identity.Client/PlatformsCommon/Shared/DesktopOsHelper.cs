﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client.Utils;

#if SUPPORTS_WIN32 
using Microsoft.Identity.Client.Platforms.Features.DesktopOs;
#endif

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{    
    internal static class DesktopOsHelper
    {
        private static Lazy<bool> s_wamSupportedOSLazy = new Lazy<bool>(
           IsWamSupportedOSInternal);
        private static Lazy<string> s_winVersionLazy = new Lazy<string>(
            GetWindowsVersionStringInternal);

        private static Lazy<bool> s_wslEnvLazy = new Lazy<bool>(IsWslEnv);

        public static bool IsWindows()
        {

#if NETFRAMEWORK
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
#elif SUPPORTS_WIN32
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
            return false;
#endif
        }

        public static bool IsLinux()
        {
#if __MOBILE__ 
            return false;
#elif NETFRAMEWORK
            return Environment.OSVersion.Platform == PlatformID.Unix;
#else
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
#endif
        }

        public static bool IsMac()
        {
#if __MOBILE__
            return false;
#elif NETFRAMEWORK
            return Environment.OSVersion.Platform == PlatformID.MacOSX;
#elif NET8_0_OR_GREATER
            string OSDescription = RuntimeInformation.OSDescription;
            return OSDescription.Contains("Darwin", StringComparison.OrdinalIgnoreCase);
#else
            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
#endif
        }

        private static bool IsWslEnv()
        {
            if (IsLinux()) {
                try
                {
                    var versionInfo = File.ReadAllText("/proc/version");
                    return versionInfo.Contains("Microsoft") || versionInfo.Contains("WSL");
                }
                catch
                {
                    return false; // if we can't read the file, we can't determine if it's WSL
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if the OS supports WAM (Web Account Manager)
        /// WAM Supported OS's are Windows 10 and above for Client, Windows 2019 and above for Server
        /// </summary>
        /// <returns>Returns <c>true</c> if the Windows Version has WAM support</returns>
        private static bool IsWamSupportedOSInternal()
        {
#if SUPPORTS_WIN32
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
#if NETFRAMEWORK
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

        public static bool IsRunningOnWsl()
        {
            return s_wslEnvLazy.Value;
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
            int error = SecurityFramework.SessionGetInfo(SecurityFramework.CallerSecuritySession, out int _, out var sessionFlags);

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

        private static readonly Lazy<bool> _isMacConsoleApp = new Lazy<bool>(() => {
#if SUPPORTS_WIN32
            return !LibObjc.IsNsApplicationRunning();
#else
            return true;
#endif
        });

        public static bool IsMacConsoleApp()
        {
            if (!DesktopOsHelper.IsMac())
                return false;
            // Checking if NsApplication is running for one time would be enough.
            return _isMacConsoleApp.Value;
        }
    }
}
