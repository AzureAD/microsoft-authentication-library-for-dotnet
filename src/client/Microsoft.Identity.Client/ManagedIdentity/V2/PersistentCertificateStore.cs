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
    /// Best-effort persistence for IMDSv2 mTLS binding certs in CurrentUser\My.
    ///
    /// Selection:
    /// 1) Filter by FriendlyName "MSAL|alias=&lt;cacheKey&gt;|ep=&lt;base&gt;" (scopes to *our* alias).
    /// 2) Enforce remaining lifetime (≥ 24h) and require HasPrivateKey; pick newest NotAfter.
    /// 3) On that single winner, read Subject CN and require a GUID (canonical client_id).
    ///
    /// Notes:
    /// - We never reattach private keys. If HasPrivateKey == false, caller will mint.
    /// - No throws; persistence must not block token acquisition.
    /// - Windows-only; FriendlyName semantics are undefined elsewhere.
    /// </summary>
    internal static class PersistentCertificateStore
    {
        public static bool TryFind(
            string aliasCacheKey,
            out CertificateCacheValue value,
            ILoggerAdapter logger = null)
        {
            value = default;

            if (!DesktopOsHelper.IsWindows())
                return false;

            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);

                // Snapshot to avoid "collection modified" during enumeration on some providers
                X509Certificate2[] items;
                try
                {
                    items = new X509Certificate2[store.Certificates.Count];
                    store.Certificates.CopyTo(items, 0);
                }
                catch
                {
                    items = store.Certificates.Cast<X509Certificate2>().ToArray();
                }

                X509Certificate2 best = null;
                string bestEndpoint = null;
                DateTime bestNotAfter = DateTime.MinValue;

                foreach (var c in items)
                {
                    try
                    {
                        if (!FriendlyNameCodec.TryDecode(c.FriendlyName, out var alias, out var epBase))
                            continue;
                        if (!StringComparer.Ordinal.Equals(alias, aliasCacheKey))
                            continue;

                        // 24h+ remaining
                        if (c.NotAfter.ToUniversalTime() <= DateTime.UtcNow + CertificateCacheEntry.MinRemainingLifetime)
                            continue;

                        if (!c.HasPrivateKey)
                        {
                            logger?.Verbose(() => "[PersistentCert] Candidate skipped: no private key.");
                            continue;
                        }

                        if (c.NotAfter > bestNotAfter)
                        {
                            best?.Dispose();
                            best = new X509Certificate2(c); // caller-owned clone (preserves private key link)
                            bestEndpoint = epBase;
                            bestNotAfter = c.NotAfter;
                        }
                    }
                    finally
                    {
                        c.Dispose();
                    }
                }

                if (best != null)
                {
                    // CN (GUID) → canonical client_id
                    string cn = null;
                    try
                    { cn = best.GetNameInfo(X509NameType.SimpleName, false); }
                    catch { }
                    if (!Guid.TryParse(cn, out var g))
                    {
                        best.Dispose();
                        logger?.Verbose(() => "[PersistentCert] Selected entry CN is not a GUID; skipping.");
                        return false;
                    }

                    value = new CertificateCacheValue(best, bestEndpoint, g.ToString("D"));
                    logger?.Verbose(() => "[PersistentCert] Reused certificate from CurrentUser/My.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger?.Verbose(() => "[PersistentCert] Store lookup failed: " + ex.Message);
            }

            return false;
        }

        public static void TryPersist(
            string aliasCacheKey,
            X509Certificate2 cert,
            string endpointBase,
            string clientId,           // unused; CN is the source of truth on read
            ILoggerAdapter logger = null)
        {
            if (!DesktopOsHelper.IsWindows() || cert == null)
                return;

            // We only persist certs that can actually be used for mTLS later.
            if (!cert.HasPrivateKey)
            {
                logger?.Verbose(() => "[PersistentCert] Not persisting: certificate has no private key.");
                return;
            }

            if (!FriendlyNameCodec.TryEncode(aliasCacheKey, endpointBase, out var friendlyName))
            {
                logger?.Verbose(() => "[PersistentCert] FriendlyName encode failed; skipping persist.");
                return;
            }

            // Best-effort: short lock, skip if busy
            InterprocessLock.TryWithAliasLock(
                aliasCacheKey,
                timeout: TimeSpan.FromMilliseconds(300),
                action: () =>
                {
                    try
                    {
                        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                        store.Open(OpenFlags.ReadWrite);

                        var nowUtc = DateTime.UtcNow;
                        var newNotAfterUtc = cert.NotAfter.ToUniversalTime();

                        // If a newer (or equal) non-expired entry for this alias already exists, skip.
                        DateTime newestForAliasUtc = DateTime.MinValue;

                        X509Certificate2[] present;
                        try
                        {
                            present = new X509Certificate2[store.Certificates.Count];
                            store.Certificates.CopyTo(present, 0);
                        }
                        catch
                        {
                            present = store.Certificates.Cast<X509Certificate2>().ToArray();
                        }

                        foreach (var existing in present)
                        {
                            try
                            {
                                if (!FriendlyNameCodec.TryDecode(existing.FriendlyName, out var a, out _))
                                    continue;
                                if (!StringComparer.Ordinal.Equals(a, aliasCacheKey))
                                    continue;

                                var existUtc = existing.NotAfter.ToUniversalTime();
                                if (existUtc > newestForAliasUtc)
                                {
                                    newestForAliasUtc = existUtc;
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
                            logger?.Verbose(() => "[PersistentCert] Newer/equal cert already present; skipping add.");
                            return;
                        }

                        // === CHANGE: set FriendlyName BEFORE add, and add the ORIGINAL instance that has the private key ===
                        try
                        {
                            try
                            {
                                cert.FriendlyName = friendlyName;
                            }
                            catch
                            {
                                logger?.Verbose(() => "[PersistentCert] Could not set FriendlyName; skipping persist.");
                                return;
                            }

                            // Add the original instance (carries private key)
                            store.Add(cert);

                            logger?.Verbose(() => "[PersistentCert] Persisted certificate to CurrentUser/My.");

                            // Conservative cleanup: remove expired entries for this alias only
                            StorePruner.PruneExpiredForAlias(store, aliasCacheKey, nowUtc, logger);
                        }
                        catch (Exception ex)
                        {
                            logger?.Verbose(() => "[PersistentCert] Persist failed: " + ex.Message);
                        }
                    }
                    catch (Exception exOuter)
                    {
                        logger?.Verbose(() => "[PersistentCert] Persist failed: " + exOuter.Message);
                    }
                },
                logVerbose: s => logger?.Verbose(() => s));
        }

        public static void TryPruneAliasOlderThan(
            string aliasCacheKey,
            DateTimeOffset baselineNotAfterUtc, // kept for API stability; we prune expired only
            ILoggerAdapter logger = null)
        {
            if (!DesktopOsHelper.IsWindows())
                return;

            InterprocessLock.TryWithAliasLock(
                aliasCacheKey,
                timeout: TimeSpan.FromMilliseconds(300),
                action: () =>
                {
                    try
                    {
                        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                        store.Open(OpenFlags.ReadWrite);
                        StorePruner.PruneExpiredForAlias(store, aliasCacheKey, DateTime.UtcNow, logger);
                    }
                    catch (Exception ex)
                    {
                        logger?.Verbose(() => "[PersistentCert] Prune failed: " + ex.Message);
                    }
                },
                logVerbose: s => logger?.Verbose(() => s));
        }
    }
}
