// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Platforms.netdesktop
{
    internal class NetDesktopFeatureFlags : IFeatureFlags
    {
        public bool IsFociEnabled => true;
    }
}
