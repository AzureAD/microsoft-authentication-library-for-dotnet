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
    internal sealed class MtlsBindingCache : IMtlsCertificateCache
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

        /// <summary>
        /// Get or create mTLS binding info
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <param name="factory"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<MtlsBindingInfo> GetOrCreateAsync(
            string cacheKey,
            Func<Task<MtlsBindingInfo>> factory,
            CancellationToken cancellationToken,
            ILoggerAdapter logger)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                throw new ArgumentException("cacheKey must be non-empty.", nameof(cacheKey));
            }

            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            // 1) In-memory cache first
            if (_memory.TryGet(cacheKey, out var cachedEntry, logger))
            {
                if (CertificatePrivateKeyValidator.IsPrivateKeyAccessible(cachedEntry.Certificate, logger))
                {
                    logger.Verbose(() =>
                        $"[PersistentCert] mTLS binding cache HIT (memory) for '{cacheKey}'.");

                    return new MtlsBindingInfo(
                        cachedEntry.Certificate,
                        cachedEntry.Endpoint,
                        cachedEntry.ClientId);
                }

                // Key inaccessible (e.g. KeyGuard became unavailable after VBS failure).
                // Evict and fall through to mint a fresh cert.
                cachedEntry.Certificate.Dispose();
                _memory.Remove(cacheKey, logger);
                logger.Verbose(() =>
                    $"[PersistentCert] Evicted in-memory cert with inaccessible private key for '{cacheKey}'.");
            }

            // 2) Per-key gate (dedupe concurrent mint)
            await _gates.EnterAsync(cacheKey, cancellationToken).ConfigureAwait(false);

            try
            {
                // Re-check after acquiring the gate
                if (_memory.TryGet(cacheKey, out cachedEntry, logger))
                {
                    if (CertificatePrivateKeyValidator.IsPrivateKeyAccessible(cachedEntry.Certificate, logger))
                    {
                        logger.Verbose(() =>
                            $"[PersistentCert] mTLS binding cache HIT (memory-after-gate) for '{cacheKey}'.");

                        return new MtlsBindingInfo(
                            cachedEntry.Certificate,
                            cachedEntry.Endpoint,
                            cachedEntry.ClientId);
                    }

                    // Key inaccessible; evict and fall through to persistent/mint.
                    cachedEntry.Certificate.Dispose();
                    _memory.Remove(cacheKey, logger);
                    logger.Verbose(() =>
                        $"[PersistentCert] Evicted in-memory cert with inaccessible private key (after gate) for '{cacheKey}'.");
                }

                // 3) Persistent cache (best-effort)
                if (_persisted.Read(cacheKey, out var persistedEntry, logger))
                {
                    logger.Verbose(() =>
                        $"[PersistentCert] mTLS binding cache HIT (persistent) for '{cacheKey}'.");

                    if (persistedEntry.Certificate.HasPrivateKey &&
                        CertificatePrivateKeyValidator.IsPrivateKeyAccessible(persistedEntry.Certificate, logger))
                    {
                        var memoryEntry = new CertificateCacheValue(
                            persistedEntry.Certificate,
                            persistedEntry.Endpoint,
                            persistedEntry.ClientId);

                        _memory.Set(cacheKey, in memoryEntry, logger);

                        return new MtlsBindingInfo(
                            memoryEntry.Certificate,
                            memoryEntry.Endpoint,
                            memoryEntry.ClientId);
                    }

                    // Defensive: persisted entry is unusable; dispose and mint new
                    persistedEntry.Certificate.Dispose();
                    logger.Verbose(() =>
                        "[PersistentCert] Skipping persisted cert with inaccessible or missing private key; minting new.");
                }

                // 4) Mint + back-fill mem + best-effort persist + prune
                var mintedBinding = await factory().ConfigureAwait(false);

                logger.Verbose(() =>
                    $"[PersistentCert] mTLS binding cache MISS -> minted new binding for '{cacheKey}'.");

                var createdEntry = new CertificateCacheValue(
                    mintedBinding.Certificate,
                    mintedBinding.Endpoint,
                    mintedBinding.ClientId);

                _memory.Set(cacheKey, in createdEntry, logger);

                // Persist newest binding for this alias (best-effort; failures are logged by the implementation).
                _persisted.Write(cacheKey, mintedBinding.Certificate, mintedBinding.Endpoint, logger);

                // Then prune older/expired entries for this alias to keep the store bounded.
                // This is also best-effort and must not throw.
                _persisted.Delete(cacheKey, logger);

                // Pass through the factory result (already an MtlsBindingInfo)
                return mintedBinding;
            }
            finally
            {
                _gates.Release(cacheKey);
            }
        }
    }
}
