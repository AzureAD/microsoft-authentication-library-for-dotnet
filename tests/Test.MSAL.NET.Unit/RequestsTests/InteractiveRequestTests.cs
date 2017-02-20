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
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Cache;
using Microsoft.Identity.Client.Internal.Http;
using Microsoft.Identity.Client.Internal.Instance;
using Microsoft.Identity.Client.Internal.Interfaces;
using Microsoft.Identity.Client.Internal.OAuth2;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Test.MSAL.NET.Unit.Mocks;

namespace Test.MSAL.NET.Unit.RequestsTests
{
    [TestClass]
    public class InteractiveRequestTests
    {
        private TokenCachePlugin _tokenCachePlugin;

        [TestInitialize]
        public void TestInitialize()
        {
            Authority.ValidatedAuthorities.Clear();
            _tokenCachePlugin = (TokenCachePlugin)PlatformPlugin.TokenCachePlugin;
            HttpClientFactory.ReturnHttpClientForMocks = true;
            HttpMessageHandlerFactory.ClearMockHandlers();
        }

        [TestMethod]
        [TestCategory("InteractiveRequestTests")]
        public void NoCacheLookup()
        {
            Authority authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false);
            TokenCache cache = new TokenCache(TestConstants.ClientId);
            TokenCacheKey atKey = new TokenCacheKey(TestConstants.AuthorityHomeTenant,
                TestConstants.Scope, TestConstants.ClientId,
                TestConstants.UniqueId, TestConstants.DisplayableId, TestConstants.HomeObjectId);

            AccessTokenCacheItem atItem = new AccessTokenCacheItem()
            {
                TokenType = "Bearer",
                AccessToken = atKey.ToString(),
                ExpiresOnUnixTimestamp = MsalHelpers.DateTimeToUnixTimestamp(DateTime.UtcNow + TimeSpan.FromSeconds(3599)),
                Scope = TestConstants.Scope
            };
            _tokenCachePlugin.TokenCacheDictionary[atKey.ToString()] = JsonHelper.SerializeToJson(atItem);

