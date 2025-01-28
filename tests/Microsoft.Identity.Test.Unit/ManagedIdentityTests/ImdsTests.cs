// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;
using Microsoft.Identity.Client.ManagedIdentity;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Test.Common.Core.Helpers.ManagedIdentityTestUtil;

namespace Microsoft.Identity.Test.Unit.ManagedIdentityTests
{
    [TestClass]
    public class ImdsTests : TestBase
    {
        [TestInitialize]
        public override void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [DataTestMethod]
        [DataRow(HttpStatusCode.BadRequest, ImdsManagedIdentitySource.IdentityUnavailableError, 1, DisplayName = "BadRequest - Identity Unavailable")]
        [DataRow(HttpStatusCode.BadGateway, ImdsManagedIdentitySource.GatewayError, 1, DisplayName = "BadGateway - Gateway Error")]
        [DataRow(HttpStatusCode.GatewayTimeout, ImdsManagedIdentitySource.GatewayError, 4, DisplayName = "GatewayTimeout - Gateway Error Retries")]
        public async Task ImdsErrorHandlingTestAsync(HttpStatusCode statusCode, string expectedErrorSubstring, int expectedAttempts)
        {
            using (new EnvVariableContext())
            using (var httpManager = new MockHttpManager())
            {
                SetEnvironmentVariables(ManagedIdentitySource.Imds, "http://169.254.169.254");

                var miBuilder = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
                    .WithHttpManager(httpManager);

                // Disabling shared cache options to avoid cross test pollution.
                miBuilder.Config.AccessorOptions = null;

                var mi = miBuilder.Build();

                httpManager.AddMockHandlerContentNotFound(HttpMethod.Post);
                httpManager.AddMockHandlerContentNotFound(HttpMethod.Post);
                httpManager.AddMockHandlerContentNotFound(HttpMethod.Post);
                httpManager.AddMockHandlerContentNotFound(HttpMethod.Post);

                // Adding multiple mock handlers to simulate retries for GatewayTimeout
                for (int i = 0; i < expectedAttempts; i++)
                {
                    httpManager.AddManagedIdentityMockHandler(ManagedIdentityTests.ImdsEndpoint, ManagedIdentityTests.Resource,
                        MockHelpers.GetMsiImdsErrorResponse(), ManagedIdentitySource.Imds, statusCode: statusCode);
                }

                // Expecting a MsalServiceException indicating an error
                MsalServiceException ex = await Assert.ThrowsExceptionAsync<MsalServiceException>(async () =>
                    await mi.AcquireTokenForManagedIdentity(ManagedIdentityTests.Resource)
                    .ExecuteAsync().ConfigureAwait(false)).ConfigureAwait(false);

                Assert.IsNotNull(ex);
                Assert.AreEqual(ManagedIdentitySource.Imds.ToString(), ex.AdditionalExceptionData[MsalException.ManagedIdentitySource]);
                Assert.AreEqual(MsalError.ManagedIdentityRequestFailed, ex.ErrorCode);
                Assert.IsTrue(ex.Message.Contains(expectedErrorSubstring), $"The error message is not as expected. Error message: {ex.Message}. Expected message should contain: {expectedErrorSubstring}");
            }
        }
    }
}
