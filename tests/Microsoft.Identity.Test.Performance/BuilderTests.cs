// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using BenchmarkDotNet.Attributes;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Performance.Helpers;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Performance
{
    /// <summary>
    /// Used to test the performance of builder methods.
    /// </summary>
    [MeanColumn, StdDevColumn, MedianColumn, MinColumn, MaxColumn]
    public class BuilderTests
    {
        private readonly X509Certificate2 _certificate;
        private readonly IConfidentialClientApplication _cca;
        private readonly LogCallback _logger = (_, _, _) => { };

        public BuilderTests()
        {
            _certificate = CertificateHelper.CreateCertificate("CN=rsa2048", RSA.Create(2048), HashAlgorithmName.SHA256, null);
            _cca = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityTenant)
                .WithRedirectUri(TestConstants.RedirectUri)
                .WithCertificate(_certificate)
                .Build();
        }

        [Benchmark(Description = "ConfidentialClientAppBuilder")]
        public IConfidentialClientApplication ConfidentialClientAppBuilder_Test()
        {
            return ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityCommonTenant)
                .WithRedirectUri(TestConstants.RedirectUri)
                .WithCertificate(_certificate)
                .WithLegacyCacheCompatibility(false)
                .WithLogging(_logger, logLevel: LogLevel.Always, enablePiiLogging: true)
                .Build();
        }

        [Benchmark(Description = "AcquireTokenForClientBuilder")]
        public AcquireTokenForClientParameterBuilder AcquireTokenForClientBuilder_Test()
        {
            return _cca
                .AcquireTokenForClient(TestConstants.s_scope)
                .WithTenantId(TestConstants.TenantId);
        }
    }
}
