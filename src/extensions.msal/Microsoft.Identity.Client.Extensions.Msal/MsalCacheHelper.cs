// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
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
        /// A default logger for use if the user doesn't want to provide their own.
        /// </summary>
        private static readonly Lazy<TraceSource> s_staticLogger = new Lazy<TraceSource>(() =>
        {
            return (TraceSource)EnvUtils.GetNewTraceSource(nameof(MsalCacheHelper) + "Singleton");
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
        internal readonly MsalCacheStorage _store;

        /// <summary>
        /// Logger to log events to.
        /// </summary>
        private readonly TraceSource _logger;

        /// <summary>
        /// Gets the token cache
        /// </summary>
        private ITokenCache _userTokenCache;

        /// <summary>
        /// Contains a list of accounts that we know about. This is used as a 'before' list when the cache is changed on disk,
        /// so that we know which accounts were added and removed. Used when sending the <see cref="CacheChanged"/> event.
        /// </summary>
        private HashSet<string> _knownAccountIds;

        /// <summary>
        /// Contains the last-read bytes from _store, so multiple callers don't necessarily result in multiple reads from the file.
        /// </summary>
        private byte[] _cachedStoreData = new byte[0];

        /// <summary>
        /// Watches a filesystem location in order to fire events when the cache on disk is changed. Internal for testing.
        /// </summary>
        internal readonly FileSystemWatcher _cacheWatcher;

        /// <summary>
        /// Allows clients to listen for cache updates originating from disk.
        /// </summary>
        public event EventHandler<CacheChangedEventArgs> CacheChanged;

        /// <summary>
        /// Contains a reference to all caches currently registered to synchronize with this MsalCacheHelper, along with
        /// timestamp of the cache file the last time they deserialized.
        /// </summary>
        internal readonly HashSet<ITokenCache> _registeredCaches = new HashSet<ITokenCache>();

        /// <summary>
        /// Creates a new instance of <see cref="MsalCacheHelper"/>.
        /// </summary>
        /// <param name="storageCreationProperties">Properties to use when creating storage on disk.</param>
        /// <param name="logger">Passing null uses a default logger</param>
        /// <returns>A new instance of <see cref="MsalCacheHelper"/>.</returns>
        public static async Task<MsalCacheHelper> CreateAsync(StorageCreationProperties storageCreationProperties, TraceSource logger = null)
        {
            // We want CrossPlatLock around this operation so that we don't have a race against first read of the file and creating the watcher
            using (CreateCrossPlatLock(storageCreationProperties))
            {
                // Cache the list of accounts
                var accountIdentifiers = await GetAccountIdentifiersAsync(storageCreationProperties).ConfigureAwait(false);

                var cacheWatcher = new FileSystemWatcher(storageCreationProperties.CacheDirectory, storageCreationProperties.CacheFileName);
                var helper = new MsalCacheHelper(storageCreationProperties, logger, accountIdentifiers, cacheWatcher);
                cacheWatcher.EnableRaisingEvents = true;

                return helper;
            }
        }

        /// <summary>
        /// Gets the current set of accounts in the cache by creating a new public client, and
        /// deserializing the cache into a temporary object.
        /// </summary>
        private static async Task<HashSet<string>> GetAccountIdentifiersAsync(StorageCreationProperties storageCreationProperties)
        {
            var accountIdentifiers = new HashSet<string>();
            if (File.Exists(storageCreationProperties.CacheFilePath))
            {
                var pca = PublicClientApplicationBuilder.Create(storageCreationProperties.ClientId).Build();

                pca.UserTokenCache.SetBeforeAccess((args) =>
                {
                    var tempCache = new MsalCacheStorage(storageCreationProperties, s_staticLogger.Value);
                    // We're using ReadData here so that decryption is gets handled within the store.
                    args.TokenCache.DeserializeMsalV3(tempCache.ReadData());
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
        private MsalCacheHelper(StorageCreationProperties storageCreationProperties, TraceSource logger, HashSet<string> knownAccountIds, FileSystemWatcher cacheWatcher)
        {
            _logger = logger ?? s_staticLogger.Value;
            _storageCreationProperties = storageCreationProperties;
            _store = new MsalCacheStorage(_storageCreationProperties, _logger);
            _knownAccountIds = knownAccountIds;

            _cacheWatcher = cacheWatcher;
            _cacheWatcher.Changed += OnCacheFileChangedAsync;
            _cacheWatcher.Deleted += OnCacheFileChangedAsync;
        }

        private async void OnCacheFileChangedAsync(object sender, FileSystemEventArgs args)
        {
            try
            {
                IEnumerable<string> added = Enumerable.Empty<string>();
                IEnumerable<string> removed = Enumerable.Empty<string>();

                using (CreateCrossPlatLock(_storageCreationProperties))
                {
                    var currentAccountIds = await GetAccountIdentifiersAsync(_storageCreationProperties).ConfigureAwait(false);

                    var intersect = currentAccountIds.Intersect(_knownAccountIds);
                    removed = _knownAccountIds.Except(intersect);
                    added = currentAccountIds.Except(intersect);

                    _knownAccountIds = currentAccountIds;
                }

                if (added.Any() || removed.Any())
                {
                    CacheChanged?.Invoke(sender, new CacheChangedEventArgs(added, removed));
                }
            }
            catch (Exception e)
            {
                // Never let this throw, just log errors
                _logger.TraceEvent(TraceEventType.Warning, /*id*/ 0, $"Exception within File Watcher : {e}");
            }
        }

        /// <summary>
        /// An internal constructor allowing unit tests to data explicitly rather than initializing here.
        /// </summary>
        /// <param name="userTokenCache">The token cache to synchronize with the backing store</param>
        /// <param name="store">The backing store to use.</param>
        /// <param name="logger">Passing null uses the default logger</param>
        internal MsalCacheHelper(ITokenCache userTokenCache, MsalCacheStorage store, TraceSource logger = null)
        {
            _logger = logger ?? s_staticLogger.Value;
            _store = store;
            _storageCreationProperties = store._creationProperties;

            RegisterCache(userTokenCache);
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
        /// Registers a token cache to synchronize with on disk storage.
        /// </summary>
        /// <param name="tokenCache">Token Cache</param>
        public void RegisterCache(ITokenCache tokenCache)
        {
            // OK, we have two nested locks here. We need to maintain a clear ordering to avoid deadlocks.
            // 1. Use the CrossPlatLock which is respected by all processes and is used around all cache accesses.
            // 2. Use _lockObject which is used in UnregisterCache, and is needed for all accesses of _registeredCaches.
            //
            // Here specifically, we don't want to set this.CacheLock because we're done with the lock by the end of the method.
            using (var crossPlatLock = CreateCrossPlatLock(_storageCreationProperties))
            {
                lock (_lockObject)
                {
                    _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Registering token cache with on disk storage");
                    if (_registeredCaches.Contains(tokenCache))
                    {
                        _logger.TraceEvent(TraceEventType.Warning, /*id*/ 0, $"Redundant registration of {nameof(tokenCache)} in {nameof(MsalCacheHelper)}, skipping further registration.");
                        return;
                    }

                    _userTokenCache = tokenCache ?? throw new ArgumentNullException(nameof(tokenCache));

                    _userTokenCache.SetBeforeAccess(BeforeAccessNotification);
                    _userTokenCache.SetAfterAccess(AfterAccessNotification);

                    _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Initializing msal cache");

                    if (_store.HasChanged)
                    {
                        _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Before access, the store has changed");
                        byte[] fileData = _store.ReadData();
                        _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Read '{fileData?.Length}' bytes from storage");
                        _cachedStoreData = _store.ReadData();
                        _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Read '{_cachedStoreData?.Length}' bytes from storage");
                    }
                }

                _registeredCaches.Add(tokenCache); // Ignore return value, since we already bail if _registeredCaches contains tokenCache earlier

                _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Done initializing");
            }
        }

        /// <summary>
        /// Unregisters a token cache so it no longer synchronizes with on disk storage.
        /// </summary>
        /// <param name="tokenCache"></param>
        public void UnregisterCache(ITokenCache tokenCache)
        {
            lock (_lockObject)
            {
                _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Unregistering token cache from on disk storage");

                if (_registeredCaches.Contains(tokenCache))
                {
                    _registeredCaches.Remove(tokenCache);
                    tokenCache.SetBeforeAccess(args => { });
                    tokenCache.SetAfterAccess(args => { });
                }
                else
                {
                    _logger.TraceEvent(TraceEventType.Warning, /*id*/ 0, $"Attempting to unregister an already unregistered {nameof(tokenCache)} in {nameof(MsalCacheHelper)}");
                }
            }
        }

        /// <summary>
        /// Clears the token store
        /// </summary>
        public void Clear()
        {
            _store.Clear();
        }

        /// <summary>
        /// Gets a new instance of a lock for synchronizing against a cache made with the same creation properties.
        /// </summary>
        private static CrossPlatLock CreateCrossPlatLock(StorageCreationProperties storageCreationProperties)
        {
            return new CrossPlatLock(storageCreationProperties.CacheFilePath + ".lockfile", storageCreationProperties.LockRetryDelay, storageCreationProperties.LockRetryCount);
        }

        /// <summary>
        /// Before cache access
        /// </summary>
        /// <param name="args">Callback parameters from MSAL</param>
        internal void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Before access");

            _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Acquiring lock for token cache");

            // OK, we have two nested locks here. We need to maintain a clear ordering to avoid deadlocks.
            // 1. Use the CrossPlatLock which is respected by all processes and is used around all cache accesses.
            // 2. Use _lockObject which is used in UnregisterCache, and is needed for all accesses of _registeredCaches.
            CacheLock = CreateCrossPlatLock(_storageCreationProperties);

            _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Before access, the store has changed");
            _cachedStoreData = _store.ReadData();
            _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Read '{_cachedStoreData?.Length}' bytes from storage");

            lock (_lockObject)
            {
                try
                {
                    _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Deserializing the store");
                    args.TokenCache.DeserializeMsalV3(_cachedStoreData, shouldClearExistingCache: true);
                }
                catch (Exception e)
                {
                    _logger.TraceEvent(TraceEventType.Error, /*id*/ 0, $"An exception was encountered while deserializing the {nameof(MsalCacheHelper)} : {e}");
                    _logger.TraceEvent(TraceEventType.Error, /*id*/ 0, $"No data found in the store, clearing the cache in memory.");

                    // Clear the memory cache
                    Clear();
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
                _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"After access");

                // if the access operation resulted in a cache update
                if (args.HasStateChanged)
                {
                    _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"After access, cache in memory HasChanged");
                    try
                    {
                        _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Before Write Store");
                        byte[] data = args.TokenCache.SerializeMsalV3();

                        _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Serializing '{data.Length}' bytes");
                        _store.WriteData(data);

                        _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"After write store");
                    }
                    catch (Exception e)
                    {
                        _logger.TraceEvent(TraceEventType.Error, /*id*/ 0, $"An exception was encountered while serializing the {nameof(MsalCacheHelper)} : {e}");
                        _logger.TraceEvent(TraceEventType.Error, /*id*/ 0, $"No data found in the store, clearing the cache in memory.");

                        // The cache is corrupt clear it out
                        Clear();
                        throw;
                    }
                }
            }
            finally
            {
                // Get a local copy and call null before disposing because when the lock is disposed the next thread will replace CacheLock with its instance,
                // therefore we do not want to null out CacheLock after dispose since this may orphan a CacheLock.
                var localDispose = CacheLock;
                CacheLock = null;
                localDispose?.Dispose();
                _logger.TraceEvent(TraceEventType.Information, /*id*/ 0, $"Released lock");
            }
        }
    }
}
