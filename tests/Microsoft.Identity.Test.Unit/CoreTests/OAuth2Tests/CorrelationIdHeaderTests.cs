// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.OAuth2Tests
{
    [TestClass]
    public class CorrelationIdHeaderTests : TestBase
    {
        [TestMethod]
        public async Task CorrelationIdHeader_Present_ValidatesSuccessfully_Async()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var correlationId = Guid.NewGuid();

                // Create a mock response with matching correlation ID in header
                var responseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage();
                responseMessage.Headers.Add("client-request-id", correlationId.ToString());

                harness.HttpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = responseMessage
                });

                OAuth2Client client = new OAuth2Client(harness.ServiceBundle.ApplicationLogger, harness.HttpManager, null);

                // Act - This should not throw
                MsalTokenResponse response = await client.GetTokenAsync(
                    new Uri(TestConstants.AuthorityCommonTenant + "oauth2/v2.0/token"),
                    new RequestContext(harness.ServiceBundle, correlationId, null),
                    addCommonHeaders: true,
                    onBeforePostRequestHandler: null).ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(response);
            }
        }

        [TestMethod]
        public async Task CorrelationIdHeader_Missing_DoesNotThrow_Async()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var correlationId = Guid.NewGuid();

                // Create a mock response WITHOUT correlation ID header (simulating Container Apps scenario)
                var responseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage();
                // Intentionally NOT adding client-request-id header

                harness.HttpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = responseMessage
                });

                OAuth2Client client = new OAuth2Client(harness.ServiceBundle.ApplicationLogger, harness.HttpManager, null);

                // Act - This should not throw even though header is missing
                MsalTokenResponse response = await client.GetTokenAsync(
                    new Uri(TestConstants.AuthorityCommonTenant + "oauth2/v2.0/token"),
                    new RequestContext(harness.ServiceBundle, correlationId, null),
                    addCommonHeaders: true,
                    onBeforePostRequestHandler: null).ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(response);
            }
        }

        [TestMethod]
        public async Task CorrelationIdHeader_MultipleHeaders_MissingCorrelationId_DoesNotThrow_Async()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var correlationId = Guid.NewGuid();

                // Create a mock response with multiple headers but WITHOUT correlation ID
                var responseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage();
                responseMessage.Headers.Add("x-custom-header", "value1");
                responseMessage.Headers.Add("x-another-header", "value2");
                responseMessage.Headers.Add("x-ms-request-id", Guid.NewGuid().ToString());
                // Intentionally NOT adding client-request-id header

                harness.HttpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = responseMessage
                });

                OAuth2Client client = new OAuth2Client(harness.ServiceBundle.ApplicationLogger, harness.HttpManager, null);

                // Act - This should not throw even with multiple headers present
                MsalTokenResponse response = await client.GetTokenAsync(
                    new Uri(TestConstants.AuthorityCommonTenant + "oauth2/v2.0/token"),
                    new RequestContext(harness.ServiceBundle, correlationId, null),
                    addCommonHeaders: true,
                    onBeforePostRequestHandler: null).ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(response);
            }
        }

        [TestMethod]
        public async Task CorrelationIdHeader_EmptyValue_DoesNotThrow_Async()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var correlationId = Guid.NewGuid();

                // Create a mock response with empty correlation ID header value
                var responseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage();
                responseMessage.Headers.Add("client-request-id", string.Empty);

                harness.HttpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = responseMessage
                });

                OAuth2Client client = new OAuth2Client(harness.ServiceBundle.ApplicationLogger, harness.HttpManager, null);

                // Act - This should not throw even with empty header value
                MsalTokenResponse response = await client.GetTokenAsync(
                    new Uri(TestConstants.AuthorityCommonTenant + "oauth2/v2.0/token"),
                    new RequestContext(harness.ServiceBundle, correlationId, null),
                    addCommonHeaders: true,
                    onBeforePostRequestHandler: null).ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(response);
            }
        }

        [TestMethod]
        public async Task CorrelationIdHeader_CaseInsensitive_ValidatesSuccessfully_Async()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var correlationId = Guid.NewGuid();

                // Create a mock response with different case for correlation ID header
                var responseMessage = MockHelpers.CreateSuccessfulClientCredentialTokenResponseMessage();
                responseMessage.Headers.Add("CLIENT-REQUEST-ID", correlationId.ToString());

                harness.HttpManager.AddMockHandler(new MockHttpMessageHandler
                {
                    ExpectedMethod = HttpMethod.Post,
                    ResponseMessage = responseMessage
                });

                OAuth2Client client = new OAuth2Client(harness.ServiceBundle.ApplicationLogger, harness.HttpManager, null);

                // Act - This should not throw with different case
                MsalTokenResponse response = await client.GetTokenAsync(
                    new Uri(TestConstants.AuthorityCommonTenant + "oauth2/v2.0/token"),
                    new RequestContext(harness.ServiceBundle, correlationId, null),
                    addCommonHeaders: true,
                    onBeforePostRequestHandler: null).ConfigureAwait(false);

                // Assert
                Assert.IsNotNull(response);
            }
        }
    }
}
