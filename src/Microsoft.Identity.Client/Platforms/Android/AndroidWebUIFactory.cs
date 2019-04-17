// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Platforms.Android.EmbeddedWebview;
using Microsoft.Identity.Client.Platforms.Android.SystemWebview;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.Android
{
    [global::Android.Runtime.Preserve(AllMembers = true)]
    internal class AndroidWebUIFactory : IWebUIFactory
    {
        public IWebUI CreateAuthenticationDialog(CoreUIParent coreUIParent, RequestContext requestContext)
        {
            if (coreUIParent.UseEmbeddedWebview)
            {
                return new EmbeddedWebUI(coreUIParent)
                {
                    RequestContext = requestContext
                };
            }

            return new SystemWebUI(coreUIParent)
            {
                RequestContext = requestContext
            };
        }
    }
}
