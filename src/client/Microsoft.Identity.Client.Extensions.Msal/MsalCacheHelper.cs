// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Extensions.Msal
{
    /// <summary>
    /// Helper to create the token cache
    /// </summary>
    public class MsalCacheHelper
    {
        /// <summary>
        /// The name of the Default KeyRing collection. Secrets stored in this collection are persisted to disk
        /// </summary>
        public const string LinuxKeyRingDefaultCollection = "default";

        /// <summary>
        /// The name of the Session KeyRing collection. Secrets stored in this collection are not persisted to disk, but
        /// will be available for the duration of the user session.
        /// </summary>
        public const string LinuxKeyRingSessionCollection = "session";

        /// <summary>
        /// A default logger for use if the user doesn't want to provide their own.
        /// </summary>
        private static readonly Lazy<TraceSourceLogger> s_staticLogger = new Lazy<TraceSourceLogger>(() =>
        {
            return new TraceSourceLogger(EnvUtils.GetNewTraceSource(nameof(MsalCacheHelper) + "Singleton"));
        });

        /// <summary>
        /// A lock object for serialization
        /// </summary>
        private readonly object _lockObject = new object();

        /// <summary>
        /// Properties used to create storage on disk.
        /// </summary>
        private readonly StorageCreationProperties _storageCreationProperties;

        /// <summary>
        /// Holds a lock object when this helper is accessing the cache. Null otherwise.
        /// </summary>
        internal CrossPlatLock CacheLock { get; private set; }

        /// <summary>
        /// Storage that handles the storing of the adal cache file on disk. Internal for testing.
        /// </summary>
        internal /* internal for testing only */ Storage CacheStore { get; }

        /// <summary>
        /// Logger to log events to.
        /// </summary>
        private readonly TraceSourceLogger _logger;

        /// <summary>
        /// Contains a list of accounts that we know about. This is used as a 'before' list when the cache is changed on disk,
        /// so that we know which accounts were added and removed. Used when sending the <see cref="CacheChanged"/> event.
        /// </summary>
        private HashSet<string> _knownAccountIds;

        /// <summary>
        /// Watches a filesystem location in order to fire events when the cache on disk is changed. Internal for testing.
        /// </summary>
        private readonly FileSystemWatcher _cacheWatcher;

        private EventHandler<CacheChangedEventArgs> _cacheChangedEventHandler;
        /// <summary>
        /// Allows clients to listen for cache updates originating from disk.
        /// </summary>
        /// <remarks>
        /// This event does not fire when the application is built against Mono framework (e.g. Xamarin.Mac), but it does fire on .Net Core on all 3 operating systems.
        /// </remarks>
        public event EventHandler<CacheChangedEventArgs> CacheChanged
        {
            add
            {
                if (!_storageCreationProperties.IsCacheEventConfigured)
                {
                    throw new InvalidOperationException(
                        "To use this event, please configure the clientId and the authority " +
                        "using  StorageCreationPropertiesBuilder.WithCacheChangedEvent");
                }
                _cacheChangedEventHandler += value;
            }
            remove
            {
                _cacheChangedEventHandler -= value;
            }
        }

        /// <summary>
        /// Gets the current set of accounts in the cache by creating a new public client, and
        /// deserializing the cache into a temporary object.
        /// </summary>
        private static async Task<HashSet<string>> GetAccountIdentifiersNoLockAsync(  // executed in a cross plat lock context
            StorageCreationProperties storageCreationProperties,
            TraceSourceLogger logger)
        {
            var accountIdentifiers = new HashSet<string>();

            if (storageCreationProperties.IsCacheEventConfigured &&
                File.Exists(storageCreationProperties.CacheFilePath))
            {
                var pca = PublicClientApplicationBuilder
                    .Create(storageCreationProperties.ClientId)
                    .WithAuthority(storageCreationProperties.Authority)
                    .Build();

                pca.UserTokenCache.SetBeforeAccess((args) =>
                {
                    Storage tempCache = null;
                    try
                    {
                        tempCache = Storage.Create(storageCreationProperties, s_staticLogger.Value.Source);
                        // We're using ReadData here so that decryption is handled within the store.
                        byte[] data = null;
                        try
                        {
                            data = tempCache.ReadData();
                        }
                        catch
                        {
                            // ignore read failures, we will try again
                        }

                        if (data != null)
                        {
                            args.TokenCache.DeserializeMsalV3(data);
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogError("An error occured while reading the token cache: " + e);
                        logger.LogError("Deleting the token cache as it might be corrupt.");
                        tempCache.Clear(ignoreExceptions: true);
                    }
                });

                var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);

                foreach (var account in accounts)
                {
                    accountIdentifiers.Add(account.HomeAccountId.Identifier);
                }
            }

            return accountIdentifiers;
        }

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="storageCreationProperties">Properties to use when creating storage on disk.</param>
        /// <param name="logger">Passing null uses a default logger</param>
        /// <param name="knownAccountIds">The set of known accounts</param>
        /// <param name="cacheWatcher">Watcher for the cache file, to enable sending updated events</param>
        private MsalCacheHelper(
            StorageCreationProperties storageCreationProperties,
            TraceSource logger,
            HashSet<string> knownAccountIds, // only used for CacheChangedEvent
            FileSystemWatcher cacheWatcher)
        {
            _logger = logger == null ? s_staticLogger.Value : new TraceSourceLogger(logger);
            _storageCreationProperties = storageCreationProperties;
            CacheStore = Storage.Create(_storageCreationProperties, _logger.Source);
            _knownAccountIds = knownAccountIds;

            _cacheWatcher = cacheWatcher;
            if (_cacheWatcher != null)
            {
                _cacheWatcher.Changed += OnCacheFileChangedAsync;
                _cacheWatcher.Deleted += OnCacheFileChangedAsync;
            }
        }

#pragma warning disable VSTHRD100 // Avoid async void methods 
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        private async void OnCacheFileChangedAsync(object sender, FileSystemEventArgs args)
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            // avoid the high cost of computing the added / removed accounts if nobody listens to this
            if (_cacheChangedEventHandler?.GetInvocationList() != null &&
                _cacheChangedEventHandler?.GetInvocationList().Length > 0)
            {
                try
                {
                    IEnumerable<string> added;
                    IEnumerable<string> removed;

                    using (CreateCrossPlatLock(_storageCreationProperties))
                    {
                        var currentAccountIds = await GetAccountIdentifiersNoLockAsync(_storageCreationProperties, _logger).ConfigureAwait(false);

                        var intersect = currentAccountIds.Intersect(_knownAccountIds);
                        removed = _knownAccountIds.Except(intersect);
                        added = currentAccountIds.Except(intersect);

                        _knownAccountIds = currentAccountIds;
                    }

                    if (added.Any() || removed.Any())
                    {
                        _cacheChangedEventHandler.Invoke(sender, new CacheChangedEventArgs(added, removed));
                    }
                }
                catch (Exception e)
                {
                    // Never let this throw, just log errors
                    _logger.LogWarning($"Exception within File Watcher : {e}");
                }
            }
        }

        /// <summary>
        /// An internal constructor allowing unit tests to data explicitly rather than initializing here.
        /// </summary>
        /// <param name="userTokenCache">The token cache to synchronize with the backing store</param>
        /// <param name="store">The backing store to use.</param>
        /// <param name="logger">Passing null uses the default logger</param>
        internal MsalCacheHelper(ITokenCache userTokenCache, Storage store, TraceSource logger = null)
        {
            _logger = logger == null ? s_staticLogger.Value : new TraceSourceLogger(logger);
            CacheStore = store;
            _storageCreationProperties = store.StorageCreationProperties;

            RegisterCache(userTokenCache);
        }

        #region Public API

        /// <summary>
        /// Creates a new instance of <see cref="MsalCacheHelper"/>. To configure MSAL to use this cache persistence, call <see cref="RegisterCache(ITokenCache)"/>
        /// </summary>
        /// <param name="storageCreationProperties">Properties to use when creating storage on disk.</param>
        /// <param name="logger">Passing null uses the default TraceSource logger. See https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/wiki/Logging for details.</param>
        /// <returns>A new instance of <see cref="MsalCacheHelper"/>.</returns>
        public static async Task<MsalCacheHelper> CreateAsync(StorageCreationProperties storageCreationProperties, TraceSource logger = null)
        {
            if (storageCreationProperties is null)
            {
                throw new ArgumentNullException(nameof(storageCreationProperties));
            }

            // We want CrossPlatLock around this operation so that we don't have a race against first read of the file and creating the watcher
            using (CreateCrossPlatLock(storageCreationProperties))
            {
                // Cache the list of accounts
                var ts = logger == null ? s_staticLogger.Value : new TraceSourceLogger(logger);

                HashSet<string> accountIdentifiers = null;
                FileSystemWatcher cacheWatcher = null;

                if (storageCreationProperties.IsCacheEventConfigured)
                {
                    accountIdentifiers = await GetAccountIdentifiersNoLockAsync(storageCreationProperties, ts)
                        .ConfigureAwait(false);
                    cacheWatcher = new FileSystemWatcher(
                        storageCreationProperties.CacheDirectory,
                        storageCreationProperties.CacheFileName);
                }

                var helper = new MsalCacheHelper(storageCreationProperties, logger, accountIdentifiers, cacheWatcher);

                try
                {
                    if (!SharedUtilities.IsMonoPlatform() && storageCreationProperties.IsCacheEventConfigured)
                    {
                        cacheWatcher.EnableRaisingEvents = true;
                    }
                }
                catch (PlatformNotSupportedException)
                {
                    helper._logger.LogError(
                        "Cannot fire the CacheChanged event because the target framework does not support FileSystemWatcher. " +
                        "This is a known issue in Xamarin / Mono.");
                }

                return helper;
            }
        }

        /// <summary>
        /// Gets the user's root directory across platforms.
        /// </summary>
        public static string UserRootDirectory
        {
            get
            {
                return SharedUtilities.GetUserRootDirectory();
            }
        }

        /// <summary>
        /// Registers a token cache to synchronize with the persistent storage.
        /// </summary>
        /// <param name="tokenCache">The application token cache, typically referenced as <see cref="IClientApplicationBase.UserTokenCache"/></param>
        /// <remarks>Call <see cref="UnregisterCache(ITokenCache)"/> to have the given token cache stop synchronizing.</remarks>
        public void RegisterCache(ITokenCache tokenCache)
        {
            if (tokenCache == null)
            {
                throw new ArgumentNullException(nameof(tokenCache));
            }

            _logger.LogInformation($"Registering token cache with on disk storage");

            // If the token cache was already registered, this operation does nothing
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);

            _logger.LogInformation($"Done initializing");
        }

        /// <summary>
        /// Unregisters a token cache so it no longer synchronizes with on disk storage.
        /// </summary>
        public void UnregisterCache(ITokenCache tokenCache)
        {
            if (tokenCache == null)
            {
                throw new ArgumentNullException(nameof(tokenCache));
            }

            tokenCache.SetBeforeAccess(null);
            tokenCache.SetAfterAccess(null);
        }

        /// <summary>
        /// Clears the token store. Equivalent to a delete operation on the persistence layer (file delete). 
        /// </summary>
        /// <remarks>
        /// Apps should use MSAL's RemoveAccount to delete accounts, which is guaranteed to remove confidential information about that account. The token
        /// cache also contains metadata required for MSAL to operate, degrading the experience and perf when deleted.
        /// </remarks>
        [Obsolete(
            "Applications should not delete the entire cache to log out all users. Instead, call app.RemoveAsync(IAccount) for each account in the cache. ",
            false)]
        public void Clear()
        {
            using (CreateCrossPlatLock(_storageCreationProperties))
            {
                CacheStore.Clear(ignoreExceptions: true);
            }
        }

        /// <summary>
        /// Extracts the token cache data from the persistent store 
        /// </summary>
        /// <returns>an UTF-8 byte array of the unencrypted token cache</returns>
        /// <remarks>This method should be used with care. The data returned is unencrypted.</remarks>
        public byte[] LoadUnencryptedTokenCache()
        {
            using (CreateCrossPlatLock(_storageCreationProperties))
            {
                return CacheStore.ReadData();
            }
        }

        /// <summary>
        /// Saves an unencrypted, UTF-8 encoded byte array representing an MSAL token cache.
        /// The save operation will persist the data in a secure location, as configured in <see cref="StorageCreationProperties"/>
        /// </summary>
        public void SaveUnencryptedTokenCache(byte[] tokenCache)
        {
            using (CreateCrossPlatLock(_storageCreationProperties))
            {
                CacheStore.WriteData(tokenCache);
            }
        }

        #endregion

        /// <summary>
        /// Gets a new instance of a lock for synchronizing against a cache made with the same creation properties.
        /// </summary>
        private static CrossPlatLock CreateCrossPlatLock(StorageCreationProperties storageCreationProperties)
        {
            return new CrossPlatLock(
                storageCreationProperties.CacheFilePath + ".lockfile",
                storageCreationProperties.LockRetryDelay,
                storageCreationProperties.LockRetryCount);
        }

        /// <summary>
        /// Before cache access
        /// </summary>
        /// <param name="args">Callback parameters from MSAL</param>
        internal void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            _logger.LogInformation($"Before access");

            _logger.LogInformation($"Acquiring lock for token cache");

            // OK, we have two nested locks here. We need to maintain a clear ordering to avoid deadlocks.
            // 1. Use the CrossPlatLock which is respected by all processes and is used around all cache accesses.
            // 2. Use _lockObject which is used in UnregisterCache, and is needed for all accesses of _registeredCaches.
            CacheLock = CreateCrossPlatLock(_storageCreationProperties);

            _logger.LogInformation($"Before access, the store has changed");
            byte[] cachedStoreData = null;
            try
            {
                cachedStoreData = CacheStore.ReadData();
            }
            catch (Exception)
            {
                _logger.LogError($"Could not read the token cache. Ignoring. See previous error message.");
                return;

            }
            _logger.LogInformation($"Read '{cachedStoreData?.Length}' bytes from storage");

            lock (_lockObject)
            {
                try
                {
                    _logger.LogInformation($"Deserializing the store");
                    args.TokenCache.DeserializeMsalV3(cachedStoreData, shouldClearExistingCache: true);
                }
                catch (Exception e)
                {
                    _logger.LogError($"An exception was encountered while deserializing the {nameof(MsalCacheHelper)} : {e}");
                    _logger.LogError($"No data found in the store, clearing the cache in memory.");

                    // Clear the memory cache without taking the lock over again
                    CacheStore.Clear(ignoreExceptions: true);
                    throw;
                }
            }
        }

        /// <summary>
        /// After cache access
        /// </summary>
        /// <param name="args">Callback parameters from MSAL</param>
        internal void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            try
            {
                _logger.LogInformation($"After access");
                byte[] data = null;
                // if the access operation resulted in a cache update
                if (args.HasStateChanged)
                {
                    _logger.LogInformation($"After access, cache in memory HasChanged");
                    try
                    {
                        data = args.TokenCache.SerializeMsalV3();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"An exception was encountered while serializing the {nameof(MsalCacheHelper)} : {e}");
                        _logger.LogError($"No data found in the store, clearing the cache in memory.");

                        // The cache is corrupt clear it out
                        CacheStore.Clear(ignoreExceptions: true);
                        throw;
                    }

                    if (data != null)
                    {
                        _logger.LogInformation($"Serializing '{data.Length}' bytes");
                        try
                        {
                            CacheStore.WriteData(data);
                        }
                        catch (Exception)
                        {
                            _logger.LogError($"Could not write the token cache. Ignoring. See previous error message.");
                        }
                    }
                }
            }
            finally
            {
                ReleaseFileLock();
            }
        }

        private void ReleaseFileLock()
        {
            // Get a local copy and call null before disposing because when the lock is disposed the next thread will replace CacheLock with its instance,
            // therefore we do not want to null out CacheLock after dispose since this may orphan a CacheLock.
            var localDispose = CacheLock;
            CacheLock = null;
            localDispose?.Dispose();
            _logger.LogInformation($"Released lock");
        }

        /// <summary>
        /// Performs a write -> read -> clear using the underlying persistence mechanism and
        /// throws an <see cref="MsalCachePersistenceException"/> if something goes wrong.
        /// </summary>
        /// <remarks>Does not overwrite the token cache. Should never fail on Windows and Mac where the cache accessors are guaranteed to exist by the OS.</remarks>
        public void VerifyPersistence()
        {
            CacheStore.VerifyPersistence();
        }
    }
}
