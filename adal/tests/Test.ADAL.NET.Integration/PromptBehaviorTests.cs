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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using Test.ADAL.Common;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Http;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform;
using Test.ADAL.NET.Common;
using Test.ADAL.NET.Common.Mocks;
using AuthenticationContext = Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext;
using Microsoft.Identity.Core.UI;

namespace Test.ADAL.NET.Integration
{
    [TestClass]
    public class PromptBehaviorTests
    {
        [TestInitialize]
        public void Initialize()
        {
            HttpMessageHandlerFactory.InitializeMockProvider();
            ResetInstanceDiscovery();
        }

        public void ResetInstanceDiscovery()
        {
            InstanceDiscovery.InstanceCache.Clear();
            HttpMessageHandlerFactory.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler(TestConstants.GetDiscoveryEndpoint(TestConstants.DefaultAuthorityCommonTenant)));
        }
        
        [TestMethod]
        [Description("Test for PromptBehavior.Auto, prompts only if necessary")]
        public async Task AutoPromptBehaviorTestAsync()
        {
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                TestConstants.DefaultRedirectUri + "?code=some-code"));

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(TestConstants.GetTokenEndpoint(TestConstants.DefaultAuthorityHomeTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"grant_type", "authorization_code"}
                }
            });

            var context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, true, new TokenCache());
            AuthenticationResult result =
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        TestConstants.DefaultRedirectUri, new PlatformParameters(Microsoft.IdentityModel.Clients.ActiveDirectory.PromptBehavior.Auto));

            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, context.Authenticator.Authority);
            Assert.AreEqual("some-access-token", result.AccessToken);
            Assert.IsNotNull(result.UserInfo);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultUniqueId, result.UserInfo.UniqueId);
            // There should be one cached entry.
            Assert.AreEqual(1, context.TokenCache.Count);

            Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount());
        }
        
        [TestMethod]
        [Description("Test for calling promptBehavior.Auto when cache already has an access token")]
        public async Task AutoPromptBehaviorWithTokenInCacheTest()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, true, new TokenCache());

            AdalTokenCacheKey key = new AdalTokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId);
            context.TokenCache.tokenCacheDictionary[key] = new AdalResultWrapper
            {
                RefreshToken = "some-rt",
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AdalResult("Bearer", "existing-access-token",
                    DateTimeOffset.UtcNow + TimeSpan.FromMinutes(100))
            };

            AuthenticationResult result =
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        TestConstants.DefaultRedirectUri, new PlatformParameters(Microsoft.IdentityModel.Clients.ActiveDirectory.PromptBehavior.Auto));

            Assert.IsNotNull(result);
            Assert.AreEqual("existing-access-token", result.AccessToken);

            // There should be only one cache entry.
            Assert.AreEqual(1, context.TokenCache.Count);

            Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount());
        }

        [TestMethod]
        [Description("Test for calling promptBehavior.Auto when cache has an expired access token, but a good refresh token")]
        public async Task AutoPromptBehaviorWithExpiredAccessTokenAndGoodRefreshTokenInCacheTestAsync()
        {
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                TestConstants.DefaultRedirectUri + "?code=some-code"));

            var context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, true, new TokenCache());

            await context.TokenCache.StoreToCache(new AdalResultWrapper
            {
                RefreshToken = "some-rt",
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AdalResult("Bearer", "existing-access-token", DateTimeOffset.UtcNow)
                {
                    UserInfo =
                        new AdalUserInfo()
                        {
                            DisplayableId = TestConstants.DefaultDisplayableId,
                            UniqueId = TestConstants.DefaultUniqueId
                        }
                },
            },
            TestConstants.DefaultAuthorityHomeTenant, TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User,
            new RequestContext(new AdalLogger(new Guid())));
            ResetInstanceDiscovery();

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(TestConstants.GetTokenEndpoint(TestConstants.DefaultAuthorityHomeTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"grant_type", "refresh_token"}
                }
            });

            AuthenticationResult result =
                await
                    context.AcquireTokenSilentAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                    new UserIdentifier(TestConstants.DefaultDisplayableId, UserIdentifierType.RequiredDisplayableId),
                    new PlatformParameters(Microsoft.IdentityModel.Clients.ActiveDirectory.PromptBehavior.Auto));

            Assert.IsNotNull(result);
            Assert.AreEqual("some-access-token", result.AccessToken);

            // There should be only one cache entry.
            Assert.AreEqual(1, context.TokenCache.Count);

            Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount());
        }

        [TestMethod]
        [Description("Test for Force Prompt with PromptBehavior.Always")]
        public async Task ForcePromptForAlwaysPromptBehaviorTestAsync()
        {
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                TestConstants.DefaultRedirectUri + "?code=some-code"));

            var context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, true, new TokenCache());

            await context.TokenCache.StoreToCache(new AdalResultWrapper
            {
                RefreshToken = "some-rt",
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AdalResult("Bearer", "existing-access-token", DateTimeOffset.UtcNow)
                {
                    UserInfo =
                        new AdalUserInfo()
                        {
                            DisplayableId = TestConstants.DefaultDisplayableId,
                            UniqueId = TestConstants.DefaultUniqueId
                        }
                },
            },
            TestConstants.DefaultAuthorityHomeTenant, TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User,
            new RequestContext(new AdalLogger(new Guid())));
            ResetInstanceDiscovery();

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(TestConstants.GetTokenEndpoint(TestConstants.DefaultAuthorityHomeTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"grant_type", "authorization_code"}
                }
            });

            AuthenticationResult result =
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        TestConstants.DefaultRedirectUri, new PlatformParameters(Microsoft.IdentityModel.Clients.ActiveDirectory.PromptBehavior.Always));

            Assert.IsNotNull(result);
            Assert.AreEqual("some-access-token", result.AccessToken);

            // There should be only one cache entry.
            Assert.AreEqual(1, context.TokenCache.Count);

            Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount());
        }
        
        [TestMethod]
        [Description("Test for Force Prompt with PromptBehavior.SelectAccount")]
        public async Task ForcePromptForSelectAccountPromptBehaviorTestAsync()
        {
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                    TestConstants.DefaultRedirectUri + "?code=some-code"),
                // validate that authorizationUri passed to WebUi contains prompt=select_account query parameter
                new Dictionary<string, string> { { "prompt", "select_account" } });

            var context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, true, new TokenCache());

            await context.TokenCache.StoreToCache(new AdalResultWrapper
            {
                RefreshToken = "some-rt",
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AdalResult("Bearer", "existing-access-token",
                    DateTimeOffset.UtcNow + TimeSpan.FromMinutes(100))
                {
                    UserInfo =
                        new AdalUserInfo()
                        {
                            DisplayableId = TestConstants.DefaultDisplayableId,
                            UniqueId = TestConstants.DefaultUniqueId
                        }
                },
            },
            TestConstants.DefaultAuthorityHomeTenant, TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User,
            new RequestContext(new AdalLogger(new Guid())));
            ResetInstanceDiscovery();

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(TestConstants.GetTokenEndpoint(TestConstants.DefaultAuthorityHomeTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>
                {
                    {"grant_type", "authorization_code"}
                }
            });

            AuthenticationResult result =
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        TestConstants.DefaultRedirectUri, new PlatformParameters(Microsoft.IdentityModel.Clients.ActiveDirectory.PromptBehavior.SelectAccount));

            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, context.Authenticator.Authority);
            Assert.AreEqual("some-access-token", result.AccessToken);
            Assert.IsNotNull(result.UserInfo);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultUniqueId, result.UserInfo.UniqueId);

            // There should be only one cache entry.
            Assert.AreEqual(1, context.TokenCache.Count);

            Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount());
        }
        
        [TestMethod]
        [Description("Test for Force Prompt with PromptBehavior.Never")]
        public void ForcePromptForNeverPromptBehaviorTestAsync()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, new TokenCache());

            AdalTokenCacheKey key = new AdalTokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
               TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User,
               TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId);
            context.TokenCache.tokenCacheDictionary[key] = new AdalResultWrapper
            {
                RefreshToken = "some-rt",
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AdalResult("Bearer", "existing-access-token", DateTimeOffset.UtcNow)
            };

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(TestConstants.GetTokenEndpoint(TestConstants.DefaultAuthorityHomeTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateInvalidRequestTokenResponseMessage()
            });

            var exc = AssertException.TaskThrows<AdalServiceException>(() =>
            context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        TestConstants.DefaultRedirectUri, new PlatformParameters(Microsoft.IdentityModel.Clients.ActiveDirectory.PromptBehavior.Never)));

            Assert.AreEqual(AdalError.FailedToRefreshToken, exc.ErrorCode);
            // There should be only one cache entry.
            Assert.AreEqual(1, context.TokenCache.Count);

            Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount());
        }
        
        [TestMethod]
        [Description("Test for Force Prompt with PromptBehavior.RefreshSession")]
        public async Task ForcePromptForRefreshSessionPromptBehaviorTestAsync()
        {
            MockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                TestConstants.DefaultRedirectUri + "?code=some-code"),
                // validate that authorizationUri passed to WebUi contains prompt=refresh_session query parameter
                new Dictionary<string, string> { { "prompt", "refresh_session" } });

            var context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, true, new TokenCache());

            await context.TokenCache.StoreToCache(new AdalResultWrapper
            {
                RefreshToken = "some-rt",
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AdalResult("Bearer", "existing-access-token", DateTimeOffset.UtcNow)
                {
                    UserInfo =
                        new AdalUserInfo()
                        {
                            DisplayableId = TestConstants.DefaultDisplayableId,
                            UniqueId = TestConstants.DefaultUniqueId
                        }
                },
            },
            TestConstants.DefaultAuthorityHomeTenant, TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User,
            new RequestContext(new AdalLogger(new Guid())));
            ResetInstanceDiscovery();

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(TestConstants.GetTokenEndpoint(TestConstants.DefaultAuthorityHomeTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"grant_type", "authorization_code"}
                }
            });

            AuthenticationResult result =
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        TestConstants.DefaultRedirectUri, new PlatformParameters(Microsoft.IdentityModel.Clients.ActiveDirectory.PromptBehavior.RefreshSession));

            Assert.IsNotNull(result);
            Assert.AreEqual("some-access-token", result.AccessToken);

            // There should be only one cache entry.
            Assert.AreEqual(1, context.TokenCache.Count);

            Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount());
        }
    }
}
