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

namespace Test.MSAL.NET.Unit
{
    [TestClass]
    public class AcquireTokenInteractiveHandlerTests
    {
        [TestMethod]
        [TestCategory("AcquireTokenInteractiveHandlerTests")]
        public void NoCacheLookup()
        {
            Authenticator authenticator = new Authenticator(TestConstants.DefaultAuthorityHomeTenant, false);
            TokenCache cache = new TokenCache();
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);
            AuthenticationResultEx ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(),
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(3599)));
            ex.Result.User = new User
            {
                DisplayableId = TestConstants.DefaultDisplayableId,
                UniqueId = TestConstants.DefaultUniqueId,
                RootId = TestConstants.DefaultRootId
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

            mockHandler.ResponseMessage = MockHelpers.CreatePositiveTokenResponseMessage();
            HttpMessageHandlerFactory.MockHandler = mockHandler;

            AcquireTokenInteractiveHandler handler = new AcquireTokenInteractiveHandler(authenticator, cache,
                TestConstants.DefaultScope.ToArray(), TestConstants.ScopeForAnotherResource.ToArray(),
                TestConstants.DefaultClientId, new Uri("some://uri"), new PlatformParameters(),
                TestConstants.DefaultDisplayableId, UiOptions.SelectAccount, "extra=qp", "some-policy", ui);
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
            Authenticator authenticator = new Authenticator(TestConstants.DefaultAuthorityHomeTenant, false);
            TokenCache cache = new TokenCache();
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);
            AuthenticationResultEx ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(),
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(3599)));
            ex.Result.User = new User
            {
                DisplayableId = TestConstants.DefaultDisplayableId,
                UniqueId = TestConstants.DefaultUniqueId,
                RootId = TestConstants.DefaultRootId
            };
            ex.Result.FamilyId = "1";
            ex.RefreshToken = "someRT";
            cache.tokenCacheDictionary[key] = ex;
            
            MockWebUI webUi = new MockWebUI();
            webUi.HeadersToValidate = new Dictionary<string, string>();
            webUi.HeadersToValidate["x-ms-sso-RefreshToken"] = "someRT";

            AcquireTokenInteractiveHandler handler = new AcquireTokenInteractiveHandler(authenticator, cache,
                TestConstants.DefaultScope.ToArray(), TestConstants.ScopeForAnotherResource.ToArray(),
                TestConstants.DefaultClientId, new Uri("some://uri"), new PlatformParameters(),
                TestConstants.DefaultDisplayableId, UiOptions.UseCurrentUser, "extra=qp", "some-policy", webUi);
            handler.PreRunAsync().Wait();
            handler.PreTokenRequest().Wait();
        }
    }
}