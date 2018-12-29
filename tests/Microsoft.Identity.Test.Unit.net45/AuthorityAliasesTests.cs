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
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    public class AuthorityAliasesTests
    {
        internal void TestInitialize(MockHttpManager httpManager)
        {
            TestCommon.ResetStateAndInitMsal();

            new AadInstanceDiscovery(null, null, true);
            new ValidatedAuthoritiesCache(true);

            httpManager.AddMockHandler(
                MockHelpers.CreateInstanceDiscoveryMockHandler(
                    MsalTestConstants.GetDiscoveryEndpoint(MsalTestConstants.AuthorityCommonTenant)));
        }

#if !NET_CORE
        [TestMethod]
        [Description("Test authority migration")]
        public void AuthorityMigration_IntegrationTest()
        {
            // make sure that for all network calls "preferred_cache" environment is used
            // (it is taken from metadata in instance discovery response),
            // except very first network call - instance discovery

            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);
                TestInitialize(httpManager);

                PublicClientApplication app = new PublicClientApplication(
                    serviceBundle,
                    MsalTestConstants.ClientId,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "https://{0}/common",
                        MsalTestConstants.ProductionNotPrefEnvironmentAlias))
                {
                    UserTokenCache =
                    {
                        LegacyCachePersistence = new TestLegacyCachePersistance()
                    }
                };

                // mock for openId config request
                httpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    Url = string.Format(CultureInfo.InvariantCulture, "https://{0}/common/v2.0/.well-known/openid-configuration",
                        MsalTestConstants.ProductionPrefNetworkEnvironment),
                    Method = HttpMethod.Get,
                    ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(MsalTestConstants.AuthorityHomeTenant)
                });

                // mock webUi authorization
                MsalMockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                    app.RedirectUri + "?code=some-code"), null, MsalTestConstants.ProductionPrefNetworkEnvironment);

                // mock token request
                httpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    Url = string.Format(CultureInfo.InvariantCulture, "https://{0}/home/oauth2/v2.0/token",
                        MsalTestConstants.ProductionPrefNetworkEnvironment),
                    Method = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
                });

                AuthenticationResult result = app.AcquireTokenAsync(MsalTestConstants.Scope).Result;

                // make sure that all cache entities are stored with "preferred_cache" environment
                // (it is taken from metadata in instance discovery response)
                ValidateCacheEntitiesEnvironment(app.UserTokenCache, MsalTestConstants.ProductionPrefCacheEnvironment);

                // silent request targeting at, should return at from cache for any environment alias
                foreach (var envAlias in MsalTestConstants.ProdEnvAliases)
                {
                    result = app.AcquireTokenSilentAsync(MsalTestConstants.Scope,
                        app.GetAccountsAsync().Result.First(),
                        string.Format(CultureInfo.InvariantCulture, "https://{0}/{1}/", envAlias, MsalTestConstants.Utid),
                        false).Result;

                    Assert.IsNotNull(result);
                }

                // mock for openId config request for tenant spesific authority
                httpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    Url = string.Format(CultureInfo.InvariantCulture, "https://{0}/{1}/v2.0/.well-known/openid-configuration",
                        MsalTestConstants.ProductionPrefNetworkEnvironment, MsalTestConstants.Utid),
                    Method = HttpMethod.Get,
                    ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(MsalTestConstants.AuthorityUtidTenant)
                });

                // silent request targeting rt should find rt in cahce for authority with any environment alias
                foreach (var envAlias in MsalTestConstants.ProdEnvAliases)
                {
                    httpManager.AddMockHandler(new MockHttpMessageHandler()
                    {
                        Url = string.Format(CultureInfo.InvariantCulture, "https://{0}/{1}/oauth2/v2.0/token",
                            MsalTestConstants.ProductionPrefNetworkEnvironment, MsalTestConstants.Utid),
                        Method = HttpMethod.Post,
                        PostData = new Dictionary<string, string>()
                    {
                        {"grant_type", "refresh_token"}
                    },
                        // return not retriable status code
                        ResponseMessage = MockHelpers.CreateInvalidGrantTokenResponseMessage()
                    });

                    try
                    {
                        result = null;
                        result = app.AcquireTokenSilentAsync(MsalTestConstants.ScopeForAnotherResource,
                            app.GetAccountsAsync().Result.First(),
                            string.Format(CultureInfo.InvariantCulture, "https://{0}/{1}/", envAlias, MsalTestConstants.Utid),
                            false).Result;
                    }
                    catch (AggregateException ex)
                    {
                        Assert.IsNotNull(ex.InnerException);
                        Assert.IsTrue(ex.InnerException is MsalUiRequiredException);
                    }

                    Assert.IsNull(result);
                }
            }
        }
#endif

        private void ValidateCacheEntitiesEnvironment(TokenCache cache, string expectedEnvironment)
        {
            var requestContext = new RequestContext(null, new MsalLogger(Guid.NewGuid(), null));
            ICollection<MsalAccessTokenCacheItem> accessTokens = cache.GetAllAccessTokensForClient(requestContext);
            foreach (var at in accessTokens)
            {
                Assert.AreEqual(expectedEnvironment, at.Environment);
            }

            ICollection<MsalRefreshTokenCacheItem> refreshTokens = cache.GetAllRefreshTokensForClient(requestContext);
            foreach (var rt in refreshTokens)
            {
                Assert.AreEqual(expectedEnvironment, rt.Environment);
            }

            ICollection<MsalIdTokenCacheItem> idTokens = cache.GetAllIdTokensForClient(requestContext);
            foreach (var id in idTokens)
            {
                Assert.AreEqual(expectedEnvironment, id.Environment);
            }

            ICollection<MsalAccountCacheItem> accounts = cache.GetAllAccounts(requestContext);
            foreach (var account in accounts)
            {
                Assert.AreEqual(expectedEnvironment, account.Environment);
            }

            IDictionary<AdalTokenCacheKey, AdalResultWrapper> adalCache =
                AdalCacheOperations.Deserialize(cache.LegacyCachePersistence.LoadCache());

            foreach (KeyValuePair<AdalTokenCacheKey, AdalResultWrapper> kvp in adalCache)
            {
                Assert.AreEqual(expectedEnvironment, new Uri(kvp.Key.Authority).Host);
            }
        }
    }
}