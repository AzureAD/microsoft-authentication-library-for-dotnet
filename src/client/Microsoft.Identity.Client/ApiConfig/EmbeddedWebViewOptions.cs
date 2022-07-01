// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.PlatformsCommon.Factories;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Options for using the embedded webview.    
    /// </summary>
#if !SUPPORTS_WIN32
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
        /// <remarks>Currently only affects the windows desktop apps (WebView1 / Vulcan and WebView2 browser).</remarks>
        public string Title { get; set; }

        /// <summary>
        /// It is possible for applications to bundle a fixed version of the runtime, and ship it side-by-side.
        /// For this you need to tell MSAL (so it can tell WebView2) where to find the runtime bits by setting this property. If you don't set it, MSAL will attempt to use a system-wide "evergreen" installation of the runtime."
        /// For more details see: https://docs.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2environment.createasync?view=webview2-dotnet-1.0.705.50
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("In case when WebView2 is not available, MSAL.NET will fallback to legacy WebView.", true)]
        public string WebView2BrowserExecutableFolder { get; set; }

        internal void LogParameters(ILoggerAdapter logger)
        {
            logger.Info("WebView2Options configured");

            logger.Info($"Title: {Title}");
        }

        internal static void ValidatePlatformAvailability()
        {
#if !SUPPORTS_WIN32
            throw new PlatformNotSupportedException(
                "EmbeddedWebViewOptions API is only supported on .NET Fx, .NET Core and .NET5 ");
#endif
        }
    }
}
