﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
        private static CacheStorageType s_cacheStorageType = CacheStorageType.None;

        public static string MsalV2CacheFileName { get; private set; }
        public static string MsalV3CacheFileName { get; private set; }

        public static void ConfigureUserCache(
            CacheStorageType cacheStorageType,
            ITokenCache tokenCache,
            string msalV2CacheFileName,
            string msalV3CacheFileName)
        {
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
                var msalv2State = CacheFileUtils.ReadFromFileIfExists(MsalV2CacheFileName);
                var msalv3State = CacheFileUtils.ReadFromFileIfExists(MsalV3CacheFileName);

                if (msalv2State != null)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    args.TokenCache.DeserializeMsalV2(msalv2State);
#pragma warning restore CS0618 // Type or member is obsolete
                }
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
#pragma warning disable CS0618 // Type or member is obsolete
                    var msalV2State = args.TokenCache.SerializeMsalV2();
#pragma warning restore CS0618 // Type or member is obsolete
                    var msalV3State = args.TokenCache.SerializeMsalV3();

                    // reflect changes in the persistent store

                    if ((s_cacheStorageType & CacheStorageType.MsalV2) == CacheStorageType.MsalV2)
                    {
                        CacheFileUtils.WriteToFileIfNotNull(MsalV2CacheFileName, msalV2State);
                    }

                    if ((s_cacheStorageType & CacheStorageType.MsalV3) == CacheStorageType.MsalV3)
                    {
                        CacheFileUtils.WriteToFileIfNotNull(MsalV3CacheFileName, msalV3State);
                    }
                }
            }
        }
    }
}
