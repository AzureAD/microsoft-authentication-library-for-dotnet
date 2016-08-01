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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.MSAL.NET.Unit.Mocks;
using System.Security.Cryptography.X509Certificates;

namespace Test.MSAL.NET.Unit
{
    [TestClass]
    [DeploymentItem(@"Resources\valid_cert.pfx")]
    public class ConfidentialClientApplicationTests
    {
        private const string AssertionType = "urn:ietf:params:oauth:grant-type:jwt-bearer";
        //The following string is hash code for a mocked Access Token
        //[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine")]
        private const string HashAccessToken = "nC2j5wL7iN83cU5DJsDXnt11TdEObirkKTVKari51Ps=";

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void ConstructorsTest()
        {
            ConfidentialClientApplication app = new ConfidentialClientApplication(TestConstants.DefaultClientId,
                TestConstants.DefaultRedirectUri, new ClientCredential(TestConstants.DefaultClientSecret), new TokenCache());
            Assert.IsNotNull(app);
            Assert.IsNotNull(app.UserTokenCache);
            Assert.IsNotNull(app.AppTokenCache);
            Assert.AreEqual("https://login.microsoftonline.com/common/", app.Authority);
            Assert.AreEqual(TestConstants.DefaultClientId, app.ClientId);
            Assert.AreEqual(TestConstants.DefaultRedirectUri, app.RedirectUri);
            Assert.AreEqual("https://login.microsoftonline.com/common/", app.Authority);
            Assert.IsNotNull(app.ClientCredential);
            Assert.IsNotNull(app.ClientCredential.Secret);
            Assert.AreEqual(TestConstants.DefaultClientSecret, app.ClientCredential.Secret);
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
        public void ConfidentialClientUsingSecretTest()
        {
            ConfidentialClientApplication app = new ConfidentialClientApplication(TestConstants.DefaultClientId,
                TestConstants.DefaultRedirectUri, new ClientCredential(TestConstants.DefaultClientSecret), new TokenCache());
            app.AppTokenCache = new TokenCache();
            HttpMessageHandlerFactory.MockHandler = new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage()
            };

            Task<AuthenticationResult> task = app.AcquireTokenForClient(TestConstants.DefaultScope,
                TestConstants.DefaultPolicy);
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.IsNotNull("header.payload.signature", result.Token);
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
        public void ConfidentialClientUsingCertificateTest()
        {
            ClientCredential cc = new ClientCredential(new ClientAssertionCertificate(new X509Certificate2("valid_cert.pfx", "password")));
            ConfidentialClientApplication app = new ConfidentialClientApplication(TestConstants.DefaultClientId,
                TestConstants.DefaultRedirectUri, cc, new TokenCache());
            app.AppTokenCache = new TokenCache();
            HttpMessageHandlerFactory.MockHandler = new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage()
            };

            Task<AuthenticationResult> task = app.AcquireTokenForClient(TestConstants.DefaultScope,
                TestConstants.DefaultPolicy);
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.IsNotNull("header.payload.signature", result.Token);
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

            //assert client credential
            Assert.IsNotNull(cc.ClientAssertion);
            Assert.AreNotEqual(0, cc.ValidTo);

            //save client assertion.
            string cachedAssertion = cc.ClientAssertion.Assertion;
            long cacheValidTo = cc.ValidTo;

            HttpMessageHandlerFactory.MockHandler = new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage()
            };

