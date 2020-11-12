// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Test.LabInfrastructure
{
    public static class CertificateHelper
    {
        /// <summary>
        /// Try and locate a certificate matching the given <paramref name="thumbprint"/> by searching in
        /// the <see cref="StoreName.My"/> store name for <see cref="StoreLocation.LocalMachine"/> and <see cref="StoreLocation.CurrentUser"/>
        /// in that order.</summary>
        /// <param name="thumbprint">Thumbprint of certificate to locate</param>
        /// <returns><see cref="X509Certificate2"/> with <paramref name="thumbprint"/>, or null if no matching certificate was found</returns>
        public static X509Certificate2 FindCertificateByThumbprint(string thumbprint)
        {
            var certificate = FindCertificateByThumbprint(thumbprint, StoreLocation.LocalMachine, StoreName.My);
            if (certificate != null)
            {
                return certificate;
            }

            return FindCertificateByThumbprint(thumbprint, StoreLocation.CurrentUser, StoreName.My);

        }

        /// <summary>
        /// Try and locate a certificate matching the given <paramref name="thumbprint"/> by searching in
        /// the in the given <see cref="StoreName"/> and <see cref="StoreLocation"/>.
        /// </summary>
        /// <param name="thumbprint">Thumbprint of certificate to locate</param>
        /// <param name="location"><see cref="StoreLocation"/> in which to search for a matching certificate</param>
        /// <param name="name"><see cref="StoreName"/> in which to search for a matching certificate</param>
        /// <returns><see cref="X509Certificate2"/> with <paramref name="thumbprint"/>, or null if no matching certificate was found</returns>
        public static X509Certificate2 FindCertificateByThumbprint(string thumbprint, StoreLocation location, StoreName name)
        {
            // Don't validate certs, since the test root isn't installed.
            const bool validateCerts = false;

            using (var store = new X509Store(name, location))
            {
                store.Open(OpenFlags.ReadOnly);
                var collection = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validateCerts);

                return collection.Count == 0
                    ? null
                    : collection[0];

            }
        }
    }
}
