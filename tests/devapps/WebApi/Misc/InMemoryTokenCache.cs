using Microsoft.Identity.Client;

namespace WebApi.Controllers
{
    public class InMemoryTokenCache
    {
        private byte[] _cacheData;
        /// <summary>
        /// Path to the token cache
        /// </summary>

        public void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            args.TokenCache.DeserializeMsalV3(_cacheData, true);
        }

        public void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                _cacheData = args.TokenCache.SerializeMsalV3();
            }
        }

        public void Bind(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);
        }
    }
}
