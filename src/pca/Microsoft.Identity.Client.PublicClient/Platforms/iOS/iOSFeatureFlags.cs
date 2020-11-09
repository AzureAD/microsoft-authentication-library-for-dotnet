// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if iOS

using Microsoft.Identity.Client.Internal.Interfaces;

namespace Microsoft.Identity.Client.Platforms.iOS
{
    internal class iOSFeatureFlags : IFeatureFlags
    {
        /// <summary>
        /// FOCI has not been tested on iOS
        /// </summary>
        public bool IsFociEnabled => false;
    }
}
#endif
