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
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Exceptions;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Config;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Guid = System.Guid;

namespace Microsoft.Identity.Test.Unit.CoreTests.InstanceTests
{
    [TestClass]
    [DeploymentItem("Resources\\drs-response-missing-field.json")]
    [DeploymentItem("Resources\\drs-response.json")]
    [DeploymentItem("Resources\\OpenidConfiguration-OnPremise.json")]
    [DeploymentItem("Resources\\OpenidConfiguration-MissingFields-OnPremise.json")]
    [Ignore] // disable until we support ADFS
    public class AdfsAuthorityTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
        }

        [TestCleanup]
        public void TestCleanup()
        {

        }

        [TestMethod]
        [TestCategory("AdfsAuthorityTests")]
        public async Task SuccessfulValidationUsingOnPremiseDrsTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = TestCommon.CreateServiceBundleWithCustomHttpManager(httpManager);

                // add mock response for on-premise DRS request
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Get,
                        Url = "https://enterpriseregistration.fabrikam.com/enrollmentserver/contract",
                        QueryParams = new Dictionary<string, string>
                        {
                            {"api-version", "1.0"}
                        },
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(
                            ResourceHelper.GetTestResourceRelativePath(File.ReadAllText("drs-response.json")))
                    });


                // add mock response for on-premise webfinger request
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
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

                // add mock response for tenant endpoint discovery
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Get,
                        Url = "https://fs.contoso.com/adfs/.well-known/openid-configuration",
                        ResponseMessage =
                            MockHelpers.CreateSuccessResponseMessage(
                                ResourceHelper.GetTestResourceRelativePath(File.ReadAllText("OpenidConfiguration-OnPremise.json")))
                    });

                Authority instance = Authority.CreateAuthority(serviceBundle, CoreTestConstants.OnPremiseAuthority, true);

                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.Adfs);
                var endpointManager = new AuthorityEndpointResolutionManager(serviceBundle);
                AuthorityEndpoints endpoints = await endpointManager.ResolveEndpointsAsync(
                    instance.AuthorityInfo,
                    CoreTestConstants.FabrikamDisplayableId,
                    RequestContext.CreateForTest()).ConfigureAwait(false);

                Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/authorize/", endpoints.AuthorizationEndpoint);
                Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/token/", endpoints.TokenEndpoint);
                Assert.AreEqual("https://fs.contoso.com/adfs", endpoints.SelfSignedJwtAudience);
                Assert.AreEqual(1, serviceBundle.ValidatedAuthoritiesCache.Count);

                // attempt to do authority validation again. NO network call should be made
                instance = Authority.CreateAuthority(serviceBundle, CoreTestConstants.OnPremiseAuthority, true);
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.Adfs);
                endpoints = await endpointManager.ResolveEndpointsAsync(
                                instance.AuthorityInfo,
                    CoreTestConstants.FabrikamDisplayableId,
                    RequestContext.CreateForTest()).ConfigureAwait(false);

                Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/authorize/", endpoints.AuthorizationEndpoint);
                Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/token/", endpoints.TokenEndpoint);
                Assert.AreEqual("https://fs.contoso.com/adfs", endpoints.SelfSignedJwtAudience);
            }
        }

        [TestMethod]
        [TestCategory("AdfsAuthorityTests")]
        public async Task SuccessfulValidationUsingCloudDrsFallbackTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = TestCommon.CreateServiceBundleWithCustomHttpManager(httpManager);

                // add mock failure response for on-premise DRS request
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Get,
                        Url = "https://enterpriseregistration.fabrikam.com/enrollmentserver/contract",
                        QueryParams = new Dictionary<string, string>
                        {
                            {"api-version", "1.0"}
                        },
                        ResponseMessage = MockHelpers.CreateFailureMessage(HttpStatusCode.NotFound, "not found")
                    });

                // add mock response for cloud DRS request
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Get,
                        Url = "https://enterpriseregistration.windows.net/fabrikam.com/enrollmentserver/contract",
                        QueryParams = new Dictionary<string, string>
                        {
                            {"api-version", "1.0"}
                        },
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(
                            ResourceHelper.GetTestResourceRelativePath(File.ReadAllText("drs-response.json")))
                    });


                // add mock response for on-premise webfinger request
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
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

                // add mock response for tenant endpoint discovery
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Get,
                        Url = "https://fs.contoso.com/adfs/.well-known/openid-configuration",
                        ResponseMessage =
                            MockHelpers.CreateSuccessResponseMessage(
                                ResourceHelper.GetTestResourceRelativePath(File.ReadAllText("OpenidConfiguration-OnPremise.json")))
                    });

                Authority instance = Authority.CreateAuthority(serviceBundle, CoreTestConstants.OnPremiseAuthority, true);
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.Adfs);

                var endpointManager = new AuthorityEndpointResolutionManager(serviceBundle);
                AuthorityEndpoints endpoints = await endpointManager.ResolveEndpointsAsync(
                                                   instance.AuthorityInfo,
                            CoreTestConstants.FabrikamDisplayableId,
                            RequestContext.CreateForTest()).ConfigureAwait(false);

                Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/authorize/", endpoints.AuthorizationEndpoint);
                Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/token/", endpoints.TokenEndpoint);
                Assert.AreEqual("https://fs.contoso.com/adfs", endpoints.SelfSignedJwtAudience);
            }
        }

        [TestMethod]
        [TestCategory("AdfsAuthorityTests")]
        public async Task ValidationOffSuccessTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = TestCommon.CreateServiceBundleWithCustomHttpManager(httpManager);

                // add mock response for tenant endpoint discovery
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Get,
                        Url = "https://fs.contoso.com/adfs/.well-known/openid-configuration",
                        ResponseMessage =
                            MockHelpers.CreateSuccessResponseMessage(
                                ResourceHelper.GetTestResourceRelativePath(File.ReadAllText("OpenidConfiguration-OnPremise.json")))
                    });

                Authority instance = Authority.CreateAuthority(serviceBundle, CoreTestConstants.OnPremiseAuthority, false);
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.Adfs);
                var endpointManager = new AuthorityEndpointResolutionManager(serviceBundle);
                AuthorityEndpoints endpoints = await endpointManager.ResolveEndpointsAsync(
                                                   instance.AuthorityInfo,
                    CoreTestConstants.FabrikamDisplayableId,
                    RequestContext.CreateForTest()).ConfigureAwait(false);

                Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/authorize/", endpoints.AuthorizationEndpoint);
                Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/token/", endpoints.TokenEndpoint);
                Assert.AreEqual("https://fs.contoso.com/adfs", endpoints.SelfSignedJwtAudience);
            }
        }

        [TestMethod]
        [TestCategory("AdfsAuthorityTests")]
        public async Task FailedValidationTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = TestCommon.CreateServiceBundleWithCustomHttpManager(httpManager);

                // add mock response for on-premise DRS request
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Get,
                        Url = "https://enterpriseregistration.fabrikam.com/enrollmentserver/contract",
                        QueryParams = new Dictionary<string, string>
                        {
                            {"api-version", "1.0"}
                        },
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(
                            ResourceHelper.GetTestResourceRelativePath(File.ReadAllText("drs-response.json")))
                    });


                // add mock response for on-premise webfinger request
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
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

                Authority instance = Authority.CreateAuthority(serviceBundle, CoreTestConstants.OnPremiseAuthority, true);
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.Adfs);
                var endpointManager = new AuthorityEndpointResolutionManager(serviceBundle);
                try
                {
                    await endpointManager.ResolveEndpointsAsync(
                        instance.AuthorityInfo,
                        CoreTestConstants.FabrikamDisplayableId,
                        RequestContext.CreateForTest()).ConfigureAwait(false);
                    Assert.Fail("ResolveEndpointsAsync should have failed here");
                }
                catch (Exception exc)
                {
                    Assert.IsNotNull(exc);
                }
            }
        }

        [TestMethod]
        [TestCategory("AdfsAuthorityTests")]
        public async Task FailedValidationResourceNotInTrustedRealmTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = TestCommon.CreateServiceBundleWithCustomHttpManager(httpManager);

                // add mock response for on-premise DRS request
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Get,
                        Url = "https://enterpriseregistration.fabrikam.com/enrollmentserver/contract",
                        QueryParams = new Dictionary<string, string>
                        {
                            {"api-version", "1.0"}
                        },
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(
                            ResourceHelper.GetTestResourceRelativePath(File.ReadAllText("drs-response.json")))
                    });


                // add mock response for on-premise webfinger request
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
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

                Authority instance = Authority.CreateAuthority(serviceBundle, CoreTestConstants.OnPremiseAuthority, true);
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.Adfs);
                var endpointManager = new AuthorityEndpointResolutionManager(serviceBundle);

                try
                {
                    await endpointManager.ResolveEndpointsAsync(
                        instance.AuthorityInfo,
                        CoreTestConstants.FabrikamDisplayableId,
                        RequestContext.CreateForTest()).ConfigureAwait(false);
                    Assert.Fail("ResolveEndpointsAsync should have failed here");
                }
                catch (Exception exc)
                {
                    Assert.IsNotNull(exc);
                }
            }
        }

        [TestMethod]
        [TestCategory("AdfsAuthorityTests")]
        public async Task FailedValidationMissingFieldsInDrsResponseTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = TestCommon.CreateServiceBundleWithCustomHttpManager(httpManager);

                // add mock failure response for on-premise DRS request
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Get,
                        Url = "https://enterpriseregistration.fabrikam.com/enrollmentserver/contract",
                        QueryParams = new Dictionary<string, string>
                        {
                            {"api-version", "1.0"}
                        },
                        ResponseMessage =
                            MockHelpers.CreateSuccessResponseMessage(
                                ResourceHelper.GetTestResourceRelativePath(File.ReadAllText("drs-response-missing-field.json")))
                    });

                Authority instance = Authority.CreateAuthority(serviceBundle, CoreTestConstants.OnPremiseAuthority, true);
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.Adfs);
                var endpointManager = new AuthorityEndpointResolutionManager(serviceBundle);
                try
                {
                    await endpointManager.ResolveEndpointsAsync(
                        instance.AuthorityInfo,
                        CoreTestConstants.FabrikamDisplayableId,
                        RequestContext.CreateForTest()).ConfigureAwait(false);
                    Assert.Fail("ResolveEndpointsAsync should have failed here");
                }
                catch (Exception exc)
                {
                    Assert.IsNotNull(exc);
                }
            }
        }

        [TestMethod]
        [TestCategory("AdfsAuthorityTests")]
        public async Task FailedTenantDiscoveryMissingEndpointsTestAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var serviceBundle = TestCommon.CreateServiceBundleWithCustomHttpManager(httpManager);

                // add mock response for tenant endpoint discovery
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Get,
                        Url = "https://fs.contoso.com/adfs/.well-known/openid-configuration",
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(
                            ResourceHelper.GetTestResourceRelativePath(File.ReadAllText("OpenidConfiguration-MissingFields-OnPremise.json")))
                    });

                Authority instance = Authority.CreateAuthority(serviceBundle, CoreTestConstants.OnPremiseAuthority, false);
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.Adfs);
                var endpointManager = new AuthorityEndpointResolutionManager(serviceBundle);
                try
                {
                    await endpointManager.ResolveEndpointsAsync(
                        instance.AuthorityInfo,
                        CoreTestConstants.FabrikamDisplayableId,
                        RequestContext.CreateForTest()).ConfigureAwait(false);
                    Assert.Fail("validation should have failed here");
                }
                catch (MsalServiceException exc)
                {
                    Assert.AreEqual(CoreErrorCodes.TenantDiscoveryFailedError, exc.ErrorCode);
                }
            }
        }
    }
}
