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

        public IWebUI CreateAuthenticationDialog(CoreUIParent coreUIParent, WebViewPreference webViewPreference, RequestContext requestContext)
        {
            return new DefaultOsBrowserWebUi(
                requestContext.ServiceBundle.PlatformProxy,
                requestContext.Logger,
                coreUIParent.SystemWebViewOptions);
        }
    }
}
