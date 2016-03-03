using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.MSAL.Common.Unit;

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

            TokenCacheTests.LoadCacheItems(app.UserTokenCache);
            users = app.Users;
            Assert.IsNotNull(users);
            Assert.AreEqual(2, users.Count());

            // another cache entry for same unique id. user count should still remain 2.
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.ScopeForAnotherResource, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultRootId,
                TestConstants.DefaultPolicy);
            AuthenticationResultEx ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(),
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(3600)));
            ex.Result.User = new User
            {
                DisplayableId = TestConstants.DefaultDisplayableId,
                UniqueId = TestConstants.DefaultUniqueId,
                RootId = TestConstants.DefaultRootId
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
        public void AcquireTokenTest()
        {
            PublicClientApplication app = new PublicClientApplication(TestConstants.DefaultClientId);
            TokenCacheTests.LoadCacheItems(app.UserTokenCache);
            
            //ask for scopes that already exist in the cache. Interactive call will ignore the cache lookup.
            Task<AuthenticationResult> task = app.AcquireTokenAsync(TestConstants.DefaultScope.ToArray());
            task.Wait();
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
        }
    }
}
