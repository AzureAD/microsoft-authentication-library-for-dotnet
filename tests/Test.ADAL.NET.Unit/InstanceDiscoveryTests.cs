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
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.ADAL.NET.Unit.Mocks;
using System.Net;

namespace Test.ADAL.NET.Unit
{
    /// <summary>
    /// This class tests the instance discovery behaviors.
    /// </summary>
    [TestClass]
    public class InstanceDiscoveryTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            HttpMessageHandlerFactory.ClearMockHandlers();
            InstanceDiscovery.InstanceCache.Clear();
        }

        [TestMethod]
        [TestCategory("InstanceDiscoveryTests")]
        public async Task TestInstanceDiscovery_WhenAuthorityIsInvalidButValidationIsNotRequired_ShouldCacheTheProvidedAuthority()
        {
            for (int i = 0; i < 2; i++) // Prepare 2 mock responses
            {
                HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
                {
                    Method = HttpMethod.Get,
                    Url = $"https://{InstanceDiscovery.DefaultTrustedAuthority}/common/discovery/instance",
                    ResponseMessage = MockHelpers.CreateFailureResponseMessage("{\"error\":\"invalid_instance\"}")
                });
            }

            CallState callState = new CallState(Guid.NewGuid());
            string host = "invalid_instance.example.com";

            // ADAL still behaves correctly using developer provided authority
            var entry = await InstanceDiscovery.GetMetadataEntry(host, false, callState).ConfigureAwait(false);
            Assert.AreEqual(host, entry.PreferredNetwork); // No exception raised, the host is returned as-is
            Assert.AreEqual(1, HttpMessageHandlerFactory.MockHandlersCount()); // 1 mock response is consumed, 1 remaining

            // Subsequent requests do not result in further authority validation network requests for the process lifetime
            var entry2 = await InstanceDiscovery.GetMetadataEntry(host, false, callState).ConfigureAwait(false);
            Assert.AreEqual(host, entry2.PreferredNetwork);
            Assert.AreEqual(1, HttpMessageHandlerFactory.MockHandlersCount()); // Still 1 mock response remaining, so no new request was attempted
        }

        [TestMethod]
        [TestCategory("InstanceDiscoveryTests")]
        public async Task TestInstanceDiscovery_WhenAuthorityIsValidButNoMetadataIsReturned_ShouldCacheTheProvidedAuthority()
        {
            for (int i = 0; i < 2; i++) // Prepare 2 mock responses
            {
                HttpMessageHandlerFactory.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler());
            }

            CallState callState = new CallState(Guid.NewGuid());
            string host = "login.windows.net";

            // ADAL still behaves correctly using developer provided authority
            var entry = await InstanceDiscovery.GetMetadataEntry(host, true, callState).ConfigureAwait(false);
            Assert.AreEqual(host, entry.PreferredNetwork); // No exception raised, the host is returned as-is
            Assert.AreEqual(1, HttpMessageHandlerFactory.MockHandlersCount()); // 1 mock response is consumed, 1 remaining

            // Subsequent requests do not result in further authority validation network requests for the process lifetime
            var entry2 = await InstanceDiscovery.GetMetadataEntry(host, true, callState).ConfigureAwait(false);
            Assert.AreEqual(host, entry2.PreferredNetwork);
            Assert.AreEqual(1, HttpMessageHandlerFactory.MockHandlersCount()); // Still 1 mock response remaining, so no new request was attempted
        }

        [TestMethod]
        [TestCategory("InstanceDiscoveryTests")]
        public async Task TestInstanceDiscovery_WhenMultipleSimultaneousCallsWithTheSameAuthority_ShouldMakeOnlyOneRequest()
        {
            for (int i = 0; i < 2; i++) // Prepare 2 mock responses
            {
                HttpMessageHandlerFactory.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler());
            }

            CallState callState = new CallState(Guid.NewGuid());
            string host = "login.windows.net";
            Task.WaitAll( // Simulate several simultaneous calls
                InstanceDiscovery.GetMetadataEntry(host, true, callState),
                InstanceDiscovery.GetMetadataEntry(host, true, callState));
            Assert.AreEqual(1, HttpMessageHandlerFactory.MockHandlersCount()); // 1 mock response is consumed, 1 remaining
        }

        [TestMethod]
        [TestCategory("InstanceDiscoveryTests")]
        public async Task TestInstanceDiscovery_WhenAuthorityIsValidAndMetadataIsReturned_ShouldCacheAllReturnedAliases()
        {
            string host = "login.windows.net";
            for (int i = 0; i < 2; i++) // Prepare 2 mock responses
            {
                HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler{
                    Method = HttpMethod.Get,
                    Url = $"https://{host}/common/discovery/instance",
                    ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            @"{
                            ""tenant_discovery_endpoint"" : ""https://login.microsoftonline.com/v1/.well-known/openid-configuration"",
                            ""metadata"": [
                                {
                                ""preferred_network"": ""login.microsoftonline.com"",
                                ""preferred_cache"": ""login.windows.net"",
                                ""aliases"": [""login.microsoftonline.com"", ""login.windows.net"", ""sts.microsoft.com""]
                                }
                            ]
                            }"
                        )
                    }
                });
            }

            CallState callState = new CallState(Guid.NewGuid());
            // ADAL still behaves correctly using developer provided authority
            var entry = await InstanceDiscovery.GetMetadataEntry(host, true, callState).ConfigureAwait(false);
            Assert.AreEqual("login.microsoftonline.com", entry.PreferredNetwork); // No exception raised, the host is returned as-is
            Assert.AreEqual(1, HttpMessageHandlerFactory.MockHandlersCount()); // 1 mock response is consumed, 1 remaining

            // Subsequent requests do not result in further authority validation network requests for the process lifetime
            var entry2 = await InstanceDiscovery.GetMetadataEntry("sts.microsoft.com", true, callState).ConfigureAwait(false);
            Assert.AreEqual("login.microsoftonline.com", entry2.PreferredNetwork);
            Assert.AreEqual(1, HttpMessageHandlerFactory.MockHandlersCount()); // Still 1 mock response remaining, so no new request was attempted
        }
    }
}
