using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetCoreTestApp
{
    /// <summary>
    /// A simplistic token cache serialization strategy that uses a file and no encryption. 
    /// Not recommended for production.
    /// </summary>
    public class TokenCacheHelper
    {
        /// <summary>
        /// Get the user token cache
        /// </summary>
        /// <returns></returns>
        public static TokenCache GetUserCache()
        {
            if (usertokenCache == null)
            {
                usertokenCache = new TokenCache();
                usertokenCache.SetBeforeAccess(BeforeAccessNotification);
                usertokenCache.SetAfterAccess(AfterAccessNotification);
            }
            return usertokenCache;
        }

        static TokenCache usertokenCache;

        /// <summary>
        /// Path to the token cache
        /// </summary>
        public static readonly string CacheFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalcache.bin";

        public static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            args.TokenCache.Deserialize(File.Exists(CacheFilePath)
                ? File.ReadAllBytes(CacheFilePath)
                : null);
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