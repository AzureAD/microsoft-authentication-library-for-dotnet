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
        private static readonly object FileLock = new object();

        /// <summary>
        ///     File path where the token cache is serialized with the unified cache format (ADAL.NET V4, MSAL.NET V3)
        /// </summary>
        public static string UnifiedCacheFileName { get; private set; }

        /// <summary>
        ///     File path where the token cache is serialized with the legacy ADAL V3 format
        /// </summary>
        public static string AdalV3CacheFileName { get; private set; }

        /// <summary>
        ///     Get the user token cache
        /// </summary>
        /// <param name="adalV3CacheFileName">
        ///     File name where the cache is serialized with the ADAL V3 token cache format. Can
        ///     be <c>null</c> if you don't want to implement the legacy ADAL V3 token cache serialization in your MSAL 2.x+
        ///     application
        /// </param>
        /// <param name="tokenCache"></param>
        /// <param name="unifiedCacheFileName">
        ///     File name where the cache is serialized with the Unified cache format, common to
        ///     ADAL V4 and MSAL V2 and above, and also across ADAL/MSAL on the same platform. Should not be <c>null</c>
        /// </param>
        /// <returns></returns>
        public static void ConfigureUserCache(ITokenCache tokenCache, string unifiedCacheFileName, string adalV3CacheFileName)
        {
            UnifiedCacheFileName = unifiedCacheFileName;
            AdalV3CacheFileName = adalV3CacheFileName;
            if (tokenCache != null)
            {
                tokenCache.SetBeforeAccess(BeforeAccessNotification);
                tokenCache.SetAfterAccess(AfterAccessNotification);
            }
        }

        public static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (FileLock)
            {
                var adalv3State = ReadFromFileIfExists(AdalV3CacheFileName);
                var unifiedState = ReadFromFileIfExists(UnifiedCacheFileName);

                if (adalv3State != null)
                {
                    args.TokenCache.DeserializeAdalV3(adalv3State);
                }
                if (unifiedState != null)
                {
                    args.TokenCache.DeserializeMsalV2(unifiedState);
                }
            }
        }

        public static void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                lock (FileLock)
                {
                    var adalV3State = args.TokenCache.SerializeAdalV3();
                    var unifiedState = args.TokenCache.SerializeMsalV2();

                    // reflect changes in the persistent store
                    WriteToFileIfNotNull(UnifiedCacheFileName, unifiedState);
                    if (!string.IsNullOrWhiteSpace(AdalV3CacheFileName))
                    {
                        WriteToFileIfNotNull(AdalV3CacheFileName, adalV3State);
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