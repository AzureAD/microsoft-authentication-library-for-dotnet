// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
                // Remove any existing with same thumbprint (avoid dup)
                foreach (var existing in store.Certificates.Find(X509FindType.FindByThumbprint, cert.Thumbprint, false))
                {
                    try
                    { store.Remove(existing); }
                    catch { }
                }
                store.Add(cert);
                store.Close();

                return cert.Subject; // canonical lookup key for ease
            }

            public static X509Certificate2 FindBySubject(string subject)
            {
                if (string.IsNullOrWhiteSpace(subject))
                    return null;

                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);
                var matches = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, subject, false);
                store.Close();

                return matches.Count > 0 ? matches[0] : null;
            }
        }
    }
}
