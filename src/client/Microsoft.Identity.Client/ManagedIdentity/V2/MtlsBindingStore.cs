// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Centralized helper for installing and retrieving mTLS binding certs
    /// from CurrentUser\My with a freshness policy and best-effort pruning.
    /// </summary>
    internal static class MtlsBindingStore
    {
        // Treat certs expiring within this window as "not fresh"
        internal static readonly TimeSpan FreshnessBuffer = TimeSpan.FromMinutes(5);

        internal static string InstallAndGetSubject(X509Certificate2 cert, ILoggerAdapter logger = null)
        {
            if (cert == null)
                return null;

            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);

                // Avoid dup by thumbprint
                var dups = store.Certificates.Find(X509FindType.FindByThumbprint, cert.Thumbprint, validOnly: false);
                foreach (var d in dups)
                { try { store.Remove(d); } catch { } }

                store.Add(cert);
                store.Close();
            }
            catch (Exception ex)
            {
                logger?.Verbose(() => $"[Managed Identity] Failed to install binding cert: {ex.Message}");
            }

            return cert.Subject;
        }

        internal static X509Certificate2 GetFreshestBySubject(string subject, ILoggerAdapter logger = null)
        {
            if (string.IsNullOrWhiteSpace(subject))
                return null;

            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);

                var freshest = store.Certificates
                    .Find(X509FindType.FindBySubjectDistinguishedName, subject, validOnly: false)
                    .Cast<X509Certificate2>()
                    .OrderByDescending(c => c.NotAfter.ToUniversalTime())
                    .FirstOrDefault();

                if (freshest == null)
                    return null;

                // Freshness check (5 minutes)
                if (freshest.NotAfter.ToUniversalTime() <= DateTime.UtcNow.Add(FreshnessBuffer))
                {
                    logger?.Info("[Managed Identity] Found binding in user store, but not fresh; minting new binding.");
                    return null;
                }

                return freshest;
            }
            catch (Exception ex)
            {
                logger?.Verbose(() => $"[Managed Identity] Failed to read binding cert from user store: {ex.Message}");
                return null;
            }
        }

        internal static void PruneOlder(string subject, string keepThumbprint, ILoggerAdapter logger = null)
        {
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(keepThumbprint))
                return;

            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);

                var matches = store.Certificates.Find(
                    X509FindType.FindBySubjectDistinguishedName, subject, validOnly: false);

                foreach (var c in matches.Cast<X509Certificate2>())
                {
                    if (!string.Equals(c.Thumbprint, keepThumbprint, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        { store.Remove(c); }
                        catch { }
                    }
                }
            }
            catch { /* best-effort */ }
        }

        // TEST ONLY — keep in test assembly if you prefer; exposed here for convenience
        internal static void RemoveBySubjectPrefixForTest(string subjectPrefix)
        {
            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);
                foreach (var c in store.Certificates)
                {
                    if (!string.IsNullOrEmpty(c.Subject) &&
                        c.Subject.StartsWith(subjectPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        { store.Remove(c); }
                        catch { }
                    }
                }
            }
            catch { }
        }
    }
}
