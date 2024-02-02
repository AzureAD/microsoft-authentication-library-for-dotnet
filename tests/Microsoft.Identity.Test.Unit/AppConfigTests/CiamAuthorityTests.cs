// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.WebRequestMethods;

namespace Microsoft.Identity.Test.Unit.AppConfigTests
{
    [TestClass]
    public class CiamAuthorityTests : TestBase
    {
        private readonly string _ciamInstance = $"https://idgciamdemo{Constants.CiamAuthorityHostSuffix}";
        private readonly string _ciamTenantGuid = "5e156ef5-9bd2-480c-9de0-d8658f21d3f7";
        private readonly string _ciamTenant = "idgciamdemo.onmicrosoft.com";

        // Possible CIAM authorities:
        // https://idgciamdemo.ciamlogin.com/idgciamdemo.onmicrosoft.com
        // https://idgciamdemo.ciamlogin.com
        // https://idgciamdemo.ciamlogin.com/5e156ef5-9bd2-480c-9de0-d8658f21d3f7
        [TestMethod]
        public void CiamAuthorityAdapater_WithAuthorityAndNamedTenantTest()
        {
            // Arrange
            string ciamAuthority = _ciamInstance + '/' + _ciamTenant;

            // Act
            var transformedAuthority = CiamAuthority.TransformAuthority(new Uri(ciamAuthority));

            // Assert
            Assert.AreEqual(ciamAuthority, transformedAuthority.AbsoluteUri);
        }

        [TestMethod]
        public void CiamAuthorityAdapater_WithAuthorityAndTenantGUIDTest()
        {
            // Arrange
            string ciamAuthority = _ciamInstance + '/' + _ciamTenantGuid;

            // Act
            var transformedAuthority = CiamAuthority.TransformAuthority(new Uri(ciamAuthority));

            // Assert
            Assert.AreEqual(ciamAuthority, transformedAuthority.AbsoluteUri);
        }

        [TestMethod]
        public void CiamAuthorityAdapater_WithCiamLoginTest()
        {
            // Arrange
            string ciamAuthority = _ciamInstance + "/";
            string ciamTenant = "idgciamdemo.onmicrosoft.com";
            string ciamTransformedAuthority = _ciamInstance + "/" + ciamTenant;

            // Act
            var transformedAuthority = CiamAuthority.TransformAuthority(new Uri(ciamAuthority));

            // Assert
            Assert.AreEqual(ciamTransformedAuthority, transformedAuthority.AbsoluteUri);
        }

        [TestMethod]
        public void CiamAuthorityAdapater_WithInstanceAndTenantTest()
        {
            // Arrange
            string ciamInstance = _ciamInstance;
            string ciamTenant = "idgciamdemo.onmicrosoft.com";
            string ciamAuthority = ciamInstance + '/' + ciamTenant;

            // Act
            var transformedAuthority = CiamAuthority.TransformAuthority(new Uri(ciamInstance + ciamTenant));

            // Assert
            Assert.AreEqual(ciamAuthority, transformedAuthority.AbsoluteUri);
        }

        [TestMethod]
        public void CiamAuthorityAdapater_WithInstanceAndNullTenantTest()
        {
            // Arrange
            string ciamInstance = _ciamInstance + "/";
            string ciamTenant = "idgciamdemo.onmicrosoft.com";
            string ciamTransformedAuthority = _ciamInstance + "/" + ciamTenant;

            // Act
            var transformedAuthority = CiamAuthority.TransformAuthority(new Uri(ciamInstance + ciamTenant));

            // Assert
            Assert.AreEqual(ciamTransformedAuthority, transformedAuthority.AbsoluteUri);
        }

