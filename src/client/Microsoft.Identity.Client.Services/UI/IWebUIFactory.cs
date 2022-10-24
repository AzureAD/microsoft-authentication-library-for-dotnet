// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.UI
{
    internal interface IWebUIFactory
    {
        IWebUI CreateAuthenticationDialog(
            CoreUIParent coreUIParent,
            WebViewPreference webViewPreference,
            RequestContext requestContext);

        bool IsSystemWebViewAvailable { get; }
        bool IsUserInteractive { get; }
        bool IsEmbeddedWebViewAvailable { get; }
    }
}
