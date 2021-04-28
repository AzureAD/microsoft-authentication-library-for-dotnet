// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Identity.Test.Common.Core.Mocks;

namespace Microsoft.Identity.Test.Unit.TelemetryTests
{
    [TestClass]
    public class MsalIdHelperTests : TestBase
    {
        [TestMethod]
        public async Task IdHelperAsync()
        {
            {
                using (var harness = base.CreateTestHarness())
                {

                    var headers1 = harness.HttpManager.AddInstanceDiscoveryMockHandler();

                    PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                                .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                                .WithHttpManager(harness.HttpManager)
                                                                                .WithTelemetry(new TraceTelemetryConfig())
                                                                                .BuildConcrete();
                    app.ServiceBundle.ConfigureMockWebUI();

                    var headers2 = harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                    Guid correlationId = Guid.NewGuid();

                    AuthenticationResult result = await app
                        .AcquireTokenInteractive(TestConstants.s_scope)
                        .WithCorrelationId(correlationId)
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    AssertHeaders(headers1, correlationId);
                    AssertHeaders(headers2, correlationId);
                }
            }

        }

        private static void AssertHeaders(MockHttpMessageHandler headers, Guid correlationId)
        {
            Assert.IsTrue(!headers.ActualRequestMessage.Headers.GetValues("x-client-os").Single().Contains("6.2.9200"));
            Assert.IsTrue(headers.ActualRequestMessage.Headers.GetValues("x-client-os").Single().Contains("Windows"));
            Assert.AreEqual(
                typeof(PublicClientApplication).Assembly.GetName().Version.ToString(),
                headers.ActualRequestMessage.Headers.GetValues("x-client-Ver").Single());

            Assert.AreEqual(1, headers.ActualRequestMessage.Headers.GetValues("x-client-CPU").Count());
            Assert.AreEqual(correlationId.ToString(), headers.ActualRequestMessage.Headers.GetValues("client-request-id").Single());
        }
    }
}
