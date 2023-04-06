// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public static class CertificateHelper
    {
        /// <summary>
        /// Try and locate a certificate matching the given <paramref name="subjectName"/> by searching in
        /// the <see cref="StoreName.My"/> store subjectName for all available <see cref="StoreLocation"/>s.
        /// </summary>
        /// <param name="subjectName">Thumbprint of certificate to locate</param>
        /// <returns><see cref="X509Certificate2"/> with <paramref subjectName="subjectName"/>, or null if no matching certificate was found</returns>
        public static X509Certificate2 FindCertificateByName(string subjectName)
        {
            foreach (StoreLocation storeLocation in Enum.GetValues(typeof(StoreLocation)))
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
        /// Try and locate a certificate matching the given <paramref name="certName"/> by searching in
        /// the in the given <see cref="StoreName"/> and <see cref="StoreLocation"/>.
        /// </summary>
        /// <param subjectName="certName">Thumbprint of certificate to locate</param>
        /// <param subjectName="location"><see cref="StoreLocation"/> in which to search for a matching certificate</param>
        /// <param subjectName="name"><see cref="StoreName"/> in which to search for a matching certificate</param>
        /// <returns><see cref="X509Certificate2"/> with <paramref subjectName="certName"/>, or null if no matching certificate was found</returns>
        public static X509Certificate2 FindCertificateByName(string certName, StoreLocation location, StoreName name)
        {
            // Don't validate certs, since the test root isn't installed.
            const bool validateCerts = false;

            using (var store = new X509Store(name, location))
            {
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection collection = store.Certificates.Find(X509FindType.FindBySubjectName, certName, validateCerts);

                X509Certificate2 certToUse = null;
                
                // select the "freshest" certificate
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
