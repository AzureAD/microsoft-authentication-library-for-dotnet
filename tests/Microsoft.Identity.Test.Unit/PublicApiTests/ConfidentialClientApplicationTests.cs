// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore.Internal.Constants;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

#if !ANDROID && !iOS && !WINDOWS_APP // No Confidential Client
namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    [DeploymentItem(@"Resources\valid.crtfile")]
    [DeploymentItem("Resources\\OpenidConfiguration-QueryParams-B2C.json")]
    public class ConfidentialClientApplicationTests
    {
        private byte[] _serializedCache;

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
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
#pragma warning disable CS0618 // Type or member is obsolete
            mockApp.GetAccountsAsync().Returns(users);

            // Now call the substitute
            IEnumerable<IAccount> actualUsers = mockApp.GetAccountsAsync().Result;
#pragma warning restore CS0618 // Type or member is obsolete

            // Check the users property
            Assert.IsNotNull(actualUsers);
            Assert.AreEqual(2, actualUsers.Count());

            Assert.AreEqual("DisplayableId_1", users.First().Username);
            Assert.AreEqual("DisplayableId_2", users.Last().Username);
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

                ConfidentialClientApplication app =
                    ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .BuildConcrete();

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithAuthority(TestConstants.AuthorityUtidTenant)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.AreEqual(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single().TenantId, TestConstants.Utid);
                Assert.AreEqual(1, app.InMemoryPartitionedCacheSerializer.CachePartition.Count);
                Assert.IsTrue(app.InMemoryPartitionedCacheSerializer.CachePartition.Keys.Single().Contains(TestConstants.Utid));

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithAuthority(TestConstants.AuthorityUtid2Tenant)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single(at => at.TenantId == TestConstants.Utid2));
                Assert.AreEqual(2, app.InMemoryPartitionedCacheSerializer.CachePartition.Count);
                Assert.IsTrue(app.InMemoryPartitionedCacheSerializer.CachePartition.Keys.Any(k => k.Contains(TestConstants.Utid)));
                Assert.IsTrue(app.InMemoryPartitionedCacheSerializer.CachePartition.Keys.Any(k => k.Contains(TestConstants.Utid2)));

            }
        }

        [TestMethod]
        public async Task ClientCreds_DefaultSerialization_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                ConfidentialClientApplication app =
                    ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .BuildConcrete();

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                // Will use the partitioned dictionary for token 
                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithAuthority(TestConstants.AuthorityUtidTenant)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.AreEqual(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single().TenantId, TestConstants.Utid);
                Assert.AreEqual(1, app.InMemoryPartitionedCacheSerializer.CachePartition.Count);
                Assert.IsTrue(app.InMemoryPartitionedCacheSerializer.CachePartition.Keys.Single().Contains(TestConstants.Utid));

                // memory cache should no longer be used
                InMemoryTokenCache inMemoryTokenCache = new InMemoryTokenCache();
                inMemoryTokenCache.Bind(app.AppTokenCache);

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithAuthority(TestConstants.AuthorityUtidTenant)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(result.AuthenticationResultMetadata.TokenSource == TokenSource.IdentityProvider);
                Assert.AreEqual(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single().TenantId, TestConstants.Utid);
            }
        }

        [TestMethod]
        [WorkItem(1403)] // https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1403
        public async Task DefaultScopesForS2SAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .BuildConcrete();

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
            SignedAssertion,
            SignedAssertionDelegate
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

            ConfidentialClientApplication app;

            switch (credentialType)
            {
                case CredentialType.CertificateAndClaims:
                    builder = builder.WithClientClaims(cert, TestConstants.s_clientAssertionClaims);
                    app = builder.BuildConcrete();
                    Assert.AreEqual(cert, app.Certificate);
                    break;
                case CredentialType.SignedAssertion:
                    builder = builder.WithClientAssertion(TestConstants.DefaultClientAssertion);
                    app = builder.BuildConcrete();
                    Assert.IsNull(app.Certificate);
                    break;
                case CredentialType.SignedAssertionDelegate:
                    builder = builder.WithClientAssertion(() => { return TestConstants.DefaultClientAssertion; });
                    app = builder.BuildConcrete();
                    Assert.IsNull(app.Certificate);
                    break;
                case CredentialType.Certificate:
                default:
                    builder = builder.WithCertificate(cert);
                    app = builder.BuildConcrete();
                    Assert.AreEqual(cert, app.Certificate);
                    break;
            }


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
                var app = CreateConfidentialClient(httpManager, cert, 1);
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
            }
        }

        [TestMethod]
        public async Task ClientCreds_And_Obo_DoNotAllow_EmptyScopes_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                var cca = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(httpManager)
                    .WithClientSecret("secret")
                    .Build();

                // OBO
                var ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                    () => cca.AcquireTokenOnBehalfOf(null, new UserAssertion("assertion", "assertiontype")).ExecuteAsync())
                    .ConfigureAwait(false);

                Assert.AreEqual(MsalError.ScopesRequired, ex.ErrorCode);

                // Client Creds
                ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                    () => cca.AcquireTokenForClient(null).ExecuteAsync())
                    .ConfigureAwait(false);

                Assert.AreEqual(MsalError.ScopesRequired, ex.ErrorCode);
            }
        }

        [TestMethod]
        public async Task ConfidentialClientUsingClientAssertionClaimsTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cert = new X509Certificate2(ResourceHelper.GetTestResourceRelativePath("valid.crtfile"));
                var app = CreateConfidentialClient(httpManager, cert, 0, CredentialType.CertificateAndClaims);
                var tokenHttpHandler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

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


                var actualAssertion = tokenHttpHandler.ActualRequestPostData["client_assertion"];

                // assert client credential

                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(actualAssertion);
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
        public async Task ConfidentialClientUsingSignedClientAssertionDelegateTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cert = new X509Certificate2(ResourceHelper.GetTestResourceRelativePath("valid.crtfile"));
                var app = CreateConfidentialClient(httpManager, null, 1, CredentialType.SignedAssertionDelegate);

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
                Assert.IsNotNull(app.ClientCredential.SignedAssertionDelegate);
                Assert.AreEqual(TestConstants.DefaultClientAssertion, app.ClientCredential.SignedAssertionDelegate());
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

                var uri = await app
                    .GetAuthorizationRequestUrl(TestConstants.s_scope)
                    .WithLoginHint(TestConstants.DisplayableId)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(uri);
                Dictionary<string, string> qp = CoreHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', true, null);
                ValidateCommonQueryParams(qp);
                CollectionAssert.AreEquivalent(
                    "offline_access openid profile r1/scope1 r1/scope2".Split(' '),
                    qp["scope"].Split(' '));
            }
        }

        [TestMethod]
        public async Task GetAuthorizationRequestUrl_IgnoreLoginHint_UseCcsRoutingHint_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                ConfidentialClientApplication app = CreateCca(httpManager);

                var uri = await app
                    .GetAuthorizationRequestUrl(TestConstants.s_scope)
                    .WithLoginHint(TestConstants.DisplayableId)
                    .WithCcsRoutingHint("oid", "tid")
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                AssertCcsHint(uri, "oid:oid@tid");
            }
        }

        [TestMethod]
        public async Task GetAuthorizationRequestUrl_UseCcsRoutingHint_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                ConfidentialClientApplication app = CreateCca(httpManager);

                var uri = await app
                    .GetAuthorizationRequestUrl(TestConstants.s_scope)
                    .WithCcsRoutingHint("oid", "tid")
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                AssertCcsHint(uri, "oid:oid@tid");
            }
        }

        [TestMethod]
        public async Task GetAuthorizationRequestUrl_WithLoginHint_UseLoginHintForCcsRoutingHint_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                ConfidentialClientApplication app = CreateCca(httpManager);

                var uri = await app
                    .GetAuthorizationRequestUrl(TestConstants.s_scope)
                    .WithLoginHint(TestConstants.DisplayableId)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                AssertCcsHint(uri, $"upn:{TestConstants.DisplayableId}");
            }
        }

        [TestMethod]
        public async Task GetAuthorizationRequestUrl_NoHint_NoCcsRoutingHint_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                ConfidentialClientApplication app = CreateCca(httpManager);

                var uri = await app
                    .GetAuthorizationRequestUrl(TestConstants.s_scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(uri);
                Dictionary<string, string> qp = CoreHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', true, null);
                Assert.IsFalse(qp.ContainsKey(Constants.CcsRoutingHintHeader));
            }
        }

        [TestMethod]
        public async Task DoNotUseNullCcsRoutingHint_TestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                ConfidentialClientApplication app = CreateCca(httpManager);

                var uri = await app
                    .GetAuthorizationRequestUrl(TestConstants.s_scope)
                    .WithCcsRoutingHint("", "")
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                AssertCcsHint(uri, "");
            }
        }

        private static void AssertCcsHint(Uri uri, string ccsHint)
        {
            Assert.IsNotNull(uri);
            Dictionary<string, string> qp = CoreHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', true, null);

            if (!string.IsNullOrEmpty(ccsHint))
            {
                Assert.IsTrue(qp.ContainsKey(Constants.CcsRoutingHintHeader));
                Assert.AreEqual(ccsHint, qp[Constants.CcsRoutingHintHeader]);
            }
            else
            {
                Assert.IsTrue(!qp.ContainsKey(Constants.CcsRoutingHintHeader));
            }
        }

        private static ConfidentialClientApplication CreateCca(MockHttpManager httpManager)
        {
            httpManager.AddInstanceDiscoveryMockHandler();

            var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                          .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                          .WithRedirectUri(TestConstants.RedirectUri)
                                                          .WithClientSecret(TestConstants.ClientSecret)
                                                          .WithHttpManager(httpManager)
                                                          .BuildConcrete();
            return app;
        }

        [TestMethod]
        public async Task GetAuthorizationRequestUrlWithPKCETestAsync()
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

                string codeVerifier = string.Empty;
                var uri = await app
                    .GetAuthorizationRequestUrl(TestConstants.s_scope)
                    .WithLoginHint(TestConstants.DisplayableId)
                    .WithPkce(out codeVerifier)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(uri);
                Dictionary<string, string> qp = CoreHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', true, null);
                ValidateCommonQueryParams(qp);
                CollectionAssert.AreEquivalent(
                    "offline_access openid profile r1/scope1 r1/scope2".Split(' '),
                    qp["scope"].Split(' '));

                httpManager.AddInstanceDiscoveryMockHandler();
                var handler = httpManager.AddSuccessTokenResponseMockHandlerForPost();
                handler.ExpectedPostData = new Dictionary<string, string>()
                {
                    //Ensure that the code verifier is sent along with the auth code request
                    { "code_verifier", codeVerifier }
                };

                //Ensure that the code verifier returned matches the codeChallenge returned in the URL
                var codeChallenge = TestCommon.CreateDefaultServiceBundle().PlatformProxy.CryptographyManager.CreateBase64UrlEncodedSha256Hash(codeVerifier);
                Assert.AreEqual(codeChallenge, qp[OAuth2Parameter.CodeChallenge]);

                await app.AcquireTokenByAuthorizationCode(TestConstants.s_scope, TestConstants.DefaultAuthorizationCode)
                    .WithPkceCodeVerifier(codeVerifier)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
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
                    string expectedError = string.Format(CultureInfo.InvariantCulture,
                        MsalErrorMessage.DuplicateQueryParameterTemplate,
                        TestConstants.LoginHintParam);
                    Assert.AreEqual(MsalError.DuplicateQueryParameterError, exc.ErrorCode);
                    Assert.AreEqual(expectedError, exc.Message);
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
                CollectionAssert.AreEquivalent(
                    "offline_access openid profile r1/scope1 r1/scope2 r2/scope1 r2/scope2".Split(' '),
                    qp["scope"].Split(' '));
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

