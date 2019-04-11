// ------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// ------------------------------------------------------------------------------

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
                ValidateCacheEntitiesEnvironment(app.UserTokenCacheInternal, MsalTestConstants.ProductionPrefCacheEnvironment);

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

        private void ValidateCacheEntitiesEnvironment(ITokenCacheInternal cache, string expectedEnvironment)
        {
            var requestContext = RequestContext.CreateForTest();
            var accessTokens = cache.GetAllAccessTokens(true);
            foreach (var at in accessTokens)
            {
                Assert.AreEqual(expectedEnvironment, at.Environment);
            }

            var refreshTokens = cache.GetAllRefreshTokens(true);
            foreach (var rt in refreshTokens)
            {
                Assert.AreEqual(expectedEnvironment, rt.Environment);
            }

            var idTokens = cache.GetAllIdTokens(true);
            foreach (var id in idTokens)
            {
                Assert.AreEqual(expectedEnvironment, id.Environment);
            }

            var accounts = cache.GetAllAccounts();
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
