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
using Microsoft.Identity.Client.Internal.Cache;
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
        private TokenCachePlugin _tokenCachePlugin;

        [TestInitialize]
        public void TestInitialize()
        {
            _tokenCachePlugin = (TokenCachePlugin)PlatformPlugin.TokenCachePlugin;
            Authority.ValidatedAuthorities.Clear();
            HttpClientFactory.ReturnHttpClientForMocks = true;
            HttpMessageHandlerFactory.ClearMockHandlers();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _tokenCachePlugin.TokenCacheDictionary.Clear();
        }

        [TestMethod]
        [TestCategory("SilentRequestTests")]
        public void ConstructorTests()
        {
            Authority authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false);
            TokenCache cache = new TokenCache(TestConstants.ClientId);
            AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
            {
                Authority = authority,
                ClientKey = new ClientKey(TestConstants.ClientId),
                Scope = TestConstants.Scope,
                TokenCache = cache,
                RequestContext = new RequestContext(Guid.Empty)
            };

            parameters.User = null;
            try
            {
                new SilentRequest(parameters, false);
                Assert.Fail("ArgumentNullException should have been thrown here");
            }
            catch (ArgumentNullException exc)
            {
                Assert.AreEqual(exc.ParamName, "User");
            }

            parameters.User = new User()
            {
                DisplayableId = TestConstants.DisplayableId
            };
            SilentRequest request = new SilentRequest(parameters, false);
            Assert.IsNotNull(request);

            parameters.User = new User()
            {
                UniqueId = TestConstants.UniqueId
            };
            request = new SilentRequest(parameters, false);
            Assert.IsNotNull(request);

            request = new SilentRequest(parameters, false);
            Assert.IsNotNull(request);
        }

        [TestMethod]
        [TestCategory("SilentRequestTests")]
        public void ExpiredTokenRefreshFlowTest()
        {
            Authority authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false);
            TokenCache cache = new TokenCache(TestConstants.ClientId);
            TokenCacheHelper.PopulateCache(_tokenCachePlugin);

            AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
            {
                Authority = authority,
                ClientKey = new ClientKey(TestConstants.ClientId),
                Scope = new[] { "some-scope1", "some-scope2" }.CreateSetFromArray(),
                TokenCache = cache,
                RequestContext = new RequestContext(Guid.Empty),
                User = new User()
                {
                    HomeObjectId = TestConstants.HomeObjectId,
                    UniqueId = TestConstants.UniqueId,
                    DisplayableId = TestConstants.DisplayableId
                }
            };
            
            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
            });

            SilentRequest request = new SilentRequest(parameters, false);
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
            Authority authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false);
            TokenCache cache = new TokenCache(TestConstants.ClientId);

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
            });

            AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
            {
                Authority = authority,
                ClientKey = new ClientKey(TestConstants.ClientId),
                Scope = new[] { "some-scope1", "some-scope2" }.CreateSetFromArray(),
                TokenCache = cache,
                User = new User() { UniqueId = ""},
                RequestContext = new RequestContext(Guid.Empty)
            };
            
            try
            {
                SilentRequest request = new SilentRequest(parameters, false);
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
