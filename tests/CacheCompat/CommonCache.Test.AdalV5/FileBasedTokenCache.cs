// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Security.Cryptography;
using CommonCache.Test.Common;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace CommonCache.Test.AdalV5
{
    public class FileBasedTokenCache : TokenCache
    {
        private static readonly object s_fileLock = new object();
        private readonly CacheStorageType _cacheStorageType;

        // Initializes the cache against a local file.
        // If the file is already present, it loads its content in the ADAL cache
        public FileBasedTokenCache(CacheStorageType cacheStorageType, string adalV3FilePath, string msalV2FilePath, string msalV3FilePath)
        {
            _cacheStorageType = cacheStorageType;
            AdalV3CacheFilePath = adalV3FilePath;
            MsalV2CacheFilePath = msalV2FilePath;
            MsalV3CacheFilePath = msalV3FilePath;

            AfterAccess = AfterAccessNotification;
            BeforeAccess = BeforeAccessNotification;

            LoadCache();
        }

        private void LoadCache()
        {
            lock (s_fileLock)
            {
                var adalV3Bytes = CacheFileUtils.ReadFromFileIfExists(AdalV3CacheFilePath);
                var msalV2Bytes = CacheFileUtils.ReadFromFileIfExists(MsalV2CacheFilePath);
                var msalV3Bytes = CacheFileUtils.ReadFromFileIfExists(MsalV3CacheFilePath);

                DeserializeAdalV3(adalV3Bytes);
                DeserializeMsalV2(msalV2Bytes);
                DeserializeMsalV3(msalV3Bytes);
            }
        }

        public string AdalV3CacheFilePath { get; }
        public string MsalV2CacheFilePath { get; }
        public string MsalV3CacheFilePath { get; }

        // Empties the persistent store.
        public override void Clear()
        {
            base.Clear();
            File.Delete(AdalV3CacheFilePath);
            File.Delete(MsalV2CacheFilePath);
            File.Delete(MsalV3CacheFilePath);
        }

        // Triggered right before ADAL needs to access the cache.
        // Reload the cache from the persistent store in case it changed since the last access.
        private void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            LoadCache();
        }

        // Triggered right after ADAL accessed the cache.
        private void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (HasStateChanged)
            {
                lock (s_fileLock)
                {
                    var adalV3Bytes = SerializeAdalV3();
                    var msalV2Bytes = SerializeMsalV2();
                    var msalV3Bytes = SerializeMsalV3();

                    // reflect changes in the persistent store
                    if ((_cacheStorageType & CacheStorageType.Adal) == CacheStorageType.Adal)
                    {
                        CacheFileUtils.WriteToFileIfNotNull(AdalV3CacheFilePath, adalV3Bytes);
                    }
                    if ((_cacheStorageType & CacheStorageType.MsalV2) == CacheStorageType.MsalV2)
                    {
                        CacheFileUtils.WriteToFileIfNotNull(MsalV2CacheFilePath, msalV2Bytes);
                    }
                    if ((_cacheStorageType & CacheStorageType.MsalV3) == CacheStorageType.MsalV3)
                    {
                        CacheFileUtils.WriteToFileIfNotNull(MsalV3CacheFilePath, msalV3Bytes);
                    }

                    // once the write operation took place, restore the HasStateChanged bit to false
                    HasStateChanged = false;
                }
            }
        }
    }
}
