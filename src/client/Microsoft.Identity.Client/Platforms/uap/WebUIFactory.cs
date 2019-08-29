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
            if (!parent.UseEmbeddedWebview)
            {
                throw new MsalClientException(
                    MsalError.WebviewUnavailable,
                    "On UWP, MSAL does not offer a system webUI out of the box. Please set .WithUseEmbeddedWebview to false. " +
                    "To use the UWP Web Authentication Manager (WAM) see https://aka.ms/msal-net-uwp-wam");
            }

            return new WebUI(parent, requestContext);
        }
    }
}
