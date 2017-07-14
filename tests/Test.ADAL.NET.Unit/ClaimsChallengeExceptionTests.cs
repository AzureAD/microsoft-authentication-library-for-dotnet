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
using Microsoft.IdentityModel.Clients.ActiveDirectory.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Test.ADAL.Common;
using Test.ADAL.NET.Unit.Mocks;
using AuthenticationContext = Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext;

namespace Test.ADAL.NET.Unit
{
    [TestClass]
    public class ClaimsChallengeExceptionTests
    {
        private PlatformParameters platformParameters;

        public const string Claims = "{\"access_token\":{\"polids\":{\"essential\":true,\"values\":[\"5ce770ea-8690-4747-aa73-c5b3cd509cd4\"]}}}";

        [TestInitialize]
        public void Initialize()
        {
            HttpMessageHandlerFactory.ClearMockHandlers();
            platformParameters = new PlatformParameters(PromptBehavior.Auto);
        }

        [TestMethod]
        [Description("Test for claims challenge exception with client credential")]
        public void AdalClaimsChallengeExceptionThrownWithAcquireTokenClientCredentialWhenClaimsChallengeRequiredTestAsync()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            var credential = new ClientCredential(TestConstants.DefaultClientId, TestConstants.DefaultClientSecret);
            string claims = "{\"error\":\"interaction_required\",\"claims\":\"{\\\"access_token\\\":{\\\"polids\\\":{\\\"essential\\\":true,\\\"values\\\":[\\\"5ce770ea-8690-4747-aa73-c5b3cd509cd4\\\"]}}}\"}";

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(claims)
                }
            });

            var result = AssertException.TaskThrows<AdalClaimChallengeException>(() =>
            context.AcquireTokenAsync(TestConstants.DefaultResource, credential));
            Assert.AreEqual(result.Claims, Claims);
        }

        [TestMethod]
        [Description("Test for claims challenge exception with client credential and user assertion")]
        public void AdalClaimsChallengeExceptionThrownWithAcquireTokenClientCredentialUserAssertionWhenClaimsChallengeRequiredTestAsync()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            var credential = new ClientCredential(TestConstants.DefaultClientId, TestConstants.DefaultClientSecret);
            string accessToken = "some-access-token";
            string claims = "{\"error\":\"interaction_required\",\"claims\":\"{\\\"access_token\\\":{\\\"polids\\\":{\\\"essential\\\":true,\\\"values\\\":[\\\"5ce770ea-8690-4747-aa73-c5b3cd509cd4\\\"]}}}\"}";

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(claims)
                }
            });

            var result = AssertException.TaskThrows<AdalClaimChallengeException>(() =>
            context.AcquireTokenAsync(TestConstants.DefaultResource, credential, new UserAssertion(accessToken)));
            Assert.AreEqual(result.Claims, Claims);
        }

        [TestMethod]
        [Description("Test for claims challenge exception with acquire token")]
        public void AdalClaimsChallengeExceptionThrownWithAcquireTokenWhenClaimsChallengeRequiredTestAsync()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            string claims = "{\"error\":\"interaction_required\",\"claims\":\"{\\\"access_token\\\":{\\\"polids\\\":{\\\"essential\\\":true,\\\"values\\\":[\\\"5ce770ea-8690-4747-aa73-c5b3cd509cd4\\\"]}}}\"}";

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(claims)
                }
            });

            var result = AssertException.TaskThrows<AdalClaimChallengeException>(() =>
            context.AcquireTokenAsync(TestConstants.DefaultResource, TestConstants.DefaultClientId, TestConstants.DefaultRedirectUri, platformParameters));
            Assert.AreEqual(result.Claims, Claims);
        }

        [TestMethod]
        [Description("Test for claims challenge exception with client assertion")]
        public void AdalClaimsChallengeExceptionThrownWithClientAssertionWhenClaimsChallengeRequiredTestAsync()
        {
            var certificate = new X509Certificate2("valid_cert.pfx", TestConstants.DefaultPassword);
            var clientAssertion = new ClientAssertionCertificate(TestConstants.DefaultClientId, certificate);
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            string claims = "{\"error\":\"interaction_required\",\"claims\":\"{\\\"access_token\\\":{\\\"polids\\\":{\\\"essential\\\":true,\\\"values\\\":[\\\"5ce770ea-8690-4747-aa73-c5b3cd509cd4\\\"]}}}\"}";

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(claims)
                }
            });

            var result = AssertException.TaskThrows<AdalClaimChallengeException>(() =>
            context.AcquireTokenAsync(TestConstants.DefaultResource, clientAssertion));
            Assert.AreEqual(result.Claims, Claims);
        }

        [TestMethod]
        [Description("Test for claims challenge exception with auth code")]
        public void AdalClaimsChallengeExceptionThrownWithAuthCodeWhenClaimsChallengeRequiredTestAsync()
        {
            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            ClientAssertion clientAssertion = new ClientAssertion(TestConstants.DefaultClientId, "some-assertion");

            string claims = "{\"error\":\"interaction_required\",\"claims\":\"{\\\"access_token\\\":{\\\"polids\\\":{\\\"essential\\\":true,\\\"values\\\":[\\\"5ce770ea-8690-4747-aa73-c5b3cd509cd4\\\"]}}}\"}";

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(claims)
                }
            });

            var result = AssertException.TaskThrows<AdalClaimChallengeException>(() =>
            context.AcquireTokenByAuthorizationCodeAsync("some-code", TestConstants.DefaultRedirectUri, clientAssertion, TestConstants.DefaultResource));
            Assert.AreEqual(result.Claims, Claims);
        }

        [TestMethod]
        [Description("Process claims when claims are passed as a string in method overload")]
        public void ClaimsPassedInAsStringPositiveTestAsync()
        {
            string processedClaims = AcquireTokenInteractiveHandler.ProcessClaims(null, Claims);
            Assert.AreEqual(processedClaims, Claims);
        }

        [TestMethod]
        [Description("Process claims when claims are passed in extra query parameters")]
        public void ClaimsPassedInAsExtraQueryParametersPositiveTestAsync()
        {
            string processedClaims = AcquireTokenInteractiveHandler.ProcessClaims("&claims=" + Claims, null);
            Assert.AreEqual(processedClaims, Claims);
        }

        [TestMethod]
        [Description("Process claims when claims are passed in extra query parameters and string in method overload")]
        public void ClaimsPassedInAsExtraQueryParametersAndStringOverloadPositiveTestAsync()
        {
            var ex = AssertException.Throws<ArgumentException>(() => AcquireTokenInteractiveHandler.ProcessClaims("&claims=" + Claims, Claims));
            Assert.AreEqual(ex.Message, "The claims parameter must be passed in either string claims or extra query parameters.");
        }
    }
}
