// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.KeyAttestation.Attestation;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
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
        // In-process cache: maps "{endpoint}|{clientId}|{keyId}" → AttestationToken (JWT + expiry).
        //
        // Cache key design:
        //   endpoint  – included because the MAA service issues endpoint-specific tokens;
        //               eastus.attestation.azure.net and westus.attestation.azure.net issue
        //               different tokens for the same key/client combination.
        //   clientId  – included because it is sent in the attestation request body and
        //               affects the claims in the issued token.
        //   keyId     – the CNG key name (CngKey.KeyName), added so that if the same
        //               endpoint+client ever uses two different CNG keys (e.g. after key
        //               rotation), the stale token for the old key is not returned for the
        //               new one. Null/empty for ephemeral (non-KSP) keys.
        private static readonly ConcurrentDictionary<string, AttestationToken> s_tokenCache =
            new ConcurrentDictionary<string, AttestationToken>(StringComparer.OrdinalIgnoreCase);

        // Per-key async gates to prevent concurrent in-flight attestation for the same cache key (single-flight).
        private static readonly KeyedSemaphorePool s_keyLocks = new KeyedSemaphorePool();

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
        internal static Func<string, SafeHandle, string, string, CancellationToken, Task<AttestationResult>> s_testAttestationProvider;

        static PopKeyAttestor()
        {
            // Register our cache reset with the MSAL master reset so that
            // IdWeb, AzSDK and other stacks can clear it via ApplicationBase.ResetStateForTest().
            ApplicationBase.RegisterResetCallback(ResetCacheForTest);
        }

        /// <summary>
        /// Resets the MAA token cache. Called automatically via
        /// <see cref="ApplicationBase.ResetStateForTest"/>; also callable directly from [TestCleanup].
        /// Note: the <see cref="KeyedSemaphorePool"/> is intentionally not reset — semaphores are
        /// stateless coordination primitives and do not hold cached data between tests.
        /// </summary>
        internal static void ResetCacheForTest()
        {
            s_tokenCache.Clear();
        }

        /// <summary>
        /// Asynchronously attests a Credential Guard/CNG key with the remote attestation service and returns a JWT.
        /// Returns a cached token if one is available and not within the expiration buffer.
        /// Uses a per-key semaphore to ensure only one in-flight attestation call occurs per cache key,
        /// preventing redundant native DLL and network calls under concurrency.
        /// </summary>
        /// <param name="endpoint">Attestation service endpoint (required).</param>
        /// <param name="keyHandle">Valid SafeNCryptKeyHandle (must remain valid for duration of call).</param>
        /// <param name="clientId">Optional client identifier (may be null/empty).</param>
        /// <param name="keyId">Optional CNG key name (<see cref="System.Security.Cryptography.CngKey.KeyName"/>).
        /// Pass the key name to scope the cache entry to a specific key; null/empty for ephemeral keys.</param>
        /// <param name="logger">Optional logger for cache hit/miss diagnostics.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static async Task<AttestationResult> AttestCredentialGuardAsync(
            string endpoint,
            SafeHandle keyHandle,
            string clientId,
            string keyId = null,
            ILoggerAdapter logger = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentNullException(nameof(endpoint));

            // Validate the key handle upfront — callers must always supply a valid key.
            if (keyHandle is null)
                throw new ArgumentNullException(nameof(keyHandle));

            if (keyHandle.IsInvalid)
                throw new ArgumentException("keyHandle is invalid", nameof(keyHandle));

            var safeNCryptKeyHandle = keyHandle as SafeNCryptKeyHandle
                ?? throw new ArgumentException("keyHandle must be a SafeNCryptKeyHandle. Only Windows CNG keys are supported.", nameof(keyHandle));

            string cacheKey = BuildCacheKey(endpoint, clientId, keyId);

            // Fast path: check cache without acquiring any lock.
            if (TryGetCachedToken(cacheKey, out AttestationToken cached))
            {
                logger?.Info($"[PopKeyAttestor] MAA token cache hit for '{cacheKey}'.");
                return new AttestationResult(
                    AttestationStatus.Success, cached, cached.Token, 0, string.Empty);
            }

            logger?.Info($"[PopKeyAttestor] MAA token cache miss for '{cacheKey}'. Acquiring semaphore.");

            // Acquire per-key semaphore to ensure only one attestation call in-flight per cache key.
            await s_keyLocks.EnterAsync(cacheKey, cancellationToken).ConfigureAwait(false);
            try
            {
                // Double-check cache after acquiring the lock — a concurrent caller may have populated it.
                if (TryGetCachedToken(cacheKey, out cached))
                {
                    logger?.Info($"[PopKeyAttestor] MAA token cache hit (post-lock) for '{cacheKey}'.");
                    return new AttestationResult(
                        AttestationStatus.Success, cached, cached.Token, 0, string.Empty);
                }

                logger?.Info($"[PopKeyAttestor] Calling attestation provider for '{cacheKey}'.");

                // Check for test provider to avoid loading native DLL in unit tests.
                Task<AttestationResult> attestTask = s_testAttestationProvider != null
                    ? s_testAttestationProvider(endpoint, keyHandle, clientId, keyId, cancellationToken)
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

                return await AttestAndCacheAsync(attestTask, cacheKey, logger).ConfigureAwait(false);
            }
            finally
            {
                s_keyLocks.Release(cacheKey);
            }
        }

        /// <summary>
        /// Awaits the attestation task and writes the result to the cache on success.
        /// </summary>
        private static async Task<AttestationResult> AttestAndCacheAsync(
            Task<AttestationResult> attestTask,
            string cacheKey,
            ILoggerAdapter logger)
        {
            AttestationResult result = await attestTask.ConfigureAwait(false);

            if (result.Status == AttestationStatus.Success && result.Token != null)
            {
                s_tokenCache[cacheKey] = result.Token;
                logger?.Info($"[PopKeyAttestor] MAA token cached for '{cacheKey}', expires {result.Token.ExpiresOn:O}.");
            }

            return result;
        }

        private static bool TryGetCachedToken(string cacheKey, out AttestationToken token)
        {
            if (s_tokenCache.TryGetValue(cacheKey, out token))
            {
                // Compare without subtraction to avoid overflow when ExpiresOn is DateTimeOffset.MinValue
                // (e.g. when the JWT contains no 'exp' claim).
                DateTimeOffset freshnessThreshold = DateTimeOffset.UtcNow + s_expirationBuffer;
                if (token.ExpiresOn > freshnessThreshold)
                {
                    return true;
                }

                // Evict the stale entry to prevent unbounded memory growth.
                s_tokenCache.TryRemove(cacheKey, out _);
            }

            token = null;
            return false;
        }

        private static string BuildCacheKey(string endpoint, string clientId, string keyId)
        {
            return $"{endpoint}|{clientId ?? string.Empty}|{keyId ?? string.Empty}";
        }
    }
}
