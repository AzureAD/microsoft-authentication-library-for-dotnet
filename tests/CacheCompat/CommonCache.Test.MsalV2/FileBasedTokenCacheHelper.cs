// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

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
