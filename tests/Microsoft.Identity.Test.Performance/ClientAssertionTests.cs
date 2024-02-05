// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using BenchmarkDotNet.Attributes;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Performance.Helpers;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Performance
{
    public class ClientAssertionTests
    {
        private static X509Certificate2 s_certificate = 
            CertificateHelper.CreateCertificate("CN=rsa2048", RSA.Create(2048), HashAlgorithmName.SHA256, null);

        string base64EncodedThumbprint = Base64UrlHelpers.Encode(s_certificate.GetCertHash());

        private static Dictionary<string, string> s_cl = new Dictionary<string, string>()
        {
            {"key1", "val1" },
            {"key2", "val2" }
        };

        [ParamsAllValues]
        public bool UseSha2 { get; set; }

        [ParamsAllValues]
        public bool UseX5C { get; set; }

        [ParamsAllValues]
        public bool UseExtraClaims { get; set; }

        [Benchmark(Description = "SimpleAssertion")]
        public void ConfidentialClientAppBuilder_Test()
        {
            JsonWebToken msalJwtTokenObj =
                new JsonWebToken(new CommonCryptographyManager(),
                TestConstants.ClientId,
                "aud",
                UseExtraClaims ? s_cl : null,
                true);


            msalJwtTokenObj.Sign(s_certificate, UseX5C, useSha2AndPss: true);
        }
    }
}
