// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.WsTrust;

namespace Microsoft.Identity.Client.Internal
{
    internal interface IServiceBundlePublic : IServiceBundle
    {
        IPlatformProxyPublic PlatformProxyPublic { get; }

        IWsTrustWebRequestManager WsTrustWebRequestManager { get; }

        ApplicationConfigurationPublic ConfigPublic { get; }
    }
}
