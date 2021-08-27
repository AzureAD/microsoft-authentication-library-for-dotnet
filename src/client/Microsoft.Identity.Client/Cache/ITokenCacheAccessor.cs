// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;

namespace Microsoft.Identity.Client.Cache
{
    internal interface ITokenCacheAccessor
    {
        void SaveAccessToken(MsalAccessTokenCacheItem item);

        void SaveRefreshToken(MsalRefreshTokenCacheItem item);

        void SaveIdToken(MsalIdTokenCacheItem item);

        void SaveAccount(MsalAccountCacheItem item);

        void SaveAppMetadata(MsalAppMetadataCacheItem item);

        MsalAccessTokenCacheItem GetAccessToken(MsalAccessTokenCacheKey accessTokenKey);

        MsalRefreshTokenCacheItem GetRefreshToken(MsalRefreshTokenCacheKey refreshTokenKey);

        MsalIdTokenCacheItem GetIdToken(MsalIdTokenCacheKey idTokenKey);

        MsalAccountCacheItem GetAccount(MsalAccountCacheKey accountKey);

        MsalAppMetadataCacheItem GetAppMetadata(MsalAppMetadataCacheKey appMetadataKey);

        void DeleteAccessToken(MsalAccessTokenCacheKey cacheKey);

        void DeleteRefreshToken(MsalRefreshTokenCacheKey cacheKey);

        void DeleteIdToken(MsalIdTokenCacheKey cacheKey);

        void DeleteAccount(MsalAccountCacheKey cacheKey);

        /// <summary>
        /// Returns all access tokens from the underlying cache collection.
        /// If optionalTenantIdFilter parameter is specified, returns access tokens pertaining to the specified tenant.
        /// Token cache accessors implementing this interface are not required to obey Parameter optionalTenantIdFilter.
        /// See <see cref="PlatformsCommon.Shared.InMemoryPartitionedTokenCacheAccessor.GetAllAccessTokens"/> which uses this filter.
        /// See <see cref="PlatformsCommon.Shared.InMemoryTokenCacheAccessor.GetAllAccessTokens"/> which does not use this filter.
        /// </summary>
        IReadOnlyList<MsalAccessTokenCacheItem> GetAllAccessTokens(string optionalTenantIdFilter = null);

        IReadOnlyList<MsalRefreshTokenCacheItem> GetAllRefreshTokens();

        IReadOnlyList<MsalIdTokenCacheItem> GetAllIdTokens();

        IReadOnlyList<MsalAccountCacheItem> GetAllAccounts();

        IReadOnlyList<MsalAppMetadataCacheItem> GetAllAppMetadata();

#if iOS
        void SetiOSKeychainSecurityGroup(string keychainSecurityGroup);
#endif

        void Clear();
    }
}
