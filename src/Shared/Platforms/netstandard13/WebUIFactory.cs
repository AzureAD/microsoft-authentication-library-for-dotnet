// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.Platforms.netstandard13
{
    internal class WebUIFactory : IWebUIFactory
    {
        public IWebUI CreateAuthenticationDialog(CoreUIParent parent, RequestContext requestContext)
        {
            throw new PlatformNotSupportedException("Possible Cause: If you are using an XForms app, or generally a netstandard assembly, " +
                "make sure you add a reference to Microsoft.Identity.Client.dll from each platform assembly " +
                "(e.g. UWP, Android, iOS), not just from the common netstandard assembly");
        }
    }
}
