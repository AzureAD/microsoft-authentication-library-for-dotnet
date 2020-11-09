// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Internal.Interfaces;

namespace Microsoft.Identity.Client.Platforms.netstandardcore
{
    internal class NetFeatureFlags : IFeatureFlags
    {
        public bool IsFociEnabled => true;
    }
}
