// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.;

namespace Microsoft.Identity.Client.Region
{
    // Enum to be used only for telemetry, to log reason for cache refresh and fetching AT from ests.
    internal enum CacheRefresh
    {
        None = -1, // When no cache is used
        NoCachedAT = 0, // When there is no AT is found in the cache
        Expired = 1, // When the token refresh happens due to expired token in cache
        RefreshIn = 2, // When the token refresh happens due to refresh in
        ForceRefresh = 3 // When the token refresh happens due to force refresh
    }
}
