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

        IEnumerable<MsalAccessTokenCacheItem> GetAllAccessTokens();

        IEnumerable<MsalRefreshTokenCacheItem> GetAllRefreshTokens();

        IEnumerable<MsalIdTokenCacheItem> GetAllIdTokens();

        IEnumerable<MsalAccountCacheItem> GetAllAccounts();

        IEnumerable<MsalAppMetadataCacheItem> GetAllAppMetadata();


#if iOS
        void SetiOSKeychainSecurityGroup(string keychainSecurityGroup);
#endif

        void Clear();
    }
}
