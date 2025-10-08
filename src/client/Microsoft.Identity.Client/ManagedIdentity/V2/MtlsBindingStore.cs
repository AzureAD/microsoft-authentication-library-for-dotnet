// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    /// <summary>
    /// Installs/locates/prunes binding certificates in CurrentUser\My,
    /// plus freshness/half-life logic. 
    /// To-Do : use expires_on from the token response to determine freshness.
    /// IMDS team will be adding this value in the future.
    /// </summary>
    internal static class MtlsBindingStore
    {
        // Certs expiring within this window are considered “not fresh”
        internal static readonly TimeSpan FreshnessBuffer = TimeSpan.FromMinutes(5);

        internal static string InstallAndGetSubject(X509Certificate2 cert, ILoggerAdapter logger = null)
        {
            if (cert == null)
                return null;

            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);

                var dups = store.Certificates.Find(X509FindType.FindByThumbprint, cert.Thumbprint, validOnly: false);
                foreach (var d in dups)
                { try { store.Remove(d); } catch { } }

                store.Add(cert);
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

                // Freshness = must be > now + buffer
                if (freshest.NotAfter.ToUniversalTime() <= DateTime.UtcNow.Add(FreshnessBuffer))
                {
                    logger?.Info("[Managed Identity] Found binding in user store, but not fresh; minting new binding.");
                    return null;
                }

                return freshest;
            }
            catch (Exception ex)
            {
                logger?.Verbose(() => $"[Managed Identity] Failed to read binding cert: {ex.Message}");
                return null;
            }
        }

        internal static X509Certificate2 FindByThumbprint(string thumbprint)
        {
            if (string.IsNullOrWhiteSpace(thumbprint))
                return null;

            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            var res = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
            return res.Count > 0 ? res[0] : null;
        }

        internal static X509Certificate2 ResolveByThumbprintThenSubject(
            string thumbprint,
            string subject,
            bool cleanupOlder,
            out string resolvedThumbprint)
        {
            resolvedThumbprint = null;

            var exact = FindByThumbprint(thumbprint);
            if (IsCurrentlyValid(exact))
            {
                resolvedThumbprint = exact.Thumbprint;
                return exact;
            }

            var freshest = GetFreshestBySubject(subject);
            if (IsCurrentlyValid(freshest))
            {
                resolvedThumbprint = freshest.Thumbprint;

                if (cleanupOlder && !string.IsNullOrWhiteSpace(subject))
                {
                    PruneOlder(subject, freshest.Thumbprint);
                }

                return freshest;
            }

            return null;
        }

        internal static bool IsCurrentlyValid(X509Certificate2 cert)
            => cert != null && DateTime.UtcNow < cert.NotAfter.ToUniversalTime();

        internal static bool IsBeyondHalfLife(X509Certificate2 cert)
        {
            if (cert == null)
                return false;
            var nb = cert.NotBefore.ToUniversalTime();
            var na = cert.NotAfter.ToUniversalTime();
            if (na <= nb)
                return true; // defensive

            var halfLife = nb + TimeSpan.FromTicks((na - nb).Ticks / 2);
            return DateTime.UtcNow >= halfLife;
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
            catch
            {
                // best effort
            }
        }

        // Test-only helpers if you need them
        internal static void RemoveAllBySubject(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject))
                return;
            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);
                var matches = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, subject, false);
                foreach (var c in matches)
                { try { store.Remove(c); } catch { } }
            }
            catch { }
        }

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
