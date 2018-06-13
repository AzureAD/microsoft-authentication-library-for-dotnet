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

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Http;
using Test.ADAL.Common;
using Test.ADAL.NET.Common;
using Test.ADAL.NET.Common.Mocks;
using AuthenticationContext = Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Cache;
using CoreHttpClientFactory = Microsoft.Identity.Core.Http.HttpClientFactory;
using CoreHttpMessageHandlerFactory = Microsoft.Identity.Core.Http.HttpMessageHandlerFactory;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform;

namespace Test.ADAL.NET.Integration
{
    [TestClass]
    public class ManagedUserCredentialTests
    {
        [TestInitialize]
        public void Initialize()
        {
            AdalHttpMessageHandlerFactory.InitializeMockProvider();
            ResetInstanceDiscovery();
            CoreHttpClientFactory.ReturnHttpClientForMocks = true;
            CoreHttpMessageHandlerFactory.ClearMockHandlers();
        }

        public void ResetInstanceDiscovery()
        {
            InstanceDiscovery.InstanceCache.Clear();
            AdalHttpMessageHandlerFactory.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler(TestConstants.GetDiscoveryEndpoint(TestConstants.DefaultAuthorityCommonTenant)));
        }

        [TestMethod]
        [Description("Test for AcquireToken with an empty cache")]
        public async Task AcquireTokenWithEmptyCache_GetsTokenFromServiceTestAsync()
        {
            CoreHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(TestConstants.GetUserRealmEndpoint(TestConstants.DefaultAuthorityCommonTenant) + "/" + TestConstants.DefaultDisplayableId
            )
            {
                Method = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content =
                        new StringContent("{\"ver\":\"1.0\",\"account_type\":\"Managed\",\"domain_name\":\"id.com\"}")
                },
                QueryParams = new Dictionary<string, string>()
                {
                    {"api-version", "1.0"}
                }
            });

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(TestConstants.GetTokenEndpoint(TestConstants.DefaultAuthorityHomeTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"grant_type", "password"},
                    {"username", TestConstants.DefaultDisplayableId},
                    {"password", TestConstants.DefaultPassword}
                }
            });

            TokenCache cache = new TokenCache();

            var context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, true, cache);
            var result =
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        new UserPasswordCredential(TestConstants.DefaultDisplayableId, TestConstants.DefaultPassword));
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, context.Authenticator.Authority);
            Assert.AreEqual("some-access-token", result.AccessToken);
            Assert.IsNotNull(result.UserInfo);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultUniqueId, result.UserInfo.UniqueId);

            // All mocks are consumed
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
            Assert.IsTrue(CoreHttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");

            // There should be one cached entry
            Assert.AreEqual(1, context.TokenCache.Count);
        }

        [TestMethod]
        [Description("Test for AcquireToken with valid token in cache")]
        public async Task AcquireTokenWithValidTokenInCache_ReturnsCachedTokenAsync()
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

            var result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                                                         new UserPasswordCredential(TestConstants.DefaultDisplayableId, TestConstants.DefaultPassword));

            Assert.IsNotNull(result);
            Assert.AreEqual("existing-access-token", result.AccessToken);
            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, context.Authenticator.Authority);
            Assert.IsNotNull(result.UserInfo);

            // There should be one cached entry.
            Assert.AreEqual(1, context.TokenCache.Count);
        }

        [TestMethod]
        [Description("Test for AcquireToken for a user when a valid access token already exists in cache for another user.")]
        public async Task AcquireTokenWithValidAccessTokenInCacheForAnotherUser_GetsTokenFromServiceAsync()
        {
            CoreHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(
            TestConstants.DefaultAuthorityCommonTenant + "userrealm/user2@id.com")
            {
                Method = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content =
            new StringContent("{\"ver\":\"1.0\",\"account_type\":\"Managed\",\"domain_name\":\"id.com\"}")
                },
                QueryParams = new Dictionary<string, string>()
                {
                    {"api-version", "1.0"}
                }
            });

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(TestConstants.GetTokenEndpoint(TestConstants.DefaultAuthorityHomeTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(TestConstants.DefaultUniqueId + "2", "user2@id.com", TestConstants.DefaultResource),
                PostData = new Dictionary<string, string>()
                {
                    {"grant_type", "password"},
                    {"username", "user2@id.com"},
                    {"password", TestConstants.DefaultPassword},
                }
            });

            TokenCache cache = new TokenCache();

            var context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, true, new TokenCache());

            AdalTokenCacheKey key = new AdalTokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
            TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User,
            TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId);
            var setupResult = new AdalResultWrapper
            {
                RefreshToken = "some-rt",
                ResourceInResponse = TestConstants.DefaultResource,
                Result = new AdalResult("Bearer", "existing-access-token",
                    DateTimeOffset.UtcNow + +TimeSpan.FromMinutes(100))
            };

            setupResult.Result.UserInfo = new AdalUserInfo();
            setupResult.Result.UserInfo.DisplayableId = TestConstants.DefaultDisplayableId;
            context.TokenCache.tokenCacheDictionary[key] = setupResult;

            var result =
                await
                    context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                        new UserPasswordCredential("user2@id.com", TestConstants.DefaultPassword));

            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, context.Authenticator.Authority);
            Assert.AreEqual("some-access-token", result.AccessToken);
            Assert.IsNotNull(result.UserInfo);
            Assert.AreEqual("user2@id.com", result.UserInfo.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultUniqueId + "2", result.UserInfo.UniqueId);

            // There should be only two cache entrys.
            Assert.AreEqual(2, context.TokenCache.Count);

            var keys = context.TokenCache.tokenCacheDictionary.Values.ToList();
            var values = context.TokenCache.tokenCacheDictionary.Values.ToList();
            Assert.AreNotEqual(keys[0].Result.UserInfo.UniqueId, keys[1].Result.UserInfo.UniqueId);
            Assert.AreNotEqual(values[0].Result.UserInfo.UniqueId, values[1].Result.UserInfo.UniqueId);
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
            Assert.IsTrue(CoreHttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [Description("Test case with expired access token and valid refresh token in cache. This should result in refresh token being used to get new AT instead of user creds")]
        public async Task AcquireTokenWithExpiredAccessTokenAndValidRefreshToken_GetsATUsingRTAsync()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, true, new TokenCache());

            await context.TokenCache.StoreToCacheAsync(new AdalResultWrapper
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
                        },
                    IdToken = MockHelpers.CreateIdToken(TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId)
                },
            },
            TestConstants.DefaultAuthorityHomeTenant, TestConstants.DefaultResource, TestConstants.DefaultClientId, TokenSubjectType.User,
            new RequestContext(new AdalLogger(new Guid())));
            ResetInstanceDiscovery();

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(TestConstants.GetTokenEndpoint(TestConstants.DefaultAuthorityHomeTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                PostData = new Dictionary<string, string>()
                {
                    {"client_id", TestConstants.DefaultClientId},
                    {"grant_type", "refresh_token"}
                }
            });

            var result = await context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                                                         new UserPasswordCredential(TestConstants.DefaultDisplayableId, TestConstants.DefaultPassword));

            Assert.IsNotNull(result);
            Assert.AreEqual("some-access-token", result.AccessToken);
            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, context.Authenticator.Authority);
            Assert.IsNotNull(result.UserInfo);

            // Cache entry updated with new access token
            var entry = await context.TokenCache.LoadFromCacheAsync(new CacheQueryData
            {
                Authority = TestConstants.DefaultAuthorityHomeTenant,
                Resource = TestConstants.DefaultResource,
                ClientId = TestConstants.DefaultClientId,
                SubjectType = TokenSubjectType.User,
                UniqueId = TestConstants.DefaultUniqueId,
                DisplayableId = TestConstants.DefaultDisplayableId
            },
            new RequestContext(new AdalLogger(new Guid())));
            Assert.AreEqual("some-access-token", entry.Result.AccessToken);

            // There should be one cached entry.
            Assert.AreEqual(1, context.TokenCache.Count);

            // All mocks are consumed
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
        }

        [TestMethod]
        [Description("Test case where user realm discovery fails.")]
        public void UserRealmDiscoveryFailsTest()
        {
            AuthenticationContext context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant);

            CoreHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(TestConstants.GetUserRealmEndpoint(TestConstants.DefaultAuthorityCommonTenant) + "/" + TestConstants.DefaultDisplayableId)
            {
                Method = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Bad request received")
                },
                QueryParams = new Dictionary<string, string>()
                {
                    {"api-version", "1.0"}
                }
            });

            var ex = AssertException.TaskThrows<AdalException>(() =>
            context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                                                         new UserPasswordCredential(TestConstants.DefaultDisplayableId,
                                                                                    TestConstants.DefaultPassword)));
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
            Assert.IsTrue(CoreHttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
            Assert.AreEqual(0, context.TokenCache.Count);

            //To be addressed in a later fix
            //Assert.AreEqual(((AdalException)ex.InnerException.InnerException).ErrorCode, AdalError.UserRealmDiscoveryFailed);
        }

        [TestMethod]
        [Description("Test case where user realm discovery cannot determine the user type.")]
        public async Task UnknownUserRealmDiscoveryTestAsync()
        {
            AuthenticationContext context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant);
            await context.Authenticator.UpdateFromTemplateAsync(null);

            CoreHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(TestConstants.GetUserRealmEndpoint(TestConstants.DefaultAuthorityCommonTenant) + "/" + TestConstants.DefaultDisplayableId)
            {
                Method = HttpMethod.Get,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"ver\":\"1.0\",\"account_type\":\"Unknown\",\"cloud_instance_name\":\"login.microsoftonline.com\"}")
                },
                QueryParams = new Dictionary<string, string>()
                {
                    {"api-version", "1.0"}
                }
            });

            var ex = AssertException.TaskThrows<AdalException>(() =>
            context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId,
                                                         new UserPasswordCredential(TestConstants.DefaultDisplayableId,
                                                                                    TestConstants.DefaultPassword)));

            Assert.AreEqual(AdalError.UnknownUserType, ex.ErrorCode);
            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
            Assert.IsTrue(CoreHttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
            Assert.AreEqual(0, context.TokenCache.Count);
        }
    }
}
