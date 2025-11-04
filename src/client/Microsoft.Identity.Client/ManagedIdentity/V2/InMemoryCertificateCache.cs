// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Certificate + endpoint + clientId cache stored in process memory.
    /// </summary>
    internal sealed class InMemoryCertificateCache : ICertificateCache, IDisposable
    {
        private readonly ConcurrentDictionary<string, CertificateCacheEntry> _entriesByCacheKey =
            new ConcurrentDictionary<string, CertificateCacheEntry>(StringComparer.Ordinal);

        private int _disposed;

        /// <inheritdoc />
        public bool TryGet(
            string cacheKey,
            out CertificateCacheValue value,
            ILoggerAdapter logger = null)
        {
            ThrowIfDisposed();
            ValidateCacheKey(cacheKey);

            value = default;

            if (_entriesByCacheKey.TryGetValue(cacheKey, out var entry))
            {
                if (TryEvictIfExpired(cacheKey, entry, logger))
                {
                    return false;
                }

                // Return a clone so the caller can dispose independently.
                var certClone = new X509Certificate2(entry.Certificate);
                value = new CertificateCacheValue(certClone, entry.Endpoint, entry.ClientId);

                logger?.Verbose(() => "[CertCache] HIT (key='" + Mask(cacheKey) + "').");
                return true;
            }

            logger?.Verbose(() => "[CertCache] MISS (key='" + Mask(cacheKey) + "').");
            return false;
        }

        /// <inheritdoc />
        public void Set(
            string cacheKey,
            in CertificateCacheValue value,
            ILoggerAdapter logger = null)
        {
            ThrowIfDisposed();
            ValidateCacheKey(cacheKey);

            if (value.Certificate is null)
                throw new ArgumentNullException(nameof(value.Certificate));
            if (string.IsNullOrWhiteSpace(value.Endpoint))
                throw new ArgumentException("Endpoint must be non-empty.", nameof(value.Endpoint));
            if (string.IsNullOrWhiteSpace(value.ClientId))
                throw new ArgumentException("ClientId must be non-empty.", nameof(value.ClientId));

            var notAfterUtc = ToNotAfterUtc(value.Certificate);
            var nowUtc = DateTimeOffset.UtcNow;

            // Enforce minimum remaining lifetime (e.g., 24h).
            if (notAfterUtc <= nowUtc + CertificateCacheEntry.MinRemainingLifetime)
            {
                var remaining = notAfterUtc - nowUtc;
                logger?.Verbose(() =>
                    "[CertCache] Skipping certificate with insufficient remaining lifetime " +
                    $"({remaining.TotalHours:F2}h) (key='{Mask(cacheKey)}').");
                return;
            }

            // Cache owns its copy; it will dispose upon eviction.
            var cachedCopy = new X509Certificate2(value.Certificate);
            var newEntry = new CertificateCacheEntry(cachedCopy, notAfterUtc, value.Endpoint, value.ClientId);

            _entriesByCacheKey.AddOrUpdate(
                cacheKey,
                _ =>
                {
                    logger?.Verbose(() => "[CertCache] SET (key='" + Mask(cacheKey) + "').");
                    return newEntry;
                },
                (_, old) =>
                {
                    if (!old.IsDisposed)
                    {
                        old.Dispose();
                    }
                    logger?.Verbose(() => "[CertCache] REPLACE (key='" + Mask(cacheKey) + "').");
                    return newEntry;
                });
        }

        /// <inheritdoc />
        public bool Remove(string cacheKey, ILoggerAdapter logger = null)
        {
            ThrowIfDisposed();
            ValidateCacheKey(cacheKey);

            if (_entriesByCacheKey.TryRemove(cacheKey, out var entry))
            {
                if (!entry.IsDisposed)
                {
                    entry.Dispose();
                }
                logger?.Verbose(() => "[CertCache] REMOVE (key='" + Mask(cacheKey) + "').");
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        public void Clear(ILoggerAdapter logger = null)
        {
            ThrowIfDisposed();

            foreach (var kvp in _entriesByCacheKey)
            {
                if (_entriesByCacheKey.TryRemove(kvp.Key, out var entry))
                {
                    if (!entry.IsDisposed)
                    {
                        entry.Dispose();
                    }
                }
            }

            logger?.Verbose(() => "[CertCache] CLEAR.");
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            // Dispose entries and empty the map
            foreach (var kvp in _entriesByCacheKey)
            {
                if (_entriesByCacheKey.TryRemove(kvp.Key, out var entry))
                {
                    if (!entry.IsDisposed)
                    {
                        entry.Dispose();
                    }
                }
            }
        }

        // --- helpers ---

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                throw new ObjectDisposedException(nameof(InMemoryCertificateCache));
            }
        }

        private static void ValidateCacheKey(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
                throw new ArgumentException("Cache key must be non-empty.", nameof(cacheKey));
        }

        private bool TryEvictIfExpired(string cacheKey, CertificateCacheEntry entry, ILoggerAdapter logger)
        {
            var nowUtc = DateTimeOffset.UtcNow;
            if (!entry.IsExpiredUtc(nowUtc))
            {
                return false;
            }

            if (_entriesByCacheKey.TryRemove(cacheKey, out var removed))
            {
                if (!removed.IsDisposed)
                {
                    removed.Dispose();
                }
                logger?.Verbose(() => "[CertCache] Evicted expired entry (key='" + Mask(cacheKey) + "').");
            }

            return true;
        }

        private static DateTimeOffset ToNotAfterUtc(X509Certificate2 cert)
        {
            var notAfter = cert.NotAfter;
            if (notAfter.Kind == DateTimeKind.Unspecified)
            {
                notAfter = DateTime.SpecifyKind(notAfter, DateTimeKind.Local);
            }
            return new DateTimeOffset(notAfter.ToUniversalTime());
        }

        /// <summary>
        /// Used for logging cache keys without exposing full values.
        /// </summary>
        /// <param name="s">The sensitive string.</param>
        /// <returns>Masked representation.</returns>
        private static string Mask(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "<empty>";

            // Do not reveal full value for short keys
            if (s.Length <= 8)
                return $"…({s.Length})";

            var take = 8;
            return "…" + s.Substring(s.Length - take, take) + "(" + s.Length + ")";
        }
    }
}
