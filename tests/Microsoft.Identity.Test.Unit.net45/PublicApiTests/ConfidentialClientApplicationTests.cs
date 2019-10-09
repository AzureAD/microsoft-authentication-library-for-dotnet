// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using System.Threading;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.TelemetryCore.Internal.Constants;
using System.IdentityModel.Tokens.Jwt;

#if !ANDROID && !iOS && !WINDOWS_APP // No Confidential Client
namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    [DeploymentItem(@"Resources\valid.crtfile")]
    [DeploymentItem("Resources\\OpenidConfiguration-QueryParams-B2C.json")]
    public class ConfidentialClientApplicationTests
    {
        private byte[] _serializedCache;
        private TokenCacheHelper _tokenCacheHelper;

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
            _tokenCacheHelper = new TokenCacheHelper();
        }

        [TestMethod]
        [Description("Tests the public interfaces can be mocked")]
        [Ignore("Bug 1001, as we deprecate public API, new methods aren't mockable.  Working on prototype.")]
        public void MockConfidentialClientApplication_AcquireToken()
        {
            // Setup up a confidential client application that returns a dummy result
            var mockResult = new AuthenticationResult(
                "",
                false,
                "",
                DateTimeOffset.Now,
                DateTimeOffset.Now,
                "",
                null,
                "id token",
                new[]
                {
                    "scope1",
                    "scope2"
                },
                Guid.NewGuid());

            var mockApp = Substitute.For<IConfidentialClientApplication>();
            mockApp.AcquireTokenByAuthorizationCode(null, "123").ExecuteAsync(CancellationToken.None).Returns(mockResult);

            // Now call the substitute with the args to get the substitute result
            var actualResult = mockApp.AcquireTokenByAuthorizationCode(null, "123").ExecuteAsync(CancellationToken.None).Result;
            Assert.IsNotNull(actualResult);
            Assert.AreEqual("id token", mockResult.IdToken, "Mock result failed to return the expected id token");
            // Check the scope property
            IEnumerable<string> scopes = actualResult.Scopes;
            Assert.IsNotNull(scopes);
            Assert.AreEqual("scope1", scopes.First());
            Assert.AreEqual("scope2", scopes.Last());
        }

        [TestMethod]
        [Description("Tests the public interfaces can be mocked")]
        public void MockConfidentialClientApplication_Users()
        {
            // Setup up a confidential client application with mocked users
            var mockApp = Substitute.For<IConfidentialClientApplication>();
            IList<IAccount> users = new List<IAccount>();

            var mockUser1 = Substitute.For<IAccount>();
            mockUser1.Username.Returns("DisplayableId_1");

            var mockUser2 = Substitute.For<IAccount>();
            mockUser2.Username.Returns("DisplayableId_2");

            users.Add(mockUser1);
            users.Add(mockUser2);
            mockApp.GetAccountsAsync().Returns(users);

            // Now call the substitute
            IEnumerable<IAccount> actualUsers = mockApp.GetAccountsAsync().Result;

            // Check the users property
            Assert.IsNotNull(actualUsers);
            Assert.AreEqual(2, actualUsers.Count());

            Assert.AreEqual("DisplayableId_1", users.First().Username);
            Assert.AreEqual("DisplayableId_2", users.Last().Username);
        }

        [TestMethod]
        [Description("Tests the public application interfaces can be mocked to throw MSAL exceptions")]
        [Ignore("Bug 1001, as we deprecate public API, new methods aren't mockable.  Working on prototype.")]
        public void MockConfidentialClientApplication_Exception()
        {
            // Setup up a confidential client application that returns throws
            var mockApp = Substitute.For<IConfidentialClientApplication>();
            mockApp
                .WhenForAnyArgs(x => x.AcquireTokenForClient(Arg.Any<string[]>()).ExecuteAsync(CancellationToken.None))
                .Do(x => throw new MsalServiceException("my error code", "my message", new HttpRequestException()));

            // Now call the substitute and check the exception is thrown
            var ex = AssertException.Throws<MsalServiceException>(
                () => mockApp
                    .AcquireTokenForClient(new string[] { "scope1" })
                    .ExecuteAsync(CancellationToken.None));
            Assert.AreEqual("my error code", ex.ErrorCode);
            Assert.AreEqual("my message", ex.Message);
        }

        [TestMethod]
        public void ConstructorsTest()
        {
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount)
                .WithRedirectUri(TestConstants.RedirectUri)
                .WithClientSecret(TestConstants.ClientSecret)
                .BuildConcrete();

            Assert.IsNotNull(app);
            Assert.IsNotNull(app.UserTokenCache);
            Assert.IsNotNull(app.AppTokenCache);
            Assert.AreEqual("https://login.microsoftonline.com/common/", app.Authority);
            Assert.AreEqual(TestConstants.ClientId, app.AppConfig.ClientId);
            Assert.AreEqual(TestConstants.RedirectUri, app.AppConfig.RedirectUri);
            Assert.AreEqual("https://login.microsoftonline.com/common/", app.Authority);
            Assert.IsNotNull(app.ClientCredential);
            Assert.IsNotNull(app.ClientCredential.Secret);
            Assert.AreEqual(TestConstants.ClientSecret, app.ClientCredential.Secret);
            Assert.IsNull(app.ClientCredential.Certificate);
            Assert.IsNull(app.ClientCredential.CachedAssertion);

            app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(new Uri(TestConstants.AuthorityGuestTenant), true)
                .WithRedirectUri(TestConstants.RedirectUri).WithClientSecret("secret")
                .BuildConcrete();

            Assert.AreEqual(TestConstants.AuthorityGuestTenant, app.Authority);

            app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                      .WithAdfsAuthority(TestConstants.OnPremiseAuthority, true)
                                                      .WithRedirectUri(TestConstants.RedirectUri)
                                                      .WithClientSecret(TestConstants.s_onPremiseCredentialWithSecret.Secret)
                                                      .BuildConcrete();


            Assert.AreEqual(TestConstants.OnPremiseAuthority, app.Authority);
        }

        [TestMethod]
        public void TestConstructorWithNullRedirectUri()
        {
            var app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(ClientApplicationBase.DefaultAuthority)
                .WithRedirectUri(null)
                .WithClientSecret("the_secret")
                .BuildConcrete();

            Assert.AreEqual(Constants.DefaultConfidentialClientRedirectUri, app.AppConfig.RedirectUri);
        }

        [TestMethod]
        public async Task ConfidentialClientUsingSecretNoCacheProvidedTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                              .WithRedirectUri(TestConstants.RedirectUri)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .BuildConcrete();

                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                httpManager.AddMockHandlerForTenantEndpointDiscovery(app.Authority);
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(result);
                Assert.IsNotNull("header.payload.signature", result.AccessToken);
                Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());

                Assert.IsNotNull(app.UserTokenCache);
                Assert.IsNotNull(app.AppTokenCache);

                appCacheAccess.AssertAccessCounts(1, 1);
                userCacheAccess.AssertAccessCounts(0, 0);
            }
        }


        [TestMethod]
        [TestCategory("Regression")]
        [WorkItem(1365)] // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1365
        public async Task ClientCreds_MustFilterByTenantId_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .BuildConcrete();

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityUtidTenant);
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithAuthority(TestConstants.AuthorityUtidTenant)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.AreEqual(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single().TenantId, TestConstants.Utid);

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityUtid2Tenant);
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithAuthority(TestConstants.AuthorityUtid2Tenant)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.AreEqual(2, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.IsNotNull(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single(at => at.TenantId == TestConstants.Utid2));
            }
        }

        [TestMethod]
        [WorkItem(1403)] // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1403
        public async Task FooAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .BuildConcrete();

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityUtidTenant);
                var handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                handler.ExpectedPostData = new Dictionary<string, string>()
                {
                    // Bug 1403: Do not add reserved scopes profile, offline_access and openid to Confidential Client request
                    { "scope", TestConstants.s_scope.AsSingleString() } 
                };

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithAuthority(TestConstants.AuthorityUtidTenant)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task ConfidentialClientUsingSecretTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                              .WithRedirectUri(TestConstants.RedirectUri)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .BuildConcrete();

                httpManager.AddMockHandlerForTenantEndpointDiscovery(app.Authority);
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(result);
                Assert.IsNotNull("header.payload.signature", result.AccessToken);
                Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());

                // make sure user token cache is empty
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());

                // check app token cache count to be 1
                Assert.AreEqual(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(0, app.AppTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());

                appCacheAccess.AssertAccessCounts(1, 1);
                userCacheAccess.AssertAccessCounts(0, 0);

                // call AcquireTokenForClientAsync again to get result back from the cache
                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(result);
                Assert.IsNotNull("header.payload.signature", result.AccessToken);
                Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());

                // make sure user token cache is empty
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());

                // check app token cache count to be 1
                Assert.AreEqual(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(0, app.AppTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());

                appCacheAccess.AssertAccessCounts(2, 1);
                userCacheAccess.AssertAccessCounts(0, 0);
            }
        }

        [TestMethod]
        public async Task ConfidentialClientUsingAdfsAsync()
        {
            using (var httpManager = new MockHttpManager())
            {

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithAuthority(new Uri(TestConstants.OnPremiseAuthority), true)
                                                              .WithRedirectUri(TestConstants.RedirectUri)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .BuildConcrete();
                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                httpManager.AddMockHandler(
                new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Get,
                    ExpectedUrl = "https://fs.contoso.com/.well-known/webfinger",
                    ExpectedQueryParams = new Dictionary<string, string>
                    {
                                            {"resource", "https://fs.contoso.com"},
                                            {"rel", "http://schemas.microsoft.com/rel/trusted-realm"}
                    },
                    ResponseMessage = MockHelpers.CreateSuccessWebFingerResponseMessage("https://fs.contoso.com")
                });

                httpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Get,
                    ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.OnPremiseAuthority)
                });

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync().ConfigureAwait(false);
                Assert.IsNotNull(result);
                Assert.IsNotNull("header.payload.signature", result.AccessToken);
                Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());
                appCacheAccess.AssertAccessCounts(1, 1);
                userCacheAccess.AssertAccessCounts(0, 0);

                // make sure user token cache is empty
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());

                // check app token cache count to be 1
                Assert.AreEqual(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(0, app.AppTokenCacheInternal.Accessor.GetAllRefreshTokens().Count()); // no refresh tokens are returned

                // call AcquireTokenForClientAsync again to get result back from the cache
                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync().ConfigureAwait(false);
                Assert.IsNotNull(result);
                Assert.IsNotNull("header.payload.signature", result.AccessToken);
                Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());

                // make sure user token cache is empty
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());

                // check app token cache count to be 1
                Assert.AreEqual(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(0, app.AppTokenCacheInternal.Accessor.GetAllRefreshTokens().Count()); // no refresh tokens are returned
                appCacheAccess.AssertAccessCounts(2, 1);
                userCacheAccess.AssertAccessCounts(0, 0);

            }
        }

        private enum CredentialType
        {
            Certificate,
            CertificateAndClaims,
            SignedAssertion
        }

        private ConfidentialClientApplication CreateConfidentialClient(
            MockHttpManager httpManager,
            X509Certificate2 cert,
            int tokenResponses,
            CredentialType credentialType = CredentialType.Certificate,
            TelemetryCallback telemetryCallback = null)
        {
            var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                              .WithRedirectUri(TestConstants.RedirectUri)
                              .WithHttpManager(httpManager)
                              .WithTelemetry(telemetryCallback);

            switch (credentialType)
            {
                case CredentialType.CertificateAndClaims:
                    builder = builder.WithClientClaims(cert, TestConstants.s_clientAssertionClaims);
                    break;
                case CredentialType.SignedAssertion:
                    builder = builder.WithClientAssertion(TestConstants.DefaultClientAssertion);
                    break;
                case CredentialType.Certificate:
                default:
                    builder = builder.WithCertificate(cert);
                    break;
            }

            var app = builder.BuildConcrete();

            httpManager.AddMockHandlerForTenantEndpointDiscovery(app.Authority);

            for (int i = 0; i < tokenResponses; i++)
            {
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
            }

            return app;
        }

        [TestMethod]
        public async Task ConfidentialClientUsingCertificateTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cert = new X509Certificate2(ResourceHelper.GetTestResourceRelativePath("valid.crtfile"));
                var app = CreateConfidentialClient(httpManager, cert, 3);
                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(result);
                Assert.IsNotNull("header.payload.signature", result.AccessToken);
                Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());
                appCacheAccess.AssertAccessCounts(1, 1);
                userCacheAccess.AssertAccessCounts(0, 0);

                // make sure user token cache is empty
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());

                // check app token cache count to be 1
                Assert.AreEqual(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(0, app.AppTokenCacheInternal.Accessor.GetAllRefreshTokens().Count()); // no RTs are returned

                // assert client credential

                Assert.IsNotNull(app.ClientCredential.CachedAssertion);
                Assert.AreNotEqual(0, app.ClientCredential.ValidTo);

                // save client assertion.
                string cachedAssertion = app.ClientCredential.CachedAssertion;
                long cacheValidTo = app.ClientCredential.ValidTo;

                result = await app
                    .AcquireTokenForClient(TestConstants.s_scopeForAnotherResource.ToArray())
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                appCacheAccess.AssertAccessCounts(2, 2);
                userCacheAccess.AssertAccessCounts(0, 0);

                Assert.IsNotNull(result);
                Assert.AreEqual(cacheValidTo, app.ClientCredential.ValidTo);
                Assert.AreEqual(cachedAssertion, app.ClientCredential.CachedAssertion);

                // validate the send x5c forces a refresh of the cached client assertion
                await app
                      .AcquireTokenForClient(TestConstants.s_scope.ToArray())
                      .WithSendX5C(true)
                      .WithForceRefresh(true)
                      .ExecuteAsync(CancellationToken.None)
                      .ConfigureAwait(false);
                Assert.AreNotEqual(cachedAssertion, app.ClientCredential.CachedAssertion);

                appCacheAccess.AssertAccessCounts(2, 3);
                userCacheAccess.AssertAccessCounts(0, 0);
            }
        }

        [TestMethod]
        public async Task ConfidentialClientUsingClientAssertionClaimsTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cert = new X509Certificate2(ResourceHelper.GetTestResourceRelativePath("valid.crtfile"));
                var app = CreateConfidentialClient(httpManager, cert, 1, CredentialType.CertificateAndClaims);
                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(result);
                Assert.IsNotNull("header.payload.signature", result.AccessToken);
                Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());
                appCacheAccess.AssertAccessCounts(1, 1);
                userCacheAccess.AssertAccessCounts(0, 0);

                // make sure user token cache is empty
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());

                // check app token cache count to be 1
                Assert.AreEqual(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(0, app.AppTokenCacheInternal.Accessor.GetAllRefreshTokens().Count()); // no RTs are returned

                // assert client credential

                Assert.IsNotNull(app.ClientCredential.CachedAssertion);
                Assert.AreNotEqual(0, app.ClientCredential.ValidTo);

                // save client assertion.
                string cachedAssertion = app.ClientCredential.CachedAssertion;
                long cacheValidTo = app.ClientCredential.ValidTo;

                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(((ConfidentialClientApplication)app).ClientCredential.CachedAssertion);
                var claims = jsonToken.Claims;
                //checked if additional claim is in signed assertion
                var audclaim = TestConstants.s_clientAssertionClaims.Where(x => x.Key == "aud").FirstOrDefault();
                var validClaim = claims.Where(x => x.Type == audclaim.Key && x.Value == audclaim.Value).FirstOrDefault();
                Assert.IsNotNull(validClaim);
            }
        }

        [TestMethod]
        public async Task ConfidentialClientUsingSignedClientAssertionTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cert = new X509Certificate2(ResourceHelper.GetTestResourceRelativePath("valid.crtfile"));
                var app = CreateConfidentialClient(httpManager, cert, 1, CredentialType.SignedAssertion);

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(result);
                Assert.IsNotNull("header.payload.signature", result.AccessToken);
                Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());

                // make sure user token cache is empty
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());

                // check app token cache count to be 1
                Assert.AreEqual(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(0, app.AppTokenCacheInternal.Accessor.GetAllRefreshTokens().Count()); // no RTs are returned

                // assert client credential

                Assert.IsNotNull(app.ClientCredential.SignedAssertion);

            }
        }

        [TestMethod]
        public async Task ConfidentialClientUsingCertificateTelemetryTestAsync()
        {
            var receiver = new MyReceiver();

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cert = new X509Certificate2(ResourceHelper.GetTestResourceRelativePath("valid.crtfile"));
                var app = CreateConfidentialClient(httpManager, cert, 1, CredentialType.Certificate, receiver.HandleTelemetryEvents);
                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(
                    receiver.EventsReceived.Find(
                        anEvent => // Expect finding such an event
                            anEvent[EventBase.EventNameKey].EndsWith("http_event") &&
                            anEvent[HttpEvent.ResponseCodeKey] == "200" && anEvent[HttpEvent.HttpPathKey]
                                .Contains(
                                    EventBase
                                        .TenantPlaceHolder) // The tenant info is expected to be replaced by a holder
                    ));

                Assert.IsNotNull(
                    receiver.EventsReceived.Find(
                        anEvent => // Expect finding such an event
                            anEvent[EventBase.EventNameKey].EndsWith("api_event") &&
                            anEvent[ApiEvent.WasSuccessfulKey] == "true" && anEvent[MsalTelemetryBlobEventNames.ApiIdConstStrKey] == "1004"));
            }
        }

        [TestMethod]
        public async Task GetAuthorizationRequestUrlNoRedirectUriTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                              .WithRedirectUri(TestConstants.RedirectUri)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .BuildConcrete();

                httpManager.AddMockHandlerForTenantEndpointDiscovery(app.Authority);

                var uri = await app
                    .GetAuthorizationRequestUrl(TestConstants.s_scope)
                    .WithLoginHint(TestConstants.DisplayableId)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(uri);
                Dictionary<string, string> qp = CoreHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', true, null);
                ValidateCommonQueryParams(qp);
                Assert.AreEqual("offline_access openid profile r1/scope1 r1/scope2", qp["scope"]);
            }
        }

        [TestMethod]
        public async Task GetAuthorizationRequestUrlB2CTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                              .WithRedirectUri(TestConstants.RedirectUri)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .BuildConcrete();

                // add mock response for tenant endpoint discovery
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(
                            File.ReadAllText(
                                ResourceHelper.GetTestResourceRelativePath(@"OpenidConfiguration-QueryParams-B2C.json")))
                    });

                var uri = await app
                    .GetAuthorizationRequestUrl(TestConstants.s_scope)
                    .WithLoginHint(TestConstants.DisplayableId)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(uri);
                Dictionary<string, string> qp = CoreHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', true, null);
                Assert.IsNotNull(qp);

                Assert.AreEqual("my-policy", qp["p"]);
                ValidateCommonQueryParams(qp);
                Assert.AreEqual("offline_access openid profile r1/scope1 r1/scope2", qp["scope"]);
            }
        }

        [TestMethod]
        public async Task GetAuthorizationRequestUrlDuplicateParamsTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                              .WithRedirectUri(TestConstants.RedirectUri)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .BuildConcrete();

                httpManager.AddMockHandlerForTenantEndpointDiscovery(app.Authority);

                try
                {
                    var uri = await app
                        .GetAuthorizationRequestUrl(TestConstants.s_scope)
                        .WithLoginHint(TestConstants.DisplayableId)
                        .WithExtraQueryParameters("login_hint=some@value.com")
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.Fail("MSALException should be thrown here");
                }
                catch (MsalException exc)
                {
                    Assert.AreEqual("duplicate_query_parameter", exc.ErrorCode);
                    Assert.AreEqual("Duplicate query parameter 'login_hint' in extraQueryParameters", exc.Message);
                }
                catch (Exception ex)
                {
                    Assert.Fail("Wrong type of exception thrown: " + ex);
                }
            }
        }

        [TestMethod]
        public void GetAuthorizationRequestUrlCustomRedirectUriTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                              .WithRedirectUri(TestConstants.RedirectUri)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .BuildConcrete();
                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityGuestTenant);

                const string CustomRedirectUri = "custom://redirect-uri";
                Task<Uri> task = app
                    .GetAuthorizationRequestUrl(TestConstants.s_scope)
                    .WithRedirectUri(CustomRedirectUri)
                    .WithLoginHint(TestConstants.DisplayableId)
                    .WithExtraQueryParameters("extra=qp")
                    .WithExtraScopesToConsent(TestConstants.s_scopeForAnotherResource)
                    .WithAuthority(TestConstants.AuthorityGuestTenant)
                    .ExecuteAsync(CancellationToken.None);

                var uri = task.Result;
                Assert.IsNotNull(uri);
                appCacheAccess.AssertAccessCounts(0, 0);
                userCacheAccess.AssertAccessCounts(0, 0);

                Assert.IsTrue(
                    uri.AbsoluteUri.StartsWith(TestConstants.AuthorityGuestTenant, StringComparison.CurrentCulture));
                Dictionary<string, string> qp = CoreHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', true, null);
                ValidateCommonQueryParams(qp, CustomRedirectUri);
                Assert.AreEqual("offline_access openid profile r1/scope1 r1/scope2 r2/scope1 r2/scope2", qp["scope"]);
                Assert.IsFalse(qp.ContainsKey("client_secret"));
                Assert.AreEqual("qp", qp["extra"]);
            }
        }

        private static void ValidateCommonQueryParams(
            Dictionary<string, string> qp,
            string redirectUri = TestConstants.RedirectUri)
        {
            Assert.IsNotNull(qp);

            Assert.IsTrue(qp.ContainsKey("client-request-id"));
            Assert.AreEqual(TestConstants.ClientId, qp["client_id"]);
            Assert.AreEqual("code", qp["response_type"]);
            Assert.AreEqual(redirectUri, qp["redirect_uri"]);
            Assert.AreEqual(TestConstants.DisplayableId, qp["login_hint"]);
            Assert.AreEqual(Prompt.SelectAccount.PromptValue, qp["prompt"]);
            Assert.AreEqual(TestCommon.CreateDefaultServiceBundle().PlatformProxy.GetProductName(), qp["x-client-sku"]);
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-ver"]));
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-os"]));

#if !NET_CORE
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-cpu"]));
#endif
        }

        [TestMethod]
        public async Task HttpRequestExceptionIsNotSuppressedAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                              .WithRedirectUri(TestConstants.RedirectUri)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .BuildConcrete();

                // add mock response bigger than 1MB for Http Client
                httpManager.AddFailingRequest(new InvalidOperationException());

                await AssertException.TaskThrowsAsync<InvalidOperationException>(
                    () => app.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync(CancellationToken.None)).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task ForceRefreshParameterFalseTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new Uri(TestConstants.AuthorityTestTenant), true)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                _tokenCacheHelper.PopulateCacheForClientCredential(app.AppTokenCacheInternal.Accessor);

                var accessTokens = await app.AppTokenCacheInternal.GetAllAccessTokensAsync(true).ConfigureAwait(false);
                var accessTokenInCache = accessTokens
                                         .Where(item => ScopeHelper.ScopeContains(item.ScopeSet, TestConstants.s_scope))
                                         .ToList().FirstOrDefault();

                // Don't add mock to fail in case of network call
                // If there's a network call by mistake, then there won't be a proper number
                // of mock web request/response objects in the queue and we'll fail.

                var result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .WithForceRefresh(false)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(accessTokenInCache.Secret, result.AccessToken);
            }
        }

        [TestMethod]
        public async Task ForceRefreshParameterTrueTestAsync()
        {
            var receiver = new MyReceiver();

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new Uri(TestConstants.AuthorityTestTenant), true)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithTelemetry(receiver.HandleTelemetryEvents)
                    .BuildConcrete();

                _tokenCacheHelper.PopulateCache(app.AppTokenCacheInternal.Accessor);

                httpManager.AddMockHandlerForTenantEndpointDiscovery(app.Authority);

                // add mock response for successful token retrieval
                const string TokenRetrievedFromNetCall = "token retrieved from network call";
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Post,
                        ResponseMessage =
                            MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage(TokenRetrievedFromNetCall)
                    });

                var result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .WithForceRefresh(true)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenRetrievedFromNetCall, result.AccessToken);

                // make sure token in Cache was updated
                var accessTokens = await app.AppTokenCacheInternal.GetAllAccessTokensAsync(true).ConfigureAwait(false);
                var accessTokenInCache = accessTokens
                                         .Where(item => ScopeHelper.ScopeContains(item.ScopeSet, TestConstants.s_scope))
                                         .ToList().FirstOrDefault();

                Assert.AreEqual(TokenRetrievedFromNetCall, accessTokenInCache.Secret);
                Assert.IsNotNull(
                    receiver.EventsReceived.Find(
                        anEvent => // Expect finding such an event
                            anEvent[EventBase.EventNameKey].EndsWith("api_event") &&
                            anEvent[ApiEvent.WasSuccessfulKey] == "true" && anEvent[MsalTelemetryBlobEventNames.ApiIdConstStrKey] == "1004"));
            }
        }

        [TestMethod]
        [Ignore] // This B2C scenario needs some rethinking
        public async Task AuthorizationCodeRequestTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new Uri("https://" + TestConstants.ProductionPrefNetworkEnvironment + "/tfp/home/policy"), true)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithClientSecret("secret")
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                app.UserTokenCache.SetBeforeAccess(BeforeCacheAccess);
                app.UserTokenCache.SetAfterAccess(AfterCacheAccess);

                httpManager.AddMockHandlerForTenantEndpointDiscovery("https://" + TestConstants.ProductionPrefNetworkEnvironment + "/tfp/home/policy/", "p=policy");
                httpManager.AddSuccessTokenResponseMockHandlerForPost("https://" + TestConstants.ProductionPrefNetworkEnvironment + "/tfp/home/policy/");

                var result = await app
                    .AcquireTokenByAuthorizationCode(TestConstants.s_scope, "some-code")
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());

                app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                          .WithAuthority(new Uri("https://" + TestConstants.ProductionPrefNetworkEnvironment + "/tfp/home/policy"), true)
                                                          .WithRedirectUri(TestConstants.RedirectUri)
                                                          .WithClientSecret("secret")
                                                          .WithHttpManager(httpManager)
                                                          .BuildConcrete();

                app.UserTokenCache.SetBeforeAccess(BeforeCacheAccess);
                app.UserTokenCache.SetAfterAccess(AfterCacheAccess);

                IEnumerable<IAccount> users = await app.GetAccountsAsync().ConfigureAwait(false);
                Assert.AreEqual(1, users.Count());
            }
        }

        [TestMethod]
        public async Task AcquireTokenByRefreshTokenTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityCommonTenant);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new Uri(TestConstants.AuthorityCommonTenant), true)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                var result = await (app as IByRefreshToken)
                    .AcquireTokenByRefreshToken(null, "SomeRefreshToken")
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(result.AccessToken, "some-access-token");

                await app.UserTokenCacheInternal.ClearAsync().ConfigureAwait(false);
                httpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);
                result = await ((IByRefreshToken)app)
                    .AcquireTokenByRefreshToken(TestConstants.s_scope, "SomeRefreshToken")
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(result.AccessToken, "some-access-token");
            }
        }

        [TestMethod]
        public void EnsurePublicApiSurfaceExistsOnInterface()
        {
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                                     .Build();

            // This test is to ensure that the methods we want/need on the IConfidentialClientApplication exist and compile.  This isn't testing functionality, that's done elsewhere.
            // It's solely to ensure we know that the methods we want/need are available where we expect them since we tend to do most testing on the concrete types.

            var authCodeBuilder = app.AcquireTokenByAuthorizationCode(TestConstants.s_scope, "authorizationcode");
            PublicClientApplicationTests.CheckBuilderCommonMethods(authCodeBuilder);

            var clientBuilder = app.AcquireTokenForClient(TestConstants.s_scope)
               .WithForceRefresh(true)
               .WithSendX5C(true);
            PublicClientApplicationTests.CheckBuilderCommonMethods(clientBuilder);

            var onBehalfOfBuilder = app.AcquireTokenOnBehalfOf(
                                           TestConstants.s_scope,
                                           new UserAssertion("assertion", "assertiontype"))
                                       .WithSendX5C(true);
            PublicClientApplicationTests.CheckBuilderCommonMethods(onBehalfOfBuilder);

            var silentBuilder = app.AcquireTokenSilent(TestConstants.s_scope, "user@contoso.com")
                .WithForceRefresh(false);

            PublicClientApplicationTests.CheckBuilderCommonMethods(silentBuilder);

            silentBuilder = app.AcquireTokenSilent(TestConstants.s_scope, TestConstants.s_user)
               .WithForceRefresh(true);
            PublicClientApplicationTests.CheckBuilderCommonMethods(silentBuilder);

            var requestUrlBuilder = app.GetAuthorizationRequestUrl(TestConstants.s_scope)
                                       .WithAccount(TestConstants.s_user)
                                       .WithLoginHint("loginhint")
                                       .WithExtraScopesToConsent(TestConstants.s_scope)
                                       .WithRedirectUri(TestConstants.RedirectUri);
            PublicClientApplicationTests.CheckBuilderCommonMethods(requestUrlBuilder);

            var byRefreshTokenBuilder = ((IByRefreshToken)app).AcquireTokenByRefreshToken(TestConstants.s_scope, "refreshtoken")
                                                              .WithRefreshToken("refreshtoken");
            PublicClientApplicationTests.CheckBuilderCommonMethods(byRefreshTokenBuilder);
        }

        private void BeforeCacheAccess(TokenCacheNotificationArgs args)
        {
            args.TokenCache.DeserializeMsalV3(_serializedCache);
        }

        private void AfterCacheAccess(TokenCacheNotificationArgs args)
        {
            _serializedCache = args.TokenCache.SerializeMsalV3();
        }
    }
}

#endif
