// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using CommonCache.Test.Common;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;

namespace CommonCache.Test.MsalV2
{
    /// <summary>
    ///     Simple persistent cache implementation using the MSAL v3 format.
    ///     MSAL.NET v5 no longer reads or writes the MSAL v2 format.
    /// </summary>
    public static class FileBasedTokenCacheHelper
    {
        private static readonly object s_fileLock = new object();
        private static CacheStorageType s_cacheStorageType = CacheStorageType.None;

        public static string MsalV2CacheFileName { get; private set; }
        public static string MsalV3CacheFileName { get; private set; }

        public static void ConfigureUserCache(
            CacheStorageType cacheStorageType,
            ITokenCache tokenCache,
            string msalV2CacheFileName,
            string msalV3CacheFileName)
        {
            if ((cacheStorageType & CacheStorageType.MsalV2) != 0)
            {
                throw new NotSupportedException("MSAL.NET v5 requires MSAL v3 cache storage. Migrate v2 data with MSAL.NET 4.x first.");
            }

            s_cacheStorageType = cacheStorageType;
            MsalV2CacheFileName = msalV2CacheFileName;
            MsalV3CacheFileName = msalV3CacheFileName;

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
                var msalv3State = CacheFileUtils.ReadFromFileIfExists(MsalV3CacheFileName);

                if (msalv3State != null)
                {
                    args.TokenCache.DeserializeMsalV3(msalv3State);
                }
            }
        }

        public static void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                lock (s_fileLock)
                {
                    var msalV3State = args.TokenCache.SerializeMsalV3();

                    // reflect changes in the persistent store

                    if ((s_cacheStorageType & CacheStorageType.MsalV3) == CacheStorageType.MsalV3)
                    {
                        CacheFileUtils.WriteToFileIfNotNull(MsalV3CacheFileName, msalV3State);
                    }
                }
            }
        }
    }
}
