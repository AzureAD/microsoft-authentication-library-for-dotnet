// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.InstanceTests
{
    [TestClass]
    [DeploymentItem("Resources\\OpenidConfiguration.json")]
    [DeploymentItem("Resources\\OpenidConfiguration-MissingFields.json")]
    [DeploymentItem("Resources\\OpenidConfigurationCommon.json")]
    public class AadAuthorityTests : TestBase
    {
        [TestMethod]
        public void ImmutableTest()
        {
            CoreAssert.IsImmutable<AadAuthority>();
            CoreAssert.IsImmutable<AdfsAuthority>();
            CoreAssert.IsImmutable<B2CAuthority>();
        }

        [TestMethod]
        public async Task CreateEndpointsWithCommonTenantAsync()
        {
            using var harness = CreateTestHarness();
            RequestContext requestContext = new RequestContext(harness.ServiceBundle, Guid.NewGuid());

            Authority instance = Authority.CreateAuthority("https://login.microsoftonline.com/common");
            Assert.IsNotNull(instance);
            Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.Aad);

            Assert.AreEqual(
                "https://login.microsoftonline.com/common/oauth2/v2.0/authorize", 
                await instance.GetAuthorizationEndpointAsync(requestContext).ConfigureAwait(false));
            Assert.AreEqual("https://login.microsoftonline.com/common/oauth2/v2.0/token", 
                await instance.GetTokenEndpointAsync(requestContext).ConfigureAwait(false));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task FailedValidationTestAsync(bool isInstanceDiscoveryEnabled)
        {
            using var harness = CreateTestHarness(isInstanceDiscoveryEnabled: isInstanceDiscoveryEnabled);

            if (isInstanceDiscoveryEnabled)
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
            }

            Authority instance = Authority.CreateAuthority("https://login.microsoft0nline.com/mytenant.com", true);
            Assert.IsNotNull(instance);
            Assert.AreEqual(instance.AuthorityInfo.AuthorityType, AuthorityType.Aad);

            TestCommon.CreateServiceBundleWithCustomHttpManager(harness.HttpManager, authority: instance.AuthorityInfo.CanonicalAuthority.ToString(), validateAuthority: true);
            try
            {
                AuthorityManager am = new AuthorityManager(new RequestContext(harness.ServiceBundle, Guid.NewGuid()), instance);
                await am.RunInstanceDiscoveryAndValidationAsync().ConfigureAwait(false);

                if (isInstanceDiscoveryEnabled)
                {
                    Assert.Fail("Validation should have failed with an exception when instance discovery is enabled.");
                }
                
                Assert.IsNotNull(am);
            }
            catch (Exception exc)
            {
                Assert.IsTrue(exc is MsalServiceException);
                Assert.AreEqual(((MsalServiceException)exc).ErrorCode, "invalid_instance");
            }
        }

        [TestMethod]
        [DataRow("https://login.microsoftonline.in/mytenant.com/", "https://login.microsoftonline.in/mytenant.com", DisplayName = "UriNoPort")]
        [DataRow("https://login.microsoftonline.in/mytenant.com/", "https://login.microsoftonline.in:443/mytenant.com", DisplayName = "UriDefaultPort")]
        [DataRow("https://login.microsoftonline.in:444/mytenant.com/", "https://login.microsoftonline.in:444/mytenant.com", DisplayName = "UriCustomPort")]
        public void CanonicalAuthorityInitTest(string expected, string input)
        {
            var authority = Authority.CreateAuthority(input);
            Assert.AreEqual(new Uri(expected), authority.AuthorityInfo.CanonicalAuthority);
        }

        [TestMethod]
        public void TenantSpecificAuthorityInitTest()
        {
            var host = string.Concat("https://", TestConstants.ProductionPrefNetworkEnvironment);
            var expectedAuthority = string.Concat(host, "/", TestConstants.TenantId, "/");

            var publicClient = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                             .WithAuthority(host, TestConstants.TenantId)
                                                             .BuildConcrete();

            Assert.AreEqual(publicClient.Authority, expectedAuthority);

            publicClient = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                         .WithAuthority(host, new Guid(TestConstants.TenantId))
                                                         .BuildConcrete();

            Assert.AreEqual(publicClient.Authority, expectedAuthority);

            publicClient = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                         .WithAuthority(new Uri(expectedAuthority))
                                                         .BuildConcrete();

            Assert.AreEqual(publicClient.Authority, expectedAuthority);
        }

        [TestMethod]
        public void MalformedAuthorityInitTest()
        {
            PublicClientApplication publicClient = null;
            var expectedAuthority = string.Concat("https://", TestConstants.ProductionPrefNetworkEnvironment, "/", TestConstants.TenantId, "/");

            //Check bad URI format
            var host = string.Concat("test", TestConstants.ProductionPrefNetworkEnvironment, "/");
            var fullAuthority = string.Concat(host, TestConstants.TenantId);

            AssertException.Throws<ArgumentException>(() =>
            {
                publicClient = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                             .WithAuthority(fullAuthority)
                                                             .BuildConcrete();
            });

            //Check empty path segments
            host = string.Concat("https://", TestConstants.ProductionPrefNetworkEnvironment, "/");
            fullAuthority = string.Concat(host, TestConstants.TenantId, "//");

            publicClient = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                         .WithAuthority(host, new Guid(TestConstants.TenantId))
                                                         .BuildConcrete();

            Assert.AreEqual(publicClient.Authority, expectedAuthority);

            //Check additional path segments
            fullAuthority = string.Concat(host, TestConstants.TenantId, "/ABCD!@#$TEST//");

            publicClient = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                         .WithAuthority(new Uri(fullAuthority))
                                                         .BuildConcrete();

            Assert.AreEqual(publicClient.Authority, expectedAuthority);
        }

        [TestMethod]
        public void CheckConsistentAuthorityTypeUriAndString()
        {
            ValidateAuthorityType(TestConstants.AadAuthorityWithTestTenantId, AuthorityType.Aad);
            ValidateAuthorityType(TestConstants.AuthorityCommonTenant, AuthorityType.Aad);
            ValidateAuthorityType(TestConstants.B2CAuthority, AuthorityType.B2C);
            ValidateAuthorityType(TestConstants.ADFSAuthority, AuthorityType.Adfs);
        }

        private static void ValidateAuthorityType(string inputAuthority, AuthorityType expectedAuthorityType)
        {
            var pca1 = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                     .WithAuthority(new Uri(inputAuthority))
                                                     .BuildConcrete();
            var pca2 = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                     .WithAuthority(inputAuthority)
                                                     .BuildConcrete();

            Assert.AreEqual(
                expectedAuthorityType,
                ((ApplicationConfiguration)pca1.AppConfig).Authority.AuthorityInfo.AuthorityType);
            Assert.AreEqual(
                expectedAuthorityType,
                ((ApplicationConfiguration)pca2.AppConfig).Authority.AuthorityInfo.AuthorityType);
        }

        [TestMethod]
        public void CreateAuthorityFromTenantedWithTenantTest()
        {
            Authority authority = AuthorityTestHelper.CreateAuthorityFromUrl("https://login.microsoft.com/tid");

            string updatedAuthority = authority.GetTenantedAuthority("other_tenant_id");
            Assert.AreEqual("https://login.microsoft.com/tid/", updatedAuthority, "Not changed, original authority already has tenant id");

            string updatedAuthority2 = authority.GetTenantedAuthority("other_tenant_id", true);
            Assert.AreEqual("https://login.microsoft.com/other_tenant_id/", updatedAuthority2, "Changed with forced flag");
        }

        [TestMethod]
        public void CreateAuthorityFromCommonWithTenantTest()
        {
            Authority authority = AuthorityTestHelper.CreateAuthorityFromUrl("https://login.microsoft.com/common");

            string updatedAuthority = authority.GetTenantedAuthority("other_tenant_id");
            Assert.AreEqual("https://login.microsoft.com/other_tenant_id/", updatedAuthority, "Changed, original is common");

            string updatedAuthority2 = authority.GetTenantedAuthority("other_tenant_id", true);
            Assert.AreEqual("https://login.microsoft.com/other_tenant_id/", updatedAuthority2, "Changed with forced flag");
        }

        [TestMethod]
        public void TenantlessAuthorityChanges()
        {
            Authority authority = AuthorityTestHelper.CreateAuthorityFromUrl(
                TestConstants.AuthorityCommonTenant);

            Assert.AreEqual("common", authority.TenantId);

            string updatedAuthority = authority.GetTenantedAuthority(TestConstants.Utid);
            Assert.AreEqual(TestConstants.AuthorityUtidTenant, updatedAuthority);
            Assert.AreEqual(updatedAuthority, TestConstants.AuthorityUtidTenant);

            authority = Authority.CreateAuthorityWithTenant(
              authority.AuthorityInfo,
              TestConstants.Utid);

            Assert.AreEqual(authority.AuthorityInfo.CanonicalAuthority, TestConstants.AuthorityUtidTenant);
        }

        [TestMethod]
        //Test for bug #1292 (https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1292)
        public void AuthorityCustomPortTest()
        {
            const string customPortAuthority = "https://localhost:5215/common/";

            using var harness = CreateTestHarness();
            harness.HttpManager.AddInstanceDiscoveryMockHandler(customPortAuthority);

            PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                        .WithAuthority(new Uri(customPortAuthority), false)
                                                                        .WithHttpManager(harness.HttpManager)
                                                                        .BuildConcrete();

            //Ensure that the PublicClientApplication init does not remove the port from the authority
            Assert.AreEqual(customPortAuthority, app.Authority);

            app.ServiceBundle.ConfigureMockWebUI(
                AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));

            harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(customPortAuthority);

            AuthenticationResult result = app.AcquireTokenInteractive(TestConstants.s_scope)
                                             .ExecuteAsync(CancellationToken.None)
                                             .Result;

            //Ensure that acquiring a token does not remove the port from the authority
            Assert.AreEqual(customPortAuthority, app.Authority);
        }
    }
}
