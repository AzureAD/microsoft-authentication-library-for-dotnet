using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Cache
{
    internal class CacheSessionManager : ICacheSessionManager
    {
        private readonly AuthenticationRequestParameters _requestParams;

        public CacheSessionManager(ITokenCacheInternal tokenCacheInternal,
            AuthenticationRequestParameters requestParams)
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

        public Tuple<MsalAccessTokenCacheItem, MsalIdTokenCacheItem> SaveAccessAndRefreshToken(MsalTokenResponse tokenResponse)
        {
            return TokenCacheInternal.SaveAccessAndRefreshToken(_requestParams, tokenResponse);
        }

        public MsalIdTokenCacheItem GetIdTokenCacheItem(MsalIdTokenCacheKey idTokenCacheKey)
        {
            return TokenCacheInternal.GetIdTokenCacheItem(idTokenCacheKey, _requestParams.RequestContext);
        }

        public Task<MsalRefreshTokenCacheItem> FindRefreshTokenAsync()
        {
            return TokenCacheInternal.FindRefreshTokenAsync(_requestParams);
        }
    }
}
