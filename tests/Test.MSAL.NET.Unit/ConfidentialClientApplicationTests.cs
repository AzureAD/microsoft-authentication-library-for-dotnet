using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.MSAL.NET.Unit.Mocks;

namespace Test.MSAL.NET.Unit
{
    [TestClass]
    public class ConfidentialClientApplicationTests
    {
        private const string AssertionType = "urn:ietf:params:oauth:grant-type:jwt-bearer";

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void ConstructorsTest()
        {
            ConfidentialClientApplication app = new ConfidentialClientApplication(TestConstants.DefaultClientId,
                TestConstants.DefaultRedirectUri, new ClientCredential("secret"), new TokenCache());
            Assert.IsNotNull(app);
            Assert.IsNotNull(app.UserTokenCache);
            Assert.IsNotNull(app.AppTokenCache);
            Assert.AreEqual("https://login.microsoftonline.com/common/", app.Authority);
            Assert.AreEqual(TestConstants.DefaultClientId, app.ClientId);
            Assert.AreEqual(TestConstants.DefaultRedirectUri, app.RedirectUri);
            Assert.AreEqual("https://login.microsoftonline.com/common/", app.Authority);
            Assert.IsNotNull(app.ClientCredential);
            Assert.IsNotNull(app.ClientCredential.Secret);
            Assert.AreEqual("secret", app.ClientCredential.Secret);
            Assert.IsNull(app.ClientCredential.Certificate);
            Assert.IsNull(app.ClientCredential.ClientAssertion);
            Assert.AreEqual(0, app.ClientCredential.ValidTo);

            app = new ConfidentialClientApplication(TestConstants.DefaultAuthorityGuestTenant,
                TestConstants.DefaultClientId,
                TestConstants.DefaultRedirectUri, new ClientCredential("secret"), new TokenCache());
            Assert.AreEqual(TestConstants.DefaultAuthorityGuestTenant, app.Authority);
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void ConfidentialClientTest()
        {
            ConfidentialClientApplication app = new ConfidentialClientApplication(TestConstants.DefaultClientId,
                TestConstants.DefaultRedirectUri, new ClientCredential("secret"), new TokenCache());
            app.AppTokenCache = new TokenCache();
            HttpMessageHandlerFactory.MockHandler = new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage()
            };

            Task<AuthenticationResult> task = app.AcquireTokenForClient(TestConstants.DefaultScope.ToArray(),
                TestConstants.DefaultPolicy);
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.IsNotNull("header.payload.signature", result.AccessToken);
            Assert.AreEqual(TestConstants.DefaultScope.AsSingleString(), result.ScopeSet.AsSingleString());

            //make sure user token cache is empty
            Assert.AreEqual(0, app.UserTokenCache.Count);

            //check app token cache count to be 1
            Assert.AreEqual(1, app.AppTokenCache.Count);
            //make sure refresh token is null
            foreach (var value in app.AppTokenCache.tokenCacheDictionary.Values)
            {
                Assert.IsNull(value.RefreshToken);
            }
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void OBOUserAssertionHashNotFoundTest()
        {
            TokenCache cache = TokenCacheHelper.CreateCacheWithItems();
            string someAssertion = "some-assertion-passed-by-developer";
            TokenCacheKey key = cache.tokenCacheDictionary.Keys.First();

            //update cache entry with hash of an assertion that will not match
            cache.tokenCacheDictionary[key].UserAssertionHash =
                new CryptographyHelper().CreateSha256Hash(someAssertion + "-but-not-in-cache");

            ConfidentialClientApplication app = new ConfidentialClientApplication(TestConstants.DefaultClientId,
                TestConstants.DefaultRedirectUri, new ClientCredential("secret"), new TokenCache());
            app.UserTokenCache = cache;

            string[] scope = {"mail.read"};
            HttpMessageHandlerFactory.MockHandler = new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage =
                    MockHelpers.CreateSuccessTokenResponseMessage("unique_id_3", "displayable@id3.com", "root_id_3",
                        scope)
            };

            UserAssertion assertion = new UserAssertion(someAssertion, AssertionType);
            Task<AuthenticationResult> task = app.AcquireTokenOnBehalfOfAsync(key.Scope.AsArray(),
                assertion, key.Authority, TestConstants.DefaultPolicy);
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual("unique_id_3", result.User.UniqueId);
            Assert.AreEqual("displayable@id3.com", result.User.DisplayableId);

            //check for new assertion Hash
            AuthenticationResultEx resultEx =
                cache.tokenCacheDictionary.Values.First(r => r.Result.User.UniqueId.Equals("unique_id_3"));
            Assert.AreEqual("nC2j5wL7iN83cU5DJsDXnt11TdEObirkKTVKari51Ps=", resultEx.UserAssertionHash);
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void OBOUserAssertionHashFoundTest()
        {
            TokenCache cache = TokenCacheHelper.CreateCacheWithItems();
            string someAssertion = "some-assertion-passed-by-developer";
            TokenCacheKey key = cache.tokenCacheDictionary.Keys.First();

            //update cache entry with hash of an assertion that will not match
            cache.tokenCacheDictionary[key].UserAssertionHash =
                new CryptographyHelper().CreateSha256Hash(someAssertion);

            ConfidentialClientApplication app = new ConfidentialClientApplication(TestConstants.DefaultClientId,
                TestConstants.DefaultRedirectUri, new ClientCredential("secret"), new TokenCache());
            app.UserTokenCache = cache;

            //this is a fail safe. No call should go on network
            HttpMessageHandlerFactory.MockHandler = new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage =
                    MockHelpers.CreateInvalidGrantTokenResponseMessage()
            };

            UserAssertion assertion = new UserAssertion(someAssertion, AssertionType);
            Task<AuthenticationResult> task = app.AcquireTokenOnBehalfOfAsync(key.Scope.AsArray(),
                assertion, key.Authority, TestConstants.DefaultPolicy);
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(key.UniqueId, result.User.UniqueId);
            Assert.AreEqual(key.DisplayableId, result.User.DisplayableId);
            Assert.AreEqual("nC2j5wL7iN83cU5DJsDXnt11TdEObirkKTVKari51Ps=",
                cache.tokenCacheDictionary[key].UserAssertionHash);
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void OBOUserAssertionHashUsernamePassedTest()
        {
            TokenCache cache = TokenCacheHelper.CreateCacheWithItems();
            string someAssertion = "some-assertion-passed-by-developer";
            TokenCacheKey key = cache.tokenCacheDictionary.Keys.First();

            //update cache entry with hash of an assertion that will not match
            cache.tokenCacheDictionary[key].UserAssertionHash =
                new CryptographyHelper().CreateSha256Hash(someAssertion);

            ConfidentialClientApplication app = new ConfidentialClientApplication(TestConstants.DefaultClientId,
                TestConstants.DefaultRedirectUri, new ClientCredential("secret"), new TokenCache());
            app.UserTokenCache = cache;

            //this is a fail safe. No call should go on network
            HttpMessageHandlerFactory.MockHandler = new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage =
                    MockHelpers.CreateInvalidGrantTokenResponseMessage()
            };

            UserAssertion assertion = new UserAssertion(someAssertion, AssertionType, key.DisplayableId);
            Task<AuthenticationResult> task = app.AcquireTokenOnBehalfOfAsync(key.Scope.AsArray(),
                assertion, key.Authority, TestConstants.DefaultPolicy);
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(key.UniqueId, result.User.UniqueId);
            Assert.AreEqual(key.DisplayableId, result.User.DisplayableId);
            Assert.AreEqual("nC2j5wL7iN83cU5DJsDXnt11TdEObirkKTVKari51Ps=",
                cache.tokenCacheDictionary[key].UserAssertionHash);
        }
    }
}