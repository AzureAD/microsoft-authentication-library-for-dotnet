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
        private static CacheStorageType s_cacheStorageType = CacheStorageType.None;

        public static string MsalV2CacheFileName { get; private set; }
        public static string MsalV3CacheFileName { get; private set; }
        public static string AdalV3CacheFileName { get; private set; }

        public static void ConfigureUserCache(
            CacheStorageType cacheStorageType,
            ITokenCache tokenCache,
            string adalV3CacheFileName,
            string msalV2CacheFileName,
            string msalV3CacheFileName)
        {
            s_cacheStorageType = cacheStorageType;
            MsalV2CacheFileName = msalV2CacheFileName;
            MsalV3CacheFileName = msalV3CacheFileName;
            AdalV3CacheFileName = adalV3CacheFileName;

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
                var adalv3State = ReadFromFileIfExists(AdalV3CacheFileName);
                var msalv2State = ReadFromFileIfExists(MsalV2CacheFileName);
                var msalv3State = ReadFromFileIfExists(MsalV3CacheFileName);

                if (adalv3State != null)
                {
                    args.TokenCache.DeserializeAdalV3(adalv3State);
                }
                if (msalv2State != null)
                {
                    args.TokenCache.DeserializeMsalV2(msalv2State);
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
                    var adalV3State = args.TokenCache.SerializeAdalV3();
                    var msalV2State = args.TokenCache.SerializeMsalV2();
                    var msalV3State = args.TokenCache.SerializeMsalV3();

                    // reflect changes in the persistent store
                    if ((s_cacheStorageType & CacheStorageType.Adal) == CacheStorageType.Adal)
                    {
                        if (!string.IsNullOrWhiteSpace(AdalV3CacheFileName))
                        {
                            WriteToFileIfNotNull(AdalV3CacheFileName, adalV3State);
                        }
                    }

                    if ((s_cacheStorageType & CacheStorageType.MsalV2) == CacheStorageType.MsalV2)
                    {
                        WriteToFileIfNotNull(MsalV2CacheFileName, msalV2State);
                    }

                    if ((s_cacheStorageType & CacheStorageType.MsalV3) == CacheStorageType.MsalV3)
                    {
                        WriteToFileIfNotNull(MsalV3CacheFileName, msalV3State);
                    }
                }
            }
        }

        /// <summary>
        ///     Read the content of a file if it exists
        /// </summary>
        /// <param name="path">File path</param>
        /// <returns>Content of the file (in bytes)</returns>
        private static byte[] ReadFromFileIfExists(string path)
        {
            byte[] protectedBytes = !string.IsNullOrEmpty(path) && File.Exists(path) ? File.ReadAllBytes(path) : null;
            byte[] unprotectedBytes = protectedBytes != null
                                          ? ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser)
                                          : null;
            return unprotectedBytes;
        }

        /// <summary>
        ///     Writes a blob of bytes to a file. If the blob is <c>null</c>, deletes the file
        /// </summary>
        /// <param name="path">path to the file to write</param>
        /// <param name="blob">Blob of bytes to write</param>
        private static void WriteToFileIfNotNull(string path, byte[] blob)
        {
            if (blob != null)
            {
                byte[] protectedBytes = ProtectedData.Protect(blob, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(path, protectedBytes);
            }
            else
            {
                File.Delete(path);
            }
        }
    }
}
