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
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;
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
        private AuthenticationContext context;

        [TestInitialize]
        public void Initialize()
        {
            AdalHttpMessageHandlerFactory.InitializeMockProvider();
            InstanceDiscovery.InstanceCache.Clear();
            AdalHttpMessageHandlerFactory.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler(TestConstants.GetDiscoveryEndpoint(TestConstants.DefaultAuthorityCommonTenant)));
        }

        [TestCleanup()]
        public void Cleanup()
        {
            if (context != null && context.TokenCache != null)
            {
                context.TokenCache.Clear();
            }
        }

        [TestMethod]
        public async Task PositiveTestAsync()
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

            AdalHttpMessageHandlerFactory.AddMockHandler(mockMessageHandler);
            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(TestConstants.DefaultAuthorityHomeTenant)
            {
                Method = HttpMethod.Post,
                Url = "https://login.microsoftonline.com/home/oauth2/token",
                ResponseMessage =
                    MockHelpers.CreateSuccessTokenResponseMessage(TestConstants.DefaultUniqueId,
                        TestConstants.DefaultDisplayableId, TestConstants.DefaultResource)
            });

            TokenCache cache = new TokenCache();
            context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, cache);
            AuthenticationResult result = await context.AcquireTokenByDeviceCodeAsync(dcr);
            Assert.IsNotNull(result);
            Assert.AreEqual("some-access-token", result.AccessToken);

            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
        }

        [TestMethod]
        public async Task FullCoveragePositiveTestAsync()
        {

            MockHttpMessageHandler mockMessageHandler = new MockHttpMessageHandler(TestConstants.DefaultAuthorityHomeTenant)
            {
                Method = HttpMethod.Get,
                Url = TestConstants.DefaultAuthorityHomeTenant + "oauth2/devicecode",
                ResponseMessage = MockHelpers.CreateSuccessDeviceCodeResponseMessage()
            };

            AdalHttpMessageHandlerFactory.AddMockHandler(mockMessageHandler);

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

            AdalHttpMessageHandlerFactory.AddMockHandler(mockMessageHandler);
            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(TestConstants.DefaultAuthorityHomeTenant)
            {
                Method = HttpMethod.Post,
                Url = TestConstants.DefaultAuthorityHomeTenant + "oauth2/token",
                ResponseMessage =
                    MockHelpers.CreateSuccessTokenResponseMessage(TestConstants.DefaultUniqueId,
                        TestConstants.DefaultDisplayableId, TestConstants.DefaultResource)
            });

            TokenCache cache = new TokenCache();
            context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, cache);
            DeviceCodeResult dcr = await context.AcquireDeviceCodeAsync("some-resource", "some-client");

            Assert.IsNotNull(dcr);
            Assert.AreEqual("some-device-code", dcr.DeviceCode);
            Assert.AreEqual("some-user-code", dcr.UserCode);
            Assert.AreEqual("some-URL", dcr.VerificationUrl);
            Assert.AreEqual(5, dcr.Interval);
            Assert.AreEqual("some-message", dcr.Message);
            Assert.AreEqual("some-client", dcr.ClientId);

            AuthenticationResult result = await context.AcquireTokenByDeviceCodeAsync(dcr);
            Assert.IsNotNull(result);
            Assert.AreEqual("some-access-token", result.AccessToken);
            // There should be one cached entry.
            Assert.AreEqual(1, context.TokenCache.Count);

            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
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

            AdalHttpMessageHandlerFactory.AddMockHandler(mockMessageHandler);

            TokenCache cache = new TokenCache();
            context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, cache);
            DeviceCodeResult dcr;
            AdalServiceException ex = AssertException.TaskThrows<AdalServiceException>(async () => dcr = await context.AcquireDeviceCodeAsync("some-resource", "some-client"));
            Assert.IsTrue(ex.Message.Contains("some error message"));

            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
        }

        [TestMethod]
        public async Task NegativeDeviceCodeTimeoutTestAsync()
        {
            MockHttpMessageHandler mockMessageHandler = new MockHttpMessageHandler(TestConstants.DefaultAuthorityHomeTenant)
            {
                Method = HttpMethod.Get,
                Url = TestConstants.DefaultAuthorityHomeTenant + "oauth2/devicecode",
                ResponseMessage = MockHelpers.CreateSuccessDeviceCodeResponseMessage(
                    // do not lower this to 1-2s as test execution may be slow and the flow 
                    // will never call the server
                    expirationTimeInSeconds: 30, 
                    retryInternvalInSeconds: 2)
            };

            AdalHttpMessageHandlerFactory.AddMockHandler(mockMessageHandler);

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

            AdalHttpMessageHandlerFactory.AddMockHandler(mockMessageHandler);

            mockMessageHandler = new MockHttpMessageHandler(TestConstants.DefaultAuthorityHomeTenant)
            {
                Method = HttpMethod.Post,
                Url = TestConstants.DefaultAuthorityHomeTenant + "oauth2/token",
                ResponseMessage = MockHelpers.CreateDeviceCodeExpirationErrorResponse()
            };
            AdalHttpMessageHandlerFactory.AddMockHandler(mockMessageHandler);

            TokenCache cache = new TokenCache();
            context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, cache);
            DeviceCodeResult dcr = await context.AcquireDeviceCodeAsync("some resource", "some authority");

            Assert.IsNotNull(dcr);
            AuthenticationResult result;
            AdalServiceException ex = AssertException.TaskThrows<AdalServiceException>(async () => result = await context.AcquireTokenByDeviceCodeAsync(dcr));
            Assert.AreEqual(AdalErrorEx.DeviceCodeAuthorizationCodeExpired, ex.ErrorCode);

            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
        }

        [TestMethod]
        public async Task NegativeDeviceCodeTimeoutTest_WithZeroRetries()
        {
            MockHttpMessageHandler mockMessageHandler = new MockHttpMessageHandler(TestConstants.DefaultAuthorityHomeTenant)
            {
                Method = HttpMethod.Get,
                Url = TestConstants.DefaultAuthorityHomeTenant + "oauth2/devicecode",
                ResponseMessage = MockHelpers.CreateSuccessDeviceCodeResponseMessage(
                    expirationTimeInSeconds: 0,
                    retryInternvalInSeconds: 1)
            };

            AdalHttpMessageHandlerFactory.AddMockHandler(mockMessageHandler);

            TokenCache cache = new TokenCache();
            context = new AuthenticationContext(TestConstants.DefaultAuthorityHomeTenant, cache);
            DeviceCodeResult dcr = await context.AcquireDeviceCodeAsync("some resource", "some authority");

            Assert.IsNotNull(dcr);
            AuthenticationResult result;
            AdalServiceException ex = AssertException.TaskThrows<AdalServiceException>(async () => result = await context.AcquireTokenByDeviceCodeAsync(dcr));
            Assert.AreEqual(AdalErrorEx.DeviceCodeAuthorizationCodeExpired, ex.ErrorCode);

            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount());
        }
    }
}
