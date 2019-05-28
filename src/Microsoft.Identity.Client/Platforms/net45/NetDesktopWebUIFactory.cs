// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;
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

            return new InteractiveWebUI(parent, requestContext);
        }
    }
}
