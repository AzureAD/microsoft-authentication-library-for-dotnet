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
        /// Resets the MAA token cache. Call from [TestCleanup] to prevent cache state leaking between tests.
        /// </summary>
        internal static void ResetCacheForTest()
        {
            s_tokenCache.Clear();
        }

        /// <summary>
        /// Asynchronously attests a Credential Guard/CNG key with the remote attestation service and returns a JWT.
        /// Returns a cached token if one is available and not within the expiration buffer.
        /// Wraps the synchronous <see cref="AttestationClient.Attest"/> in a Task.Run so callers can
        /// avoid blocking. Cancellation only applies before the native call starts.
        /// </summary>
        /// <param name="endpoint">Attestation service endpoint (required).</param>
        /// <param name="keyHandle">Valid SafeNCryptKeyHandle (must remain valid for duration of call).</param>
        /// <param name="clientId">Optional client identifier (may be null/empty).</param>
        /// <param name="cancellationToken">Cancellation token (cooperative before scheduling / start).</param>
        public static Task<AttestationResult> AttestCredentialGuardAsync(
            string endpoint,
            SafeHandle keyHandle,
            string clientId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentNullException(nameof(endpoint));

            cancellationToken.ThrowIfCancellationRequested();

            // Check the in-process cache before making any native/network calls.
            // Key validation is intentionally deferred: a cache hit returns immediately
            // without requiring (or validating) the key handle.
            string cacheKey = BuildCacheKey(endpoint, clientId);
            if (TryGetCachedToken(cacheKey, out AttestationToken cached))
            {
                return Task.FromResult(new AttestationResult(
                    AttestationStatus.Success, cached, cached.Token, 0, null));
            }

            // Cache miss — validate the key handle before any native/network call.
            if (keyHandle is null)
                throw new ArgumentNullException(nameof(keyHandle));

            if (keyHandle.IsInvalid)
                throw new ArgumentException("keyHandle is invalid", nameof(keyHandle));

            var safeNCryptKeyHandle = keyHandle as SafeNCryptKeyHandle
                ?? throw new ArgumentException("keyHandle must be a SafeNCryptKeyHandle. Only Windows CNG keys are supported.", nameof(keyHandle));

            // Check for test provider to avoid loading native DLL in unit tests.
            if (s_testAttestationProvider != null)
            {
                return AttestAndCacheAsync(
                    s_testAttestationProvider(endpoint, keyHandle, clientId, cancellationToken),
                    cacheKey);
            }

            return AttestAndCacheAsync(
                Task.Run(() =>
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
                }, cancellationToken),
                cacheKey);
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
            if (s_tokenCache.TryGetValue(cacheKey, out token) &&
                token.ExpiresOn - s_expirationBuffer > DateTimeOffset.UtcNow)
            {
                return true;
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
