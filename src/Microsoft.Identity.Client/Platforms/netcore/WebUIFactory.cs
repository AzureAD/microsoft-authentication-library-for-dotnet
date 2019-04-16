// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.netcore
{
    internal class WebUIFactory : IWebUIFactory
    {
        public IWebUI CreateAuthenticationDialog(CoreUIParent parent, RequestContext requestContext)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
