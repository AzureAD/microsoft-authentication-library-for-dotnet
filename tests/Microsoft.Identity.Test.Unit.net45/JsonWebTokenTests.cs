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


// Test should run on net core. Please re-enable once bug
// https://identitydivision.visualstudio.com/DevEx/_workitems/edit/574705 is fixed
#if !ANDROID && !iOS && !WINDOWS_APP && !NET_CORE
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit
{
    [TestClass]
    [DeploymentItem(@"Resources\valid_cert.pfx")]
    public class JsonWebTokenTests
    {
        private TokenCache _cache;

        private readonly MockHttpMessageHandler X5CMockHandler = new MockHttpMessageHandler()
        {
            Method = HttpMethod.Post,
            ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                MsalTestConstants.Scope.AsSingleString(),
                MockHelpers.CreateIdToken(MsalTestConstants.UniqueId, MsalTestConstants.DisplayableId),
                MockHelpers.CreateClientInfo(MsalTestConstants.Uid, MsalTestConstants.Utid + "more")),
            AdditionalRequestValidation = request =>
            {
                var requestContent = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var formsData = CoreHelpers.ParseKeyValueList(requestContent, '&', true, null);

                // Check presence of client_assertion in request
                Assert.IsTrue(formsData.TryGetValue("client_assertion", out string encodedJwt), "Missing client_assertion from request");

                // Check presence of x5c cert claim. It should exist.
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(encodedJwt);
                Assert.IsTrue(jsonToken.Header.Any(header => header.Key == "x5c"), "x5c should be present");
            }
        };

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetStateAndInitMsal();
            _cache = new TokenCache();
        }

        internal void SetupMocks(MockHttpManager httpManager)
        {
            httpManager.AddInstanceDiscoveryMockHandler();
            httpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityHomeTenant);
        }

        [TestMethod]
        [Description("Test for client assertion with X509 public certificate using sendCertificate")]
        public async Task JsonWebTokenWithX509PublicCertSendCertificateTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);

                SetupMocks(httpManager);
                var certificate = new X509Certificate2(
                    ResourceHelper.GetTestResourceRelativePath("valid_cert.pfx"),
                    MsalTestConstants.DefaultPassword);

                var clientAssertion = new ClientAssertionCertificate(certificate);
                var clientCredential = new ClientCredential(clientAssertion);
                var app = new ConfidentialClientApplication(
                    serviceBundle,
                    MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority,
                    MsalTestConstants.RedirectUri,
                    clientCredential,
                    _cache,
                    _cache)
                {
                    ValidateAuthority = false
                };

                //Check for x5c claim
                httpManager.AddMockHandler(X5CMockHandler);
                AuthenticationResult result =
                    await (app as IConfidentialClientApplicationWithCertificate).AcquireTokenForClientWithCertificateAsync(
                        MsalTestConstants.Scope).ConfigureAwait(false);
                Assert.IsNotNull(result.AccessToken);

                result = await app.AcquireTokenForClientAsync(MsalTestConstants.Scope).ConfigureAwait(false);
                Assert.IsNotNull(result.AccessToken);
            }
        }

        [TestMethod]
        [Description("Test for client assertion with X509 public certificate using sendCertificate")]
        public async Task JsonWebTokenWithX509PublicCertSendCertificateOnBehalfOfTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = ServiceBundle.CreateWithCustomHttpManager(httpManager);
                SetupMocks(httpManager);

                var certificate = new X509Certificate2(
                    ResourceHelper.GetTestResourceRelativePath("valid_cert.pfx"),
                    MsalTestConstants.DefaultPassword);
                var clientAssertion = new ClientAssertionCertificate(certificate);
                var clientCredential = new ClientCredential(clientAssertion);
                var app = new ConfidentialClientApplication(
                    serviceBundle,
                    MsalTestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority,
                    MsalTestConstants.RedirectUri,
                    clientCredential,
                    _cache,
                    _cache)
                {
                    ValidateAuthority = false
                };
                var userAssertion = new UserAssertion(MsalTestConstants.DefaultAccessToken);

                //Check for x5c claim
                httpManager.AddMockHandler(X5CMockHandler);
                AuthenticationResult result =
                    await (app as IConfidentialClientApplicationWithCertificate).AcquireTokenOnBehalfOfWithCertificateAsync(
                        MsalTestConstants.Scope,
                        userAssertion).ConfigureAwait(false);
                Assert.IsNotNull(result.AccessToken);

                result = await app.AcquireTokenOnBehalfOfAsync(MsalTestConstants.Scope, userAssertion).ConfigureAwait(false);
                Assert.IsNotNull(result.AccessToken);
            }
        }
    }
}
#endif