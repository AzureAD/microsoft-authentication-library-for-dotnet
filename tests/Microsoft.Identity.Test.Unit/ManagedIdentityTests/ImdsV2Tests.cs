// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class ImdsV2Tests : TestBase
    {
        private readonly TestRetryPolicyFactory _testRetryPolicyFactory = new TestRetryPolicyFactory();

        [TestMethod]
        public async Task GetCsrMetadataAsyncSucceeds()
        {
            using (var httpManager = new MockHttpManager())
            {
                var handler = httpManager.AddMockHandler(MockHelpers.MockCsrResponse());

                var managedIdentityApp = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .Build();

                var miSource = await (managedIdentityApp as ManagedIdentityApplication).GetManagedIdentitySourceAsync().ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.ImdsV2, miSource);

                Assert.IsTrue(handler.ActualRequestHeaders.Contains("Metadata"));
                Assert.IsTrue(handler.ActualRequestHeaders.Contains("x-ms-client-request-id"));
                Assert.IsTrue(handler.ActualRequestMessage.RequestUri.Query.Contains("api-version"));
            }
        }

        [TestMethod]
        public async Task GetCsrMetadataAsyncSucceedsAfterRetry()
        {
            using (var httpManager = new MockHttpManager())
            {
                // First attempt fails with INTERNAL_SERVER_ERROR (500)
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse(HttpStatusCode.InternalServerError));

                // Second attempt succeeds
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse());

                var managedIdentityApp = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .Build();

                var miSource = await (managedIdentityApp as ManagedIdentityApplication).GetManagedIdentitySourceAsync().ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.ImdsV2, miSource);
            }
        }

        [TestMethod]
        public async Task GetCsrMetadataAsyncFailsWithMissingServerHeader()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse(responseServerHeader: null));

                var managedIdentityApp = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .Build();

                var miSource = await (managedIdentityApp as ManagedIdentityApplication).GetManagedIdentitySourceAsync().ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.DefaultToImds, miSource);
            }
        }

        [TestMethod]
        public async Task GetCsrMetadataAsyncFailsWithInvalidVersion()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddMockHandler(MockHelpers.MockCsrResponse(responseServerHeader: "IMDS/150.870.65.1324"));

                var managedIdentityApp = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .Build();

                var miSource = await (managedIdentityApp as ManagedIdentityApplication).GetManagedIdentitySourceAsync().ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.DefaultToImds, miSource);
            }
        }

        [TestMethod]
        public async Task GetCsrMetadataAsyncFailsAfterMaxRetries()
        {
            using (var httpManager = new MockHttpManager())
            {
                var managedIdentityApp = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .Build();

                const int Num500Errors = 1 + TestCsrMetadataProbeRetryPolicy.ExponentialStrategyNumRetries;
                for (int i = 0; i < Num500Errors; i++)
                {
                    httpManager.AddMockHandler(MockHelpers.MockCsrResponse(HttpStatusCode.InternalServerError));
                }

                var miSource = await (managedIdentityApp as ManagedIdentityApplication).GetManagedIdentitySourceAsync().ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.DefaultToImds, miSource);
            }
        }

        [TestMethod]
        public async Task GetCsrMetadataAsyncFails404WhichIsNonRetriableAndRetryPolicyIsNotTriggeredAsync()
        {
            using (var httpManager = new MockHttpManager())
            {
                var managedIdentityApp = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager)
                    .WithRetryPolicyFactory(_testRetryPolicyFactory)
                    .Build();

                httpManager.AddMockHandler(MockHelpers.MockCsrResponse(HttpStatusCode.NotFound));

                var miSource = await (managedIdentityApp as ManagedIdentityApplication).GetManagedIdentitySourceAsync().ConfigureAwait(false);
                Assert.AreEqual(ManagedIdentitySource.DefaultToImds, miSource);
            }
        }

        [TestMethod]
        public void TestCsrGeneration()
        {
            var cuid = new CuidInfo
            {
                Vmid = "test-vm-id-12345",
                Vmssid = "test-vmss-id-67890"
            };

            string clientId = "12345678-1234-1234-1234-123456789012";
            string tenantId = "87654321-4321-4321-4321-210987654321";

            // Generate CSR
            var csrRequest = CsrRequest.Generate(clientId, tenantId, cuid);

            // Validate the CSR contents using the helper
            CsrValidator.ValidateCsrContent(csrRequest.Pem, clientId, tenantId, cuid);
        }

        [DataTestMethod]
        [DataRow(null, "87654321-4321-4321-4321-210987654321", DisplayName = "Null ClientId")]
        [DataRow("", "87654321-4321-4321-4321-210987654321", DisplayName = "Empty ClientId")]
        [DataRow("   ", "87654321-4321-4321-4321-210987654321", DisplayName = "Whitespace ClientId")]
        [DataRow("12345678-1234-1234-1234-123456789012", null, DisplayName = "Null TenantId")]
        [DataRow("12345678-1234-1234-1234-123456789012", "", DisplayName = "Empty TenantId")]
        [DataRow("12345678-1234-1234-1234-123456789012", "   ", DisplayName = "Whitespace TenantId")]
        public void TestCsrGeneration_InvalidParameters(string clientId, string tenantId)
        {
            var cuid = new CuidInfo
            {
                Vmid = "test-vm-id-12345",
                Vmssid = "test-vmss-id-67890"
            };

            Assert.ThrowsException<ArgumentException>(() => 
                CsrRequest.Generate(clientId, tenantId, cuid));
        }

        [TestMethod]
        public void TestCsrGeneration_NullCuid()
        {
            string clientId = "12345678-1234-1234-1234-123456789012";
            string tenantId = "87654321-4321-4321-4321-210987654321";

            // Test with null CUID
            Assert.ThrowsException<ArgumentNullException>(() => 
                CsrRequest.Generate(clientId, tenantId, null));
        }

        [DataTestMethod]
        [DataRow(null, "test-vmss-id-67890", DisplayName = "Null VMID")]
        [DataRow("", "test-vmss-id-67890", DisplayName = "Empty VMID")]
        public void TestCsrGeneration_InvalidVmid(string vmid, string vmssid)
        {
            string clientId = "12345678-1234-1234-1234-123456789012";
            string tenantId = "87654321-4321-4321-4321-210987654321";

            var cuid = new CuidInfo
            {
                Vmid = vmid,
                Vmssid = vmssid
            };

            // Should throw ArgumentException since Vmid is required
            Assert.ThrowsException<ArgumentException>(() => 
                CsrRequest.Generate(clientId, tenantId, cuid));
        }

        [DataTestMethod]
        [DataRow("test-vm-id-12345", null, DisplayName = "Null VMSSID")]
        [DataRow("test-vm-id-12345", "", DisplayName = "Empty VMSSID")]
        public void TestCsrGeneration_OptionalVmssid(string vmid, string vmssid)
        {
            string clientId = "12345678-1234-1234-1234-123456789012";
            string tenantId = "87654321-4321-4321-4321-210987654321";

            var cuid = new CuidInfo
            {
                Vmid = vmid,
                Vmssid = vmssid
            };

            // Should succeed since Vmssid is optional (Vmid is provided and valid)
            var csrRequest = CsrRequest.Generate(clientId, tenantId, cuid);
            Assert.IsNotNull(csrRequest);
            Assert.IsFalse(string.IsNullOrWhiteSpace(csrRequest.Pem));

            // Validate the CSR contents - this should handle null/empty VMSSID gracefully
            CsrValidator.ValidateCsrContent(csrRequest.Pem, clientId, tenantId, cuid);
        }

        [TestMethod]
        public void TestCsrGeneration_MalformedPem_FormatException()
        {
            string malformedPem = "-----BEGIN CERTIFICATE REQUEST-----\nInvalid@#$%Base64Content!\n-----END CERTIFICATE REQUEST-----";
            Assert.ThrowsException<FormatException>(() => 
                TestCsrValidator.ParseCsrFromPem(malformedPem));
        }

        [DataTestMethod]
        [DataRow("-----BEGIN CERTIFICATE-----\nTUlJQzNqQ0NBY1lDQVFBd1pURT0K\n-----END CERTIFICATE REQUEST-----", DisplayName = "Wrong Headers")]
        [DataRow("", DisplayName = "Empty PEM")]
        [DataRow(null, DisplayName = "Null PEM")]
        public void TestCsrGeneration_MalformedPem_ArgumentException(string malformedPem)
        {
            Assert.ThrowsException<ArgumentException>(() => 
                TestCsrValidator.ParseCsrFromPem(malformedPem));
        }
    }
}
