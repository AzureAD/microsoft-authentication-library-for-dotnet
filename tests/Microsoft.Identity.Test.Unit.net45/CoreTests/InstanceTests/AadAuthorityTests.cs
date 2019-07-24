// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common.Core.Helpers;

namespace Microsoft.Identity.Test.Unit.CoreTests.InstanceTests
{
    [TestClass]
    [DeploymentItem("Resources\\OpenidConfiguration.json")]
    [DeploymentItem("Resources\\OpenidConfiguration-MissingFields.json")]
    [DeploymentItem("Resources\\OpenidConfigurationCommon.json")]
    public class AadAuthorityTests : TestBase
    {
        [TestMethod]
        [TestCategory("AadAuthorityTests")]
        public void SuccessfulValidationTest()
        {
            using (var harness = CreateTestHarness())
            {
                // add mock response for instance validation
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ExpectedUrl = "https://login.microsoftonline.com/common/discovery/instance",
                        ExpectedQueryParams = new Dictionary<string, string>
                        {
                            {"api-version", "1.1"},
                            {
                                "authorization_endpoint",
                                "https%3A%2F%2Flogin.microsoftonline.in%2Fmytenant.com%2Foauth2%2Fv2.0%2Fauthorize"
                            },
                        },
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(
                            "{\"tenant_discovery_endpoint\":\"https://login.microsoftonline.in/mytenant.com/.well-known/openid-configuration\"}")
                    });