            MockWebUI ui = new MockWebUI()
            {
                MockResult = new AuthorizationResult(AuthorizationStatus.Success,
                    TestConstants.AuthorityHomeTenant + "?code=some-code")
            };

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
            });

            MockHttpMessageHandler mockHandler = new MockHttpMessageHandler();
            mockHandler.Method = HttpMethod.Post;

            mockHandler.ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage();
            HttpMessageHandlerFactory.AddMockHandler(mockHandler);

            AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
            {
                Authority = authority,
                ClientKey = new ClientKey(TestConstants.ClientId),
                Scope = TestConstants.Scope,
                TokenCache = cache
            };

            parameters.RedirectUri = new Uri("some://uri");
            parameters.ExtraQueryParameters = "extra=qp";

            InteractiveRequest request = new InteractiveRequest(parameters,
                TestConstants.ScopeForAnotherResource.ToArray(),
                 TestConstants.DisplayableId,
                UiOptions.SelectAccount, ui);
            Task<AuthenticationResult> task = request.RunAsync();
            task.Wait();
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(3, _tokenCachePlugin.TokenCacheDictionary.Count);
            Assert.AreEqual(1, cache.RefreshTokenCount);
            Assert.AreEqual(2, cache.TokenCount);
            Assert.AreEqual(result.Token, "some-access-token");

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }
        
        [TestMethod]
        [TestCategory("InteractiveRequestTests")]
        public void RedirectUriContainsFragmentErrorTest()
        {
            Authority authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false);
            try
            {
                AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
                {
                    Authority = authority,
                    ClientKey = new ClientKey(TestConstants.ClientId),
                    Scope = TestConstants.Scope,
                    TokenCache = null
                };

                parameters.RedirectUri = new Uri("some://uri#fragment=not-so-good");
                parameters.ExtraQueryParameters = "extra=qp";

                new InteractiveRequest(parameters, TestConstants.ScopeForAnotherResource.ToArray(),
                    (string) null, UiOptions.ForceLogin, new MockWebUI()
                    );
                Assert.Fail("ArgumentException should be thrown here");
            }
            catch (ArgumentException ae)
            {
                Assert.IsTrue(ae.Message.Contains(MsalErrorMessage.RedirectUriContainsFragment));
            }
        }

        [TestMethod]
        [TestCategory("InteractiveRequestTests")]
        public void VerifyAuthorizationResultTest()
        {
            Authority authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false);

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
            });

            MockWebUI webUi = new MockWebUI();
            webUi.MockResult = new AuthorizationResult(AuthorizationStatus.ErrorHttp,
                TestConstants.AuthorityHomeTenant + "?error=" + OAuth2Error.LoginRequired);

            AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
            {
                Authority = authority,
                ClientKey = new ClientKey(TestConstants.ClientId),
                Scope = TestConstants.Scope,
                TokenCache = null
            };

            parameters.RedirectUri = new Uri("some://uri");
            parameters.ExtraQueryParameters = "extra=qp";

            InteractiveRequest request = new InteractiveRequest(parameters,
                TestConstants.ScopeForAnotherResource.ToArray(), 
                (string) null, UiOptions.ForceLogin, webUi);
            request.PreRunAsync().Wait();
            try
            {
                request.PreTokenRequest().Wait();
                Assert.Fail("MsalException should have been thrown here");
            }
            catch (Exception exc)
            {
                Assert.IsTrue(exc.InnerException is MsalException);
                Assert.AreEqual(MsalError.UserInteractionRequired, ((MsalException) exc.InnerException).ErrorCode);
            }


            webUi = new MockWebUI();
            webUi.MockResult = new AuthorizationResult(AuthorizationStatus.ErrorHttp,
                TestConstants.AuthorityHomeTenant +
                "?error=invalid_request&error_description=some error description");

            request = new InteractiveRequest(parameters,
                TestConstants.ScopeForAnotherResource.ToArray(),
                (string) null, UiOptions.ForceLogin, webUi);
            request.PreRunAsync().Wait();

            try
            {
                request.PreTokenRequest().Wait();
                Assert.Fail("MsalException should have been thrown here");
            }
            catch (Exception exc)
            {
                Assert.IsTrue(exc.InnerException is MsalException);
                Assert.AreEqual("invalid_request", ((MsalException) exc.InnerException).ErrorCode);
                Assert.AreEqual("some error description", ((MsalException) exc.InnerException).Message);
            }

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("InteractiveRequestTests")]
        public void NullLoginHintForActAsCurrentUserTest()
        {
            Authority authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false);
            try
            {
                AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
                {
                    Authority = authority,
                    ClientKey = new ClientKey(TestConstants.ClientId),
                    Scope = TestConstants.Scope,
                    TokenCache = null
                };

                parameters.RedirectUri = new Uri("some://uri");
                parameters.ExtraQueryParameters = "extra=qp";

                InteractiveRequest request = new InteractiveRequest(parameters,
                    TestConstants.ScopeForAnotherResource.ToArray(),
                    (string) null, UiOptions.ActAsCurrentUser, new MockWebUI());
                Assert.Fail("ArgumentException should be thrown here");
            }
            catch (ArgumentException ae)
            {
                Assert.IsTrue(ae.Message.Contains(MsalErrorMessage.LoginHintNullForUiOption));
            }
        }

        [TestMethod]
        [TestCategory("InteractiveRequestTests")]
        public void NullUserForActAsCurrentUserTest()
        {
            Authority authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false);
            try
            {
                AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
                {
                    Authority = authority,
                    ClientKey = new ClientKey(TestConstants.ClientId),
                    Scope = TestConstants.Scope,
                    TokenCache = null
                };

                parameters.RedirectUri = new Uri("some://uri");
                parameters.ExtraQueryParameters = "extra=qp";

                new InteractiveRequest(parameters,
                    TestConstants.ScopeForAnotherResource.ToArray(),
                    null, UiOptions.ActAsCurrentUser, new MockWebUI());
                Assert.Fail("ArgumentException should be thrown here");
            }
            catch (ArgumentException ae)
            {
                Assert.IsTrue(ae.Message.Contains(MsalErrorMessage.LoginHintNullForUiOption));
            }
        }

        [TestMethod]
        [TestCategory("InteractiveRequestTests")]
        public void DuplicateQueryParameterErrorTest()
        {
            Authority authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false);

            AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
            {
                Authority = authority,
                ClientKey = new ClientKey(TestConstants.ClientId),
                Scope = TestConstants.Scope,
                TokenCache = null
            };

            parameters.RedirectUri = new Uri("some://uri");
            parameters.ExtraQueryParameters = "extra=qp&prompt=login";

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
            });

            InteractiveRequest request = new InteractiveRequest(parameters,
                TestConstants.ScopeForAnotherResource.ToArray(),
                null, UiOptions.ForceLogin, new MockWebUI());
            request.PreRunAsync().Wait();

            try
            {
                request.PreTokenRequest().Wait();
                Assert.Fail("MsalException should be thrown here");
            }
            catch (Exception exc)
            {
                Assert.IsTrue(exc.InnerException is MsalException);
                Assert.AreEqual(MsalError.DuplicateQueryParameter, ((MsalException) exc.InnerException).ErrorCode);
            }

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }
    }
}
