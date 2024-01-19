// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Identity.Test.Performance.Helpers
{
    public class CertificateHelper
    {
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
    }
}
