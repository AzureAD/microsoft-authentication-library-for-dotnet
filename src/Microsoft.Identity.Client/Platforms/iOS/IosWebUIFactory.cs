// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Platforms.iOS.EmbeddedWebview;
using Microsoft.Identity.Client.Platforms.iOS.SystemWebview;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.iOS
{
    internal class IosWebUIFactory : IWebUIFactory
    {
        public IWebUI CreateAuthenticationDialog(CoreUIParent coreUIParent, RequestContext requestContext, IPlatformProxy platformProxy)
        {
            if (coreUIParent.UseEmbeddedWebview)
            {
                return new EmbeddedWebUI()
                {
                    RequestContext = requestContext,
                    CoreUIParent = coreUIParent
                };
            }

            //there is no need to pass UIParent.
            return new SystemWebUI()
            {
                RequestContext = requestContext
            };
        }
    }
}
