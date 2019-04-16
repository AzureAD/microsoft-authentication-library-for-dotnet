// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Guid = System.Guid;
using Microsoft.Identity.Test.Common;

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
            TestCommon.ResetInternalStaticCaches();
        }

        [TestCleanup]
        public void TestCleanup()
        {

        }

        [TestMethod]
        [TestCategory("AdfsAuthorityTests")]
        public void SuccessfulValidationUsingOnPremiseDrsTest()
        {
            using (var harness = new MockHttpAndServiceBundle())
            {
                // add mock response for on-premise DRS request
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ExpectedUrl = "https://enterpriseregistration.fabrikam.com/enrollmentserver/contract",
                        ExpectedQueryParams = new Dictionary<string, string>
                        {
                            {"api-version", "1.0"}
                        },
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(
                            ResourceHelper.GetTestResourceRelativePath(File.ReadAllText("drs-response.json")))
                    });


                // add mock response for on-premise webfinger request
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ExpectedUrl = "https://fs.fabrikam.com/adfs/.well-known/webfinger",
                        ExpectedQueryParams = new Dictionary<string, string>
                        {
                            {"resource", "https://fs.contoso.com"},
                            {"rel", "http://schemas.microsoft.com/rel/trusted-realm"}
                        },
                        ResponseMessage = MockHelpers.CreateSuccessWebFingerResponseMessage()
                    });

                // add mock response for tenant endpoint discovery
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ExpectedUrl = "https://fs.contoso.com/adfs/.well-known/openid-configuration",
                        ResponseMessage =
                            MockHelpers.CreateSuccessResponseMessage(
                                ResourceHelper.GetTestResourceRelativePath(File.ReadAllText("OpenidConfiguration-OnPremise.json")))
                    });

                Authority instance = Authority.CreateAuthority(harness.ServiceBundle, MsalTestConstants.OnPremiseAuthority);
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.Adfs);

                var endpoints = harness.ServiceBundle.AuthorityEndpointResolutionManager.ResolveEndpointsAsync(
                    instance.AuthorityInfo,
                    MsalTestConstants.FabrikamDisplayableId,
                    RequestContext.CreateForTest(harness.ServiceBundle)).ConfigureAwait(false).GetAwaiter().GetResult();

                Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/authorize/", endpoints.AuthorizationEndpoint);
                Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/token/", endpoints.TokenEndpoint);
                Assert.AreEqual("https://fs.contoso.com/adfs", endpoints.SelfSignedJwtAudience);

                // attempt to do authority validation again. NO network call should be made
                instance = Authority.CreateAuthority(harness.ServiceBundle, MsalTestConstants.OnPremiseAuthority);
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.Adfs);

                endpoints = harness.ServiceBundle.AuthorityEndpointResolutionManager.ResolveEndpointsAsync(
                    instance.AuthorityInfo,
                    MsalTestConstants.FabrikamDisplayableId,
                    RequestContext.CreateForTest(harness.ServiceBundle)).ConfigureAwait(false).GetAwaiter().GetResult();

                Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/authorize/", endpoints.AuthorizationEndpoint);
                Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/token/", endpoints.TokenEndpoint);
                Assert.AreEqual("https://fs.contoso.com/adfs", endpoints.SelfSignedJwtAudience);
            }
        }

        [TestMethod]
        [TestCategory("AdfsAuthorityTests")]
        public void SuccessfulValidationUsingCloudDrsFallbackTest()
        {
            using (var harness = new MockHttpAndServiceBundle())
            {
                // add mock failure response for on-premise DRS request
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ExpectedUrl = "https://enterpriseregistration.fabrikam.com/enrollmentserver/contract",
                        ExpectedQueryParams = new Dictionary<string, string>
                        {
                            {"api-version", "1.0"}
                        },
                        ResponseMessage = MockHelpers.CreateFailureMessage(HttpStatusCode.NotFound, "not found")
                    });

                // add mock response for cloud DRS request
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ExpectedUrl = "https://enterpriseregistration.windows.net/fabrikam.com/enrollmentserver/contract",
                        ExpectedQueryParams = new Dictionary<string, string>
                        {
                            {"api-version", "1.0"}
                        },
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(
                            ResourceHelper.GetTestResourceRelativePath(File.ReadAllText("drs-response.json")))
                    });


                // add mock response for on-premise webfinger request
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ExpectedUrl = "https://fs.fabrikam.com/adfs/.well-known/webfinger",
                        ExpectedQueryParams = new Dictionary<string, string>
                        {
                            {"resource", "https://fs.contoso.com"},
                            {"rel", "http://schemas.microsoft.com/rel/trusted-realm"}
                        },
                        ResponseMessage = MockHelpers.CreateSuccessWebFingerResponseMessage()
                    });

                // add mock response for tenant endpoint discovery
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ExpectedUrl = "https://fs.contoso.com/adfs/.well-known/openid-configuration",
                        ResponseMessage =
                            MockHelpers.CreateSuccessResponseMessage(
                                ResourceHelper.GetTestResourceRelativePath(File.ReadAllText("OpenidConfiguration-OnPremise.json")))
                    });

                Authority instance = Authority.CreateAuthority(harness.ServiceBundle, MsalTestConstants.OnPremiseAuthority);
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.Adfs);

                var resolver = new AuthorityEndpointResolutionManager(harness.ServiceBundle);
                var endpoints = resolver.ResolveEndpointsAsync(
                    instance.AuthorityInfo,
                    MsalTestConstants.FabrikamDisplayableId,
                    RequestContext.CreateForTest(harness.ServiceBundle)).ConfigureAwait(false).GetAwaiter().GetResult();

                Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/authorize/", endpoints.AuthorizationEndpoint);
                Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/token/", endpoints.TokenEndpoint);
                Assert.AreEqual("https://fs.contoso.com/adfs", endpoints.SelfSignedJwtAudience);
            }
        }

        [TestMethod]
        [TestCategory("AdfsAuthorityTests")]
        public void ValidationOffSuccessTest()
        {
            using (var harness = new MockHttpAndServiceBundle())
            {
                // add mock response for tenant endpoint discovery
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ExpectedUrl = "https://fs.contoso.com/adfs/.well-known/openid-configuration",
                        ResponseMessage =
                            MockHelpers.CreateSuccessResponseMessage(
                                ResourceHelper.GetTestResourceRelativePath(File.ReadAllText("OpenidConfiguration-OnPremise.json")))
                    });

                Authority instance = Authority.CreateAuthority(harness.ServiceBundle, MsalTestConstants.OnPremiseAuthority);
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.Adfs);
                var resolver = new AuthorityEndpointResolutionManager(harness.ServiceBundle);
                var endpoints = resolver.ResolveEndpointsAsync(
                    instance.AuthorityInfo,
                    MsalTestConstants.FabrikamDisplayableId,
                    RequestContext.CreateForTest(harness.ServiceBundle)).ConfigureAwait(false).GetAwaiter().GetResult();

                Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/authorize/", endpoints.AuthorizationEndpoint);
                Assert.AreEqual("https://fs.contoso.com/adfs/oauth2/token/", endpoints.TokenEndpoint);
                Assert.AreEqual("https://fs.contoso.com/adfs", endpoints.SelfSignedJwtAudience);
            }
        }

        [TestMethod]
        [TestCategory("AdfsAuthorityTests")]
        public void FailedValidationTest()
        {
            using (var harness = new MockHttpAndServiceBundle())
            {
                // add mock response for on-premise DRS request
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ExpectedUrl = "https://enterpriseregistration.fabrikam.com/enrollmentserver/contract",
                        ExpectedQueryParams = new Dictionary<string, string>
                        {
                            {"api-version", "1.0"}
                        },
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(
                            ResourceHelper.GetTestResourceRelativePath(File.ReadAllText("drs-response.json")))
                    });


                // add mock response for on-premise webfinger request
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ExpectedUrl = "https://fs.fabrikam.com/adfs/.well-known/webfinger",
                        ExpectedQueryParams = new Dictionary<string, string>
                        {
                            {"resource", "https://fs.contoso.com"},
                            {"rel", "http://schemas.microsoft.com/rel/trusted-realm"}
                        },
                        ResponseMessage = MockHelpers.CreateFailureMessage(HttpStatusCode.NotFound, "not-found")
                    });

                Authority instance = Authority.CreateAuthority(harness.ServiceBundle, MsalTestConstants.OnPremiseAuthority);
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.Adfs);

                try
                {
                    var resolver = new AuthorityEndpointResolutionManager(harness.ServiceBundle);
                    var endpoints = resolver.ResolveEndpointsAsync(
                        instance.AuthorityInfo,
                        MsalTestConstants.FabrikamDisplayableId,
                        RequestContext.CreateForTest(harness.ServiceBundle)).ConfigureAwait(false).GetAwaiter().GetResult();
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
        public void FailedValidationResourceNotInTrustedRealmTest()
        {
            using (var harness = new MockHttpAndServiceBundle())
            {
                // add mock response for on-premise DRS request
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ExpectedUrl = "https://enterpriseregistration.fabrikam.com/enrollmentserver/contract",
                        ExpectedQueryParams = new Dictionary<string, string>
                        {
                            {"api-version", "1.0"}
                        },
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(
                            ResourceHelper.GetTestResourceRelativePath(File.ReadAllText("drs-response.json")))
                    });


                // add mock response for on-premise webfinger request
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ExpectedUrl = "https://fs.fabrikam.com/adfs/.well-known/webfinger",
                        ExpectedQueryParams = new Dictionary<string, string>
                        {
                            {"resource", "https://fs.contoso.com"},
                            {"rel", "http://schemas.microsoft.com/rel/trusted-realm"}
                        },
                        ResponseMessage = MockHelpers.CreateSuccessWebFingerResponseMessage("https://fs.some-other-sts.com")
                    });

                Authority instance = Authority.CreateAuthority(harness.ServiceBundle, MsalTestConstants.OnPremiseAuthority);
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.Adfs);
                try
                {
                    var resolver = new AuthorityEndpointResolutionManager(harness.ServiceBundle);
                    var endpoints = resolver.ResolveEndpointsAsync(
                        instance.AuthorityInfo,
                        MsalTestConstants.FabrikamDisplayableId,
                        RequestContext.CreateForTest(harness.ServiceBundle)).ConfigureAwait(false).GetAwaiter().GetResult();
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
        public void FailedValidationMissingFieldsInDrsResponseTest()
        {
            using (var harness = new MockHttpAndServiceBundle())
            {
                // add mock failure response for on-premise DRS request
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ExpectedUrl = "https://enterpriseregistration.fabrikam.com/enrollmentserver/contract",
                        ExpectedQueryParams = new Dictionary<string, string>
                        {
                            {"api-version", "1.0"}
                        },
                        ResponseMessage =
                            MockHelpers.CreateSuccessResponseMessage(
                                ResourceHelper.GetTestResourceRelativePath(File.ReadAllText("drs-response-missing-field.json")))
                    });

                Authority instance = Authority.CreateAuthority(harness.ServiceBundle, MsalTestConstants.OnPremiseAuthority);
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.Adfs);
                try
                {
                    var resolver = new AuthorityEndpointResolutionManager(harness.ServiceBundle);
                    var endpoints = resolver.ResolveEndpointsAsync(
                        instance.AuthorityInfo,
                        MsalTestConstants.FabrikamDisplayableId,
                        RequestContext.CreateForTest(harness.ServiceBundle)).ConfigureAwait(false).GetAwaiter().GetResult();
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
        public void FailedTenantDiscoveryMissingEndpointsTest()
        {
            using (var harness = new MockHttpAndServiceBundle())
            {
                // add mock response for tenant endpoint discovery
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ExpectedUrl = "https://fs.contoso.com/adfs/.well-known/openid-configuration",
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(
                            ResourceHelper.GetTestResourceRelativePath(File.ReadAllText("OpenidConfiguration-MissingFields-OnPremise.json")))
                    });

                Authority instance = Authority.CreateAuthority(harness.ServiceBundle, MsalTestConstants.OnPremiseAuthority);
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.Adfs);
                try
                {
                    var resolver = new AuthorityEndpointResolutionManager(harness.ServiceBundle);
                    var endpoints = resolver.ResolveEndpointsAsync(
                        instance.AuthorityInfo,
                        MsalTestConstants.FabrikamDisplayableId,
                        RequestContext.CreateForTest(harness.ServiceBundle)).ConfigureAwait(false).GetAwaiter().GetResult();
                    Assert.Fail("validation should have failed here");
                }
                catch (MsalServiceException exc)
                {
                    Assert.AreEqual(MsalError.TenantDiscoveryFailedError, exc.ErrorCode);
                }
            }
        }
    }
}
