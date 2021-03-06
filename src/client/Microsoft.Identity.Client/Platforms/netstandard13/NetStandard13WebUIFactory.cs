// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.netstandard13
{
    internal class NetStandard13WebUiFactory : IWebUIFactory
    {
        public bool IsSystemWebViewAvailable => false;

        public bool IsDesktopSession => DesktopOsHelper.IsDesktopSession();

        public bool IsEmbeddedWebviewAvailable => false;

        public IWebUI CreateAuthenticationDialog(CoreUIParent coreUIParent, WebViewPreference webViewPreference, RequestContext requestContext)
        {
            throw new PlatformNotSupportedException(
                "Possible Cause: If you are using an XForms app, or generally a netstandard assembly, " +
                "make sure you add a reference to Microsoft.Identity.Client.dll from each platform assembly " +
                "(e.g. UWP, Android, iOS), not just from the common netstandard assembly. " +
                "A browser is not avaiable in the box on .NETStandard 1.3");
        }
    }
}
