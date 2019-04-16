// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Platforms.Android
{
    internal class AndroidFeatureFlags : IFeatureFlags
    {
        /// <summary>
        /// FOCI is not currently supported on Android because app metadata serialization is not defined.
        /// </summary>
        public bool IsFociEnabled => false;
    }
}
