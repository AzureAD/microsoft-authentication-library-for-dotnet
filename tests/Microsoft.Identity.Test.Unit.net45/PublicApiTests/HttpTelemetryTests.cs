//// Copyright (c) Microsoft Corporation. All rights reserved.
//// Licensed under the MIT License.

//using System;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.Identity.Client;
//using Microsoft.Identity.Client.TelemetryCore;
//using Microsoft.Identity.Client.TelemetryCore.Internal;
//using Microsoft.Identity.Client.UI;
//using Microsoft.Identity.Test.Common.Core.Helpers;
//using Microsoft.Identity.Test.Common.Core.Mocks;
//using Microsoft.Identity.Test.Common.Mocks;
//using Microsoft.Identity.Test.Unit.CoreTests.Telemetry;
//using Microsoft.VisualStudio.TestTools.UnitTesting;

//namespace Microsoft.Identity.Test.Unit.PublicApiTests
//{
//    [TestClass]
//    public class HttpTelemetryTests : TestBase
//    {
//        private TokenCacheHelper _tokenCacheHelper;
//        private HttpTelemetryContent _httpTelemetryContent;
//        private Guid _correlationId;
//        private const string Comma = ",";

//        [TestInitialize]
//        public override void TestInitialize()
//        {
//            base.TestInitialize();
//            _tokenCacheHelper = new TokenCacheHelper();
//            new HttpTelemetryContent(true);
//            _httpTelemetryContent = new HttpTelemetryContent(
//               new TelemetryHelperTests._TestEvent("tracking event", TestConstants.ClientId));
//        }

//        [TestMethod]
//        public async Task AcquireTokenInteractive_AuthCancelled_FollowedByASuccessfulTokenResponseAsync()
//        {
//            await CreateFailedAndThenSuccessfulResponseToVerifyErrorCodesInHttpTelemetryDataAsync(MsalError.AuthenticationCanceledError).ConfigureAwait(false);
//        }

//        [TestMethod]
//        public async Task AcquireTokenInteractive_AccessDenied_FollowedByASuccessfulTokenResponseAsync()
//        {
//            await CreateFailedAndThenSuccessfulResponseToVerifyErrorCodesInHttpTelemetryDataAsync(MsalError.AccessDenied).ConfigureAwait(false);
//        }

//        [TestMethod]
//        public async Task AcquireTokenInteractive_StateMismatch_FollowedByASuccessfulTokenResponseAsync()
//        {
//            await CreateFailedAndThenSuccessfulResponseToVerifyErrorCodesInHttpTelemetryDataAsync(MsalError.StateMismatchError).ConfigureAwait(false);
//        }

//        [TestMethod]
//        public async Task AcquireTokenInteractive_InvalidGrant_FollowedByASuccessfulTokenResponseAsync()
//        {
//            await CreateFailedAndThenSuccessfulResponseToVerifyErrorCodesInHttpTelemetryDataAsync(MsalError.InvalidGrantError).ConfigureAwait(false);
//        }

//        [TestMethod]
//        public async Task AcquireTokenInteractive_UserMismatch_FollowedByASuccessfulTokenResponseAsync()
//        {
//            await CreateFailedAndThenSuccessfulResponseToVerifyErrorCodesInHttpTelemetryDataAsync(MsalError.UserMismatch).ConfigureAwait(false);
//        }

//        [TestMethod]
//        public async Task AcquireTokenInteractive_UnknownError_FollowedByASuccessfulTokenResponseAsync()
//        {
//            await CreateFailedAndThenSuccessfulResponseToVerifyErrorCodesInHttpTelemetryDataAsync(MsalError.UnknownError).ConfigureAwait(false);
//        }

//        [TestMethod]
//        public void CheckCurrentHttpTelemetryHeaderContent()
//        {
//            const string csvString = "2|,1|";
//            HttpTelemetryContent httpTelemetryContent = CreateHttpTelemetryContent();
//            Assert.AreEqual(csvString, httpTelemetryContent.GetCsvAsCurrent());
//        }

