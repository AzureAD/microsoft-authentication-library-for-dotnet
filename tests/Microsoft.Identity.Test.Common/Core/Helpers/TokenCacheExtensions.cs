// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    internal static class TokenCacheExtensions
    {
        public static TokenCacheAccessRecorder RecordAccess(this ITokenCache tokenCache)
        {
            return new TokenCacheAccessRecorder(tokenCache as TokenCache);
        }

        public static void ClearAccessTokens(this ITokenCacheAccessor accessor)
        {
            foreach (var item in accessor.GetAllAccessTokens())
            {
                accessor.DeleteAccessToken(item);
            }
        }

        public static void ClearRefreshTokens(this ITokenCacheAccessor accessor)
        {
            foreach (var item in accessor.GetAllRefreshTokens())
            {
                accessor.DeleteRefreshToken(item);
            }
        }
    }
}
