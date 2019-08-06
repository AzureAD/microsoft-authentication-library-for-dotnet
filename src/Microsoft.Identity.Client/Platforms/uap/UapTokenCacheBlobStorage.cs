// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
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

                byte[] blob = args.TokenCache.SerializeMsalV3();
                string cache = Encoding.UTF8.GetString(blob);

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
                    args.TokenCache.DeserializeMsalV3(decryptedBlob);
                }
            }
        }

    }
}
