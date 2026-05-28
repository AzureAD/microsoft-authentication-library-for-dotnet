// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Best-effort persistence for IMDSv2 mTLS binding certificates in the
    /// <c>CurrentUser\My</c> store on Windows.
    /// </summary>
    /// <remarks>
    /// <para><strong>Selection:</strong></para>
    /// <list type="number">
    ///   <item>
    ///     <description>Filter by FriendlyName: <c>MSAL|alias=&lt;cacheKey&gt;|ep=&lt;base&gt;</c>.</description>
    ///   </item>
    ///   <item>
    ///     <description>Require <c>HasPrivateKey</c> and remaining lifetime>=24h; pick newest <c>NotAfter</c>.</description>
    ///   </item>
    ///   <item>
    ///     <description>Canonical <c>client_id</c> is the GUID in the certificate <c>CN</c>.</description>
    ///   </item>
    /// </list>
    /// <para><strong>Notes:</strong> operations are non-throwing and must not block token acquisition.
    /// FriendlyName tagging semantics are Windows-only.</para>
    /// </remarks>
    internal sealed class WindowsPersistentCertificateCache : IPersistentCertificateCache
    {
        public bool Read(string alias, out CertificateCacheValue value, ILoggerAdapter logger)
        {
            return TryRead(alias, logger, MtlsBindingCache.IsCertKeyOrphaned, out value);
        }

        // Overload for testability: accepts an injectable orphan-check delegate.
        public bool TryRead(
            string alias,
            ILoggerAdapter logger,
            Func<X509Certificate2, ILoggerAdapter, bool> isOrphaned,
            out CertificateCacheValue value)
        {
            value = default;

            if (isOrphaned is null)
            {
                throw new ArgumentNullException(nameof(isOrphaned));
            }

            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);

                // Snapshot first to avoid provider quirks ("collection modified" during enumeration).
                var items = SnapshotCertificates(store, logger);

                X509Certificate2 best = null;
                string bestEndpoint = null;
                DateTime bestNotAfter = DateTime.MinValue;
                List<string> orphanedThumbprints = null;

                foreach (var candidate in items)
                {
                    try
                    {
                        if (!MsiCertificateFriendlyNameEncoder.TryDecode(candidate.FriendlyName, out var decodedAlias, out var endpointBase))
                        {
                            continue;
                        }

                        if (!StringComparer.Ordinal.Equals(decodedAlias, alias))
                        {
                            continue;
                        }

                        // ≥ 24h remaining
                        if (candidate.NotAfter.ToUniversalTime() <= DateTime.UtcNow + CertificateCacheEntry.MinRemainingLifetime)
                        {
                            continue;
                        }

                        // Defensive read-time check: only usable entries
                        if (!candidate.HasPrivateKey)
                        {
                            logger.Verbose(() => "[PersistentCert] Candidate skipped at read: no private key.");
                            continue;
                        }

                        // Skip certs whose CNG container key no longer matches the cert's public key.
                        // This detects orphaned certs left on disk after a reboot regenerates the KG per-boot key.
                        // Collect thumbprints for post-loop removal so the store is only opened once for writes.
                        if (isOrphaned(candidate, logger))
                        {
                            logger.Verbose(() => "[PersistentCert] Candidate skipped: CNG container key does not match cert public key (orphaned post-reboot).");
                            orphanedThumbprints ??= new List<string>();
                            orphanedThumbprints.Add(candidate.Thumbprint);
                            continue;
                        }

                        if (candidate.NotAfter > bestNotAfter)
                        {
                            best?.Dispose();
                            best = new X509Certificate2(candidate); // caller-owned clone (preserves private key link)
                            bestEndpoint = endpointBase;
                            bestNotAfter = candidate.NotAfter;
                        }
                    }
                    finally
                    {
                        candidate.Dispose();
                    }
                }

                // Remove any orphaned certs discovered during the scan.
                if (orphanedThumbprints is { Count: > 0 })
                {
                    RemoveByThumbprints(alias, orphanedThumbprints, logger);
                }

                if (best != null)
                {
                    // CN (GUID) → canonical client_id
                    string cn = null;
                    try
                    {
                        cn = best.GetNameInfo(X509NameType.SimpleName, false);
                    }
                    catch (Exception ex)
                    {
                        logger.Verbose(() => "[PersistentCert] Failed to read CN from selected certificate: " + ex.Message);
                    }

                    if (!Guid.TryParse(cn, out var clientIdGuid))
                    {
                        best.Dispose();
                        logger.Verbose(() => "[PersistentCert] Selected entry CN is not a GUID; skipping.");
                        return false;
                    }

                    value = new CertificateCacheValue(best, bestEndpoint, clientIdGuid.ToString("D"));
                    logger.Info(() => "[PersistentCert] Reused certificate from CurrentUser/My.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.Verbose(() => "[PersistentCert] Store lookup failed: " + ex.Message);
            }

            return false;
        }

        private static void RemoveByThumbprints(string alias, List<string> thumbprints, ILoggerAdapter logger)
        {
            var thumbprintSet = new HashSet<string>(thumbprints, StringComparer.OrdinalIgnoreCase);

            InterprocessLock.TryWithAliasLock(
                alias,
                timeout: TimeSpan.FromMilliseconds(300),
                action: () =>
                {
                    try
                    {
                        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                        store.Open(OpenFlags.ReadWrite);

                        var items = SnapshotCertificates(store, logger);
                        int removed = 0;

                        foreach (var cert in items)
                        {
                            try
                            {
                                if (thumbprintSet.Contains(cert.Thumbprint))
                                {
                                    store.Remove(cert);
                                    removed++;
                                }
                            }
                            finally
                            {
                                cert.Dispose();
                            }
                        }

                        logger.Verbose(() => $"[PersistentCert] Removed {removed} orphaned cert(s) for alias '{alias}'.");
                    }
                    catch (Exception ex)
                    {
                        logger.Verbose(() => "[PersistentCert] Orphan removal failed (best-effort): " + ex.Message);
                    }
                },
                logVerbose: s => logger.Verbose(() => s));
        }

        public void Write(string alias, X509Certificate2 cert, string endpointBase, ILoggerAdapter logger)
        {
            if (cert == null)
                return;

            // IMDSv2 attaches the private key earlier (will throw if it cannot).
            // We do not block here; log defensively if we see a public-only cert.
            if (!cert.HasPrivateKey)
            {
                logger.Verbose(() => "[PersistentCert] Unexpected: Write() received a cert without a private key. Continuing best-effort.");
            }

            if (!MsiCertificateFriendlyNameEncoder.TryEncode(alias, endpointBase, out var friendlyName))
            {
                logger.Verbose(() => "[PersistentCert] FriendlyName encode failed; skipping persist.");
                return;
            }

            // Best-effort: short, non-configurable timeout. We intentionally do not retry here:
            // if the lock is busy we skip persistence and fall back to in-memory cache only,
            // so token acquisition is never blocked on certificate store operations.
            InterprocessLock.TryWithAliasLock(
                alias,
                timeout: TimeSpan.FromMilliseconds(300),
                action: () =>
                {
                    try
                    {
                        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                        store.Open(OpenFlags.ReadWrite);

                        var nowUtc = DateTime.UtcNow;
                        var newNotAfterUtc = cert.NotAfter.ToUniversalTime();

                        // Skip write if a newer/equal, non-expired binding for this alias already exists.
                        DateTime newestForAliasUtc = DateTime.MinValue;

                        var present = SnapshotCertificates(store, logger);

                        foreach (var existing in present)
                        {
                            try
                            {
                                if (!MsiCertificateFriendlyNameEncoder.TryDecode(existing.FriendlyName, out var existingAlias, out _))
                                {
                                    continue;
                                }

                                if (!StringComparer.Ordinal.Equals(existingAlias, alias))
                                {
                                    continue;
                                }

                                var existingNotAfterUtc = existing.NotAfter.ToUniversalTime();
                                if (existingNotAfterUtc > newestForAliasUtc)
                                {
                                    newestForAliasUtc = existingNotAfterUtc;
                                }
                            }
                            finally
                            {
                                existing.Dispose();
                            }
                        }

                        if (newestForAliasUtc != DateTime.MinValue &&
                            newestForAliasUtc >= newNotAfterUtc &&
                            newestForAliasUtc > nowUtc)
                        {
                            logger.Verbose(() => "[PersistentCert] Newer/equal cert already present; skipping add.");
                            return;
                        }

                        try
                        {
                            try
                            {
                                cert.FriendlyName = friendlyName;
                            }
                            catch (Exception exSet)
                            {
                                logger.Verbose(() => "[PersistentCert] Could not set FriendlyName; skipping persist. " + exSet.Message);
                                return;
                            }

                            // Add the original instance (carries private key if present)
                            store.Add(cert);
                            logger.Info(() => "[PersistentCert] Persisted certificate to CurrentUser/My.");

                            // Conservative cleanup: remove expired entries for this alias only
                            PruneExpiredForAlias(store, alias, nowUtc, logger);
                        }
                        catch (Exception exAdd)
                        {
                            logger.Verbose(() => "[PersistentCert] Persist failed: " + exAdd.Message);
                        }
                    }
                    catch (Exception exOuter)
                    {
                        logger.Verbose(() => "[PersistentCert] Persist failed: " + exOuter.Message);
                    }
                },
                logVerbose: s => logger.Verbose(() => s));
        }

        public void Delete(string alias, ILoggerAdapter logger)
        {
            // Best-effort: short, non-configurable timeout. We intentionally do not retry here:
            // if the lock is busy we skip persistence and fall back to in-memory cache only,
            // so token acquisition is never blocked on certificate store operations.
            InterprocessLock.TryWithAliasLock(
                alias,
                timeout: TimeSpan.FromMilliseconds(300),
                action: () =>
                {
                    try
                    {
                        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                        store.Open(OpenFlags.ReadWrite);
                        PruneExpiredForAlias(store, alias, DateTime.UtcNow, logger);
                    }
                    catch (Exception ex)
                    {
                        logger.Verbose(() => "[PersistentCert] Delete (prune) failed: " + ex.Message);
                    }
                },
                logVerbose: s => logger.Verbose(() => s));
        }

        public void DeleteAllForAlias(string alias, ILoggerAdapter logger)
        {
            // Best-effort: short, non-configurable timeout.
            InterprocessLock.TryWithAliasLock(
                alias,
                timeout: TimeSpan.FromMilliseconds(300),
                action: () =>
                {
                    try
                    {
                        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                        store.Open(OpenFlags.ReadWrite);

                        var items = SnapshotCertificates(store, logger);
                        int removed = 0;

                        foreach (var existing in items)
                        {
                            try
                            {
                                if (!MsiCertificateFriendlyNameEncoder.TryDecode(existing.FriendlyName, out var decodedAlias, out _))
                                    continue;
                                if (!StringComparer.Ordinal.Equals(decodedAlias, alias))
                                    continue;

                                // Delete ALL certs for this alias
                                store.Remove(existing);
                                removed++;
                                logger?.Verbose(() => $"[PersistentCert] Deleted certificate from store for alias '{alias}'");
                            }
                            finally
                            {
                                existing.Dispose();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Verbose(() => "[PersistentCert] DeleteAllForAlias failed: " + ex.Message);
                    }
                },
                logVerbose: s => logger.Verbose(() => s));
        }

        /// <summary>
        /// Safely snapshots all certificates in <paramref name="store"/> into an array.
        /// Falls back to LINQ enumeration if <c>CopyTo</c> throws (some providers don't support it).
        /// </summary>
        private static X509Certificate2[] SnapshotCertificates(X509Store store, ILoggerAdapter logger)
        {
            try
            {
                var items = new X509Certificate2[store.Certificates.Count];
                store.Certificates.CopyTo(items, 0);
                return items;
            }
            catch (Exception ex)
            {
                logger.Verbose(() => "[PersistentCert] Store snapshot via CopyTo failed; falling back to enumeration. Details: " + ex.Message);
                return store.Certificates.Cast<X509Certificate2>().ToArray();
            }
        }

        /// <summary>
        /// Deletes only certificates that are actually expired (NotAfter &lt; nowUtc),
        /// scoped to the given alias (cache key) via FriendlyName.
        /// </summary>
        private static void PruneExpiredForAlias(
            X509Store store,
            string aliasCacheKey,
            DateTime nowUtc,
            ILoggerAdapter logger)
        {
            var items = SnapshotCertificates(store, logger);
            int removed = 0;

            foreach (var existing in items)
            {
                try
                {
                    if (!MsiCertificateFriendlyNameEncoder.TryDecode(existing.FriendlyName, out var alias, out _))
                        continue;
                    if (!StringComparer.Ordinal.Equals(alias, aliasCacheKey))
                        continue;

                    if (existing.NotAfter.ToUniversalTime() <= nowUtc)
                    {
                        store.Remove(existing);
                        removed++;
                    }
                }
                finally
                {
                    existing.Dispose();
                }
            }

            logger.Verbose(() => "[PersistentCert] PruneExpired completed for alias '" + aliasCacheKey + "'. Removed=" + removed + ".");
        }
    }
}