//        [TestMethod]
//        public void CheckPreviousHttpTelemetryHeaderContentWithAuthFailed()
//        {
//            const string csvString =
//                "2|" +
//                "0|" +
//                ",1005,1005,d3adb33f-c0de-ed0c-c0de-deadb33fc0d3,0a69ff54-3e00-4244-a6c8-09ccc1efa707|" +
//                ",authentication_failed|";

//            List<string> errorCodes = new List<string>(new string[] { MsalError.AuthenticationFailed });
//            PopulateHttpTelemetryContent(errorCodes);

//            Assert.AreEqual(csvString, _httpTelemetryContent.GetCsvAsPrevious(0));
//        }

//        [TestMethod]
//        public void CheckPreviousHttpTelemetryHeaderContentWithTwoFailures()
//        {
//            const string csvString =
//                "2|" +
//                "0|" +
//                ",1005,1005,d3adb33f-c0de-ed0c-c0de-deadb33fc0d3,0a69ff54-3e00-4244-a6c8-09ccc1efa707|" +
//                ",authentication_failed,user_mismatch|";

//            List<string> errorCodes = new List<string>(new string[] {
//                MsalError.AuthenticationFailed,
//                MsalError.UserMismatch });
//            PopulateHttpTelemetryContent(errorCodes);

//            Assert.AreEqual(csvString, _httpTelemetryContent.GetCsvAsPrevious(0));
//        }

//        [TestMethod]
//        public void CheckPreviousHttpTelemetryHeaderContentWithThreeFailures()
//        {
//            const string csvString =
//                "2|1|" +
//                ",1005,1005,1005" +
//                ",d3adb33f-c0de-ed0c-c0de-deadb33fc0d3,0a69ff54-3e00-4244-a6c8-09ccc1efa707,b1e662cf-8efb-4e13-b89a-71e845bbb62f|" +
//                ",authentication_failed,user_mismatch,invalid_grant|";

//            List<string> errorCodes = new List<string>(new string[] {
//                MsalError.AuthenticationFailed,
//                MsalError.UserMismatch,
//                MsalError.InvalidGrantError });
//            PopulateHttpTelemetryContent(errorCodes);
//            _httpTelemetryContent.ApiId.Add("1005");
//            _httpTelemetryContent.CorrelationId.Add("b1e662cf-8efb-4e13-b89a-71e845bbb62f");

//            Assert.AreEqual(csvString, _httpTelemetryContent.GetCsvAsPrevious(1));
//        }

//        [TestMethod]
//        public void CheckHttpPreviousTelemetryHeaderSize()
//        {
//            string hugeString = new string('*', 3757);
//            _httpTelemetryContent.ApiId.Add(hugeString);
//            string previousCsvString = _httpTelemetryContent.GetCsvAsPrevious(0);

//            Assert.IsTrue(string.IsNullOrEmpty(previousCsvString));
//        }

//        private HttpTelemetryContent CreateHttpTelemetryContent()
//        {
//            HttpTelemetryContent httpTelemetryContent = new HttpTelemetryContent(
//                new TelemetryHelperTests._TestEvent("tracking event", TestConstants.ClientId));
//            httpTelemetryContent.ForceRefresh = "1";
//            return httpTelemetryContent;
//        }

//        private void PopulateHttpTelemetryContent(List<string> errorCodes)
//        {
//            _httpTelemetryContent.ApiId.Add("1005");
//            _httpTelemetryContent.ApiId.Add("1005");
//            _httpTelemetryContent.CorrelationId.Add("0a69ff54-3e00-4244-a6c8-09ccc1efa707");
//            _httpTelemetryContent.LastErrorCode.AddRange(errorCodes);
//        }

//        public static IDictionary<string, string> CreateHttpTelemetryHeaders(
//           Guid correlationId,
//           string apiId,
//           string errorCode,
//           string errorCode2,
//           string forceRefresh)
//        {
//            string repeatedCorrelationId = CreateRepeatInTelemetryHeader(correlationId.ToString());
//            string corrIdSection = repeatedCorrelationId.Trim(',');

