﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client.Utils;
using Microsoft.Win32;

#if SUPPORTS_WIN32 && !WINDOWS_APP
using Microsoft.Identity.Client.Platforms.Features.DesktopOs;
#endif

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    internal static class DesktopOsHelper
    {
        private static Lazy<bool> s_win10OrServerEquivalentLazy = new Lazy<bool>(
           () => IsWin10OrServerEquivalentInternal());
        private static Lazy<bool> s_win10Lazy = new Lazy<bool>(
            () => IsWin10Internal());
        private static Lazy<string> s_winVersionLazy = new Lazy<string>(
            () => GetWindowsVersionStringInternal());
        private static Lazy<string> s_runtimeVersionLazy = new Lazy<string>(
            () => GetRuntimeVersionInternal());

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

        public static bool IsWin10()
        {
            return s_win10Lazy.Value;
        }

        private static bool IsWin10Internal()
        {
            if (IsWindows())
            {
                string winVersion = GetWindowsVersionString();

                if (winVersion.Contains("Windows 10", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsWin10OrServerEquivalentInternal()
        {
            if (IsWindows())
            {
                string winVersion = GetWindowsVersionString();

                if (winVersion.Contains("Windows 10", StringComparison.OrdinalIgnoreCase) ||
                    winVersion.Contains("Windows Server 2016", StringComparison.OrdinalIgnoreCase) ||
                    winVersion.Contains("Windows Server 2019", StringComparison.OrdinalIgnoreCase))
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
            return s_win10OrServerEquivalentLazy.Value;
        }

        private static string GetRuntimeVersionInternal()
        {
#if DESKTOP
            // https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed#query-the-registry-using-code
            try
            {
                string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";
                using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
                {
                    if (ndpKey?.GetValue("Release") != null)
                    {
                        int releaseKey = (int)ndpKey.GetValue("Release");
                        if (releaseKey >= 528040)
                            return "4.8 or later";
                        if (releaseKey >= 461808)
                            return "4.7.2";
                        if (releaseKey >= 461308)
                            return "4.7.1";
                        if (releaseKey >= 460798)
                            return "4.7";
                        if (releaseKey >= 394802)
                            return "4.6.2";
                        if (releaseKey >= 394254)
                            return "4.6.1";
                        if (releaseKey >= 393295)
                            return "4.6";
                        if (releaseKey >= 379893)
                            return "4.5.2";
                        if (releaseKey >= 378675)
                            return "4.5.1";
                        if (releaseKey >= 378389)
                            return "4.5";
                    }
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
#else
            return RuntimeInformation.FrameworkDescription;
#endif
        }

        public static string GetRuntimeVersion() => s_runtimeVersionLazy.Value;
      
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
