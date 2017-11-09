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
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Helpers;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Http;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Test.ADAL.Common;
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

        [TestInitialize]
        public void Initialize()
        {
            HttpMessageHandlerFactory.ClearMockHandlers();
            InstanceDiscovery.InstanceCache.Clear();
            HttpMessageHandlerFactory.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler(TestConstants.GetDiscoveryEndpoint(TestConstants.DefaultAuthorityCommonTenant)));
            platformParameters = new PlatformParameters(PromptBehavior.Auto);
        }

        [TestMethod]
        [Description("Test for Json Web Token with client assertion and a X509 public certificate claim")]
        public async Task JsonWebTokenWithX509PublicCertClaimTest()
        {
            var certificate = new X509Certificate2("valid_cert.pfx", TestConstants.DefaultPassword);
            var clientAssertion = new ClientAssertionCertificate(TestConstants.DefaultClientId, certificate);
            var context = new AuthenticationContext(TestConstants.TenantSpecificAuthority, new TokenCache());

            var validCertClaim = "\"x5c\":\"" + Convert.ToBase64String(certificate.GetRawCertData());

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(TestConstants.GetTokenEndpoint(TestConstants.TenantSpecificAuthority))
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"some-access-token\"}")
                },
                AdditionalRequestValidation = request =>
                {
                    var requestContent = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var formsData = EncodingHelper.ParseKeyValueList(requestContent, '&', true, null);

                    // Check presence of client_assertion in request
                    string encodedJwt;
                    Assert.IsTrue(formsData.TryGetValue("client_assertion", out encodedJwt), "Missing client_assertion from request");

                    // Check presence of x5c cert claim. It should not exist.
                    var jwtHeader = EncodingHelper.UrlDecode(encodedJwt.Split('.')[0]);
                    Assert.IsTrue(!jwtHeader.Contains("\"x5c\":"));
                }
            });

            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, clientAssertion);
            Assert.IsNotNull(result.AccessToken);
        }

        [TestMethod]
        [Description("Test for Json Web Token with developer implemented client assertion")]
        public async Task JsonWebTokenWithDeveloperImplementedClientAssertionTest()
        {
            var certificate = new X509Certificate2("valid_cert.pfx", TestConstants.DefaultPassword);
            var clientAssertion = new ClientAssertionTestImplementation();
            var context = new AuthenticationContext(TestConstants.TenantSpecificAuthority, new TokenCache());

            var validCertClaim = "\"x5c\":\"" + Convert.ToBase64String(certificate.GetRawCertData());

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(TestConstants.GetTokenEndpoint(TestConstants.TenantSpecificAuthority))
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"some-access-token\"}")
                },
                AdditionalRequestValidation = request =>
                {
                    var requestContent = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var formsData = EncodingHelper.ParseKeyValueList(requestContent, '&', true, null);

                    // Check presence of client_assertion in request
                    string encodedJwt;
                    Assert.IsTrue(formsData.TryGetValue("client_assertion", out encodedJwt), "Missing client_assertion from request");

                    // Check presence of x5c cert claim. It should not exist.
                    var jwtHeader = EncodingHelper.UrlDecode(encodedJwt.Split('.')[0]);
                    Assert.IsTrue(!jwtHeader.Contains("\"x5c\":"));
                }
            });

            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, clientAssertion);
            Assert.IsNotNull(result.AccessToken);
        }
    }


    [DeploymentItem("valid_cert.pfx")]
    class ClientAssertionTestImplementation : IClientAssertionCertificate
    {

        public string ClientId { get { return TestConstants.DefaultClientId; } }

        public string Thumbprint { get { return TestConstants.DefaultThumbprint; } }

        public byte[] Sign(string message)
        {
            CryptographyHelper helper = new CryptographyHelper();
            return helper.SignWithCertificate(message, this.Certificate);
        }

        public X509Certificate2 Certificate { get; }

        public ClientAssertionTestImplementation()
        {
            this.Certificate = new X509Certificate2("valid_cert.pfx", TestConstants.DefaultPassword);
        }
    }
}

