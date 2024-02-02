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
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.ClientCredential;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    [DeploymentItem(@"Resources\valid.crtfile")]
    [DeploymentItem("Resources\\OpenidConfiguration-QueryParams-B2C.json")]
    public class ConfidentialClientApplicationTests : TestBase
    {

        private byte[] _serializedCache;        

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
            Assert.IsNotNull((app.AppConfig as ApplicationConfiguration).ClientCredential);
            Assert.IsNotNull(app.AppConfig.ClientSecret);
            Assert.AreEqual(TestConstants.ClientSecret, app.AppConfig.ClientSecret);
            Assert.IsNull(app.AppConfig.ClientCredentialCertificate);

            app = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(new Uri(TestConstants.AuthorityGuestTenant), true)
                .WithRedirectUri(TestConstants.RedirectUri).WithClientSecret("secret")
                .BuildConcrete();

            Assert.AreEqual(TestConstants.AuthorityGuestTenant, app.Authority);

            app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                      .WithAdfsAuthority(TestConstants.OnPremiseAuthority, true)
                                                      .WithRedirectUri(TestConstants.RedirectUri)
                                                      .WithClientSecret(TestConstants.ClientSecret)
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
        public async Task ConfidentialClientUsingSecretNoInstanceDiscoveryTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                              .WithRedirectUri(TestConstants.RedirectUri)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .WithInstanceDiscovery(false)
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
        [TestCategory(TestCategories.Regression)]
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
                    .WithTenantId(TestConstants.Utid)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.AreEqual(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single().TenantId, TestConstants.Utid);
                string partitionKey = CacheKeyFactory.GetClientCredentialKey(TestConstants.ClientId, TestConstants.Utid, null);
                Assert.AreEqual(
                    partitionKey,
                    ((InMemoryPartitionedAppTokenCacheAccessor)app.AppTokenCacheInternal.Accessor).AccessTokenCacheDictionary.Keys.Single());

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithTenantId(TestConstants.Utid2)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Single(at => at.TenantId == TestConstants.Utid2));
                Assert.AreEqual(2, ((InMemoryPartitionedAppTokenCacheAccessor)app.AppTokenCacheInternal.Accessor).AccessTokenCacheDictionary.Count);
                string partitionKey2 = CacheKeyFactory.GetClientCredentialKey(TestConstants.ClientId, TestConstants.Utid2, null);

                Assert.IsTrue(((InMemoryPartitionedAppTokenCacheAccessor)app.AppTokenCacheInternal.Accessor).AccessTokenCacheDictionary.Keys.Any(k => k.Equals(partitionKey)));
                Assert.IsTrue(((InMemoryPartitionedAppTokenCacheAccessor)app.AppTokenCacheInternal.Accessor).AccessTokenCacheDictionary.Keys.Any(k => k.Equals(partitionKey2)));
            }
        }

        [TestMethod]
        public async Task ClientCreds_UsesDefaultPartitionedCacheCorrectly_Async()
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

                var result = await app.AcquireTokenForClient(new[] { "scope1" })
                    .WithTenantId(TestConstants.Utid)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                // One tenant partition with one token
                Assert.AreEqual(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(1, ((InMemoryPartitionedAppTokenCacheAccessor)app.AppTokenCacheInternal.Accessor).AccessTokenCacheDictionary.Count);
                string partitionKey = CacheKeyFactory.GetClientCredentialKey(TestConstants.ClientId, TestConstants.Utid, null);

                Assert.IsNotNull(((InMemoryPartitionedAppTokenCacheAccessor)app.AppTokenCacheInternal.Accessor).AccessTokenCacheDictionary[partitionKey]);
                Assert.AreEqual(1, ((InMemoryPartitionedAppTokenCacheAccessor)app.AppTokenCacheInternal.Accessor).AccessTokenCacheDictionary[partitionKey].Count);

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                result = await app.AcquireTokenForClient(new[] { "scope2" })
                    .WithTenantId(TestConstants.Utid)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                // One tenant partition with two tokens
                Assert.AreEqual(2, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(1, ((InMemoryPartitionedAppTokenCacheAccessor)app.AppTokenCacheInternal.Accessor).AccessTokenCacheDictionary.Count);
                Assert.IsNotNull(((InMemoryPartitionedAppTokenCacheAccessor)app.AppTokenCacheInternal.Accessor).AccessTokenCacheDictionary[partitionKey]);
                Assert.AreEqual(2, ((InMemoryPartitionedAppTokenCacheAccessor)app.AppTokenCacheInternal.Accessor).AccessTokenCacheDictionary[partitionKey].Count);

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                result = await app.AcquireTokenForClient(new[] { "scope1" })
                    .WithTenantId(TestConstants.Utid2)
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                // Two tenant partitions with three tokens total
                Assert.AreEqual(3, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                string partitionKey2 = CacheKeyFactory.GetClientCredentialKey(TestConstants.ClientId, TestConstants.Utid2, null);

                Assert.AreEqual(2, ((InMemoryPartitionedAppTokenCacheAccessor)app.AppTokenCacheInternal.Accessor).AccessTokenCacheDictionary.Count);
                Assert.IsNotNull(((InMemoryPartitionedAppTokenCacheAccessor)app.AppTokenCacheInternal.Accessor).AccessTokenCacheDictionary[partitionKey2]);
                Assert.AreEqual(1, ((InMemoryPartitionedAppTokenCacheAccessor)app.AppTokenCacheInternal.Accessor).AccessTokenCacheDictionary[partitionKey2].Count);

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
                    .WithTenantId(TestConstants.Utid)
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
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);

                // check app token cache count to be 1
                Assert.AreEqual(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(0, app.AppTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);

                appCacheAccess.AssertAccessCounts(1, 1);
                userCacheAccess.AssertAccessCounts(0, 0);

                // call AcquireTokenForClientAsync again to get result back from the cache
                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(result);
                Assert.IsNotNull("header.payload.signature", result.AccessToken);
                Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());

                // make sure user token cache is empty
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);

                // check app token cache count to be 1
                Assert.AreEqual(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(0, app.AppTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);

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
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);

                // check app token cache count to be 1
                Assert.AreEqual(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(0, app.AppTokenCacheInternal.Accessor.GetAllRefreshTokens().Count); // no refresh tokens are returned

                // call AcquireTokenForClientAsync again to get result back from the cache
                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync().ConfigureAwait(false);
                Assert.IsNotNull(result);
                Assert.IsNotNull("header.payload.signature", result.AccessToken);
                Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());

                // make sure user token cache is empty
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);

                // check app token cache count to be 1
                Assert.AreEqual(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(0, app.AppTokenCacheInternal.Accessor.GetAllRefreshTokens().Count); // no refresh tokens are returned
                appCacheAccess.AssertAccessCounts(2, 1);
                userCacheAccess.AssertAccessCounts(0, 0);

            }
        }

        private enum CredentialType
        {
            Certificate,
            CertificateAndClaims,
            SignedAssertion,
            SignedAssertionDelegate,
            SignedAssertionAsyncDelegate,
            SignedAssertionWithAssertionRequestOptionsAsyncDelegate,

        }

        private (ConfidentialClientApplication app, MockHttpMessageHandler handler) CreateConfidentialClient(
            MockHttpManager httpManager,
            X509Certificate2 cert,
            CredentialType credentialType = CredentialType.Certificate)
        {
            var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                              .WithRedirectUri(TestConstants.RedirectUri)
                              .WithHttpManager(httpManager);

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
                case CredentialType.SignedAssertionAsyncDelegate:
                    builder = builder.WithClientAssertion(async (CancellationToken _) => await Task.FromResult(TestConstants.DefaultClientAssertion).ConfigureAwait(false));
                    app = builder.BuildConcrete();
                    Assert.IsNull(app.Certificate);
                    break;
                case CredentialType.SignedAssertionWithAssertionRequestOptionsAsyncDelegate:
                    builder = builder.WithClientAssertion((options) =>
                    {
                        Assert.IsNotNull(options.ClientID);
                        Assert.IsNotNull(options.TokenEndpoint);
                        return Task.FromResult(TestConstants.DefaultClientAssertion);
                    });
                    app = builder.BuildConcrete();
                    Assert.IsNull(app.Certificate);
                    break;
                case CredentialType.Certificate:
                    builder = builder.WithCertificate(cert);
                    app = builder.BuildConcrete();
                    Assert.AreEqual(cert, app.Certificate);
                    break;
                default:
                    throw new NotImplementedException();
            }

            MockHttpMessageHandler handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

            return (app, handler);
        }

        private static Task ModifyRequestAsync(OnBeforeTokenRequestData requestData)
        {
            Assert.AreEqual("https://login.microsoftonline.com/tid/oauth2/v2.0/token", requestData.RequestUri.AbsoluteUri);
            requestData.BodyParameters.Add("param1", "val1");
            requestData.BodyParameters.Add("param2", "val2");

            requestData.Headers.Add("header1", "hval1");
            requestData.Headers.Add("header2", "hval2");

            return Task.CompletedTask;
        }

        [TestMethod]
        public async Task CertificateOverrideAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                              .WithAuthority("https://login.microsoftonline.com/tid/")
                              .WithExperimentalFeatures(true)
                              .WithHttpManager(httpManager)
                              .Build();

                MockHttpMessageHandler handler = httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithProofOfPosessionKeyId("key1")
                    .OnBeforeTokenRequest(ModifyRequestAsync)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual("Bearer", result.TokenType);

                Assert.AreEqual("val1", handler.ActualRequestPostData["param1"]);
                Assert.AreEqual("val2", handler.ActualRequestPostData["param2"]);
                Assert.AreEqual("hval1", handler.ActualRequestHeaders.GetValues("header1").Single());
                Assert.AreEqual("hval2", handler.ActualRequestHeaders.GetValues("header2").Single());
                Assert.IsFalse(handler.ActualRequestPostData.ContainsKey(OAuth2Parameter.ClientAssertion));
                Assert.IsFalse(handler.ActualRequestPostData.ContainsKey(OAuth2Parameter.ClientAssertionType));
                Assert.AreEqual("key1", (app.AppTokenCache as ITokenCacheInternal).Accessor.GetAllAccessTokens().Single().KeyId);

                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .WithProofOfPosessionKeyId("key1")
                    .OnBeforeTokenRequest(ModifyRequestAsync)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual("Bearer", result.TokenType);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);

                Assert.AreEqual(
                    "key1",
                    (app.AppTokenCache as ITokenCacheInternal).Accessor.GetAllAccessTokens().Single().KeyId);

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                 .OnBeforeTokenRequest(ModifyRequestAsync)
                 .ExecuteAsync()
                 .ConfigureAwait(false);

                Assert.AreEqual("Bearer", result.TokenType);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                IReadOnlyList<Client.Cache.Items.MsalAccessTokenCacheItem> ats = (app.AppTokenCache as ITokenCacheInternal).Accessor.GetAllAccessTokens();
                Assert.AreEqual(2, ats.Count);
                Assert.IsTrue(ats.Single(at => at.KeyId == "key1") != null);
                Assert.IsTrue(ats.Single(at => at.KeyId == null) != null);
            }
        }

        [TestMethod]
        public async Task ConfidentialClientUsingCertificateTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var cert = new X509Certificate2(ResourceHelper.GetTestResourceRelativePath("valid.crtfile"));
                var (app, _) = CreateConfidentialClient(httpManager, cert);
                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(result);
                Assert.IsNotNull("header.payload.signature", result.AccessToken);
                Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());
                appCacheAccess.AssertAccessCounts(1, 1);
                userCacheAccess.AssertAccessCounts(0, 0);

                // make sure user token cache is empty
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);

                // check app token cache count to be 1
                Assert.AreEqual(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(0, app.AppTokenCacheInternal.Accessor.GetAllRefreshTokens().Count); // no RTs are returned
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
                (ConfidentialClientApplication App, MockHttpMessageHandler Handler) setup = CreateConfidentialClient(httpManager, cert, CredentialType.CertificateAndClaims);
                var app = setup.App;
                var tokenHttpHandler = setup.Handler;

                var appCacheAccess = app.AppTokenCache.RecordAccess();
                var userCacheAccess = app.UserTokenCache.RecordAccess();

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(setup);
                Assert.IsNotNull("header.payload.signature", result.AccessToken);
                Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());
                appCacheAccess.AssertAccessCounts(1, 1);
                userCacheAccess.AssertAccessCounts(0, 0);

                // make sure user token cache is empty
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);

                // check app token cache count to be 1
                Assert.AreEqual(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(0, app.AppTokenCacheInternal.Accessor.GetAllRefreshTokens().Count); // no RTs are returned

                var actualAssertion = tokenHttpHandler.ActualRequestPostData["client_assertion"];

                // assert client credential

                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(actualAssertion);
                var claims = jsonToken.Claims;
                //checked if additional claim is in signed assertion
                var audclaim = TestConstants.s_clientAssertionClaims.FirstOrDefault(x => x.Key == "aud");
                var validClaim = claims.FirstOrDefault(x => x.Type == audclaim.Key && x.Value == audclaim.Value);
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
                (ConfidentialClientApplication App, MockHttpMessageHandler Handler) setup
                    = CreateConfidentialClient(httpManager, cert, CredentialType.SignedAssertion);
                var app = setup.App;

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(result);
                Assert.IsNotNull("header.payload.signature", result.AccessToken);
                Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());

                // make sure user token cache is empty
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);

                // check app token cache count to be 1
                Assert.AreEqual(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(0, app.AppTokenCacheInternal.Accessor.GetAllRefreshTokens().Count); // no RTs are returned

                // assert client credential

                Assert.IsTrue((app.AppConfig as ApplicationConfiguration).ClientCredential is SignedAssertionClientCredential);
            }
        }

        [TestMethod]
        public async Task ConfidentialClientUsingSignedClientAssertion_SyncDelegateTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                (ConfidentialClientApplication App, MockHttpMessageHandler Handler) setup =
                    CreateConfidentialClient(httpManager, null, CredentialType.SignedAssertionDelegate);

                var app = setup.App;

                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(result);
                Assert.IsNotNull("header.payload.signature", result.AccessToken);
                Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());

                // make sure user token cache is empty
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(0, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);

                // check app token cache count to be 1
                Assert.AreEqual(1, app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(0, app.AppTokenCacheInternal.Accessor.GetAllRefreshTokens().Count); // no RTs are returned

                // assert client credential
                Assert.AreEqual(
                                    TestConstants.DefaultClientAssertion,
                                    setup.Handler.ActualRequestPostData["client_assertion"]);

                Assert.AreEqual(
                    "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                    setup.Handler.ActualRequestPostData["client_assertion_type"]);
            }
        }

        [TestMethod]
        public async Task ConfidentialClientUsingSignedClientAssertion_AsyncDelegateTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                (ConfidentialClientApplication App, MockHttpMessageHandler Handler) setup =
                    CreateConfidentialClient(httpManager, null, CredentialType.SignedAssertionAsyncDelegate);

                var result = await setup.App.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(result);
                Assert.IsNotNull("header.payload.signature", result.AccessToken);
                Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());

                Assert.AreEqual(
                    TestConstants.DefaultClientAssertion,
                    setup.Handler.ActualRequestPostData["client_assertion"]);

                Assert.AreEqual(
                    "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                    setup.Handler.ActualRequestPostData["client_assertion_type"]);
            }
        }

        [TestMethod]
        public async Task ConfidentialClientUsingSignedClientAssertion_AsyncDelegateWithRequestOptionsTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                (ConfidentialClientApplication App, MockHttpMessageHandler Handler) setup =
                    CreateConfidentialClient(httpManager, null, CredentialType.SignedAssertionWithAssertionRequestOptionsAsyncDelegate);

                var result = await setup.App.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.IsNotNull(result);
                Assert.IsNotNull("header.payload.signature", result.AccessToken);
                Assert.AreEqual(TestConstants.s_scope.AsSingleString(), result.Scopes.AsSingleString());

                Assert.AreEqual(
                    TestConstants.DefaultClientAssertion,
                    setup.Handler.ActualRequestPostData["client_assertion"]);

                Assert.AreEqual(
                    "urn:ietf:params:oauth:client-assertion-type:jwt-bearer",
                    setup.Handler.ActualRequestPostData["client_assertion_type"]);
            }
        }

        [TestMethod]
        public async Task ConfidentialClientUsingSignedClientAssertion_AsyncDelegate_CancellationTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

                var builder = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                            .WithHttpManager(httpManager)
                            .WithClientAssertion(
                            async ct =>
                            {
                                // make sure that the cancellation token given to AcquireToken method
                                // is propagated to here
                                cancellationTokenSource.Cancel();
                                ct.ThrowIfCancellationRequested();
                                return await Task.FromResult(TestConstants.DefaultClientAssertion)
                                .ConfigureAwait(false);
                            });

                var app = builder.BuildConcrete();
                Assert.IsNull(app.Certificate);

                await AssertException.TaskThrowsAsync<OperationCanceledException>(
                    () => app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .ExecuteAsync(cancellationTokenSource.Token)).ConfigureAwait(false);
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
        public void GetAuthorizationRequestUrl_WithConsumerInCreate_ReturnsConsumers()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                ConfidentialClientApplicationOptions applicationOptions;
                applicationOptions = new ConfidentialClientApplicationOptions();
                applicationOptions.ClientId = "fakeId";
                applicationOptions.RedirectUri = "https://example.com";
                applicationOptions.ClientSecret = "rwerewrwe";

                var confidentialClientApplicationBuilder =
                    ConfidentialClientApplicationBuilder
                        .CreateWithApplicationOptions(applicationOptions)
                        .WithHttpManager(httpManager);

                var confidentialClientApplication = confidentialClientApplicationBuilder.Build();

