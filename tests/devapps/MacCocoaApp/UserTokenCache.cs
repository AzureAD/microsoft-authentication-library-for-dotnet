using System;
using System.IO;
using Microsoft.Identity.Client;

namespace MacCocoaApp
{
    public static class UserTokenCache
    {
        private static TokenCache _usertokenCache;

        // This is a simple, unencrypted, file based cache
        public static readonly string CacheFilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "mac_sample_cache.txt";


        public static TokenCache GetUserTokenCache()
        {
            if (_usertokenCache == null)
            {
                _usertokenCache = new TokenCache();
                _usertokenCache.SetBeforeAccess(BeforeAccessNotification);
                _usertokenCache.SetAfterAccess(AfterAccessNotification);
            }
            return _usertokenCache;
        }

        public static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
                args.TokenCache.Deserialize(
                    File.Exists(CacheFilePath) ? File.ReadAllBytes(CacheFilePath): null);
       }

        public static void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {

                // reflect changesgs in the persistent store
                File.WriteAllBytes(CacheFilePath, args.TokenCache.Serialize());

            }
        }
    }
}
