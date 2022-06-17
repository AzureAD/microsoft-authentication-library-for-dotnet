// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Cache
{
    internal interface ITokenCacheAccessor
    {
        void SaveAccessToken(MsalAccessTokenCacheItem item);

        void SaveRefreshToken(MsalRefreshTokenCacheItem item);

        void SaveIdToken(MsalIdTokenCacheItem item);

        void SaveAccount(MsalAccountCacheItem item);

        void SaveAppMetadata(MsalAppMetadataCacheItem item);

        MsalIdTokenCacheItem GetIdToken(MsalAccessTokenCacheItem accessTokenCacheItem);

        MsalAccountCacheItem GetAccount(MsalAccountCacheKey accountKey);

        MsalAppMetadataCacheItem GetAppMetadata(MsalAppMetadataCacheKey appMetadataKey);

        void DeleteAccessToken(MsalAccessTokenCacheItem item);

        void DeleteRefreshToken(MsalRefreshTokenCacheItem item);

        void DeleteIdToken(MsalIdTokenCacheItem item);

        void DeleteAccount(MsalAccountCacheItem item);

        /// <summary>
        /// Returns all access tokens from the underlying cache collection.
        /// If <paramref name="optionalPartitionKey"/> is specified, returns access tokens from that partition only.
        /// </summary>
        /// <remarks>
        /// WARNING: if partitionKey is null, this API is slow as it loads all tokens, not just from 1 partition. 
        /// It should only support external token caching, in the hope that the external token cache is partitioned.
        /// Not all classes that implement this method are required to filter by partition (e.g. mobile)
        /// </remarks>
        List<MsalAccessTokenCacheItem> GetAllAccessTokens(string optionalPartitionKey = null, ILoggerAdapter requestlogger = null);

        /// <summary>
        /// Returns all refresh tokens from the underlying cache collection.
        /// If <paramref name="optionalPartitionKey"/> is specified, returns refresh tokens from that partition only.
        /// </summary>
        /// <remarks>
        /// WARNING: if partitionKey is null, this API is slow as it loads all tokens, not just from 1 partition. 
        /// It should only support external token caching, in the hope that the external token cache is partitioned.
        /// Not all classes that implement this method are required to filter by partition (e.g. mobile)
        /// </remarks>
        List<MsalRefreshTokenCacheItem> GetAllRefreshTokens(string optionalPartitionKey = null, ILoggerAdapter requestlogger = null);

        /// <summary>
        /// Returns all ID tokens from the underlying cache collection.
        /// If <paramref name="optionalPartitionKey"/> is specified, returns ID tokens from that partition only.
        /// </summary>
        /// <remarks>
        /// WARNING: if partitionKey is null, this API is slow as it loads all tokens, not just from 1 partition. 
        /// It should only support external token caching, in the hope that the external token cache is partitioned.
        /// Not all classes that implement this method are required to filter by partition (e.g. mobile)
        /// </remarks>
        List<MsalIdTokenCacheItem> GetAllIdTokens(string optionalPartitionKey = null, ILoggerAdapter requestlogger = null);

        /// <summary>
        /// Returns all accounts from the underlying cache collection.
        /// If <paramref name="optionalPartitionKey"/> is specified, returns accounts from that partition only.
        /// </summary>
        /// <remarks>
        /// WARNING: if partitionKey is null, this API is slow as it loads all tokens, not just from 1 partition. 
        /// It should only support external token caching, in the hope that the external token cache is partitioned.
        /// Not all classes that implement this method are required to filter by partition (e.g. mobile)
        /// </remarks>
        List<MsalAccountCacheItem> GetAllAccounts(string optionalPartitionKey = null, ILoggerAdapter requestlogger = null);

        List<MsalAppMetadataCacheItem> GetAllAppMetadata();

#if iOS
        void SetiOSKeychainSecurityGroup(string keychainSecurityGroup);
#endif

        void Clear(ILoggerAdapter requestlogger = null);

        /// <remarks>
        /// WARNING: this API is slow as it loads all tokens, not just from 1 partition. 
        /// It should only support external token caching, in the hope that the external token cache is partitioned.
        /// </remarks>
        bool HasAccessOrRefreshTokens();
    }
}
