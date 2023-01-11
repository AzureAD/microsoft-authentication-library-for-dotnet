// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client.Utils;

#if SUPPORTS_WIN32 && !WINDOWS_APP
using Microsoft.Identity.Client.Platforms.Features.DesktopOs;
#endif

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    internal static class DesktopOsHelper2
    {
        private static readonly Lazy<bool> s_wamSupportedOSLazy = new Lazy<bool>(
           () => IsWamSupportedOSInternal());
        private static readonly Lazy<string> s_winVersionLazy = new Lazy<string>(
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
            throw new PlatformNotSupportedException();
        }
    }
}
