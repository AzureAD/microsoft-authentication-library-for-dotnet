// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AuthScheme.PoP;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.UI;
using Microsoft.Win32;

namespace Microsoft.Identity.Client.Platforms.net45
{
    /// <summary>
    ///     Platform / OS specific logic.
    /// </summary>
    internal class NetDesktopPlatformProxyPublic : NetDesktopPlatformProxy
    {
        /// <inheritdoc />
        public NetDesktopPlatformProxyPublic(ILoggerAdapter logger)
            : base(logger)
        {
        }

        /// <summary>
        ///     Get the user logged in to Windows or throws
        /// </summary>
        /// <returns>Upn or throws</returns>
        public override Task<string> GetUserPrincipalNameAsync()
        {
            const int NameUserPrincipal = 8;
            return Task.FromResult(GetUserPrincipalName(NameUserPrincipal));
        }

        private string GetUserPrincipalName(int nameFormat)
        {
            // TODO: there is discrepancy between the implementation of this method on net45 - throws if upn not found - and uap and
            // the rest of the platforms - returns ""

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

        /// <inheritdoc />
        protected override IWebUIFactory CreateWebUiFactory()
        {
            return new NetDesktopWebUIFactory();
        }

        public override Task StartDefaultOsBrowserAsync(string url, bool isBrokerConfigured)
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
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }

            return Task.FromResult(0);
        }

        public override bool BrokerSupportsWamAccounts => true;
    }
}
