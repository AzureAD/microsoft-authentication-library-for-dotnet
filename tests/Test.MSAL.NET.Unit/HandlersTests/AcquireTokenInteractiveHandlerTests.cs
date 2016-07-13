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
using Microsoft.Identity.Client.Requests;
using Microsoft.Identity.Client.Interfaces;
using Microsoft.Identity.Client.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Test.MSAL.Common.Unit;
using Test.MSAL.NET.Unit.Mocks;

namespace Test.MSAL.NET.Unit.RequestsTests
{
    [TestClass]
    public class AcquireTokenInteractiveHandlerTests
    {
        [TestMethod]
        [TestCategory("AcquireTokenInteractiveHandlerTests")]
        public void NoCacheLookup()
        {
            Authenticator authenticator = new Authenticator(TestConstants.DefaultAuthorityHomeTenant, false, Guid.NewGuid());
            TokenCache cache = new TokenCache();
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultHomeObjectId,
                TestConstants.DefaultPolicy);
            AuthenticationResultEx ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(),
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(3599)));
            ex.Result.User = new User
            {
                DisplayableId = TestConstants.DefaultDisplayableId,
                UniqueId = TestConstants.DefaultUniqueId,
                HomeObjectId = TestConstants.DefaultHomeObjectId
            };
            ex.Result.FamilyId = "1";
            ex.RefreshToken = "someRT";
            cache.tokenCacheDictionary[key] = ex;

            IWebUI ui = Substitute.For<IWebUI>();
            AuthorizationResult ar = new AuthorizationResult(AuthorizationStatus.Success,
                TestConstants.DefaultAuthorityHomeTenant + "?code=some-code");
            ui.AcquireAuthorizationAsync(Arg.Any<Uri>(), Arg.Any<Uri>(), Arg.Any<IDictionary<string, string>>(),
                Arg.Any<CallState>())
                .Returns(ar);

            MockHttpMessageHandler mockHandler = new MockHttpMessageHandler();
            mockHandler.Method = HttpMethod.Post;
            mockHandler.QueryParams = new Dictionary<string, string>() {{"p", "some-policy"}};

            mockHandler.ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage();
            HttpMessageHandlerFactory.MockHandler = mockHandler;

            AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
            {
                Authenticator = authenticator,
                ClientKey = new ClientKey(TestConstants.DefaultClientId),
                Policy = "some-policy",
                RestrictToSingleUser = TestConstants.DefaultRestrictToSingleUser,
                Scope = TestConstants.DefaultScope.ToArray(),
                TokenCache = cache
            };

            InteractiveRequest request = new InteractiveRequest(parameters,
                TestConstants.ScopeForAnotherResource.ToArray(),
                new Uri("some://uri"), new PlatformParameters(), TestConstants.DefaultDisplayableId,
                UiOptions.SelectAccount, "extra=qp", ui);
            Task<AuthenticationResult> task = request.RunAsync();
            task.Wait();
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(2, cache.Count);
            Assert.AreEqual(result.Token, "some-access-token");

            //both cache entry authorities are TestConstants.DefaultAuthorityHomeTenant
            foreach (var item in cache.ReadItems(TestConstants.DefaultClientId))
            {
                Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, item.Authority);
            }
        }

        //TODO commented code should be uncommented as per https://github.com/AzureAD/MSAL-Prototype/issues/66
        /*        [TestMethod]
                [TestCategory("AcquireTokenInteractiveHandlerTests")]
                public void SsoRrefreshTokenInHeaderTest()
                {
                    Authenticator authenticator = new Authenticator(TestConstants.DefaultAuthorityHomeTenant, false, Guid.NewGuid());
                    TokenCache cache = new TokenCache();
                    TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                        TestConstants.DefaultScope, TestConstants.DefaultClientId,
                        TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultHomeObjectId,
                        TestConstants.DefaultPolicy);
                    AuthenticationResultEx ex = new AuthenticationResultEx();
                    ex.Result = new AuthenticationResult("Bearer", key.ToString(),
                        new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(3599)));
                    ex.Result.User = new User
                    {
                        DisplayableId = TestConstants.DefaultDisplayableId,
                        UniqueId = TestConstants.DefaultUniqueId,
                        HomeObjectId = TestConstants.DefaultHomeObjectId
                    };
                    ex.Result.FamilyId = "1";
                    ex.RefreshToken = "someRT";
                    cache.tokenCacheDictionary[key] = ex;

                    MockWebUI webUi = new MockWebUI();
                    webUi.HeadersToValidate = new Dictionary<string, string>();
                    webUi.HeadersToValidate["x-ms-sso-RefreshToken"] = "someRT";
                    webUi.MockResult = new AuthorizationResult(AuthorizationStatus.Success,
                        TestConstants.DefaultAuthorityHomeTenant + "?code=some-code");

                    AuthenticationRequestParameters data = new AuthenticationRequestParameters()
                    {
                        Authenticator = authenticator,
                        ClientKey = new ClientKey(TestConstants.DefaultClientId),
                        Policy = TestConstants.DefaultPolicy,
                        RestrictToSingleUser = TestConstants.DefaultRestrictToSingleUser,
                        Scope = TestConstants.DefaultScope.ToArray(),
                        TokenCache = cache
                    };

                    InteractiveRequest handler = new InteractiveRequest(data,
                        TestConstants.ScopeForAnotherResource.ToArray(),
                        new Uri("some://uri"), new PlatformParameters(),
                        ex.Result.User, UiOptions.ActAsCurrentUser, "extra=qp", webUi);
                    handler.PreRunAsync().Wait();
                    handler.PreTokenRequest().Wait();
                }*/

        [TestMethod]
        [TestCategory("AcquireTokenInteractiveHandlerTests")]
        public void ActAsCurrentUserNoSsoHeaderForLoginHintOnlyTest()
        {
            //this test validates that no SSO header is added when developer passes only login hint and UiOption.ActAsCurrentUser
            Authenticator authenticator = new Authenticator(TestConstants.DefaultAuthorityHomeTenant, false, Guid.NewGuid());
            TokenCache cache = new TokenCache();
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultHomeObjectId,
                TestConstants.DefaultPolicy);
            AuthenticationResultEx ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(),
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(3599)));
            ex.Result.User = new User
            {
                DisplayableId = TestConstants.DefaultDisplayableId,
                UniqueId = TestConstants.DefaultUniqueId,
                HomeObjectId = TestConstants.DefaultHomeObjectId
            };
            ex.Result.FamilyId = "1";
            ex.RefreshToken = "someRT";
            cache.tokenCacheDictionary[key] = ex;

            MockWebUI webUi = new MockWebUI();
            webUi.MockResult = new AuthorizationResult(AuthorizationStatus.Success,
                TestConstants.DefaultAuthorityHomeTenant + "?code=some-code");

            AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
            {
                Authenticator = authenticator,
                ClientKey = new ClientKey(TestConstants.DefaultClientId),
                Policy = TestConstants.DefaultPolicy,
                RestrictToSingleUser = TestConstants.DefaultRestrictToSingleUser,
                Scope = TestConstants.DefaultScope.ToArray(),
                TokenCache = cache
            };

            InteractiveRequest request = new InteractiveRequest(parameters,
                TestConstants.ScopeForAnotherResource.ToArray(),
                new Uri("some://uri"), new PlatformParameters(),
                ex.Result.User, UiOptions.ActAsCurrentUser, "extra=qp", webUi);
            request.PreRunAsync().Wait();
            request.PreTokenRequest().Wait();
        }


        [TestMethod]
        [TestCategory("AcquireTokenInteractiveHandlerTests")]
        public void RedirectUriContainsFragmentErrorTest()
        {
            Authenticator authenticator = new Authenticator(TestConstants.DefaultAuthorityHomeTenant, false, Guid.NewGuid());
            try
            {

                AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
                {
                    Authenticator = authenticator,
                    ClientKey = new ClientKey(TestConstants.DefaultClientId),
                    Policy = TestConstants.DefaultPolicy,
                    RestrictToSingleUser = TestConstants.DefaultRestrictToSingleUser,
                    Scope = TestConstants.DefaultScope.ToArray(),
                    TokenCache = null
                };

                InteractiveRequest request = new InteractiveRequest(parameters, TestConstants.ScopeForAnotherResource.ToArray(),
                    new Uri("some://uri#fragment=not-so-good"), new PlatformParameters(),
                    (string) null, UiOptions.ForceLogin, "extra=qp", new MockWebUI()
                    );
                Assert.Fail("ArgumentException should be thrown here");
            }
            catch (ArgumentException ae)
            {
                Assert.IsTrue(ae.Message.Contains(MsalErrorMessage.RedirectUriContainsFragment));
            }
        }

        [TestMethod]
        [TestCategory("AcquireTokenInteractiveHandlerTests")]
        public void CacheWithMultipleUsersAndRestrictToSingleUserTrueTest()
        {
            Authenticator authenticator = new Authenticator(TestConstants.DefaultAuthorityHomeTenant, false, Guid.NewGuid());
            TokenCache cache = TokenCacheHelper.CreateCacheWithItems();

            try
            {

                AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
                {
                    Authenticator = authenticator,
                    ClientKey = new ClientKey(TestConstants.DefaultClientId),
                    Policy = TestConstants.DefaultPolicy,
                    RestrictToSingleUser = true,
                    Scope = TestConstants.DefaultScope.ToArray(),
                    TokenCache = cache
                };

                InteractiveRequest request = new InteractiveRequest(parameters,
                    TestConstants.ScopeForAnotherResource.ToArray(),
                    new Uri("some://uri"), new PlatformParameters(),
                    new User {UniqueId = TestConstants.DefaultUniqueId}, UiOptions.ForceLogin, "extra=qp",
                    new MockWebUI());
                Assert.Fail("ArgumentException should be thrown here");
            }
            catch (ArgumentException ae)
            {
                Assert.AreEqual(
                    "Cache cannot have entries for more than 1 unique id when RestrictToSingleUser is set to TRUE.",
                    ae.Message);
            }
        }

        [TestMethod]
        [TestCategory("AcquireTokenInteractiveHandlerTests")]
        public void VerifyAuthorizationResultTest()
        {
            Authenticator authenticator = new Authenticator(TestConstants.DefaultAuthorityHomeTenant, false, Guid.NewGuid());

            MockWebUI webUi = new MockWebUI();
            webUi.MockResult = new AuthorizationResult(AuthorizationStatus.ErrorHttp,
                TestConstants.DefaultAuthorityHomeTenant + "?error="+OAuthError.LoginRequired);

            AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
            {
                Authenticator = authenticator,
                ClientKey = new ClientKey(TestConstants.DefaultClientId),
                Policy = TestConstants.DefaultPolicy,
                RestrictToSingleUser = TestConstants.DefaultRestrictToSingleUser,
                Scope = TestConstants.DefaultScope.ToArray(),
                TokenCache = null
            };

            InteractiveRequest request = new InteractiveRequest(parameters,
                TestConstants.ScopeForAnotherResource.ToArray(), new Uri("some://uri"), new PlatformParameters(),
                (string) null, UiOptions.ForceLogin, "extra=qp", webUi);
            request.PreRunAsync().Wait();
            try
            {
                request.PreTokenRequest().Wait();
                Assert.Fail("MsalException should have been thrown here");
            }
            catch (Exception exc)
            {
                Assert.IsTrue(exc.InnerException is MsalException);
                Assert.AreEqual(MsalError.UserInteractionRequired, ((MsalException)exc.InnerException).ErrorCode);
            }


            webUi = new MockWebUI();
            webUi.MockResult = new AuthorizationResult(AuthorizationStatus.ErrorHttp,
                TestConstants.DefaultAuthorityHomeTenant + "?error=invalid_request&error_description=some error description");

            request = new InteractiveRequest(parameters,
                TestConstants.ScopeForAnotherResource.ToArray(), new Uri("some://uri"), new PlatformParameters(),
                (string)null, UiOptions.ForceLogin, "extra=qp", webUi);
            request.PreRunAsync().Wait();

            try
            {
                request.PreTokenRequest().Wait();
                Assert.Fail("MsalException should have been thrown here");
            }
            catch (Exception exc)
            {
                Assert.IsTrue(exc.InnerException is MsalException);
                Assert.AreEqual("invalid_request", ((MsalException)exc.InnerException).ErrorCode);
                Assert.AreEqual("some error description", ((MsalException)exc.InnerException).Message);
            }
        }

        [TestMethod]
        [TestCategory("AcquireTokenInteractiveHandlerTests")]
        public void NullLoginHintForActAsCurrentUserTest()
        {
            Authenticator authenticator = new Authenticator(TestConstants.DefaultAuthorityHomeTenant, false, Guid.NewGuid());
            try
            {

                AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
                {
                    Authenticator = authenticator,
                    ClientKey = new ClientKey(TestConstants.DefaultClientId),
                    Policy = TestConstants.DefaultPolicy,
                    RestrictToSingleUser = TestConstants.DefaultRestrictToSingleUser,
                    Scope = TestConstants.DefaultScope.ToArray(),
                    TokenCache = null
                };

                InteractiveRequest request = new InteractiveRequest(parameters,
                    TestConstants.ScopeForAnotherResource.ToArray(),
                    new Uri("some://uri"), new PlatformParameters(),
                    (string) null, UiOptions.ActAsCurrentUser, "extra=qp", new MockWebUI());
                Assert.Fail("ArgumentException should be thrown here");
            }
            catch (ArgumentException ae)
            {
                Assert.IsTrue(ae.Message.Contains(MsalErrorMessage.LoginHintNullForUiOption));
            }
        }

        [TestMethod]
        [TestCategory("AcquireTokenInteractiveHandlerTests")]
        public void NullUserForActAsCurrentUserTest()
        {
            Authenticator authenticator = new Authenticator(TestConstants.DefaultAuthorityHomeTenant, false, Guid.NewGuid());
            try
            {
                AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
                {
                    Authenticator = authenticator,
                    ClientKey = new ClientKey(TestConstants.DefaultClientId),
                    Policy = TestConstants.DefaultPolicy,
                    RestrictToSingleUser = TestConstants.DefaultRestrictToSingleUser,
                    Scope = TestConstants.DefaultScope.ToArray(),
                    TokenCache = null
                };

                InteractiveRequest request = new InteractiveRequest(parameters,
                    TestConstants.ScopeForAnotherResource.ToArray(), new Uri("some://uri"), new PlatformParameters(),
                    (User) null, UiOptions.ActAsCurrentUser, "extra=qp", new MockWebUI());
                Assert.Fail("ArgumentException should be thrown here");
            }
            catch (ArgumentException ae)
            {
                Assert.IsTrue(ae.Message.Contains(MsalErrorMessage.LoginHintNullForUiOption));
            }
        }

        [TestMethod]
        [TestCategory("AcquireTokenInteractiveHandlerTests")]
        public void DuplicateQueryParameterErrorTest()
        {
            Authenticator authenticator = new Authenticator(TestConstants.DefaultAuthorityHomeTenant, false, Guid.NewGuid());

            AuthenticationRequestParameters parameters = new AuthenticationRequestParameters()
            {
                Authenticator = authenticator,
                ClientKey = new ClientKey(TestConstants.DefaultClientId),
                Policy = TestConstants.DefaultPolicy,
                RestrictToSingleUser = TestConstants.DefaultRestrictToSingleUser,
                Scope = TestConstants.DefaultScope.ToArray(),
                TokenCache = null
            };

            InteractiveRequest request = new InteractiveRequest(parameters,
                TestConstants.ScopeForAnotherResource.ToArray(),
                new Uri("some://uri"), new PlatformParameters(),
                (User) null, UiOptions.ForceLogin, "extra=qp&prompt=login", new MockWebUI());
            request.PreRunAsync().Wait();

            try
            {
                    request.PreTokenRequest().Wait();
                    Assert.Fail("MsalException should be thrown here");
            }
            catch (Exception exc)
            {
                Assert.IsTrue(exc.InnerException is MsalException);
                Assert.AreEqual(MsalError.DuplicateQueryParameter, ((MsalException)exc.InnerException).ErrorCode);
            }
        }
    }
}