                // add mock response for tenant endpoint discovery
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ExpectedUrl = "https://login.microsoftonline.in/mytenant.com/v2.0/.well-known/openid-configuration",
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(
                           File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("OpenidConfiguration.json")))
                    });

                Authority instance = Authority.CreateAuthority(harness.ServiceBundle, "https://login.microsoftonline.in/mytenant.com", true);
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.Aad);

                var resolver = new AuthorityEndpointResolutionManager(harness.ServiceBundle);
                var endpoints = resolver.ResolveEndpointsAsync(
                    instance.AuthorityInfo,
                    null,
                    new RequestContext(harness.ServiceBundle, Guid.NewGuid()))
                    .GetAwaiter().GetResult();

                Assert.AreEqual(
                    "https://login.microsoftonline.com/6babcaad-604b-40ac-a9d7-9fd97c0b779f/oauth2/v2.0/authorize",
                    endpoints.AuthorizationEndpoint);
                Assert.AreEqual(
                    "https://login.microsoftonline.com/6babcaad-604b-40ac-a9d7-9fd97c0b779f/oauth2/v2.0/token",
                    endpoints.TokenEndpoint);
                Assert.AreEqual("https://sts.windows.net/6babcaad-604b-40ac-a9d7-9fd97c0b779f/", endpoints.SelfSignedJwtAudience);
                Assert.AreEqual("https://login.microsoftonline.in/common/userrealm/", instance.AuthorityInfo.UserRealmUriPrefix);
            }
        }

        [TestMethod]
        [TestCategory("AadAuthorityTests")]
        public void ValidationOffSuccessTest()
        {
            using (var harness = CreateTestHarness())
            {
                // add mock response for tenant endpoint discovery
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ExpectedUrl = "https://login.microsoftonline.in/mytenant.com/v2.0/.well-known/openid-configuration",
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(
                           File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("OpenidConfiguration.json")))
                    });

                Authority instance = Authority.CreateAuthority(harness.ServiceBundle, "https://login.microsoftonline.in/mytenant.com");
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.Aad);

                var resolver = new AuthorityEndpointResolutionManager(harness.ServiceBundle);
                var endpoints = resolver.ResolveEndpointsAsync(
                    instance.AuthorityInfo,
                    null,
                    new RequestContext(harness.ServiceBundle, Guid.NewGuid()))
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                Assert.AreEqual(
                    "https://login.microsoftonline.com/6babcaad-604b-40ac-a9d7-9fd97c0b779f/oauth2/v2.0/authorize",
                    endpoints.AuthorizationEndpoint);
                Assert.AreEqual(
                    "https://login.microsoftonline.com/6babcaad-604b-40ac-a9d7-9fd97c0b779f/oauth2/v2.0/token",
                    endpoints.TokenEndpoint);
                Assert.AreEqual("https://sts.windows.net/6babcaad-604b-40ac-a9d7-9fd97c0b779f/", endpoints.SelfSignedJwtAudience);
            }
        }

        [TestMethod]
        [TestCategory("AadAuthorityTests")]
        public void CreateEndpointsWithCommonTenantTest()
        {
            using (var harness = CreateTestHarness())
            {
                // add mock response for tenant endpoint discovery
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ExpectedUrl = "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration",
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(
                           File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("OpenidConfigurationCommon.json")))
                    });

                Authority instance = Authority.CreateAuthority(harness.ServiceBundle, "https://login.microsoftonline.com/common");
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.Aad);

                var resolver = new AuthorityEndpointResolutionManager(harness.ServiceBundle);
                var endpoints = resolver.ResolveEndpointsAsync(
                    instance.AuthorityInfo,
                    null,
                    new RequestContext(harness.ServiceBundle, Guid.NewGuid()))
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                Assert.AreEqual("https://login.microsoftonline.com/common/oauth2/v2.0/authorize", endpoints.AuthorizationEndpoint);
                Assert.AreEqual("https://login.microsoftonline.com/common/oauth2/v2.0/token", endpoints.TokenEndpoint);
                Assert.AreEqual("https://login.microsoftonline.com/common/v2.0", endpoints.SelfSignedJwtAudience);
            }
        }

        [TestMethod]
        [TestCategory("AadAuthorityTests")]
        public void SelfSignedJwtAudienceEndpointValidationTest()
        {
            string common = MsalTestConstants.Common;
            string tenantSpecific = MsalTestConstants.TenantId;
            string issuerCommonWithTenant = "https://login.microsoftonline.com/{tenant}/v2.0";
            string issuerCommonWithTenantId = "https://login.microsoftonline.com/{tenantid}/v2.0";
            string issuerTenantSpecific = $"https://login.microsoftonline.com/{tenantSpecific}/v2.0";
            string jwtAudienceEndpointCommon = $"https://login.microsoftonline.com/{common}/v2.0";

            CheckCorrectJwtAudienceEndpointIsCreatedFromIssuer(issuerCommonWithTenant, common, jwtAudienceEndpointCommon);
            CheckCorrectJwtAudienceEndpointIsCreatedFromIssuer(issuerCommonWithTenantId, common, jwtAudienceEndpointCommon);
            CheckCorrectJwtAudienceEndpointIsCreatedFromIssuer(issuerTenantSpecific, common, issuerTenantSpecific);
        }

        [TestMethod]
        [TestCategory("AadAuthorityTests")]
        public void FailedValidationTest()
        {
            using (var harness = CreateTestHarness())
            {
                // add mock response for instance validation
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ExpectedUrl = "https://login.microsoftonline.com/common/discovery/instance",
                        ExpectedQueryParams = new Dictionary<string, string>
                        {
                            {"api-version", "1.1"},
                            {
                                "authorization_endpoint",
                                "https%3A%2F%2Flogin.microsoft0nline.com%2Fmytenant.com%2Foauth2%2Fv2.0%2Fauthorize"
                            },
                        },
                        ResponseMessage = MockHelpers.CreateFailureMessage(
                            HttpStatusCode.BadRequest,
                            "{\"error\":\"invalid_instance\"," + "\"error_description\":\"AADSTS50049: " +
                            "Unknown or invalid instance. Trace " + "ID: b9d0894d-a9a4-4dba-b38e-8fb6a009bc00 " +
                            "Correlation ID: 34f7b4cf-4fa2-4f35-a59b" + "-54b6f91a9c94 Timestamp: 2016-08-23 " +
                            "20:45:49Z\",\"error_codes\":[50049]," + "\"timestamp\":\"2016-08-23 20:45:49Z\"," +
                            "\"trace_id\":\"b9d0894d-a9a4-4dba-b38e-8f" + "b6a009bc00\",\"correlation_id\":\"34f7b4cf-" +
                            "4fa2-4f35-a59b-54b6f91a9c94\"}")
                    });

                Authority instance = Authority.CreateAuthority(harness.ServiceBundle, "https://login.microsoft0nline.com/mytenant.com", true);
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.Aad);
                try
                {
                    var resolver = new AuthorityEndpointResolutionManager(harness.ServiceBundle);
                    var endpoints = resolver.ResolveEndpointsAsync(
                        instance.AuthorityInfo,
                        null,
                        new RequestContext(harness.ServiceBundle, Guid.NewGuid()))
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                    Assert.Fail("validation should have failed here");
                }
                catch (Exception exc)
                {
                    Assert.IsTrue(exc is MsalServiceException);
                    Assert.AreEqual(((MsalServiceException)exc).ErrorCode, "invalid_instance");
                }
            }
        }

        [TestMethod]
        [TestCategory("AadAuthorityTests")]
        public void FailedValidationMissingFieldsTest()
        {
            using (var harness = CreateTestHarness())
            {
                // add mock response for instance validation
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ExpectedUrl = "https://login.windows.net/common/discovery/instance",
                        ExpectedQueryParams = new Dictionary<string, string>
                        {
                            {"api-version", "1.0"},
                            {"authorization_endpoint", "https://login.microsoft0nline.com/mytenant.com/oauth2/v2.0/authorize"},
                        },
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage("{}")
                    });

                Authority instance = Authority.CreateAuthority(harness.ServiceBundle, "https://login.microsoft0nline.com/mytenant.com");
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.Aad);
                try
                {
                    var resolver = new AuthorityEndpointResolutionManager(harness.ServiceBundle);
                    var endpoints = resolver.ResolveEndpointsAsync(
                        instance.AuthorityInfo,
                        null,
                        new RequestContext(harness.ServiceBundle, Guid.NewGuid()))
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                    Assert.Fail("validation should have failed here");
                }
                catch (Exception exc)
                {
                    Assert.IsNotNull(exc);
                }
            }
        }

        [TestMethod]
        [TestCategory("AadAuthorityTests")]
        public void FailedTenantDiscoveryMissingEndpointsTest()
        {
            using (var harness = CreateTestHarness())
            {
                // add mock response for tenant endpoint discovery
                harness.HttpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ExpectedUrl = "https://login.microsoftonline.in/mytenant.com/v2.0/.well-known/openid-configuration",
                        ResponseMessage =
                            MockHelpers.CreateSuccessResponseMessage(
                                File.ReadAllText(ResourceHelper.GetTestResourceRelativePath("OpenidConfiguration-MissingFields.json")))
                    });

                Authority instance = Authority.CreateAuthority(harness.ServiceBundle, "https://login.microsoftonline.in/mytenant.com");
                Assert.IsNotNull(instance);
                Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.Aad);
                try
                {
                    var resolver = new AuthorityEndpointResolutionManager(harness.ServiceBundle);
                    var endpoints = resolver.ResolveEndpointsAsync(
                        instance.AuthorityInfo,
                        null,
                    new RequestContext(harness.ServiceBundle, Guid.NewGuid()))
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                    Assert.Fail("validation should have failed here");
                }
                catch (MsalClientException exc)
                {
                    Assert.AreEqual(MsalError.TenantDiscoveryFailedError, exc.ErrorCode);
                }
            }
        }

        [TestMethod]
        [TestCategory("AadAuthorityTests")]
        public void CanonicalAuthorityInitTest()
        {
            var serviceBundle = TestCommon.CreateDefaultServiceBundle();

            const string UriNoPort = "https://login.microsoftonline.in/mytenant.com";
            const string UriNoPortTailSlash = "https://login.microsoftonline.in/mytenant.com/";

            const string UriDefaultPort = "https://login.microsoftonline.in:443/mytenant.com";

            const string UriCustomPort = "https://login.microsoftonline.in:444/mytenant.com";
            const string UriCustomPortTailSlash = "https://login.microsoftonline.in:444/mytenant.com/";

            var authority = Authority.CreateAuthority(serviceBundle, UriNoPort);
            Assert.AreEqual(UriNoPortTailSlash, authority.AuthorityInfo.CanonicalAuthority);

            authority = Authority.CreateAuthority(serviceBundle, UriDefaultPort);
            Assert.AreEqual(UriNoPortTailSlash, authority.AuthorityInfo.CanonicalAuthority);

            authority = Authority.CreateAuthority(serviceBundle, UriCustomPort);
            Assert.AreEqual(UriCustomPortTailSlash, authority.AuthorityInfo.CanonicalAuthority);
        }

        [TestMethod]
        public void TenantSpecificAuthorityInitTest()
        {
            var host = String.Concat("https://", MsalTestConstants.ProductionPrefNetworkEnvironment);
            var expectedAuthority = String.Concat(host, "/" , MsalTestConstants.TenantId, "/");

            var publicClient = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                             .WithAuthority(host, MsalTestConstants.TenantId)
                                                             .BuildConcrete();

            Assert.AreEqual(publicClient.Authority, expectedAuthority);

            publicClient = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                         .WithAuthority(host, new Guid(MsalTestConstants.TenantId))
                                                         .BuildConcrete();

            Assert.AreEqual(publicClient.Authority, expectedAuthority);

            publicClient = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                         .WithAuthority(new Uri(expectedAuthority))
                                                         .BuildConcrete();

            Assert.AreEqual(publicClient.Authority, expectedAuthority);
        }

        [TestMethod]
        public void MalformedAuthorityInitTest()
        {
            PublicClientApplication publicClient = null;
            var expectedAuthority = String.Concat("https://", MsalTestConstants.ProductionPrefNetworkEnvironment, "/", MsalTestConstants.TenantId, "/");

            //Check bad URI format
            var host = String.Concat("test", MsalTestConstants.ProductionPrefNetworkEnvironment, "/");
            var fullAuthority = String.Concat(host, MsalTestConstants.TenantId);

            AssertException.Throws<UriFormatException>(() =>
            {
                publicClient = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                             .WithAuthority(fullAuthority)
                                                             .BuildConcrete();
            });

            //Check empty path segments
            host = String.Concat("https://", MsalTestConstants.ProductionPrefNetworkEnvironment, "/");
            fullAuthority = String.Concat(host, MsalTestConstants.TenantId, "//");

            publicClient = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                         .WithAuthority(host, new Guid(MsalTestConstants.TenantId))
                                                         .BuildConcrete();

            Assert.AreEqual(publicClient.Authority, expectedAuthority);

            //Check additional path segments
            fullAuthority = String.Concat(host , MsalTestConstants.TenantId, "/ABCD!@#$TEST//");

            publicClient = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId)
                                                         .WithAuthority(new Uri(fullAuthority))
                                                         .BuildConcrete();

            Assert.AreEqual(publicClient.Authority, expectedAuthority);
        }

        [TestMethod]
        public void TenantAuthorityDoesNotChange()
        {
            // no change because initial authority is tenanted
            AuthorityTestHelper.AuthorityDoesNotUpdateTenant(
                MsalTestConstants.AuthorityUtidTenant, MsalTestConstants.Utid);
        }

        [TestMethod]
        public void TenantlessAuthorityChanges()
        {
            Authority authority = AuthorityTestHelper.CreateAuthorityFromUrl(
                MsalTestConstants.AuthorityCommonTenant);

            Assert.AreEqual("common", authority.GetTenantId());

            string updatedAuthority = authority.GetTenantedAuthority(MsalTestConstants.Utid);
            Assert.AreEqual(MsalTestConstants.AuthorityUtidTenant, updatedAuthority);
            Assert.AreEqual(updatedAuthority, MsalTestConstants.AuthorityUtidTenant);

            authority.UpdateWithTenant(MsalTestConstants.Utid);
            Assert.AreEqual(authority.AuthorityInfo.CanonicalAuthority, MsalTestConstants.AuthorityUtidTenant);
        }
               
        private void CheckCorrectJwtAudienceEndpointIsCreatedFromIssuer(string issuer, string tenantId, string expectedJwtAudience)
        {
            var resolver = new AuthorityEndpointResolutionManager(null);

            TenantDiscoveryResponse tenantDiscoveryResponse = new TenantDiscoveryResponse();

            tenantDiscoveryResponse.Issuer = issuer;
            string selfSignedJwtAudience = resolver.ReplaceNonTenantSpecificValueWithTenant(tenantDiscoveryResponse, tenantId);
            Assert.AreEqual(expectedJwtAudience, selfSignedJwtAudience);
        }
    }
}
