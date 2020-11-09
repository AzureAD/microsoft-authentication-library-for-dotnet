// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if UWP

using Microsoft.Identity.Client.Internal.Interfaces;

namespace Microsoft.Identity.Client.Platforms.uap
{
    internal class UapFeatureFlags : IFeatureFlags
    {
        public bool IsFociEnabled => true;
    }
}
#endif
