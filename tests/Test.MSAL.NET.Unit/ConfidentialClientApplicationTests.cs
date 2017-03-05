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
using Microsoft.Identity.Client.Internal.Http;
using Microsoft.Identity.Client.Internal.Instance;

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

        [TestInitialize]
        public void TestInitialize()
        {
            Authority.ValidatedAuthorities.Clear();
            HttpClientFactory.ReturnHttpClientForMocks = true;
            HttpMessageHandlerFactory.ClearMockHandlers();
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void ConstructorsTest()
        {
            ConfidentialClientApplication app = new ConfidentialClientApplication(TestConstants.ClientId,
                TestConstants.RedirectUri, new ClientCredential(TestConstants.ClientSecret),
                new TokenCache(), new TokenCache());
            Assert.IsNotNull(app);
            Assert.IsNotNull(app.UserTokenCache);
            Assert.IsNotNull(app.AppTokenCache);
            Assert.AreEqual("https://login.microsoftonline.com/common/", app.Authority);
            Assert.AreEqual(TestConstants.ClientId, app.ClientId);
            Assert.AreEqual(TestConstants.RedirectUri, app.RedirectUri);
            Assert.AreEqual("https://login.microsoftonline.com/common/", app.Authority);
            Assert.IsNotNull(app.ClientCredential);
            Assert.IsNotNull(app.ClientCredential.Secret);
            Assert.AreEqual(TestConstants.ClientSecret, app.ClientCredential.Secret);
            Assert.IsNull(app.ClientCredential.Certificate);
            Assert.IsNull(app.ClientCredential.Assertion);
            Assert.AreEqual(0, app.ClientCredential.ValidTo);

            app = new ConfidentialClientApplication(TestConstants.ClientId,
                TestConstants.AuthorityGuestTenant,
                TestConstants.RedirectUri, new ClientCredential("secret"), new TokenCache(),
                new TokenCache());
            Assert.AreEqual(TestConstants.AuthorityGuestTenant, app.Authority);
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void ConfidentialClientUsingSecretTest()
        {
            ConfidentialClientApplication app = new ConfidentialClientApplication(TestConstants.ClientId,
                TestConstants.RedirectUri, new ClientCredential(TestConstants.ClientSecret),
                new TokenCache(), new TokenCache())
            {
                ValidateAuthority = false
            };
            
            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(app.Authority)
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage()
            });

            Task<AuthenticationResult> task = app.AcquireTokenForClientAsync(TestConstants.Scope.ToArray());
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.IsNotNull("header.payload.signature", result.AccessToken);
            Assert.AreEqual(TestConstants.Scope.AsSingleString(), result.Scope.AsSingleString());

            //make sure user token cache is empty
            Assert.AreEqual(0, app.UserTokenCache.AccessTokenCount);
            Assert.AreEqual(0, app.UserTokenCache.RefreshTokenCount);

            //check app token cache count to be 1
            Assert.AreEqual(1, app.AppTokenCache.AccessTokenCount);
            Assert.AreEqual(0, app.AppTokenCache.RefreshTokenCount); //no refresh tokens are returned

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void ConfidentialClientUsingCertificateTest()
        {
            ClientCredential cc =
                new ClientCredential(new ClientAssertionCertificate(new X509Certificate2("valid_cert.pfx", "password")));
            ConfidentialClientApplication app = new ConfidentialClientApplication(TestConstants.ClientId,
                TestConstants.RedirectUri, cc, new TokenCache(),
                new TokenCache())
            {
                ValidateAuthority = false
            };

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(app.Authority)
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage()
            });

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage()
            });

            Task<AuthenticationResult> task = app.AcquireTokenForClientAsync(TestConstants.Scope.ToArray());
            AuthenticationResult result = task.Result;
            Assert.IsNotNull(result);
            Assert.IsNotNull("header.payload.signature", result.AccessToken);
            Assert.AreEqual(TestConstants.Scope.AsSingleString(), result.Scope.AsSingleString());

            //make sure user token cache is empty
            Assert.AreEqual(0, app.UserTokenCache.AccessTokenCount);
            Assert.AreEqual(0, app.UserTokenCache.RefreshTokenCount);

            //check app token cache count to be 1
            Assert.AreEqual(1, app.AppTokenCache.AccessTokenCount);
            Assert.AreEqual(0, app.AppTokenCache.RefreshTokenCount); //no refresh tokens are returned

            //assert client credential
            Assert.IsNotNull(cc.Assertion);
            Assert.AreNotEqual(0, cc.ValidTo);

            //save client assertion.
            string cachedAssertion = cc.Assertion;
            long cacheValidTo = cc.ValidTo;

            task = app.AcquireTokenForClientAsync(TestConstants.ScopeForAnotherResource.ToArray());
            result = task.Result;
            Assert.IsNotNull(result);
            Assert.AreEqual(cacheValidTo, cc.ValidTo);
            Assert.AreEqual(cachedAssertion, cc.Assertion);

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }

        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void GetAuthorizationRequestUrlNoRedirectUriTest()
        {
            ConfidentialClientApplication app = new ConfidentialClientApplication(TestConstants.ClientId,
                TestConstants.RedirectUri, new ClientCredential(TestConstants.ClientSecret),
                new TokenCache(), new TokenCache())
            {
                ValidateAuthority = false
            };

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(app.Authority)
            });

            Task<Uri> task = app.GetAuthorizationRequestUrlAsync(TestConstants.Scope.AsArray(),
                TestConstants.DisplayableId, null);
            Uri uri = task.Result;
            Assert.IsNotNull(uri);
            Dictionary<string, string> qp = MsalHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', true, null);
            Assert.IsNotNull(qp);
            Assert.AreEqual(10, qp.Count);
            Assert.IsTrue(qp.ContainsKey("client-request-id"));
            Assert.AreEqual("offline_access openid profile r1/scope1 r1/scope2", qp["scope"]);
            Assert.AreEqual(TestConstants.ClientId, qp["client_id"]);
            Assert.AreEqual("code", qp["response_type"]);
            Assert.AreEqual(TestConstants.RedirectUri, qp["redirect_uri"]);
            Assert.AreEqual(TestConstants.DisplayableId, qp["login_hint"]);
            Assert.AreEqual("MSAL.Desktop", qp["x-client-sku"]);
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-ver"]));
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-cpu"]));
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-os"]));


            app = new ConfidentialClientApplication(TestConstants.ClientId,
                TestConstants.RedirectUri, new ClientCredential(TestConstants.ClientSecret),
                new TokenCache(), new TokenCache())
            {
                ValidateAuthority = false
            };

            task = app.GetAuthorizationRequestUrlAsync(TestConstants.Scope.AsArray(), TestConstants.DisplayableId,
                "extra=qp&prompt=none");
            uri = task.Result;
            Assert.IsNotNull(uri);
            Assert.IsTrue(uri.AbsoluteUri.StartsWith(app.Authority, StringComparison.CurrentCulture));
            qp = MsalHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', true, null);
            Assert.IsNotNull(qp);
            Assert.AreEqual(12, qp.Count);
            Assert.IsTrue(qp.ContainsKey("client-request-id"));
            Assert.AreEqual("offline_access openid profile r1/scope1 r1/scope2", qp["scope"]);
            Assert.AreEqual(TestConstants.ClientId, qp["client_id"]);
            Assert.AreEqual("code", qp["response_type"]);
            Assert.AreEqual(TestConstants.RedirectUri, qp["redirect_uri"]);
            Assert.AreEqual(TestConstants.DisplayableId, qp["login_hint"]);
            Assert.AreEqual("MSAL.Desktop", qp["x-client-sku"]);
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-ver"]));
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-cpu"]));
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-os"]));
            Assert.AreEqual("qp", qp["extra"]);
            Assert.AreEqual("none", qp["prompt"]);

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }


        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void GetAuthorizationRequestUrlDuplicateParamsTest()
        {
            ConfidentialClientApplication app = new ConfidentialClientApplication(TestConstants.ClientId,
                TestConstants.RedirectUri, new ClientCredential(TestConstants.ClientSecret),
                new TokenCache(), new TokenCache())
            {
                ValidateAuthority = false
            };

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(app.Authority)
            });

            try
            {
                Task<Uri> task = app.GetAuthorizationRequestUrlAsync(TestConstants.Scope.AsArray(),
                    TestConstants.DisplayableId, "login_hint=some@value.com");
                Uri uri = task.Result;
                Assert.Fail("MSALException should be thrown here");
            }
            catch (Exception exc)
            {
                Assert.IsTrue(exc.InnerException is MsalException);
                Assert.AreEqual("duplicate_query_parameter", ((MsalException) exc.InnerException).ErrorCode);
                Assert.AreEqual("Duplicate query parameter 'login_hint' in extraQueryParameters",
                    ((MsalException) exc.InnerException).Message);
            }

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }


        [TestMethod]
        [TestCategory("ConfidentialClientApplicationTests")]
        public void GetAuthorizationRequestUrlCustomRedirectUriTest()
        {
            ConfidentialClientApplication app =
                new ConfidentialClientApplication(TestConstants.ClientId, TestConstants.AuthorityGuestTenant,
                    TestConstants.RedirectUri, new ClientCredential(TestConstants.ClientSecret),
                    new TokenCache(), new TokenCache())
                {ValidateAuthority = false};

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(app.Authority)
            });

            Task<Uri> task = app.GetAuthorizationRequestUrlAsync(TestConstants.Scope.AsArray(),
                "custom://redirect-uri", TestConstants.DisplayableId, "extra=qp&prompt=none",
                TestConstants.ScopeForAnotherResource.AsArray(), TestConstants.AuthorityGuestTenant);
            Uri uri = task.Result;
            Assert.IsNotNull(uri);
            Assert.IsTrue(uri.AbsoluteUri.StartsWith(TestConstants.AuthorityGuestTenant, StringComparison.CurrentCulture));
            Dictionary<string, string> qp = MsalHelpers.ParseKeyValueList(uri.Query.Substring(1), '&', true, null);
            Assert.IsNotNull(qp);
            Assert.AreEqual(12, qp.Count);
            Assert.IsTrue(qp.ContainsKey("client-request-id"));
            Assert.IsFalse(qp.ContainsKey("client_secret"));
            Assert.AreEqual("offline_access openid profile r1/scope1 r1/scope2 r2/scope1 r2/scope2", qp["scope"]);
            Assert.AreEqual(TestConstants.ClientId, qp["client_id"]);
            Assert.AreEqual("code", qp["response_type"]);
            Assert.AreEqual("custom://redirect-uri/", qp["redirect_uri"]);
            Assert.AreEqual(TestConstants.DisplayableId, qp["login_hint"]);
            Assert.AreEqual("MSAL.Desktop", qp["x-client-sku"]);
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-ver"]));
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-cpu"]));
            Assert.IsFalse(string.IsNullOrEmpty(qp["x-client-os"]));
            Assert.AreEqual("qp", qp["extra"]);
            Assert.AreEqual("none", qp["prompt"]);

            Assert.IsTrue(HttpMessageHandlerFactory.IsMocksQueueEmpty, "All mocks should have been consumed");
        }
    }
}
