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

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Helpers;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Test.ADAL.NET.Common;
using Test.ADAL.NET.Common.Mocks;
using AuthenticationContext = Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext;

namespace Test.ADAL.NET.Unit
{
    [TestClass]
    [DeploymentItem("valid_cert.pfx")]
    public class JsonWebTokenTests
    {
        private PlatformParameters platformParameters;

        MockHttpMessageHandler X5CMockHandler = new MockHttpMessageHandler(request =>
        {
            var requestContent = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            var formsData = EncodingHelper.ParseKeyValueList(requestContent, '&', true, null);

            // Check presence of client_assertion in request
            Assert.IsTrue(formsData.TryGetValue("client_assertion", out string encodedJwt), "Missing client_assertion from request");

            // Check presence of x5c cert claim. It should exist.
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(encodedJwt);
            var x5c = jsonToken.Header.Where(header => header.Key == "x5c").FirstOrDefault();
            Assert.IsTrue(x5c.Key == "x5c");
        })
        {
            Method = HttpMethod.Post,
            ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultResource)
    };

        MockHttpMessageHandler EmptyX5CMockHandler = new MockHttpMessageHandler(request =>
            {
                var requestContent = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var formsData = EncodingHelper.ParseKeyValueList(requestContent, '&', true, null);

                // Check presence of client_assertion in request
                Assert.IsTrue(formsData.TryGetValue("client_assertion", out string encodedJwt), "Missing client_assertion from request");

                // Check presence of x5c cert claim. It should not exist.
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(encodedJwt);
                var x5c = jsonToken.Header.Where(header => header.Key == "x5c").FirstOrDefault();
                Assert.IsTrue(x5c.Key != "x5c");
            })
        {
            Method = HttpMethod.Post,
            ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultResource)
    };

        [TestInitialize]
        public void Initialize()
        {
            ModuleInitializer.ForceModuleInitializationTestOnly();
            AdalHttpMessageHandlerFactory.InitializeMockProvider();
            InstanceDiscovery.InstanceCache.Clear();
            AdalHttpMessageHandlerFactory.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler(TestConstants.GetDiscoveryEndpoint(TestConstants.DefaultAuthorityCommonTenant)));
            platformParameters = new PlatformParameters(PromptBehavior.Auto);
        }

        [TestMethod]
        [Description("Test for Json Web Token with client assertion and a X509 public certificate claim")]
        public async Task JsonWebTokenWithX509PublicCertClaimTestAsync()
        {
            var certificate = new X509Certificate2("valid_cert.pfx", TestConstants.DefaultPassword);
            var clientAssertion = new ClientAssertionCertificate(TestConstants.DefaultClientId, certificate);
            var context = new AuthenticationContext(TestConstants.TenantSpecificAuthority, new TokenCache());

            var validCertClaim = "\"x5c\":\"" + Convert.ToBase64String(certificate.GetRawCertData());

            //Check for x5c claim
            AdalHttpMessageHandlerFactory.AddMockHandler(X5CMockHandler);
            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, clientAssertion, true).ConfigureAwait(false);
            Assert.IsNotNull(result.AccessToken);

            AdalHttpMessageHandlerFactory.AddMockHandler(X5CMockHandler);
            result = await context.AcquireTokenByAuthorizationCodeAsync(TestConstants.DefaultAuthorizationCode, TestConstants.DefaultRedirectUri, clientAssertion, TestConstants.DefaultResource, true).ConfigureAwait(false);
            Assert.IsNotNull(result.AccessToken);

            AdalHttpMessageHandlerFactory.AddMockHandler(X5CMockHandler);
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, clientAssertion, new UserAssertion("Access Token"), true).ConfigureAwait(false);
            Assert.IsNotNull(result.AccessToken);

            //Check for empty x5c claim
            AdalHttpMessageHandlerFactory.AddMockHandler(EmptyX5CMockHandler);
            context.TokenCache.Clear();
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, clientAssertion, false).ConfigureAwait(false);
            Assert.IsNotNull(result.AccessToken);

            AdalHttpMessageHandlerFactory.AddMockHandler(EmptyX5CMockHandler);
            result = await context.AcquireTokenByAuthorizationCodeAsync(TestConstants.DefaultAuthorizationCode, TestConstants.DefaultRedirectUri, clientAssertion, TestConstants.DefaultResource, false).ConfigureAwait(false);
            Assert.IsNotNull(result.AccessToken);

            AdalHttpMessageHandlerFactory.AddMockHandler(EmptyX5CMockHandler);
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, clientAssertion, new UserAssertion("Access Token"), false).ConfigureAwait(false);
            Assert.IsNotNull(result.AccessToken);
        }

        [TestMethod]
        [Description("Test for default client assertion without X509 public certificate claim")]
        public async Task JsonWebTokenDefaultX509PublicCertClaimTestAsync()
        {
            var certificate = new X509Certificate2("valid_cert.pfx", TestConstants.DefaultPassword);
            var clientAssertion = new ClientAssertionCertificate(TestConstants.DefaultClientId, certificate);
            var context = new AuthenticationContext(TestConstants.TenantSpecificAuthority, new TokenCache());

            AdalHttpMessageHandlerFactory.AddMockHandler(EmptyX5CMockHandler);
            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, clientAssertion).ConfigureAwait(false);
            Assert.IsNotNull(result.AccessToken);

            AdalHttpMessageHandlerFactory.AddMockHandler(EmptyX5CMockHandler);
            result = await context.AcquireTokenByAuthorizationCodeAsync(TestConstants.DefaultAuthorizationCode, TestConstants.DefaultRedirectUri, clientAssertion, TestConstants.DefaultResource).ConfigureAwait(false);
            Assert.IsNotNull(result.AccessToken);

            AdalHttpMessageHandlerFactory.AddMockHandler(EmptyX5CMockHandler);
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, clientAssertion, new UserAssertion("Access Token")).ConfigureAwait(false);
            Assert.IsNotNull(result.AccessToken);
        }

        [TestMethod]
        [Description("Test for client assertion with developer implemented client assertion")]
        public async Task JsonWebTokenWithDeveloperImplementedClientAssertionTestAsync()
        {
            var certificate = new X509Certificate2("valid_cert.pfx", TestConstants.DefaultPassword);
            var clientAssertion = new ClientAssertionTestImplementation();
            var context = new AuthenticationContext(TestConstants.TenantSpecificAuthority, new TokenCache());

            AdalHttpMessageHandlerFactory.AddMockHandler(EmptyX5CMockHandler);
            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, clientAssertion, true).ConfigureAwait(false);
            Assert.IsNotNull(result.AccessToken);

            AdalHttpMessageHandlerFactory.AddMockHandler(EmptyX5CMockHandler);
            result = await context.AcquireTokenByAuthorizationCodeAsync(TestConstants.DefaultAuthorizationCode, TestConstants.DefaultRedirectUri, clientAssertion, TestConstants.DefaultResource, true).ConfigureAwait(false);
            Assert.IsNotNull(result.AccessToken);

            AdalHttpMessageHandlerFactory.AddMockHandler(EmptyX5CMockHandler);
            result = await context.AcquireTokenAsync(TestConstants.DefaultResource, clientAssertion, new UserAssertion("Access Token"), true).ConfigureAwait(false);
            Assert.IsNotNull(result.AccessToken);
        }
    }
}