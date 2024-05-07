// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.Shared.NetStdCore
{
    internal class NetCoreWebUIFactory : IWebUIFactory
    {
        public bool IsSystemWebViewAvailable => IsUserInteractive;

        public bool IsUserInteractive => DesktopOsHelper.IsUserInteractive();

        public bool IsEmbeddedWebViewAvailable => false;

        public IWebUI CreateAuthenticationDialog(
            CoreUIParent coreUIParent,
            WebViewPreference webViewPreference,
            RequestContext requestContext)
        {
            if (webViewPreference == WebViewPreference.Embedded)
            {
                throw new MsalClientException(MsalError.WebviewUnavailable,
                    "To enable the embedded webview on Windows, reference Microsoft.Identity.Client.Desktop and call the extension method .WithWindowsEmbeddedBrowserSupport().");
            }

            requestContext.Logger.Info("Using system browser.");
            return new DefaultOsBrowserWebUi(
                requestContext.ServiceBundle.PlatformProxy,
                requestContext.Logger,
                coreUIParent.SystemWebViewOptions);
        }
    }
}
