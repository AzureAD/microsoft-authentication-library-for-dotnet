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
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Http;
using Microsoft.Identity.Client.Internal.Instance;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.MSAL.NET.Unit.Mocks;

namespace Test.MSAL.NET.Unit.RequestsTests
{
    [TestClass]
    public class SilentRequestTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            Authority._validatedAuthorities.Clear();
            TokenCache.DefaultSharedAppTokenCache = new TokenCache();
            TokenCache.DefaultSharedUserTokenCache = new TokenCache();
            HttpClientFactory.ReturnHttpClientForMocks = true;
            HttpMessageHandlerFactory.ClearMockHandlers();
        }

        [TestMethod]
        [TestCategory("SilentRequestTests")]
        public void ConstructorTests()
        {
            Authority authenticator = Authority.CreateAuthority(TestConstants.DefaultAuthorityHomeTenant, false);
            TokenCache cache = new TokenCache();
            AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
            {
                Authority = authenticator,
                ClientKey = new ClientKey(TestConstants.DefaultClientId),
                Policy = TestConstants.DefaultPolicy,
                RestrictToSingleUser = true,
                Scope = TestConstants.DefaultScope,
                TokenCache = cache
            };

            SilentRequest request = new SilentRequest(parameters, (string) null,
                new PlatformParameters(), false);
            Assert.IsNotNull(request);

            request = new SilentRequest(parameters, (User) null, new PlatformParameters(), false);
            Assert.IsNotNull(request);

            request = new SilentRequest(parameters, TestConstants.DefaultDisplayableId, new PlatformParameters(), false);
            Assert.IsNotNull(request);

            request = new SilentRequest(parameters, TestConstants.DefaultUniqueId, new PlatformParameters(), false);
            Assert.IsNotNull(request);

            request = new SilentRequest(parameters, TestConstants.DefaultUser, new PlatformParameters(), false);
            Assert.IsNotNull(request);
        }


        [TestMethod]
        [TestCategory("SilentRequestTests")]
        public void MapToIdentifierNullInputTest()
        {
            Authority authenticator = Authority.CreateAuthority(TestConstants.DefaultAuthorityHomeTenant, false);
            TokenCache cache = new TokenCache();
            AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
            {
                Authority = authenticator,
                ClientKey = new ClientKey(TestConstants.DefaultClientId),
                Policy = TestConstants.DefaultPolicy,
                RestrictToSingleUser = true,
                Scope = TestConstants.DefaultScope,
                TokenCache = cache
            };

            SilentRequest request = new SilentRequest(parameters, (string)null,
                new PlatformParameters(), false);
            User user = request.MapIdentifierToUser(null);
            Assert.IsNull(user);
        }

        [TestMethod]
        [TestCategory("SilentRequestTests")]
        public void MapToIdentifierNoItemFoundTest()
        {
            Authority authenticator = Authority.CreateAuthority(TestConstants.DefaultAuthorityHomeTenant, false);
            TokenCache cache = new TokenCache();
            AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
            {
                Authority = authenticator,
                ClientKey = new ClientKey(TestConstants.DefaultClientId),
                Policy = TestConstants.DefaultPolicy,
                RestrictToSingleUser = true,
                Scope = TestConstants.DefaultScope,
                TokenCache = cache
            };

            SilentRequest request = new SilentRequest(parameters, (string) null,
                new PlatformParameters(), false);
            User user = request.MapIdentifierToUser(TestConstants.DefaultUniqueId);
            Assert.IsNull(user);
        }

        [TestMethod]
        [TestCategory("SilentRequestTests")]
        public void MapToIdentifierItemFoundTest()
        {
            Authority authenticator = Authority.CreateAuthority(TestConstants.DefaultAuthorityHomeTenant, false);
            TokenCache cache = TokenCacheHelper.CreateCacheWithItems();
            AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
            {
                Authority = authenticator,
                ClientKey = new ClientKey(TestConstants.DefaultClientId),
                Policy = TestConstants.DefaultPolicy,
                RestrictToSingleUser = TestConstants.DefaultRestrictToSingleUser,
                Scope = TestConstants.DefaultScope,
                TokenCache = cache
            };

            SilentRequest request = new SilentRequest(parameters, (string)null,
                new PlatformParameters(), false);
            User user = request.MapIdentifierToUser(TestConstants.DefaultUniqueId);
            Assert.IsNotNull(user);
            Assert.AreEqual(TestConstants.DefaultUniqueId, user.UniqueId);
        }

        [TestMethod]
        [TestCategory("SilentRequestTests")]
        public void MapToIdentifierMultipleMatchingEntriesTest()
        {
            Authority authenticator = Authority.CreateAuthority(TestConstants.DefaultAuthorityHomeTenant, false);
            TokenCache cache = TokenCacheHelper.CreateCacheWithItems();

            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.ScopeForAnotherResource, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultHomeObjectId,
                TestConstants.DefaultPolicy);
            AuthenticationResultEx ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(),
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(3600)));
            ex.Result.User = new User
            {
                DisplayableId = TestConstants.DefaultDisplayableId,
                UniqueId = TestConstants.DefaultUniqueId,
                HomeObjectId = TestConstants.DefaultHomeObjectId
            };
            ex.Result.ScopeSet = TestConstants.DefaultScope;

            ex.Result.FamilyId = "1";
            ex.RefreshToken = "someRT";
            cache.tokenCacheDictionary[key] = ex;


            AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
            {
                Authority = authenticator,
                ClientKey = new ClientKey(TestConstants.DefaultClientId),
                Policy = TestConstants.DefaultPolicy,
                RestrictToSingleUser = TestConstants.DefaultRestrictToSingleUser,
                Scope = new[] { "something" }.CreateSetFromArray(),
                TokenCache = cache
            };

            SilentRequest request = new SilentRequest(parameters, (string) null,
                new PlatformParameters(), false);
            User user = request.MapIdentifierToUser(TestConstants.DefaultUniqueId);
            Assert.IsNotNull(user);
            Assert.AreEqual(TestConstants.DefaultUniqueId, user.UniqueId);
        }

        [TestMethod]
        [TestCategory("SilentRequestTests")]
        public void ExpiredTokenRefreshFlowTest()
        {
            Authority authenticator = Authority.CreateAuthority(TestConstants.DefaultAuthorityHomeTenant, false);
            TokenCache cache = TokenCacheHelper.CreateCacheWithItems();

            AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
            {
                Authority = authenticator,
                ClientKey = new ClientKey(TestConstants.DefaultClientId),
                Policy = TestConstants.DefaultPolicy,
                RestrictToSingleUser = TestConstants.DefaultRestrictToSingleUser,
                Scope = new[] { "some-scope1", "some-scope2" }.CreateSetFromArray(),
                TokenCache = cache
            };
            
            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.DefaultAuthorityHomeTenant)
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            SilentRequest request = new SilentRequest(parameters, (string)null,
                new PlatformParameters(), false);
            Task<AuthenticationResult> task = request.RunAsync();
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual("some-access-token", result.Token);
            Assert.AreEqual("some-scope1 some-scope2", result.Scope.AsSingleString());

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }


        [TestMethod]
        [TestCategory("SilentRequestTests")]
        public void SilentRefreshFailedNoCacheItemFoundTest()
        {
            Authority authenticator = Authority.CreateAuthority(TestConstants.DefaultAuthorityHomeTenant, false);
            TokenCache cache = new TokenCache();

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.DefaultAuthorityHomeTenant)
            });

            AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
            {
                Authority = authenticator,
                ClientKey = new ClientKey(TestConstants.DefaultClientId),
                Policy = TestConstants.DefaultPolicy,
                RestrictToSingleUser = TestConstants.DefaultRestrictToSingleUser,
                Scope = new[] { "some-scope1", "some-scope2" }.CreateSetFromArray(),
                TokenCache = cache
            };
            
            try
            {
                SilentRequest request = new SilentRequest(parameters, (string) null,
                    new PlatformParameters(), false);
                Task<AuthenticationResult> task = request.RunAsync();
                var authenticationResult = task.Result;
                Assert.Fail("MsalSilentTokenAcquisitionException should be thrown here");
            }
            catch (AggregateException ae)
            {
                Assert.IsTrue(ae.InnerException is MsalSilentTokenAcquisitionException);
            }
        }
    }
}
