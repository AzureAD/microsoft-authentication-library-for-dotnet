// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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
            value = default;

            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);

                // Snapshot first to avoid provider quirks ("collection modified" during enumeration).
                X509Certificate2[] items;
                try
                {
                    items = new X509Certificate2[store.Certificates.Count];
                    store.Certificates.CopyTo(items, 0);
                }
                catch (Exception ex)
                {
                    logger.Verbose(() => "[PersistentCert] Store snapshot via CopyTo failed; falling back to enumeration. Details: " + ex.Message);
                    items = store.Certificates.Cast<X509Certificate2>().ToArray();
                }

                X509Certificate2 best = null;
                string bestEndpoint = null;
                DateTime bestNotAfter = DateTime.MinValue;

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

                        X509Certificate2[] present;
                        try
                        {
                            present = new X509Certificate2[store.Certificates.Count];
                            store.Certificates.CopyTo(present, 0);
                        }
                        catch (Exception ex)
                        {
                            logger.Verbose(() => "[PersistentCert] Store snapshot via CopyTo failed; falling back to enumeration. Details: " + ex.Message);
                            present = store.Certificates.Cast<X509Certificate2>().ToArray();
                        }

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

                        X509Certificate2[] items;
                        try
                        {
                            items = new X509Certificate2[store.Certificates.Count];
                            store.Certificates.CopyTo(items, 0);
                        }
                        catch (Exception ex)
                        {
                            logger.Verbose(() => "[PersistentCert] Store snapshot via CopyTo failed; falling back to enumeration. Details: " + ex.Message);
                            items = store.Certificates.Cast<X509Certificate2>().ToArray();
                        }

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
        /// Deletes only certificates that are actually expired (NotAfter &lt; nowUtc),
        /// scoped to the given alias (cache key) via FriendlyName.
        /// </summary>
        private static void PruneExpiredForAlias(
            X509Store store,
            string aliasCacheKey,
            DateTime nowUtc,
            ILoggerAdapter logger)
        {
            X509Certificate2[] items;
            try
            {
                items = new X509Certificate2[store.Certificates.Count];
                // Safe snapshot for providers that throw if removing while iterating
                store.Certificates.CopyTo(items, 0);
            }
            catch (Exception ex)
            {
                logger.Verbose(() => "[PersistentCert] Prune snapshot via CopyTo failed; falling back to enumeration. Details: " + ex.Message);
                items = store.Certificates.Cast<X509Certificate2>().ToArray();
            }

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
