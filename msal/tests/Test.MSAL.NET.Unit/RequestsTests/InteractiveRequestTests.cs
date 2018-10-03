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
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.MSAL.NET.Unit.Mocks;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Telemetry;
using Microsoft.Identity.Core.Cache;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Http;
using Microsoft.Identity.Core.Instance;
using Microsoft.Identity.Core.OAuth2;
using Test.Microsoft.Identity.Core.Unit;
using Test.Microsoft.Identity.Core.Unit.Mocks;
using Microsoft.Identity.Core.UI;
using System.Threading;

namespace Test.MSAL.NET.Unit.RequestsTests
{
    [TestClass]
    public class InteractiveRequestTests
    {
        TokenCache cache;
        private readonly MyReceiver _myReceiver = new MyReceiver();

        [TestInitialize]
        public void TestInitialize()
        {
            RequestTestsCommon.InitializeRequestTests();
            Telemetry.GetInstance().RegisterReceiver(_myReceiver.OnEvents);

            cache = new TokenCache();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            cache.tokenCacheAccessor.AccessTokenCacheDictionary.Clear();
            cache.tokenCacheAccessor.RefreshTokenCacheDictionary.Clear();
        }

        [TestMethod]
        [TestCategory("InteractiveRequestTests")]
        public void SliceParametersTest()
        {
            Authority authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false);
            cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            MockWebUI ui = new MockWebUI()
            {
                MockResult = new AuthorizationResult(AuthorizationStatus.Success,
                    TestConstants.AuthorityHomeTenant + "?code=some-code"),
                QueryParamsToValidate = new Dictionary<string, string>()
                {
                    {"key1", "value1%20with%20encoded%20space"},
                    {"key2", "value2"}
                }
            };

            RequestTestsCommon.MockInstanceDiscoveryAndOpenIdRequest();

            MockHttpMessageHandler mockHandler = new MockHttpMessageHandler();
            mockHandler.Method = HttpMethod.Post;
            mockHandler.QueryParams = new Dictionary<string, string>()
            {
                {"key1", "value1%20with%20encoded%20space"},
                {"key2", "value2"}
            };
            mockHandler.ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage();
            HttpMessageHandlerFactory.AddMockHandler(mockHandler);

            AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
            {
                Authority = authority,
                SliceParameters = "key1=value1%20with%20encoded%20space&key2=value2",
                ClientId = TestConstants.ClientId,
                Scope = TestConstants.Scope,
                TokenCache = cache,
                RequestContext = new RequestContext(new MsalLogger(Guid.NewGuid(), null))
            };

            parameters.RedirectUri = new Uri("some://uri");
            parameters.ExtraQueryParameters = "extra=qp";

            InteractiveRequest request = new InteractiveRequest(parameters,
                TestConstants.ScopeForAnotherResource.ToArray(),
                TestConstants.DisplayableId,
                UIBehavior.SelectAccount, ui);
            Task<AuthenticationResult> task = request.RunAsync(CancellationToken.None);
            task.Wait();
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(1, cache.tokenCacheAccessor.RefreshTokenCacheDictionary.Count);
            Assert.AreEqual(1, cache.tokenCacheAccessor.AccessTokenCacheDictionary.Count);
            Assert.AreEqual(result.AccessToken, "some-access-token");

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }


        [TestMethod]
        [TestCategory("InteractiveRequestTests")]
        public void NoCacheLookup()
        {
            Authority authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false);
            cache = new TokenCache()
            {
                ClientId = TestConstants.ClientId
            };