        [DataTestMethod]
        [DataRow("https://idgciamdemo.ciamlogin.com/", "https://idgciamdemo.ciamlogin.com/idgciamdemo.onmicrosoft.com/")]
        [DataRow("https://idgciamdemo.ciamlogin.com/d57fb3d4-4b5a-4144-9328-9c1f7d58179d", "https://idgciamdemo.ciamlogin.com/d57fb3d4-4b5a-4144-9328-9c1f7d58179d/")]
        [DataRow("https://idgciamdemo.ciamlogin.com/idgciamdemo.onmicrosoft.com", "https://idgciamdemo.ciamlogin.com/idgciamdemo.onmicrosoft.com/")]
        [DataRow("https://idgciamdemo.ciamlogin.com/aDomain", "https://idgciamdemo.ciamlogin.com/adomain/")]
        public void CiamWithAuthorityTransformationTest(string authority, string expectedAuthority)
        {
            string effectiveAuthority =
            PublicClientApplicationBuilder.Create(Guid.NewGuid().ToString())
                                                    .WithAuthority(authority)
                                                    .WithDefaultRedirectUri()
                                                    .Build()
                                                    .Authority;

            Assert.AreEqual(expectedAuthority, effectiveAuthority);
        }

        [DataTestMethod]
        [DataRow("https://app.ciamlogin.com/")]
        //[DataRow("https://app.ciamlogin.com/d57fb3d4-4b5a-4144-9328-9c1f7d58179d")]
        //[DataRow("https://app.ciamlogin.com/aDomain")]
        public async Task CiamWithAuthorityRequestTestAsync(string appAuthority)
        {

            using (var harness = base.CreateTestHarness())
            {
                await TestRequestAuthorityAsync(
                    harness,
                    appAuthority,
                    requestAuthority: "https://request.ciamlogin.com/",
                    expectedEndpoint: "https://request.ciamlogin.com/request.onmicrosoft.com/oauth2/v2.0/token"
                    ).ConfigureAwait(false);

                await TestRequestAuthorityAsync(
                    harness,
                    appAuthority,
                    requestAuthority: $"https://request.ciamlogin.com/{TestConstants.TenantId2}",
                    expectedEndpoint: $"https://request.ciamlogin.com/{TestConstants.TenantId2}/oauth2/v2.0/token"
                    ).ConfigureAwait(false);

                await TestRequestAuthorityAsync(
                    harness,
                    appAuthority,
                    requestAuthority: $"https://request.ciamlogin.com/bDomain",
                    expectedEndpoint: $"https://request.ciamlogin.com/bdomain/oauth2/v2.0/token"
                    ).ConfigureAwait(false);

                
            }

            static async Task TestRequestAuthorityAsync(MockHttpAndServiceBundle harness, string appAuthority, string requestAuthority, string expectedEndpoint)
            {
                var app = ConfidentialClientApplicationBuilder.Create(Guid.NewGuid().ToString())
                                                                    .WithAuthority(appAuthority)
                                                                    .WithClientSecret("secret")
                                                                    .WithHttpManager(harness.HttpManager)
                                                                    .Build();

                var handler = new MockHttpMessageHandler()
                {
                    ExpectedUrl = expectedEndpoint,
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                };

                harness.HttpManager.AddMockHandler(handler);

#pragma warning disable CS0618 // Type or member is obsolete
                var result = await app.AcquireTokenOnBehalfOf(new[] { "someScope" }, new UserAssertion("some_assertion"))
                     .WithAuthority(requestAuthority)
                     .ExecuteAsync()
                     .ConfigureAwait(false);
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }

        [DataTestMethod]
        [DataRow("https://app.ciamlogin.com/")]
        [DataRow("https://app.ciamlogin.com/d57fb3d4-4b5a-4144-9328-9c1f7d58179d")]
        [DataRow("https://app.ciamlogin.com/aDomain")]
        public async Task CiamWithTenantRequestTestAsync(string appAuthority)
        {

            using (var harness = base.CreateTestHarness())
            {
                var app = ConfidentialClientApplicationBuilder.Create(Guid.NewGuid().ToString())
                                                                   .WithAuthority(appAuthority)
                                                                   .WithClientSecret("secret")
                                                                   .WithHttpManager(harness.HttpManager)
                                                                   .Build();

                var handler = new MockHttpMessageHandler()
                {
                    ExpectedUrl = $"https://app.ciamlogin.com/{TestConstants.TenantId2}/oauth2/v2.0/token",
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(),
                };

                harness.HttpManager.AddMockHandler(handler);

                
                var result = await app.AcquireTokenOnBehalfOf(new[] { "someScope" }, new UserAssertion("some_assertion"))
                     .WithTenantId(TestConstants.TenantId2)
                     .ExecuteAsync()
                     .ConfigureAwait(false);
            }
        }
    }
}
