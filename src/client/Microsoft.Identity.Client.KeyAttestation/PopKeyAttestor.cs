// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.KeyAttestation.Attestation;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Identity.Client.KeyAttestation
{
    /// <summary>
    /// Static facade for attesting a Credential Guard/CNG key and getting a JWT back.
    /// Caches valid MAA tokens to avoid redundant native DLL calls and network round-trips.
    /// Key discovery / rotation is the caller's responsibility.
    /// </summary>
    internal static class PopKeyAttestor
    {
        // In-process cache: maps "{endpoint}|{clientId}" → AttestationToken (JWT + expiry).
        private static readonly ConcurrentDictionary<string, AttestationToken> s_tokenCache =
            new ConcurrentDictionary<string, AttestationToken>(StringComparer.OrdinalIgnoreCase);

        // Per-key semaphores to prevent concurrent in-flight attestation for the same cache key (single-flight).
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> s_keyLocks =
            new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.OrdinalIgnoreCase);

        // Tokens within this window of expiry are considered stale and will be refreshed.
        // Matches MSAL's AccessTokenExpirationBuffer (5 minutes).
        internal static TimeSpan s_expirationBuffer = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Test hook to inject a mock attestation provider for unit testing.
        /// When set, this delegate is called instead of loading the native DLL.
        /// </summary>
        /// <remarks>
        /// This field is internal and accessible only via InternalsVisibleTo for test assemblies.
        /// Tests should not run in parallel when using this hook to avoid race conditions.
        /// </remarks>
        internal static Func<string, SafeHandle, string, CancellationToken, Task<AttestationResult>> s_testAttestationProvider;

        /// <summary>
        /// Resets the MAA token cache and key locks. Call from [TestCleanup] to prevent cache state leaking between tests.
        /// </summary>
        internal static void ResetCacheForTest()
        {
            s_tokenCache.Clear();
            s_keyLocks.Clear();
        }

        /// <summary>
        /// Asynchronously attests a Credential Guard/CNG key with the remote attestation service and returns a JWT.
        /// Returns a cached token if one is available and not within the expiration buffer.
        /// Uses a per-key semaphore to ensure only one in-flight attestation call occurs per cache key,
        /// preventing redundant native DLL and network calls under concurrency.
        /// Cancellation only applies before the native call starts.
        /// </summary>
        /// <param name="endpoint">Attestation service endpoint (required).</param>
        /// <param name="keyHandle">Valid SafeNCryptKeyHandle (must remain valid for duration of call).</param>
        /// <param name="clientId">Optional client identifier (may be null/empty).</param>
        /// <param name="cancellationToken">Cancellation token (cooperative before scheduling / start).</param>
        public static async Task<AttestationResult> AttestCredentialGuardAsync(
            string endpoint,
            SafeHandle keyHandle,
            string clientId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentNullException(nameof(endpoint));

            cancellationToken.ThrowIfCancellationRequested();

            // Fast path: check cache without acquiring any lock.
            // Key validation is intentionally deferred: a cache hit returns immediately
            // without requiring (or validating) the key handle.
            string cacheKey = BuildCacheKey(endpoint, clientId);
            if (TryGetCachedToken(cacheKey, out AttestationToken cached))
            {
                return new AttestationResult(
                    AttestationStatus.Success, cached, cached.Token, 0, string.Empty);
            }

            // Cache miss — validate the key handle before acquiring the lock.
            if (keyHandle is null)
                throw new ArgumentNullException(nameof(keyHandle));

            if (keyHandle.IsInvalid)
                throw new ArgumentException("keyHandle is invalid", nameof(keyHandle));

            var safeNCryptKeyHandle = keyHandle as SafeNCryptKeyHandle
                ?? throw new ArgumentException("keyHandle must be a SafeNCryptKeyHandle. Only Windows CNG keys are supported.", nameof(keyHandle));

            // Acquire per-key semaphore to ensure only one attestation call in-flight per cache key.
            SemaphoreSlim semaphore = s_keyLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Double-check cache after acquiring the lock — a concurrent caller may have populated it.
                if (TryGetCachedToken(cacheKey, out cached))
                {
                    return new AttestationResult(
                        AttestationStatus.Success, cached, cached.Token, 0, string.Empty);
                }

                // Check for test provider to avoid loading native DLL in unit tests.
                Task<AttestationResult> attestTask = s_testAttestationProvider != null
                    ? s_testAttestationProvider(endpoint, keyHandle, clientId, cancellationToken)
                    : Task.Run(() =>
                    {
                        try
                        {
                            using var client = new AttestationClient();
                            return client.Attest(endpoint, safeNCryptKeyHandle, clientId ?? string.Empty);
                        }
                        catch (Exception ex)
                        {
                            return new AttestationResult(AttestationStatus.Exception, null, string.Empty, -1, ex.Message);
                        }
                    }, cancellationToken);

                return await AttestAndCacheAsync(attestTask, cacheKey).ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Awaits the attestation task and writes the result to the cache on success.
        /// </summary>
        private static async Task<AttestationResult> AttestAndCacheAsync(
            Task<AttestationResult> attestTask,
            string cacheKey)
        {
            AttestationResult result = await attestTask.ConfigureAwait(false);

            if (result.Status == AttestationStatus.Success && result.Token != null)
            {
                s_tokenCache[cacheKey] = result.Token;
            }

            return result;
        }

        private static bool TryGetCachedToken(string cacheKey, out AttestationToken token)
        {
            if (s_tokenCache.TryGetValue(cacheKey, out token))
            {
                if (token.ExpiresOn - s_expirationBuffer > DateTimeOffset.UtcNow)
                {
                    return true;
                }

                // Evict the stale entry to prevent unbounded memory growth.
                s_tokenCache.TryRemove(cacheKey, out _);
            }

            token = null;
            return false;
        }

        private static string BuildCacheKey(string endpoint, string clientId)
        {
            return $"{endpoint}|{clientId ?? string.Empty}";
        }
    }
}
