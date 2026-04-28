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
        // In-process cache: maps "{endpoint}|{keyId}" → AttestationToken (JWT + expiry).
        //
        // Cache key design:
        //   endpoint  – included because the MAA service issues endpoint-specific tokens;
        //               eastus.attestation.azure.net and westus.attestation.azure.net issue
        //               different tokens for the same CNG key.
        //   keyId     – the CNG key identifier, either the CNG key name (CngKey.KeyName) for
        //               KSP-persisted keys, or a SHA-256 fingerprint of the RSA public key for
        //               ephemeral keys. Callers always supply a non-empty keyId; null/empty is
        //               treated as a defensive fallback that bypasses the cache.
        //
        //   clientId  – intentionally NOT included. MAA attests the CNG key itself; clientId is
        //               forwarded to the service as metadata but does not change which attestation
        //               token is valid for a given key. Different managed identities (SAMI vs UAMI)
        //               sharing the same CNG key at the same endpoint correctly share the cached token.
        //
        //   Why a dictionary and not a single field?
        //               A process could theoretically use multiple MAA endpoints (e.g. in multi-region
        //               deployments) or multiple named CNG keys (e.g. during key rotation). A single
        //               field would incorrectly return the wrong token for the second endpoint/key.
        //               With endpoint+keyId as a compound key the cache correctly scopes each entry.
        // Ordinal comparison is intentional: the endpoint is normalized to lowercase in BuildCacheKey,
        // so endpoint comparisons are effectively case-insensitive without making the keyId portion
        // case-insensitive (CNG key names and SHA-256 fingerprints are case-sensitive identifiers).
        private static readonly ConcurrentDictionary<string, AttestationToken> s_tokenCache =
            new ConcurrentDictionary<string, AttestationToken>(StringComparer.Ordinal);

        // Per-key async gates to prevent concurrent in-flight attestation for the same cache key (single-flight).
        // Callers (e.g. ImdsV2ManagedIdentitySource) always supply a non-empty keyId — either the CNG key's
        // KeyName (for KSP-persisted keys) or a SHA-256 fingerprint of the RSA public key (for ephemeral keys).
        // The null/empty keyId bypass below is therefore a defensive fallback, not the common path.
        // A typical process uses one CNG key per endpoint, so the pool size remains small in practice.
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
        /// The implementation wraps the synchronous native attestation call in <see cref="System.Threading.Tasks.Task.Run(System.Action)"/>;
        /// cancellation can prevent the operation from starting but cannot stop the native call once it has begun.
        /// </summary>
        /// <param name="endpoint">Attestation service endpoint (required).</param>
        /// <param name="keyHandle">Valid SafeNCryptKeyHandle (must remain valid for duration of call).</param>
        /// <param name="clientId">Optional client identifier (may be null/empty).</param>
        /// <param name="keyId">CNG key identifier scoping the cache entry. Callers should pass the
        /// CNG key's <see cref="System.Security.Cryptography.CngKey.KeyName"/> when available, or a
        /// stable derived identifier (e.g. SHA-256 fingerprint of the RSA public key) for ephemeral keys.
        /// Null/empty disables caching and is treated as a defensive fallback.</param>
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

            // Defensive fallback: if the caller supplies no keyId, skip the cache entirely.
            // Callers such as ImdsV2ManagedIdentitySource always derive a non-empty keyId (the CNG
            // key's KeyName, or a SHA-256 fingerprint for ephemeral/unnamed keys), so this path is
            // not reached under normal operation.
            if (string.IsNullOrEmpty(keyId))
            {
                logger?.Verbose(() => $"[PopKeyAttestor] Bypassing MAA token cache — no keyId supplied (endpoint '{Mask(endpoint)}').");

                Task<AttestationResult> nonCachedAttestTask = s_testAttestationProvider != null
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

                return await nonCachedAttestTask.ConfigureAwait(false);
            }

            string cacheKey = BuildCacheKey(endpoint, keyId);

            // Fast path: check cache without acquiring any lock.
            if (TryGetCachedToken(cacheKey, out AttestationToken cached))
            {
                logger?.Verbose(() => $"[PopKeyAttestor] MAA token cache hit for '{Mask(cacheKey)}'.");
                return new AttestationResult(
                    AttestationStatus.Success, cached, cached.Token, 0, string.Empty);
            }

            logger?.Verbose(() => $"[PopKeyAttestor] MAA token cache miss for '{Mask(cacheKey)}'. Acquiring semaphore.");

            // Acquire per-key semaphore to ensure only one attestation call in-flight per cache key.
            await s_keyLocks.EnterAsync(cacheKey, cancellationToken).ConfigureAwait(false);
            try
            {
                // Double-check cache after acquiring the lock — a concurrent caller may have populated it.
                if (TryGetCachedToken(cacheKey, out cached))
                {
                    logger?.Verbose(() => $"[PopKeyAttestor] MAA token cache hit (post-lock) for '{Mask(cacheKey)}'.");
                    return new AttestationResult(
                        AttestationStatus.Success, cached, cached.Token, 0, string.Empty);
                }

                logger?.Verbose(() => $"[PopKeyAttestor] Calling attestation provider for '{Mask(cacheKey)}'.");

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
                logger?.Verbose(() => $"[PopKeyAttestor] MAA token cached for '{Mask(cacheKey)}', expires {result.Token.ExpiresOn:O}.");
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

        private static string BuildCacheKey(string endpoint, string keyId)
        {
            return $"{NormalizeEndpoint(endpoint)}|{keyId ?? string.Empty}";
        }

        /// <summary>
        /// Normalizes an endpoint for use as a cache key component: lowercases and strips a trailing slash
        /// so that "https://Host/" and "https://host" map to the same cache key.
        /// Because the dictionary uses <see cref="StringComparer.Ordinal"/>, normalization is required
        /// to make endpoint comparisons case-insensitive while keeping the keyId portion case-sensitive.
        /// </summary>
        private static string NormalizeEndpoint(string endpoint)
        {
            return endpoint?.TrimEnd('/').ToLowerInvariant() ?? string.Empty;
        }

        /// <summary>
        /// Returns a truncated representation of a cache key for logging — avoids exposing the full
        /// endpoint URL or key fingerprint in logs. Matches the masking convention in InMemoryCertificateCache.
        /// </summary>
        private static string Mask(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "<empty>";

            if (s.Length <= 8)
                return $"…({s.Length})";

            return "…" + s.Substring(s.Length - 8, 8) + $"({s.Length})";
        }
    }
}
