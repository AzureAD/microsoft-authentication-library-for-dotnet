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
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;
using Microsoft.Identity.Core.Http;
using Microsoft.Identity.Core.WsTrust;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Http;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Instance;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Flows;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.ClientCreds;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Cache;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Platform;
using Test.ADAL.NET.Common;
using Test.Microsoft.Identity.Core.Unit.Mocks;
using CoreHttpClientFactory = Microsoft.Identity.Core.Http.HttpClientFactory;
using MockHelpers = Test.ADAL.NET.Common.Mocks.MockHelpers;
using MockHttpMessageHandler = Test.ADAL.NET.Common.Mocks.MockHttpMessageHandler;

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
            ModuleInitializer.ForceModuleInitializationTestOnly();
            AdalHttpMessageHandlerFactory.InitializeMockProvider();
            InstanceDiscovery.InstanceCache.Clear();
        }

        [TestMethod]
        [TestCategory("InstanceDiscoveryTests")]
        public async Task TestInstanceDiscovery_WhenAuthorityIsInvalidButValidationIsNotRequired_ShouldCacheTheProvidedAuthorityAsync()
        {
            for (int i = 0; i < 2; i++) // Prepare 2 mock responses
            {
                AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(AdalTestConstants.DefaultAuthorityHomeTenant)
                {
                    Method = HttpMethod.Get,
                    Url = $"https://{InstanceDiscovery.DefaultTrustedAuthority}/common/discovery/instance",
                    ResponseMessage = MockHelpers.CreateFailureResponseMessage("{\"error\":\"invalid_instance\"}")
                });
            }

            RequestContext requestContext = new RequestContext(new AdalLogger(new Guid()));
            string host = "invalid_instance.example.com";

            // ADAL still behaves correctly using developer provided authority
            var entry = await InstanceDiscovery.GetMetadataEntryAsync(new Uri($"https://{host}/tenant"), false, requestContext).ConfigureAwait(false);
            Assert.AreEqual(host, entry.PreferredNetwork); // No exception raised, the host is returned as-is
            Assert.AreEqual(1, AdalHttpMessageHandlerFactory.MockHandlersCount()); // 1 mock response is consumed, 1 remaining

            // Subsequent requests do not result in further authority validation network requests for the process lifetime
            var entry2 = await InstanceDiscovery.GetMetadataEntryAsync(new Uri($"https://{host}/tenant"), false, requestContext).ConfigureAwait(false);
            Assert.AreEqual(host, entry2.PreferredNetwork);
            Assert.AreEqual(1, AdalHttpMessageHandlerFactory.MockHandlersCount()); // Still 1 mock response remaining, so no new request was attempted
        }

        [TestMethod]
        [TestCategory("InstanceDiscoveryTests")]
        public async Task TestInstanceDiscovery_WhenAuthorityIsValidButNoMetadataIsReturned_ShouldCacheTheProvidedAuthorityAsync()
        {
            string host = "login.windows.net";  // A whitelisted host
            RequestContext requestContext = new RequestContext(new AdalLogger(new Guid()));
            for (int i = 0; i < 2; i++) // Prepare 2 mock responses
            {
                AdalHttpMessageHandlerFactory.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler(
                    $"https://{host}/common/discovery/instance",
                    @"{""tenant_discovery_endpoint"":""https://login.microsoftonline.com/tenant/.well-known/openid-configuration""}"));
            }

            // ADAL still behaves correctly using developer provided authority
            var entry = await InstanceDiscovery.GetMetadataEntryAsync(new Uri($"https://{host}/tenant"), true, requestContext).ConfigureAwait(false);
            Assert.AreEqual(host, entry.PreferredNetwork); // No exception raised, the host is returned as-is
            Assert.AreEqual(1, AdalHttpMessageHandlerFactory.MockHandlersCount()); // 1 mock response is consumed, 1 remaining

            // Subsequent requests do not result in further authority validation network requests for the process lifetime
            var entry2 = await InstanceDiscovery.GetMetadataEntryAsync(new Uri($"https://{host}/tenant"), true, requestContext).ConfigureAwait(false);
            Assert.AreEqual(host, entry2.PreferredNetwork);
            Assert.AreEqual(1, AdalHttpMessageHandlerFactory.MockHandlersCount()); // Still 1 mock response remaining, so no new request was attempted
        }

        [TestMethod]
        [TestCategory("InstanceDiscoveryTests")]
        public void TestInstanceDiscovery_WhenMultipleSimultaneousCallsWithTheSameAuthority_ShouldMakeOnlyOneRequest()
        {
            for (int i = 0; i < 2; i++) // Prepare 2 mock responses
            {
                AdalHttpMessageHandlerFactory.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler("https://login.windows.net/common/discovery/instance"));
            }

            RequestContext requestContext = new RequestContext(new AdalLogger(new Guid()));
            string host = "login.windows.net";
            Task.WaitAll( // Simulate several simultaneous calls
                InstanceDiscovery.GetMetadataEntryAsync(new Uri($"https://{host}/tenant"), true, requestContext),
                InstanceDiscovery.GetMetadataEntryAsync(new Uri($"https://{host}/tenant"), true, requestContext));
            Assert.AreEqual(1, AdalHttpMessageHandlerFactory.MockHandlersCount()); // 1 mock response is consumed, 1 remaining
        }

        [TestMethod]
        [TestCategory("InstanceDiscoveryTests")]
        public async Task TestInstanceDiscovery_WhenAuthorityIsValidAndMetadataIsReturned_ShouldCacheAllReturnedAliasesAsync()
        {
            string host = "login.windows.net";
            for (int i = 0; i < 2; i++) // Prepare 2 mock responses
            {
                AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(AdalTestConstants.DefaultAuthorityHomeTenant)
                {
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

            RequestContext requestContext = new RequestContext(new AdalLogger(new Guid()));
            // ADAL still behaves correctly using developer provided authority
            var entry = await InstanceDiscovery.GetMetadataEntryAsync(new Uri($"https://{host}/tenant"), true, requestContext).ConfigureAwait(false);
            Assert.AreEqual("login.microsoftonline.com", entry.PreferredNetwork); // No exception raised, the host is returned as-is
            Assert.AreEqual(1, AdalHttpMessageHandlerFactory.MockHandlersCount()); // 1 mock response is consumed, 1 remaining

            // Subsequent requests do not result in further authority validation network requests for the process lifetime
            var entry2 = await InstanceDiscovery.GetMetadataEntryAsync(new Uri("https://sts.microsoft.com/tenant"), true, requestContext).ConfigureAwait(false);
            Assert.AreEqual("login.microsoftonline.com", entry2.PreferredNetwork);
            Assert.AreEqual(1, AdalHttpMessageHandlerFactory.MockHandlersCount()); // Still 1 mock response remaining, so no new request was attempted
        }

        [TestMethod]
        [TestCategory("InstanceDiscoveryTests")]
        public async Task TestInstanceDiscovery_WhenAuthorityIsAdfs_ShouldNotDoInstanceDiscoveryAsync()
        {
            AdalHttpMessageHandlerFactory.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler(AdalTestConstants.GetDiscoveryEndpoint(AdalTestConstants.DefaultAuthorityCommonTenant)));
            var authenticator = new Authenticator("https://login.contoso.com/adfs", false);
            await authenticator.UpdateFromTemplateAsync(new RequestContext(new AdalLogger(new Guid()))).ConfigureAwait(false);
            Assert.AreEqual(1, AdalHttpMessageHandlerFactory.MockHandlersCount()); // mock is NOT consumed, so no new request was NOT attempted
        }

        [TestMethod]
        [TestCategory("InstanceDiscoveryTests")]
        public async Task TestGetOrderedAliases_ShouldStartWithPreferredCacheAndGivenHostAsync()
        {
            string givenHost = "sts.microsoft.com";
            string preferredCache = "login.windows.net";
            InstanceDiscovery.InstanceCache.TryAdd(givenHost, new InstanceDiscoveryMetadataEntry
            {
                PreferredNetwork = "login.microsoftonline.com",
                PreferredCache = preferredCache,
                Aliases = new string[] { "login.microsoftonline.com", "login.windows.net", "sts.microsoft.com" }
            });
            var orderedList = await TokenCache.GetOrderedAliasesAsync(
                $"https://{givenHost}/tenant", false, new RequestContext(new AdalLogger(new Guid()))).ConfigureAwait(false);
            CollectionAssert.AreEqual(
                new string[] { preferredCache, givenHost, "login.microsoftonline.com", "login.windows.net", "sts.microsoft.com" },
                orderedList);
        }

        private void AddMockInstanceDiscovery(string host)
        {
            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                Url = $"https://{host}/common/discovery/instance",
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        @"{
                        ""tenant_discovery_endpoint"":""https://login.microsoftonline.com/tenant/.well-known/openid-configuration"",
                        ""api-version"":""1.1"",
                        ""metadata"":[
                            {
                            ""preferred_network"":""login.microsoftonline.com"",
                            ""preferred_cache"":""login.windows.net"",
                            ""aliases"":[
                                ""login.microsoftonline.com"",
                                ""login.windows.net"",
                                ""login.microsoft.com"",
                                ""sts.windows.net""]},
                            {
                            ""preferred_network"":""login.partner.microsoftonline.cn"",
                            ""preferred_cache"":""login.partner.microsoftonline.cn"",
                            ""aliases"":[
                                ""login.partner.microsoftonline.cn"",
                                ""login.chinacloudapi.cn""]},
                            {
                            ""preferred_network"":""login.microsoftonline.de"",
                            ""preferred_cache"":""login.microsoftonline.de"",
                            ""aliases"":[
                                    ""login.microsoftonline.de""]},
                            {
                            ""preferred_network"":""login.microsoftonline.us"",
                            ""preferred_cache"":""login.microsoftonline.us"",
                            ""aliases"":[
                                ""login.microsoftonline.us"",
                                ""login.usgovcloudapi.net""]},
                            {
                            ""preferred_network"":""login-us.microsoftonline.com"",
                            ""preferred_cache"":""login-us.microsoftonline.com"",
                            ""aliases"":[
                                ""login-us.microsoftonline.com""]},
                             {
                                ""preferred_network"": ""uswest-dsts.dsts.core.windows.net/dstsv2"",
                                ""preferred_cache"": ""uswest-dsts.dsts.core.windows.net/dstsv2"",
                                ""aliases"": [""uswest-dsts.dsts.core.windows.net""]}
                        ]}"
                    )
                }
            });
        }

