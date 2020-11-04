// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if ANDROID

using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Android.EmbeddedWebview;
using Microsoft.Identity.Client.Platforms.Android.SystemWebview;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.Android
{
    [global::Android.Runtime.Preserve(AllMembers = true)]
    internal class AndroidWebUIFactory : IWebUIFactory
    {
        public IWebUI CreateAuthenticationDialog(CoreUIParent parent, RequestContext requestContext)
        {
            if (parent.UseEmbeddedWebview)
            {
                return new EmbeddedWebUI(parent)
                {
                    RequestContext = requestContext
                };
            }

            return new SystemWebUI(parent)
            {
                RequestContext = requestContext
            };
        }
    }
}
#endif
