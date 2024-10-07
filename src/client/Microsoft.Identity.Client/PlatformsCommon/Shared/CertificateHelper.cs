// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Cryptography.X509Certificates;
using System;
using System.Security.Cryptography;

namespace Microsoft.Identity.Client.Internal.Utilities
{
    internal static class CertificateHelper
    {
        // Method to get certificate from the store or create if it does not exist using certificate name
        public static X509Certificate2 GetOrCreateCertificate(string certificateName)
        {
            var cert = GetCertificateFromStore(certificateName);
            if (cert == null)
            {
                cert = CreateNewCertificate(certificateName);
            }
            return cert;
        }

        // Method to retrieve certificate from the store using the certificate's subject name
        private static X509Certificate2 GetCertificateFromStore(string certificateName)
        {
            using (var store = new X509Store(StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);
                var certificates = store.Certificates.Find(X509FindType.FindBySubjectName, certificateName, validOnly: false);
                if (certificates.Count > 0)
                {
                    return certificates[0];
                }
                return null;
            }
        }

        // Method to create a new self-signed certificate if it doesn't exist
        private static X509Certificate2 CreateNewCertificate(string certificateName)
        {
            // Create a self-signed certificate for this purpose.
            var ecdsa = ECDsa.Create(); // generate asymmetric key pair

//To-Do need to move this to platform specific code
#if NET472 || NET6_0
            var req = new CertificateRequest($"CN={certificateName}", ecdsa, HashAlgorithmName.SHA256);
            
            // Specify validity period, extensions, etc.
            var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));

            // Add the cert to the store (optional)
            using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Add(cert);
            }

            return cert;
#else
            // Fallback or alternative code for other frameworks
            throw new PlatformNotSupportedException("This certificate creation is supported only on .NET Framework 4.7.2 and .NET 6.0");
#endif
        }
    }
}
