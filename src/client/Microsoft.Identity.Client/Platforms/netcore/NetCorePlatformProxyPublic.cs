// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs;
using Microsoft.Identity.Client.Platforms.Shared.NetStdCore;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.netcore
{
    /// <summary>
    /// Platform / OS specific logic.  No library (ADAL / MSAL) specific code should go in here.
    /// </summary>
    internal class NetCorePlatformProxyPublic : NetCorePlatformProxy
    {
        public NetCorePlatformProxyPublic(ILoggerAdapter logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Get the user logged in
        /// </summary>
        public override Task<string> GetUserPrincipalNameAsync()
        {
            const int NameUserPrincipal = 8;
            return Task.FromResult(GetUserPrincipalName(NameUserPrincipal));
        }

        private string GetUserPrincipalName(int nameFormat)
        {
            if (DesktopOsHelper.IsWindows())
            {
                uint userNameSize = 0;
                WindowsNativeMethods.GetUserNameEx(nameFormat, null, ref userNameSize);
                if (userNameSize == 0)
                {
                    throw new MsalClientException(
                        MsalError.GetUserNameFailed,
                        MsalErrorMessage.GetUserNameFailed,
                        new Win32Exception(Marshal.GetLastWin32Error()));
                }

                var sb = new StringBuilder((int)userNameSize);
                if (!WindowsNativeMethods.GetUserNameEx(nameFormat, sb, ref userNameSize))
                {
                    throw new MsalClientException(
                        MsalError.GetUserNameFailed,
                        MsalErrorMessage.GetUserNameFailed,
                        new Win32Exception(Marshal.GetLastWin32Error()));
                }

                return sb.ToString();
            }

            throw new PlatformNotSupportedException(
                "MSAL cannot determine the username (UPN) of the currently logged in user." +
                "For Integrated Windows Authentication and Username/Password flows, please use .WithUsername() before calling ExecuteAsync(). " +
                "For more details see https://aka.ms/msal-net-iwa");
        }

        protected override IWebUIFactory CreateWebUiFactory() => new NetCoreWebUIFactory();

        public override Task StartDefaultOsBrowserAsync(string url, bool isBrokerConfigured)
        {
            if (DesktopOsHelper.IsWindows())
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
                catch
                {
                    // hack because of this: https://github.com/dotnet/corefx/issues/10361
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start msedge {url}") { CreateNoWindow = true });
                }
            }
            else if (DesktopOsHelper.IsLinux())
            {
                string sudoUser = Environment.GetEnvironmentVariable("SUDO_USER");
                if (!string.IsNullOrWhiteSpace(sudoUser))
                {
                    throw new MsalClientException(
                        MsalError.LinuxXdgOpen,
                        MsalErrorMessage.LinuxOpenAsSudoNotSupported);
                }

                try
                {
                    bool opened = false;

                    foreach (string openTool in GetOpenToolsLinux(isBrokerConfigured))
                    {
                        if (TryGetExecutablePath(openTool, out string openToolPath))
                        {
                            OpenLinuxBrowser(openToolPath, url);
                            opened = true;
                            break;
                        }
                    }

                    if (!opened)

                    {
                        throw new MsalClientException(
                            MsalError.LinuxXdgOpen,
                            MsalErrorMessage.LinuxOpenToolFailed);
                    }
                }
                catch (Exception ex)
                {
                    throw new MsalClientException(
                        MsalError.LinuxXdgOpen,
                        MsalErrorMessage.LinuxOpenToolFailed,
                        ex);
                }
            }
            else if (DesktopOsHelper.IsMac())
            {
                Process.Start("/usr/bin/open", url);
            }
            else
            {
                throw new PlatformNotSupportedException(RuntimeInformation.OSDescription);
            }
            return Task.FromResult(0);

        }

        private void OpenLinuxBrowser(string openToolPath, string url)
        {
            ProcessStartInfo psi = new ProcessStartInfo(openToolPath, url)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process.Start(psi);

        }

        private string[] GetOpenToolsLinux(bool isBrokerConfigured)
        {
            if (isBrokerConfigured)
            {
                return new[] { "microsoft-edge", "xdg-open", "gnome-open", "kfmclient", "wslview" };
            }

            return new[] { "xdg-open", "gnome-open", "kfmclient", "microsoft-edge", "wslview" };
        }

        public override bool BrokerSupportsWamAccounts => true;

        /// <summary>
        /// Searches through PATH variable to find the path to the specified executable.
        /// </summary>
        /// <param name="executable">Executable to find the path for.</param>
        /// <param name="path">Location of the specified executable.</param>
        /// <returns></returns>
        private bool TryGetExecutablePath(string executable, out string path)
        {
            string pathEnvVar = Environment.GetEnvironmentVariable("PATH");
            if (pathEnvVar != null)
            {
                var paths = pathEnvVar.Split(':');
                foreach (var basePath in paths)
                {
                    path = Path.Combine(basePath, executable);
                    if (File.Exists(path))
                    {
                        return true;
                    }
                }
            }

            path = null;
            return false;
        }
    }
}
