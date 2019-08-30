// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.PlatformsCommon.Interfaces;

namespace Microsoft.Identity.Client.Platforms.netstandard13
{
    /// <summary>
    /// These control the behaviour of platforms targetting directly NetStandard (e.g. WinRT)
    /// </summary>
    internal class NetStandardFeatureFlags : IFeatureFlags
    {
        public bool IsFociEnabled => true;
    }
}
