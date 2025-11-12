// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Orchestrates mTLS binding retrieval:
    ///   1) local in-memory cache
    ///   2) per-key async gate (dedup concurrent mint)
    ///   3) persisted cache (best-effort)
    ///   4) factory mint + back-fill
    /// Persistence is best-effort and non-throwing.
    /// </summary>
    internal sealed class MtlsBindingCache : IMtlsBindingCache
    {
        private readonly KeyedSemaphorePool _gates = new();
        private readonly ICertificateCache _memory;
        private readonly IPersistentCertificateCache _persisted;

        /// <summary>
        /// Inject both caches to avoid global state and enable testing.
        /// </summary>
        public MtlsBindingCache(ICertificateCache memory, IPersistentCertificateCache persisted)
        {
            _memory = memory ?? throw new ArgumentNullException(nameof(memory));
            _persisted = persisted ?? throw new ArgumentNullException(nameof(persisted));
        }

        public async Task<Tuple<X509Certificate2, string, string>> GetOrCreateAsync(
            string cacheKey,
            Func<Task<Tuple<X509Certificate2, string, string>>> factory,
            CancellationToken cancellationToken,
            ILoggerAdapter logger)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
                throw new ArgumentException("cacheKey must be non-empty.", nameof(cacheKey));
            if (factory is null)
                throw new ArgumentNullException(nameof(factory));

            // 1) In-memory cache first
            if (_memory.TryGet(cacheKey, out var cached, logger))
            {
                return Tuple.Create(cached.Certificate, cached.Endpoint, cached.ClientId);
            }

            // 2) Per-key gate (dedupe concurrent mint)
            await _gates.EnterAsync(cacheKey, cancellationToken).ConfigureAwait(false);

            try
            {
                // Re-check after acquiring the gate
                if (_memory.TryGet(cacheKey, out cached, logger))
                {
                    return Tuple.Create(cached.Certificate, cached.Endpoint, cached.ClientId);
                }

                // 3) Persistent cache (best-effort)
                if (_persisted.Read(cacheKey, out var persisted, logger))
                {
                    if (persisted.Certificate.HasPrivateKey)
                    {
                        var v = new CertificateCacheValue(persisted.Certificate, persisted.Endpoint, persisted.ClientId);
                        _memory.Set(cacheKey, in v, logger);
                        return Tuple.Create(v.Certificate, v.Endpoint, v.ClientId);
                    }

                    // Defensive: persisted entry is unusable; dispose and mint new
                    persisted.Certificate.Dispose();
                    logger?.Verbose(() => "[PersistentCert] Skipping persisted cert without private key; minting new.");
                }

                // 4) Mint + back-fill mem + best-effort persist + prune
                var created = await factory().ConfigureAwait(false);
                var createdValue = new CertificateCacheValue(created.Item1, created.Item2, created.Item3);

                _memory.Set(cacheKey, in createdValue, logger);
                _persisted.Write(cacheKey, created.Item1, created.Item2, logger);
                _persisted.Delete(cacheKey, logger);

                return created;
            }
            finally
            {
                _gates.Release(cacheKey);
            }
        }
    }
}
