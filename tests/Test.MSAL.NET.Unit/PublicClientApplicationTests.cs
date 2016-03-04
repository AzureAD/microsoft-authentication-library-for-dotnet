using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.MSAL.Common.Unit;
using Test.MSAL.NET.Unit.Mocks;

namespace Test.MSAL.NET.Unit
{
    [TestClass]
    public class PublicClientApplicationTests
    {
        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void ConstructorsTest()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.DefaultClientId);
            Assert.IsNotNull(app);
            Assert.AreEqual("https://login.microsoftonline.com/common/", app.Authority);
            Assert.AreEqual(TestConstants.DefaultClientId, app.ClientId);
            Assert.AreEqual("urn:ietf:wg:oauth:2.0:oob", app.RedirectUri);
            Assert.IsTrue(app.ValidateAuthority);

            app = new PublicClientApplication(TestConstants.DefaultAuthorityGuestTenant, TestConstants.DefaultClientId);
            Assert.IsNotNull(app);
            Assert.AreEqual(TestConstants.DefaultAuthorityGuestTenant, app.Authority);
            Assert.AreEqual(TestConstants.DefaultClientId, app.ClientId);
            Assert.AreEqual("urn:ietf:wg:oauth:2.0:oob", app.RedirectUri);
            Assert.IsTrue(app.ValidateAuthority);
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void GetUsersTest()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.DefaultClientId);
            IEnumerable<User> users = app.Users;
            Assert.IsNotNull(users);
            Assert.IsFalse(users.Any());
            app.UserTokenCache = TokenCacheHelper.CreateCacheWithItems();
            users = app.Users;
            Assert.IsNotNull(users);
            Assert.AreEqual(2, users.Count());

            // another cache entry for same unique id. user count should still remain 2.
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
                RootId = TestConstants.DefaultHomeObjectId
            };
            ex.Result.ScopeSet = TestConstants.DefaultScope;

            ex.Result.FamilyId = "1";
            ex.RefreshToken = "someRT";
            app.UserTokenCache.tokenCacheDictionary[key] = ex;

            users = app.Users;
            Assert.IsNotNull(users);
            Assert.AreEqual(2, users.Count());
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSilentCacheOnlyLookupTest()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.DefaultClientId);
            app.UserTokenCache = TokenCacheHelper.CreateCacheWithItems();
            HttpMessageHandlerFactory.MockHandler = new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.Forbidden) //fail the request if it goes to http client due to any error
            };

            Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(TestConstants.DefaultScope.ToArray());
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, result.User.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultUniqueId, result.User.UniqueId);
            Assert.AreEqual(TestConstants.DefaultScope.AsSingleString(), result.Scope.AsSingleString());
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSilentForceRefreshTest()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.DefaultClientId);
            app.UserTokenCache = TokenCacheHelper.CreateCacheWithItems();

            HttpMessageHandlerFactory.MockHandler = new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreatePositiveTokenResponseMessage(TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultHomeObjectId, TestConstants.DefaultScope.Union(TestConstants.ScopeForAnotherResource).ToArray())
            };

            Task<AuthenticationResult> task = app.AcquireTokenSilentAsync(TestConstants.DefaultScope.ToArray(), TestConstants.DefaultUniqueId, app.Authority, null, true);
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, result.User.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultUniqueId, result.User.UniqueId);
            Assert.AreEqual(TestConstants.DefaultScope.Union(TestConstants.ScopeForAnotherResource).ToArray().AsSingleString(), result.Scope.AsSingleString());
        }

        [TestMethod]
        [TestCategory("PublicClientApplicationTests")]
        public void AcquireTokenSilentServiceErrorTest()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.DefaultClientId);
            app.UserTokenCache = TokenCacheHelper.CreateCacheWithItems();

            MockHttpMessageHandler mockHandler = new MockHttpMessageHandler();
            mockHandler.Method = HttpMethod.Post;
            mockHandler.ResponseMessage = MockHelpers.CreateInvalidGrantTokenResponseMessage();
            HttpMessageHandlerFactory.MockHandler = mockHandler;
                try
                {
                    Task<AuthenticationResult> task =app.AcquireTokenSilentAsync(TestConstants.ScopeForAnotherResource.ToArray(), TestConstants.DefaultUniqueId);
                    AuthenticationResult result = task.Result;
                    Assert.Fail("AdalSilentTokenAcquisitionException was expected");
                }
                catch (AggregateException ex)
                {
                    Assert.IsNotNull(ex.InnerException);

                    Assert.IsTrue(ex.InnerException is MsalSilentTokenAcquisitionException);
                    var msalExc = (MsalSilentTokenAcquisitionException) ex.InnerException;
                    Assert.AreEqual(MsalError.FailedToAcquireTokenSilently, msalExc.ErrorCode);
                    Assert.IsNotNull(msalExc.InnerException, "MsalSilentTokenAcquisitionException inner exception is null");
                    Assert.AreEqual(((MsalException)msalExc.InnerException).ErrorCode, "invalid_grant");
                }
            }


            /*        [TestMethod]
                    [TestCategory("PublicClientApplicationTests")]
                    public void AcquireTokenMoreScopesTest()
                    {
                        PublicClientApplication app = new PublicClientApplication(TestConstants.DefaultClientId);
                        app.UserTokenCache = TokenCacheTests.CreateCacheWithItems();
                        string[] scope = TestConstants.DefaultScope.Union(TestConstants.ScopeForAnotherResource).ToArray();

                        MockWebUI webUi

                        //ask for scopes that already exist in the cache. Interactive call will ignore the cache lookup.
                        Task<AuthenticationResult> task = app.AcquireTokenAsync(scope, TestConstants.DefaultDisplayableId);
                        task.Wait();
                        AuthenticationResult result = task.Result;
                        Assert.IsNotNull(result);
                    }*/
        }
}