            task = app.AcquireTokenForClient(TestConstants.ScopeForAnotherResource.ToArray(),
                TestConstants.DefaultPolicy);
            result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(cacheValidTo, cc.ValidTo);
            Assert.AreEqual(cachedAssertion, cc.ClientAssertion.Assertion);
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
                TestConstants.DefaultRedirectUri, new ClientCredential(TestConstants.DefaultClientSecret), new TokenCache());
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
            Assert.AreEqual(HashAccessToken, resultEx.UserAssertionHash);
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
                TestConstants.DefaultRedirectUri, new ClientCredential(TestConstants.DefaultClientSecret), new TokenCache());
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
            Assert.AreEqual(HashAccessToken,
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
                TestConstants.DefaultRedirectUri, new ClientCredential(TestConstants.DefaultClientSecret), new TokenCache());
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
            Assert.AreEqual(HashAccessToken,
                cache.tokenCacheDictionary[key].UserAssertionHash);
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void GetAuthorizationRequestUrlNoRedirectUriTest()
        {
            ConfidentialClientApplication app = new ConfidentialClientApplication(TestConstants.DefaultClientId,
                TestConstants.DefaultRedirectUri, new ClientCredential(TestConstants.DefaultClientSecret), new TokenCache());
            Task<Uri> task = app.GetAuthorizationRequestUrlAsync(TestConstants.DefaultScope.AsArray(), TestConstants.DefaultDisplayableId, null);
            Uri uri = task.Result;
            Assert.IsNotNull(uri);
            Dictionary<string, string> qp = EncodingHelper.ParseKeyValueList(uri.Query.Substring(1),'&', true, null);
            Assert.IsNotNull(qp);
            Assert.AreEqual(9, qp.Count);
            Assert.AreEqual("r1/scope1 r1/scope2 openid email profile offline_access", qp["scope"]);
            Assert.AreEqual(TestConstants.DefaultClientId, qp["client_id"]);
            Assert.AreEqual("code", qp["response_type"]);
            Assert.AreEqual(TestConstants.DefaultRedirectUri, qp["redirect_uri"]);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, qp["login_hint"]);
            Assert.AreEqual("MSAL.Desktop", qp["x-client-sku"]);
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-ver"]));
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-cpu"]));
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-os"]));


            app = new ConfidentialClientApplication(TestConstants.DefaultClientId,
                TestConstants.DefaultRedirectUri, new ClientCredential(TestConstants.DefaultClientSecret), new TokenCache());
            task = app.GetAuthorizationRequestUrlAsync(TestConstants.DefaultScope.AsArray(), TestConstants.DefaultDisplayableId, "extra=qp&prompt=none");
            uri = task.Result;
            Assert.IsNotNull(uri);
            Assert.IsTrue(uri.AbsoluteUri.StartsWith(app.Authority, StringComparison.CurrentCulture));
            qp = EncodingHelper.ParseKeyValueList(uri.Query.Substring(1), '&', true, null);
            Assert.IsNotNull(qp);
            Assert.AreEqual(11, qp.Count);
            Assert.AreEqual("r1/scope1 r1/scope2 openid email profile offline_access", qp["scope"]);
            Assert.AreEqual(TestConstants.DefaultClientId, qp["client_id"]);
            Assert.AreEqual("code", qp["response_type"]);
            Assert.AreEqual(TestConstants.DefaultRedirectUri, qp["redirect_uri"]);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, qp["login_hint"]);
            Assert.AreEqual("MSAL.Desktop", qp["x-client-sku"]);
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-ver"]));
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-cpu"]));
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-os"]));
            Assert.AreEqual("qp", qp["extra"]);
            Assert.AreEqual("none", qp["prompt"]);
        }


        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void GetAuthorizationRequestUrlDuplicateParamsTest()
        {

            ConfidentialClientApplication app = new ConfidentialClientApplication(TestConstants.DefaultClientId,
                TestConstants.DefaultRedirectUri, new ClientCredential(TestConstants.DefaultClientSecret), new TokenCache());
            try
            {
                Task<Uri> task = app.GetAuthorizationRequestUrlAsync(TestConstants.DefaultScope.AsArray(),
                    TestConstants.DefaultDisplayableId, "login_hint=some@value.com");
                Uri uri = task.Result;
                Assert.Fail("MSALException should be thrown here");
            }
            catch (Exception exc)
            {
                Assert.IsTrue(exc.InnerException is MsalException);
                Assert.AreEqual("duplicate_query_parameter", ((MsalException)exc.InnerException).ErrorCode);
                Assert.AreEqual("Duplicate query parameter 'login_hint' in extraQueryParameters", ((MsalException)exc.InnerException).Message);

            }
        }


        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void GetAuthorizationRequestUrlCustomRedirectUriTest()
        {
            ConfidentialClientApplication app = new ConfidentialClientApplication(TestConstants.DefaultClientId,
                TestConstants.DefaultRedirectUri, new ClientCredential(TestConstants.DefaultClientSecret), new TokenCache());
            Task<Uri> task = app.GetAuthorizationRequestUrlAsync(TestConstants.DefaultScope.AsArray(),
                "custom://redirect-uri", TestConstants.DefaultDisplayableId, "extra=qp&prompt=none",
                TestConstants.ScopeForAnotherResource.AsArray(), TestConstants.DefaultAuthorityGuestTenant,
                TestConstants.DefaultPolicy);
            Uri uri = task.Result;
            Assert.IsNotNull(uri);
            Assert.IsTrue(uri.AbsoluteUri.StartsWith(TestConstants.DefaultAuthorityGuestTenant, StringComparison.CurrentCulture));
            Dictionary<string, string> qp = EncodingHelper.ParseKeyValueList(uri.Query.Substring(1), '&', true, null);
            Assert.IsNotNull(qp);
            Assert.AreEqual(12, qp.Count);
            Assert.AreEqual("r1/scope1 r1/scope2 r2/scope1 r2/scope2 openid offline_access", qp["scope"]);
            Assert.AreEqual(TestConstants.DefaultClientId, qp["client_id"]);
            Assert.AreEqual("code", qp["response_type"]);
            Assert.AreEqual("custom://redirect-uri", qp["redirect_uri"]);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, qp["login_hint"]);
            Assert.AreEqual("MSAL.Desktop", qp["x-client-sku"]);
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-ver"]));
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-cpu"]));
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-os"]));
            Assert.AreEqual("qp", qp["extra"]);
            Assert.AreEqual("none", qp["prompt"]);
            Assert.AreEqual(TestConstants.DefaultPolicy, qp["p"]);
        }
    }
}
