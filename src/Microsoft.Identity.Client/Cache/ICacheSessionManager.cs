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
        bool HasCache { get; }
        Task<MsalAccessTokenCacheItem> FindAccessTokenAsync();
        Tuple<MsalAccessTokenCacheItem, MsalIdTokenCacheItem> SaveAccessAndRefreshToken(MsalTokenResponse response);
        MsalIdTokenCacheItem GetIdTokenCacheItem(MsalIdTokenCacheKey msalIdTokenCacheKey);
        Task<MsalRefreshTokenCacheItem> FindRefreshTokenAsync();
    }
}
