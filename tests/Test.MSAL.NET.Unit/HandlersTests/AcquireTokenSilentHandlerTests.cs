using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Handlers;
using Microsoft.Identity.Client.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.MSAL.Common.Unit;
using Test.MSAL.NET.Unit.Mocks;

namespace Test.MSAL.NET.Unit.HandlersTests
{
    [TestClass]
    public class AcquireTokenSilentHandlerTests
    {
        [TestMethod]
        [TestCategory("AcquireTokenSilentHandlerTests")]
        public void ConstructorTests()
        {
            Authenticator authenticator = new Authenticator(TestConstants.DefaultAuthorityHomeTenant, false,
                Guid.NewGuid());
            TokenCache cache = new TokenCache();
            HandlerData data = new HandlerData()
            {
                Authenticator = authenticator,
                ClientKey = new ClientKey(TestConstants.DefaultClientId),
                Policy = TestConstants.DefaultPolicy,
                RestrictToSingleUser = true,
                Scope = TestConstants.DefaultScope.ToArray(),
                TokenCache = cache
            };

            AcquireTokenSilentHandler handler = new AcquireTokenSilentHandler(data, (string) null,
                new PlatformParameters());
            Assert.IsNotNull(handler);

            handler = new AcquireTokenSilentHandler(data, (User) null, new PlatformParameters());
            Assert.IsNotNull(handler);

            handler = new AcquireTokenSilentHandler(data, TestConstants.DefaultDisplayableId, new PlatformParameters());
            Assert.IsNotNull(handler);

            handler = new AcquireTokenSilentHandler(data, TestConstants.DefaultUniqueId, new PlatformParameters());
            Assert.IsNotNull(handler);

            handler = new AcquireTokenSilentHandler(data, TestConstants.DefaultUser, new PlatformParameters());
            Assert.IsNotNull(handler);
        }


        [TestMethod]
        [TestCategory("AcquireTokenSilentHandlerTests")]
        public void MapToIdentifierNullInputTest()
        {
            Authenticator authenticator = new Authenticator(TestConstants.DefaultAuthorityHomeTenant, false,
                Guid.NewGuid());
            TokenCache cache = new TokenCache();
            HandlerData data = new HandlerData()
            {
                Authenticator = authenticator,
                ClientKey = new ClientKey(TestConstants.DefaultClientId),
                Policy = TestConstants.DefaultPolicy,
                RestrictToSingleUser = true,
                Scope = TestConstants.DefaultScope.ToArray(),
                TokenCache = cache
            };

            AcquireTokenSilentHandler handler = new AcquireTokenSilentHandler(data, (string)null,
                new PlatformParameters());
            User user = handler.MapIdentifierToUser(null);
            Assert.IsNull(user);
        }

        [TestMethod]
        [TestCategory("AcquireTokenSilentHandlerTests")]
        public void MapToIdentifierNoItemFoundTest()
        {
            Authenticator authenticator = new Authenticator(TestConstants.DefaultAuthorityHomeTenant, false,
                Guid.NewGuid());
            TokenCache cache = new TokenCache();
            HandlerData data = new HandlerData()
            {
                Authenticator = authenticator,
                ClientKey = new ClientKey(TestConstants.DefaultClientId),
                Policy = TestConstants.DefaultPolicy,
                RestrictToSingleUser = true,
                Scope = TestConstants.DefaultScope.ToArray(),
                TokenCache = cache
            };

            AcquireTokenSilentHandler handler = new AcquireTokenSilentHandler(data, (string) null,
                new PlatformParameters());
            User user = handler.MapIdentifierToUser(TestConstants.DefaultUniqueId);
            Assert.IsNull(user);
        }

