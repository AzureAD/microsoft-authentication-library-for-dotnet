using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    internal static class TokenCacheExtensions
    {
        public static void AddAccessTokenCacheItem(this ITokenCacheInternal tokenCache, MsalAccessTokenCacheItem accessTokenItem)
        {
            lock (tokenCache.LockObject)
            {
                tokenCache.Accessor.SaveAccessToken(accessTokenItem);
            }
        }

        public static void AddRefreshTokenCacheItem(this ITokenCacheInternal tokenCache, MsalRefreshTokenCacheItem refreshTokenItem)
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (tokenCache.LockObject)
            {
                tokenCache.Accessor.SaveRefreshToken(refreshTokenItem);
            }
        }

        public static void DeleteAccessToken(this ITokenCacheInternal tokenCache, MsalAccessTokenCacheItem msalAccessTokenCacheItem,
            MsalIdTokenCacheItem msalIdTokenCacheItem,
            RequestContext requestContext)
        {
            lock (tokenCache.LockObject)
            {
                tokenCache.Accessor.DeleteAccessToken(msalAccessTokenCacheItem.GetKey());
            }
        }

        public static void SaveRefreshTokenCacheItem(
            this ITokenCacheInternal tokenCache, 
            MsalRefreshTokenCacheItem msalRefreshTokenCacheItem,
            MsalIdTokenCacheItem msalIdTokenCacheItem)
        {
            lock (tokenCache.LockObject)
            {
                tokenCache.Accessor.SaveRefreshToken(msalRefreshTokenCacheItem);
            }
        }

        public static void SaveAccessTokenCacheItem(
            this ITokenCacheInternal tokenCache, 
            MsalAccessTokenCacheItem msalAccessTokenCacheItem,
            MsalIdTokenCacheItem msalIdTokenCacheItem)
        {
            lock (tokenCache.LockObject)
            {
                tokenCache.Accessor.SaveAccessToken(msalAccessTokenCacheItem);
            }
        }

        public static MsalAccountCacheItem GetAccount(
            this ITokenCacheInternal tokenCache, 
            MsalRefreshTokenCacheItem refreshTokenCacheItem, 
            RequestContext requestContext)
        {
            var accounts = tokenCache.GetAllAccounts();

            foreach (MsalAccountCacheItem account in accounts)
            {
                if (refreshTokenCacheItem.HomeAccountId.Equals(account.HomeAccountId, StringComparison.OrdinalIgnoreCase) &&
                    refreshTokenCacheItem.Environment.Equals(account.Environment, StringComparison.OrdinalIgnoreCase))
                {
                    return account;
                }
            }
            return null;
        }

    }
}
