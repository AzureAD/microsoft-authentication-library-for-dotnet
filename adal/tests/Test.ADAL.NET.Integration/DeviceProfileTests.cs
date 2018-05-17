//------------------------------------------------------------------------------
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
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.ADAL.Common;
using Test.ADAL.NET.Common;
using Test.ADAL.NET.Common.Mocks;

namespace Test.ADAL.NET.Integration
{
    [TestClass]
    public class DeviceCodeFlowTests
    {
        [TestInitialize]
        public void Initialize()
        {
            HttpMessageHandlerFactory.InitializeMockProvider();
            InstanceDiscovery.InstanceCache.Clear();
            HttpMessageHandlerFactory.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler(TestConstants.GetDiscoveryEndpoint(TestConstants.DefaultAuthorityCommonTenant)));
        }

        [TestMethod]
        public async Task PositiveTest()
        {
            DeviceCodeResult dcr = new DeviceCodeResult()
            {
                ClientId = TestConstants.DefaultClientId,
                Resource = TestConstants.DefaultResource,
                DeviceCode = "device-code",
                ExpiresOn = (DateTimeOffset.UtcNow + TimeSpan.FromMinutes(10)),
                Interval = 5,
                Message = "get token here",
                UserCode = "user-code",
                VerificationUrl = "https://login.microsoftonline.com/home.oauth2/token"
            };

            MockHttpMessageHandler mockMessageHandler = new MockHttpMessageHandler(TestConstants.DefaultAuthorityHomeTenant)
            {
                Method = HttpMethod.Post,
                Url = "https://login.microsoftonline.com/home/oauth2/token",
                ResponseMessage = MockHelpers.CreateFailureResponseMessage("{\"error\":\"authorization_pending\"," +
                                                                           "\"error_description\":\"AADSTS70016: Pending end-user authorization." +
                                                                           "\\r\\nTrace ID: f6c2c73f-a21d-474e-a71f-d8b121a58205\\r\\nCorrelation ID: " +
                                                                           "36fe3e82-442f-4418-b9f4-9f4b9295831d\\r\\nTimestamp: 2015-09-24 19:51:51Z\"," +
                                                                           "\"error_codes\":[70016],\"timestamp\":\"2015-09-24 19:51:51Z\",\"trace_id\":" +
                                                                           "\"f6c2c73f-a21d-474e-a71f-d8b121a58205\",\"correlation_id\":" +
                                                                           "\"36fe3e82-442f-4418-b9f4-9f4b9295831d\"}")
            };

            HttpMessageHandlerFactory.AddMockHandler(mockMessageHandler);
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(TestConstants.DefaultAuthorityHomeTenant)
            {
                Method = HttpMethod.Post,
                Url = "https://login.microsoftonline.com/home/oauth2/token",
                ResponseMessage =
                    MockHelpers.CreateSuccessTokenResponseMessage(TestConstants.DefaultUniqueId,
                        TestConstants.DefaultDisplayableId, TestConstants.DefaultResource)
            });

