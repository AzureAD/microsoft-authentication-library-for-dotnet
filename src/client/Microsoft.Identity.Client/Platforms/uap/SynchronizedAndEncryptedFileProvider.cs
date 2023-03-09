// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Cache.CacheImpl;
using Microsoft.Identity.Client.Core;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Cryptography.DataProtection;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Microsoft.Identity.Client.Platforms.uap
{

    internal class SynchronizedAndEncryptedFileProvider : ICacheSerializationProvider
    {
        private const string CacheFileName = "msalcache.dat"; // same as what MSAL out of the box

        private const string ProtectionDescriptor = "LOCAL=user";
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly ILoggerAdapter _logger;

        public SynchronizedAndEncryptedFileProvider(ILoggerAdapter logger)
        {
            _logger = logger;
        }

        public void Initialize(TokenCache tokenCache)
        {
            tokenCache.AsyncAfterAccess = OnAfterAccessAsync;
            tokenCache.AsyncBeforeAccess = OnBeforeAccessAsync;
        }       

        private async Task OnBeforeAccessAsync(TokenCacheNotificationArgs args)
        {
            _logger.Verbose(() => $"OnBeforeAccessAsync - before getting the lock " + _semaphoreSlim.CurrentCount);

            // prevent other threads / background tasks from reading the file            
            await _semaphoreSlim.WaitAsync().ConfigureAwait(true);

            _logger.Verbose(() => $"OnBeforeAccessAsync - acquired the lock");

            IStorageFile cacheFile = await ApplicationData.Current.LocalFolder.TryGetItemAsync(CacheFileName) as IStorageFile;

            if (cacheFile != null)
            {
                byte[] decryptedBlob;
                try
                {
                    decryptedBlob = await ReadAndDecryptAsync(cacheFile).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    _logger.Error("UWP cache file could not be loaded. Using in-memory cache only.");
                    _logger.ErrorPii(ex);

                    return;
                }

                if (decryptedBlob != null)
                {
                    args.TokenCache.DeserializeMsalV3(decryptedBlob);
                }
            }
        }

        private async Task OnAfterAccessAsync(TokenCacheNotificationArgs args)
        {
            try
            {
                if (args.HasStateChanged)
                {
                    await EncryptAndWriteAsync(args).ConfigureAwait(true);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("UWP cache file could not be loaded. Using in-memory cache only.");
                _logger.ErrorPii(ex);

                return;
            }
            finally
            {
                _logger.Verbose(() => "OnAfterAccessAsync - released the lock");
                _semaphoreSlim.Release();
            }
        }

        private static async Task EncryptAndWriteAsync(TokenCacheNotificationArgs args)
        {
            StorageFile cacheFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(CacheFileName, CreationCollisionOption.ReplaceExisting);

            byte[] clearBlob = args.TokenCache.SerializeMsalV3();

            DataProtectionProvider dataProtectionProvider = new DataProtectionProvider(ProtectionDescriptor);
            IBuffer protectedBuffer = await dataProtectionProvider.ProtectAsync(clearBlob.AsBuffer());
            byte[] protectedBlob = protectedBuffer.ToArray(0, (int)protectedBuffer.Length);

            await FileIO.WriteBytesAsync(cacheFile, protectedBlob);
        }

        private async Task<byte[]> ReadAndDecryptAsync(IStorageFile cacheFile)
        {
            IBuffer buffer = await FileIO.ReadBufferAsync(cacheFile);

            if (buffer.Length != 0)
            {
                DataProtectionProvider dataProtectionProvider = new DataProtectionProvider(ProtectionDescriptor);
                IBuffer clearBuffer = await dataProtectionProvider.UnprotectAsync(buffer);
                return clearBuffer.ToArray(0, (int)clearBuffer.Length);
            }

            return null;
        }
    }   
}
