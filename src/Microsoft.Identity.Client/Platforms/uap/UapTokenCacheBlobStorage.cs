//------------------------------------------------------------------------------
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
//------------------------------------------------------------------------------

using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Microsoft.Identity.Client.Platforms.uap
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>All continuations must be on the same thread, i.e. ConfigureAwait(true)
    /// because the TokenCache calls are under lock() so continuing on different threads will cause 
    /// deadlocks.
    /// </remarks>
    internal class UapTokenCacheBlobStorage : ITokenCacheBlobStorage
    {
        private const string CacheFileName = "msalcache.dat";

        private readonly ICryptographyManager _cryptographyManager;

        public UapTokenCacheBlobStorage(ICryptographyManager cryptographyManager, ICoreLogger logger)
        {
            _cryptographyManager = cryptographyManager;
        }

        public void OnBeforeWrite(TokenCacheNotificationArgs args)
        {
            // NO OP
        }

        public void OnBeforeAccess(TokenCacheNotificationArgs args)
        {
            OnBeforeAccessAsync(args);
        }
      
        public void OnAfterAccess(TokenCacheNotificationArgs args)
        {
            OnAfterAccessAsync(args);
        }


        private void OnAfterAccessAsync(TokenCacheNotificationArgs args)
        {
            if (args.HasStateChanged)
            {
                StorageFile cacheFile = ApplicationData.Current.LocalFolder.CreateFileAsync(
                    CacheFileName,
                    CreationCollisionOption.ReplaceExisting).AsTask().GetAwaiter().GetResult();

                byte[] blob = args.TokenCache.SerializeV3();
                byte[] encryptedBlob = _cryptographyManager.Encrypt(blob);

                FileIO.WriteBytesAsync(cacheFile, encryptedBlob).GetAwaiter().GetResult();
            }
        }

        private void OnBeforeAccessAsync(TokenCacheNotificationArgs args)
        {
            var cacheFile = ApplicationData.Current.LocalFolder.TryGetItemAsync(CacheFileName)
                                .AsTask().GetAwaiter().GetResult() as IStorageFile;

            if (cacheFile != null)
            {
                IBuffer buffer = FileIO.ReadBufferAsync(cacheFile).AsTask().GetAwaiter().GetResult();

                if (buffer.Length != 0)
                {
                    byte[] encryptedblob = buffer.ToArray();
                    byte[] decryptedBlob = _cryptographyManager.Decrypt(encryptedblob);
                    args.TokenCache.DeserializeV3(decryptedBlob);
                }
            }
        }

    }
}
