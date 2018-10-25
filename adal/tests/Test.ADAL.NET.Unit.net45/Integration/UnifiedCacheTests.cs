//----------------------------------------------------------------------
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
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AuthenticationContext = Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext;
using PromptBehavior = Microsoft.IdentityModel.Clients.ActiveDirectory.PromptBehavior;
using Test.ADAL.NET.Common;
using Test.ADAL.NET.Common.Mocks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Identity.Core.OAuth2;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.OAuth2;
using Microsoft.Identity.Core.UI;

namespace Test.ADAL.NET.Integration
{
#if !NET_CORE  // PromptBehavior
    [TestClass]
    public class UnifiedCacheTests
    {
        private PlatformParameters _platformParameters;

        [TestInitialize]
        public void Initialize()
        {
            AdalHttpMessageHandlerFactory.InitializeMockProvider();
            _platformParameters = new PlatformParameters(PromptBehavior.Auto);
            InstanceDiscovery.InstanceCache.Clear();
        }

        [TestMethod]
        [Description("Test unified token cache")]
        public async Task UnifedCache_AdalStoresToAndReadRtFromMsalCacheAsync()
        {
            AdalHttpMessageHandlerFactory.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler(
                AdalTestConstants.GetDiscoveryEndpoint(AdalTestConstants.DefaultAuthorityCommonTenant)));

            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                AdalTestConstants.DefaultRedirectUri + "?code=some-code"));
            AdalHttpMessageHandlerFactory.AddMockHandler(
                new MockHttpMessageHandler(AdalTestConstants.GetTokenEndpoint(AdalTestConstants.DefaultAuthorityCommonTenant))
                {
                    Method = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
                });

            var adalTokenCache = TokenCache.DefaultShared;

            var adalContext = new AuthenticationContext(AdalTestConstants.DefaultAuthorityCommonTenant, true);
            var result =
                await
                    adalContext.AcquireTokenAsync(AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId,
                        AdalTestConstants.DefaultRedirectUri, _platformParameters).ConfigureAwait(false);

            Assert.IsTrue(adalTokenCache.Count > 0);
            Assert.IsNotNull(result);

            IEnumerable<TokenCacheItem> tokenCacheItems = adalTokenCache.ReadItems();

            Assert.IsTrue(adalTokenCache.tokenCacheAccessor.GetAllAccessTokensAsString().Count == 0);
            Assert.IsTrue(adalTokenCache.tokenCacheAccessor.GetAllRefreshTokensAsString().Count > 0);

            // clear Adal Cache
            adalTokenCache.ClearAdalCache();

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
                (AdalTestConstants.GetTokenEndpoint(AdalTestConstants.TenantSpecificAuthority))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>
                    {
                        {"client_id", AdalTestConstants.DefaultClientId},
                        {"grant_type", OAuth2GrantType.RefreshToken},
                        {"refresh_token", AdalTestConstants.DefaultRefreshTokenValue},
                        {"resource", AdalTestConstants.DefaultResource},
                        {"scope", OAuthValue.ScopeOpenId}
                    }
            });

            // get refresh token from Msal Cache
            result =
                await
                    adalContext.AcquireTokenAsync(AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId,
                        AdalTestConstants.DefaultRedirectUri, _platformParameters,
                        new UserIdentifier(result.UserInfo.DisplayableId, UserIdentifierType.RequiredDisplayableId)).ConfigureAwait(false);

            //ps todo validate that state in adal is same as was before adal cache clean
            Assert.IsNotNull(result);
        }
    }
#endif
}
