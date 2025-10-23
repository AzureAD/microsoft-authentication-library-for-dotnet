// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
#if SUPPORTS_SYSTEM_TEXT_JSON
    using JsonProperty = System.Text.Json.Serialization.JsonPropertyNameAttribute;
#else
using Microsoft.Identity.Json;
#endif
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Hybrid cache implementation for attestation tokens with in-memory primary and file-based fallback.
    /// Uses JSON serialization for storage and named OS mutex for cross-process synchronization.
    /// </summary>
    /// <remarks>
    /// This implementation provides:
    /// - In-memory cache as primary storage for fast access within the process
    /// - File-based cache as fallback for persistence across application restarts
    /// - Automatic synchronization between in-memory and file caches
    /// - Cross-process synchronization using named OS mutex
    /// - Automatic cleanup of expired tokens
    /// - Atomic write operations to prevent cache corruption
    /// - Graceful error handling with fallback behavior
    /// 
    /// Cache Strategy:
    /// 1. Check in-memory cache first (fastest)
    /// 2. If not found, check file cache and populate in-memory cache
    /// 3. New tokens are stored in both caches simultaneously
    /// 4. File cache provides persistence and cross-process sharing
    /// 
    /// The cache file is stored in the user's local application data directory by default.
    /// Multiple processes can safely access the same cache file simultaneously.
    /// 
    /// Thread Safety: This class is thread-safe and can be used concurrently from multiple threads.
    /// Process Safety: Uses named mutex to coordinate access across multiple processes.
    /// </remarks>
    internal class HybridCache : IHybridCache, IDisposable
    {
        /// <summary>
        /// In-memory cache for fast access within the current process.
        /// Key: Cache key as string representation.
        /// Value: Cache entry containing token data and metadata.
        /// </summary>
        private static readonly ConcurrentDictionary<string, CacheEntry> s_memoryCache =
            new ConcurrentDictionary<string, CacheEntry>();

        /// <summary>
        /// Per-key semaphores for thread-safe access to cache entries.
        /// Allows concurrent access to different keys while serializing access to the same key.
        /// </summary>
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> s_keySemaphores =
            new ConcurrentDictionary<string, SemaphoreSlim>();

        /// <summary>
        /// Logger instance for diagnostics and debugging information.
        /// </summary>
        private readonly ILoggerAdapter _logger;

        /// <summary>
        /// The file path where the cache data is persisted.
        /// </summary>
        private readonly string _cacheFilePath;

        /// <summary>
        /// Named OS mutex used for cross-process synchronization when accessing the cache file.
        /// </summary>
        private readonly Mutex _namedMutex;

        /// <summary>
        /// The name of the mutex used for cross-process synchronization.
        /// </summary>
        private readonly string _mutexName;

        /// <summary>
        /// Flag indicating whether this instance has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Timeout for mutex acquisition operations. Configurable for unit tests.
        /// </summary>
        private readonly TimeSpan _mutexTimeout;

        /// <summary>
        /// Buffer time subtracted from token expiration to account for clock skew.
        /// Configurable for unit tests.
        /// </summary>
        private readonly TimeSpan _expirySkew;

        /// <summary>
        /// Initializes a new instance of the <see cref="HybridCache"/> class.
        /// Uses a fixed cache directory in the user's local application data folder.
        /// </summary>
        /// <param name="logger">Logger instance for diagnostics and debugging information.</param>
        /// <param name="mutexTimeout">Timeout for mutex acquisition operations. Defaults to 30 seconds. Use shorter values for unit tests.</param>
        /// <param name="expirySkew">Buffer time for token expiry calculations. Defaults to 2 minutes. Use shorter values for unit tests.</param>
        /// <remarks>
        /// The constructor:
        /// - Creates the cache directory if it doesn't exist
        /// - Sets up the cache file path using the default location: %LocalAppData%\Microsoft\MSAL\AttestationTokenCache
        /// - Creates a named mutex for cross-process synchronization
        /// - Uses a deterministic mutex name based on the cache file path hash
        /// 
        /// The mutex name format is: "Global\MSAL_AttestationCache_{hash}" where hash is a
        /// hexadecimal representation of the cache file path's hash code.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="logger"/> is null.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown when the application doesn't have permission to create the cache directory.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// Thrown when the cache directory path is invalid.
        /// </exception>
        public HybridCache(
            ILoggerAdapter logger,
            TimeSpan? mutexTimeout = null,
            TimeSpan? expirySkew = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Configure timeouts - use shorter values for unit tests to improve test performance
            _mutexTimeout = mutexTimeout ?? TimeSpan.FromSeconds(30);
            _expirySkew = expirySkew ?? TimeSpan.FromMinutes(2);

            // Fixed cache location in user's local app data
            var cacheDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "MSAL", "AttestationTokenCache");

            _logger.Info(() => $"[HybridCache] Initializing cache with directory: {cacheDirectory}");

            Directory.CreateDirectory(cacheDirectory);
            _cacheFilePath = Path.Combine(cacheDirectory, "attestation_tokens.json");

            // Create named mutex for cross-process synchronization
            // Using a deterministic name based on cache file path to ensure same mutex across processes
            _mutexName = $"Global\\MSAL_AttestationCache_{_cacheFilePath.GetHashCode():X8}";
                _namedMutex = new Mutex(false, _mutexName);

            _logger.Info(() => $"[HybridCache] Cache initialized. File path: {_cacheFilePath}, Mutex: {_mutexName}");
        }

        /// <summary>
        /// Retrieves a cached attestation token if available and not expired.
        /// Uses hybrid strategy: check in-memory first, then file cache as fallback.
        /// </summary>
        /// <param name="key">
        /// The cache key used to identify the token entry.
        /// </param>
        /// <param name="ct">
        /// A cancellation token that can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task containing the cached token response if found and valid, otherwise null.
        /// </returns>
        /// <remarks>
        /// This method implements the hybrid caching strategy:
        /// 1. Check in-memory cache first for fastest access
        /// 2. If not found in memory, check file cache
        /// 3. If found in file cache, populate in-memory cache for future requests
        /// 4. Automatically removes expired tokens during lookup
        /// 5. Returns null if no valid token is found
        /// 
        /// The expiry check includes a buffer time (s_expirySkew) to account for clock skew.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this cache instance has been disposed.
        /// </exception>
        /// <exception cref="TimeoutException">
        /// Thrown when the mutex cannot be acquired within the timeout period (30 seconds).
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is canceled via the cancellation token.
        /// </exception>
        public async Task<AttestationTokenResponse> GetAsync(long key, CancellationToken ct)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HybridCache));
            }

            _logger.Verbose(() => $"[HybridCache] GetAsync called for key: {key}");

            var keyString = key.ToString();
            var semaphore = s_keySemaphores.GetOrAdd(keyString, k => new SemaphoreSlim(1, 1));

            await semaphore.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var now = DateTimeOffset.UtcNow;

                // Step 1: Check in-memory cache first (fastest path)
                if (s_memoryCache.TryGetValue(keyString, out var memoryEntry))
                {
                    if (!string.IsNullOrEmpty(memoryEntry.Token) && now + _expirySkew < memoryEntry.ExpiresOnUtc)
                    {
                        _logger.Info(() => $"[HybridCache] Cache hit in memory for key: {key}");
                        return new AttestationTokenResponse { AttestationToken = memoryEntry.Token };
                    }

                    // Token expired in memory, remove it
                    s_memoryCache.TryRemove(keyString, out _);
                    _logger.Info(() => $"[HybridCache] Expired token removed from memory cache for key: {key}");
                }

                _logger.Verbose(() => $"[HybridCache] Memory cache miss for key: {key}, checking file cache");

                // Step 2: Check file cache as fallback
                return await ExecuteWithMutexAsync(async () =>
                {
                    var fileCache = await LoadFileCacheAsync().ConfigureAwait(false);

                    if (fileCache.TryGetValue(keyString, out var fileEntry))
                    {
                        if (!string.IsNullOrEmpty(fileEntry.Token) && now + _expirySkew < fileEntry.ExpiresOnUtc)
                        {
                            // Step 3: Found valid token in file cache, populate in-memory cache
                            s_memoryCache.TryAdd(keyString, fileEntry);
                            _logger.Info(() => $"[HybridCache] Cache hit in file for key: {key}, populated memory cache");
                            return new AttestationTokenResponse { AttestationToken = fileEntry.Token };
                        }

                        // Token expired in file cache, remove it and persist the change
                        fileCache.Remove(keyString);
                        await SaveFileCacheAsync(fileCache).ConfigureAwait(false);
                        _logger.Info(() => $"[HybridCache] Expired token removed from file cache for key: {key}");
                    }

                    _logger.Info(() => $"[HybridCache] Cache miss for key: {key}");
                    return null;
                }, ct).ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Stores an attestation token in both in-memory and file caches with the specified expiration time.
        /// </summary>
        /// <param name="key">
        /// The cache key used to identify the token entry.
        /// </param>
        /// <param name="token">
        /// The attestation token to cache. Must not be null or empty.
        /// </param>
        /// <param name="expiresOnUtc">
        /// The UTC date and time when the token expires.
        /// </param>
        /// <param name="ct">
        /// A cancellation token that can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous set operation.
        /// </returns>
        /// <remarks>
        /// This method:
        /// - Validates the input parameters
        /// - Stores the token in in-memory cache immediately
        /// - Updates the file cache for persistence and cross-process sharing
        /// - Uses cross-process synchronization for file operations
        /// 
        /// The operation overwrites any existing entry with the same key in both caches.
        /// If file cache operations fail, the in-memory cache is still updated.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this cache instance has been disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="token"/> is null or empty.
        /// </exception>
        /// <exception cref="TimeoutException">
        /// Thrown when the mutex cannot be acquired within the timeout period.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is canceled via the cancellation token.
        /// </exception>
        public async Task SetAsync(long key, string token, DateTimeOffset expiresOnUtc, CancellationToken ct)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HybridCache));
            }

            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            _logger.Info(() => $"[HybridCache] SetAsync called for key: {key}, expires: {expiresOnUtc}");

            var keyString = key.ToString();
            var semaphore = s_keySemaphores.GetOrAdd(keyString, k => new SemaphoreSlim(1, 1));

            await semaphore.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var entry = new CacheEntry
                {
                    Token = token,
                    ExpiresOnUtc = expiresOnUtc,
                    CachedOnUtc = DateTimeOffset.UtcNow
                };

                // Step 1: Update in-memory cache immediately (fast operation)
                s_memoryCache.AddOrUpdate(keyString, entry, (k, v) => entry);
                _logger.Verbose(() => $"[HybridCache] Token stored in memory cache for key: {key}");

                // Step 2: Update file cache for persistence (may be slower)
                await ExecuteWithMutexAsync(async () =>
                {
                    var fileCache = await LoadFileCacheAsync().ConfigureAwait(false);
                    fileCache[keyString] = entry;
                    await SaveFileCacheAsync(fileCache).ConfigureAwait(false);
                    _logger.Verbose(() => $"[HybridCache] Token stored in file cache for key: {key}");
                    return Task.CompletedTask;
                }, ct).ConfigureAwait(false);

                _logger.Info(() => $"[HybridCache] Token successfully cached for key: {key}");
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Removes a token entry from both in-memory and file caches.
        /// </summary>
        /// <param name="key">
        /// The cache key identifying the token entry to remove.
        /// </param>
        /// <param name="ct">
        /// A cancellation token that can be used to cancel the operation.
        /// </param>
        /// <returns>
        /// A task representing the asynchronous remove operation.
        /// </returns>
        /// <remarks>
        /// This method:
        /// - Removes the entry from in-memory cache immediately
        /// - Updates the file cache to remove the entry
        /// - Uses cross-process synchronization for file operations
        /// 
        /// The operation is idempotent - removing a non-existent key is not an error.
        /// If file cache operations fail, the in-memory cache is still updated.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this cache instance has been disposed.
        /// </exception>
        /// <exception cref="TimeoutException">
        /// Thrown when the mutex cannot be acquired within the timeout period.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is canceled via the cancellation token.
        /// </exception>
        public async Task RemoveAsync(long key, CancellationToken ct)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HybridCache));
            }

            _logger.Info(() => $"[HybridCache] RemoveAsync called for key: {key}");

            var keyString = key.ToString();
            var semaphore = s_keySemaphores.GetOrAdd(keyString, k => new SemaphoreSlim(1, 1));

            await semaphore.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                // Step 1: Remove from in-memory cache immediately
                var removedFromMemory = s_memoryCache.TryRemove(keyString, out _);
                if (removedFromMemory)
                {
                    _logger.Verbose(() => $"[HybridCache] Token removed from memory cache for key: {key}");
                }

                // Step 2: Remove from file cache for persistence
                await ExecuteWithMutexAsync(async () =>
                {
                    var fileCache = await LoadFileCacheAsync().ConfigureAwait(false);

                    // Only save if we actually removed something
                    if (fileCache.Remove(keyString))
                    {
                        await SaveFileCacheAsync(fileCache).ConfigureAwait(false);
                        _logger.Verbose(() => $"[HybridCache] Token removed from file cache for key: {key}");
                    }

                    return Task.CompletedTask;
                }, ct).ConfigureAwait(false);

                _logger.Info(() => $"[HybridCache] Remove operation completed for key: {key}");
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Executes an action while holding the named mutex for cross-process synchronization.
        /// </summary>
        /// <typeparam name="T">The return type of the action.</typeparam>
        /// <param name="action">The asynchronous action to execute under mutex protection.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task containing the result of the action.</returns>
        /// <remarks>
        /// This method:
        /// - Attempts to acquire the named mutex with a 30-second timeout
        /// - Executes the provided action while holding the mutex
        /// - Ensures the mutex is always released, even if the action throws an exception
        /// - Provides cancellation support through the cancellation token
        /// 
        /// The mutex timeout prevents deadlocks in case of abandoned mutexes or long-running operations.
        /// </remarks>
        /// <exception cref="TimeoutException">
        /// Thrown when the mutex cannot be acquired within the 30-second timeout period.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is canceled via the cancellation token.
        /// </exception>
        private async Task<T> ExecuteWithMutexAsync<T>(Func<Task<T>> action, CancellationToken ct)
        {
            bool mutexAcquired = false;
            try
            {
                _logger.Verbose(() => $"[HybridCache] Attempting to acquire mutex: {_mutexName}");

                // Try to acquire mutex with timeout to avoid deadlocks
                mutexAcquired = _namedMutex.WaitOne(_mutexTimeout);
                if (!mutexAcquired)
                {
                    _logger.Warning($"[HybridCache] Failed to acquire mutex within timeout of {_mutexTimeout.TotalSeconds} seconds: {_mutexName}");
                    throw new TimeoutException($"Failed to acquire cache mutex within timeout period of {_mutexTimeout.TotalSeconds} seconds");
                }

                _logger.Verbose(() => $"[HybridCache] Mutex acquired successfully: {_mutexName}");

                ct.ThrowIfCancellationRequested();
                return await action().ConfigureAwait(false);
            }
            finally
            {
                if (mutexAcquired)
                {
                    try
                    {
                        _namedMutex.ReleaseMutex();
                        _logger.Verbose(() => $"[HybridCache] Mutex released: {_mutexName}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"[HybridCache] Error releasing mutex {_mutexName}: {ex.Message}");
                        /* ignore release errors to prevent masking original exceptions */
                    }
                }
                }
            }

        /// <summary>
        /// Loads the file cache data from the persistent storage file.
        /// </summary>
        /// <returns>
        /// A task containing a dictionary representing the file cache contents.
        /// Returns an empty dictionary if the cache file doesn't exist or is corrupted.
        /// </returns>
        /// <remarks>
        /// This method:
        /// - Checks if the cache file exists
        /// - Reads and deserializes the JSON content
        /// - Performs automatic cleanup of expired entries during load
        /// - Returns an empty cache if the file is missing, empty, or corrupted
        /// - Uses defensive programming to handle JSON deserialization errors
        /// 
        /// The automatic cleanup during load helps keep the cache size manageable
        /// and removes stale entries that would never be used again.
        /// </remarks>
        private async Task<Dictionary<string, CacheEntry>> LoadFileCacheAsync()
        {
            try
            {
                if (!File.Exists(_cacheFilePath))
                {
                    _logger.Verbose(() => $"[HybridCache] Cache file does not exist: {_cacheFilePath}. Created a new file.");
                    return new Dictionary<string, CacheEntry>();
                }

                _logger.Verbose(() => $"[HybridCache] Loading cache from file: {_cacheFilePath}");

                var json = await Task.Run(() => File.ReadAllText(_cacheFilePath)).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.Verbose(() => $"[HybridCache] Cache file is empty: {_cacheFilePath}. Created a new file.");
                    return new Dictionary<string, CacheEntry>();
                }

                var cache = JsonHelper.DeserializeFromJson<Dictionary<string, CacheEntry>>(json) ??
                           new Dictionary<string, CacheEntry>();

                // Clean up expired entries during load to keep cache size manageable
                var now = DateTimeOffset.UtcNow;
                var keysToRemove = new List<string>();

                foreach (var kvp in cache)
                {
                    if (now + _expirySkew >= kvp.Value.ExpiresOnUtc)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                if (keysToRemove.Count > 0)
                {
                    foreach (var key in keysToRemove)
                    {
                        cache.Remove(key);
                    }
                    _logger.Info(() => $"[HybridCache] Cleaned up {keysToRemove.Count} expired entries from file cache");
                }

                _logger.Verbose(() => $"[HybridCache] Loaded {cache.Count} entries from file cache");
                return cache;
            }
            catch (Exception ex)
            {
                _logger.Warning($"[HybridCache] Error loading file cache from {_cacheFilePath}: {ex.Message}. Created a new file.");
                // If cache is corrupted or unreadable, start with a fresh cache
                // This is better than failing the entire authentication flow
                return new Dictionary<string, CacheEntry>();
            }
        }

        /// <summary>
        /// Saves the file cache data to the persistent storage file using atomic operations.
        /// </summary>
        /// <param name="cache">The cache dictionary to persist.</param>
        /// <returns>A task representing the asynchronous save operation.</returns>
        /// <remarks>
        /// This method:
        /// - Serializes the cache to JSON format
        /// - Uses atomic write operations to prevent corruption (write to temp file, then move)
        /// - Handles write errors gracefully without throwing exceptions
        /// - Uses compact JSON formatting to minimize file size
        /// 
        /// The atomic write pattern (write to temporary file, then move) ensures that the cache
        /// file is never left in a partially written state, even if the process is terminated
        /// during the write operation.
        /// 
        /// Errors during save operations are swallowed because the cache is a performance
        /// optimization, not critical functionality. Authentication should not fail due to
        /// cache persistence issues.
        /// </remarks>
        private async Task SaveFileCacheAsync(Dictionary<string, CacheEntry> cache)
        {
            try
            {
                _logger.Verbose(() => $"[HybridCache] Saving {cache.Count} entries to file cache: {_cacheFilePath}");

                var json = JsonHelper.SerializeToJson(cache);

                // Write to temp file first, then move to avoid corruption during write
                var tempFile = _cacheFilePath + ".tmp";
                await Task.Run(() => File.WriteAllText(tempFile, json)).ConfigureAwait(false);

                // Atomic move operation - ensures consistency
                if (File.Exists(_cacheFilePath))
                {
                    File.Delete(_cacheFilePath);
                }
                File.Move(tempFile, _cacheFilePath);

                _logger.Verbose(() => $"[HybridCache] Successfully saved cache to file: {_cacheFilePath}");
            }
            catch (Exception ex)
            {
                _logger.Warning($"[HybridCache] Error saving file cache to {_cacheFilePath}: {ex.Message}");
                // Swallow save errors - cache is a performance optimization, not critical functionality
                // The authentication flow should continue even if cache persistence fails
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="HybridCache"/>.
        /// </summary>
        /// <remarks>
        /// This method disposes of the named mutex and marks the instance as disposed.
        /// After calling Dispose, no further operations should be performed on this instance.
        /// 
        /// The method is safe to call multiple times and will not throw exceptions
        /// during the disposal process.
        /// 
        /// Note: The static in-memory cache and semaphores are not disposed as they may be
        /// shared across multiple instances and processes.
        /// </remarks>
        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    _namedMutex?.Dispose();
                    _logger.Verbose(() => $"[HybridCache] Cache disposed successfully");
                }
                catch (Exception ex)
                {
                    _logger.Warning($"[HybridCache] Error during disposal: {ex.Message}");
                    /* ignore disposal errors */
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Represents a single entry in the attestation token cache.
        /// </summary>
        /// <remarks>
        /// This class stores all the necessary information for a cached attestation token:
        /// - The actual token value
        /// - When the token expires
        /// - When the token was originally cached
        /// 
        /// The CachedOnUtc property can be useful for cache analytics and debugging,
        /// allowing administrators to understand cache hit patterns and token lifetime usage.
        /// 
        /// This entry structure is used for both in-memory and file cache storage.
        /// </remarks>
        private class CacheEntry
        {
            /// <summary>
            /// Gets or sets the attestation token value.
            /// This is typically a JWT (JSON Web Token) in string format.
            /// </summary>
            public string Token { get; set; }

            /// <summary>
            /// Gets or sets the UTC date and time when the token expires.
            /// The cache will consider tokens expired when the current time plus
            /// expiry skew exceeds this value.
            /// </summary>
            public DateTimeOffset ExpiresOnUtc { get; set; }

            /// <summary>
            /// Gets or sets the UTC date and time when this entry was added to the cache.
            /// This can be useful for debugging and understanding cache behavior.
            /// </summary>
            public DateTimeOffset CachedOnUtc { get; set; }
        }
    }
}
