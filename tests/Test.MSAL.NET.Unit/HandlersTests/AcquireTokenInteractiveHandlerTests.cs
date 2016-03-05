using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Handlers;
using Microsoft.Identity.Client.Interfaces;
using Microsoft.Identity.Client.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Test.MSAL.Common.Unit;
using Test.MSAL.NET.Unit.Mocks;

namespace Test.MSAL.NET.Unit.HandlersTests
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
                RootId = TestConstants.DefaultHomeObjectId
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

            HandlerData data = new HandlerData()
            {
                Authenticator = authenticator,
                ClientKey = new ClientKey(TestConstants.DefaultClientId),
                Policy = "some-policy",
                RestrictToSingleUser = TestConstants.DefaultRestrictToSingleUser,
                Scope = TestConstants.DefaultScope.ToArray(),
                TokenCache = cache
            };

            AcquireTokenInteractiveHandler handler = new AcquireTokenInteractiveHandler(data,
                TestConstants.ScopeForAnotherResource.ToArray(),
                new Uri("some://uri"), new PlatformParameters(), TestConstants.DefaultDisplayableId,
                UiOptions.SelectAccount, "extra=qp", ui);
            Task<AuthenticationResult> task = handler.RunAsync();
            task.Wait();
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(2, cache.Count);
            Assert.AreEqual(result.AccessToken, "some-access-token");

            //both cache entry authorities are TestConstants.DefaultAuthorityHomeTenant
            foreach (var item in cache.ReadItems(TestConstants.DefaultClientId))
            {
                Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, item.Authority);
            }
        }

        [TestMethod]
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
                RootId = TestConstants.DefaultHomeObjectId
            };
            ex.Result.FamilyId = "1";
            ex.RefreshToken = "someRT";
            cache.tokenCacheDictionary[key] = ex;

            MockWebUI webUi = new MockWebUI();
            webUi.HeadersToValidate = new Dictionary<string, string>();
            webUi.HeadersToValidate["x-ms-sso-RefreshToken"] = "someRT";
            webUi.MockResult = new AuthorizationResult(AuthorizationStatus.Success,
                TestConstants.DefaultAuthorityHomeTenant + "?code=some-code");

            HandlerData data = new HandlerData()
            {
                Authenticator = authenticator,
                ClientKey = new ClientKey(TestConstants.DefaultClientId),
                Policy = TestConstants.DefaultPolicy,
                RestrictToSingleUser = TestConstants.DefaultRestrictToSingleUser,
                Scope = TestConstants.DefaultScope.ToArray(),
                TokenCache = cache
            };
            
            AcquireTokenInteractiveHandler handler = new AcquireTokenInteractiveHandler(data,
                TestConstants.ScopeForAnotherResource.ToArray(),
                new Uri("some://uri"), new PlatformParameters(),
                ex.Result.User, UiOptions.ActAsCurrentUser, "extra=qp", webUi);
            handler.PreRunAsync().Wait();
            handler.PreTokenRequest().Wait();
        }

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
                RootId = TestConstants.DefaultHomeObjectId
            };
            ex.Result.FamilyId = "1";
            ex.RefreshToken = "someRT";
            cache.tokenCacheDictionary[key] = ex;

            MockWebUI webUi = new MockWebUI();
            webUi.MockResult = new AuthorizationResult(AuthorizationStatus.Success,
                TestConstants.DefaultAuthorityHomeTenant + "?code=some-code");

            HandlerData data = new HandlerData()
            {
                Authenticator = authenticator,
                ClientKey = new ClientKey(TestConstants.DefaultClientId),
                Policy = TestConstants.DefaultPolicy,
                RestrictToSingleUser = TestConstants.DefaultRestrictToSingleUser,
                Scope = TestConstants.DefaultScope.ToArray(),
                TokenCache = cache
            };

            AcquireTokenInteractiveHandler handler = new AcquireTokenInteractiveHandler(data,
                TestConstants.ScopeForAnotherResource.ToArray(),
                new Uri("some://uri"), new PlatformParameters(),
                ex.Result.User, UiOptions.ActAsCurrentUser, "extra=qp", webUi);
            handler.PreRunAsync().Wait();
            handler.PreTokenRequest().Wait();
        }


        [TestMethod]
        [TestCategory("AcquireTokenInteractiveHandlerTests")]
        public void RedirectUriContainsFragmentErrorTest()
        {
            Authenticator authenticator = new Authenticator(TestConstants.DefaultAuthorityHomeTenant, false, Guid.NewGuid());
            try
            {

                HandlerData data = new HandlerData()
                {
                    Authenticator = authenticator,
                    ClientKey = new ClientKey(TestConstants.DefaultClientId),
                    Policy = TestConstants.DefaultPolicy,
                    RestrictToSingleUser = TestConstants.DefaultRestrictToSingleUser,
                    Scope = TestConstants.DefaultScope.ToArray(),
                    TokenCache = null
                };

                AcquireTokenInteractiveHandler handler = new AcquireTokenInteractiveHandler(data, TestConstants.ScopeForAnotherResource.ToArray(),
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

                HandlerData data = new HandlerData()
                {
                    Authenticator = authenticator,
                    ClientKey = new ClientKey(TestConstants.DefaultClientId),
                    Policy = TestConstants.DefaultPolicy,
                    RestrictToSingleUser = true,
                    Scope = TestConstants.DefaultScope.ToArray(),
                    TokenCache = cache
                };

                AcquireTokenInteractiveHandler handler = new AcquireTokenInteractiveHandler(data,
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

            HandlerData data = new HandlerData()
            {
                Authenticator = authenticator,
                ClientKey = new ClientKey(TestConstants.DefaultClientId),
                Policy = TestConstants.DefaultPolicy,
                RestrictToSingleUser = TestConstants.DefaultRestrictToSingleUser,
                Scope = TestConstants.DefaultScope.ToArray(),
                TokenCache = null
            };

            AcquireTokenInteractiveHandler handler = new AcquireTokenInteractiveHandler(data,
                TestConstants.ScopeForAnotherResource.ToArray(), new Uri("some://uri"), new PlatformParameters(),
                (string) null, UiOptions.ForceLogin, "extra=qp", webUi);
            handler.PreRunAsync().Wait();
            try
            {
                handler.PreTokenRequest().Wait();
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

            handler = new AcquireTokenInteractiveHandler(data,
                TestConstants.ScopeForAnotherResource.ToArray(), new Uri("some://uri"), new PlatformParameters(),
                (string)null, UiOptions.ForceLogin, "extra=qp", webUi);
            handler.PreRunAsync().Wait();

            try
            {
                handler.PreTokenRequest().Wait();
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

                HandlerData data = new HandlerData()
                {
                    Authenticator = authenticator,
                    ClientKey = new ClientKey(TestConstants.DefaultClientId),
                    Policy = TestConstants.DefaultPolicy,
                    RestrictToSingleUser = TestConstants.DefaultRestrictToSingleUser,
                    Scope = TestConstants.DefaultScope.ToArray(),
                    TokenCache = null
                };

                AcquireTokenInteractiveHandler handler = new AcquireTokenInteractiveHandler(data,
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
                HandlerData data = new HandlerData()
                {
                    Authenticator = authenticator,
                    ClientKey = new ClientKey(TestConstants.DefaultClientId),
                    Policy = TestConstants.DefaultPolicy,
                    RestrictToSingleUser = TestConstants.DefaultRestrictToSingleUser,
                    Scope = TestConstants.DefaultScope.ToArray(),
                    TokenCache = null
                };

                AcquireTokenInteractiveHandler handler = new AcquireTokenInteractiveHandler(data,
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

            HandlerData data = new HandlerData()
            {
                Authenticator = authenticator,
                ClientKey = new ClientKey(TestConstants.DefaultClientId),
                Policy = TestConstants.DefaultPolicy,
                RestrictToSingleUser = TestConstants.DefaultRestrictToSingleUser,
                Scope = TestConstants.DefaultScope.ToArray(),
                TokenCache = null
            };

            AcquireTokenInteractiveHandler handler = new AcquireTokenInteractiveHandler(data,
                TestConstants.ScopeForAnotherResource.ToArray(),
                new Uri("some://uri"), new PlatformParameters(),
                (User) null, UiOptions.ForceLogin, "extra=qp&prompt=login", new MockWebUI());
            handler.PreRunAsync().Wait();

            try
            {
                    handler.PreTokenRequest().Wait();
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