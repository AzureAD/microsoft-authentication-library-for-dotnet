// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if MSAL_DESKTOP || MSAL_XAMARIN
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.UI
{
    internal interface IWebUIFactory
    {
        IWebUI CreateAuthenticationDialog(
            CoreUIParent coreUIParent,
            RequestContext requestContext);
    }
}
#endif
