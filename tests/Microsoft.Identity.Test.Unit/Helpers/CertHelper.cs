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
        private static Dictionary<KnownTestCertType, X509Certificate2> s_x509Certificates = new Dictionary<KnownTestCertType, X509Certificate2>();

        public static X509Certificate2 GetOrCreateTestCert(KnownTestCertType knownTestCertType = KnownTestCertType.RSA)
        {
            // create the cert if it doesn't exist. use a lock to prevent multiple threads from creating the cert
            s_x509Certificates.TryGetValue(knownTestCertType, out X509Certificate2 x509Certificate2);

            if (x509Certificate2 == null)
            {
                lock (typeof(CertHelper))
                {
                    if (x509Certificate2 == null)
                    {
                        x509Certificate2 = CreateTestCert(knownTestCertType);
                        s_x509Certificates.Add(knownTestCertType, x509Certificate2);
                    }
                }
            }

            return x509Certificate2;
        }

        private static X509Certificate2 CreateTestCert(KnownTestCertType knownTestCertType = KnownTestCertType.RSA)
        {
            switch (knownTestCertType)
            {
                case KnownTestCertType.ECD:
                    string secp256r1Oid = "1.2.840.10045.3.1.7";  //oid for prime256v1(7)  other identifier: secp256r1

                    using (var ecdsa = ECDsa.Create(ECCurve.CreateFromValue(secp256r1Oid)))
                    {
                        string subjectName = "SelfSignedEdcCert";

                        var certRequest = new CertificateRequest($"CN={subjectName}", ecdsa, HashAlgorithmName.SHA256);

                        X509Certificate2 generatedCert = certRequest.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(10)); // generate the cert and sign!

                        X509Certificate2 pfxGeneratedCert = new X509Certificate2(generatedCert.Export(X509ContentType.Pfx)); //has to be turned into pfx or Windows at least throws a security credentials not found during sslStream.connectAsClient or HttpClient request...

                        return pfxGeneratedCert;
                    }
                case KnownTestCertType.RSA:
                default:
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
        }
    }

    public enum KnownTestCertType
    {
        RSA,
        ECD
    }
}