#pragma warning disable CS0618 // Type or member is obsolete
                Uri authorizationRequestUrl = confidentialClientApplication
                    .GetAuthorizationRequestUrl(new List<string> { "" })
                    .WithAuthority(AzureCloudInstance.AzurePublic, Constants.ConsumerTenant)
                    .ExecuteAsync()
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
#pragma warning restore CS0618 // Type or member is obsolete

                Assert.IsTrue(authorizationRequestUrl.Segments[1].StartsWith(Constants.CommonTenant));
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

                var uri = await app
                    .GetAuthorizationRequestUrl(TestConstants.s_scope)
                    .WithLoginHint(TestConstants.DisplayableId)
                    .WithPkce(out string codeVerifier)
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
        // regression test for https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/4140
        public async Task IdTokenHasNoOid_ADALSerialization_Async()
        {
            // Arrange
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler("https://login.windows-ppe.net/98ecb0ef-bb8d-4216-b45a-70df950dc6e3/");

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithAuthority("https://login.windows-ppe.net/98ecb0ef-bb8d-4216-b45a-70df950dc6e3/")
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .Build();

                byte[] tokenCacheInAdalFormat = null;
                app.UserTokenCache.SetAfterAccess(
                    (args) =>
                    {
                        if (args.HasStateChanged)
                        {
                            tokenCacheInAdalFormat = args.TokenCache.SerializeAdalV3();
                        };
                    });

                var handler = httpManager.AddMockHandler(
                       new MockHttpMessageHandler()
                       {
                           ExpectedMethod = HttpMethod.Post,
                           ResponseMessage = MockHelpers.CreateSuccessResponseMessage(MockHelpers.GetTokenResponseWithNoOidClaim())
                       });
                

                // Act
                var result = await app.AcquireTokenByAuthorizationCode(new[] { "https://management.core.windows.net//.default" }, "code")                    
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(tokenCacheInAdalFormat);

                Assert.AreEqual("AujLDQp5yRMRcGpPcDBft9Nb5uFSKYxDZq65-ebfHls", result.UniqueId);
                Assert.AreEqual("AujLDQp5yRMRcGpPcDBft9Nb5uFSKYxDZq65-ebfHls", result.ClaimsPrincipal.FindFirst("sub").Value);
                Assert.IsNull(result.ClaimsPrincipal.FindFirst("oid"));
            }
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task AcquireTokenByAuthorizationCode_IgnoresRegion_Async(bool autodetectRegion)
        {
            using (var httpManager = new MockHttpManager())
            {
                // MSAL should not auto-detect, but if it does, this test should fail because a call to IMDS is configured
                string region = autodetectRegion ? ConfidentialClientApplication.AttemptRegionDiscovery : TestConstants.Region;

                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithAzureRegion(region)
                                                              .WithHttpManager(httpManager)
                                                              .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddSuccessTokenResponseMockHandlerForPost();

                var result = await app.AcquireTokenByAuthorizationCode(TestConstants.s_scope, TestConstants.DefaultAuthorizationCode)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual("https://login.microsoftonline.com/common/oauth2/v2.0/token", result.AuthenticationResultMetadata.TokenEndpoint);
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
#pragma warning disable CS0618 // Type or member is obsolete
                Task<Uri> task = app
                    .GetAuthorizationRequestUrl(TestConstants.s_scope)
                    .WithRedirectUri(CustomRedirectUri)
                    .WithLoginHint(TestConstants.DisplayableId)
                    .WithExtraQueryParameters("extra=qp")
                    .WithExtraScopesToConsent(TestConstants.s_scopeForAnotherResource)
                    .WithAuthority(TestConstants.AuthorityGuestTenant)
                    .ExecuteAsync(CancellationToken.None);
#pragma warning restore CS0618 // Type or member is obsolete

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
        }

        [TestMethod]
        public async Task GetAuthorizationRequestUrlValidateDefaultPromptTestAsync()
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
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(uri);
                Dictionary<string, string> qp = CoreHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', true, null);

                Assert.IsTrue(qp.ContainsKey(TestConstants.PromptParam));
                Assert.AreEqual(Prompt.SelectAccount.PromptValue, qp[TestConstants.PromptParam]);
            }
        }

        [TestMethod]
        public async Task GetAuthorizationRequestUrlValidatePromptSelectAccountTestAsync()
        {
            Dictionary<string, string> qp = await GetAuthorizationRequestUrlQueryParamsWithPromptAsync(Prompt.SelectAccount).ConfigureAwait(false);

            Assert.IsTrue(qp.ContainsKey(TestConstants.PromptParam));
            Assert.AreEqual(Prompt.SelectAccount.PromptValue, qp[TestConstants.PromptParam]);

        }

        [TestMethod]
        public async Task GetAuthorizationRequestUrlValidateNoPromptTestAsync()
        {
            Dictionary<string, string> qp = await GetAuthorizationRequestUrlQueryParamsWithPromptAsync(Prompt.NoPrompt).ConfigureAwait(false);

            Assert.IsFalse(qp.ContainsKey(TestConstants.PromptParam));

        }

        [TestMethod]
        public async Task GetAuthorizationRequestUrlValidatePromptNotSpecifiedTestAsync()
        {
            Dictionary<string, string> qp = await GetAuthorizationRequestUrlQueryParamsWithPromptAsync(Prompt.NotSpecified).ConfigureAwait(false);

            Assert.IsTrue(qp.ContainsKey(TestConstants.PromptParam));
            Assert.AreEqual(Prompt.SelectAccount.PromptValue, qp[TestConstants.PromptParam]);

        }

        [TestMethod]
        public async Task GetAuthorizationRequestUrlValidatePromptCreateTestAsync()
        {
            Dictionary<string, string> qp = await GetAuthorizationRequestUrlQueryParamsWithPromptAsync(Prompt.Create).ConfigureAwait(false);

            Assert.IsTrue(qp.ContainsKey(TestConstants.PromptParam));
            Assert.AreEqual(Prompt.Create.PromptValue, qp[TestConstants.PromptParam]);

        }

        [TestMethod]
        public async Task GetAuthorizationRequestUrlValidatePromptForceLoginTestAsync()
        {
            Dictionary<string, string> qp = await GetAuthorizationRequestUrlQueryParamsWithPromptAsync(Prompt.ForceLogin).ConfigureAwait(false);

            Assert.IsTrue(qp.ContainsKey(TestConstants.PromptParam));
            Assert.AreEqual(Prompt.ForceLogin.PromptValue, qp[TestConstants.PromptParam]);

        }

        private async Task<Dictionary<string, string>> GetAuthorizationRequestUrlQueryParamsWithPromptAsync(Prompt prompt)
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
                    .WithPrompt(prompt)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Dictionary<string, string> qp = CoreHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', true, null);

                return qp;
            }
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

                TokenCacheHelper.PopulateCache(app.AppTokenCacheInternal.Accessor);

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
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new Uri(TestConstants.AuthorityTestTenant), true)
                    .WithRedirectUri(TestConstants.RedirectUri)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
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
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);

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
        public async Task GetAuthCode_HybridSpa_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithRedirectUri(TestConstants.RedirectUri)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithHttpManager(httpManager)
                .BuildConcrete();

                string expectedSpaCode = "my_spa_code";
                httpManager.AddInstanceDiscoveryMockHandler();
                var handler = httpManager.AddSuccessTokenResponseMockHandlerForPost(
                responseMessage: MockHelpers.CreateSuccessResponseMessage(MockHelpers.GetHybridSpaTokenResponse(expectedSpaCode)));

                var result = await app.AcquireTokenByAuthorizationCode(TestConstants.s_scope, TestConstants.DefaultAuthorizationCode)
                .WithSpaAuthorizationCode(true)
                .ExecuteAsync()
                .ConfigureAwait(false);

                Assert.AreEqual(expectedSpaCode, result.SpaAuthCode);
                Assert.IsFalse(result.AdditionalResponseParameters.ContainsKey("spa_accountid"));
                Assert.AreEqual("1", handler.ActualRequestPostData["return_spa_code"]);

                handler = httpManager.AddSuccessTokenResponseMockHandlerForPost(
                responseMessage: MockHelpers.CreateSuccessResponseMessage(MockHelpers.GetHybridSpaTokenResponse(expectedSpaCode)));

                result = await app.AcquireTokenByAuthorizationCode(TestConstants.s_scope, TestConstants.DefaultAuthorizationCode)
                .WithSpaAuthorizationCode()
                .ExecuteAsync()
                .ConfigureAwait(false);

                Assert.AreEqual(expectedSpaCode, result.SpaAuthCode);
                Assert.AreEqual("1", handler.ActualRequestPostData["return_spa_code"]);

                handler = httpManager.AddSuccessTokenResponseMockHandlerForPost(
                responseMessage: MockHelpers.CreateSuccessResponseMessage(MockHelpers.GetHybridSpaTokenResponse(null)));

                result = await app.AcquireTokenByAuthorizationCode(TestConstants.s_scope, TestConstants.DefaultAuthorizationCode)
                .WithSpaAuthorizationCode(false)
                .ExecuteAsync()
                .ConfigureAwait(false);

                Assert.IsTrue(string.IsNullOrEmpty(result.SpaAuthCode));

            }
        }

        [TestMethod]
        public async Task BridgedHybridSpa_Async()
        {
            var wamAccountId = "wam_account_id_1234";

            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();
                var handler = httpManager.AddSuccessTokenResponseMockHandlerForPost(
                    responseMessage: MockHelpers.CreateSuccessResponseMessage(MockHelpers.GetBridgedHybridSpaTokenResponse(wamAccountId)));

                var result = await app.AcquireTokenByAuthorizationCode(TestConstants.s_scope, TestConstants.DefaultAuthorizationCode)
                    .WithSpaAuthorizationCode(true)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Assert.AreEqual(wamAccountId, result.AdditionalResponseParameters["spa_Accountid"]);

                Assert.IsNull(result.SpaAuthCode);
                Assert.AreEqual("1", handler.ActualRequestPostData["return_spa_code"]);
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

                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(result.AccessToken, "some-access-token");

                app.UserTokenCacheInternal.Accessor.Clear();
                httpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);
                result = await ((IByRefreshToken)app)
                    .AcquireTokenByRefreshToken(TestConstants.s_scope, "SomeRefreshToken")
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count);
                Assert.AreEqual(1, app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(result.AccessToken, "some-access-token");
            }
        }

        [TestMethod]
        // Regression test for https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1193
        public async Task GetAuthorizationRequestUrl_ReturnsUri_Async()
        {
            using (var harness = base.CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                string[] s_userReadScope = { "User.Read" };

                var cca = ConfidentialClientApplicationBuilder
                       .Create(TestConstants.ClientId)
                       .WithHttpManager(harness.HttpManager)
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

                Assert.AreEqual(uriParams1["x-client-OS"], uriParams2["x-client-OS"]);
                Assert.AreEqual(uriParams1["x-client-Ver"], uriParams2["x-client-Ver"]);
                Assert.AreEqual(uriParams1["x-client-SKU"], uriParams2["x-client-SKU"]);
            }
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
                                       .WithSendX5C(true);
            PublicClientApplicationTests.CheckBuilderCommonMethods(onBehalfOfBuilder);

            var oboCacheKey = "oboCacheKey";
            var longRunningOboBuilder = ((ILongRunningWebApi)app).InitiateLongRunningProcessInWebApi(
                               TestConstants.s_scope.ToArray(),
                               TestConstants.DefaultClientAssertion,
                               ref oboCacheKey)
                .WithSearchInCacheForLongRunningProcess();
            PublicClientApplicationTests.CheckBuilderCommonMethods(longRunningOboBuilder);

            longRunningOboBuilder = ((ILongRunningWebApi)app).AcquireTokenInLongRunningProcess(
                               TestConstants.s_scope.ToArray(),
                               oboCacheKey);
            PublicClientApplicationTests.CheckBuilderCommonMethods(longRunningOboBuilder);

            var silentBuilder = app.AcquireTokenSilent(TestConstants.s_scope, TestConstants.s_user)
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

                InMemoryTokenCache cache = new InMemoryTokenCache();
                cache.Bind(app.AppTokenCache);

                var cacheRecorder = app.AppTokenCache.RecordAccess();

                (app.AppTokenCache as TokenCache).AfterAccess += (args) =>
                {
                    if (args.HasStateChanged == true)
                    {
                        Assert.IsTrue(args.SuggestedCacheExpiry.HasValue);

                        var allAts = app.AppTokenCacheInternal.Accessor.GetAllAccessTokens();
                        var maxAtExpiration = allAts.Max(at => at.ExpiresOn);
                        Assert.AreEqual(maxAtExpiration, args.SuggestedCacheExpiry);

                        switch (cacheRecorder.AfterAccessWriteCount)
                        {
                            case 1:
                            case 2:
                                CoreAssert.IsWithinRange(
                                 DateTimeOffset.UtcNow + TimeSpan.FromSeconds(3600),
                                 args.SuggestedCacheExpiry.Value,
                                 TimeSpan.FromSeconds(5));
                                break;
                            case 3:
                                CoreAssert.IsWithinRange(
                                    DateTimeOffset.UtcNow + TimeSpan.FromSeconds(7200),
                                    args.SuggestedCacheExpiry.Value,
                                    TimeSpan.FromSeconds(5));
                                break;
                            default:
                                Assert.Fail("Not expecting more than 3 calls");
                                break;
                        }
                    }
                };

                // Add first token into the cache
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(expiresIn: "3600");
                var result = await app.AcquireTokenForClient(new string[] { "scope1" }.ToArray())
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                CoreAssert.IsWithinRange(
                            DateTimeOffset.UtcNow + TimeSpan.FromSeconds(3600),
                            result.ExpiresOn,
                            TimeSpan.FromSeconds(5));

                // Add second token with shorter expiration time
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(expiresIn: "1800");
                result = await app.AcquireTokenForClient(new string[] { "scope2" }.ToArray())
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                CoreAssert.IsWithinRange(
                          DateTimeOffset.UtcNow + TimeSpan.FromSeconds(1800),
                          result.ExpiresOn,
                          TimeSpan.FromSeconds(5));

                // Add third token with the largest expiration time
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage(expiresIn: "7200");
                result = await app.AcquireTokenForClient(new string[] { "scope3" }.ToArray())
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                CoreAssert.IsWithinRange(
                            DateTimeOffset.UtcNow + TimeSpan.FromSeconds(7200),
                            result.ExpiresOn,
                            TimeSpan.FromSeconds(5));

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

        [TestMethod]
        public async Task AcquireTokenForClientAuthorityCheckTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                string log = string.Empty;

                var app = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithClientSecret(TestConstants.ClientSecret)
                    .WithHttpManager(httpManager)
                    .WithLogging((LogLevel _, string message, bool _) => log += message)
                    .BuildConcrete();

#pragma warning disable CS0618 // Type or member is obsolete
                var result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .WithAuthority(TestConstants.AuthorityCommonTenant, true)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsTrue(log.Contains(MsalErrorMessage.ClientCredentialWrongAuthority));

                log = string.Empty;
                result = await app
                    .AcquireTokenForClient(TestConstants.s_scope)
                    .WithAuthority(TestConstants.AuthorityOrganizationsTenant, true)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
#pragma warning restore CS0618 // Type or member is obsolete

                Assert.IsTrue(log.Contains(MsalErrorMessage.ClientCredentialWrongAuthority));
            }
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(null)]
        public async Task ValidateGetAccountAsyncWithNullEmptyAccountIdAsync(string accountId)
        {
            using (var httpManager = new MockHttpManager())
            {
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithHttpManager(httpManager)
                                                              .BuildConcrete();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddSuccessTokenResponseMockHandlerForPost();

                var result = await app.AcquireTokenByAuthorizationCode(TestConstants.s_scope, TestConstants.DefaultAuthorizationCode)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                var acc = await app.GetAccountAsync(accountId).ConfigureAwait(false);

                Assert.IsNull(acc);
            }
        }

        [TestMethod]
        public async Task ValidateAppTokenProviderAsync()
        {
            using (var harness = base.CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                bool usingClaims = false;
                string differentScopesForAt = string.Empty;
                int callbackInvoked = 0;
                var app = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithAppTokenProvider((AppTokenProviderParameters parameters) =>
                                                              {
                                                                  Assert.IsNotNull(parameters.Scopes);
                                                                  Assert.IsNotNull(parameters.CorrelationId);
                                                                  Assert.IsNotNull(parameters.TenantId);
                                                                  Assert.IsNotNull(parameters.CancellationToken);

                                                                  if (usingClaims)
                                                                  {
                                                                      Assert.IsNotNull(parameters.Claims);
                                                                  }

                                                                  Interlocked.Increment(ref callbackInvoked);

                                                                  return Task.FromResult(GetAppTokenProviderResult(differentScopesForAt));
                                                              })
                                                              .WithHttpManager(harness.HttpManager)
                                                              .BuildConcrete();

                // AcquireToken from app provider
                AuthenticationResult result = await app.AcquireTokenForClient(TestConstants.s_scope)
                                                        .ExecuteAsync(new CancellationToken()).ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TestConstants.DefaultAccessToken, result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(1, callbackInvoked);

                var tokens = app.AppTokenCacheInternal.Accessor.GetAllAccessTokens();

                Assert.AreEqual(1, tokens.Count);

                var token = tokens.FirstOrDefault();
                Assert.IsNotNull(token);
                Assert.AreEqual(TestConstants.DefaultAccessToken, token.Secret);

                // AcquireToken from cache
                result = await app.AcquireTokenForClient(TestConstants.s_scope)
                                                        .ExecuteAsync(new CancellationToken()).ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TestConstants.DefaultAccessToken, result.AccessToken);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(1, callbackInvoked);

                // Expire token
                TokenCacheHelper.ExpireAllAccessTokens(app.AppTokenCacheInternal);

                // Acquire token from app provider with expired token
                result = await app.AcquireTokenForClient(TestConstants.s_scope)
                                                        .ExecuteAsync(new CancellationToken()).ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TestConstants.DefaultAccessToken, result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(2, callbackInvoked);

                differentScopesForAt = "new scope";

                // Acquire token from app provider with new scopes
                result = await app.AcquireTokenForClient(new[] { differentScopesForAt })
                                                        .ExecuteAsync(new CancellationToken()).ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TestConstants.DefaultAccessToken + differentScopesForAt, result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(app.AppTokenCacheInternal.Accessor.GetAllAccessTokens().Count, 2);
                Assert.AreEqual(3, callbackInvoked);

                // Acquire token from app provider with claims. Should not use cache
                result = await app.AcquireTokenForClient(TestConstants.s_scope)
                                                        .WithClaims(TestConstants.Claims)
                                                        .ExecuteAsync(new CancellationToken()).ConfigureAwait(false);

                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TestConstants.DefaultAccessToken + differentScopesForAt, result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
                Assert.AreEqual(4, callbackInvoked);
            }
        }

        private AppTokenProviderResult GetAppTokenProviderResult(string differentScopesForAt = "", long? refreshIn = 1000)
        {
            var token = new AppTokenProviderResult();
            token.AccessToken = TestConstants.DefaultAccessToken + differentScopesForAt; //Used to indicate that there is a new access token for a different set of scopes
            token.ExpiresInSeconds = 3600;
            token.RefreshInSeconds = refreshIn;

            return token;
        }
    }
}
