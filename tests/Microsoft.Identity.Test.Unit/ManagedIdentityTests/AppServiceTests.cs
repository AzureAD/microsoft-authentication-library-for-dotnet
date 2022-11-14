// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class AppServiceTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [DataTestMethod]
        [DataRow("http://127.0.0.1:41564/msi/token/", "https://management.azure.com", "https://management.azure.com")]
        [DataRow("http://127.0.0.1:41564/msi/token", "https://management.azure.com", "https://management.azure.com")]
        [DataRow("http://127.0.0.1:41564/msi/token", "https://management.azure.com/.default", "https://management.azure.com")]
        public async Task AppServiceHappyPathAsync(string endpoint, string scope, string resource)
        {
            Environment.SetEnvironmentVariable("IDENTITY_ENDPOINT", endpoint);
            Environment.SetEnvironmentVariable("IDENTITY_HEADER", "secret");

            using (var httpManager = new MockHttpManager())
            {
                IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                    .Create("clientId")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures()
                    .Build();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddManagedIdentityMockHandler(endpoint, resource, MockHelpers.GetMsiSuccessfulResponse());

                var result = await cca.AcquireTokenForClient(new string[] { scope })
                    .WithManagedIdentity()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNotNull(result.AccessToken);
                Assert.AreEqual(TokenSource.IdentityProvider, result.AuthenticationResultMetadata.TokenSource);

                result = await cca.AcquireTokenForClient(new string[] { scope })
                    .WithManagedIdentity()
                    .ExecuteAsync().ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual(TokenSource.Cache, result.AuthenticationResultMetadata.TokenSource);
            }
        }

        [DataTestMethod]
        [DataRow("user.read")]
        [DataRow("https://management.core.windows.net//user_impersonation")]
        [DataRow("s")]
        public async Task AppServiceTestWrongScopeAsync(string resource)
        {
            Environment.SetEnvironmentVariable("IDENTITY_ENDPOINT", "http://127.0.0.1:41564/msi/token");
            Environment.SetEnvironmentVariable("IDENTITY_HEADER", "secret");

            using (var httpManager = new MockHttpManager())
            {
                IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                    .Create("clientId")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures()
                    .Build();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddManagedIdentityMockHandler("http://127.0.0.1:41564/msi/token", resource, MockHelpers.GetMsiErrorResponse(), HttpStatusCode.InternalServerError);
                httpManager.AddManagedIdentityMockHandler("http://127.0.0.1:41564/msi/token", resource, MockHelpers.GetMsiErrorResponse(), HttpStatusCode.InternalServerError);

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () => 
                    await cca.AcquireTokenForClient(new string[] { resource })
                    .WithManagedIdentity()
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                Assert.AreEqual("An unexpected error occured while fetching the AAD Token.", ex.Message);
            }
        }

        [TestMethod]
        public async Task AppServiceNullResponseAsync()
        {
            Environment.SetEnvironmentVariable("IDENTITY_ENDPOINT", "http://127.0.0.1:41564/msi/token");
            Environment.SetEnvironmentVariable("IDENTITY_HEADER", "secret");

            using (var httpManager = new MockHttpManager())
            {
                IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                    .Create("clientId")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures()
                    .Build();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddManagedIdentityMockHandler("http://127.0.0.1:41564/msi/token", "https://management.azure.com", "", HttpStatusCode.OK);

                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await cca.AcquireTokenForClient(new string[] { "https://management.azure.com" })
                    .WithManagedIdentity()
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.InvalidManagedIdentityResponse, ex.ErrorCode);
                Assert.AreEqual("Invalid response, the authentication response was not in the expected format.", ex.Message);
            }
        }

        [TestMethod]
        public async Task AppServiceInvalidEndpointAsync()
        {
            Environment.SetEnvironmentVariable("IDENTITY_ENDPOINT", "127.0.0.1:41564/msi/token");
            Environment.SetEnvironmentVariable("IDENTITY_HEADER", "secret");

            using (var httpManager = new MockHttpManager())
            {
                IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                    .Create("clientId")
                    .WithHttpManager(httpManager)
                    .WithExperimentalFeatures()
                    .Build();

                httpManager.AddInstanceDiscoveryMockHandler();
                httpManager.AddManagedIdentityMockHandler("http://127.0.0.1:41564/msi/token", "https://management.azure.com", "", HttpStatusCode.OK);

                MsalClientException ex = await Assert.ThrowsExceptionAsync<MsalClientException>(async () =>
                    await cca.AcquireTokenForClient(new string[] { "https://management.azure.com" })
                    .WithManagedIdentity()
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(MsalError.InvalidManagedIdentityEndpoint, ex.ErrorCode);
            }
        }
    }
}
