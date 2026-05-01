// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class ComputeMetadataTests : TestBase
    {
        private const string FullComputeResponse = @"{
            ""azEnvironment"": ""AzurePublicCloud"",
            ""location"": ""westus2"",
            ""name"": ""test-vm"",
            ""osType"": ""Windows"",
            ""resourceGroupName"": ""test-rg"",
            ""resourceId"": ""/subscriptions/sub-id/resourceGroups/test-rg/providers/Microsoft.Compute/virtualMachines/test-vm"",
            ""subscriptionId"": ""sub-id"",
            ""vmId"": ""vm-guid-1234"",
            ""vmSize"": ""Standard_D4s_v5"",
            ""securityProfile"": {
                ""secureBootEnabled"": ""true"",
                ""virtualTpmEnabled"": ""true""
            }
        }";

        private const string NoSecurityProfileResponse = @"{
            ""azEnvironment"": ""AzurePublicCloud"",
            ""location"": ""eastus"",
            ""name"": ""gen1-vm"",
            ""osType"": ""Linux"",
            ""vmId"": ""vm-guid-5678"",
            ""vmSize"": ""Standard_B2s""
        }";

        private const string MinimalResponse = @"{
            ""osType"": ""Linux""
        }";

        [TestMethod]
        public async Task GetComputeMetadata_FullResponse_ParsesAllFields()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                ImdsManagedIdentitySource.ResetEndpointCacheForTest();

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(FullComputeResponse)
                    });

                var result = await ImdsComputeMetadataManager
                    .GetComputeMetadataAsync(httpManager, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("AzurePublicCloud", result.AzEnvironment);
                Assert.AreEqual("westus2", result.Location);
                Assert.AreEqual("test-vm", result.Name);
                Assert.AreEqual("Windows", result.OsType);
                Assert.AreEqual("test-rg", result.ResourceGroupName);
                Assert.AreEqual("sub-id", result.SubscriptionId);
                Assert.AreEqual("vm-guid-1234", result.VmId);
                Assert.AreEqual("Standard_D4s_v5", result.VmSize);
                Assert.IsNotNull(result.SecurityProfile);
                Assert.AreEqual("true", result.SecurityProfile.SecureBootEnabled);
                Assert.AreEqual("true", result.SecurityProfile.VirtualTpmEnabled);
            }
        }

        [TestMethod]
        public async Task GetComputeMetadata_NoSecurityProfile_ReturnsNullSecurityProfile()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                ImdsManagedIdentitySource.ResetEndpointCacheForTest();

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(NoSecurityProfileResponse)
                    });

                var result = await ImdsComputeMetadataManager
                    .GetComputeMetadataAsync(httpManager, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("AzurePublicCloud", result.AzEnvironment);
                Assert.AreEqual("eastus", result.Location);
                Assert.AreEqual("Linux", result.OsType);
                Assert.AreEqual("vm-guid-5678", result.VmId);
                Assert.IsNull(result.SecurityProfile);
            }
        }

        [TestMethod]
        public async Task GetComputeMetadata_MinimalResponse_ParsesAvailableFields()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                ImdsManagedIdentitySource.ResetEndpointCacheForTest();

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(MinimalResponse)
                    });

                var result = await ImdsComputeMetadataManager
                    .GetComputeMetadataAsync(httpManager, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.AreEqual("Linux", result.OsType);
                Assert.IsNull(result.AzEnvironment);
                Assert.IsNull(result.Location);
                Assert.IsNull(result.SecurityProfile);
            }
        }

        [TestMethod]
        public async Task GetComputeMetadata_Non200Response_ReturnsNull()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                ImdsManagedIdentitySource.ResetEndpointCacheForTest();

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = MockHelpers.CreateFailureMessage(HttpStatusCode.NotFound, "")
                    });

                var result = await ImdsComputeMetadataManager
                    .GetComputeMetadataAsync(httpManager, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNull(result);
            }
        }

        [TestMethod]
        public async Task GetComputeMetadata_InternalServerError_ReturnsNull()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                ImdsManagedIdentitySource.ResetEndpointCacheForTest();

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = MockHelpers.CreateFailureMessage(HttpStatusCode.InternalServerError, "")
                    });

                var result = await ImdsComputeMetadataManager
                    .GetComputeMetadataAsync(httpManager, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNull(result);
            }
        }

        [TestMethod]
        public async Task GetComputeMetadata_HttpException_ReturnsNull()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                ImdsManagedIdentitySource.ResetEndpointCacheForTest();

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = MockHelpers.CreateFailureMessage(HttpStatusCode.GatewayTimeout, ""),
                        ExceptionToThrow = new HttpRequestException("IMDS unreachable")
                    });

                var result = await ImdsComputeMetadataManager
                    .GetComputeMetadataAsync(httpManager, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNull(result);
            }
        }

        [TestMethod]
        public async Task GetComputeMetadata_SecurityProfileDisabled_ReturnsCorrectValues()
        {
            string response = @"{
                ""osType"": ""Windows"",
                ""securityProfile"": {
                    ""secureBootEnabled"": ""false"",
                    ""virtualTpmEnabled"": ""false""
                }
            }";

            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                ImdsManagedIdentitySource.ResetEndpointCacheForTest();

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage(response)
                    });

                var result = await ImdsComputeMetadataManager
                    .GetComputeMetadataAsync(httpManager, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result.SecurityProfile);
                Assert.AreEqual("false", result.SecurityProfile.SecureBootEnabled);
                Assert.AreEqual("false", result.SecurityProfile.VirtualTpmEnabled);
            }
        }

        [TestMethod]
        public async Task GetComputeMetadata_EmptyJsonResponse_ReturnsEmptyObject()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                ImdsManagedIdentitySource.ResetEndpointCacheForTest();

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = MockHelpers.CreateSuccessResponseMessage("{}")
                    });

                var result = await ImdsComputeMetadataManager
                    .GetComputeMetadataAsync(httpManager, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
                Assert.IsNull(result.OsType);
                Assert.IsNull(result.SecurityProfile);
            }
        }

        [TestMethod]
        public async Task GetComputeMetadata_BadRequest_ReturnsNull()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                ImdsManagedIdentitySource.ResetEndpointCacheForTest();

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        ExpectedMethod = HttpMethod.Get,
                        ResponseMessage = MockHelpers.CreateFailureMessage(HttpStatusCode.BadRequest, "{\"error\":\"bad api version\"}")
                    });

                var result = await ImdsComputeMetadataManager
                    .GetComputeMetadataAsync(httpManager, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNull(result);
            }
        }

        [TestMethod]
        public async Task GetComputeMetadata_VerifiesCorrectEndpointAndHeaders()
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                ImdsManagedIdentitySource.ResetEndpointCacheForTest();

                var handler = new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Get,
                    ExpectedUrl = "http://169.254.169.254/metadata/instance/compute",
                    ExpectedQueryParams = new Dictionary<string, string>
                    {
                        { "api-version", "2021-02-01" }
                    },
                    ExpectedRequestHeaders = new Dictionary<string, string>
                    {
                        { "Metadata", "true" }
                    },
                    ResponseMessage = MockHelpers.CreateSuccessResponseMessage(MinimalResponse)
                };
                httpManager.AddMockHandler(handler);

                var result = await ImdsComputeMetadataManager
                    .GetComputeMetadataAsync(httpManager, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(result);
            }
        }
    }
}
