// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
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

        public static void AddRefreshTokenCacheItem(
            this ITokenCacheInternal tokenCache,
            MsalRefreshTokenCacheItem refreshTokenItem)
        {
            // this method is called by serialize and does not require
            // delegates because serialize itself is called from delegates
            lock (tokenCache.LockObject)
            {
                tokenCache.Accessor.SaveRefreshToken(refreshTokenItem);
            }
        }

        public static void DeleteAccessToken(
            this ITokenCacheInternal tokenCache,
            MsalAccessTokenCacheItem msalAccessTokenCacheItem,
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
            IEnumerable<MsalAccountCacheItem> accounts = tokenCache.GetAllAccounts();

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
