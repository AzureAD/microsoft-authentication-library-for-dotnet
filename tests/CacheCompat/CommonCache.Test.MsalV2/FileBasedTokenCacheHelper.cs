// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Security.Cryptography;
using CommonCache.Test.Common;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;

namespace CommonCache.Test.MsalV2
{
    /// <summary>
    ///     Simple persistent cache implementation of the dual cache serialization (ADAL V3 legacy
    ///     and unified cache format) for a desktop applications (from MSAL 2.x)
    /// </summary>
    public static class FileBasedTokenCacheHelper
    {
        private static readonly object s_fileLock = new object();
        private static CacheStorageType s_cacheStorage = CacheStorageType.None;
        public static string AdalV3CacheFileName { get; private set; }
        public static string UnifiedCacheFileName { get; private set; }

        public static void ConfigureUserCache(CacheStorageType cacheStorageType, TokenCache tokenCache, string adalV3CacheFileName, string unifiedCacheFileName)
        {
            s_cacheStorage = cacheStorageType;
            AdalV3CacheFileName = adalV3CacheFileName;
            UnifiedCacheFileName = unifiedCacheFileName;
            if (tokenCache != null)
            {
                tokenCache.SetBeforeAccess(BeforeAccessNotification);
                tokenCache.SetAfterAccess(AfterAccessNotification);
            }
        }

        public static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (s_fileLock)
            {
                var adalv3State = CacheFileUtils.ReadFromFileIfExists(AdalV3CacheFileName);
                var unifiedState = CacheFileUtils.ReadFromFileIfExists(UnifiedCacheFileName);

                args.TokenCache.DeserializeUnifiedAndAdalCache(new CacheData { AdalV3State = adalv3State, UnifiedState = unifiedState });
            }
        }

        public static void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                lock (s_fileLock)
                {
                    var cacheData = args.TokenCache.SerializeUnifiedAndAdalCache();

                    // reflect changes in the persistent store
                    if ((s_cacheStorage & CacheStorageType.Adal) == CacheStorageType.Adal)
                    {
                        if (!string.IsNullOrWhiteSpace(AdalV3CacheFileName))
                        {
                            CacheFileUtils.WriteToFileIfNotNull(AdalV3CacheFileName, cacheData.AdalV3State);
                        }
                    }

                    if ((s_cacheStorage & CacheStorageType.MsalV2) == CacheStorageType.MsalV2)
                    {
                        CacheFileUtils.WriteToFileIfNotNull(UnifiedCacheFileName, cacheData.UnifiedState);
                    }
                }
            }
        }
    }
}
