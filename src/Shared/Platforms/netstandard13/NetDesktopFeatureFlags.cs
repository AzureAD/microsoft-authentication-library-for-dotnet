// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NETSTANDARD
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Platforms.Android
{
    internal class NetDesktopFeatureFlags : IFeatureFlags
    {
        public bool IsFociEnabled => true;
    }
}
#endif