//            IDictionary<string, string> httpTelemetryHeaders = new Dictionary<string, string>
//                {
//                    { TelemetryConstants.XClientLastTelemetry,
//                        TelemetryConstants.HttpTelemetrySchemaVersion2 +
//                        TelemetryConstants.HttpTelemetryPipe +
//                        "0" +
//                        TelemetryConstants.HttpTelemetryPipe +
//                        CreateRepeatInTelemetryHeader(apiId) +
//                        corrIdSection +
//                        TelemetryConstants.HttpTelemetryPipe +
//                        CreateErrorCodeRepeatHeader(errorCode, errorCode2) +
//                        TelemetryConstants.HttpTelemetryPipe
//                        },
//                    { TelemetryConstants.XClientCurrentTelemetry,
//                        TelemetryConstants.HttpTelemetrySchemaVersion2 +
//                        TelemetryConstants.HttpTelemetryPipe +
//                        apiId +
//                        Comma +
//                        forceRefresh +
//                        TelemetryConstants.HttpTelemetryPipe}
//                };
//            return httpTelemetryHeaders;
//        }

//        private static string CreateRepeatInTelemetryHeader(
//            string stringToRepeat)
//        {
//            return stringToRepeat + Comma;
//        }

//        private static string CreateErrorCodeRepeatHeader(
//            string errorCode1,
//            string errorCode2)
//        {
//            if (!string.IsNullOrEmpty(errorCode2))
//            {
//                return errorCode1 + Comma + errorCode2;
//            }
//            return errorCode1;
//        }

//        private async Task CreateFailedAndThenSuccessfulResponseToVerifyErrorCodesInHttpTelemetryDataAsync(string errorCode)
//        {
//            using (var harness = CreateTestHarness())
//            {
//                harness.HttpManager.AddInstanceDiscoveryMockHandler();

//                // Interactive call and authentication fails
//                var ui = new MockWebUI()
//                {
//                    MockResult = AuthorizationResult.FromUri(TestConstants.AuthorityHomeTenant + "?error=" + errorCode)
//                };

//                PublicClientApplication pca = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
//                                                                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
//                                                                    .WithTelemetry(new TraceTelemetryConfig())
//                                                                    .WithHttpManager(harness.HttpManager)
//                                                                    .BuildConcrete();

//                harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(TestConstants.AuthorityCommonTenant);
//                MsalMockHelpers.ConfigureMockWebUI(pca.ServiceBundle.PlatformProxy, ui);

//                _correlationId = new Guid();
//                AuthenticationResult result = null;

//                try
//                {
//                    result = await pca
//                        .AcquireTokenInteractive(TestConstants.s_scope)
//                        .WithCorrelationId(_correlationId)
//                        .ExecuteAsync(CancellationToken.None)
//                        .ConfigureAwait(false);
//                }
//                catch (MsalException exc)
//                {
//                    Assert.IsNotNull(exc);
//                    Assert.AreEqual(errorCode, exc.ErrorCode);

//                    // Try authentication again...
//                    MsalMockHelpers.ConfigureMockWebUI(
//                        pca.ServiceBundle.PlatformProxy,
//                        AuthorizationResult.FromUri(pca.AppConfig.RedirectUri + "?code=some-code"));
//                    var userCacheAccess = pca.UserTokenCache.RecordAccess();

//                    harness.HttpManager.AddSuccessfulTokenResponseWithHttpTelemetryMockHandlerForPost(
//                        TestConstants.AuthorityCommonTenant,
//                        null,
//                        null,
//                        CreateHttpTelemetryHeaders(
//                            _correlationId,
//                            TestConstants.InteractiveRequestApiId,
//                            exc.ErrorCode,
//                            null,
//                            TelemetryConstants.Zero));

//                    result = pca
//                         .AcquireTokenInteractive(TestConstants.s_scope)
//                         .WithCorrelationId(_correlationId)
//                         .ExecuteAsync(CancellationToken.None)
//                         .Result;

//                    Assert.IsNotNull(result);
//                    Assert.IsNotNull(result.Account);
//                    Assert.AreEqual(TestConstants.UniqueId, result.UniqueId);
//                    Assert.AreEqual(TestConstants.CreateUserIdentifier(), result.Account.HomeAccountId.Identifier);
//                    Assert.AreEqual(TestConstants.DisplayableId, result.Account.Username);
//                    userCacheAccess.AssertAccessCounts(0, 1);
//                }
//            }
//        }
//    }
//}
