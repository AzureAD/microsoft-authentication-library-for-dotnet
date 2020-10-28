// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if NET_CORE
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.Shared.NetStdCore
{
    internal class NetCoreWebUIFactory : IWebUIFactory
    {
        public IWebUI CreateAuthenticationDialog(CoreUIParent parent, RequestContext requestContext)
        {
            return new DefaultOsBrowserWebUi(
                requestContext.ServiceBundle.PlatformProxy,
                requestContext.Logger,
                parent.SystemWebViewOptions);
        }
    }
}
#endif