#if !NET_CORE // no support for private object
        [TestMethod]
        [TestCategory("InstanceDiscoveryTests")]
        public async Task TestInstanceDiscovery_WhenMetadataIsReturned_ShouldUsePreferredNetworkForTokenRequestAsync()
        {
            string host = "login.windows.net";
            string preferredNetwork = "login.microsoftonline.com";
            var authenticator = new Authenticator($"https://{host}/contoso.com/", false);
            AddMockInstanceDiscovery(host);
            await authenticator.UpdateFromTemplateAsync(new RequestContext(new AdalLogger(new Guid()))).ConfigureAwait(false);

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                Url = $"https://{preferredNetwork}/contoso.com/oauth2/token", // This validates the token request is sending to expected host
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"some-token\"}")
                }
            });

            var privateObject = new PrivateObject(new AcquireTokenForClientHandler(new RequestData
            {
                Authenticator = authenticator,
                Resource = "resource1",
                ClientKey = new ClientKey(new ClientCredential("client1", "something")),
                SubjectType = TokenSubjectType.Client,
                ExtendedLifeTimeEnabled = false
            }));

            Task sendTokenRequestTask = (Task)privateObject.Invoke("SendTokenRequestAsync");
            await sendTokenRequestTask.ConfigureAwait(false);

            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount()); // This validates that all the mock handlers have been consumed
        }

        [TestMethod]
        [TestCategory("InstanceDiscoveryTests")]
        public async Task TestInstanceDiscovery_WhenMetadataIsReturned_ShouldUsePreferredNetworkForUserRealmDiscoveryAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                string host = "login.windows.net";
                string preferredNetwork = "login.microsoftonline.com";
                var authenticator = new Authenticator($"https://{host}/contoso.com/", false);
                AddMockInstanceDiscovery(host);
                await authenticator.UpdateFromTemplateAsync(new RequestContext(new AdalLogger(new Guid()))).ConfigureAwait(false);

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        Method = HttpMethod.Get,
                        Url =
                            $"https://{preferredNetwork}/common/userrealm/johndoe@contoso.com", // This validates the token request is sending to expected host
                        ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent("{\"account_type\":\"managed\"}")
                        }
                    });

                var requestData = new RequestData
                {
                    Authenticator = authenticator,
                    Resource = "resource1",
                    ClientKey = new ClientKey(new ClientCredential("client1", "something")),
                    SubjectType = TokenSubjectType.Client,
                    ExtendedLifeTimeEnabled = false
                };

                var privateObject = new PrivateObject(
                    new AcquireTokenUsernamePasswordHandler(
                        new WsTrustWebRequestManager(httpManager),
                        requestData,
                        new UsernamePasswordInput("johndoe@contoso.com", "fakepassword")));

                await ((Task)privateObject.Invoke("PreTokenRequestAsync")).ConfigureAwait(false);
            }
        }