            var atItem = new MsalAccessTokenCacheItem(
                TestConstants.ProductionPrefNetworkEnvironment,
                TestConstants.ClientId,
                "Bearer",
                TestConstants.Scope.AsSingleString(),
                TestConstants.Utid,
                null,
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(3599)),
                MockHelpers.CreateClientInfo());

            string atKey = atItem.GetKey().ToString();
            atItem.Secret = atKey;
            cache.tokenCacheAccessor.AccessTokenCacheDictionary[atKey] = JsonHelper.SerializeToJson(atItem);

            MockWebUI ui = new MockWebUI()
            {
                MockResult = new AuthorizationResult(AuthorizationStatus.Success,
                    TestConstants.AuthorityHomeTenant + "?code=some-code")
            };

            RequestTestsCommon.MockInstanceDiscoveryAndOpenIdRequest();

            MockHttpMessageHandler mockHandler = new MockHttpMessageHandler();
            mockHandler.Method = HttpMethod.Post;

            mockHandler.ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage();
            HttpMessageHandlerFactory.AddMockHandler(mockHandler);

            AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
            {
                Authority = authority,
                ClientId = TestConstants.ClientId,
                Scope = TestConstants.Scope,
                TokenCache = cache,
                RequestContext = new RequestContext(new MsalLogger(Guid.NewGuid(), null))
            };

            parameters.RedirectUri = new Uri("some://uri");
            parameters.ExtraQueryParameters = "extra=qp";

            InteractiveRequest request = new InteractiveRequest(parameters,
                TestConstants.ScopeForAnotherResource.ToArray(),
                 TestConstants.DisplayableId,
                UIBehavior.SelectAccount, ui);
            Task<AuthenticationResult> task = request.RunAsync(CancellationToken.None);
            task.Wait();
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(1, cache.tokenCacheAccessor.RefreshTokenCacheDictionary.Count);
            Assert.AreEqual(2, cache.tokenCacheAccessor.AccessTokenCacheDictionary.Count);
            Assert.AreEqual(result.AccessToken, "some-access-token");

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");

            Assert.IsNotNull(_myReceiver.EventsReceived.Find(anEvent =>  // Expect finding such an event
                anEvent[EventBase.EventNameKey].EndsWith("ui_event") && anEvent[UiEvent.UserCancelledKey] == "false"));
            Assert.IsNotNull(_myReceiver.EventsReceived.Find(anEvent =>  // Expect finding such an event
                anEvent[EventBase.EventNameKey].EndsWith("api_event") && anEvent[ApiEvent.UiBehaviorKey] == "select_account"));
            Assert.IsNotNull(_myReceiver.EventsReceived.Find(anEvent =>  // Expect finding such an event
                anEvent[EventBase.EventNameKey].EndsWith("ui_event") && anEvent[UiEvent.AccessDeniedKey] == "false"));
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
                    ClientId = TestConstants.ClientId,
                    Scope = TestConstants.Scope,
                    TokenCache = null,
                    RequestContext = new RequestContext(new MsalLogger(Guid.NewGuid(), null))
                };

                parameters.RedirectUri = new Uri("some://uri#fragment=not-so-good");
                parameters.ExtraQueryParameters = "extra=qp";

                new InteractiveRequest(parameters, TestConstants.ScopeForAnotherResource.ToArray(),
                    (string)null, UIBehavior.ForceLogin, new MockWebUI()
                    );
                Assert.Fail("ArgumentException should be thrown here");
            }
            catch (ArgumentException ae)
            {
                Assert.IsTrue(ae.Message.Contains(MsalErrorMessage.RedirectUriContainsFragment));
            }
        }

        [TestMethod]
        public void Foo()
        {
            Authority authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false);

            HttpMessageHandlerFactory.AddMockHandler(
               MockHelpers.CreateInstanceDiscoveryMockHandler(
               TestConstants.GetDiscoveryEndpoint(TestConstants.AuthorityCommonTenant)));

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
            });
        }

        [TestMethod]
        [TestCategory("InteractiveRequestTests")]
        public void VerifyAuthorizationResultTest()
        {
            Authority authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false);

            RequestTestsCommon.MockInstanceDiscoveryAndOpenIdRequest();

            MockWebUI webUi = new MockWebUI()
            {
                MockResult = new AuthorizationResult(AuthorizationStatus.ErrorHttp,
                TestConstants.AuthorityHomeTenant + "?error=" + OAuth2Error.LoginRequired)
            };

            AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
            {
                Authority = authority,
                ClientId = TestConstants.ClientId,
                Scope = TestConstants.Scope,
                TokenCache = null,
                RequestContext = new RequestContext(new MsalLogger(Guid.NewGuid(), null))
            };

            parameters.RedirectUri = new Uri("some://uri");
            parameters.ExtraQueryParameters = "extra=qp";

            InteractiveRequest request = new InteractiveRequest(parameters,
                TestConstants.ScopeForAnotherResource.ToArray(),
                (string)null, UIBehavior.ForceLogin, webUi);
            try
            {
                request.PreTokenRequestAsync(CancellationToken.None).Wait();
                Assert.Fail("MsalException should have been thrown here");
            }
            catch (Exception exc)
            {
                Assert.IsTrue(exc.InnerException is MsalUiRequiredException);
                Assert.AreEqual(MsalUiRequiredException.NoPromptFailedError, ((MsalUiRequiredException)exc.InnerException).ErrorCode);
            }


            webUi = new MockWebUI();
            webUi.MockResult = new AuthorizationResult(AuthorizationStatus.ErrorHttp,
                TestConstants.AuthorityHomeTenant +
                "?error=invalid_request&error_description=some error description");

            request = new InteractiveRequest(parameters,
                TestConstants.ScopeForAnotherResource.ToArray(),
                (string)null, UIBehavior.ForceLogin, webUi);

            try
            {
                request.PreTokenRequestAsync(CancellationToken.None).Wait(CancellationToken.None);
                Assert.Fail("MsalException should have been thrown here");
            }
            catch (Exception exc)
            {
                Assert.IsTrue(exc.InnerException is MsalException);
                Assert.AreEqual("invalid_request", ((MsalException)exc.InnerException).ErrorCode);
                Assert.AreEqual("some error description", ((MsalException)exc.InnerException).Message);
            }

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("InteractiveRequestTests")]
        public void DuplicateQueryParameterErrorTest()
        {
            Authority authority = Authority.CreateAuthority(TestConstants.AuthorityHomeTenant, false);

            AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
            {
                Authority = authority,
                ClientId = TestConstants.ClientId,
                Scope = TestConstants.Scope,
                TokenCache = null,
                RequestContext = new RequestContext(new MsalLogger(Guid.NewGuid(), null))
            };

            parameters.RedirectUri = new Uri("some://uri");
            parameters.ExtraQueryParameters = "extra=qp&prompt=login";

            RequestTestsCommon.MockInstanceDiscoveryAndOpenIdRequest();

            InteractiveRequest request = new InteractiveRequest(parameters,
                TestConstants.ScopeForAnotherResource.ToArray(),
                null, UIBehavior.ForceLogin, new MockWebUI());

            try
            {
                request.PreTokenRequestAsync(CancellationToken.None).Wait();
                Assert.Fail("MsalException should be thrown here");
            }
            catch (Exception exc)
            {
                Assert.IsTrue(exc.InnerException is MsalException);
                Assert.AreEqual(MsalClientException.DuplicateQueryParameterError, ((MsalException)exc.InnerException).ErrorCode);
            }

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }
    }
}