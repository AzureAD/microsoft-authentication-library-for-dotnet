// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.Shared.NetStdCore
{
    internal class NetCoreWebUIFactory : IWebUIFactory
    {
        public bool IsSystemWebViewAvailable => true;

        public IWebUI CreateAuthenticationDialog(
            CoreUIParent coreUIParent, 
            WebViewPreference webViewPreference, 
            RequestContext requestContext)
        {
            if (webViewPreference == WebViewPreference.Embedded)
            {
                throw new MsalClientException(MsalError.WebviewUnavailable, 
                   "An embedded webview is not available in the box on .NET Core 3.x " +
                   "Please reference the package Microsoft.Indentity.Client.Desktop and call WithDesktopFeatures(). See https://aka.ms/msal-net-webview2 " +
                   "Or use the system webview - see https://aka.ms/msal-net-os-browser");
            }

            return new DefaultOsBrowserWebUi(
                requestContext.ServiceBundle.PlatformProxy,
                requestContext.Logger,
                coreUIParent.SystemWebViewOptions);
        }
    }
}
