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
        private readonly ITokenCacheInternal _tokenCacheInternal;
        private readonly AuthenticationRequestParameters _requestParams;

        public CacheSessionManager(ITokenCacheInternal tokenCacheInternal,
            AuthenticationRequestParameters requestParams)
        {
            _tokenCacheInternal = tokenCacheInternal;
            _requestParams = requestParams;
        }

        public bool HasCache => _tokenCacheInternal != null;

        public Task<MsalAccessTokenCacheItem> FindAccessTokenAsync()
        {
            throw new NotImplementedException();
        }

        public Tuple<MsalAccessTokenCacheItem, MsalIdTokenCacheItem> SaveAccessAndRefreshToken(MsalTokenResponse response)
        {
            throw new NotImplementedException();
        }

        public MsalIdTokenCacheItem GetIdTokenCacheItem(MsalIdTokenCacheKey msalIdTokenCacheKey)
        {
            throw new NotImplementedException();
        }

        public Task<MsalRefreshTokenCacheItem> FindRefreshTokenAsync()
        {
            throw new NotImplementedException();
        }
    }
}
