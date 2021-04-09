// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.uap
{
    internal class UapWebUIFactory : IWebUIFactory
    {
        public bool IsSystemWebViewAvailable => false;

        public bool IsUserInteractive => true;

        public bool IsEmbeddedWebViewAvailable => true;

        public IWebUI CreateAuthenticationDialog(CoreUIParent coreUIParent, WebViewPreference webViewPreference, RequestContext requestContext)
        {
            if (webViewPreference == WebViewPreference.System)
            {
                throw new MsalClientException(
                    MsalError.WebViewUnavailable,
                    "On UWP, MSAL cannot use the system browser. " +
                    "The preferred auth mechanism is the Web Authentication Manager (WAM). See https://aka.ms/msal-net-uwp-wam");
            }

            return new WebUI(coreUIParent, requestContext);
        }
    }
}
