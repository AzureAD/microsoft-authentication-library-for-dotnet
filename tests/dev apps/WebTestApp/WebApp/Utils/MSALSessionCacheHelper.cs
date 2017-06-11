using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;

namespace WebApp.Utils
{
    public class MsalSessionCacheHelper
    {
        public static TokenCache GetMsalSessionCacheInstance(ISession session, string cacheId)
        {
            var cache = new TokenCache();

            cache.SetBeforeAccess(delegate
            {
                LoadCache(session, cacheId, cache);
            });

            cache.SetAfterAccess(delegate
            {
                // if the access operation resulted in a cache update
                if (cache.HasStateChanged)
                {
                    PersistCache(session, cacheId, cache);
                    cache.HasStateChanged = false;
                }
            });

            LoadCache(session, cacheId, cache);

            return cache;
        }

        private static void PersistCache(ISession session, string cacheId,
            TokenCache cache)
        {
            session.Set(cacheId, cache.Serialize());
        }

        private static void LoadCache(ISession session, string cacheId, TokenCache cache)
        {
            cache.Deserialize(session.Get(cacheId));
        }
    }
}
