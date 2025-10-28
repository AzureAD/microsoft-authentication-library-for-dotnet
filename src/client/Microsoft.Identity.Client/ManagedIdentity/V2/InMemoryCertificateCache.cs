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
    /// certificate+endpoint+clientId cache stored in process memory.
    /// </summary>
    internal sealed class InMemoryCertificateCache : ICertificateCache
    {
        private readonly ConcurrentDictionary<string, CertificateCacheEntry> _entriesByCacheKey =
            new ConcurrentDictionary<string, CertificateCacheEntry>(StringComparer.Ordinal);

        private int _disposed;

        /// <summary>
        /// try to get a cached certificate+endpoint+clientId for the specified cacheKey.
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="certificate"></param>
        /// <param name="endpoint"></param>
        /// <param name="clientId"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        /// <exception cref="MsalClientException"></exception>
        public bool TryGet(
            string cacheKey, 
            out X509Certificate2 certificate, 
            out string endpoint, 
            out string clientId,
            ILoggerAdapter logger = null)
        {
            certificate = null;
            endpoint = null;
            clientId = null;

            if (cacheKey == null)
                throw new MsalClientException(nameof(cacheKey));
            if (Volatile.Read(ref _disposed) != 0)
                return false;

            CertificateCacheEntry entry;
            if (_entriesByCacheKey.TryGetValue(cacheKey, out entry))
            {
                var nowUtc = DateTimeOffset.UtcNow;

                if (entry.IsExpiredUtc(nowUtc))
                {
                    CertificateCacheEntry removed;
                    if (_entriesByCacheKey.TryRemove(cacheKey, out removed))
                    {
                        removed.Dispose();
                        if (logger != null)
                            logger.Verbose(() => "[CertCache] Evicted expired entry (key='" + Mask(cacheKey) + "').");
                    }
                    return false;
                }

                // Return a clone so the caller can dispose independently.
                certificate = new X509Certificate2(entry.Certificate);
                endpoint = entry.Endpoint;
                clientId = entry.ClientId;

                if (logger != null)
                    logger.Verbose(() => "[CertCache] HIT (key='" + Mask(cacheKey) + "').");
                return true;
            }

            if (logger != null)
                logger.Verbose(() => "[CertCache] MISS (key='" + Mask(cacheKey) + "').");
            return false;
        }

        /// <summary>
        /// sets the cached certificate+endpoint+clientId for cacheKey.
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="certificate"></param>
        /// <param name="endpoint"></param>
        /// <param name="clientId"></param>
        /// <param name="logger"></param>
        /// <exception cref="MsalClientException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Set(
            string cacheKey, 
            X509Certificate2 certificate, 
            string endpoint, 
            string clientId,
            ILoggerAdapter logger = null)
        {
            if (cacheKey == null)
                throw new MsalClientException(nameof(cacheKey));
            if (certificate == null)
                throw new MsalClientException(nameof(certificate));
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new MsalClientException("Endpoint must be non-empty.", nameof(endpoint));
            if (string.IsNullOrWhiteSpace(clientId))
                throw new MsalClientException("ClientId must be non-empty.", nameof(clientId));
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException(nameof(InMemoryCertificateCache));

            var notAfterUtc = ToNotAfterUtc(certificate);
            var nowUtc = DateTimeOffset.UtcNow;

            if (notAfterUtc <= nowUtc + CertificateCacheEntry.MinRemainingLifetime)
            {
                if (logger != null)
                {
                    var remaining = notAfterUtc - nowUtc;
                    logger.Verbose(() => "[CertCache] Skipping certificate with insufficient remaining lifetime (" +
                                           $"{remaining.TotalHours:F2}h) (key='{Mask(cacheKey)}').");
                }
                return;
            }

            // Cache owns its copy; it will dispose upon eviction.
            var cachedCopy = new X509Certificate2(certificate);
            var newEntry = new CertificateCacheEntry(cachedCopy, notAfterUtc, endpoint, clientId);

            _entriesByCacheKey.AddOrUpdate(
                cacheKey,
                _ =>
                {
                    if (logger != null)
                        logger.Verbose(() => "[CertCache] SET (key='" + Mask(cacheKey) + "').");
                    return newEntry;
                },
                (_, old) =>
                {
                    old.Dispose();
                    if (logger != null)
                        logger.Verbose(() => "[CertCache] REPLACE (key='" + Mask(cacheKey) + "').");
                    return newEntry;
                });
        }

        /// <summary>
        /// removes an entry fromn the cache, if present.
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        /// <exception cref="MsalClientException"></exception>
        public bool Remove(string cacheKey, ILoggerAdapter logger = null)
        {
            if (cacheKey == null)
                throw new MsalClientException(nameof(cacheKey));
            CertificateCacheEntry entry;
            if (_entriesByCacheKey.TryRemove(cacheKey, out entry))
            {
                entry.Dispose();
                if (logger != null)
                    logger.Verbose(() => "[CertCache] REMOVE (key='" + Mask(cacheKey) + "').");
                return true;
            }
            return false;
        }

        /// <summary>
        /// for testing: clears all entries.
        /// </summary>
        /// <param name="logger"></param>
        public void Clear(ILoggerAdapter logger = null)
        {
            foreach (var kvp in _entriesByCacheKey)
            {
                CertificateCacheEntry entry;
                if (_entriesByCacheKey.TryRemove(kvp.Key, out entry))
                {
                    entry.Dispose();
                }
            }
            if (logger != null)
                logger.Verbose(() => "[CertCache] CLEAR.");
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;
            Clear();
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
        /// used for logging cache keys without exposing full values.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static string Mask(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "<empty>";
            var take = Math.Min(8, s.Length);
            return "…" + s.Substring(s.Length - take, take) + "(" + s.Length + ")";
        }
    }
}
