using System.IO;
using System.Text;
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
                LoadCacheFromSession(session, cacheId, cache);
            });

            cache.SetAfterAccess(delegate
            {
                // if the access operation resulted in a cache update
                if (cache.HasStateChanged)
                {
                    PersistCacheToSession(session, cacheId, cache);
                    cache.HasStateChanged = false;
                }
            });

            LoadCacheFromSession(session, cacheId, cache);

            return cache;
        }

        private static void PersistCacheToSession(ISession session, string cacheId,
            TokenCache cache)
        {
            session.Set(cacheId, cache.Serialize());
        }

        private static void LoadCacheFromSession(ISession session, string cacheId, TokenCache cache)
        {
            cache.Deserialize(session.Get(cacheId));
        }

        private static readonly string TokenCacheDir = Startup.Configuration["TokenCacheDir"];
        private const string TokenCacheFileExtension = ".txt";


        public static TokenCache GetMsalFileCacheInstance(string cacheId)
        {
            var cache = new TokenCache();

            cache.SetBeforeAccess(delegate
            {
                LoadCacheFromFile(cacheId, cache);
            });

            cache.SetAfterAccess(delegate
            {
                // if the access operation resulted in a cache update
                if (cache.HasStateChanged)
                {
                    PersistCacheToFile(cacheId, cache);
                    cache.HasStateChanged = false;
                }
            });

            LoadCacheFromFile(cacheId, cache);

            return cache;
        }

        private static void PersistCacheToFile(string cacheId,
            TokenCache cache)
        {
            var str = Encoding.UTF8.GetString(cache.Serialize());
            File.WriteAllText(TokenCacheDir + cacheId + TokenCacheFileExtension, str);
        }

        private static void LoadCacheFromFile(string cacheId, TokenCache cache)
        {
            var file = TokenCacheDir + cacheId + TokenCacheFileExtension;

            if (File.Exists(file))
            {
                var str = File.ReadAllText(file);
                cache.Deserialize(Encoding.UTF8.GetBytes(str));
            }
        }
    }
}
