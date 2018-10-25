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
using Test.ADAL.NET.Common;
using Test.ADAL.NET.Common.Mocks;
using AuthenticationContext = Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Cache;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform;
using Test.Microsoft.Identity.Core.Unit;

namespace Test.ADAL.NET.Integration
{
    [TestClass]
    public class ManagedUserCredentialTests
    {
        [TestInitialize]
        public void Initialize()
        {
            TokenCache.DefaultShared.Clear();
            AdalHttpMessageHandlerFactory.InitializeMockProvider();
            ResetInstanceDiscovery();
        }

        private void ResetInstanceDiscovery()
        {
            InstanceDiscovery.InstanceCache.Clear();
            AdalHttpMessageHandlerFactory.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler(AdalTestConstants.GetDiscoveryEndpoint(AdalTestConstants.DefaultAuthorityCommonTenant)));
        }

#if DESKTOP // UserPasswordCredential available only on net45
        [TestMethod]
        [Description("Test for AcquireToken with an empty cache")]
        public async Task AcquireTokenWithEmptyCache_GetsTokenFromServiceTestAsync()
        {
            using (var httpManager = new Microsoft.Identity.Core.Unit.Mocks.MockHttpManager())
            {
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler(
                        AdalTestConstants.GetUserRealmEndpoint(AdalTestConstants.DefaultAuthorityCommonTenant) + "/" +
                        AdalTestConstants.DefaultDisplayableId)
                    {
                        Method = HttpMethod.Get,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"ver\":\"1.0\",\"account_type\":\"Managed\",\"domain_name\":\"id.com\"}")
                        },
                        QueryParams = new Dictionary<string, string>()
                        {
                            {"api-version", "1.0"}
                        }
                    });

                AdalHttpMessageHandlerFactory.AddMockHandler(
                    new MockHttpMessageHandler(AdalTestConstants.GetTokenEndpoint(AdalTestConstants.DefaultAuthorityHomeTenant))
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                        PostData = new Dictionary<string, string>()
                        {
                            {"grant_type", "password"},
                            {"username", AdalTestConstants.DefaultDisplayableId},
                            {"password", AdalTestConstants.DefaultPassword}
                        }
                    });

                TokenCache cache = new TokenCache();

                var context = new AuthenticationContext(
                    httpManager,
                    AdalTestConstants.DefaultAuthorityHomeTenant,
                    AuthorityValidationType.True,
                    cache);

                Assert.AreEqual(0, context.TokenCache.Count);

                var result = await context.AcquireTokenAsync(
                                 AdalTestConstants.DefaultResource,
                                 AdalTestConstants.DefaultClientId,
                                 new UserPasswordCredential(AdalTestConstants.DefaultDisplayableId, AdalTestConstants.DefaultPassword)).ConfigureAwait(false);
                Assert.IsNotNull(result);
                Assert.AreEqual(AdalTestConstants.DefaultAuthorityHomeTenant, context.Authenticator.Authority);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.IsNotNull(result.UserInfo);
                Assert.AreEqual(AdalTestConstants.DefaultDisplayableId, result.UserInfo.DisplayableId);
                Assert.AreEqual(AdalTestConstants.DefaultUniqueId, result.UserInfo.UniqueId);

                // All mocks are consumed
                Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());

                // There should be one cached entry
                Assert.AreEqual(1, context.TokenCache.Count);
            }
        }
        [TestMethod]
        [Description("Test for AcquireToken with valid token in cache")]
        public async Task AcquireTokenWithValidTokenInCache_ReturnsCachedTokenAsync()
        {
            var context = new AuthenticationContext(AdalTestConstants.DefaultAuthorityHomeTenant, true, new TokenCache());

            AdalTokenCacheKey key = new AdalTokenCacheKey(AdalTestConstants.DefaultAuthorityHomeTenant,
                AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId, TokenSubjectType.User,
                AdalTestConstants.DefaultUniqueId, AdalTestConstants.DefaultDisplayableId);
            context.TokenCache.tokenCacheDictionary[key] = new AdalResultWrapper
            {
                RefreshToken = "some-rt",
                ResourceInResponse = AdalTestConstants.DefaultResource,
                Result = new AdalResult("Bearer", "existing-access-token",
                    DateTimeOffset.UtcNow + TimeSpan.FromMinutes(100))
            };

            var result = await context.AcquireTokenAsync(AdalTestConstants.DefaultResource, AdalTestConstants.DefaultClientId,
                                                         new UserPasswordCredential(AdalTestConstants.DefaultDisplayableId, AdalTestConstants.DefaultPassword)).ConfigureAwait(false);

            Assert.IsNotNull(result);
            Assert.AreEqual("existing-access-token", result.AccessToken);
            Assert.AreEqual(AdalTestConstants.DefaultAuthorityHomeTenant, context.Authenticator.Authority);
            Assert.IsNotNull(result.UserInfo);

            // There should be one cached entry.
            Assert.AreEqual(1, context.TokenCache.Count);
        }

        [TestMethod]
        [Description("Test for AcquireToken for a user when a valid access token already exists in cache for another user.")]
        public async Task AcquireTokenWithValidAccessTokenInCacheForAnotherUser_GetsTokenFromServiceAsync()
        {
            using (var httpManager = new Microsoft.Identity.Core.Unit.Mocks.MockHttpManager())
            {

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler(AdalTestConstants.DefaultAuthorityCommonTenant + "userrealm/user2@id.com")
                    {
                        Method = HttpMethod.Get,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"ver\":\"1.0\",\"account_type\":\"Managed\",\"domain_name\":\"id.com\"}")
                        },
                        QueryParams = new Dictionary<string, string>()
                        {
                            {"api-version", "1.0"}
                        }
                    });

                AdalHttpMessageHandlerFactory.AddMockHandler(
                    new MockHttpMessageHandler(AdalTestConstants.GetTokenEndpoint(AdalTestConstants.DefaultAuthorityHomeTenant))
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            AdalTestConstants.DefaultUniqueId + "2",
                            "user2@id.com",
                            AdalTestConstants.DefaultResource),
                        PostData = new Dictionary<string, string>()
                        {
                            {"grant_type", "password"},
                            {"username", "user2@id.com"},
                            {"password", AdalTestConstants.DefaultPassword},
                        }
                    });

                TokenCache cache = new TokenCache();

                var context = new AuthenticationContext(
                    httpManager,
                    AdalTestConstants.DefaultAuthorityHomeTenant,
                    AuthorityValidationType.True,
                    new TokenCache());

                AdalTokenCacheKey key = new AdalTokenCacheKey(
                    AdalTestConstants.DefaultAuthorityHomeTenant,
                    AdalTestConstants.DefaultResource,
                    AdalTestConstants.DefaultClientId,
                    TokenSubjectType.User,
                    AdalTestConstants.DefaultUniqueId,
                    AdalTestConstants.DefaultDisplayableId);
                var setupResult = new AdalResultWrapper
                {
                    RefreshToken = "some-rt",
                    ResourceInResponse = AdalTestConstants.DefaultResource,
                    Result = new AdalResult("Bearer", "existing-access-token", DateTimeOffset.UtcNow + +TimeSpan.FromMinutes(100))
                };

                setupResult.Result.UserInfo = new AdalUserInfo();
                setupResult.Result.UserInfo.DisplayableId = AdalTestConstants.DefaultDisplayableId;
                context.TokenCache.tokenCacheDictionary[key] = setupResult;

                var result = await context.AcquireTokenAsync(
                                 AdalTestConstants.DefaultResource,
                                 AdalTestConstants.DefaultClientId,
                                 new UserPasswordCredential("user2@id.com", AdalTestConstants.DefaultPassword)).ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(AdalTestConstants.DefaultAuthorityHomeTenant, context.Authenticator.Authority);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.IsNotNull(result.UserInfo);
                Assert.AreEqual("user2@id.com", result.UserInfo.DisplayableId);
                Assert.AreEqual(AdalTestConstants.DefaultUniqueId + "2", result.UserInfo.UniqueId);

                // There should be only two cache entries.
                Assert.AreEqual(2, context.TokenCache.Count);

                var keys = context.TokenCache.tokenCacheDictionary.Values.ToList();
                var values = context.TokenCache.tokenCacheDictionary.Values.ToList();
                Assert.AreNotEqual(keys[0].Result.UserInfo.UniqueId, keys[1].Result.UserInfo.UniqueId);
                Assert.AreNotEqual(values[0].Result.UserInfo.UniqueId, values[1].Result.UserInfo.UniqueId);
                Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
            }
        }

        [TestMethod]
        [Description("Test case with expired access token and valid refresh token in cache. This should result in refresh token being used to get new AT instead of user creds")]
        public async Task AcquireTokenWithExpiredAccessTokenAndValidRefreshToken_GetsATUsingRTAsync()
        {
            using (var httpManager = new Microsoft.Identity.Core.Unit.Mocks.MockHttpManager())
            {
                var context = new AuthenticationContext(
                    httpManager,
                    AdalTestConstants.DefaultAuthorityHomeTenant,
                    AuthorityValidationType.True,
                    new TokenCache());

                await context.TokenCache.StoreToCacheAsync(
                    new AdalResultWrapper
                    {
                        RefreshToken = "some-rt",
                        ResourceInResponse = AdalTestConstants.DefaultResource,
                        Result = new AdalResult("Bearer", "existing-access-token", DateTimeOffset.UtcNow)
                        {
                            UserInfo = new AdalUserInfo()
                            {
                                DisplayableId = AdalTestConstants.DefaultDisplayableId,
                                UniqueId = AdalTestConstants.DefaultUniqueId
                            },
                            IdToken = MockHelpers.CreateIdToken(
                                AdalTestConstants.DefaultUniqueId,
                                AdalTestConstants.DefaultDisplayableId)
                        },
                    },
                    AdalTestConstants.DefaultAuthorityHomeTenant,
                    AdalTestConstants.DefaultResource,
                    AdalTestConstants.DefaultClientId,
                    TokenSubjectType.User,
                    new RequestContext(new AdalLogger(new Guid()))).ConfigureAwait(false);
                ResetInstanceDiscovery();

                AdalHttpMessageHandlerFactory.AddMockHandler(
                    new MockHttpMessageHandler(AdalTestConstants.GetTokenEndpoint(AdalTestConstants.DefaultAuthorityHomeTenant))
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                        PostData = new Dictionary<string, string>()
                        {
                            {"client_id", AdalTestConstants.DefaultClientId},
                            {"grant_type", "refresh_token"}
                        }
                    });

                var result = await context.AcquireTokenAsync(
                                 AdalTestConstants.DefaultResource,
                                 AdalTestConstants.DefaultClientId,
                                 new UserPasswordCredential(AdalTestConstants.DefaultDisplayableId, AdalTestConstants.DefaultPassword)).ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("some-access-token", result.AccessToken);
                Assert.AreEqual(AdalTestConstants.DefaultAuthorityHomeTenant, context.Authenticator.Authority);
                Assert.IsNotNull(result.UserInfo);

                // Cache entry updated with new access token
                var entry = await context.TokenCache.LoadFromCacheAsync(
                                new CacheQueryData
                                {
                                    Authority = AdalTestConstants.DefaultAuthorityHomeTenant,
                                    Resource = AdalTestConstants.DefaultResource,
                                    ClientId = AdalTestConstants.DefaultClientId,
                                    SubjectType = TokenSubjectType.User,
                                    UniqueId = AdalTestConstants.DefaultUniqueId,
                                    DisplayableId = AdalTestConstants.DefaultDisplayableId
                                },
                                new RequestContext(new AdalLogger(new Guid()))).ConfigureAwait(false);
                Assert.AreEqual("some-access-token", entry.Result.AccessToken);

                // There should be one cached entry.
                Assert.AreEqual(1, context.TokenCache.Count);
            }
        }

        [TestMethod]
        [Description("Test case where user realm discovery fails.")]
        public void UserRealmDiscoveryFailsTest()
        {
            using (var httpManager = new Microsoft.Identity.Core.Unit.Mocks.MockHttpManager())
            {
                AuthenticationContext context = new AuthenticationContext(
                    httpManager,
                    AdalTestConstants.DefaultAuthorityCommonTenant,
                    AuthorityValidationType.NotProvided,
                    TokenCache.DefaultShared);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler(
                        AdalTestConstants.GetUserRealmEndpoint(AdalTestConstants.DefaultAuthorityCommonTenant) + "/" +
                        AdalTestConstants.DefaultDisplayableId)
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

                var ex = AssertException.TaskThrows<AdalException>(
                    () => context.AcquireTokenAsync(
                        AdalTestConstants.DefaultResource,
                        AdalTestConstants.DefaultClientId,
                        new UserPasswordCredential(AdalTestConstants.DefaultDisplayableId, AdalTestConstants.DefaultPassword)));
                Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
                Assert.AreEqual(0, context.TokenCache.Count);

                //To be addressed in a later fix
                //Assert.AreEqual(((AdalException)ex.InnerException.InnerException).ErrorCode, AdalError.UserRealmDiscoveryFailed);
            }
        }

        [TestMethod]
        [Description("Test case where user realm discovery cannot determine the user type.")]
        public async Task UnknownUserRealmDiscoveryTestAsync()
        {
            Assert.AreEqual(1, AdalHttpMessageHandlerFactory.MockHandlersCount());

            using (var httpManager = new Microsoft.Identity.Core.Unit.Mocks.MockHttpManager())
            {
                AuthenticationContext context = new AuthenticationContext(
                    httpManager,
                    AdalTestConstants.DefaultAuthorityCommonTenant,
                    AuthorityValidationType.NotProvided,
                    TokenCache.DefaultShared);

                await context.Authenticator.UpdateFromTemplateAsync(null).ConfigureAwait(false);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler(
                        AdalTestConstants.GetUserRealmEndpoint(AdalTestConstants.DefaultAuthorityCommonTenant) + "/" +
                        AdalTestConstants.DefaultDisplayableId)
                    {
                        Method = HttpMethod.Get,
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"ver\":\"1.0\",\"account_type\":\"Unknown\",\"cloud_instance_name\":\"login.microsoftonline.com\"}")
                        },
                        QueryParams = new Dictionary<string, string>()
                        {
                            {"api-version", "1.0"}
                        }
                    });

                var ex = AssertException.TaskThrows<AdalException>(
                    () => context.AcquireTokenAsync(
                        AdalTestConstants.DefaultResource,
                        AdalTestConstants.DefaultClientId,
                        new UserPasswordCredential(AdalTestConstants.DefaultDisplayableId, AdalTestConstants.DefaultPassword)));

                Assert.AreEqual(AdalError.UnknownUserType, ex.ErrorCode);
                Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
                Assert.AreEqual(0, context.TokenCache.Count);
            }
        }
#endif

    }

}
