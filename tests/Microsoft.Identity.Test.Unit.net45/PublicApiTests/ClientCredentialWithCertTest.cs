// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


#if !ANDROID && !iOS && !WINDOWS_APP 
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Client.Internal.JsonWebToken;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    [DeploymentItem(@"Resources\valid_cert.pfx")]
    [DeploymentItem(@"Resources\testCert.crtfile")]
    public class ConfidentialClientWithCertTests : TestBase
    {
        private static MockHttpMessageHandler CreateTokenResponseHttpHandlerWithX5CValidation(bool clientCredentialFlow)
        {
            return new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = CreateResponse(clientCredentialFlow),
                AdditionalRequestValidation = request =>
                {
                    var requestContent = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var formsData = CoreHelpers.ParseKeyValueList(requestContent, '&', true, null);

                    // Check presence of client_assertion in request
                    Assert.IsTrue(formsData.TryGetValue("client_assertion", out string encodedJwt), "Missing client_assertion from request");

                    // Check presence of x5c cert claim. It should exist.
                    var handler = new JwtSecurityTokenHandler();
                    var jsonToken = handler.ReadJwtToken(encodedJwt);
                    Assert.IsTrue(jsonToken.Header.Any(header => header.Key == "x5c"), "x5c should be present");
                }
            };
        }

        private static HttpResponseMessage CreateResponse(bool clientCredentialFlow)
        {
            return clientCredentialFlow ?
                MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage() :
                MockHelpers.CreateSuccessTokenResponseMessage(
                          MsalTestConstants.Scope.AsSingleString(),
                          MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId),
                          MockHelpers.CreateClientInfo(MsalTestConstants.Uid, MsalTestConstants.Utid + "more"));
        }

        internal void SetupMocks(MockHttpManager httpManager)
        {
            httpManager.AddInstanceDiscoveryMockHandler();
            httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);
        }

        [TestMethod]
        [Description("Test for client assertion with X509 public certificate using sendCertificate")]
        public async Task JsonWebTokenWithX509PublicCertSendCertificateTestAsync()
        {
            using (var harness = CreateTestHarness())
            {
                SetupMocks(harness.HttpManager);
                var certificate = new X509Certificate2(
                    ResourceHelper.GetTestResourceRelativePath("valid_cert.pfx"),
                    MsalTestConstants.DefaultPassword);

                var app = ConfidentialClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                              .WithAuthority(
                                                                  new System.Uri(ClientApplicationBase.DefaultAuthority),
                                                                  true).WithRedirectUri(MsalTestConstants.RedirectUri)
                                                              .WithHttpManager(harness.HttpManager)
                                                              .WithCertificate(certificate).BuildConcrete();

                //Check for x5c claim
                harness.HttpManager.AddMockHandler(CreateTokenResponseHttpHandlerWithX5CValidation(true));
                AuthenticationResult result = await app
                    .AcquireTokenForClient(MsalTestConstants.Scope)
                    .WithSendX5C(true)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);

                result = await app
                    .AcquireTokenForClient(MsalTestConstants.Scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
            }
        }

        [TestMethod]
        [Description("Test for client assertion with X509 public certificate using sendCertificate")]
        public async Task JsonWebTokenWithX509PublicCertSendCertificateOnBehalfOfTestAsync()
        {
            using (var harness = CreateTestHarness())
            {
                SetupMocks(harness.HttpManager);

                var certificate = new X509Certificate2(
                    ResourceHelper.GetTestResourceRelativePath("valid_cert.pfx"),
                    MsalTestConstants.DefaultPassword);

                var app = ConfidentialClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId)
                    .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithRedirectUri(MsalTestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate)
                    .BuildConcrete();

                var userAssertion = new UserAssertion(MsalTestConstants.DefaultAccessToken);

                //Check for x5c claim
                harness.HttpManager.AddMockHandler(CreateTokenResponseHttpHandlerWithX5CValidation(false));
                AuthenticationResult result = await app
                    .AcquireTokenOnBehalfOf(MsalTestConstants.Scope, userAssertion)
                    .WithSendX5C(true)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                Assert.IsNotNull(result.AccessToken);

                result = await app
                    .AcquireTokenOnBehalfOf(MsalTestConstants.Scope, userAssertion)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
            }
        }

        [TestMethod]
        [Description("Check the JWTHeader when sendCert is true")]
        public void CheckJWTHeaderWithCertTrueTest()
        {
            var credential = GenerateClientAssertionCredential();

            var header = new JWTHeaderWithCertificate(credential, true);

            Assert.IsNotNull(header.X509CertificatePublicCertValue);
            Assert.IsNotNull(header.X509CertificateThumbprint);
        }

        [TestMethod]
        [Description("Check the JWTHeader when sendCert is false")]
        public void CheckJWTHeaderWithCertFalseTest()
        {
            var credential = GenerateClientAssertionCredential();

            var header = new JWTHeaderWithCertificate(credential, false);

            Assert.IsNull(header.X509CertificatePublicCertValue);
            Assert.IsNotNull(header.X509CertificateThumbprint);
        }

        private ClientAssertionCertificateWrapper GenerateClientAssertionCredential()
        {
            var cert = new X509Certificate2(
            ResourceHelper.GetTestResourceRelativePath("testCert.crtfile"), "passw0rd!");

            var credential = new ClientAssertionCertificateWrapper(cert);
            return credential;
        }
    }
}
#endif
