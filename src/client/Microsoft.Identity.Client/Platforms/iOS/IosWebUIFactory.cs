// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.iOS.EmbeddedWebview;
using Microsoft.Identity.Client.Platforms.iOS.SystemWebview;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.iOS
{
    internal class IosWebUIFactory : IWebUIFactory
    {
        public bool IsSystemWebViewAvailable => true;
        public bool IsUserInteractive => true;
        public bool IsEmbeddedWebViewAvailable => true;

        public IWebUI CreateAuthenticationDialog(
            CoreUIParent coreUIParent, 
            WebViewPreference useEmbeddedWebView, 
            RequestContext requestContext)
        {
            if (useEmbeddedWebView == WebViewPreference.Embedded)
            {
                return new EmbeddedWebUI()
                {
                    RequestContext = requestContext,
                    CoreUIParent = coreUIParent
                };
            }
            
            return new SystemWebUI()
            {
                RequestContext = requestContext
            };
        }

    }
}
