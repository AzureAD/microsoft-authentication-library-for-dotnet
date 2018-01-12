//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted free of charge to any person obtaining a copy
// of this software and associated documentation files(the "Software") to deal
// in the Software without restriction including without limitation the rights
// to use copy modify merge publish distribute sublicense and / or sell
// copies of the Software and to permit persons to whom the Software is
// furnished to do so subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND EXPRESS OR
// IMPLIED INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM DAMAGES OR OTHER
// LIABILITY WHETHER IN AN ACTION OF CONTRACT TORT OR OTHERWISE ARISING FROM
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Http;
using Microsoft.Identity.Core.Instance;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Microsoft.Identity.Core.Unit;
using Test.Microsoft.Identity.Unit.Mocks;
using Guid = System.Guid;

namespace Test.Microsoft.Identity.Unit.InstanceTests
{
    [TestClass]
    [DeploymentItem("Resources\\drs-response-missing-field.json")]
    [DeploymentItem("Resources\\drs-response.json")]
    [DeploymentItem("Resources\\OpenidConfiguration-OnPremise.json")]
    [DeploymentItem("Resources\\OpenidConfiguration-MissingFields-OnPremise.json")]
    [Ignore] //disable until we support ADFS
    public class AdfsAuthorityTests
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
        [TestCategory("AdfsAuthorityTests")]
        public void SuccessfulValidationUsingOnPremiseDrsTest()
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
                await instance.ResolveEndpointsAsync(TestConstants.FabrikamDisplayableId, new RequestContext(new TestLogger(Guid.NewGuid(), null)));
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
                await instance.ResolveEndpointsAsync(TestConstants.FabrikamDisplayableId, new RequestContext(new TestLogger(Guid.NewGuid(), null)));
            }).GetAwaiter().GetResult();

            Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/authorize/",
                instance.AuthorizationEndpoint);
            Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/token/",
                instance.TokenEndpoint);
            Assert.AreEqual("https://fs.contoso.com/adfs",
                instance.SelfSignedJwtAudience);
        }

        [TestMethod]
        [TestCategory("AdfsAuthorityTests")]
        public void SuccessfulValidationUsingCloudDrsFallbackTest()
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
                await instance.ResolveEndpointsAsync(TestConstants.FabrikamDisplayableId, new RequestContext(new TestLogger(Guid.NewGuid(), null)));
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
        [TestCategory("AdfsAuthorityTests")]
        public void ValidationOffSuccessTest()
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
                await instance.ResolveEndpointsAsync(TestConstants.FabrikamDisplayableId, new RequestContext(new TestLogger(Guid.NewGuid(), null)));
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
        [TestCategory("AdfsAuthorityTests")]
        public void FailedValidationTest()
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
                    await instance.ResolveEndpointsAsync(TestConstants.FabrikamDisplayableId, new RequestContext(new TestLogger(Guid.NewGuid(), null)));
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
        [TestCategory("AdfsAuthorityTests")]
        public void FailedValidationResourceNotInTrustedRealmTest()
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
                    await instance.ResolveEndpointsAsync(TestConstants.FabrikamDisplayableId, new RequestContext(new TestLogger(Guid.NewGuid(), null)));
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
        [TestCategory("AdfsAuthorityTests")]
        public void FailedValidationMissingFieldsInDrsResponseTest()
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
                    await instance.ResolveEndpointsAsync(TestConstants.FabrikamDisplayableId, new RequestContext(new TestLogger(Guid.NewGuid(), null)));
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
        [TestCategory("AdfsAuthorityTests")]
        public void FailedTenantDiscoveryMissingEndpointsTest()
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
                    await instance.ResolveEndpointsAsync(TestConstants.FabrikamDisplayableId, new RequestContext(new TestLogger(Guid.NewGuid(), null)));
                }).GetAwaiter().GetResult();
                Assert.Fail("validation should have failed here");
            }
            catch (MsalClientException exc)
            {
                Assert.AreEqual(MsalClientException.TenantDiscoveryFailedError, exc.ErrorCode);
            }

            Assert.AreEqual(0, HttpMessageHandlerFactory.MockCount);
        }
    }
}