#if DESKTOP
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

                // add mock response bigger than 1MB for HTTP Client
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

                TokenCacheHelper.PopulateDefaultAppTokenCache(app);

                // Don't add mock to fail in case of network call
                // If there's a network call by mistake, then there won't be a proper number
                // of mock web request/response objects in the queue and we'll fail.

                var result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .WithForceRefresh(false)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                var accessTokens = app.AppTokenCacheInternal.Accessor.GetAllAccessTokens();
                var accessTokenInCache = accessTokens
                                         .Where(item => ScopeHelper.ScopeContains(item.ScopeSet, TestConstants.s_scope))
                                         .ToList().FirstOrDefault();

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

                TokenCacheHelper.PopulateCache(app.AppTokenCacheInternal.Accessor);

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
                var accessTokens = app.AppTokenCacheInternal.Accessor.GetAllAccessTokens();
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

                app.UserTokenCacheInternal.Accessor.Clear();
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
        // Regression test for https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1193
        public async Task GetAuthorizationRequestUrl_ReturnsUri_Async()
        {
            string[] s_userReadScope = { "User.Read" };

            var cca = ConfidentialClientApplicationBuilder
                   .Create(TestConstants.ClientId)
                   .WithClientSecret("secret")
                   .WithRedirectUri(TestConstants.RedirectUri)
                   .Build();

            var uri1 = await cca.GetAuthorizationRequestUrl(s_userReadScope).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            var uri2 = await cca.GetAuthorizationRequestUrl(s_userReadScope).ExecuteAsync().ConfigureAwait(false);

            Assert.AreEqual(uri1.Host, uri2.Host);
            Assert.AreEqual(uri1.LocalPath, uri2.LocalPath);

            var uriParams1 = uri1.ParseQueryString();
            var uriParams2 = uri2.ParseQueryString();

            CollectionAssert.AreEquivalent(
                "offline_access openid profile User.Read".Split(' '),
                uriParams1["scope"].Split(' '));
            CollectionAssert.AreEquivalent(
                "offline_access openid profile User.Read".Split(' '),
                uriParams2["scope"].Split(' '));
            CoreAssert.AreEqual("code", uriParams1["response_type"], uriParams2["response_type"]);
            CoreAssert.AreEqual(TestConstants.ClientId, uriParams1["client_id"], uriParams2["client_id"]);
            CoreAssert.AreEqual(TestConstants.RedirectUri, uriParams1["redirect_uri"], uriParams2["redirect_uri"]);
            CoreAssert.AreEqual("select_account", uriParams1["prompt"], uriParams2["prompt"]);

            Assert.AreEqual(uriParams1["x-client-CPU"], uriParams2["x-client-CPU"]);
            Assert.AreEqual(uriParams1["x-client-OS"], uriParams2["x-client-OS"]);
            Assert.AreEqual(uriParams1["x-client-Ver"], uriParams2["x-client-Ver"]);
            Assert.AreEqual(uriParams1["x-client-SKU"], uriParams2["x-client-SKU"]);
        }

        [TestMethod]
        public void EnsurePublicApiSurfaceExistsOnInterface()
        {
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                                     .WithClientSecret("cats")
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
                                       .WithSendX5C(true)
                                       .WithCacheKey("obo-cache-key");
            PublicClientApplicationTests.CheckBuilderCommonMethods(onBehalfOfBuilder);

            var silentBuilder = app.AcquireTokenSilent(TestConstants.s_scope, "user@contoso.com")
                .WithForceRefresh(false);

            PublicClientApplicationTests.CheckBuilderCommonMethods(silentBuilder);

            silentBuilder = app.AcquireTokenSilent(TestConstants.s_scope, TestConstants.s_user)
               .WithForceRefresh(true);
            PublicClientApplicationTests.CheckBuilderCommonMethods(silentBuilder);

            var requestUrlBuilder = app.GetAuthorizationRequestUrl(TestConstants.s_scope)
                                       .WithAccount(TestConstants.s_user)
                                       .WithLoginHint(TestConstants.LoginHint)
                                       .WithExtraScopesToConsent(TestConstants.s_scope)
                                       .WithRedirectUri(TestConstants.RedirectUri);
            PublicClientApplicationTests.CheckBuilderCommonMethods(requestUrlBuilder);

            var byRefreshTokenBuilder = ((IByRefreshToken)app).AcquireTokenByRefreshToken(TestConstants.s_scope, "refreshtoken")
                                                              .WithRefreshToken("refreshtoken");
            PublicClientApplicationTests.CheckBuilderCommonMethods(byRefreshTokenBuilder);
        }

        [TestMethod]
        public async Task ConfidentialClientSuggestedExpiryAsync()
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

                string expectedTimeDiff = "32800";
                string shortExpectedTimeDiff = "30800";

                //Since the timeDiff is calculated using the DateTimeOffset.Now, there may be a slight variation of a few seconds depending on test environment.
                int allowedTimeVariation = 15;

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(TestConstants.DefaultAccessToken, expectedTimeDiff);

                TokenCacheHelper.PopulateDefaultAppTokenCache(app);
                app.AppTokenCache.SetAfterAccess((args) =>
                {
                    if (args.HasStateChanged == true)
                    {
                        Assert.IsNotNull(args.SuggestedCacheExpiry);
                        var timeDiff = args.SuggestedCacheExpiry.Value.ToUnixTimeSeconds() - DateTimeOffset.Now.ToUnixTimeSeconds();

                        CoreAssert.AreEqual(
                            CoreHelpers.UnixTimestampStringToDateTime(expectedTimeDiff),
                            CoreHelpers.UnixTimestampStringToDateTime(timeDiff.ToString()),
                            TimeSpan.FromSeconds(allowedTimeVariation));
                    }
                });

                //Token cache will have one token with expiry of 1000
                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(result);
                Assert.IsNotNull(TestConstants.DefaultAccessToken, result.AccessToken);

                //Using a shorter expiry should not change the SuggestedCacheExpiry. Should be 2 tokens in cache
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(TestConstants.DefaultAccessToken, shortExpectedTimeDiff);
                result = await app.AcquireTokenForClient(new[] { "scope1.scope1" }).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(result);
                Assert.IsNotNull(TestConstants.DefaultAccessToken, result.AccessToken);

                //Using longer cache expiry should update the SuggestedCacheExpiry with 3 tokens in cache
                expectedTimeDiff = "40000";
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(TestConstants.DefaultAccessToken, expectedTimeDiff);
                result = await app.AcquireTokenForClient(new[] { "scope2.scope2" }).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(result);
                Assert.IsNotNull(TestConstants.DefaultAccessToken, result.AccessToken);
            }
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
