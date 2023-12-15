// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Performance.Helpers;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Performance
{
    /// <summary>
    /// Specifically used to test <c>SignWithCertificate</c> method 
    /// </summary>
    public class CryptoManagerTests
    {
        private const int AppsCount = 100;
        private MockHttpManager _httpManager;
        private readonly AcquireTokenForClientParameterBuilder[] _requests;
        private int _requestIdx;
        private ConfidentialClientApplication _cca;

        /// <summary>
        /// Generate a certificate. Create a Confidential Client Application with that certificate and
        /// an AcquireTokenForClient call to benchmark.
        /// </summary>
        public CryptoManagerTests()
        {
            _httpManager = new MockHttpManager(messageHandlerFunc: () => new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage()
            });
            _requests = new AcquireTokenForClientParameterBuilder[AppsCount];
            for (int i = 0; i < AppsCount; i++)
            {
                X509Certificate2 certificate = CertificateHelper.CreateCertificate("CN=rsa2048", RSA.Create(2048), HashAlgorithmName.SHA256, null);
                _cca = ConfidentialClientApplicationBuilder
                        .Create(TestConstants.ClientId)
                        .WithAuthority(new Uri(TestConstants.AuthorityTestTenant))
                        .WithRedirectUri(TestConstants.RedirectUri)
                        .WithCertificate(certificate)
                        .WithHttpManager(_httpManager)
                        .BuildConcrete();
                AddHostToInstanceCache(_cca.ServiceBundle, TestConstants.ProductionPrefNetworkEnvironment);
                _requests[_requestIdx] = _cca.AcquireTokenForClient(TestConstants.s_scope)
                    .WithForceRefresh(true);
            }
        }

        /// <summary>
        /// Adds mocked HTTP response to the HTTP manager before each call.
        /// Sets the index of the next app request to use.
        /// </summary>
        [IterationSetup]
        public void IterationSetup()
        {
            _requestIdx = _requestIdx++ % AppsCount;
        }

        [Benchmark]
        public async Task<AuthenticationResult> BenchmarkAsync()
        {
            return await _requests[_requestIdx].ExecuteAsync(System.Threading.CancellationToken.None).ConfigureAwait(true);
        }

        private void AddHostToInstanceCache(IServiceBundle serviceBundle, string host)
        {
            (serviceBundle.InstanceDiscoveryManager as InstanceDiscoveryManager)
                .AddTestValueToStaticProvider(
                    host,
                    new InstanceDiscoveryMetadataEntry
                    {
                        PreferredNetwork = host,
                        PreferredCache = host,
                        Aliases = new string[]
                        {
                            host
                        }
                    });
        }
    }
}
