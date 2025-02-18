using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

namespace Microsoft.Identity.Client.Internal.Utilities
{
    internal static class ManagedIdentityCertificateManager
    {
        private const string CertificateName = "devicecert.mtlsauth.local";
        private static readonly object s_lock = new object();
        private static X509Certificate2 s_cachedCertificate;
        private static bool s_isInMemoryCertificate;

        /// <summary>
        /// Event triggered when a new certificate is created.
        /// </summary>
        internal static event Action<X509Certificate2> CertificateUpdated;

        /// <summary>
        /// Main entry point for retrieving a certificate. Splits logic between Linux (always in-memory)
        /// and Windows/macOS (attempt platform store first, else create in-memory).
        /// </summary>
        /// <param name="forceUpdate">If true, forces renewal of an in-memory certificate.</param>
        /// <returns>An X509Certificate2 valid for 90 days.</returns>
        public static X509Certificate2 GetOrCreateCertificate(bool forceUpdate = false)
        {
            lock (s_lock)
            {
                // Determine if running on Linux
                bool isLinux = DesktopOsHelper.IsLinux();

                if (isLinux)
                {
                    return GetOrCreateLinuxCertificate(forceUpdate);
                }
                else
                {
                    return GetOrCreateWindowsCertificate(forceUpdate);
                }
            }
        }

        /// <summary>
        /// Convenience method for Azure SDK to force update the in-memory certificate.
        /// Not for final release. For platform certificates, this is a no-op.
        /// </summary>
        /// <returns>The renewed certificate.</returns>
        public static X509Certificate2 ForceUpdateInMemoryCertificate()
        {
            return GetOrCreateCertificate(forceUpdate: true);
        }

        /// <summary>
        /// Creates a new self-signed certificate valid for 90 days.
        /// Uses ECDSA (NIST P-256 curve).
        /// </summary>
        /// <param name="certificateName">The subject name of the certificate.</param>
        /// <returns>A newly created self-signed certificate.</returns>
        private static X509Certificate2 CreateNewCertificate(string certificateName)
        {
#if SUPPORTS_MTLS
            using (var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256))
            {
                var req = new CertificateRequest($"CN={certificateName}",
                    ecdsa,
                    HashAlgorithmName.SHA256);

                req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
                req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));

                X509Certificate2 cert = req.CreateSelfSigned(
                    DateTimeOffset.Now,
                    DateTimeOffset.Now.AddDays(90));

                bool isWindows = DesktopOsHelper.IsWindows();

                X509KeyStorageFlags flags = isWindows
                    ? X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable
                    : X509KeyStorageFlags.EphemeralKeySet;

                // Export as PFX 
                return new X509Certificate2(
                    cert.Export(X509ContentType.Pfx),
                    (string)null,
                    flags);
            }
#else
            return null;
#endif
        }

        /// <summary>
        /// Retrieves a certificate from the store using the certificate's subject name.
        /// </summary>
        /// <param name="certificateName">The subject name of the certificate.</param>
        /// <returns>The certificate if found, otherwise null.</returns>
        private static X509Certificate2 GetCertificateFromStore(string certificateName)
        {
            // Search in the LocalMachine store.
            using (var store = new X509Store(StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);
                var certificates = store.Certificates.Find(
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
                var certificates = store.Certificates.Find(
                    X509FindType.FindBySubjectName, certificateName, validOnly: false);

                if (certificates.Count > 0)
                {
                    return certificates[0];
                }
            }

            return null;
        }

        /// <summary>
        /// On Windows: Attempt to retrieve from store, then create in-memory cert if none found.
        /// Renews ephemeral cert if nearly expired or forced to update.
        /// </summary>
        private static X509Certificate2 GetOrCreateWindowsCertificate(bool forceUpdate)
        {
            // Try to get from store if not cached
            if (s_cachedCertificate == null)
            {
                s_cachedCertificate = GetCertificateFromStore(CertificateName);

                if (s_cachedCertificate != null)
                {
                    // Found a platform certificate
                    s_isInMemoryCertificate = false;
                }
            }

            // If cached is ephemeral
            if (s_cachedCertificate != null && s_isInMemoryCertificate)
            {
                if (forceUpdate)
                {
                    s_cachedCertificate = CreateNewCertificate(CertificateName);
                    CertificateUpdated?.Invoke(s_cachedCertificate);
                }
                else
                {
                    // Renew if less than 5 days remain
                    TimeSpan timeLeft = s_cachedCertificate.NotAfter - DateTimeOffset.Now;
                    if (timeLeft < TimeSpan.FromDays(5))
                    {
                        s_cachedCertificate = CreateNewCertificate(CertificateName);
                        CertificateUpdated?.Invoke(s_cachedCertificate);
                    }
                }
            }

            // If still no certificate, create ephemeral
            if (s_cachedCertificate == null)
            {
                s_cachedCertificate = CreateNewCertificate(CertificateName);
                s_isInMemoryCertificate = true;
                CertificateUpdated?.Invoke(s_cachedCertificate);
            }

            return s_cachedCertificate;
        }

        /// <summary>
        /// On Linux: Always uses an in-memory certificate.
        /// Renews ephemeral cert if nearly expired or forced to update.
        /// </summary>
        private static X509Certificate2 GetOrCreateLinuxCertificate(bool forceUpdate)
        {
            // If no certificate or forced update, create ephemeral
            if (s_cachedCertificate == null || forceUpdate)
            {
                s_cachedCertificate = CreateNewCertificate(CertificateName);
                s_isInMemoryCertificate = true;
                CertificateUpdated?.Invoke(s_cachedCertificate);
                return s_cachedCertificate;
            }

            // If ephemeral cert is near expiration, renew it
            if (s_isInMemoryCertificate)
            {
                TimeSpan timeLeft = s_cachedCertificate.NotAfter - DateTimeOffset.Now;
                if (timeLeft < TimeSpan.FromDays(5))
                {
                    s_cachedCertificate = CreateNewCertificate(CertificateName);
                    CertificateUpdated?.Invoke(s_cachedCertificate);
                }
                return s_cachedCertificate;
            }

            // Fallback if we somehow have a non-in-memory cert on Linux (unlikely)
            s_cachedCertificate = CreateNewCertificate(CertificateName);
            s_isInMemoryCertificate = true;
            CertificateUpdated?.Invoke(s_cachedCertificate);
            return s_cachedCertificate;
        }
    }
}
