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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Http;
using Microsoft.Identity.Client.Internal.Instance;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.MSAL.NET.Unit.Mocks;

namespace Test.MSAL.NET.Unit.InstanceTests
{
    [TestClass]
    [DeploymentItem("Resources\\OpenidConfiguration-OnPremise.json")]
    [DeploymentItem("Resources\\OpenidConfiguration-MissingFields-OnPremise.json")]
    [DeploymentItem("Resources\\OpenidConfiguration.json")]
    [DeploymentItem("Resources\\OpenidConfiguration-MissingFields.json")]
    [DeploymentItem("Resources\\drs-response-missing-field.json")]
    [DeploymentItem("Resources\\drs-response.json")]
    public class AuthorityTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            Authority.ValidatedAuthorities.Clear();
            HttpClientFactory.ReturnHttpClientForMocks = true;
            HttpMessageHandlerFactory.ClearMockHandlers();
        }

        [TestCleanup]
        public void TestCleanup()
        {

        }

        [TestMethod]
        [TestCategory("AuthorityTests")]
        public void AadSuccessfulValidationTest()
        {
            //add mock response for instance validation
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                Url = "https://login.windows.net/common/discovery/instance",
                QueryParams = new Dictionary<string, string>
                {
                    {"api-version", "1.0"},
                    {"authorization_endpoint", "https://login.microsoftonline.in/mytenant.com/oauth2/v2.0/authorize"},
                },
                ResponseMessage = MockHelpers.CreateSuccessResponseMessage(
                    "{\"tenant_discovery_endpoint\":\"https://login.microsoftonline.in/mytenant.com/.well-known/openid-configuration\"}")
            });

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                Url = "https://login.microsoftonline.in/mytenant.com/.well-known/openid-configuration",
                ResponseMessage = MockHelpers.CreateSuccessResponseMessage(File.ReadAllText("OpenidConfiguration.json"))
            });

            Authority instance = Authority.CreateAuthority("https://login.microsoftonline.in/mytenant.com", true);
            Assert.IsNotNull(instance);
            Assert.AreEqual(instance.AuthorityType, AuthorityType.Aad);
            Task.Run(async () =>
            {
                await instance.ResolveEndpointsAsync(null, new CallState(Guid.NewGuid()));
            }).GetAwaiter().GetResult();

            Assert.AreEqual("https://login.microsoftonline.com/6babcaad-604b-40ac-a9d7-9fd97c0b779f/oauth2/authorize",
                instance.AuthorizationEndpoint);
            Assert.AreEqual("https://login.microsoftonline.com/6babcaad-604b-40ac-a9d7-9fd97c0b779f/oauth2/token",
                instance.TokenEndpoint);
            Assert.AreEqual("https://sts.windows.net/6babcaad-604b-40ac-a9d7-9fd97c0b779f/",
                instance.SelfSignedJwtAudience);
            Assert.AreEqual(0, HttpMessageHandlerFactory.MockCount);
        }

        [TestMethod]
        [TestCategory("AuthorityTests")]
        public void AadValidationOffSuccessTest()
        {
            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                Url = "https://login.microsoftonline.in/mytenant.com/v2.0/.well-known/openid-configuration",
                ResponseMessage = MockHelpers.CreateSuccessResponseMessage(File.ReadAllText("OpenidConfiguration.json"))
            });

            Authority instance = Authority.CreateAuthority("https://login.microsoftonline.in/mytenant.com", false);
            Assert.IsNotNull(instance);
            Assert.AreEqual(instance.AuthorityType, AuthorityType.Aad);
            Task.Run(async () =>
            {
                await instance.ResolveEndpointsAsync(null, new CallState(Guid.NewGuid()));
            }).GetAwaiter().GetResult();

            Assert.AreEqual("https://login.microsoftonline.com/6babcaad-604b-40ac-a9d7-9fd97c0b779f/oauth2/authorize",
                instance.AuthorizationEndpoint);
            Assert.AreEqual("https://login.microsoftonline.com/6babcaad-604b-40ac-a9d7-9fd97c0b779f/oauth2/token",
                instance.TokenEndpoint);
            Assert.AreEqual("https://sts.windows.net/6babcaad-604b-40ac-a9d7-9fd97c0b779f/",
                instance.SelfSignedJwtAudience);
            Assert.AreEqual(0, HttpMessageHandlerFactory.MockCount);
        }

        [TestMethod]
        [TestCategory("AuthorityTests")]
        public void AadFailedValidationTest()
        {
            //add mock response for instance validation
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                Url = "https://login.windows.net/common/discovery/instance",
                QueryParams = new Dictionary<string, string>
                {
                    {"api-version", "1.0"},
                    {"authorization_endpoint", "https://login.microsoft0nline.com/mytenant.com/oauth2/v2.0/authorize"},
                },
                ResponseMessage =
                    MockHelpers.CreateFailureMessage(HttpStatusCode.BadRequest, "{\"error\":\"invalid_instance\"," +
                                                                                "\"error_description\":\"AADSTS50049: " +
                                                                                "Unknown or invalid instance. Trace " +
                                                                                "ID: b9d0894d-a9a4-4dba-b38e-8fb6a009bc00 " +
                                                                                "Correlation ID: 34f7b4cf-4fa2-4f35-a59b" +
                                                                                "-54b6f91a9c94 Timestamp: 2016-08-23 " +
                                                                                "20:45:49Z\",\"error_codes\":[50049]," +
                                                                                "\"timestamp\":\"2016-08-23 20:45:49Z\"," +
                                                                                "\"trace_id\":\"b9d0894d-a9a4-4dba-b38e-8f" +
                                                                                "b6a009bc00\",\"correlation_id\":\"34f7b4cf-" +
                                                                                "4fa2-4f35-a59b-54b6f91a9c94\"}")
            });

            Authority instance = Authority.CreateAuthority("https://login.microsoft0nline.com/mytenant.com", true);
            Assert.IsNotNull(instance);
            Assert.AreEqual(instance.AuthorityType, AuthorityType.Aad);
            try
            {
                Task.Run(async () =>
                {
                    await instance.ResolveEndpointsAsync(null, new CallState(Guid.NewGuid()));
                }).GetAwaiter().GetResult();
                Assert.Fail("validation should have failed here");
            }
            catch (Exception exc)
            {
                Assert.IsNotNull(exc is MsalServiceException);
                Assert.AreEqual(((MsalServiceException) exc).ErrorCode, "invalid_instance");
            }

            Assert.AreEqual(0, HttpMessageHandlerFactory.MockCount);
        }

        [TestMethod]
        [TestCategory("AuthorityTests")]
        public void AadFailedValidationMissingFieldsTest()
        {
            //add mock response for instance validation
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                Url = "https://login.windows.net/common/discovery/instance",
                QueryParams = new Dictionary<string, string>
                {
                    {"api-version", "1.0"},
                    {"authorization_endpoint", "https://login.microsoft0nline.com/mytenant.com/oauth2/v2.0/authorize"},
                },
                ResponseMessage = MockHelpers.CreateSuccessResponseMessage("{}")
            });

            Authority instance = Authority.CreateAuthority("https://login.microsoft0nline.com/mytenant.com", true);
            Assert.IsNotNull(instance);
            Assert.AreEqual(instance.AuthorityType, AuthorityType.Aad);
            try
            {
                Task.Run(async () =>
                {
                    await instance.ResolveEndpointsAsync(null, new CallState(Guid.NewGuid()));
                }).GetAwaiter().GetResult();
                Assert.Fail("validation should have failed here");
            }
            catch (Exception exc)
            {
                Assert.IsNotNull(exc);
            }

            Assert.AreEqual(0, HttpMessageHandlerFactory.MockCount);
        }

        [TestMethod]
        [TestCategory("AuthorityTests")]
        public void AadFailedTenantDiscoveryMissingEndpointsTest()
        {
            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                Url = "https://login.microsoftonline.in/mytenant.com/v2.0/.well-known/openid-configuration",
                ResponseMessage =
                    MockHelpers.CreateSuccessResponseMessage(File.ReadAllText("OpenidConfiguration-MissingFields.json"))
            });

            Authority instance = Authority.CreateAuthority("https://login.microsoftonline.in/mytenant.com", false);
            Assert.IsNotNull(instance);
            Assert.AreEqual(instance.AuthorityType, AuthorityType.Aad);
            try
            {
                Task.Run(async () =>
                {
                    await instance.ResolveEndpointsAsync(null, new CallState(Guid.NewGuid()));
                }).GetAwaiter().GetResult();
                Assert.Fail("validation should have failed here");
            }
            catch (MsalServiceException exc)
            {
                Assert.AreEqual(MsalError.TenantDiscoveryFailed, exc.ErrorCode);
            }

            Assert.AreEqual(0, HttpMessageHandlerFactory.MockCount);
        }

        [TestMethod]
        [TestCategory("AuthorityTests")]
        public void AdfsSuccessfulValidationUsingOnPremiseDrsTest()
        {
            //add mock response for on-premise DRS request
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                Url = "https://enterpriseregistration.fabrikam.com/enrollmentserver/contract",
                QueryParams = new Dictionary<string, string>
                {
                    {"api-version", "1.0"}
                },
                ResponseMessage = MockHelpers.CreateSuccessResponseMessage(File.ReadAllText("drs-response.json"))
            });


            //add mock response for on-premise webfinger request
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                Url = "https://fs.fabrikam.com/adfs/.well-known/webfinger",
                QueryParams = new Dictionary<string, string>
                {
                    {"resource", "https://fs.contoso.com"},
                    {"rel", "http://schemas.microsoft.com/rel/trusted-realm"}
                },
                ResponseMessage = MockHelpers.CreateSuccessWebFingerResponseMessage()
            });

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                Url = "https://fs.contoso.com/adfs/.well-known/openid-configuration",
                ResponseMessage = MockHelpers.CreateSuccessResponseMessage(File.ReadAllText("OpenidConfiguration-OnPremise.json"))
            });

            Authority instance = Authority.CreateAuthority(TestConstants.OnPremiseAuthority, true);
            Assert.IsNotNull(instance);
            Assert.AreEqual(instance.AuthorityType, AuthorityType.Adfs);
            Task.Run(async () =>
            {
                await instance.ResolveEndpointsAsync(TestConstants.FabrikamDisplayableId, new CallState(Guid.NewGuid()));
            }).GetAwaiter().GetResult();

            Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/authorize/",
                instance.AuthorizationEndpoint);
            Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/token/",
                instance.TokenEndpoint);
            Assert.AreEqual("https://fs.contoso.com/adfs",
                instance.SelfSignedJwtAudience);
            Assert.AreEqual(0, HttpMessageHandlerFactory.MockCount);
            Assert.AreEqual(1, Authority.ValidatedAuthorities.Count);

            //attempt to do authority validation again. NO network call should be made
            instance = Authority.CreateAuthority(TestConstants.OnPremiseAuthority, true);
            Assert.IsNotNull(instance);
            Assert.AreEqual(instance.AuthorityType, AuthorityType.Adfs);
            Task.Run(async () =>
            {
                await instance.ResolveEndpointsAsync(TestConstants.FabrikamDisplayableId, new CallState(Guid.NewGuid()));
            }).GetAwaiter().GetResult();

            Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/authorize/",
                instance.AuthorizationEndpoint);
            Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/token/",
                instance.TokenEndpoint);
            Assert.AreEqual("https://fs.contoso.com/adfs",
                instance.SelfSignedJwtAudience);
        }

        [TestMethod]
        [TestCategory("AuthorityTests")]
        public void AdfsSuccessfulValidationUsingCloudDrsFallbackTest()
        {
            //add mock failure response for on-premise DRS request
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                Url = "https://enterpriseregistration.fabrikam.com/enrollmentserver/contract",
                QueryParams = new Dictionary<string, string>
                {
                    {"api-version", "1.0"}
                },
                ResponseMessage = MockHelpers.CreateFailureMessage(HttpStatusCode.NotFound, "not found")
            });

            //add mock response for cloud DRS request
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                Url = "https://enterpriseregistration.windows.net/fabrikam.com/enrollmentserver/contract",
                QueryParams = new Dictionary<string, string>
                {
                    {"api-version", "1.0"}
                },
                ResponseMessage = MockHelpers.CreateSuccessResponseMessage(File.ReadAllText("drs-response.json"))
            });


            //add mock response for on-premise webfinger request
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                Url = "https://fs.fabrikam.com/adfs/.well-known/webfinger",
                QueryParams = new Dictionary<string, string>
                {
                    {"resource", "https://fs.contoso.com"},
                    {"rel", "http://schemas.microsoft.com/rel/trusted-realm"}
                },
                ResponseMessage = MockHelpers.CreateSuccessWebFingerResponseMessage()
            });

            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                Url = "https://fs.contoso.com/adfs/.well-known/openid-configuration",
                ResponseMessage = MockHelpers.CreateSuccessResponseMessage(File.ReadAllText("OpenidConfiguration-OnPremise.json"))
            });

            Authority instance = Authority.CreateAuthority(TestConstants.OnPremiseAuthority, true);
            Assert.IsNotNull(instance);
            Assert.AreEqual(instance.AuthorityType, AuthorityType.Adfs);
            Task.Run(async () =>
            {
                await instance.ResolveEndpointsAsync(TestConstants.FabrikamDisplayableId, new CallState(Guid.NewGuid()));
            }).GetAwaiter().GetResult();

            Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/authorize/",
                instance.AuthorizationEndpoint);
            Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/token/",
                instance.TokenEndpoint);
            Assert.AreEqual("https://fs.contoso.com/adfs",
                instance.SelfSignedJwtAudience);
            Assert.AreEqual(0, HttpMessageHandlerFactory.MockCount);
        }

        [TestMethod]
        [TestCategory("AuthorityTests")]
        public void AdfsValidationOffSuccessTest()
        {
            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                Url = "https://fs.contoso.com/adfs/.well-known/openid-configuration",
                ResponseMessage = MockHelpers.CreateSuccessResponseMessage(File.ReadAllText("OpenidConfiguration-OnPremise.json"))
            });

            Authority instance = Authority.CreateAuthority(TestConstants.OnPremiseAuthority, false);
            Assert.IsNotNull(instance);
            Assert.AreEqual(instance.AuthorityType, AuthorityType.Adfs);
            Task.Run(async () =>
            {
                await instance.ResolveEndpointsAsync(TestConstants.FabrikamDisplayableId, new CallState(Guid.NewGuid()));
            }).GetAwaiter().GetResult();

            Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/authorize/",
                instance.AuthorizationEndpoint);
            Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/token/",
                instance.TokenEndpoint);
            Assert.AreEqual("https://fs.contoso.com/adfs",
                instance.SelfSignedJwtAudience);
            Assert.AreEqual(0, HttpMessageHandlerFactory.MockCount);
        }

        [TestMethod]
        [TestCategory("AuthorityTests")]
        public void AdfsFailedValidationTest()
        {
            //add mock response for on-premise DRS request
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                Url = "https://enterpriseregistration.fabrikam.com/enrollmentserver/contract",
                QueryParams = new Dictionary<string, string>
                {
                    {"api-version", "1.0"}
                },
                ResponseMessage = MockHelpers.CreateSuccessResponseMessage(File.ReadAllText("drs-response.json"))
            });


            //add mock response for on-premise webfinger request
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                Url = "https://fs.fabrikam.com/adfs/.well-known/webfinger",
                QueryParams = new Dictionary<string, string>
                {
                    {"resource", "https://fs.contoso.com"},
                    {"rel", "http://schemas.microsoft.com/rel/trusted-realm"}
                },
                ResponseMessage = MockHelpers.CreateFailureMessage(HttpStatusCode.NotFound, "not-found")
            });

            Authority instance = Authority.CreateAuthority(TestConstants.OnPremiseAuthority, true);
            Assert.IsNotNull(instance);
            Assert.AreEqual(instance.AuthorityType, AuthorityType.Adfs);
            try
            {
                Task.Run(async () =>
                {
                    await instance.ResolveEndpointsAsync(TestConstants.FabrikamDisplayableId, new CallState(Guid.NewGuid()));
                }).GetAwaiter().GetResult();
                Assert.Fail("ResolveEndpointsAsync should have failed here");
            }
            catch (Exception exc)
            {
                Assert.IsNotNull(exc);
            }

            Assert.AreEqual(0, HttpMessageHandlerFactory.MockCount);
        }

        [TestMethod]
        [TestCategory("AuthorityTests")]
        public void AdfsFailedValidationResourceNotInTrustedRealmTest()
        {
            //add mock response for on-premise DRS request
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                Url = "https://enterpriseregistration.fabrikam.com/enrollmentserver/contract",
                QueryParams = new Dictionary<string, string>
                {
                    {"api-version", "1.0"}
                },
                ResponseMessage = MockHelpers.CreateSuccessResponseMessage(File.ReadAllText("drs-response.json"))
            });


            //add mock response for on-premise webfinger request
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                Url = "https://fs.fabrikam.com/adfs/.well-known/webfinger",
                QueryParams = new Dictionary<string, string>
                {
                    {"resource", "https://fs.contoso.com"},
                    {"rel", "http://schemas.microsoft.com/rel/trusted-realm"}
                },
                ResponseMessage = MockHelpers.CreateSuccessWebFingerResponseMessage("https://fs.some-other-sts.com")
            });

            Authority instance = Authority.CreateAuthority(TestConstants.OnPremiseAuthority, true);
            Assert.IsNotNull(instance);
            Assert.AreEqual(instance.AuthorityType, AuthorityType.Adfs);
            try
            {
                Task.Run(async () =>
                {
                    await instance.ResolveEndpointsAsync(TestConstants.FabrikamDisplayableId, new CallState(Guid.NewGuid()));
                }).GetAwaiter().GetResult();
                Assert.Fail("ResolveEndpointsAsync should have failed here");
            }
            catch (Exception exc)
            {
                Assert.IsNotNull(exc);
            }

            Assert.AreEqual(0, HttpMessageHandlerFactory.MockCount);
        }

        [TestMethod]
        [TestCategory("AuthorityTests")]
        public void AdfsFailedValidationMissingFieldsInDrsResponseTest()
        {
            //add mock failure response for on-premise DRS request
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                Url = "https://enterpriseregistration.fabrikam.com/enrollmentserver/contract",
                QueryParams = new Dictionary<string, string>
                {
                    {"api-version", "1.0"}
                },
                ResponseMessage = MockHelpers.CreateSuccessResponseMessage(File.ReadAllText("drs-response-missing-field.json"))
            });

            Authority instance = Authority.CreateAuthority(TestConstants.OnPremiseAuthority, true);
            Assert.IsNotNull(instance);
            Assert.AreEqual(instance.AuthorityType, AuthorityType.Adfs);
            try
            {
                Task.Run(async () =>
                {
                    await instance.ResolveEndpointsAsync(TestConstants.FabrikamDisplayableId, new CallState(Guid.NewGuid()));
                }).GetAwaiter().GetResult();
                Assert.Fail("ResolveEndpointsAsync should have failed here");
            }
            catch (Exception exc)
            {
                Assert.IsNotNull(exc);
            }

            Assert.AreEqual(0, HttpMessageHandlerFactory.MockCount);
        }

        [TestMethod]
        [TestCategory("AuthorityTests")]
        public void AdfsFailedTenantDiscoveryMissingEndpointsTest()
        {
            //add mock response for tenant endpoint discovery
            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler
            {
                Method = HttpMethod.Get,
                Url = "https://fs.contoso.com/adfs/.well-known/openid-configuration",
                ResponseMessage = MockHelpers.CreateSuccessResponseMessage(File.ReadAllText("OpenidConfiguration-MissingFields-OnPremise.json"))
            });

            Authority instance = Authority.CreateAuthority(TestConstants.OnPremiseAuthority, false);
            Assert.IsNotNull(instance);
            Assert.AreEqual(instance.AuthorityType, AuthorityType.Adfs);
            try
            {
                Task.Run(async () =>
                {
                    await instance.ResolveEndpointsAsync(TestConstants.FabrikamDisplayableId, new CallState(Guid.NewGuid()));
                }).GetAwaiter().GetResult();
                Assert.Fail("validation should have failed here");
            }
            catch (MsalServiceException exc)
            {
                Assert.AreEqual(MsalError.TenantDiscoveryFailed, exc.ErrorCode);
            }

            Assert.AreEqual(0, HttpMessageHandlerFactory.MockCount);
        }
    }
}