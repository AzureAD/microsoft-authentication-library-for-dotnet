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
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class SilentRequestTests
    {
        private TokenCache _cache;
        private TokenCacheHelper _tokenCacheHelper;

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetStateAndInitMsal();
            _cache = new TokenCache();
            _tokenCacheHelper = new TokenCacheHelper();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (_cache != null)
            {
                _cache.TokenCacheAccessor.ClearAccessTokens();
                _cache.TokenCacheAccessor.ClearRefreshTokens();
            }
        }

        [TestMethod]
        [TestCategory("SilentRequestTests")]
        public void ConstructorTests()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);
                var aadInstanceDiscovery = new AadInstanceDiscovery(httpManager, new TelemetryManager());
                var authority = Authority.CreateAuthority(serviceBundle, MsalTestConstants.AuthorityHomeTenant, false);
                var cache = new TokenCache()
                {
                    ClientId = MsalTestConstants.ClientId,
                    ServiceBundle = serviceBundle
                };
                var parameters = new AuthenticationRequestParameters()
                {
                    Authority = authority,
                    ClientId = MsalTestConstants.ClientId,
                    Scope = MsalTestConstants.Scope,
                    TokenCache = cache,
                    Account = new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null),
                    RequestContext = new RequestContext(null, new MsalLogger(Guid.NewGuid(), null))
                };

                var crypto = PlatformProxyFactory.GetPlatformProxy().CryptographyManager;
                var telemetryManager = new TelemetryManager();

                var request = new SilentRequest(serviceBundle, parameters, ApiEvent.ApiIds.None, false);
                Assert.IsNotNull(request);

                parameters.Account = new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null);

                request = new SilentRequest(serviceBundle, parameters, ApiEvent.ApiIds.None, false);
                Assert.IsNotNull(request);

                request = new SilentRequest(serviceBundle, parameters, ApiEvent.ApiIds.None, false);
                Assert.IsNotNull(request);
            }
        }

        [TestMethod]
        [TestCategory("SilentRequestTests")]
        public void ExpiredTokenRefreshFlowTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);
                var aadInstanceDiscovery = new AadInstanceDiscovery(httpManager, new TelemetryManager());
                Authority authority = Authority.CreateAuthority(serviceBundle, MsalTestConstants.AuthorityHomeTenant, false);
                TokenCache cache = new TokenCache()
                {
                    ClientId = MsalTestConstants.ClientId,
                    ServiceBundle = serviceBundle
                };
                _tokenCacheHelper.PopulateCache(cache.TokenCacheAccessor);

                AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
                {
                    Authority = authority,
                    ClientId = MsalTestConstants.ClientId,
                    Scope = MsalTestConstants.Scope,
                    TokenCache = cache,
                    RequestContext = new RequestContext(null, new MsalLogger(Guid.Empty, null)),
                    Account = new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null)
                };

                // set access tokens as expired
                foreach (var atCacheItemStr in cache.GetAllAccessTokenCacheItems(new RequestContext(null, new MsalLogger(Guid.NewGuid(), null))))
                {
                    MsalAccessTokenCacheItem accessItem =
                        JsonHelper.DeserializeFromJson<MsalAccessTokenCacheItem>(atCacheItemStr);
                    accessItem.ExpiresOnUnixTimestamp =
                        ((long)((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds)).ToString(CultureInfo.InvariantCulture);

                    cache.AddAccessTokenCacheItem(accessItem);
                }
                TestCommon.MockInstanceDiscoveryAndOpenIdRequest(httpManager);

                httpManager.AddMockHandler(new MockHttpMessageHandler()
                {
                    Method = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
                });

                var crypto = PlatformProxyFactory.GetPlatformProxy().CryptographyManager;

                SilentRequest request = new SilentRequest(
                    serviceBundle,
                    parameters,
                    ApiEvent.ApiIds.None,
                    false);

                Task<AuthenticationResult> task = request.RunAsync(CancellationToken.None);
                AuthenticationResult result = task.Result;
                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.AreEqual(MsalTestConstants.Scope.AsSingleString(), result.Scopes.AsSingleString());

            }
        }

        [TestMethod]
        [TestCategory("SilentRequestTests")]
        public void SilentRefreshFailedNullCacheTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);
                var aadInstanceDiscovery = new AadInstanceDiscovery(httpManager, new TelemetryManager());
                var authority = Authority.CreateAuthority(serviceBundle, MsalTestConstants.AuthorityHomeTenant, false);
                _cache = null;

                var parameters = new AuthenticationRequestParameters()
                {
                    Authority = authority,
                    ClientId = MsalTestConstants.ClientId,
                    Scope = ScopeHelper.CreateSortedSetFromEnumerable(
                        new[]
                        {
                            "some-scope1",
                            "some-scope2"
                        }),
                    TokenCache = _cache,
                    Account = new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null),
                    RequestContext = new RequestContext(null, new MsalLogger(Guid.NewGuid(), null))
                };

                var crypto = PlatformProxyFactory.GetPlatformProxy().CryptographyManager;
                var telemetryManager = new TelemetryManager();
                try
                {
                    var request = new SilentRequest(serviceBundle, parameters, ApiEvent.ApiIds.None, false);
                    Task<AuthenticationResult> task = request.RunAsync(CancellationToken.None);
                    var authenticationResult = task.Result;
                    Assert.Fail("MsalUiRequiredException should be thrown here");
                }
                catch (AggregateException ae)
                {
                    var exc = ae.InnerException as MsalUiRequiredException;
                    Assert.IsNotNull(exc);
                    Assert.AreEqual(MsalUiRequiredException.TokenCacheNullError, exc.ErrorCode);
                }
            }
        }


        [TestMethod]
        [TestCategory("SilentRequestTests")]
        public void SilentRefreshFailedNoCacheItemFoundTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);
                var aadInstanceDiscovery = new AadInstanceDiscovery(httpManager, new TelemetryManager());
                var authority = Authority.CreateAuthority(serviceBundle, MsalTestConstants.AuthorityHomeTenant, false);
                _cache = new TokenCache()
                {
                    ClientId = MsalTestConstants.ClientId,
                    ServiceBundle = serviceBundle
                };

                httpManager.AddInstanceDiscoveryMockHandler();

                var parameters = new AuthenticationRequestParameters()
                {
                    Authority = authority,
                    ClientId = MsalTestConstants.ClientId,
                    Scope = ScopeHelper.CreateSortedSetFromEnumerable(
                        new[]
                        {
                            "some-scope1",
                            "some-scope2"
                        }),
                    TokenCache = _cache,
                    Account = new Account(MsalTestConstants.UserIdentifier, MsalTestConstants.DisplayableId, null),
                    RequestContext = new RequestContext(null, new MsalLogger(Guid.NewGuid(), null))
                };

                var crypto = PlatformProxyFactory.GetPlatformProxy().CryptographyManager;
                var telemetryManager = new TelemetryManager();

                try
                {
                    var request = new SilentRequest(serviceBundle, parameters, ApiEvent.ApiIds.None, false);
                    Task<AuthenticationResult> task = request.RunAsync(CancellationToken.None);
                    var authenticationResult = task.Result;
                    Assert.Fail("MsalUiRequiredException should be thrown here");
                }
                catch (AggregateException ae)
                {
                    var exc = ae.InnerException as MsalUiRequiredException;
                    Assert.IsNotNull(exc, "Actual exception type is " + ae.InnerException.GetType());
                    Assert.AreEqual(MsalUiRequiredException.NoTokensFoundError, exc.ErrorCode);
                }
            }
        }
    }
}
