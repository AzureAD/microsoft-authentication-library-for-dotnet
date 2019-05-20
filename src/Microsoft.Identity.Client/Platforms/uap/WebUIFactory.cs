// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.uap
{
    internal class WebUIFactory : IWebUIFactory
    {
        public IWebUI CreateAuthenticationDialog(CoreUIParent parent, RequestContext requestContext)
        {
            return new WebUI(parent, requestContext);
        }
    }
}
