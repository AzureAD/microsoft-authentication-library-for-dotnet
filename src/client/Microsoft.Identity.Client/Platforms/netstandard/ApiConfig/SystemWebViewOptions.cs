﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.PlatformsCommon.Factories;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Use Microsoft Edge to navigate to the given URI. On non-windows platforms it uses 
    /// whatever browser is the default.
    /// </summary>
    public partial class SystemWebViewOptions
    {
        /// <summary>
        /// Use Microsoft Edge to navigate to the given URI. On non-windows platforms it uses 
        /// whatever browser is the default.
        /// </summary>
        public static async Task OpenWithEdgeBrowserAsync(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            string url = uri.AbsoluteUri;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start microsoft-edge:{url}") { CreateNoWindow = true });
            }
            else
            {
                var proxy = PlatformProxyFactory.CreatePlatformProxy(new NullLogger());
                await proxy.StartDefaultOsBrowserAsync(url, true).ConfigureAwait(false);
            }

        }

        /// <summary>
        /// Use Microsoft Edge Chromium to navigate to the given URI. Requires the browser to be installed.
        /// On Linux, open edge if available otherwise open the default browser.
        /// </summary>
        public static async Task OpenWithChromeEdgeBrowserAsync(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            string url = uri.AbsoluteUri;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start msedge {url}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var proxy = PlatformProxyFactory.CreatePlatformProxy(new NullLogger());
                await proxy.StartDefaultOsBrowserAsync(url, true).ConfigureAwait(false);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("msedge", url);
            }
            else
            {
                throw new PlatformNotSupportedException(RuntimeInformation.OSDescription);
            }
        }
    }
}
