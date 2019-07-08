// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    internal static class TokenCacheExtensions
    {
        public static void AddAccessTokenCacheItem(this ITokenCacheInternal tokenCache, MsalAccessTokenCacheItem accessTokenItem)
        {
            tokenCache.Semaphore.Wait();
            try
            {
                tokenCache.Accessor.SaveAccessToken(accessTokenItem);
            }
            finally
            {
                tokenCache.Semaphore.Release();
            }
        }

        public static void AddRefreshTokenCacheItem(
            this ITokenCacheInternal tokenCache,
            MsalRefreshTokenCacheItem refreshTokenItem)
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            tokenCache.Semaphore.Wait();
            try
            {
                tokenCache.Accessor.SaveRefreshToken(refreshTokenItem);
            }
            finally
            {
                tokenCache.Semaphore.Release();
            }
        }

        public static void DeleteAccessToken(
            this ITokenCacheInternal tokenCache,
            MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            tokenCache.Semaphore.Wait();
            try
            {
                tokenCache.Accessor.DeleteAccessToken(msalAccessTokenCacheItem.GetKey());
            }
            finally
            {
                tokenCache.Semaphore.Release();
            }
        }

        public static async Task<MsalAccountCacheItem> GetAccountAsync(
            this ITokenCacheInternal tokenCache,
            MsalRefreshTokenCacheItem refreshTokenCacheItem)
        {
            IEnumerable<MsalAccountCacheItem> accounts = await tokenCache.GetAllAccountsAsync().ConfigureAwait(false);

            foreach (var account in accounts)
            {
                if (refreshTokenCacheItem.HomeAccountId.Equals(account.HomeAccountId, StringComparison.OrdinalIgnoreCase) &&
                    refreshTokenCacheItem.Environment.Equals(account.Environment, StringComparison.OrdinalIgnoreCase))
                {
                    return account;
                }
            }

            return null;
        }

        public static void ClearAccessTokens(this ITokenCacheAccessor accessor)
        {
            foreach (var item in accessor.GetAllAccessTokens())
            {
                accessor.DeleteAccessToken(item.GetKey());
            }
        }

        public static void ClearRefreshTokens(this ITokenCacheAccessor accessor)
        {
            foreach (var item in accessor.GetAllRefreshTokens())
            {
                accessor.DeleteRefreshToken(item.GetKey());
            }
        }
    }
}
