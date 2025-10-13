// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Extensions.FileCache;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Identity.Client.MtlsPop.Attestation
{
    /// <summary>Cross-process/cache-first provider that never parses the JWT.</summary>
    internal sealed class CachedAttestationProvider : IAttestationProvider
    {
        private const string Bucket = "maa_attestation";
        private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan DefaultRefreshSkew = TimeSpan.FromMinutes(1);

        private readonly ISecureTokenCache _cache;
        private readonly NativeAttestationProvider _native;

        public CachedAttestationProvider(ISecureTokenCache cache, NativeAttestationProvider native)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _native = native ?? throw new ArgumentNullException(nameof(native));
        }

        public async Task<AttestationTokenResponse> GetAsync(AttestationTokenInput input, CancellationToken ct)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            // 1) Resolve a stable cache key (prefer KeyId via reflection; else fallback from handle+endpoint+clientId)
            string keyId = ResolveKeyId(input);

            // 2) Fast path: read from cache (no native call)
            byte[] cached = await _cache.TryReadAsync(Bucket, keyId, ct).ConfigureAwait(false);
            if (cached != null && cached.Length != 0)
            {
                return new AttestationTokenResponse { AttestationToken = Encoding.ASCII.GetString(cached) };
            }

            // 3) Resolve initial time hints (if not present we use defaults)
            var initialTimes = ResolveTimes(input);

            // 4) Cross-process-safe mint + write
            byte[] payload = await _cache.GetOrCreateAsync(
                Bucket,
                keyId,
                async (CancellationToken ctk) =>
                {
                    // Mint (no JWT parsing)
                    (byte[] token, DateTimeOffset? mintedExp, DateTimeOffset? mintedRefresh) =
                        await _native.MintAsync(input, ctk).ConfigureAwait(false);

                    // Prefer times on input; else minted; else defaults
                    var finalTimes = ResolveTimes(input, mintedExp, mintedRefresh);

                    return finalTimes.RefreshOnUtc.HasValue
                        ? new CacheValue(token, finalTimes.ExpiresOnUtc, finalTimes.RefreshOnUtc.Value)
                        : new CacheValue(token, finalTimes.ExpiresOnUtc);
                },
                ct).ConfigureAwait(false);

            return new AttestationTokenResponse { AttestationToken = Encoding.ASCII.GetString(payload) };
        }

        // ---------------- helpers ----------------

        private static string ResolveKeyId(AttestationTokenInput input)
        {
            var t = input.GetType();

            // Try 'KeyId' if present (public or internal)
            var kidProp = t.GetProperty("KeyId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (kidProp != null)
            {
                var kidObj = kidProp.GetValue(input, null) as string;
                if (!string.IsNullOrEmpty(kidObj))
                    return kidObj;
            }

            // Use only endpoint + clientId for a cross-process stable cache key
            string endpoint = TryGetEndpointString(input) ?? string.Empty;
            string clientId = TryGetString(input, "ClientId") ?? string.Empty;
            
            // Try to get a unique identifier for the key from KeyHandle
            SafeHandle sh = null;
            var khProp = t.GetProperty("KeyHandle", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (khProp != null)
                sh = khProp.GetValue(input, null) as SafeHandle;

            // Try to get key name or properties if available
            string keyInfo = string.Empty;
            if (sh != null && !sh.IsInvalid)
            {
                try
                {
                    // First try to get any key name/identifier from the handle through reflection
                    var keyInfoProp = sh.GetType().GetProperty("KeyName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (keyInfoProp != null)
                    {
                        var propValue = keyInfoProp.GetValue(sh, null);
                        if (propValue != null)
                            keyInfo = propValue.ToString();
                    }
                    
                    // If no name is available, use the handle type name which should be consistent
                    if (string.IsNullOrEmpty(keyInfo))
                        keyInfo = sh.GetType().FullName;
                }
                catch { /* best effort */ }
            }

            return "hk_" + HashShort(keyInfo + "_" + endpoint + "|" + clientId);
        }

        // If input provides ExpiresOnUtc/RefreshOnUtc, use them; otherwise use provided fallbacks; otherwise defaults.
        private static (DateTimeOffset ExpiresOnUtc, DateTimeOffset? RefreshOnUtc)
            ResolveTimes(AttestationTokenInput input, DateTimeOffset? fallbackExp = null, DateTimeOffset? fallbackRefresh = null)
        {
            var now = DateTimeOffset.UtcNow;

            var expOnInput = TryGetDto(input, "ExpiresOnUtc");
            var rfrOnInput = TryGetDto(input, "RefreshOnUtc");

            var exp = expOnInput ?? fallbackExp ?? now.Add(DefaultTtl);
            var rfr = rfrOnInput ?? fallbackRefresh ?? (exp - DefaultRefreshSkew);

            return (exp, rfr);
        }

        private static string TryGetEndpointString(AttestationTokenInput input)
        {
            var p = input.GetType().GetProperty("AttestationEndpoint",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p == null)
                return null;

            var v = p.GetValue(input, null);
            if (v is string s)
                return s;
            if (v is Uri u)
                return u.AbsoluteUri;
            return null;
        }

        private static string TryGetString(object obj, string name)
        {
            var p = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p == null)
                return null;
            var v = p.GetValue(obj, null);
            return v as string;
        }

        // *** FIX: this is where the 'nd' errors came from. ***
        private static DateTimeOffset? TryGetDto(object obj, string name)
        {
            var p = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p == null)
                return null;

            var v = p.GetValue(obj, null);
            if (v is DateTimeOffset dto)
                return dto;

            // Nullable<DateTimeOffset> path without relying on pattern-variables if your compiler balks
            if (v != null)
            {
                var t = v.GetType();
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                    t.GetGenericArguments()[0] == typeof(DateTimeOffset))
                {
                    var nd = (DateTimeOffset?)v;   // OK to use nullable internally
                    if (nd.HasValue)
                        return nd.Value;
                }
            }
            return null;
        }

        // small, dependency-free 64-bit hex hash
        private static string HashShort(string s)
        {
            unchecked
            {
                ulong h = 1469598103934665603UL; // FNV-1a
                for (int i = 0; i < s.Length; i++)
                {
                    h ^= (byte)s[i];
                    h *= 1099511628211UL;
                }
                return h.ToString("x16");
            }
        }
    }

    /// <summary>Calls the native AttestationClient. Ensures correct types for endpoint and key handle.</summary>
    internal sealed class NativeAttestationProvider
    {
        public async Task<(byte[] token, DateTimeOffset? expires, DateTimeOffset? refresh)> MintAsync(
            AttestationTokenInput input, CancellationToken ct)
        {
            return await Task.Run<(byte[] token, DateTimeOffset? expires, DateTimeOffset? refresh)>(() =>
            {
                using var client = new AttestationClient();

                // Cast SafeHandle -> SafeNCryptKeyHandle (AttestationClient requires SafeNCryptKeyHandle)
                var sh = GetKeyHandle(input);
                var handle = sh as SafeNCryptKeyHandle
                    ?? throw new InvalidOperationException("KeyHandle must be a SafeNCryptKeyHandle.");

                // AttestationClient expects endpoint as string
                string endpoint = GetEndpointString(input);

                bool addRef = false;
                try
                {
                    handle.DangerousAddRef(ref addRef);
                    var result = client.Attest(endpoint, handle, GetClientId(input));

                    if (result.Status != AttestationStatus.Success || string.IsNullOrEmpty(result.Jwt))
                    {
                        throw new InvalidOperationException(
                            $"Attestation failed: status={result.Status} rc={result.NativeErrorCode} err={result.ErrorMessage}");
                    }

                    var tokenBytes = Encoding.ASCII.GetBytes(result.Jwt);
                    // Return null time hints; cache will prefer any hints present on input (or defaults).
                    return (tokenBytes, null, null);
                }
                finally
                {
                    if (addRef)
                        handle.DangerousRelease();
                }
            }, ct).ConfigureAwait(false);
        }

        private static SafeHandle GetKeyHandle(AttestationTokenInput input)
        {
            var p = input.GetType().GetProperty("KeyHandle",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException("AttestationTokenInput.KeyHandle was not found.");
            var v = p.GetValue(input, null) as SafeHandle;
            if (v == null || v.IsInvalid)
                throw new InvalidOperationException("KeyHandle is invalid.");
            return v;
        }

        private static string GetEndpointString(AttestationTokenInput input)
        {
            var p = input.GetType().GetProperty("AttestationEndpoint",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException("AttestationTokenInput.AttestationEndpoint was not found.");
            var v = p.GetValue(input, null);
            if (v is Uri u)
                return u.AbsoluteUri;
            if (v is string s)
                return s;
            throw new InvalidOperationException("AttestationEndpoint must be a Uri or string.");
        }

        private static string GetClientId(AttestationTokenInput input)
        {
            var p = input.GetType().GetProperty("ClientId",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var v = p?.GetValue(input, null) as string;
            return v ?? string.Empty;
        }
    }
}
