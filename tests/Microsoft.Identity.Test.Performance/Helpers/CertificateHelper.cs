// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Test.Performance.Helpers
{
    public class CertificateHelper
    {

#if NET6_0_OR_GREATER
        public static X509Certificate2 CreateCertificate(string x509DistinguishedName, object key, HashAlgorithmName hashAlgorithmName, X509Certificate2 issuer)
        {
            CertificateRequest certificateRequest = null;
            if (key is RSA rsa1)
                certificateRequest = new CertificateRequest(x509DistinguishedName, rsa1, hashAlgorithmName, RSASignaturePadding.Pkcs1);

            if (issuer == null)
            {
                certificateRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, true, 0, true));
                return certificateRequest.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(20));
            }
            else
            {
                var certificate = certificateRequest.Create(issuer, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(10), Guid.NewGuid().ToByteArray());

                if (key is RSA rsa)
                    return certificate.CopyWithPrivateKey(rsa);

                return certificate;
            }
        }
#endif

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
