// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class AuthorityAliasesTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

#if !NET_CORE
        [TestMethod]
        [Description("Test authority migration")]
        public async Task AuthorityMigrationTestAsync()
        {
            // make sure that for all network calls "preferred_cache" environment is used
            // (it is taken from metadata in instance discovery response),
            // except very first network call - instance discovery

            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var authorityUri = new Uri(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "https://{0}/common",
                        MsalTestConstants.ProductionNotPrefEnvironmentAlias));

                var app = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId)
                          .WithAuthority(authorityUri, true)
                          .WithHttpManager(httpManager)
                          .WithUserTokenLegacyCachePersistenceForTest(new TestLegacyCachePersistance())
                          .BuildConcrete();

                // mock for openId config request
                httpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    ExpectedUrl = string.Format(CultureInfo.InvariantCulture, "https://{0}/common/v2.0/.well-known/openid-configuration",
                        MsalTestConstants.ProductionPrefNetworkEnvironment),
                    ExpectedMethod = HttpMethod.Get,
                    ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(MsalTestConstants.AuthorityHomeTenant)
                });

                // mock webUi authorization
                MsalMockHelpers.ConfigureMockWebUI(
                    app.ServiceBundle.PlatformProxy,
                    new AuthorizationResult(AuthorizationStatus.Success,
                    app.AppConfig.RedirectUri + "?code=some-code"), null, MsalTestConstants.ProductionPrefNetworkEnvironment);

                // mock token request
                httpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    ExpectedUrl = string.Format(CultureInfo.InvariantCulture, "https://{0}/home/oauth2/v2.0/token",
                        MsalTestConstants.ProductionPrefNetworkEnvironment),
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
                });

                AuthenticationResult result = app.AcquireTokenInteractive(MsalTestConstants.Scope).ExecuteAsync(CancellationToken.None).Result;

                // make sure that all cache entities are stored with "preferred_cache" environment
                // (it is taken from metadata in instance discovery response)
                await ValidateCacheEntitiesEnvironmentAsync(app.UserTokenCacheInternal, MsalTestConstants.ProductionPrefCacheEnvironment).ConfigureAwait(false);

                // silent request targeting at, should return at from cache for any environment alias
                foreach (var envAlias in MsalTestConstants.ProdEnvAliases)
                {
                    result = await app
                        .AcquireTokenSilent(
                            MsalTestConstants.Scope,
                            app.GetAccountsAsync().Result.First())
                        .WithAuthority(string.Format(CultureInfo.InvariantCulture, "https://{0}/{1}/", envAlias, MsalTestConstants.Utid))
                        .WithForceRefresh(false)
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.IsNotNull(result);
                }

                // mock for openId config request for tenant specific authority
                httpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    ExpectedUrl = string.Format(CultureInfo.InvariantCulture, "https://{0}/{1}/v2.0/.well-known/openid-configuration",
                        MsalTestConstants.ProductionPrefNetworkEnvironment, MsalTestConstants.Utid),
                    ExpectedMethod = HttpMethod.Get,
                    ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(MsalTestConstants.AuthorityUtidTenant)
                });

                // silent request targeting rt should find rt in cache for authority with any environment alias
                foreach (var envAlias in MsalTestConstants.ProdEnvAliases)
                {
                    result = null;

                    httpManager.AddMockHandler(new MockHttpMessageHandler()
                    {
                        ExpectedUrl = string.Format(CultureInfo.InvariantCulture, "https://{0}/{1}/oauth2/v2.0/token",
                            MsalTestConstants.ProductionPrefNetworkEnvironment, MsalTestConstants.Utid),
                        ExpectedMethod = HttpMethod.Post,
                        ExpectedPostData = new Dictionary<string, string>()
                    {
                        {"grant_type", "refresh_token"}
                    },
                        // return not retriable status code
                        ResponseMessage = MockHelpers.CreateInvalidGrantTokenResponseMessage()
                    });

                    try
                    {
                        result = await app
                            .AcquireTokenSilent(
                                MsalTestConstants.ScopeForAnotherResource,
                                (await app.GetAccountsAsync().ConfigureAwait(false)).First())
                            .WithAuthority(string.Format(CultureInfo.InvariantCulture, "https://{0}/{1}/", envAlias, MsalTestConstants.Utid))
                            .WithForceRefresh(false)
                            .ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (MsalUiRequiredException)
                    {
                    }
                    catch (Exception)
                    {
                        Assert.Fail();
                    }

                    Assert.IsNull(result);
                }
            }
        }
#endif

        private async Task ValidateCacheEntitiesEnvironmentAsync(ITokenCacheInternal cache, string expectedEnvironment)
        {
            var requestContext = RequestContext.CreateForTest();
            var accessTokens = await cache.GetAllAccessTokensAsync(true).ConfigureAwait(false);
            foreach (var at in accessTokens)
            {
                Assert.AreEqual(expectedEnvironment, at.Environment);
            }

            var refreshTokens = await cache.GetAllRefreshTokensAsync(true).ConfigureAwait(false);
            foreach (var rt in refreshTokens)
            {
                Assert.AreEqual(expectedEnvironment, rt.Environment);
            }

            var idTokens = await cache.GetAllIdTokensAsync(true).ConfigureAwait(false);
            foreach (var id in idTokens)
            {
                Assert.AreEqual(expectedEnvironment, id.Environment);
            }

            var accounts = await cache.GetAllAccountsAsync().ConfigureAwait(false);
            foreach (var account in accounts)
            {
                Assert.AreEqual(expectedEnvironment, account.Environment);
            }

            IDictionary<AdalTokenCacheKey, AdalResultWrapper> adalCache =
                AdalCacheOperations.Deserialize(requestContext.Logger, cache.LegacyPersistence.LoadCache());

            foreach (KeyValuePair<AdalTokenCacheKey, AdalResultWrapper> kvp in adalCache)
            {
                Assert.AreEqual(expectedEnvironment, new Uri(kvp.Key.Authority).Host);
            }
        }
    }
}
