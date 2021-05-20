// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
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

            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start microsoft-edge:{url}") { CreateNoWindow = true });
            await Task.FromResult(0).ConfigureAwait(false);
        }

        /// <summary>
        /// Use Microsoft Edge Chromium to navigate to the given URI. Requires the browser to be installed.
        /// On Linux, uses the default system browser instead, as Edge is not available.
        /// </summary>
        public static async Task OpenWithChromeEdgeBrowserAsync(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            string url = uri.AbsoluteUri;

            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start msedge {url}") { CreateNoWindow = true });
            await Task.FromResult(0).ConfigureAwait(false);
        }
    }
}
