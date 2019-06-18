// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


#if !ANDROID && !iOS && !WINDOWS_APP 
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Client.Internal.JsonWebToken;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    [DeploymentItem(@"Resources\valid_cert.pfx")]
    [DeploymentItem(@"Resources\testCert.crtfile")]
    public class ConfidentialClientWithCertTests : TestBase
    {
        private TokenCacheHelper _tokenCacheHelper;

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            _tokenCacheHelper = new TokenCacheHelper();
        }

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

                    // Check presence and value of x5c cert claim.
                    var handler = new JwtSecurityTokenHandler();
                    var jsonToken = handler.ReadJwtToken(encodedJwt);
                    var x5c = jsonToken.Header.Where(header => header.Key == "x5c").FirstOrDefault();
                    Assert.AreEqual("x5c", x5c.Key, "x5c should be present");
                    Assert.AreEqual(x5c.Value.ToString(), MsalTestConstants.Defaultx5cValue);
                }
            };
        }

        private static HttpResponseMessage CreateResponse(bool clientCredentialFlow)
        {
            return clientCredentialFlow ?
                MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage(MockHelpers.CreateClientInfo(MsalTestConstants.Uid, MsalTestConstants.Utid)) :
                MockHelpers.CreateSuccessTokenResponseMessage(
                          MsalTestConstants.Scope.AsSingleString(),
                          MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId),
                          MockHelpers.CreateClientInfo(MsalTestConstants.Uid, MsalTestConstants.Utid));
        }

        private void SetupMocks(MockHttpManager httpManager)
        {
            httpManager.AddInstanceDiscoveryMockHandler();
            httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);
        }

        private void SetupMocks(MockHttpManager httpManager, string authority)
        {
            httpManager.AddInstanceDiscoveryMockHandler();
            httpManager.AddMockHandlerForTenantEndpointDiscovery(authority);
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

                var app = ConfidentialClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId)
                    .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithRedirectUri(MsalTestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate)
                    .BuildConcrete();

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

                //Check app cache
                Assert.AreEqual(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count());

                //Clear cache
                app.AppTokenCacheInternal.ClearMsalCache();
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

                //Check user cache
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());

                //Clear cache
                app.UserTokenCacheInternal.ClearMsalCache();
            }
        }

        [TestMethod]
        [Description("Test for acqureTokenSilent with X509 public certificate using sendCertificate")]
        public async Task JsonWebTokenWithX509PublicCertSendCertificateSilentTestAsync()
        {
            using (var harness = CreateTestHarness())
            {
                SetupMocks(harness.HttpManager, "https://login.microsoftonline.com/my-utid/");
                var certificate = new X509Certificate2(
                    ResourceHelper.GetTestResourceRelativePath("valid_cert.pfx"),
                    MsalTestConstants.DefaultPassword);

                var app = ConfidentialClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId)
                    .WithAuthority(new System.Uri("https://login.microsoftonline.com/my-utid"),true)
                    .WithRedirectUri(MsalTestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate).BuildConcrete();

                _tokenCacheHelper.PopulateCacheWithOneAccessToken(app.UserTokenCacheInternal.Accessor);
                app.UserTokenCacheInternal.Accessor.DeleteAccessToken(
                    new MsalAccessTokenCacheKey(
                        MsalTestConstants.ProductionPrefNetworkEnvironment,
                        MsalTestConstants.Utid,
                        MsalTestConstants.UserIdentifier,
                        MsalTestConstants.ClientId,
                        MsalTestConstants.ScopeForAnotherResourceStr));

                //Check for x5c claim
                harness.HttpManager.AddMockHandler(CreateTokenResponseHttpHandlerWithX5CValidation(false));

                var result = await app
                    .AcquireTokenSilent(
                        new[] { "someTestScope"},
                        new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null))
                    .WithSendX5C(true)
                    .WithForceRefresh(true)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);

                //Check user cache
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());

                //Clear cache
                app.UserTokenCacheInternal.ClearMsalCache();
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

        private ClientCredentialWrapper GenerateClientAssertionCredential()
        {
            var cert = new X509Certificate2(
            ResourceHelper.GetTestResourceRelativePath("testCert.crtfile"), "passw0rd!");

            var credential = ClientCredentialWrapper.CreateWithCertificate(cert);
            return credential;
        }
    }
}
#endif
