// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Identity.Test.Unit.CoreTests.Telemetry.TelemetryHelperTests;
using System.Threading;
using Microsoft.Identity.Test.Integration.net45.Infrastructure;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class HttpTelemetryTests2 : TestBase
    {
        private TokenCacheHelper _tokenCacheHelper;
        private TelemetryManager _telemetryManager;
        private _TestReceiver _testReceiver;
        internal _TestEvent _trackingEvent;
        private const string Comma = ",";

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            _tokenCacheHelper = new TokenCacheHelper();
            var serviceBundle = TestCommon.CreateServiceBundleWithCustomHttpManager(null, clientId: TestConstants.ClientId);
            _testReceiver = new _TestReceiver();
            _telemetryManager = new TelemetryManager(
                serviceBundle.Config,
                serviceBundle.PlatformProxy,
                _testReceiver.HandleTelemetryEvents);
            _trackingEvent = new _TestEvent("tracking event", "thetelemetrycorrelationid");
        }

        [TestMethod]
        public void FetchAndResetPreviousHttpTelemetryContent_ContainsNoStoppedEvents_ReturnsEmptyString()
        {
            Assert.AreEqual(string.Empty, _telemetryManager.FetchAndResetPreviousHttpTelemetryContent(_trackingEvent));
            Assert.AreEqual(0, _telemetryManager.SuccessfulSilentCallCount);
        }

        [TestMethod]
        public void FetchCurrentHttpTelemetryContent_ContainsNoEventsInProgress_ReturnsEmptyString()
        {
            Assert.AreEqual(string.Empty, _telemetryManager.FetchCurrentHttpTelemetryContent(_trackingEvent));
        }

        [TestMethod]
        public async Task AcquireToken_CreateHttpTelemetryHeaders_ReturnsSomethingInterestingAsync()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();
                var factory = new HttpSnifferClientFactory();

                PublicClientApplication pca = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                   .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                   .WithTelemetry(new TraceTelemetryConfig())
                                                                   .WithHttpManager(harness.HttpManager)
                                                                   .WithHttpClientFactory(factory)
                                                                   .BuildConcrete();
                var mockUi = MsalMockHelpers.ConfigureMockWebUI(
                     pca.ServiceBundle.PlatformProxy,
                     AuthorizationResult.FromUri(pca.AppConfig.RedirectUri + "?code=some-code"));

                harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityCommonTenant);
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(
                   TestConstants.AuthorityCommonTenant);

                Guid correlationId = new Guid();

                AuthenticationResult result = await pca
                        .AcquireTokenInteractive(TestConstants.s_scope)
                        .WithCorrelationId(correlationId)
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(false);

                var (req, res) = factory.RequestsAndResponses.Single(x => x.Item1.RequestUri.AbsoluteUri == "https://login.microsoftonline.com/common/oauth2/v2.0/token");

                var telemetryLastValue = req.Headers.Single(h => h.Key == "x-client-last-telemetry").Value;
                var telemetryCurrentValue = req.Headers.Single(h => h.Key == "x-client-current-telemetry").Value;

                Assert.IsNotNull(result.Account);
                HttpTelemetryRecorder httpTelemetryRecorder = new HttpTelemetryRecorder();
                httpTelemetryRecorder.SplitCurrentCsv(telemetryCurrentValue.FirstOrDefault());
                httpTelemetryRecorder.SplitPreviousCsv(telemetryLastValue.FirstOrDefault());

                Assert.AreEqual(0, httpTelemetryRecorder.CorrelationIdPrevious.Count());
                Assert.AreEqual("1003", httpTelemetryRecorder.ApiId.FirstOrDefault());
                Assert.AreEqual(0, httpTelemetryRecorder.ErrorCode.Count());
                Assert.AreEqual(TelemetryConstants.Zero, httpTelemetryRecorder.SilentCallSuccessfulCount);
                Assert.AreEqual(TelemetryConstants.Zero, httpTelemetryRecorder.ForceRefresh);
            }
        }

        public static IDictionary<string, string> CreateHttpTelemetryHeaders(
          Guid correlationId,
          string apiId,
          string errorCode,
          string errorCode2,
          string forceRefresh)
        {
            string repeatedCorrelationId = CreateRepeatInTelemetryHeader(correlationId.ToString());
            string corrIdSection = repeatedCorrelationId.Trim(',');

            IDictionary<string, string> httpTelemetryHeaders = new Dictionary<string, string>
                {
                    { TelemetryConstants.XClientLastTelemetry,
                        TelemetryConstants.HttpTelemetrySchemaVersion2 +
                        TelemetryConstants.HttpTelemetryPipe +
                        "0" +
                        TelemetryConstants.HttpTelemetryPipe +
                        CreateRepeatInTelemetryHeader(apiId) +
                        corrIdSection +
                        TelemetryConstants.HttpTelemetryPipe +
                        CreateErrorCodeRepeatHeader(errorCode, errorCode2) +
                        TelemetryConstants.HttpTelemetryPipe
                        },
                    { TelemetryConstants.XClientCurrentTelemetry,
                        TelemetryConstants.HttpTelemetrySchemaVersion2 +
                        TelemetryConstants.HttpTelemetryPipe +
                        apiId +
                        Comma +
                        forceRefresh +
                        TelemetryConstants.HttpTelemetryPipe}
                };
            return httpTelemetryHeaders;
        }

        private static string CreateRepeatInTelemetryHeader(
            string stringToRepeat)
        {
            return stringToRepeat + Comma;
        }

        private static string CreateErrorCodeRepeatHeader(
            string errorCode1,
            string errorCode2)
        {
            if (!string.IsNullOrEmpty(errorCode2))
            {
                return errorCode1 + Comma + errorCode2;
            }
            return errorCode1;
        }
    }
}
