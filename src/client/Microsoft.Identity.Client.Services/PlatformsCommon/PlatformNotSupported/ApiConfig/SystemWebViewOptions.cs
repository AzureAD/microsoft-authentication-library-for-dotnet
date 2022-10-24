// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client
{
    public partial class SystemWebViewOptions
    {
#pragma warning disable 1998
        /// <summary>
        /// Use Microsoft Edge to navigate to the given URI. On non-windows platforms it uses 
        /// whatever browser is the default.
        /// </summary>
        public static async Task OpenWithEdgeBrowserAsync(Uri uri)
        {
            throw new PlatformNotSupportedException("Only on .NET  Classic / Core ");
        }

        /// <summary>
        /// Use Microsoft Edge Chromium to navigate to the given URI. Requires the browser to be installed.
        /// On Linux, uses the default system browser instead, as Edge is not available.
        /// </summary>
        public static async Task OpenWithChromeEdgeBrowserAsync(Uri uri)
        {
            throw new PlatformNotSupportedException("Only on .NET  Classic / Core ");
        }
#pragma warning restore 1998
    }
}
