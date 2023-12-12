// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    public static class CertHelper
    {
        private static X509Certificate2 s_x509Certificate2 = null;
        private static object s_lockObject;

        public static X509Certificate2 GetOrCreateTestCert()
        {
           // create the cert if it doesn't exist. use a lock to prevent multiple threads from creating the cert

            if (s_x509Certificate2 == null)
            {
                lock (typeof(CertHelper))
                {
                    if (s_x509Certificate2 == null)
                    {
                        s_x509Certificate2 = CreateTestCert();
                    }
                }
            }

            return s_x509Certificate2;
        }

        private static X509Certificate2 CreateTestCert()
        {
            using (RSA rsa = RSA.Create(4096))
            {
                CertificateRequest parentReq = new CertificateRequest(
                    "CN=Test Cert",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                parentReq.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(true, false, 0, true));

                parentReq.CertificateExtensions.Add(
                    new X509SubjectKeyIdentifierExtension(parentReq.PublicKey, false));

                X509Certificate2 cert = parentReq.CreateSelfSigned(
                     DateTimeOffset.UtcNow,
                     DateTimeOffset.UtcNow.AddDays(1));

                return cert;
            }
        }

        public static X509Certificate2 GetOrCreateTestCertWithBuilder()
        {
            // create the cert if it doesn't exist. use a lock to prevent multiple threads from creating the cert

            if (s_x509Certificate2 == null)
            {
                lock (s_lockObject)
                {
                    if (s_x509Certificate2 == null)
                    {
                        // Use the X509Certificate2Builder to create the certificate
                        using (RSA rsa = RSA.Create(4096))
                        {
                            DateTimeOffset notBefore = DateTimeOffset.UtcNow;
                            DateTimeOffset notAfter = notBefore.AddDays(1);

                            X509Certificate2 cert = new X509Certificate2Builder()
                                .WithSubjectName("CN=Test Cert")
                                .WithPublicKey(rsa)
                                .WithHashAlgorithm(HashAlgorithmName.SHA256)
                                .WithSignatureAlgorithm(RSASignaturePadding.Pkcs1)
                                .WithBasicConstraintsExtension(true, false, 0, true)
                                .WithSubjectKeyIdentifierExtension(false)
                                .WithNotBefore(notBefore)
                                .WithNotAfter(notAfter)
                                .Build();

                            return cert;
                        }
                    }
                }
            }

            return s_x509Certificate2;
        }


    }
}
