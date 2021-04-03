// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.PlatformsCommon.Factories;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Options for using the modern Windows embedded browser WebView2. 
    /// For more details see https://aka.ms/msal-net-webview2
    /// </summary>
#if !SUPPORTS_WEBVIEW2
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
    public class EmbeddedWebViewOptions
    {
        /// <summary>
        /// </summary>
        public EmbeddedWebViewOptions()
        {
            ValidatePlatformAvailability();
        }

        internal static EmbeddedWebViewOptions GetDefaultOptions()
        {
            return new EmbeddedWebViewOptions();
        }

        /// <summary>
        /// Forces a static title to be set on the window hosting the browser. If not configured, the widow's title is set to the web page title.
        /// </summary>
        /// <remarks>Currently only affects WebView2 browser on Windows.</remarks>
        public string Title { get; set; }

        /// <summary>
        /// It is possible for applications to bundle a fixed version of the runtime, and ship it side-by-side.
        /// For this you need to tell MSAL (so it can tell WebView2) where to find the runtime bits by setting this property. If you don't set it, MSAL will attempt to use a system-wide "evergreen" installation of the runtime."
        /// For more details see: https://docs.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2environment.createasync?view=webview2-dotnet-1.0.705.50
        /// </summary>
        [Obsolete("In case when WebView2 is not available, MSAL.NET will fallback to legacy WebView.", true)]
        public string WebView2BrowserExecutableFolder { get; set; }

        internal void LogParameters(ICoreLogger logger)
        {
            logger.Info("WebView2Options configured");

            logger.Info($"Title: {Title}");
        }

        internal static void ValidatePlatformAvailability()
        {
#if !SUPPORTS_WEBVIEW2
            throw new PlatformNotSupportedException(
                "WebView2Options API is only supported on .NET Fx, .NET Core and .NET5 ");
#endif
        }
    }
}