#endif

        [TestMethod]
        [TestCategory("InstanceDiscoveryTests")]
        public async Task TestInstanceDiscovery_WhenMetadataIsReturned_ShouldUsePreferredNetworkForDeviceCodeRequestAsync()
        {
            string host = "login.windows.net";
            string preferredNetwork = "login.microsoftonline.com";
            var authenticator = new Authenticator($"https://{host}/contoso.com/", false);
            AddMockInstanceDiscovery(host);
            await authenticator.UpdateFromTemplateAsync(new RequestContext(new AdalLogger(new Guid()))).ConfigureAwait(false);

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                Url = $"https://{preferredNetwork}/contoso.com/oauth2/devicecode", // This validates the token request is sending to expected host
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"user_code\":\"A1B2C3D4\"}")
                }
            });

            var handler = new AcquireDeviceCodeHandler(authenticator, "resource1", "clientId", null);
            await handler.RunHandlerAsync().ConfigureAwait(false);

            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount()); // This validates that all the mock handlers have been consumed
        }

        [TestMethod]
        [TestCategory("InstanceDiscoveryTests")]
        public async Task TestInstanceDiscovery_WhenAuthorityIsDstsAsync()
        {
            string host = "uswest-dsts.dsts.core.windows.net/dstsv2";
            for (int i = 0; i < 2; i++) // Prepare 2 mock responses
            {
                AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler($"https://{host}/home")
                {
                    Method = HttpMethod.Get,
                    Url = $"https://{host}/common/discovery/instance",
                    ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            @"{
                            ""tenant_discovery_endpoint"" : ""https://uswest-dsts.dsts.core.windows.net/dstsv2/v1/.well-known/openid-configuration"",
                            ""metadata"": [
                                {
                                ""preferred_network"": ""uswest-dsts.dsts.core.windows.net/dstsv2"",
                                ""preferred_cache"": ""uswest-dsts.dsts.core.windows.net/dstsv2"",
                                ""aliases"": [""uswest-dsts.dsts.core.windows.net""]
                                }
                            ]
                            }"
                        )
                    }
                });
            }

            RequestContext requestContext = new RequestContext(new AdalLogger(new Guid()));
            // ADAL still behaves correctly using developer provided authority
            var entry = await InstanceDiscovery.GetMetadataEntryAsync(new Uri($"https://{host}/tenant"), true, requestContext).ConfigureAwait(false);
            Assert.AreEqual("uswest-dsts.dsts.core.windows.net/dstsv2", entry.PreferredNetwork); // No exception raised, the host is returned as-is
            Assert.AreEqual(1, AdalHttpMessageHandlerFactory.MockHandlersCount()); // 1 mock response is consumed, 1 remaining

            // Subsequent requests do not result in further authority validation network requests for the process lifetime
            var entry2 = await InstanceDiscovery.GetMetadataEntryAsync(new Uri($"https://{host}/tenant"), true, requestContext).ConfigureAwait(false);
            Assert.AreEqual("uswest-dsts.dsts.core.windows.net/dstsv2", entry2.PreferredNetwork);
            Assert.AreEqual(1, AdalHttpMessageHandlerFactory.MockHandlersCount()); // Still 1 mock response remaining, so no new request was attempted
        }

