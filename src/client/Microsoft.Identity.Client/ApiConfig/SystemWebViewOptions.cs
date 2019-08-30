// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Factories;

namespace Microsoft.Identity.Client
{
    // Default browser WebUI is not available on mobile (Android, iOS, UWP), but allow it at runtime
    // to avoid MissingMethodException

    /// <summary>
    /// Options for using the default OS browser as a separate process to handle interactive auth.
    /// MSAL will be listening for the OS browser to finish authenticating, but it cannot close the browser.
    /// It can however respond with a 200 OK message or a 302 Redirect, which can be configured here.
    /// For more details see https://aka.ms/msal-net-os-browser
    /// </summary>
#if NET_CORE || NETSTANDARD || DESKTOP || RUNTIME
    public
#else
    internal
#endif
    class SystemWebViewOptions
    {
        /// <summary>
        /// When the user finishes authenticating, MSAL will respond with a 200 OK message,
        /// which the browser will show to the user. 
        /// </summary>
        public string HtmlMessageSuccess { get; set; }

        /// <summary>
        /// When the user finishes authenticating, but an error occurred,
        /// MSAL will respond with a 200 OK message, which the browser will show to the user.
        /// You can use a string format e.g. "An error has occurred: {0} details: {1}"
        /// </summary>
        public string HtmlMessageError { get; set; }

        /// <summary>
        /// When the user finishes authenticating, MSAL will redirect the browser to the given Uri
        /// </summary>
        /// <remarks>Takes precedence over <see cref="HtmlMessageSuccess"/></remarks>
        public Uri BrowserRedirectSuccess { get; set; }

        /// <summary>
        /// When the user finishes authenticating, but an error occurred, MSAL will redirect the browser to the given Uri
        /// </summary>
        /// <remarks>Takes precedence over <see cref="HtmlMessageError"/></remarks>
        public Uri BrowserRedirectError { get; set; }

        /// <summary>
        /// Allows developers to implement their own logic for starting a browser and navigating to a specific Uri. MSAL
        /// will use this when opening the browser. Leave it null and the user configured browser will be used.
        /// Consider using the static helpers OpenWithEdgeBrowserAsync and OpenWithChromeEdgeBrowserAsync
        /// </summary>
        /// <remarks>This property is experimental and the signature may change without a major version increment.</remarks>
        public Func<Uri, Task> OpenBrowserAsync { get; set; }

        internal void LogParameters(ICoreLogger logger)
        {
            logger.Info("DefaultBrowserOptions configured");

            logger.InfoPii("HtmlMessageSuccess " + HtmlMessageSuccess,
                "HtmlMessageSuccess? " + !String.IsNullOrEmpty(HtmlMessageSuccess));
            logger.InfoPii("HtmlMessageError " + HtmlMessageError,
               "HtmlMessageError? " + !String.IsNullOrEmpty(HtmlMessageError));
            logger.InfoPii("BrowserRedirectSuccess " + BrowserRedirectSuccess,
               "BrowserRedirectSuccess? " + (BrowserRedirectSuccess != null));
            logger.InfoPii("BrowserRedirectError " + BrowserRedirectError,
               "BrowserRedirectError? " + (BrowserRedirectError != null));
        }

#if NET_CORE || DESKTOP || NETSTANDARD

        /// <summary>
        /// Use Microsoft Edge to navigate to the given uri. On non-windows platforms it uses 
        /// whatever browser is the default
        /// </summary>
        /// <remarks>This helper is experimental and the signature may change without a major version increment.</remarks>
        public static async Task OpenWithEdgeBrowserAsync(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            string url = uri.AbsoluteUri;

#if DESKTOP
            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start microsoft-edge:{url}") { CreateNoWindow = true });
            await Task.FromResult(0).ConfigureAwait(false);
#else

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start microsoft-edge:{url}") { CreateNoWindow = true });
            }
            else
            {
                var proxy = PlatformProxyFactory.CreatePlatformProxy(new NullLogger());
                await proxy.StartDefaultOsBrowserAsync(url).ConfigureAwait(false);
            }
#endif
        }



        /// <summary>
        /// Use Microsoft Edge Chromium to navigate to the given uri. Requires the browser to be installed.
        /// On Linux, uses the default system browser instead, as Edge is not available.
        /// </summary>
        /// <remarks>This helper is experimental and the signature may change without a major version increment.</remarks>
        public static async Task OpenWithChromeEdgeBrowserAsync(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            string url = uri.AbsoluteUri;

#if DESKTOP
            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start msedge {url}") { CreateNoWindow = true });
            await Task.FromResult(0).ConfigureAwait(false);
#else

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start msedge {url}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var proxy = PlatformProxyFactory.CreatePlatformProxy(new NullLogger());
                await proxy.StartDefaultOsBrowserAsync(url).ConfigureAwait(false);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("msedge", url);
            }
            else
            {
                throw new PlatformNotSupportedException(RuntimeInformation.OSDescription);
            }
#endif

        }
#endif
    }
}
