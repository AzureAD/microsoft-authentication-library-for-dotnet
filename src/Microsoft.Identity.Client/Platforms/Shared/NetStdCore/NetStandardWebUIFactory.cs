// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.Shared.NetStdCore
{
    internal class NetStandardWebUIFactory : IWebUIFactory
    {
        public IWebUI CreateAuthenticationDialog(CoreUIParent parent, RequestContext requestContext)
        {
            return new DefaultOsBrowserWebUi(requestContext.ServiceBundle.PlatformProxy, requestContext.Logger);
        }
    }
}
