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
        internal static object _lock = new object();

        public static void AddAccessTokenCacheItem(this ITokenCacheInternal tokenCache, MsalAccessTokenCacheItem accessTokenItem)
        {
            lock (_lock)
            {
                tokenCache.Accessor.SaveAccessToken(accessTokenItem);
            }
        }


        public static void ClearAccessTokens(this ITokenCacheAccessor accessor)
        {
            lock (_lock)
            {
                foreach (var item in accessor.GetAllAccessTokens())
                {
                    accessor.DeleteAccessToken(item.GetKey());
                }
            }
        }

        public static void ClearRefreshTokens(this ITokenCacheAccessor accessor)
        {
            lock (_lock)
            {
                foreach (var item in accessor.GetAllRefreshTokens())
                {
                    accessor.DeleteRefreshToken(item.GetKey());
                }
            }
        }
    }
}
