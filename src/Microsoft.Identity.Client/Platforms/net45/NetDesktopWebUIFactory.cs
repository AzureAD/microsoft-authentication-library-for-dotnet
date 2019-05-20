// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.net45
{
    internal class NetDesktopWebUIFactory : IWebUIFactory
    {
        public IWebUI CreateAuthenticationDialog(CoreUIParent parent, RequestContext requestContext)
        {
            if (parent.UseHiddenBrowser)
            {
                return new SilentWebUI(parent, requestContext);
            }

            if (!parent.UseEmbeddedWebview)
            {
                return new DefaultOsBrowserWebUi(requestContext.ServiceBundle.PlatformProxy, requestContext.Logger);
            }

            return new InteractiveWebUI(parent, requestContext);
        }
    }
}
