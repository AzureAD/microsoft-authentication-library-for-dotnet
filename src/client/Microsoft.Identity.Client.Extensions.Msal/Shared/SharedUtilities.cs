// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Extensions.Msal
{
    /// <summary>
    /// A set of utilities shared between service and client
    /// </summary>
    public static class SharedUtilities
    {
        /// <summary>
        /// default base cache path
        /// </summary>
        private static readonly string s_homeEnvVar = Environment.GetEnvironmentVariable("HOME");
        private static readonly string s_lognameEnvVar = Environment.GetEnvironmentVariable("LOGNAME");
        private static readonly string s_userEnvVar = Environment.GetEnvironmentVariable("USER");
        private static readonly string s_lNameEnvVar = Environment.GetEnvironmentVariable("LNAME");
        private static readonly string s_usernameEnvVar = Environment.GetEnvironmentVariable("USERNAME");

        private static readonly Lazy<bool> s_isMono = new Lazy<bool>(() => Type.GetType("Mono.Runtime") != null);

        private static string s_processName = null;
        private static int s_processId = default(int);

        /// <summary>
        ///  Is this a windows platform
        /// </summary>
        /// <returns>A  value indicating if we are running on windows or not</returns>
        public static bool IsWindowsPlatform()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
        }

        /// <summary>
        /// Is this a MAC platform
        /// </summary>
        /// <returns>A value indicating if we are running on mac or not</returns>
        public static bool IsMacPlatform()
        {
#if NET45_OR_GREATER
            // we have to also check for PlatformID.Unix because Mono can sometimes return Unix as the platform on a Mac machine.
            // see http://www.mono-project.com/docs/faq/technical/
            return Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix;
#else
            return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX);
#endif
        }

        /// <summary>
        /// Is this a linux platform
        /// </summary>
        /// <returns>A  value indicating if we are running on linux or not</returns>
        public static bool IsLinuxPlatform()
        {
#if NET45_OR_GREATER
            return Environment.OSVersion.Platform == PlatformID.Unix;
#else
            return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);
#endif
        }

        /// <summary>
        ///  Is this running on mono
        /// </summary>
        /// <returns>A  value indicating if we are running on mono or not</returns>
        internal static bool IsMonoPlatform()
        {
            return s_isMono.Value;
        }

        /// <summary>
        /// Instantiates the process if not done already and retrieves the id of the process.
        /// Caches it for the next call.
        /// </summary>
        /// <returns>process id</returns>
        internal static int GetCurrentProcessId()
        {
            if (s_processId == default)
            {
                using (var process = Process.GetCurrentProcess())
                {
                    s_processId = process.Id;
                    s_processName = process.ProcessName;
                }
            }

            return s_processId;
        }

        /// <summary>
        /// Instantiates the process if not done already and retrieves the name of the process.
        /// Caches it for the next call
        /// </summary>
        /// <returns>process name</returns>
        internal static string GetCurrentProcessName()
        {
            if (string.IsNullOrEmpty(s_processName))
            {
                using (var process = Process.GetCurrentProcess())
                {
                    s_processName = process.ProcessName;
                    s_processId = process.Id;
                }
            }

            return s_processName;
        }

        /// <summary>
        /// Generate the default file location
        /// </summary>
        /// <returns>Root directory</returns>
        public static string GetUserRootDirectory()
        {
            return !IsWindowsPlatform()
                ? GetUserHomeDirOnUnix()
                : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }       

        private static string GetUserHomeDirOnUnix()
        {
            if (IsWindowsPlatform())
            {
                throw new NotSupportedException();
            }

            if (!string.IsNullOrEmpty(s_homeEnvVar))
            {
                return s_homeEnvVar;
            }

            string username = null;
            if (!string.IsNullOrEmpty(s_lognameEnvVar))
            {
                username = s_lognameEnvVar;
            }
            else if (!string.IsNullOrEmpty(s_userEnvVar))
            {
                username = s_userEnvVar;
            }
            else if (!string.IsNullOrEmpty(s_lNameEnvVar))
            {
                username = s_lNameEnvVar;
            }
            else if (!string.IsNullOrEmpty(s_usernameEnvVar))
            {
                username = s_usernameEnvVar;
            }

            if (IsMacPlatform())
            {
                return !string.IsNullOrEmpty(username) ? Path.Combine("/Users", username) : null;
            }
            else if (IsLinuxPlatform())
            {
                if (LinuxNativeMethods.getuid() == LinuxNativeMethods.RootUserId)
                {
                    return "/root";
                }
                else
                {
                    return !string.IsNullOrEmpty(username) ? Path.Combine("/home", username) : null;
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
