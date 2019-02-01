using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Cache
{
    internal interface ICacheSessionManager
    {
        ITokenCacheInternal TokenCacheInternal { get; }
        bool HasCache { get; }
        Task<MsalAccessTokenCacheItem> FindAccessTokenAsync();
        Tuple<MsalAccessTokenCacheItem, MsalIdTokenCacheItem> SaveAccessAndRefreshToken(MsalTokenResponse tokenResponse);
        MsalIdTokenCacheItem GetIdTokenCacheItem(MsalIdTokenCacheKey idTokenCacheKey);
        Task<MsalRefreshTokenCacheItem> FindRefreshTokenAsync();
    }
}
