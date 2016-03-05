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

            app = new ConfidentialClientApplication(TestConstants.DefaultAuthorityGuestTenant, TestConstants.DefaultClientId,
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

            Task<AuthenticationResult> task = app.AcquireTokenForClient(TestConstants.DefaultScope.ToArray(), TestConstants.DefaultPolicy);
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
    }
}
