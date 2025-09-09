// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Represents the per-identity binding used by IMDSv2 mTLS flows:
    /// the issued client certificate plus the data needed to build the
    /// token request without re-issuing on every call.
    /// </summary>
    internal sealed class CertCacheEntry
    {
        /// <summary>
        /// The client certificate (with private key) used to authenticate over mTLS.
        /// </summary>
        public X509Certificate2 Certificate { get; private set; }

        /// <summary>
        /// UTC moment when the cache should proactively refresh this certificate.
        /// </summary>
        public DateTimeOffset RefreshAt { get; private set; }

        /// <summary>
        /// The authoritative client_id for the identity (SAMI/UAMI) as returned by IMDSv2.
        /// </summary>
        public string ClientId { get; private set; }

        /// <summary>
        /// The tenant to use when calling the token endpoint.
        /// </summary>
        public string TenantId { get; private set; }

        /// <summary>
        /// The base endpoint for OAuth2 token acquisition over mTLS.
        /// </summary>
        public string MtlsAuthenticationEndpoint { get; private set; }

        /// <summary>
        /// Initializes a new cache entry.
        /// </summary>
        public CertCacheEntry(
            X509Certificate2 certificate,
            DateTimeOffset refreshAt,
            string clientId,
            string tenantId,
            string mtlsAuthenticationEndpoint)
        {
            Certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
            RefreshAt = refreshAt;

            ClientId = clientId ?? string.Empty;
            TenantId = tenantId ?? string.Empty;
            MtlsAuthenticationEndpoint = mtlsAuthenticationEndpoint ?? string.Empty;
        }

        /// <summary>
        /// Forces a refresh on next access by setting <see cref="RefreshAt"/> to now.
        /// Useful when the server hints that the certificate was revoked/bad.
        /// </summary>
        public void ForceRefreshNow() => RefreshAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Minimal abstraction for the certificate cache.
    /// the certificate (CSR + /issuecredential + bind private key).
    /// </summary>
    internal interface ICertCache
    {
        /// <summary>
        /// Gets a valid binding for <paramref name="clientId"/> or refreshes it if needed.
        /// Exactly one caller per identity runs <paramref name="acquireFunc"/> under contention.
        /// </summary>
        Task<CertCacheEntry> GetAsync(
            string clientId,
            Func<CancellationToken, Task<CertCacheEntry>> acquireFunc,
            RequestContext ctx,
            CancellationToken ct);

        /// <summary>
        /// Marks the binding for <paramref name="clientId"/> as stale so that
        /// the next <see cref="GetAsync"/> will refresh it.
        /// </summary>
        void Invalidate(string clientId, string reason);

        /// <summary>
        /// Clears all entries (test-only; production code should not call this).
        /// </summary>
        void ClearForTest();
    }

    /// <summary>
    /// Process-wide, identity-scoped, thread-safe certificate cache,
    /// race-proofed to ensure only one mint/rotation occurs per identity.
    /// </summary>
    internal sealed class ManagedIdentityCertificateCache : ICertCache
    {
        /// <summary>
        /// Per-identity container that serializes mint/refresh via a gate.
        /// </summary>
        private sealed class Entry
        {
            private readonly SemaphoreSlim _gate = new (1, 1);
            private CertCacheEntry _current;

            internal Entry() { } // First caller will mint behind the gate.

            /// <summary>
            /// Returns the current binding if still fresh; otherwise mints/rotates exactly once.
            /// </summary>
            internal async Task<CertCacheEntry> GetOrRefreshAsync(
                Func<CancellationToken, Task<CertCacheEntry>> acquireFunc,
                RequestContext ctx,
                CancellationToken ct)
            {
                var now = DateTimeOffset.UtcNow;
                if (_current != null && now < _current.RefreshAt)
                {
                    return _current;
                }

                await _gate.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    now = DateTimeOffset.UtcNow;
                    if (_current != null && now < _current.RefreshAt)
                    {
                        return _current;
                    }

                    _current = await acquireFunc(ct).ConfigureAwait(false);
                    ctx.Logger.Info($"[Managed Identity] mTLS cert minted/rotated; expires={_current.Certificate.NotAfter:u}");
                    return _current;
                }
                finally
                {
                    _gate.Release();
                }
            }

            /// <summary>
            /// Forces a refresh by marking the current entry as expired.
            /// </summary>
            internal void Invalidate()
            {
                if (_current != null)
                {
                    _current.ForceRefreshNow();
                }
            }
        }

        // Identity map keyed by authoritative client_id (IMDSv2). Case-insensitive for GUID robustness.
        private readonly ConcurrentDictionary<string, Entry> _entries =
            new ConcurrentDictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        public async Task<CertCacheEntry> GetAsync(
            string clientId,
            Func<CancellationToken, Task<CertCacheEntry>> acquireFunc,
            RequestContext ctx,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                clientId = string.Empty; // normalize
            }

            // Race-proof: create the Entry up-front; the Entry gate ensures only one cert mint occurs.
            var entry = _entries.GetOrAdd(clientId, _ => new Entry());

            return await entry.GetOrRefreshAsync(acquireFunc, ctx, ct).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public void Invalidate(string clientId, string reason)
        {
            if (string.IsNullOrEmpty(clientId))
            {
                clientId = string.Empty; // normalize
            }

            Entry entry;
            if (_entries.TryGetValue(clientId, out entry))
            {
                entry.Invalidate();
                // Optional: hook telemetry here with `reason`.
            }
        }

        /// <inheritdoc />
        public void ClearForTest() => _entries.Clear();

        /// <summary>
        /// Computes a proactive refresh point for <paramref name="cert"/>: 20% of lifetime
        /// before expiry, capped at 24 hours. If already expired (or clock skew), returns now.
        /// </summary>
        public static DateTimeOffset ComputeRefreshAt(X509Certificate2 cert, DateTimeOffset now)
        {
            var notAfter = new DateTimeOffset(cert.NotAfter);
            var lifetime = notAfter - now;
            if (lifetime <= TimeSpan.Zero)
            {
                // Already expired or clock skew: force immediate refresh
                return now;
            }

            // 20% of lifetime, capped at 24h
            var margin = TimeSpan.FromTicks(lifetime.Ticks / 5);
            var cap = TimeSpan.FromHours(24);
            if (margin > cap)
            {
                margin = cap;
            }

            return notAfter - margin;
        }
    }
}
