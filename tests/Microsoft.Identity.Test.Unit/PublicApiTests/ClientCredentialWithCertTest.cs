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
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Client.Internal.JsonWebToken;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    [DeploymentItem(@"Resources\testCert.crtfile")]
    [DeploymentItem(@"Resources\RSATestCertDotNet.pfx")]
    public class ConfidentialClientWithCertTests : TestBase
    {
        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
        }

        private static MockHttpMessageHandler CreateTokenResponseHttpHandler(bool clientCredentialFlow)
        {
            return new MockHttpMessageHandler()
            {
                ExpectedMethod = HttpMethod.Post,
                ResponseMessage = CreateResponse(clientCredentialFlow)
            };
        }

        private static MockHttpMessageHandler CreateTokenResponseHttpHandlerWithX5CValidation(
            bool clientCredentialFlow, 
            string expectedX5C = null)
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
                    var x5c = jsonToken.Header.FirstOrDefault(header => header.Key == "x5c");
                    if (expectedX5C != null)
                    {
                        Assert.AreEqual("x5c", x5c.Key, "x5c should be present");
                        Assert.AreEqual(x5c.Value.ToString(), expectedX5C);
                    }
                    else
                    {
                        Assert.IsNull(x5c.Key);
                        Assert.IsNull(x5c.Value);
                    }
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
        }

        private void SetupMocks(MockHttpManager httpManager, string authority)
        {
            httpManager.AddInstanceDiscoveryMockHandler();
        }

        [DataTestMethod]
        [DataRow(true, null, true)]
        [DataRow(false, null, false)]
        [DataRow(null, null, false)] // the default is false
        [DataRow(null, true, true)]
        [DataRow(null, false, false)]
        [DataRow(true, true, true)]
        [DataRow(false, false, false)]
        [DataRow(true, false, false)] // request overrides
        [DataRow(false, true, true)] // request overrides
        public async Task JsonWebTokenWithX509PublicCertSendCertificateTestSendX5cCombinationsAsync(bool? appFlag, bool? requestFlag, bool expectX5c)
        {
            using (var harness = CreateTestHarness())
            {
                SetupMocks(harness.HttpManager);
                var certificate = CertHelper.GetOrCreateTestCert();
                var exportedCertificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));

                var appBuilder = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithHttpManager(harness.HttpManager);

                if (appFlag.HasValue)
                {
                    appBuilder = appBuilder.WithCertificate(certificate, appFlag.Value); // app flag
                }
                else
                {
                    appBuilder = appBuilder.WithCertificate(certificate); // no app flag
                }

                var app = appBuilder.BuildConcrete();

                //Check for x5c claim
                harness.HttpManager.AddMockHandler(CreateTokenResponseHttpHandlerWithX5CValidation(
                    true, expectX5c ? exportedCertificate : null));

                var builder = app.AcquireTokenForClient(TestConstants.s_scope);

                if (requestFlag != null)
                {
                    builder = builder.WithSendX5C(requestFlag.Value);
                }

                AuthenticationResult result = await builder
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
            }
        }

        [DataTestMethod]
        [DataRow(true, null, true)]
        [DataRow(false, null, false)]
        [DataRow(null, null, false)] // the default is false
        [DataRow(null, true, true)]
        [DataRow(null, false, false)]
        [DataRow(true, true, true)]
        [DataRow(false, false, false)]
        [DataRow(true, false, false)] // request overrides
        [DataRow(false, true, true)] // request overrides
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Internal.Analyzers", "IA5352:DoNotMisuseCryptographicApi", Justification = "Suppressing RoslynAnalyzers: Rule: IA5352 - Do Not Misuse Cryptographic APIs in test only code")]
        public async Task JsonWebTokenWithX509PublicCertSendCertificateWithClaimsTestSendX5cCombinationsAsync(
            bool? appFlag,
            bool? requestFlag,
            bool expectX5c)
        {
            using (var harness = CreateTestHarness())
            {
                SetupMocks(harness.HttpManager);
                var certificate = CertHelper.GetOrCreateTestCert();
                var exportedCertificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));

                IDictionary<string, string> claimsToSign = new Dictionary<string, string>();
                claimsToSign.Add("Foo", "Bar");

                var appBuilder = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithHttpManager(harness.HttpManager);

                if (appFlag.HasValue)
                {
                    appBuilder = appBuilder.WithClientClaims(certificate, claimsToSign, sendX5C: appFlag.Value); // app flag
                }
                else
                {
                    appBuilder = appBuilder.WithClientClaims(certificate, claimsToSign); // no app flag
                }

                var app = appBuilder.BuildConcrete();

                //Check for x5c claim
                harness.HttpManager.AddMockHandler(CreateTokenResponseHttpHandlerWithX5CValidation(
                    true, expectX5c ? exportedCertificate : null));

                var builder = app.AcquireTokenForClient(TestConstants.s_scope);

                if (requestFlag != null)
                {
                    builder = builder.WithSendX5C(requestFlag.Value);
                }

                AuthenticationResult result = await builder
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
            }
        }

        [TestMethod]
        [Description("Test for client assertion with X509 public certificate using sendCertificate")]
        public async Task JsonWebTokenWithX509PublicCertSendCertificateTestAsync()
        {
            using (var harness = CreateTestHarness())
            {
                SetupMocks(harness.HttpManager);
                var certificate = CertHelper.GetOrCreateTestCert();
                var exportedCertificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate, true)
                    .BuildConcrete();

                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                //Check for x5c claim
                harness.HttpManager.AddMockHandler(
                    CreateTokenResponseHttpHandlerWithX5CValidation(true, exportedCertificate));
                AuthenticationResult result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
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
                Assert.IsNull(result.ClaimsPrincipal);

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

                var certificate = CertHelper.GetOrCreateTestCert();
                var exportedCertificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate, true)
                    .BuildConcrete();

                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                var userAssertion = new UserAssertion(TestConstants.DefaultAccessToken);

                //Check for x5c claim
                harness.HttpManager.AddMockHandler(CreateTokenResponseHttpHandlerWithX5CValidation(false, exportedCertificate));
                AuthenticationResult result = await app
                    .AcquireTokenOnBehalfOf(TestConstants.s_scope, userAssertion)
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

                var certificate = CertHelper.GetOrCreateTestCert();
                var exportedCertificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate, true)
                    .BuildConcrete();

                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                //Check for x5c claim
                harness.HttpManager.AddMockHandler(
                    CreateTokenResponseHttpHandlerWithX5CValidation(false, exportedCertificate));
                AuthenticationResult result = await app
                    .AcquireTokenByAuthorizationCode(TestConstants.s_scope, TestConstants.DefaultAuthorizationCode)
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

                var certificate = CertHelper.GetOrCreateTestCert();
                var exportedCertificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate, true)
                    .BuildConcrete();

                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                //Check for x5c claim
                harness.HttpManager.AddMockHandler(
                    CreateTokenResponseHttpHandlerWithX5CValidation(false, exportedCertificate));
                AuthenticationResult result = await ((IByRefreshToken)app)
                    .AcquireTokenByRefreshToken(TestConstants.s_scope, TestConstants.DefaultAuthorizationCode)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                Assert.IsNotNull(result.AccessToken);

                appCacheAccess.AssertAccessCounts(0, 0);
                userCacheAccess.AssertAccessCounts(0, 1);

                Assert.AreEqual(result.Account.HomeAccountId.Identifier,
                    userCacheAccess.LastAfterAccessNotificationArgs.SuggestedCacheKey);
                Assert.AreEqual(result.Account.HomeAccountId.Identifier,
                    userCacheAccess.LastBeforeAccessNotificationArgs.SuggestedCacheKey);                
            }
        }

        [TestMethod]
        [Description("Test for acquireTokenSilent with X509 public certificate using sendCertificate")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Internal.Analyzers", "IA5352:DoNotMisuseCryptographicApi", Justification = "Fake password only used for tests.")]
        public async Task JsonWebTokenWithX509PublicCertSendCertificateSilentTestAsync()
        {
            using (var harness = CreateTestHarness())
            {
                SetupMocks(harness.HttpManager, "https://login.microsoftonline.com/my-utid/");
                var certificate = CertHelper.GetOrCreateTestCert();
                var exportedCertificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new System.Uri("https://login.microsoftonline.com/my-utid"),true)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithHttpManager(harness.HttpManager)
                    .WithCertificate(certificate, true)
                    .BuildConcrete();

                TokenCacheHelper.PopulateCacheWithOneAccessToken(app.UserTokenCacheInternal.Accessor);
                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                //Check for x5c claim
                harness.HttpManager.AddMockHandler(
                    CreateTokenResponseHttpHandlerWithX5CValidation(false, exportedCertificate));

                var result = await app
                    .AcquireTokenSilent(
                        new[] { "someTestScope"},
                        new Account(TestConstants.s_userIdentifier, TestConstants.DisplayableId, null))
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Internal.Analyzers", "IA5352:DoNotMisuseCryptographicApi", Justification = "Suppressing RoslynAnalyzers: Rule: IA5352 - Do Not Misuse Cryptographic APIs in test only code")]
        public void CheckJWTHeaderWithCertTrueTest()
        {
            var cert = new X509Certificate2(
                ResourceHelper.GetTestResourceRelativePath("testCert.crtfile"), TestConstants.TestCertPassword);

            var header = new JWTHeaderWithCertificate(cert, Base64UrlHelpers.Encode(cert.GetCertHash()), true);

            Assert.IsNotNull(header.X509CertificatePublicCertValue);
            Assert.IsNotNull(header.X509CertificateThumbprint);
        }

        [TestMethod]
        [Description("Check the JWTHeader when sendCert is false")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Internal.Analyzers", "IA5352:DoNotMisuseCryptographicApi", Justification = "Suppressing RoslynAnalyzers: Rule: IA5352 - Do Not Misuse Cryptographic APIs in test only code")]
        public void CheckJWTHeaderWithCertFalseTest()
        {
            var cert = new X509Certificate2(
                 ResourceHelper.GetTestResourceRelativePath("testCert.crtfile"), TestConstants.TestCertPassword);

            var header = new JWTHeaderWithCertificate(cert, Base64UrlHelpers.Encode(cert.GetCertHash()), false);

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
    }
}
#endif
