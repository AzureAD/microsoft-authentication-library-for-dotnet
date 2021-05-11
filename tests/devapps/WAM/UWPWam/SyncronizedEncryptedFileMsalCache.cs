using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.DataProtection;
using Windows.Storage.Streams;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Identity.Client;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;

namespace UWP_standalone
{

    /// <summary>
    /// A token cache implementation for use in UWP apps that:
    /// - encrypts data with DPAPI
    /// - syncronizes using a SemaphoreSlim (i.e. does not allow multiple reads / writes)
    /// </summary>
    internal class SyncronizedEncryptedFileMsalCache
    {
        private const string CacheFileName = "msalcache.dat"; // same as what MSAL out of the box

        private const string ProtectionDescriptor = "LOCAL=user";
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public void Initialize(ITokenCache tokenCache)
        {
            tokenCache.SetAfterAccessAsync(OnAfterAccessAsync);
            tokenCache.SetBeforeAccessAsync(OnBeforeAccessAsync);
        }

        private void Log(string message)
        {
            Debug.WriteLine(message);
        }

        private async Task OnBeforeAccessAsync(TokenCacheNotificationArgs args)
        {
            Log($"{DateTime.UtcNow} OnBeforeAccessAsync - before getting the lock " + _semaphoreSlim.CurrentCount);

            // prevent other threads / background tasks from reading the file
            //
            // TODO: this works for in-process background tasks, but not sure if it'll work for out of proc background tasks...
            // a different sync mechanism might be needed, such as named semaphore or a file lock. 
            await _semaphoreSlim.WaitAsync().ConfigureAwait(true);

            Log($"{DateTime.UtcNow} OnBeforeAccessAsync - acquired the lock");

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
                    Log("UWP cache file could not be loaded. Using in-memory cache only.");
                    Log(ex.ToString());

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
                Log("UWP cache file could not be loaded. Using in-memory cache only.");
                Log(ex.ToString());

                return;
            }           
            finally
            {
                Log($"{DateTime.UtcNow} OnAfterAccessAsync - released the lock");
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
                return clearBuffer.ToArray(0, (int)buffer.Length);
            }

            return null;
        }
    }
}
