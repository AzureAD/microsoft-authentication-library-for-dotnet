// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Cache;

namespace Microsoft.Identity.Client.PlatformsCommon.Interfaces
{
    /// <summary>
    /// Provides a high level token cache serialization solution that is similar to the one offered to MSAL customers.
    /// Platforms should try to implement <see cref="ITokenCacheAccessor"/> if possible, as it provides more granular
    /// access.
    /// </summary>
    internal interface ITokenCacheBlobStorage
    {
        void OnAfterAccess(TokenCacheNotificationArgs args);
        void OnBeforeAccess(TokenCacheNotificationArgs args);
        void OnBeforeWrite(TokenCacheNotificationArgs args);
    }
}
