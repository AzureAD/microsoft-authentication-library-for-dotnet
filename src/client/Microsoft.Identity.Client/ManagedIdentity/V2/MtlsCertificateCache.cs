// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
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
        private readonly ConcurrentDictionary<string, byte> _forceMint = new();

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

            bool forceMint = _forceMint.ContainsKey(cacheKey);

            // 1) In-memory cache first
            if (!forceMint && _memory.TryGet(cacheKey, out var cachedEntry, logger))
            {
                logger.Verbose(() =>
                    $"[PersistentCert] mTLS binding cache HIT (memory) for '{cacheKey}'.");

                return new MtlsBindingInfo(
                    cachedEntry.Certificate,
                    cachedEntry.Endpoint,
                    cachedEntry.ClientId);
            }

            // 2) Per-key gate (dedupe concurrent mint)
            await _gates.EnterAsync(cacheKey, cancellationToken).ConfigureAwait(false);

            try
            {
                forceMint = _forceMint.ContainsKey(cacheKey);

                // Re-check after acquiring the gate
                if (!forceMint && _memory.TryGet(cacheKey, out cachedEntry, logger))
                {
                    logger.Verbose(() =>
                        $"[PersistentCert] mTLS binding cache HIT (memory-after-gate) for '{cacheKey}'.");

                    return new MtlsBindingInfo(
                        cachedEntry.Certificate,
                        cachedEntry.Endpoint,
                        cachedEntry.ClientId);
                }

                // 3) Persistent cache (best-effort)
                if (!forceMint && _persisted.Read(cacheKey, out var persistedEntry, logger))
                {
                    logger.Verbose(() =>
                        $"[PersistentCert] mTLS binding cache HIT (persistent) for '{cacheKey}'.");

                    if (persistedEntry.Certificate.HasPrivateKey)
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
                        "[PersistentCert] Skipping persisted cert without private key; minting new.");
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

                if (forceMint)
                {
                    _forceMint.TryRemove(cacheKey, out _);
                }

                // Pass through the factory result (already an MtlsBindingInfo)
                return mintedBinding;
            }
            finally
            {
                _gates.Release(cacheKey);
            }
        }

        /// <summary>
        /// Removes a certificate from both in-memory and persistent cache when SCHANNEL rejects it.
        /// </summary>
        public void RemoveBadCert(string cacheKey, ILoggerAdapter logger)
        {
            if (cacheKey != null)
            {
                _forceMint[cacheKey] = 0;
            }

            try
            {
                _memory.Remove(cacheKey, logger);
                logger?.Verbose(() => $"[PersistentCert] Removed bad cert from memory cache for '{cacheKey}'");
            }
            catch (Exception ex)
            {
                logger?.Verbose(() => $"[PersistentCert] Error removing from memory cache: {ex.Message}");
            }

            try
            {
                _persisted.DeleteAllForAlias(cacheKey, logger);
                logger?.Verbose(() => $"[PersistentCert] Removed bad cert from persistent cache for '{cacheKey}'");
            }
            catch (Exception ex)
            {
                logger?.Verbose(() => $"[PersistentCert] Error removing from persistent cache: {ex.Message}");
            }
        }
    }
}
