// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
        // OID 1.3.6.1.4.1.311.90.2.1 is the Microsoft-defined token_not_after X.509 extension.
        // ESS embeds this in issued credentials to indicate when the cert can no longer be used
        // to acquire tokens (even while the X.509 NotAfter is still in the future for renewal).
        // MSAL reads this OID to proactively evict a cached cert before it would be rejected.
        internal const string TokenNotAfterOid = "1.3.6.1.4.1.311.90.2.1";
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
                if (!IsCertTokenExpiredForTokenRequests(cachedEntry.Certificate, logger))
                {
                    logger.Verbose(() =>
                        $"[PersistentCert] mTLS binding cache HIT (memory) for '{cacheKey}'.");

                    return new MtlsBindingInfo(
                        cachedEntry.Certificate,
                        cachedEntry.Endpoint,
                        cachedEntry.ClientId);
                }

                // Cert is past its token_not_after window; evict and fall through to mint.
                logger.Verbose(() =>
                    $"[PersistentCert] mTLS binding cache HIT (memory) for '{cacheKey}' but cert is past token validity window. Evicting and minting fresh.");
                _memory.Remove(cacheKey, logger);
            }

            // 2) Per-key gate (dedupe concurrent mint)
            await _gates.EnterAsync(cacheKey, cancellationToken).ConfigureAwait(false);

            try
            {
                forceMint = _forceMint.ContainsKey(cacheKey);

                // Re-check after acquiring the gate
                if (!forceMint && _memory.TryGet(cacheKey, out cachedEntry, logger))
                {
                    if (!IsCertTokenExpiredForTokenRequests(cachedEntry.Certificate, logger))
                    {
                        logger.Verbose(() =>
                            $"[PersistentCert] mTLS binding cache HIT (memory-after-gate) for '{cacheKey}'.");

                        return new MtlsBindingInfo(
                            cachedEntry.Certificate,
                            cachedEntry.Endpoint,
                            cachedEntry.ClientId);
                    }

                    logger.Verbose(() =>
                        $"[PersistentCert] mTLS binding (memory-after-gate) for '{cacheKey}' is past token validity window. Evicting and minting fresh.");
                    _memory.Remove(cacheKey, logger);
                }

                // 3) Persistent cache (best-effort)
                if (!forceMint && _persisted.Read(cacheKey, out var persistedEntry, logger))
                {
                    logger.Verbose(() =>
                        $"[PersistentCert] mTLS binding cache HIT (persistent) for '{cacheKey}'.");

                    if (persistedEntry.Certificate.HasPrivateKey &&
                        !IsCertTokenExpiredForTokenRequests(persistedEntry.Certificate, logger))
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
        /// Returns <see langword="true"/> if the cached cert's token validity window has closed
        /// and the cert should therefore be treated as a cache miss so a fresh cert is minted.
        /// </summary>
        /// <remarks>
        /// ESS-issued credentials embed a <c>token_not_after</c> X.509 extension (OID
        /// <see cref="TokenNotAfterOid"/>) that Entra uses to reject token requests even while
        /// the X.509 <c>NotAfter</c> is still in the future (the cert remains usable for renewal
        /// after the token window closes). MSAL reads the OID directly so the check is always
        /// accurate regardless of the configured validity window. For certs that do not carry the
        /// OID (old format or CSK certs where token validity == cert validity), the standard
        /// X.509 <c>NotAfter</c> is used as the fallback boundary.
        /// </remarks>
        internal static bool IsCertTokenExpiredForTokenRequests(X509Certificate2 cert, ILoggerAdapter logger)
        {
            if (cert is null)
            {
                return true;
            }

            DateTimeOffset tokenNotAfter;
            var oidExt = cert.Extensions[TokenNotAfterOid];

            if (oidExt is not null && TryParseTokenNotAfterExtension(oidExt.RawData, out DateTimeOffset parsedNotAfter))
            {
                tokenNotAfter = parsedNotAfter;
                logger?.Verbose(() =>
                    $"[PersistentCert] Read token_not_after OID: {tokenNotAfter:u}.");
            }
            else
            {
                // OID absent or unparseable — fall back to the X.509 NotAfter as the token boundary.
                tokenNotAfter = new DateTimeOffset(cert.NotAfter, TimeSpan.Zero);
                logger?.Verbose(() =>
                    $"[PersistentCert] token_not_after OID not present; using cert NotAfter {tokenNotAfter:u} as boundary.");
            }

            var now = DateTimeOffset.UtcNow;
            if (now >= tokenNotAfter)
            {
                logger?.Verbose(() =>
                    $"[PersistentCert] Cert token validity window has closed. " +
                    $"token_not_after={tokenNotAfter:u}, now={now:u}. Evicting.");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to parse a DER-encoded <c>GeneralizedTime</c> or <c>UTCTime</c> value from the
        /// raw bytes of the <c>token_not_after</c> X.509 extension (OID <see cref="TokenNotAfterOid"/>).
        /// Returns <see langword="false"/> on any parse failure so callers can apply a safe fallback.
        /// </summary>
        internal static bool TryParseTokenNotAfterExtension(byte[] rawData, out DateTimeOffset result)
        {
            result = default;
            try
            {
                if (rawData is null || rawData.Length < 4)
                    return false;

                int pos = 0;

                // Skip an optional outer SEQUENCE wrapper (0x30 tag).
                if (rawData[pos] == 0x30)
                {
                    pos++; // skip tag
                    if (pos >= rawData.Length) return false;
                    if ((rawData[pos] & 0x80) != 0)
                        pos += 1 + (rawData[pos] & 0x7F); // long-form length
                    else
                        pos++; // short-form length
                }

                if (pos + 2 > rawData.Length) return false;

                byte tag = rawData[pos++];
                if (tag != 0x18 && tag != 0x17) return false; // must be GeneralizedTime or UTCTime

                int len = rawData[pos++];
                if ((len & 0x80) != 0)
                {
                    int extraBytes = len & 0x7F;
                    len = 0;
                    for (int i = 0; i < extraBytes; i++)
                        len = (len << 8) | rawData[pos++];
                }

                if (pos + len > rawData.Length) return false;

                string timeStr = Encoding.ASCII.GetString(rawData, pos, len).TrimEnd('Z');

                // DER GeneralizedTime: YYYYMMDDHHMMSS[.fff]
                if (tag == 0x18)
                {
                    return DateTimeOffset.TryParseExact(
                        timeStr,
                        new[] { "yyyyMMddHHmmss", "yyyyMMddHHmmss.fff" },
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        out result);
                }

                // DER UTCTime: YYMMDDHHMMSS
                return DateTimeOffset.TryParseExact(
                    timeStr,
                    "yyMMddHHmmss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out result);
            }
            catch
            {
                return false;
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
