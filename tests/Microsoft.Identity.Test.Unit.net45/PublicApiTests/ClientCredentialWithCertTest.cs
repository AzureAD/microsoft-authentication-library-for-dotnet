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
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Client.Internal.JsonWebToken;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    [DeploymentItem(@"Resources\valid_cert.pfx")]
    [DeploymentItem(@"Resources\testCert.crtfile")]
    [DeploymentItem(@"Resources\RSATestCertDotNet.pfx")]
    public class ConfidentialClientWithCertTests : TestBase
    {
        private TokenCacheHelper _tokenCacheHelper;

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            _tokenCacheHelper = new TokenCacheHelper();
        }

        private static MockHttpMessageHandler CreateTokenResponseHttpHandler(bool clientCredentialFlow)
        {
            return new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = CreateResponse(clientCredentialFlow)
            };
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
                    Assert.AreEqual(x5c.Value.ToString(), TestConstants.Defaultx5cValue);
                }
            };
        }

        private static HttpResponseMessage CreateResponse(bool clientCredentialFlow)
        {
            return clientCredentialFlow ?
                MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage(MockHelpers.CreateClientInfo(TestConstants.Uid, TestConstants.Utid)) :
                MockHelpers.CreateSuccessTokenResponseMessage(
                          TestConstants.s_scope.AsSingleString(),
                          MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                          MockHelpers.CreateClientInfo(TestConstants.Uid, TestConstants.Utid));
        }

        private void SetupMocks(MockHttpManager httpManager)
        {
            httpManager.AddInstanceDiscoveryMockHandler();
            httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityCommonTenant);
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
                    TestConstants.DefaultPassword);

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate)
                    .BuildConcrete();

                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                //Check for x5c claim
                harness.HttpManager.AddMockHandler(CreateTokenResponseHttpHandlerWithX5CValidation(true));
                AuthenticationResult result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .WithSendX5C(true)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                appCacheAccess.AssertAccessCounts(1, 1);
                userCacheAccess.AssertAccessCounts(0, 0);

                // from the cache
                result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);

                //Check app cache
                Assert.AreEqual(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                appCacheAccess.AssertAccessCounts(2, 1);
                userCacheAccess.AssertAccessCounts(0, 0);
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
                    TestConstants.DefaultPassword);

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate)
                    .BuildConcrete();

                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                var userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);

                //Check for x5c claim
                harness.HttpManager.AddMockHandler(CreateTokenResponseHttpHandlerWithX5CValidation(false));
                AuthenticationResult result = await app
                    .AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                    .WithSendX5C(true)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                Assert.IsNotNull(result.AccessToken);

                appCacheAccess.AssertAccessCounts(0, 0);
                userCacheAccess.AssertAccessCounts(1, 1);

                result = await app
                    .AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);

                //Check user cache
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                appCacheAccess.AssertAccessCounts(0, 0);
                userCacheAccess.AssertAccessCounts(2, 1);
            }
        }

        [TestMethod]
        [Description("Test for client assertion with X509 public certificate using Auth code")]
        public async Task JsonWebTokenWithX509PublicCertSendCertificateByAuthCodeTestAsync()
        {
            using (var harness = CreateTestHarness())
            {
                SetupMocks(harness.HttpManager);

                var certificate = new X509Certificate2(
                    ResourceHelper.GetTestResourceRelativePath("valid_cert.pfx"),
                    TestConstants.DefaultPassword);

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate)
                    .BuildConcrete();

                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                var userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);

                //Check for x5c claim
                harness.HttpManager.AddMockHandler(CreateTokenResponseHttpHandlerWithX5CValidation(false));
                AuthenticationResult result = await app
                    .AcquireTokenByAuthorizationCode(TestConstants.s_scope, TestConstants.DefaultAuthorizationCode)
                    .WithSendX5C(true)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                Assert.IsNotNull(result.AccessToken);

                appCacheAccess.AssertAccessCounts(0, 0);
                userCacheAccess.AssertAccessCounts(0, 1);
            }
        }

        [TestMethod]
        [Description("Test for client assertion with X509 public certificate using acquire token by refresh token")]
        public async Task JsonWebTokenWithX509PublicCertSendCertificateByRefreshTokenTestAsync()
        {
            using (var harness = CreateTestHarness())
            {
                SetupMocks(harness.HttpManager);

                var certificate = new X509Certificate2(
                    ResourceHelper.GetTestResourceRelativePath("valid_cert.pfx"),
                    TestConstants.DefaultPassword);

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate)
                    .BuildConcrete();

                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                var userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);

                //Check for x5c claim
                harness.HttpManager.AddMockHandler(CreateTokenResponseHttpHandlerWithX5CValidation(false));
                AuthenticationResult result = await ((IByRefreshToken)app)
                    .AcquireTokenByRefreshToken(TestConstants.s_scope, TestConstants.DefaultAuthorizationCode)
                    .WithSendX5C(true)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                Assert.IsNotNull(result.AccessToken);

                appCacheAccess.AssertAccessCounts(0, 0);
                userCacheAccess.AssertAccessCounts(0, 1);
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
                    TestConstants.DefaultPassword);

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri("https://login.microsoftonline.com/my-utid"),true)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate).BuildConcrete();

                _tokenCacheHelper.PopulateCacheWithOneAccessToken(app.UserTokenCacheInternal.Accessor);
                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                app.UserTokenCacheInternal.Accessor.DeleteAccessToken(
                    new MsalAccessTokenCacheKey(
                        TestConstants.ProductionPrefNetworkEnvironment,
                        TestConstants.Utid,
                        TestConstants.s_userIdentifier,
                        TestConstants.ClientId,
                        TestConstants.ScopeForAnotherResourceStr));

                //Check for x5c claim
                harness.HttpManager.AddMockHandler(CreateTokenResponseHttpHandlerWithX5CValidation(false));

                var result = await app
                    .AcquireTokenSilent(
                        new[] { "someTestScope"},
                        new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null))
                    .WithSendX5C(true)
                    .WithForceRefresh(true)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);

                //Check user cache
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());

                appCacheAccess.AssertAccessCounts(0, 0);
                userCacheAccess.AssertAccessCounts(1, 1);
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

        [TestMethod]
        [Description("Check that the private key is accessable when signing")]
        public async Task CheckRSAPrivateKeyCanSignAssertionAsync()
        {
            using (var harness = CreateTestHarness())
            {
                SetupMocks(harness.HttpManager);
                var certificate = new X509Certificate2(
                    ResourceHelper.GetTestResourceRelativePath("RSATestCertDotNet.pfx"));

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate)
                    .BuildConcrete();

                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                harness.HttpManager.AddMockHandler(CreateTokenResponseHttpHandler(true));
                AuthenticationResult result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .WithSendX5C(true)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                appCacheAccess.AssertAccessCounts(1, 1);
                userCacheAccess.AssertAccessCounts(0, 0);

                // from the cache
                result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);

                //Check app cache
                Assert.AreEqual(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                appCacheAccess.AssertAccessCounts(2, 1);
                userCacheAccess.AssertAccessCounts(0, 0);
            }
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
