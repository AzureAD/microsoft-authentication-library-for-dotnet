// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.netstandard13
{
    internal class NetStandard13WebUiFactory : IWebUIFactory
    {
        public bool IsSystemWebViewAvailable => false;

        public bool IsUserInteractive => DesktopOsHelper.IsUserInteractive();

        public bool IsEmbeddedWebViewAvailable => false;

        public IWebUI CreateAuthenticationDialog(CoreUIParent coreUIParent, WebViewPreference webViewPreference, RequestContext requestContext)
        {
            throw new PlatformNotSupportedException(
                "Possible cause: If you are using an XForms app, or generally a .NET Standard assembly, " +
                "make sure you add a reference to Microsoft.Identity.Client.dll from each platform assembly " +
                "(e.g. UWP, Android, iOS), not just from the common .NET Standard assembly. " +
                "A browser is not available in the box on .NET Standard 2.0." +
                "If you are on UWP, you may need to update to version 1809 (Build 17763) in order to use a browser.");
        }
    }
}
