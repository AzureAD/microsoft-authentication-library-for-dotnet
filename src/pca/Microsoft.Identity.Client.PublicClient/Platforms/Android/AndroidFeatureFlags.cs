// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if ANDROID

using Microsoft.Identity.Client.Internal.Interfaces;

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
#endif