#if !NET_CORE // no support for PrivateObject
        [TestMethod]
        [TestCategory("InstanceDiscoveryTests")]
        public async Task TestInstanceDiscovery_WhenMetadataIsReturned_ShouldUsePreferredNetworkForTokenRequest_WithDstsAsync()
        {
            string host = "uswest-dsts.dsts.core.windows.net/dstsv2";
            string preferredNetwork = "uswest-dsts.dsts.core.windows.net/dstsv2";
            var authenticator = new Authenticator($"https://{host}/contoso.com/", false);
            AddMockInstanceDiscovery(host);
            await authenticator.UpdateFromTemplateAsync(new RequestContext(new AdalLogger(new Guid()))).ConfigureAwait(false);

            AdalHttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler()
            {
                Method = HttpMethod.Post,
                Url = $"https://{preferredNetwork}/contoso.com/oauth2/token", // This validates the token request is sending to expected host
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"some-token\"}")
                }
            });

            var privateObject = new PrivateObject(new AcquireTokenForClientHandler(new RequestData
            {
                Authenticator = authenticator,
                Resource = "resource1",
                ClientKey = new ClientKey(new ClientCredential("client1", "something")),
                SubjectType = TokenSubjectType.Client,
                ExtendedLifeTimeEnabled = false
            }));
            Task sendTokenRequestTask = (Task)privateObject.Invoke("SendTokenRequestAsync");
            await sendTokenRequestTask.ConfigureAwait(false);

            Assert.AreEqual(0, AdalHttpMessageHandlerFactory.MockHandlersCount()); // This validates that all the mock handlers have been consumed
        }
#endif
    }
}
