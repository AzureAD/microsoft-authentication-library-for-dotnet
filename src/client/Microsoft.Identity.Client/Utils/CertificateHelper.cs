// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using System;
using System.Security.Cryptography;

namespace Microsoft.Identity.Client.Internal.Utilities
{
    internal static class CertificateHelper
    {
        /// <summary>
        /// Method to get certificate from the store or create if it does not exist using certificate name
        /// </summary>
        /// <param name="certificateName"></param>
        /// <returns></returns>
        public static X509Certificate2 GetOrCreateCertificate(string certificateName)
        {
            X509Certificate2 cert = GetCertificateFromStore(certificateName);

            cert ??= CreateNewCertificate(certificateName);

            return cert;
        }

        /// <summary>
        /// Method to retrieve certificate from the store using the certificate's subject name
        /// </summary>
        /// <param name="certificateName"></param>
        /// <returns></returns>
        private static X509Certificate2 GetCertificateFromStore(string certificateName)
        {
            // First, search in the LocalMachine store
            using (var store = new X509Store(StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);

                X509Certificate2Collection certificates = store.Certificates.Find
                    (X509FindType.FindBySubjectName, certificateName, validOnly: false);

                if (certificates.Count > 0)
                {
                    return certificates[0];
                }
            }

            // If not found in LocalMachine, search in the CurrentUser store
            // This will help with App Service scenarios where the certificate is installed in the CurrentUser store
            using (var store = new X509Store(StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);

                X509Certificate2Collection certificates = store.Certificates.Find
                    (X509FindType.FindBySubjectName, certificateName, validOnly: false);

                if (certificates.Count > 0)
                {
                    return certificates[0];
                }
            }

            // If the certificate is not found in either store, return null
            return null;
        }

        private static X509Certificate2 CreateNewCertificate(string certificateName)
        {
#if SUPPORTS_MTLS
            // Create an RSA key pair
            // The test machine is throwing an exception when trying to create an ECD key pair
            // For now using RSA key pair
            using (var rsa = RSA.Create(2048)) // Generate a 2048-bit RSA key
            {
                // Create a certificate request using RSA
                var req = new CertificateRequest($"CN={certificateName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                // Set other certificate properties, extensions, etc.
                // need to review this list and add/remove as needed
                req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false)); // Basic constraints (not a CA)
                req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.2") }, true)); // Client Authentication
                req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true)); // Digital signature usage

                // Create the self-signed certificate with the associated RSA private key
                X509Certificate2 cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(90));

                // Ensure that the private key is stored in a way that SChannel can access it
                var certWithPrivateKey = new X509Certificate2(cert.Export(X509ContentType.Pfx),
                    (string)null, // No password
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

                return certWithPrivateKey;
            }
#else
        return null;
#endif
        }
    }
}
