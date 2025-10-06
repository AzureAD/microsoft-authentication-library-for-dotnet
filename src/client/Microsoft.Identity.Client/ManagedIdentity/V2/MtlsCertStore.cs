// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    internal partial class ImdsV2ManagedIdentitySource
    {
        internal static class MtlsCertStore
        {
            // Store in CurrentUser\My
            public static string InstallAndGetSubject(X509Certificate2 cert)
            {
                if (cert == null)
                    return null;

                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadWrite);

                // De‑dup by thumbprint (best effort)
                var dupes = store.Certificates.Find(X509FindType.FindByThumbprint, cert.Thumbprint, false);
                foreach (var existing in dupes)
                {
                    try
                    { store.Remove(existing); }
                    catch { /* best effort */ }
                }

                store.Add(cert);
                store.Close();

                return cert.Subject; // canonical lookup key for ease
            }

            /// <summary>
            /// Return the newest (by NotAfter) certificate for this exact subject DN.
            /// Optionally removes older matches (best effort).
            /// </summary>
            public static X509Certificate2 FindFreshestBySubject(string subject, bool cleanupOlder = true)
            {
                if (string.IsNullOrWhiteSpace(subject))
                    return null;

                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(cleanupOlder ? OpenFlags.ReadWrite : OpenFlags.ReadOnly);

                var matches = store.Certificates.Find(
                    X509FindType.FindBySubjectDistinguishedName,
                    subject,
                    validOnly: false);

                if (matches == null || matches.Count == 0)
                {
                    store.Close();
                    return null;
                }

                var freshest = matches.OfType<X509Certificate2>()
                                      .OrderBy(c => c.NotAfter)
                                      .Last();

                if (cleanupOlder)
                {
                    foreach (var c in matches)
                    {
                        if (!ReferenceEquals(c, freshest))
                        {
                            try
                            { store.Remove(c); }
                            catch { /* best effort */ }
                        }
                    }
                }

                store.Close();
                return freshest;
            }

            /// <summary>
            /// True if cert is currently valid (not expired).
            /// </summary>
            public static bool IsCurrentlyValid(X509Certificate2 cert)
            {
                if (cert == null)
                    return false;
                return DateTime.UtcNow < cert.NotAfter.ToUniversalTime();
            }

            /// <summary>
            /// True if we are at or past half of the certificate lifetime window.
            /// </summary>
            public static bool IsBeyondHalfLife(X509Certificate2 cert)
            {
                if (cert == null)
                    return false;

                var nb = cert.NotBefore.ToUniversalTime();
                var na = cert.NotAfter.ToUniversalTime();

                // Defensive: zero/negative lifetime => treat as beyond half-life
                if (na <= nb)
                    return true;

                var halfLife = nb + TimeSpan.FromTicks((na - nb).Ticks / 2);
                return DateTime.UtcNow >= halfLife;
            }

            /// <summary>
            /// Best‑effort removal of all certs matching a subject DN (used by tests or rotation cleanup).
            /// </summary>
            public static void RemoveAllBySubject(string subject)
            {
                if (string.IsNullOrWhiteSpace(subject))
                    return;

                try
                {
                    using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                    store.Open(OpenFlags.ReadWrite);

                    var matches = store.Certificates.Find(
                        X509FindType.FindBySubjectDistinguishedName,
                        subject,
                        validOnly: false);

                    foreach (var c in matches)
                    {
                        try
                        { store.Remove(c); }
                        catch { /* best effort */ }
                    }

                    store.Close();
                }
                catch { /* best effort */ }
            }
        }
    }
}
