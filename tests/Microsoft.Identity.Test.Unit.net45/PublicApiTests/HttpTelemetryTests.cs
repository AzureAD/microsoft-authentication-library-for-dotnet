// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class HttpTelemetryTests : TestBase
    {
        private TokenCacheHelper _tokenCacheHelper;
        private Guid _correlationId;

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            _tokenCacheHelper = new TokenCacheHelper();
        }

        [TestMethod]
        public async Task AcquireTokenInteractive_AuthCancelled_FollowedByASuccessfulTokenResponseAsync()
        {
            await CreateFailedAndThenSuccessfulResponseToVerifyErrorCodesInHttpTelemetryDataAsync(MsalError.AuthenticationCanceledError).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AcquireTokenInteractive_AccessDenied_FollowedByASuccessfulTokenResponseAsync()
        {
            await CreateFailedAndThenSuccessfulResponseToVerifyErrorCodesInHttpTelemetryDataAsync(MsalError.AccessDenied).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AcquireTokenInteractive_StateMismatch_FollowedByASuccessfulTokenResponseAsync()
        {
            await CreateFailedAndThenSuccessfulResponseToVerifyErrorCodesInHttpTelemetryDataAsync(MsalError.StateMismatchError).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AcquireTokenInteractive_InvalidGrant_FollowedByASuccessfulTokenResponseAsync()
        {
            await CreateFailedAndThenSuccessfulResponseToVerifyErrorCodesInHttpTelemetryDataAsync(MsalError.InvalidGrantError).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AcquireTokenInteractive_UserMismatch_FollowedByASuccessfulTokenResponseAsync()
        {
            await CreateFailedAndThenSuccessfulResponseToVerifyErrorCodesInHttpTelemetryDataAsync(MsalError.UserMismatch).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AcquireTokenInteractive_UnknownError_FollowedByASuccessfulTokenResponseAsync()
        {
            await CreateFailedAndThenSuccessfulResponseToVerifyErrorCodesInHttpTelemetryDataAsync(MsalError.UnknownError).ConfigureAwait(false);
        }

        public static IDictionary<string, string> CreateHttpTelemetryHeaders(
           Guid correlationId,
           string apiId,
           string errorCode)
        {
            const string comma = ",";
            IDictionary<string, string> httpTelemetryHeaders = new Dictionary<string, string>
                {
                    { TelemetryConstants.XClientLastTelemetry,
                        TelemetryConstants.HttpTelemetrySchemaVersion1 +
                        TelemetryConstants.HttpTelemetryPipe +
                        apiId +
                        comma +
                        correlationId.ToString() +
                        comma +
                        errorCode
                        },
                    { TelemetryConstants.XClientCurrentTelemetry,
                        TelemetryConstants.HttpTelemetrySchemaVersion1 +
                        TelemetryConstants.HttpTelemetryPipe +
                        apiId }
                };
            return httpTelemetryHeaders;
        }

        private async Task CreateFailedAndThenSuccessfulResponseToVerifyErrorCodesInHttpTelemetryDataAsync(string errorCode)
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication app = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                                                                            .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                                                                            .WithHttpManager(harness.HttpManager)
                                                                            .WithTelemetry(new TraceTelemetryConfig())
                                                                            .BuildConcrete();
                // Interactive call and authentication fails
                var ui = new MockWebUI()
                {
                    MockResult = AuthorizationResult.FromUri(TestConstants.AuthorityHomeTenant + "?error=" + errorCode)
                };

                harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityCommonTenant);
                MsalMockHelpers.ConfigureMockWebUI(app.ServiceBundle.PlatformProxy, ui);

                _correlationId = new Guid();
                AuthenticationResult result = null;

                try
                {
                    result = await app
                        .AcquireTokenInteractive(TestConstants.s_scope)
                        .WithCorrelationId(_correlationId)
                        .ExecuteAsync(CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch (MsalException exc)
                {
                    Assert.IsNotNull(exc);
                    Assert.AreEqual(errorCode, exc.ErrorCode);

                    // Try authentication again...
                    MsalMockHelpers.ConfigureMockWebUI(
                        app.ServiceBundle.PlatformProxy,
                        AuthorizationResult.FromUri(app.AppConfig.RedirectUri + "?code=some-code"));
                    var userCacheAccess = app.UserTokenCache.RecordAccess();

                    harness.HttpManager.AddSuccessfulTokenResponseWithHttpTelemetryMockHandlerForPost(
                        TestConstants.AuthorityCommonTenant,
                        null,
                        null,
                        CreateHttpTelemetryHeaders(
                            _correlationId,
                            TestConstants.InteractiveRequestApiId,
                            exc.ErrorCode));

                    result = app
                         .AcquireTokenInteractive(TestConstants.s_scope)
                         .WithCorrelationId(_correlationId)
                         .ExecuteAsync(CancellationToken.None)
                         .Result;

                    Assert.IsNotNull(result);
                    Assert.IsNotNull(result.Account);
                    Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
                    Assert.AreEqual(TestConstants.CreateUserIdentifier(), result.Account.HomeAccountId.Identifier);
                    Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
                    userCacheAccess.AssertAccessCounts(0, 1);
                }
            }
        }
    }
}
