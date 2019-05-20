// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;
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