        [TestMethod]
        [TestCategory("AcquireTokenSilentHandlerTests")]
        public void MapToIdentifierItemFoundTest()
        {
            Authenticator authenticator = new Authenticator(TestConstants.DefaultAuthorityHomeTenant, false,
                Guid.NewGuid());
            TokenCache cache = TokenCacheTests.CreateCacheWithItems();
            HandlerData data = new HandlerData()
            {
                Authenticator = authenticator,
                ClientKey = new ClientKey(TestConstants.DefaultClientId),
                Policy = TestConstants.DefaultPolicy,
                RestrictToSingleUser = TestConstants.DefaultRestrictToSingleUser,
                Scope = TestConstants.DefaultScope.ToArray(),
                TokenCache = cache
            };

            AcquireTokenSilentHandler handler = new AcquireTokenSilentHandler(data, (string)null,
                new PlatformParameters());
            User user = handler.MapIdentifierToUser(TestConstants.DefaultUniqueId);
            Assert.IsNotNull(user);
            Assert.AreEqual(TestConstants.DefaultUniqueId, user.UniqueId);
        }

        [TestMethod]
        [TestCategory("AcquireTokenSilentHandlerTests")]
        public void MapToIdentifierMultipleMatchingEntriesTest()
        {
            Authenticator authenticator = new Authenticator(TestConstants.DefaultAuthorityHomeTenant, false,
                Guid.NewGuid());
            TokenCache cache = TokenCacheTests.CreateCacheWithItems();

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
            cache.tokenCacheDictionary[key] = ex;


            HandlerData data = new HandlerData()
            {
                Authenticator = authenticator,
                ClientKey = new ClientKey(TestConstants.DefaultClientId),
                Policy = TestConstants.DefaultPolicy,
                RestrictToSingleUser = TestConstants.DefaultRestrictToSingleUser,
                Scope = new[] { "something" },
                TokenCache = cache
            };

            AcquireTokenSilentHandler handler = new AcquireTokenSilentHandler(data, (string) null,
                new PlatformParameters());
            User user = handler.MapIdentifierToUser(TestConstants.DefaultUniqueId);
            Assert.IsNotNull(user);
            Assert.AreEqual(TestConstants.DefaultUniqueId, user.UniqueId);
        }

        [TestMethod]
        [TestCategory("AcquireTokenSilentHandlerTests")]
        public void ExpiredTokenRefreshFlowTest()
        {
            Authenticator authenticator = new Authenticator(TestConstants.DefaultAuthorityHomeTenant, false,
                Guid.NewGuid());
            TokenCache cache = TokenCacheTests.CreateCacheWithItems();

            HandlerData data = new HandlerData()
            {
                Authenticator = authenticator,
                ClientKey = new ClientKey(TestConstants.DefaultClientId),
                Policy = TestConstants.DefaultPolicy,
                RestrictToSingleUser = TestConstants.DefaultRestrictToSingleUser,
                Scope = new[] { "some-scope1", "some-scope2" },
                TokenCache = cache
            };

            HttpMessageHandlerFactory.MockHandler = new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreatePositiveTokenResponseMessage()
            };

            AcquireTokenSilentHandler handler = new AcquireTokenSilentHandler(data, (string)null,
                new PlatformParameters());
            Task<AuthenticationResult> task = handler.RunAsync();
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual("some-access-token", result.AccessToken);
            Assert.AreEqual("some-scope1 some-scope2", result.Scope.AsSingleString());
        }


        [TestMethod]
        [TestCategory("AcquireTokenSilentHandlerTests")]
        public void SilentRefreshFailedNoCacheItemFoundTest()
        {
            Authenticator authenticator = new Authenticator(TestConstants.DefaultAuthorityHomeTenant, false,
                Guid.NewGuid());
            TokenCache cache = new TokenCache();

            HandlerData data = new HandlerData()
            {
                Authenticator = authenticator,
                ClientKey = new ClientKey(TestConstants.DefaultClientId),
                Policy = TestConstants.DefaultPolicy,
                RestrictToSingleUser = TestConstants.DefaultRestrictToSingleUser,
                Scope = new[] { "some-scope1", "some-scope2" },
                TokenCache = cache
            };

            HttpMessageHandlerFactory.MockHandler = new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreatePositiveTokenResponseMessage()
            };

            try
            {
                AcquireTokenSilentHandler handler = new AcquireTokenSilentHandler(data, (string) null,
                    new PlatformParameters());
                Task<AuthenticationResult> task = handler.RunAsync();
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
