// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Platforms.Features.WinFormsLegacyWebUi;
using Microsoft.Identity.Client.Platforms.Shared.Desktop.OsBrowser;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.netdesktop
{
    internal class NetDesktopWebUIFactory : IWebUIFactory
    {
        public bool IsSystemWebViewAvailable => IsUserInteractive;

        public bool IsUserInteractive => DesktopOsHelper.IsUserInteractive();

        public bool IsEmbeddedWebViewAvailable => IsUserInteractive; // WebBrowser control is always available

        public IWebUI CreateAuthenticationDialog(
            CoreUIParent coreUIParent,
            WebViewPreference useEmbeddedWebView,
            RequestContext requestContext)
        {
            if (coreUIParent.UseHiddenBrowser)
            {
                return new SilentWebUI(coreUIParent, requestContext);
            }

            if (useEmbeddedWebView == WebViewPreference.System)
            {
                requestContext.Logger.Info("Using system browser.");
                return new DefaultOsBrowserWebUi(
                    requestContext.ServiceBundle.PlatformProxy,
                    requestContext.Logger,
                    coreUIParent.SystemWebViewOptions);
            }

            // Use the old legacy WebUi by default on .NET classic
            requestContext.Logger.Info("Using legacy embedded browser.");
            return new InteractiveWebUI(coreUIParent, requestContext);
        }
    }
}
