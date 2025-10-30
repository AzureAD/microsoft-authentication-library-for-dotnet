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
        internal static (string csrPem, RSA privateKey) Generate(RSA rsa, string clientId, string tenantId, CuidInfo cuid)
        {
            // Use custom polyfill for downlevel frameworks (net462, net472, netstandard2.0)
            // See CertificateRequest.cs
            var req = new CertificateRequest(
                new X500DistinguishedName($"CN={clientId}, DC={tenantId}"),
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pss);

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            writer.WriteCharacterString(UniversalTagNumber.UTF8String, JsonHelper.SerializeToJson(cuid));

            req.OtherRequestAttributes.Add(
                new AsnEncodedData(
                    "1.3.6.1.4.1.311.90.2.10",
                    writer.Encode()));

            string pemCsr = req.CreateSigningRequestPem();

            // Remove PEM headers and format as single line
            string rawCsr = pemCsr
                .Replace("-----BEGIN CERTIFICATE REQUEST-----", "")
                .Replace("-----END CERTIFICATE REQUEST-----", "")
                .Replace("\r", "")
                .Replace("\n", "")
                .Trim();

            return (rawCsr, rsa);
        }
    }
}
