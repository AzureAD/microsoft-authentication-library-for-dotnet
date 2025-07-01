// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Client.Internal.Utilities
{
    // (Optionally, if you need to expose this for partners, you may change the visibility to public.)
    internal static class CertificateHelper
    {
        private const string CertificateName = "devicecert.mtlsauth.local";
        private static readonly object s_lock = new object();
        private static X509Certificate2 s_cachedCertificate;
        // Flag indicates whether the cached certificate was created in memory.
        private static bool s_isInMemoryCertificate;

        /// <summary>
        /// Event triggered when a new certificate is created.
        /// </summary>
        internal static event Action<X509Certificate2> CertificateUpdated;

        /// <summary>
        /// Retrieves the certificate from a static cache (or store) or creates one if it does not exist.
        /// When <paramref name="forceUpdate"/> is true, and if the certificate was created in memory,
        /// a new certificate is generated regardless of its remaining lifetime.
        /// For platform certificates (from the store), forceUpdate is ignored.
        /// Otherwise, if the certificate is in memory and its remaining lifetime is less than 5 days, it is renewed.
        /// </summary>
        /// <param name="forceUpdate">If true, forces renewal of an in-memory certificate.</param>
        /// <returns>An X509Certificate2 that is valid for 90 days.</returns>
        public static X509Certificate2 GetOrCreateCertificate(bool forceUpdate = false)
        {
            lock (s_lock)
            {
                // If no certificate is cached, try to retrieve it from the store.
                if (s_cachedCertificate == null)
                {
                    s_cachedCertificate = GetCertificateFromStore(CertificateName);
                    if (s_cachedCertificate != null)
                    {
                        s_isInMemoryCertificate = false; // Platform certificate.
                    }
                }

                // If we have an in-memory certificate...
                if (s_cachedCertificate != null && s_isInMemoryCertificate)
                {
                    if (forceUpdate)
                    {
                        // Force update always renews the in-memory certificate.
                        s_cachedCertificate = CreateNewCertificate(CertificateName);
                        CertificateUpdated?.Invoke(s_cachedCertificate);
                    }
                    else
                    {
                        // Otherwise, renew only if less than 5 days remain.
                        TimeSpan timeLeft = s_cachedCertificate.NotAfter - DateTimeOffset.Now;
                        if (timeLeft < TimeSpan.FromDays(5))
                        {
                            s_cachedCertificate = CreateNewCertificate(CertificateName);
                            CertificateUpdated?.Invoke(s_cachedCertificate);
                        }
                    }
                }

                // If no certificate was found in the store, create an in-memory certificate.
                if (s_cachedCertificate == null)
                {
                    s_cachedCertificate = CreateNewCertificate(CertificateName);
                    s_isInMemoryCertificate = true;
                    CertificateUpdated?.Invoke(s_cachedCertificate);
                }

                return s_cachedCertificate;
            }
        }

        /// <summary>
        /// Convenience method to force update the in-memory certificate.
        /// For platform certificates, this is a no-op.
        /// </summary>
        /// <returns>The (possibly renewed) certificate.</returns>
        public static X509Certificate2 ForceUpdateInMemoryCertificate()
        {
            return GetOrCreateCertificate(forceUpdate: true);
        }

        /// <summary>
        /// Retrieves a certificate from the store using the certificate's subject name.
        /// </summary>
        /// <param name="certificateName">The subject name of the certificate.</param>
        /// <returns>The certificate if found; otherwise, null.</returns>
        private static X509Certificate2 GetCertificateFromStore(string certificateName)
        {
            // Search in the LocalMachine store.
            using (var store = new X509Store(StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certificates = store.Certificates.Find(
                    X509FindType.FindBySubjectName, certificateName, validOnly: false);
                if (certificates.Count > 0)
                {
                    return certificates[0];
                }
            }

            // Search in the CurrentUser store.
            using (var store = new X509Store(StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certificates = store.Certificates.Find(
                    X509FindType.FindBySubjectName, certificateName, validOnly: false);
                if (certificates.Count > 0)
                {
                    return certificates[0];
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a new self-signed certificate with a validity of 90 days.
        /// </summary>
        /// <param name="certificateName">The subject name to use for the certificate.</param>
        /// <returns>A newly created certificate.</returns>
        private static X509Certificate2 CreateNewCertificate(string certificateName)
        {
#if SUPPORTS_MTLS
            // Create an RSA key pair
            using (var rsa = RSA.Create(2048))
            {
                // Create a certificate request.
                var req = new CertificateRequest(
                    $"CN={certificateName}",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                // Add necessary extensions.
                req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
                req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
                    new OidCollection { new Oid("1.3.6.1.5.5.7.3.2") }, true));
                req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));

                // Create the certificate valid for 7 days.
                X509Certificate2 cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(7));

                // Export and re-import to ensure the private key is stored properly.
                var certWithPrivateKey = new X509Certificate2(
                    cert.Export(X509ContentType.Pfx),
                    (string)null,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

                return certWithPrivateKey;
            }
#else
            return null;
#endif
        }
    }
}
