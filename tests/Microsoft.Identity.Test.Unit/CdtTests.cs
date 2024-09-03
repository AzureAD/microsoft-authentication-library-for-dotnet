// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AuthScheme;
using Microsoft.Identity.Client.AuthScheme.CDT;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    internal class CdtTests : TestBase
    {
        private const string ProtectedUrl = "https://www.contoso.com/path1/path2?queryParam1=a&queryParam2=b";

        [TestMethod]
        public async Task CDT_InMemoryTest_Async()
        {
            Constraint constraint = new Constraint();

            constraint.Type = "wk:user";
            constraint.Action = "update";
            constraint.Values = new[] { "val1", "val2" };

            var constraintAsString = JsonHelper.SerializeToJson<Constraint>(constraint);

            using (var httpManager = new MockHttpManager())
            {
                ConfidentialClientApplication app =
                    ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .WithExperimentalFeatures(true)
                                                              .BuildConcrete();

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(ProtectedUrl));
                //Gets default InMemory CDT key provider
                var provider = MsalCryptoProviderFactory.GetOrCreateProvider();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulCDTClientCredentialTokenResponseMessage();

                // Act
                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithTenantId(TestConstants.Utid)
                    .WithConstraints(constraintAsString)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // access token parsing can be done with MSAL's id token parsing logic
                var claims = IdToken.Parse(result.AccessToken).ClaimsPrincipal;

                AssertConstrainedDelegationClaims(provider, claims, constraintAsString);
            }
        }

        [TestMethod]
        [DeploymentItem(@"Resources\testCert.crtfile")]
        public async Task CDT_WithCertTest_Async()
        {
            Constraint constraint = new Constraint();

            constraint.Type = "wk:user";
            constraint.Action = "update";
            constraint.Values = new[] { "val1", "val2" };

            var constraintAsString = JsonHelper.SerializeToJson<Constraint>(constraint);

            using (var httpManager = new MockHttpManager())
            {
                ConfidentialClientApplication app =
                    ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .WithExperimentalFeatures(true)
                                                              .BuildConcrete();

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(ProtectedUrl));

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulCDTClientCredentialTokenResponseMessage();

                var cert = new X509Certificate2(
                            ResourceHelper.GetTestResourceRelativePath("testCert.crtfile"), TestConstants.TestCertPassword);

                var provider = new CdtCryptoProvider(cert);

                // Act
                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithTenantId(TestConstants.Utid)
                    .WithConstraints(constraintAsString, cert)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // access token parsing can be done with MSAL's id token parsing logic
                var claims = IdToken.Parse(result.AccessToken).ClaimsPrincipal;

                AssertConstrainedDelegationClaims(provider, claims, constraintAsString);
            }
        }

        private static void AssertConstrainedDelegationClaims(ICdtCryptoProvider cdtCryptoProvider, System.Security.Claims.ClaimsPrincipal claims, string constraint)
        {
            var ticket = claims.FindAll("t").Single().Value;
            var constraints = claims.FindAll("c").Single().Value;
            Assert.IsTrue(!string.IsNullOrEmpty(ticket));
            Assert.IsTrue(!string.IsNullOrEmpty(constraints));

            Assert.IsTrue(ticket.Contains($"header.payload.signature[ds_cnf="));
            var keyId = ticket.Split('=')[1].TrimEnd(']');
            var decodedKey = Base64UrlHelpers.Decode(keyId);
            Assert.IsTrue(decodedKey.Contains(ComputeThumbprint(cdtCryptoProvider.CannonicalPublicKeyJwk)));

            var constraintsClaims = IdToken.Parse(constraints).ClaimsPrincipal;
            var constraintsClaim = constraintsClaims.FindAll("constraints").Single().Value;
            Assert.AreEqual(constraint, constraintsClaim);
        }

        private static string ComputeThumbprint(string canonicalJwk)
        {
            using (SHA256 hash = SHA256.Create())
            {
                return Base64UrlHelpers.Encode(hash.ComputeHash(Encoding.UTF8.GetBytes(canonicalJwk)));
            }
        }
    }
}
