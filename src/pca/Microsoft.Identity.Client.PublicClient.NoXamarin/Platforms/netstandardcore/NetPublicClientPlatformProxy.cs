// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NET_CORE || NETSTANDARD

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Platforms.Features.Windows;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.netstandardcore
{
    class NetPublicClientPlatformProxy : NetPlatformProxy, IPublicClientPlatformProxy
    {
        public NetPublicClientPlatformProxy(ICoreLogger logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Get the user logged in
        /// </summary>
        public Task<string> GetUserPrincipalNameAsync()
        {
            const int NameUserPrincipal = 8;
            return Task.FromResult(GetUserPrincipalName(NameUserPrincipal));
        }

        private string GetUserPrincipalName(int nameFormat)
        {
            if (IsWindowsPlatform())
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

        public IWebUIFactory CreateWebUiFactory() => new NetWebUIFactory();


        public override Task StartDefaultOsBrowserAsync(string url)
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
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start msedge {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    try
                    {
                        Process.Start("xdg-open", url);
                    }
                    catch (Exception ex)
                    {
                        throw new MsalClientException(
                            MsalError.LinuxXdgOpen,
                            "Unable to open a web page using xdg-open. See inner exception for details. Possible causes for this error are: xdg-open is not installed or " +
                            "it cannot find a way to open an url - make sure you can open a web page by invoking from a terminal: xdg-open https://www.bing.com ",
                            ex);
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw new PlatformNotSupportedException(RuntimeInformation.OSDescription);
                }
            }

            return Task.FromResult(0);
        }



        public IWebUIFactory OverloadWebUiFactory { get; set; }

        public IWebUIFactory GetWebUiFactory()
        {
            return OverloadWebUiFactory ?? CreateWebUiFactory();
        }

        public void SetWebUiFactory(IWebUIFactory webUiFactory)
        {
            OverloadWebUiFactory = webUiFactory;
        }



        public bool IsSystemWebViewAvailable => true;

        public bool UseEmbeddedWebViewDefault => false;

        public virtual IBroker CreateBroker(CoreUIParent uiParent)
        {
            return OverloadBrokerForTest ?? new NullBroker();
        }
    }
}
#endif
