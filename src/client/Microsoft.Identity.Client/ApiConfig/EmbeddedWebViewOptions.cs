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

        internal void LogParameters(ILoggerAdapter logger)
        {
            logger.Info("WebView2Options configured");

            logger.Info(() => $"Title: {Title}");
        }

        internal static void ValidatePlatformAvailability()
        {
#if !SUPPORTS_WIN32
            throw new PlatformNotSupportedException(
                "EmbeddedWebViewOptions API is only supported on .NET Fx, .NET Core and .NET8 ");
#endif
        }
    }
}
