// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.ManagedIdentity.V2
{
    internal class Csr
    {
        internal static string Generate(string clientId, string tenantId, CuidInfo cuid)
        {
            using (RSA rsa = CreateRsaKeyPair())
            {
#if NET8_0_OR_GREATER
                // Use built-in .NET 8.0+ CertificateRequest with OtherRequestAttributes support
                var req = new System.Security.Cryptography.X509Certificates.CertificateRequest(
                    new X500DistinguishedName($"CN={clientId}, DC={tenantId}"),
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pss);
#else
                // Use custom polyfill for downlevel frameworks (net462, net472, netstandard2.0)
                var req = new CertificateRequest(
                    new X500DistinguishedName($"CN={clientId}, DC={tenantId}"),
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pss);
#endif

                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                writer.WriteCharacterString(UniversalTagNumber.UTF8String, JsonHelper.SerializeToJson(cuid));

                req.OtherRequestAttributes.Add(
                    new AsnEncodedData(
                        "1.3.6.1.4.1.311.90.2.10",
                        writer.Encode()));

                return req.CreateSigningRequestPem();
            }
        }

        private static RSA CreateRsaKeyPair()
        {
            // TODO: use the strongest key on the machine i.e. a TPM key
            RSA rsa = null;

#if NET462 || NET472
            // .NET Framework runs only on Windows, so RSACng (Windows-only) is always available
            rsa = new RSACng();
#else
            // Cross-platform .NET - RSA.Create() returns appropriate PSS-capable implementation
            rsa = RSA.Create();
#endif
            rsa.KeySize = 2048;
            return rsa;
        }
    }
}
