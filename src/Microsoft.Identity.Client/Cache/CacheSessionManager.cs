// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Cache
{
    internal class CacheSessionManager : ICacheSessionManager
    {
        private readonly AuthenticationRequestParameters _requestParams;

        public CacheSessionManager(ITokenCacheInternal tokenCacheInternal, AuthenticationRequestParameters requestParams)
        {
            TokenCacheInternal = tokenCacheInternal;
            _requestParams = requestParams;
        }

        public ITokenCacheInternal TokenCacheInternal { get; }
        public bool HasCache => TokenCacheInternal != null;

        public Task<MsalAccessTokenCacheItem> FindAccessTokenAsync()
        {
            return TokenCacheInternal.FindAccessTokenAsync(_requestParams);
        }

        public Task<Tuple<MsalAccessTokenCacheItem, MsalIdTokenCacheItem>> SaveTokenResponseAsync(MsalTokenResponse tokenResponse)
        {
            return TokenCacheInternal.SaveTokenResponseAsync(_requestParams, tokenResponse);
        }

        public Task<MsalIdTokenCacheItem> GetIdTokenCacheItemAsync(MsalIdTokenCacheKey idTokenCacheKey)
        {
            return TokenCacheInternal.GetIdTokenCacheItemAsync(idTokenCacheKey, _requestParams.RequestContext);
        }

        public Task<MsalRefreshTokenCacheItem> FindFamilyRefreshTokenAsync(string familyId)
        {
            if (String.IsNullOrEmpty(familyId))
            {
                throw new ArgumentNullException(nameof(familyId));
            }

            return TokenCacheInternal.FindRefreshTokenAsync(_requestParams, familyId);
        }

        public Task<MsalRefreshTokenCacheItem> FindRefreshTokenAsync()
        {
            return TokenCacheInternal.FindRefreshTokenAsync(_requestParams);
        }

        public Task<bool?> IsAppFociMemberAsync(string familyId)
        {
            return TokenCacheInternal.IsFociMemberAsync(_requestParams, familyId);
        }
    }
}
