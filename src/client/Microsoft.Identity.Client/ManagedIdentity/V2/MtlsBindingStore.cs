// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Centralized helper for installing, locating and pruning IMDSv2 client mTLS binding certs
    /// from the CurrentUser\My store.
    /// </summary>
    internal static class MtlsBindingStore
    {
        // Minimum remaining lifetime required to reuse a binding.
        internal static readonly TimeSpan MinFreshRemaining = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Installs the certificate in CurrentUser\My, removes any exact-duplicate thumbprints,
        /// and best-effort prunes older certs for the same subject. Returns the subject DN used
        /// as lookup key.
        /// </summary>
        internal static string InstallAndGetSubject(X509Certificate2 cert, ILoggerAdapter logger = null)
        {
            if (cert == null)
                return null;

            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);

            try
            {
                store.Open(OpenFlags.ReadWrite);

                // Avoid duplicates by thumbprint
                foreach (var dup in store.Certificates.Find(X509FindType.FindByThumbprint, cert.Thumbprint, validOnly: false))
                {
                    try
                    { store.Remove(dup); }
                    catch { /* best effort */ }
                }

                store.Add(cert);

                // Best effort prune for same subject (keep newly added one)
                TryPruneOlderForSubject(store, cert.Subject, keepThumbprint: cert.Thumbprint, logger);
            }
            catch
            {
                // If store operations fail, carry on; the cert is still available in-memory for this call.
            }
            finally
            {
                try
                { store.Close(); }
                catch { }
            }

            return cert.Subject;
        }

        /// <summary>
        /// Returns the freshest (max NotAfter) cert for the given subject that still has at least
        /// <paramref name="minFreshRemaining"/> lifetime. Best-effort prunes older certs for the same subject.
        /// Returns null if nothing qualifies.
        /// </summary>
        internal static X509Certificate2 GetFreshestBySubject(
            string subject,
            TimeSpan minFreshRemaining,
            ILoggerAdapter logger = null)
        {
            if (string.IsNullOrWhiteSpace(subject))
                return null;

            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            bool rw = true;
            try
            {
                // Try RW to allow pruning; fall back to RO if needed.
                store.Open(OpenFlags.ReadWrite);
            }
            catch
            {
                rw = false;
                try
                { store.Open(OpenFlags.ReadOnly); }
                catch { return null; }
            }

            try
            {
                var all = store.Certificates
                               .Find(X509FindType.FindBySubjectDistinguishedName, subject, validOnly: false)
                               .OfType<X509Certificate2>()
                               .ToList();

                if (all.Count == 0)
                {
                    return null;
                }

                var freshest = all.OrderByDescending(c => c.NotAfter).First();

                // Best effort prune older ones (only if RW)
                if (rw)
                {
                    foreach (var c in all)
                    {
                        if (!string.Equals(c.Thumbprint, freshest.Thumbprint, StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            { store.Remove(c); }
                            catch { /* best effort */ }
                        }
                    }
                }

                var remaining = freshest.NotAfter.ToUniversalTime() - DateTime.UtcNow;
                if (remaining <= minFreshRemaining)
                {
                    // Treat as non-usable -> force mint
                    return null;
                }

                return freshest;
            }
            finally
            {
                try
                { store.Close(); }
                catch { }
            }
        }

        /// <summary>
        /// Test-only utility to scrub all certs whose subject starts with the prefix.
        /// </summary>
        internal static void RemoveBySubjectPrefixForTest(string subjectPrefix)
        {
            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);
                foreach (var c in store.Certificates.OfType<X509Certificate2>())
                {
                    if (c.Subject?.StartsWith(subjectPrefix, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        try
                        { store.Remove(c); }
                        catch { /* best effort */ }
                    }
                }
                store.Close();
            }
            catch { /* best effort */ }
        }

        private static void TryPruneOlderForSubject(
            X509Store store,
            string subject,
            string keepThumbprint,
            ILoggerAdapter logger)
        {
            try
            {
                var toRemove = store.Certificates
                    .Find(X509FindType.FindBySubjectDistinguishedName, subject, validOnly: false)
                    .OfType<X509Certificate2>()
                    .Where(c => !string.Equals(c.Thumbprint, keepThumbprint, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var c in toRemove)
                {
                    try
                    { store.Remove(c); }
                    catch { /* best effort */ }
                }
            }
            catch { /* best effort */ }
        }
    }
}
