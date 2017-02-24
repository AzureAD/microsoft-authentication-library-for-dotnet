
using System.IO;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace WinFormsAutomationApp
{
    internal static class TokenCacheDelegates
    {
        public static string CacheFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location + "adal-cache.txt";
        private static readonly object FileLock = new object();

        public static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (FileLock)
            {
                args.TokenCache.Deserialize(File.Exists(CacheFilePath)
                    ? ProtectedData.Unprotect(File.ReadAllBytes(CacheFilePath), null, DataProtectionScope.CurrentUser)
                    : null);
            }
        }

        public static void AfterAccessNotification(TokenCacheNotificationArgs args)
        {

            // if the access operation resulted in a cache update
            if (args.TokenCache.HasStateChanged)
            {
                lock (FileLock)
                {
                    // reflect changes in the persistent store
                    File.WriteAllBytes(CacheFilePath,
                        ProtectedData.Protect(args.TokenCache.Serialize(), null, DataProtectionScope.CurrentUser));
                    // once the write operation took place, restore the HasStateChanged bit to false
                    args.TokenCache.HasStateChanged = false;
                }
            }
        }
    }
}
