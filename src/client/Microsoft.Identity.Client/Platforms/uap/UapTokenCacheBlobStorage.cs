// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
        private readonly ICoreLogger _logger;

        public UapTokenCacheBlobStorage(ICryptographyManager cryptographyManager, ICoreLogger logger)
        {
            _cryptographyManager = cryptographyManager;
            _logger = logger;
        }

        public void OnBeforeWrite(TokenCacheNotificationArgs args)
        {
            // NO OP
        }

        public void OnBeforeAccess(TokenCacheNotificationArgs args)
        {
            IStorageFile cacheFile = ApplicationData.Current.LocalFolder.TryGetItemAsync(CacheFileName)
                                .AsTask().GetAwaiter().GetResult() as IStorageFile;


            if (cacheFile != null)
            {
                byte[] decryptedBlob = null;

                try
                {
                    decryptedBlob = ReadAndDecrypt(cacheFile);                   
                }
                catch (Exception ex)
                {
                    _logger.Error("The UWP cache file could not be decrypted. Corrupted files cannot be restored. Deleting the file.");
                    _logger.ErrorPii(ex);

                    cacheFile.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask().GetAwaiter().GetResult();
                    return;
                }

                if (decryptedBlob != null)
                {
                    args.TokenCache.DeserializeMsalV3(decryptedBlob);
                }
            }
        }

        private byte[] ReadAndDecrypt(IStorageFile cacheFile)
        {
            IBuffer buffer = FileIO.ReadBufferAsync(cacheFile).AsTask().GetAwaiter().GetResult();

            if (buffer.Length != 0)
            {
                byte[] encryptedblob = buffer.ToArray();
                return _cryptographyManager.Decrypt(encryptedblob);
            }

            return null;
        }

        public void OnAfterAccess(TokenCacheNotificationArgs args)
        {
            if (args.HasStateChanged)
            {
                StorageFile cacheFile = ApplicationData.Current.LocalFolder.CreateFileAsync(
                    CacheFileName,
                    CreationCollisionOption.ReplaceExisting).AsTask().GetAwaiter().GetResult();

                byte[] blob = args.TokenCache.SerializeMsalV3();
                byte[] encryptedBlob = _cryptographyManager.Encrypt(blob);

                FileIO.WriteBytesAsync(cacheFile, encryptedBlob).GetAwaiter().GetResult();
            }
        }
    }
}
