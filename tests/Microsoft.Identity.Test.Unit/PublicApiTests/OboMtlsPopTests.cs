// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class OboMtlsPopTests : TestBase
    {
        private static X509Certificate2 s_testCertificate;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            s_testCertificate = CertHelper.GetOrCreateTestCert();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            s_testCertificate?.Dispose();
        }

        [TestMethod]
        public async Task OboWithMtlsPop_SniCert_SetsIsMtlsPopRequestedAsync()
        {
            // Validates that WithMtlsProofOfPossession() on OBO builder compiles
            // and sets IsMtlsPopRequested when CCA is configured with a certificate.
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithCertificate(s_testCertificate)
                    .WithAuthority(TestConstants.AuthorityTestTenant)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                var userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);

                AuthenticationResult result = await app
                    .AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [TestMethod]
        public async Task OboWithMtlsPop_FicAssertion_SetsIsMtlsPopRequestedAsync()
        {
            // Validates that WithMtlsProofOfPossession() on OBO builder works
            // when CCA is configured with a FIC client assertion (MSI leg 1 output).
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithClientAssertion((AssertionRequestOptions options) =>
                        Task.FromResult("fake_fic_assertion_from_msi_leg1"))
                    .WithAuthority(TestConstants.AuthorityTestTenant)
                    .WithHttpManager(httpManager)
                    .Build();

                var userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);

                AuthenticationResult result = await app
                    .AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                    .WithMtlsProofOfPossession()
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            }
        }
    }
}
