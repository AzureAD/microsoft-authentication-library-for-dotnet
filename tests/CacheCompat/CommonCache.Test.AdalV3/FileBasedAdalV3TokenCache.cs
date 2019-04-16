// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Security.Cryptography;
using CommonCache.Test.Common;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace CommonCache.Test.AdalV3
{
    // This is a simple persistent cache implementation for an ADAL V3 desktop application
    public class FileBasedAdalV3TokenCache : TokenCache
    {
        private static readonly object s_fileLock = new object();

        // Initializes the cache against a local file.
        // If the file is already present, it loads its content in the ADAL cache
        public FileBasedAdalV3TokenCache(string filePath)
        {
            CacheFilePath = filePath;
            AfterAccess = AfterAccessNotification;
            BeforeAccess = BeforeAccessNotification;
            lock (s_fileLock)
            {
                Deserialize(CacheFileUtils.ReadFromFileIfExists(CacheFilePath));
            }
        }

        public string CacheFilePath { get; }

        // Empties the persistent store.
        public override void Clear()
        {
            base.Clear();
            File.Delete(CacheFilePath);
        }

        // Triggered right before ADAL needs to access the cache.
        // Reload the cache from the persistent store in case it changed since the last access.
        private void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (s_fileLock)
            {
                Deserialize(CacheFileUtils.ReadFromFileIfExists(CacheFilePath));
            }
        }

        // Triggered right after ADAL accessed the cache.
        private void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (HasStateChanged)
            {
                lock (s_fileLock)
                {
                    // reflect changes in the persistent store
                    CacheFileUtils.WriteToFileIfNotNull(CacheFilePath, Serialize());
                    // once the write operation took place, restore the HasStateChanged bit to false
                    HasStateChanged = false;
                }
            }
        }
    }
}