            TokenCache cache = new TokenCache();
            AuthenticationContext ctx = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, cache);
            AuthenticationResult result = await ctx.AcquireTokenByDeviceCodeAsync(dcr);
            Assert.IsNotNull(result);
            Assert.AreEqual("some-access-token", result.AccessToken);

            Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount());
        }

        [TestMethod]
        public async Task FullCoveragePositiveTest()
        {

            MockHttpMessageHandler mockMessageHandler = new MockHttpMessageHandler(TestConstants.DefaultAuthorityHomeTenant)
            {
                Method = HttpMethod.Get,
                Url = TestConstants.DefaultAuthorityHomeTenant + "oauth2/devicecode",
                ResponseMessage = MockHelpers.CreateSuccessDeviceCodeResponseMessage()
            };

            HttpMessageHandlerFactory.AddMockHandler(mockMessageHandler);

            mockMessageHandler = new MockHttpMessageHandler(TestConstants.DefaultAuthorityHomeTenant)
            {
                Method = HttpMethod.Post,
                Url = TestConstants.DefaultAuthorityHomeTenant + "oauth2/token",
                ResponseMessage = MockHelpers.CreateFailureResponseMessage("{\"error\":\"authorization_pending\"," +
                                                               "\"error_description\":\"AADSTS70016: Pending end-user authorization." +
                                                               "\\r\\nTrace ID: f6c2c73f-a21d-474e-a71f-d8b121a58205\\r\\nCorrelation ID: " +
                                                               "36fe3e82-442f-4418-b9f4-9f4b9295831d\\r\\nTimestamp: 2015-09-24 19:51:51Z\"," +
                                                               "\"error_codes\":[70016],\"timestamp\":\"2015-09-24 19:51:51Z\",\"trace_id\":" +
                                                               "\"f6c2c73f-a21d-474e-a71f-d8b121a58205\",\"correlation_id\":" +
                                                               "\"36fe3e82-442f-4418-b9f4-9f4b9295831d\"}")
            };

            HttpMessageHandlerFactory.AddMockHandler(mockMessageHandler);
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(TestConstants.DefaultAuthorityHomeTenant)
            {
                Method = HttpMethod.Post,
                Url = TestConstants.DefaultAuthorityHomeTenant + "oauth2/token",
                ResponseMessage =
                    MockHelpers.CreateSuccessTokenResponseMessage(TestConstants.DefaultUniqueId,
                        TestConstants.DefaultDisplayableId, TestConstants.DefaultResource)
            });

            TokenCache cache = new TokenCache();
            AuthenticationContext ctx = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, cache);
            DeviceCodeResult dcr = await ctx.AcquireDeviceCodeAsync("some-resource", "some-client");

            Assert.IsNotNull(dcr);
            Assert.AreEqual("some-device-code", dcr.DeviceCode);
            Assert.AreEqual("some-user-code", dcr.UserCode);
            Assert.AreEqual("some-URL", dcr.VerificationUrl);
            Assert.AreEqual(5, dcr.Interval);
            Assert.AreEqual("some-message", dcr.Message);
            Assert.AreEqual("some-client", dcr.ClientId);

            AuthenticationResult result = await ctx.AcquireTokenByDeviceCodeAsync(dcr);
            Assert.IsNotNull(result);
            Assert.AreEqual("some-access-token", result.AccessToken);
            // There should be one cached entry.
            Assert.AreEqual(1, ctx.TokenCache.Count);

            Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount());
        }

        [TestMethod]
        public void NegativeDeviceCodeTest()
        {
            MockHttpMessageHandler mockMessageHandler = new MockHttpMessageHandler(TestConstants.DefaultAuthorityHomeTenant)
            {
                Method = HttpMethod.Get,
                Url = TestConstants.DefaultAuthorityHomeTenant + "oauth2/devicecode",
                ResponseMessage = MockHelpers.CreateDeviceCodeErrorResponse()
            };

            HttpMessageHandlerFactory.AddMockHandler(mockMessageHandler);

            TokenCache cache = new TokenCache();
            AuthenticationContext ctx = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, cache);
            DeviceCodeResult dcr;
            AdalServiceException ex = AssertException.TaskThrows<AdalServiceException>(async () => dcr = await ctx.AcquireDeviceCodeAsync("some-resource", "some-client"));
            Assert.IsTrue(ex.Message.Contains("some error message"));

            Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount());
        }

        [TestMethod]
        public async Task NegativeDeviceCodeTimeoutTest()
        {
            MockHttpMessageHandler mockMessageHandler = new MockHttpMessageHandler(TestConstants.DefaultAuthorityHomeTenant)
            {
                Method = HttpMethod.Get,
                Url = TestConstants.DefaultAuthorityHomeTenant + "oauth2/devicecode",
                ResponseMessage = MockHelpers.CreateSuccessDeviceCodeResponseMessage("1")
            };

            HttpMessageHandlerFactory.AddMockHandler(mockMessageHandler);

            mockMessageHandler = new MockHttpMessageHandler(TestConstants.DefaultAuthorityHomeTenant)
            {
                Method = HttpMethod.Post,
                Url = TestConstants.DefaultAuthorityHomeTenant + "oauth2/token",
                ResponseMessage = MockHelpers.CreateFailureResponseMessage("{\"error\":\"authorization_pending\"," +
                                                               "\"error_description\":\"AADSTS70016: Pending end-user authorization." +
                                                               "\\r\\nTrace ID: f6c2c73f-a21d-474e-a71f-d8b121a58205\\r\\nCorrelation ID: " +
                                                               "36fe3e82-442f-4418-b9f4-9f4b9295831d\\r\\nTimestamp: 2015-09-24 19:51:51Z\"," +
                                                               "\"error_codes\":[70016],\"timestamp\":\"2015-09-24 19:51:51Z\",\"trace_id\":" +
                                                               "\"f6c2c73f-a21d-474e-a71f-d8b121a58205\",\"correlation_id\":" +
                                                               "\"36fe3e82-442f-4418-b9f4-9f4b9295831d\"}")
            };

            HttpMessageHandlerFactory.AddMockHandler(mockMessageHandler);

            mockMessageHandler = new MockHttpMessageHandler(TestConstants.DefaultAuthorityHomeTenant)
            {
                Method = HttpMethod.Post,
                Url = TestConstants.DefaultAuthorityHomeTenant + "oauth2/token",
                ResponseMessage = MockHelpers.CreateDeviceCodeExpirationErrorResponse()
            };
            HttpMessageHandlerFactory.AddMockHandler(mockMessageHandler);

            TokenCache cache = new TokenCache();
            AuthenticationContext ctx = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, cache);
            DeviceCodeResult dcr = await ctx.AcquireDeviceCodeAsync("some resource", "some authority");

            Assert.IsNotNull(dcr);
            AuthenticationResult result;
            AdalServiceException ex = AssertException.TaskThrows<AdalServiceException>(async () => result = await ctx.AcquireTokenByDeviceCodeAsync(dcr));
            Assert.IsTrue(ex.Message.Contains("Verification code expired"));

            Assert.AreEqual(0, HttpMessageHandlerFactory.MockHandlersCount());
        }
    }
}
