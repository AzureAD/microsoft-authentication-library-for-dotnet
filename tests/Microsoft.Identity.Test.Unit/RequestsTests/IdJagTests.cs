using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class IdJagTests : TestBase
    {
        private const string Audience = "https://authorization-server.example/";
        private const string Resource = "https://api.example/";
        private const string IdJagTokenType = "urn:ietf:params:oauth:token-type:id-jag";
        private static readonly string[] s_scopes = { "agent.read", "agent.write" };

        [TestMethod]
        [DataRow(null, 300)]
        [DataRow(Resource, 900)]
        public async Task AuthorizationCodeCachesGrantAndBootstrapsSessionAsync(string resource, int lifetime)
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            var app = BuildApp(httpManager);
            AddTokenResponse(httpManager, CreateResponse("id-jag", "initial-rt", lifetime),
                new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code",
                    ["code"] = "authorization-code",
                    ["code_verifier"] = "pkce-verifier",
                    ["redirect_uri"] = "https://app.example/callback"
                }, resource: resource);
            var correlationId = Guid.NewGuid();
            var before = DateTimeOffset.UtcNow;

            // Act
            var result = await app.AcquireTokenByAuthorizationCode(s_scopes, "authorization-code")
                .WithPkceCodeVerifier("pkce-verifier")
                .WithCorrelationId(correlationId)
                .WithIdJag(Audience, resource)
                .ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual("id-jag", result.AccessToken);
            Assert.AreEqual("Bearer", result.TokenType);
            Assert.AreEqual(correlationId, result.CorrelationId);
            Assert.IsTrue(result.ExpiresOn >= before.AddSeconds(lifetime));
            Assert.IsTrue(result.ExpiresOn <= DateTimeOffset.UtcNow.AddSeconds(lifetime));
            Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(IdJagTokenType, result.AdditionalResponseParameters["issued_token_type"]);
            Assert.AreEqual("id-jag", app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Single().Secret);
            Assert.AreEqual("initial-rt", app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Single().Secret);
            Assert.HasCount(1, app.UserTokenCacheInternal.Accessor.GetAllIdTokens());
            Assert.AreEqual(result.Account.HomeAccountId, (await app.GetAccountsAsync().ConfigureAwait(false)).Single().HomeAccountId);

            AddTokenResponse(httpManager, CreateResponse("second-id-jag", null, includeIdToken: false),
                new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = "initial-rt"
                });
            var refreshed = await app.AcquireTokenSilent(s_scopes, result.Account)
                .WithIdJag(Audience).WithForceRefresh(true).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("second-id-jag", refreshed.AccessToken);
            Assert.AreEqual("initial-rt", app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Single().Secret);
            CollectionAssert.AreEquivalent(resource is null ? new[] { "second-id-jag" } : new[] { "id-jag", "second-id-jag" },
                app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Select(item => item.Secret).ToArray());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task SilentCachesGrantAndPreservesOrdinaryTokenAsync(bool forceRefresh)
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            var app = BuildApp(httpManager);
            AddTokenResponse(httpManager, CreateResponse("ordinary-at", "initial-rt", 3600),
                new Dictionary<string, string> { ["grant_type"] = "authorization_code" }, idJag: false);
            var session = await app.AcquireTokenByAuthorizationCode(s_scopes, "code").ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("ordinary-at", app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Single().Secret);

            AddTokenResponse(httpManager, CreateResponse("id-jag", "rotated-rt", includeIdToken: false),
                new Dictionary<string, string> { ["grant_type"] = "refresh_token", ["refresh_token"] = "initial-rt" },
                resource: Resource);

            // Act
            var grant = await app.AcquireTokenSilent(s_scopes, session.Account)
                .WithIdJag(Audience, Resource).WithForceRefresh(forceRefresh).ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual("id-jag", grant.AccessToken);
            CollectionAssert.AreEquivalent(new[] { "ordinary-at", "id-jag" },
                app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Select(item => item.Secret).ToArray());
            Assert.AreEqual("rotated-rt", app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Single().Secret);
            var ordinary = await app.AcquireTokenSilent(s_scopes, session.Account).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("ordinary-at", ordinary.AccessToken);
            Assert.AreEqual(TokenSource.Cache, ordinary.AuthenticationResultMetadata.TokenSource);

            AddTokenResponse(httpManager, CreateResponse("next-id-jag", null, includeIdToken: false),
                new Dictionary<string, string> { ["grant_type"] = "refresh_token", ["refresh_token"] = "rotated-rt" },
                audience: Audience + "other");
            var next = await app.AcquireTokenSilent(s_scopes, session.Account)
                .WithIdJag(Audience + "other").ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("next-id-jag", next.AccessToken);
            Assert.AreEqual("rotated-rt", app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Single().Secret);
            CollectionAssert.AreEquivalent(new[] { "ordinary-at", "id-jag", "next-id-jag" },
                app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Select(item => item.Secret).ToArray());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task OboCachesGrantWithoutReplacingOrdinaryTokenAsync(bool forceRefresh)
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            var app = BuildApp(httpManager);
            var assertion = new UserAssertion("validated-incoming-access-token");
            AddTokenResponse(httpManager, CreateResponse("ordinary-obo-at", "ignored-rt", 3600),
                new Dictionary<string, string> { ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer" }, idJag: false);
            await app.AcquireTokenOnBehalfOf(s_scopes, assertion).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("ordinary-obo-at", app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Single().Secret);

            AddTokenResponse(httpManager, CreateResponse("obo-id-jag", "ignored-rt"),
                new Dictionary<string, string>
                {
                    ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                    ["requested_token_use"] = "on_behalf_of",
                    ["assertion"] = assertion.Assertion
                }, resource: Resource);

            // Act
            var result = await app.AcquireTokenOnBehalfOf(s_scopes, assertion)
                .WithForceRefresh(forceRefresh).WithIdJag(Audience, Resource).ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual("obo-id-jag", result.AccessToken);
            CollectionAssert.AreEquivalent(new[] { "ordinary-obo-at", "obo-id-jag" },
                app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Select(item => item.Secret).ToArray());
            Assert.IsEmpty(app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens());
            var ordinary = await app.AcquireTokenOnBehalfOf(s_scopes, assertion).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("ordinary-obo-at", ordinary.AccessToken);
            Assert.AreEqual(TokenSource.Cache, ordinary.AuthenticationResultMetadata.TokenSource);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public void InvalidAudienceIsRejectedOnAllBuilders(string audience)
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            var app = BuildApp(httpManager);

            // Act / Assert
            Assert.Throws<ArgumentException>(() => app.AcquireTokenByAuthorizationCode(s_scopes, "code").WithIdJag(audience));
            Assert.Throws<ArgumentException>(() => app.AcquireTokenOnBehalfOf(s_scopes, new UserAssertion("assertion")).WithIdJag(audience));
            Assert.Throws<ArgumentException>(() => app.AcquireTokenSilent(s_scopes, (IAccount)null).WithIdJag(audience));
        }

        [TestMethod]
        public async Task UnsupportedOptionsAreRejectedRegardlessOfCallOrderAsync()
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            var app = BuildApp(httpManager);
            var pop = new PoPAuthenticationConfiguration(new Uri(Resource));

            // Act / Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => app.AcquireTokenOnBehalfOf(s_scopes, new UserAssertion("assertion"))
                .WithIdJag(Audience).WithSignedHttpRequestProofOfPossession(pop).ExecuteAsync()).ConfigureAwait(false);
            await Assert.ThrowsAsync<InvalidOperationException>(() => app.AcquireTokenOnBehalfOf(s_scopes, new UserAssertion("assertion"))
                .WithSignedHttpRequestProofOfPossession(pop).WithIdJag(Audience).ExecuteAsync()).ConfigureAwait(false);
            await Assert.ThrowsAsync<InvalidOperationException>(() => ((ILongRunningWebApi)app)
                .AcquireTokenInLongRunningProcess(s_scopes, "session-key").WithIdJag(Audience).ExecuteAsync()).ConfigureAwait(false);
            var publicApp = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithExperimentalFeatures().WithHttpManager(httpManager).Build();
            await Assert.ThrowsAsync<InvalidOperationException>(() => publicApp.AcquireTokenSilent(s_scopes, (IAccount)null)
                .WithIdJag(Audience).ExecuteAsync()).ConfigureAwait(false);
            Assert.Throws<ArgumentException>(() => app.AcquireTokenByAuthorizationCode(s_scopes, "code").WithIdJag(Audience, " "));
            var nonExperimental = ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithClientSecret(TestConstants.ClientSecret).Build();
            var error = Assert.Throws<MsalClientException>(() => nonExperimental
                .AcquireTokenByAuthorizationCode(s_scopes, "code").WithIdJag(Audience));
            Assert.AreEqual(MsalError.ExperimentalFeature, error.ErrorCode);
        }

        [TestMethod]
        [DataRow("audience")]
        [DataRow("resource")]
        [DataRow("requested_token_type")]
        [DataRow("grant_type")]
        public async Task CallbackCannotChangeGrantParametersAsync(string parameter)
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            var app = BuildApp(httpManager);
            var request = app.AcquireTokenByAuthorizationCode(s_scopes, "code").WithIdJag(Audience);
            request.OnBeforeTokenRequest(data =>
            {
                data.BodyParameters[parameter] = "conflicting-value";
                return Task.CompletedTask;
            });

            // Act / Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => request.ExecuteAsync()).ConfigureAwait(false);
            Assert.IsEmpty(app.UserTokenCacheInternal.Accessor.GetAllAccessTokens());
            Assert.IsEmpty(app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens());
        }

        [TestMethod]
        public async Task MissingRefreshTokenDoesNotFallBackToOrdinaryAccessTokenAsync()
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            var app = BuildApp(httpManager);
            AddTokenResponse(httpManager, CreateResponse("ordinary-at", null, 3600),
                new Dictionary<string, string> { ["grant_type"] = "authorization_code" }, idJag: false);
            var session = await app.AcquireTokenByAuthorizationCode(s_scopes, "code").ExecuteAsync().ConfigureAwait(false);

            // Act / Assert
            await Assert.ThrowsAsync<MsalUiRequiredException>(() => app.AcquireTokenSilent(s_scopes, session.Account)
                .WithIdJag(Audience).ExecuteAsync()).ConfigureAwait(false);
            Assert.AreEqual("ordinary-at", app.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Single().Secret);
        }

        [TestMethod]
        public async Task ServiceErrorsAndCancellationPreserveCacheAsync()
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            var app = BuildApp(httpManager);
            AddTokenResponse(httpManager, MockHelpers.CreateFailureTokenResponseMessage("invalid_grant"),
                new Dictionary<string, string> { ["grant_type"] = "authorization_code" });

            // Act / Assert
            var error = await Assert.ThrowsAsync<MsalServiceException>(() => app.AcquireTokenByAuthorizationCode(s_scopes, "code")
                .WithIdJag(Audience).ExecuteAsync()).ConfigureAwait(false);
            Assert.AreEqual("invalid_grant", error.ErrorCode);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(() => app.AcquireTokenByAuthorizationCode(s_scopes, "code")
                .WithIdJag(Audience).ExecuteAsync(cancellation.Token)).ConfigureAwait(false);
            Assert.IsEmpty(app.UserTokenCacheInternal.Accessor.GetAllAccessTokens());
            Assert.IsEmpty(app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens());
        }

        [TestMethod]
        [TestCategory("IdJagFunctional")]
        public async Task SerializedSessionRestoresIsolatedGrantsAcrossApplicationRestartsAsync()
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            byte[] serializedCache = null;
            int cacheWrites = 0;
            IConfidentialClientApplication app = BuildApp(httpManager);
            AttachSerializedCache(app, () => serializedCache, data =>
            {
                serializedCache = data;
                cacheWrites++;
            });
            AddTokenResponse(httpManager, CreateResponse("bootstrap-grant", "initial-rt", 900),
                new Dictionary<string, string> { ["grant_type"] = "authorization_code" }, resource: Resource);

            // Act
            var bootstrap = await app.AcquireTokenByAuthorizationCode(s_scopes, "code")
                .WithIdJag(Audience, Resource).ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual("bootstrap-grant", bootstrap.AccessToken);
            Assert.AreEqual(1, cacheWrites);
            CollectionAssert.AreEquivalent(new[] { "bootstrap-grant" }, ReadCacheSecrets(serializedCache, "AccessToken"));
            CollectionAssert.AreEquivalent(new[] { "initial-rt" }, ReadCacheSecrets(serializedCache, "RefreshToken"));
            Assert.HasCount(1, ReadCacheSecrets(serializedCache, "IdToken"));

            IConfidentialClientApplication restarted = BuildApp(httpManager);
            AttachSerializedCache(restarted, () => serializedCache, data =>
            {
                serializedCache = data;
                cacheWrites++;
            });
            var account = await restarted.GetAccountAsync(bootstrap.Account.HomeAccountId.Identifier).ConfigureAwait(false);
            Assert.IsNotNull(account);
            Assert.AreEqual(bootstrap.Account.HomeAccountId.Identifier, account.HomeAccountId.Identifier);
            var cachedGrant = await restarted.AcquireTokenSilent(s_scopes, account)
                .WithIdJag(Audience, Resource).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("bootstrap-grant", cachedGrant.AccessToken);
            Assert.AreEqual(TokenSource.Cache, cachedGrant.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(1, cacheWrites);
            AddTokenResponse(httpManager, CreateResponse("refreshed-grant", "rotated-rt", includeIdToken: false),
                new Dictionary<string, string> { ["grant_type"] = "refresh_token", ["refresh_token"] = "initial-rt" },
                resource: Resource);
            var refreshed = await restarted.AcquireTokenSilent(s_scopes, account)
                .WithIdJag(Audience, Resource).WithForceRefresh(true).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("refreshed-grant", refreshed.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, refreshed.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual(2, cacheWrites);
            CollectionAssert.AreEquivalent(new[] { "refreshed-grant" }, ReadCacheSecrets(serializedCache, "AccessToken"));
            CollectionAssert.AreEquivalent(new[] { "rotated-rt" }, ReadCacheSecrets(serializedCache, "RefreshToken"));
            Assert.HasCount(1, ReadCacheSecrets(serializedCache, "IdToken"));

            IConfidentialClientApplication ordinaryApp = BuildApp(httpManager);
            AttachSerializedCache(ordinaryApp, () => serializedCache, data => serializedCache = data);
            var restoredAccount = await ordinaryApp.GetAccountAsync(bootstrap.Account.HomeAccountId.Identifier).ConfigureAwait(false);
            Assert.IsNotNull(restoredAccount);
            AddTokenResponse(httpManager, CreateResponse("ordinary-at", null, 3600, includeIdToken: false),
                new Dictionary<string, string> { ["grant_type"] = "refresh_token", ["refresh_token"] = "rotated-rt" },
                idJag: false);
            var ordinary = await ordinaryApp.AcquireTokenSilent(s_scopes, restoredAccount)
                .ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("ordinary-at", ordinary.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, ordinary.AuthenticationResultMetadata.TokenSource);
            CollectionAssert.AreEquivalent(new[] { "ordinary-at", "refreshed-grant" }, ReadCacheSecrets(serializedCache, "AccessToken"));
            CollectionAssert.AreEquivalent(new[] { "rotated-rt" }, ReadCacheSecrets(serializedCache, "RefreshToken"));
            var cached = await ordinaryApp.AcquireTokenSilent(s_scopes, restoredAccount)
                .ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("ordinary-at", cached.AccessToken);
            Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
            var stillCachedGrant = await ordinaryApp.AcquireTokenSilent(s_scopes, restoredAccount)
                .WithIdJag(Audience, Resource).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("refreshed-grant", stillCachedGrant.AccessToken);
            Assert.AreEqual(TokenSource.Cache, stillCachedGrant.AuthenticationResultMetadata.TokenSource);
        }

        [TestMethod]
        [TestCategory("IdJagFunctional")]
        [DataRow(false)]
        [DataRow(true)]
        public async Task RepeatedGrantsPreserveSerializedOrdinaryTokensAsync(bool useObo)
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            byte[] serializedCache = null;
            IConfidentialClientApplication app = BuildApp(httpManager);
            AttachSerializedCache(app, () => serializedCache, data => serializedCache = data);
            var assertion = new UserAssertion("validated-incoming-access-token");
            string grantType = useObo ? "urn:ietf:params:oauth:grant-type:jwt-bearer" : "authorization_code";
            AddTokenResponse(httpManager, CreateResponse("ordinary-at", "initial-rt", 3600),
                new Dictionary<string, string> { ["grant_type"] = grantType }, idJag: false);
            var ordinary = useObo
                ? await app.AcquireTokenOnBehalfOf(s_scopes, assertion).ExecuteAsync().ConfigureAwait(false)
                : await app.AcquireTokenByAuthorizationCode(s_scopes, "code").ExecuteAsync().ConfigureAwait(false);
            CollectionAssert.AreEquivalent(new[] { "ordinary-at" }, ReadCacheSecrets(serializedCache, "AccessToken"));
            CollectionAssert.AreEquivalent(useObo ? Array.Empty<string>() : new[] { "initial-rt" },
                ReadCacheSecrets(serializedCache, "RefreshToken"));
            var requests = new[]
            {
                (Audience, Resource, Scopes: s_scopes),
                (Audience, Resource, Scopes: s_scopes),
                (Audience: Audience + "other", Resource, Scopes: s_scopes),
                (Audience, Resource: Resource + "other", Scopes: s_scopes),
                (Audience, Resource, Scopes: new[] { "agent.admin" }),
                (Audience, Resource: (string)null, Scopes: s_scopes)
            };
            string refreshToken = "initial-rt";
            var expectedTokens = new List<string> { "ordinary-at" };
            int requestIndex = 0;

            // Act / Assert
            foreach (var request in requests)
            {
                IConfidentialClientApplication restarted = BuildApp(httpManager);
                AttachSerializedCache(restarted, () => serializedCache, data => serializedCache = data);
                IAccount account = null;
                if (!useObo)
                {
                    account = await restarted.GetAccountAsync(ordinary.Account.HomeAccountId.Identifier).ConfigureAwait(false);
                    Assert.IsNotNull(account);
                }
                bool cacheHit = requestIndex++ == 1;
                string nextRefreshToken = cacheHit ? refreshToken : refreshToken + "-rotated";
                var expected = new Dictionary<string, string>
                {
                    ["grant_type"] = useObo ? "urn:ietf:params:oauth:grant-type:jwt-bearer" : "refresh_token"
                };
                if (useObo)
                {
                    expected["assertion"] = assertion.Assertion;
                    expected["requested_token_use"] = "on_behalf_of";
                }
                else
                {
                    expected["refresh_token"] = refreshToken;
                }
                string grantToken = "grant-for-" + nextRefreshToken;
                if (!cacheHit)
                {
                    AddTokenResponse(httpManager, CreateResponse(grantToken, nextRefreshToken, 900, scopes: request.Scopes),
                        expected, resource: request.Resource, audience: request.Audience, scopes: request.Scopes);
                    expectedTokens.Add(grantToken);
                }
                var grant = useObo
                    ? await restarted.AcquireTokenOnBehalfOf(request.Scopes, assertion)
                        .WithIdJag(request.Audience, request.Resource).ExecuteAsync().ConfigureAwait(false)
                    : await restarted.AcquireTokenSilent(request.Scopes, account)
                        .WithIdJag(request.Audience, request.Resource).ExecuteAsync().ConfigureAwait(false);
                Assert.AreEqual(grantToken, grant.AccessToken);
                Assert.AreEqual(cacheHit ? TokenSource.Cache : TokenSource.IdentityProvider, grant.AuthenticationResultMetadata.TokenSource);
                CollectionAssert.AreEquivalent(expectedTokens, ReadCacheSecrets(serializedCache, "AccessToken"));
                CollectionAssert.AreEquivalent(useObo ? Array.Empty<string>() : new[] { nextRefreshToken },
                    ReadCacheSecrets(serializedCache, "RefreshToken"));

                var cached = useObo
                    ? await restarted.AcquireTokenOnBehalfOf(s_scopes, assertion).ExecuteAsync().ConfigureAwait(false)
                    : await restarted.AcquireTokenSilent(s_scopes, account).ExecuteAsync().ConfigureAwait(false);
                Assert.AreEqual("ordinary-at", cached.AccessToken);
                Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
                CollectionAssert.AreEquivalent(expectedTokens, ReadCacheSecrets(serializedCache, "AccessToken"));
                refreshToken = nextRefreshToken;
            }
        }

        [TestMethod]
        [TestCategory("IdJagFunctional")]
        public async Task SerializedAccountsKeepRefreshTokensIsolatedAndRespectRemovalAsync()
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            byte[] serializedCache = null;
            IConfidentialClientApplication app = BuildApp(httpManager);
            AttachSerializedCache(app, () => serializedCache, data => serializedCache = data);
            var accounts = new List<IAccount>();
            foreach (string user in new[] { "first-user", "second-user" })
            {
                AddTokenResponse(httpManager, CreateResponse(user + "-at", user + "-rt", 3600, uid: user),
                    new Dictionary<string, string> { ["grant_type"] = "authorization_code" }, idJag: false);
                var session = await app.AcquireTokenByAuthorizationCode(s_scopes, user + "-code")
                    .ExecuteAsync().ConfigureAwait(false);
                accounts.Add(session.Account);
                Assert.HasCount(accounts.Count, ReadCacheSecrets(serializedCache, "AccessToken"));
                Assert.HasCount(accounts.Count, ReadCacheSecrets(serializedCache, "RefreshToken"));
            }
            CollectionAssert.AreEquivalent(new[] { "first-user-at", "second-user-at" },
                ReadCacheSecrets(serializedCache, "AccessToken"));
            CollectionAssert.AreEquivalent(new[] { "first-user-rt", "second-user-rt" },
                ReadCacheSecrets(serializedCache, "RefreshToken"));

            // Act
            IConfidentialClientApplication restarted = BuildApp(httpManager);
            AttachSerializedCache(restarted, () => serializedCache, data => serializedCache = data);
            var first = await restarted.GetAccountAsync(accounts[0].HomeAccountId.Identifier).ConfigureAwait(false);
            var second = await restarted.GetAccountAsync(accounts[1].HomeAccountId.Identifier).ConfigureAwait(false);
            Assert.IsNotNull(first);
            Assert.IsNotNull(second);
            AddTokenResponse(httpManager, CreateResponse("first-user-grant", "first-user-rotated-rt", uid: "first-user"),
                new Dictionary<string, string> { ["grant_type"] = "refresh_token", ["refresh_token"] = "first-user-rt" });
            var firstGrant = await restarted.AcquireTokenSilent(s_scopes, first)
                .WithIdJag(Audience).ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual(first.HomeAccountId.Identifier, firstGrant.Account.HomeAccountId.Identifier);
            CollectionAssert.AreEquivalent(new[] { "first-user-at", "second-user-at", "first-user-grant" },
                ReadCacheSecrets(serializedCache, "AccessToken"));
            CollectionAssert.AreEquivalent(new[] { "first-user-rotated-rt", "second-user-rt" },
                ReadCacheSecrets(serializedCache, "RefreshToken"));
            await restarted.RemoveAsync(first).ConfigureAwait(false);
            Assert.IsNull(await restarted.GetAccountAsync(first.HomeAccountId.Identifier).ConfigureAwait(false));
            CollectionAssert.AreEquivalent(new[] { "second-user-at" }, ReadCacheSecrets(serializedCache, "AccessToken"));
            CollectionAssert.AreEquivalent(new[] { "second-user-rt" }, ReadCacheSecrets(serializedCache, "RefreshToken"));
            await Assert.ThrowsAsync<MsalUiRequiredException>(() => restarted.AcquireTokenSilent(s_scopes, first)
                .WithIdJag(Audience).ExecuteAsync()).ConfigureAwait(false);
            CollectionAssert.AreEquivalent(new[] { "second-user-rt" }, ReadCacheSecrets(serializedCache, "RefreshToken"));

            AddTokenResponse(httpManager, CreateResponse("second-user-grant", null, includeIdToken: false, uid: "second-user"),
                new Dictionary<string, string> { ["grant_type"] = "refresh_token", ["refresh_token"] = "second-user-rt" });
            var secondGrant = await restarted.AcquireTokenSilent(s_scopes, second)
                .WithIdJag(Audience).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual(second.HomeAccountId.Identifier, secondGrant.Account.HomeAccountId.Identifier);
            CollectionAssert.AreEquivalent(new[] { "second-user-at", "second-user-grant" }, ReadCacheSecrets(serializedCache, "AccessToken"));
            CollectionAssert.AreEquivalent(new[] { "second-user-rt" }, ReadCacheSecrets(serializedCache, "RefreshToken"));
            var ordinary = await restarted.AcquireTokenSilent(s_scopes, second).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("second-user-at", ordinary.AccessToken);
            Assert.AreEqual(TokenSource.Cache, ordinary.AuthenticationResultMetadata.TokenSource);
        }

        [TestMethod]
        [TestCategory("IdJagFunctional")]
        public async Task FailedSilentGrantPreservesSerializedSessionAndCanRecoverAsync()
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            byte[] serializedCache = null;
            int cacheWrites = 0;
            IConfidentialClientApplication app = BuildApp(httpManager);
            AttachSerializedCache(app, () => serializedCache, data =>
            {
                serializedCache = data;
                cacheWrites++;
            });
            AddTokenResponse(httpManager, CreateResponse("ordinary-at", "initial-rt", 3600),
                new Dictionary<string, string> { ["grant_type"] = "authorization_code" }, idJag: false);
            var session = await app.AcquireTokenByAuthorizationCode(s_scopes, "code").ExecuteAsync().ConfigureAwait(false);
            byte[] beforeFailure = serializedCache.ToArray();
            int writesBeforeFailure = cacheWrites;
            AddTokenResponse(httpManager, MockHelpers.CreateFailureTokenResponseMessage("invalid_grant"),
                new Dictionary<string, string> { ["grant_type"] = "refresh_token", ["refresh_token"] = "initial-rt" });

            // Act
            var error = await Assert.ThrowsAsync<MsalUiRequiredException>(() => app.AcquireTokenSilent(s_scopes, session.Account)
                .WithIdJag(Audience).ExecuteAsync()).ConfigureAwait(false);

            // Assert
            Assert.AreEqual("invalid_grant", error.ErrorCode);
            Assert.AreEqual(writesBeforeFailure, cacheWrites);
            CollectionAssert.AreEqual(beforeFailure, serializedCache);
            var ordinary = await app.AcquireTokenSilent(s_scopes, session.Account).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("ordinary-at", ordinary.AccessToken);
            Assert.AreEqual(TokenSource.Cache, ordinary.AuthenticationResultMetadata.TokenSource);
            CollectionAssert.AreEqual(beforeFailure, serializedCache);

            IConfidentialClientApplication restarted = BuildApp(httpManager);
            AttachSerializedCache(restarted, () => serializedCache, data => serializedCache = data);
            var account = await restarted.GetAccountAsync(session.Account.HomeAccountId.Identifier).ConfigureAwait(false);
            Assert.IsNotNull(account);
            AddTokenResponse(httpManager, CreateResponse("recovered-grant", "recovered-rt", includeIdToken: false),
                new Dictionary<string, string> { ["grant_type"] = "refresh_token", ["refresh_token"] = "initial-rt" });
            var recovered = await restarted.AcquireTokenSilent(s_scopes, account)
                .WithIdJag(Audience).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("recovered-grant", recovered.AccessToken);
            CollectionAssert.AreEquivalent(new[] { "ordinary-at", "recovered-grant" }, ReadCacheSecrets(serializedCache, "AccessToken"));
            CollectionAssert.AreEquivalent(new[] { "recovered-rt" }, ReadCacheSecrets(serializedCache, "RefreshToken"));
        }

        [TestMethod]
        [TestCategory("IdJagFunctional")]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CachedGrantsAreReusedWithoutMatchingOrdinaryTokensAsync(bool useObo)
        {
            // Arrange
            using var httpManager = new MockHttpManager();
            IConfidentialClientApplication app = BuildApp(httpManager);
            var assertion = new UserAssertion("validated-incoming-access-token");
            AddTokenResponse(httpManager, CreateResponse("ordinary-at", "initial-rt", 3600),
                new Dictionary<string, string>
                {
                    ["grant_type"] = useObo ? "urn:ietf:params:oauth:grant-type:jwt-bearer" : "authorization_code"
                }, idJag: false);
            var session = useObo
                ? await app.AcquireTokenOnBehalfOf(s_scopes, assertion).ExecuteAsync().ConfigureAwait(false)
                : await app.AcquireTokenByAuthorizationCode(s_scopes, "code").ExecuteAsync().ConfigureAwait(false);
            AddTokenResponse(httpManager, CreateResponse("cached-grant", "rotated-rt", 900),
                new Dictionary<string, string>
                {
                    ["grant_type"] = useObo ? "urn:ietf:params:oauth:grant-type:jwt-bearer" : "refresh_token"
                }, resource: Resource);

            // Act
            var first = useObo
                ? await app.AcquireTokenOnBehalfOf(s_scopes, assertion).WithIdJag(Audience, Resource).ExecuteAsync().ConfigureAwait(false)
                : await app.AcquireTokenSilent(s_scopes, session.Account).WithIdJag(Audience, Resource).ExecuteAsync().ConfigureAwait(false);
            var cached = useObo
                ? await app.AcquireTokenOnBehalfOf(s_scopes, assertion).WithIdJag(Audience, Resource).ExecuteAsync().ConfigureAwait(false)
                : await app.AcquireTokenSilent(s_scopes, session.Account).WithIdJag(Audience, Resource).ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.AreEqual("cached-grant", first.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, first.AuthenticationResultMetadata.TokenSource);
            Assert.AreEqual("cached-grant", cached.AccessToken);
            Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
            var ordinary = useObo
                ? await app.AcquireTokenOnBehalfOf(s_scopes, assertion).ExecuteAsync().ConfigureAwait(false)
                : await app.AcquireTokenSilent(s_scopes, session.Account).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("ordinary-at", ordinary.AccessToken);
            Assert.AreEqual(TokenSource.Cache, ordinary.AuthenticationResultMetadata.TokenSource);

            var expectedRefresh = new Dictionary<string, string>
            {
                ["grant_type"] = useObo ? "urn:ietf:params:oauth:grant-type:jwt-bearer" : "refresh_token"
            };
            if (!useObo)
            {
                expectedRefresh["refresh_token"] = "rotated-rt";
            }
            AddTokenResponse(httpManager, CreateResponse("replacement-grant", "replacement-rt"), expectedRefresh, resource: Resource);
            var refreshed = useObo
                ? await app.AcquireTokenOnBehalfOf(s_scopes, assertion).WithIdJag(Audience, Resource).WithForceRefresh(true).ExecuteAsync().ConfigureAwait(false)
                : await app.AcquireTokenSilent(s_scopes, session.Account).WithIdJag(Audience, Resource).WithForceRefresh(true).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("replacement-grant", refreshed.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, refreshed.AuthenticationResultMetadata.TokenSource);

            var expectedOrdinary = new Dictionary<string, string>
            {
                ["grant_type"] = useObo ? "urn:ietf:params:oauth:grant-type:jwt-bearer" : "refresh_token"
            };
            if (!useObo)
            {
                expectedOrdinary["refresh_token"] = "replacement-rt";
            }
            AddTokenResponse(httpManager, CreateResponse("replacement-at", null, 3600), expectedOrdinary, idJag: false);
            var refreshedOrdinary = useObo
                ? await app.AcquireTokenOnBehalfOf(s_scopes, assertion).WithForceRefresh(true).ExecuteAsync().ConfigureAwait(false)
                : await app.AcquireTokenSilent(s_scopes, session.Account).WithForceRefresh(true).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("replacement-at", refreshedOrdinary.AccessToken);
            var retainedGrant = useObo
                ? await app.AcquireTokenOnBehalfOf(s_scopes, assertion).WithIdJag(Audience, Resource).ExecuteAsync().ConfigureAwait(false)
                : await app.AcquireTokenSilent(s_scopes, session.Account).WithIdJag(Audience, Resource).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("replacement-grant", retainedGrant.AccessToken);
            Assert.AreEqual(TokenSource.Cache, retainedGrant.AuthenticationResultMetadata.TokenSource);
        }

        [TestMethod]
        [TestCategory("IdJagFunctional")]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(true, true)]
        public async Task CachedGrantsRespectExpiryAndClaimsAsync(bool useObo, bool useClaims)
        {
            using var httpManager = new MockHttpManager();
            IConfidentialClientApplication app = BuildApp(httpManager);
            var assertion = new UserAssertion("validated-incoming-access-token");
            AddTokenResponse(httpManager, CreateResponse("initial-grant", "initial-rt", useClaims ? 900 : 1),
                new Dictionary<string, string>
                {
                    ["grant_type"] = useObo ? "urn:ietf:params:oauth:grant-type:jwt-bearer" : "authorization_code"
                });
            var session = useObo
                ? await app.AcquireTokenOnBehalfOf(s_scopes, assertion).WithIdJag(Audience).ExecuteAsync().ConfigureAwait(false)
                : await app.AcquireTokenByAuthorizationCode(s_scopes, "code").WithIdJag(Audience).ExecuteAsync().ConfigureAwait(false);
            string claims = useClaims ? "{\"access_token\":{\"acrs\":{\"essential\":true,\"value\":\"c1\"}}}" : null;
            var expected = new Dictionary<string, string>
            {
                ["grant_type"] = useObo ? "urn:ietf:params:oauth:grant-type:jwt-bearer" : "refresh_token"
            };
            if (useClaims)
            {
                expected["claims"] = claims;
            }
            AddTokenResponse(httpManager, CreateResponse("refreshed-grant", "rotated-rt"), expected);
            var refreshed = useObo
                ? await app.AcquireTokenOnBehalfOf(s_scopes, assertion).WithIdJag(Audience).WithClaims(claims).ExecuteAsync().ConfigureAwait(false)
                : await app.AcquireTokenSilent(s_scopes, session.Account).WithIdJag(Audience).WithClaims(claims).ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("refreshed-grant", refreshed.AccessToken);
            Assert.AreEqual(TokenSource.IdentityProvider, refreshed.AuthenticationResultMetadata.TokenSource);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task ReservedCacheKeyNamespaceIsRejectedAsync(bool idJag)
        {
            using var httpManager = new MockHttpManager();
            var app = BuildApp(httpManager);
            var request = app.AcquireTokenByAuthorizationCode(s_scopes, "code");
            request.WithAdditionalCacheKeyComponents(new SortedList<string, Func<CancellationToken, Task<string>>>
                {
                    ["msal.id_jag.token_type"] = cancellationToken => Task.FromResult(IdJagTokenType)
                });
            if (idJag)
            {
                request.WithIdJag(Audience);
            }
            await Assert.ThrowsAsync<ArgumentException>(() => request.ExecuteAsync()).ConfigureAwait(false);
            Assert.IsEmpty(app.UserTokenCacheInternal.Accessor.GetAllAccessTokens());
            Assert.IsEmpty(app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CallerCacheKeysPreserveRefreshTokenPartitioningAsync(bool partitionRefreshToken)
        {
            using var httpManager = new MockHttpManager();
            var app = BuildApp(httpManager);
            var components = new SortedList<string, Func<CancellationToken, Task<string>>>
            {
                ["caller-partition"] = cancellationToken => Task.FromResult("first-partition")
            };
            AddTokenResponse(httpManager, CreateResponse("ordinary-at", "initial-rt", 3600),
                new Dictionary<string, string> { ["grant_type"] = "authorization_code" }, idJag: false);
            var bootstrapRequest = app.AcquireTokenByAuthorizationCode(s_scopes, "code");
            bootstrapRequest.WithAdditionalCacheKeyComponents(components);
            bootstrapRequest.CommonParameters.PartitionRefreshToken = partitionRefreshToken;
            var session = await bootstrapRequest.ExecuteAsync().ConfigureAwait(false);
            AddTokenResponse(httpManager, CreateResponse("grant", "rotated-rt"),
                new Dictionary<string, string> { ["grant_type"] = "refresh_token", ["refresh_token"] = "initial-rt" });
            var grantRequest = app.AcquireTokenSilent(s_scopes, session.Account).WithIdJag(Audience);
            grantRequest.WithAdditionalCacheKeyComponents(components);
            grantRequest.CommonParameters.PartitionRefreshToken = partitionRefreshToken;
            var grant = await grantRequest.ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("grant", grant.AccessToken);
            var cachedRequest = app.AcquireTokenSilent(s_scopes, session.Account).WithIdJag(Audience);
            cachedRequest.WithAdditionalCacheKeyComponents(components);
            cachedRequest.CommonParameters.PartitionRefreshToken = partitionRefreshToken;
            var cached = await cachedRequest.ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("grant", cached.AccessToken);
            Assert.AreEqual(TokenSource.Cache, cached.AuthenticationResultMetadata.TokenSource);
            var ordinaryRequest = app.AcquireTokenSilent(s_scopes, session.Account);
            ordinaryRequest.WithAdditionalCacheKeyComponents(components);
            ordinaryRequest.CommonParameters.PartitionRefreshToken = partitionRefreshToken;
            var ordinary = await ordinaryRequest.ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual("ordinary-at", ordinary.AccessToken);
            Assert.AreEqual(TokenSource.Cache, ordinary.AuthenticationResultMetadata.TokenSource);
            var refreshToken = app.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Single();
            Assert.AreEqual("rotated-rt", refreshToken.Secret);
            if (partitionRefreshToken)
            {
                Assert.HasCount(1, refreshToken.AdditionalCacheKeyComponents);
                Assert.AreEqual("first-partition", refreshToken.AdditionalCacheKeyComponents["caller-partition"]);
            }
            else
            {
                Assert.IsTrue(refreshToken.AdditionalCacheKeyComponents is null || refreshToken.AdditionalCacheKeyComponents.Count == 0);
            }
        }

        private static void AttachSerializedCache(
            IConfidentialClientApplication app, Func<byte[]> read, Action<byte[]> write)
        {
            app.UserTokenCache.SetBeforeAccess(args =>
            {
                byte[] serializedCache = read();
                if (serializedCache is not null)
                {
                    args.TokenCache.DeserializeMsalV3(serializedCache, shouldClearExistingCache: true);
                }
            });
            app.UserTokenCache.SetAfterAccess(args =>
            {
                if (args.HasStateChanged)
                {
                    write(args.TokenCache.SerializeMsalV3());
                }
            });
        }

        private static string[] ReadCacheSecrets(byte[] serializedCache, string credentialType)
        {
            Assert.IsNotNull(serializedCache);
            using var document = JsonDocument.Parse(serializedCache);
            return document.RootElement.TryGetProperty(credentialType, out var credentials)
                ? credentials.EnumerateObject().Select(item => item.Value.GetProperty("secret").GetString()).ToArray()
                : Array.Empty<string>();
        }

        private static ConfidentialClientApplication BuildApp(MockHttpManager httpManager)
        {
            return ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithAuthority(TestConstants.AuthorityHomeTenant)
                .WithRedirectUri("https://app.example/callback")
                .WithInstanceDiscovery(false)
                .WithExperimentalFeatures()
                .WithHttpManager(httpManager)
                .BuildConcrete();
        }

        private static HttpResponseMessage CreateResponse(
            string token, string refreshToken, int lifetime = 900, bool includeIdToken = true,
            string[] scopes = null, string uid = TestConstants.Uid)
        {
            return MockHelpers.CreateSuccessResponseMessage(JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["access_token"] = token,
                ["token_type"] = "Bearer",
                ["expires_in"] = lifetime,
                ["scope"] = string.Join(" ", scopes ?? s_scopes),
                ["refresh_token"] = refreshToken,
                ["client_info"] = MockHelpers.CreateClientInfo(uid),
                ["id_token"] = includeIdToken ? MockHelpers.CreateIdToken(uid, uid + "@example.com") : null,
                ["issued_token_type"] = IdJagTokenType
            }));
        }

        private static void AddTokenResponse(
            MockHttpManager httpManager,
            HttpResponseMessage response,
            Dictionary<string, string> expectedPostData,
            bool idJag = true,
            string resource = null,
            string audience = Audience,
            string[] scopes = null)
        {
            expectedPostData["client_id"] = TestConstants.ClientId;
            expectedPostData["client_secret"] = TestConstants.ClientSecret;
            expectedPostData["scope"] = string.Join(" ", scopes ?? s_scopes) + " openid profile offline_access";
            if (idJag)
            {
                expectedPostData["requested_token_type"] = IdJagTokenType;
                expectedPostData["audience"] = audience;
                if (resource is not null)
                {
                    expectedPostData["resource"] = resource;
                }
            }

            var unexpected = new Dictionary<string, string> { ["subject_token"] = null, ["subject_token_type"] = null };
            if (!idJag)
            {
                unexpected["requested_token_type"] = null;
                unexpected["audience"] = null;
            }
            if (resource is null)
            {
                unexpected["resource"] = null;
            }

            httpManager.AddMockHandler(new MockHttpMessageHandler
            {
                ExpectedUrl = TestConstants.AuthorityHomeTenant + "oauth2/v2.0/token",
                ExpectedMethod = HttpMethod.Post,
                ExpectedPostData = expectedPostData,
                UnExpectedPostData = unexpected,
                ResponseMessage = response
            });
        }
    }
}