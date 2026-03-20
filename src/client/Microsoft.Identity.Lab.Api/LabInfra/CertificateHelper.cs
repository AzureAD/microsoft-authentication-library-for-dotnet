// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    /// <summary>
    /// Provides helper methods for locating and loading X509 certificates used by lab infrastructure tests.
    /// </summary>
    public static class CertificateHelper
    {
        /// <summary>
        /// Attempts to find a certificate with the specified subject name by searching the
        /// <see cref="StoreName.My"/> store in all available <see cref="StoreLocation"/> values.
        /// </summary>
        /// <param name="subjectName">The certificate subject name to search for.</param>
        /// <returns>
        /// The matching <see cref="X509Certificate2"/> if found; otherwise, <see langword="null"/>.
        /// </returns>
        public static X509Certificate2 FindCertificateByName(string subjectName)
        {
            StoreLocation[] storeLocations = { StoreLocation.CurrentUser,StoreLocation.LocalMachine };

            foreach (StoreLocation storeLocation in storeLocations)
            {
                var certificate = FindCertificateByName(subjectName, storeLocation, StoreName.My);
                if (certificate != null)
                {
                    return certificate;
                }
            }

            return null;
        }

        /// <summary>
        /// Attempts to find a certificate with the specified subject name in the given
        /// certificate store location and store name.
        /// </summary>
        /// <param name="certName">The certificate subject name to search for.</param>
        /// <param name="location">The <see cref="StoreLocation"/> to search in.</param>
        /// <param name="name">The <see cref="StoreName"/> to search in.</param>
        /// <returns>
        /// The matching <see cref="X509Certificate2"/> if found; otherwise, <see langword="null"/>.
        /// </returns>
        private static X509Certificate2 FindCertificateByName(string certName, StoreLocation location, StoreName name)
        {
            // On Linux, the LocalMachine X509Store is limited, so tests load the certificate
            // directly from the file path provided through environment variables.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var certPassword = Environment.GetEnvironmentVariable("CERTIFICATE_PASSWORD");
                var certLocation = Environment.GetEnvironmentVariable("CERTIFICATE_LOCATION");

                return new X509Certificate2(
                    certLocation,
                    certPassword,
                    X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
            }

            // Certificate validation is intentionally disabled because the test root
            // certificate may not be installed on the machine running the tests.
            const bool validateCerts = false;

            using (var store = new X509Store(name, location))
            {
                store.Open(OpenFlags.ReadOnly);

                X509Certificate2Collection collection =
                    store.Certificates.Find(X509FindType.FindBySubjectName, certName, validateCerts);

                X509Certificate2 certToUse = null;

                // If multiple matching certificates exist, prefer the newest one.
                foreach (X509Certificate2 cert in collection)
                {
                    if (certToUse == null || cert.NotBefore > certToUse.NotBefore)
                    {
                        certToUse = cert;
                    }
                }

                return certToUse;
            }
        }
    }
}
