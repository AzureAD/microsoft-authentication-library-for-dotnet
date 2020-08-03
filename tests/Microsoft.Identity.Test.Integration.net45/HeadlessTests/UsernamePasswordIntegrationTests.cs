// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if !WINDOWS_APP && !ANDROID && !iOS // U/P not available on UWP, Android and iOS
using System;
using System.Linq;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.net45.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    // Note: these tests require permission to a KeyVault Microsoft account;
    // Please ignore them if you are not a Microsoft FTE, they will run as part of the CI build
    [TestClass]
    public class UsernamePasswordIntegrationTests
    {
        private const string Authority = "https://login.microsoftonline.com/organizations/";
        private static readonly string[] s_scopes = { "User.Read" };
        public string CurrentApiId { get; set; }

        // Http Telemetry Constants
        private static Guid CorrelationId = new Guid("ad8c894a-557f-48c0-b045-c129590c344e");
        private const string XClientCurrentTelemetryROPC = "2|1003,0|";
        private const string XClientCurrentTelemetryROPCFailure = "2|1003,0|";
        private const string XClientLastTelemetryROPC = "2|0|||";
        private const string XClientLastTelemetryROPCFailure =
            "2|0|1003,ad8c894a-557f-48c0-b045-c129590c344e|invalid_grant|";
        private const string ApiIdAndCorrelationIdSection =
            "1003,ad8c894a-557f-48c0-b045-c129590c344e";
        private const string InvalidGrantError = "invalid_grant";
        private const string UPApiId = "1003";

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        #region Happy Path Tests
        [TestMethod]
        public async Task ROPC_AAD_Async()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            await RunHappyPathTestAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory(TestCategories.Arlington)]
        public async Task ARLINGTON_ROPC_AAD_Async()
        {
            var labResponse = await LabUserHelper.GetArlingtonUserAsync().ConfigureAwait(false);
            await RunHappyPathTestAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory(TestCategories.Arlington)]
        public async Task ARLINGTON_ROPC_ADFS_Async()
        {
            var labResponse = await LabUserHelper.GetArlingtonADFSUserAsync().ConfigureAwait(false);
            await RunHappyPathTestAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ROPC_ADFSv4Federated_Async()
        {
            var labResponse = await LabUserHelper.GetAdfsUserAsync(FederationProvider.AdfsV4, true).ConfigureAwait(false);
            await RunHappyPathTestAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ROPC_ADFSv3Federated_Async()
        {
            var labResponse = await LabUserHelper.GetAdfsUserAsync(FederationProvider.AdfsV3, true).ConfigureAwait(false);
            await RunHappyPathTestAsync(labResponse).ConfigureAwait(false);
        }

        [TestMethod]
        [TestCategory(TestCategories.ADFS)]
        public async Task AcquireTokenFromAdfsUsernamePasswordAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetAdfsUserAsync(FederationProvider.ADFSv2019, true).ConfigureAwait(false);

            var user = labResponse.User;

            SecureString securePassword = new NetworkCredential("", user.GetOrFetchPassword()).SecurePassword;

            var msalPublicClient = PublicClientApplicationBuilder
                .Create(Adfs2019LabConstants.PublicClientId)
                .WithAdfsAuthority(Adfs2019LabConstants.Authority)
                .WithTestLogging()
                .Build();
            AuthenticationResult authResult = await msalPublicClient
                .AcquireTokenByUsernamePassword(s_scopes, user.Upn, securePassword)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
        }

        #endregion

        /// <summary>
        /// ROPC does not support MSA accounts
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        [TestCategory(TestCategories.MSA)]
        public async Task ROPC_MSA_Async()
        {
            var labResponse = await LabUserHelper.GetMsaUserAsync().ConfigureAwait(false);

            SecureString securePassword = new NetworkCredential("", labResponse.User.GetOrFetchPassword()).SecurePassword;

            var msalPublicClient = PublicClientApplicationBuilder
                .Create(labResponse.App.AppId)
                .WithAuthority(Authority)
                .Build();

            await AssertException.TaskThrowsAsync<MsalServiceException>(() =>
                msalPublicClient
                    .AcquireTokenByUsernamePassword(s_scopes, labResponse.User.Upn, securePassword)
                    .ExecuteAsync(CancellationToken.None))
                    .ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AcquireTokenWithManagedUsernameIncorrectPasswordAsync()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            var msalPublicClient = PublicClientApplicationBuilder
                .Create(labResponse.App.AppId)
                .WithTestLogging()
                .WithAuthority(Authority)
                .Build();
            await RunAcquireTokenWithUsernameIncorrectPasswordAsync(msalPublicClient, labResponse.User.Upn).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task AcquireTokenWithFederatedUsernameIncorrectPasswordAsync()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            var user = labResponse.User;

            SecureString incorrectSecurePassword = new SecureString();
            incorrectSecurePassword.AppendChar('x');
            incorrectSecurePassword.MakeReadOnly();

            var msalPublicClient = PublicClientApplicationBuilder
                .Create(labResponse.App.AppId)
                .WithTestLogging()
                .WithAuthority(Authority).Build();

            var result = await AssertException.TaskThrowsAsync<MsalUiRequiredException>(() =>
                msalPublicClient
                    .AcquireTokenByUsernamePassword(s_scopes, user.Upn, incorrectSecurePassword)
                    .ExecuteAsync(CancellationToken.None)
                    )
                .ConfigureAwait(false);

            Assert.AreEqual(result.ErrorCode, "invalid_grant");
        }

        [TestMethod]
        public async Task AcquireToken_ManagedUsernameIncorrectPassword_AcquireTokenSuccessful_CheckTelemetryHeadersAsync()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            await CheckTelemetryHeadersAsync(labResponse).ConfigureAwait(false);
        }

        private async Task CheckTelemetryHeadersAsync(
            LabResponse labResponse)
        {
            var factory = new HttpSnifferClientFactory();
            
            var msalPublicClient = PublicClientApplicationBuilder
                .Create(labResponse.App.AppId)
                .WithAuthority(Authority)
                .WithHttpClientFactory(factory)
                .Build();

            await RunAcquireTokenWithUsernameIncorrectPasswordAsync(msalPublicClient, labResponse.User.Upn).ConfigureAwait(false);

            AuthenticationResult authResult = await msalPublicClient
                    .AcquireTokenByUsernamePassword(s_scopes, labResponse.User.Upn, new NetworkCredential("", labResponse.User.GetOrFetchPassword()).SecurePassword)
                    .WithCorrelationId(CorrelationId)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
            Assert.IsTrue(string.Equals(labResponse.User.Upn, authResult.Account.Username, StringComparison.InvariantCultureIgnoreCase));
            AssertTelemetryHeaders(factory, true, labResponse);
        }

        private async Task RunAcquireTokenWithUsernameIncorrectPasswordAsync(
            IPublicClientApplication msalPublicClient,
            string userName)
        {
            SecureString incorrectSecurePassword = new SecureString();
            incorrectSecurePassword.AppendChar('x');
            incorrectSecurePassword.MakeReadOnly();

            try
            {
                var result = await msalPublicClient
                    .AcquireTokenByUsernamePassword(s_scopes, userName, incorrectSecurePassword)
                    .WithCorrelationId(CorrelationId)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (MsalServiceException ex)
            {
                Assert.IsTrue(!string.IsNullOrWhiteSpace(ex.CorrelationId));
                Assert.AreEqual(400, ex.StatusCode);
                Assert.AreEqual(InvalidGrantError, ex.ErrorCode);
                Assert.IsTrue(ex.Message.StartsWith("AADSTS50126"));

                return;
            }

            Assert.Fail("Bad exception or no exception thrown");
        }

        private async Task RunHappyPathTestAsync(LabResponse labResponse)
        {
            var factory = new HttpSnifferClientFactory();
            var msalPublicClient = PublicClientApplicationBuilder
                .Create(labResponse.App.AppId)
                .WithTestLogging()
                .WithHttpClientFactory(factory)
                .WithAuthority(labResponse.Lab.Authority, "organizations")
                .Build();

            AuthenticationResult authResult = await msalPublicClient
                .AcquireTokenByUsernamePassword(s_scopes, labResponse.User.Upn, new NetworkCredential("", labResponse.User.GetOrFetchPassword()).SecurePassword)
                .WithCorrelationId(CorrelationId)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
            Assert.IsTrue(string.Equals(labResponse.User.Upn, authResult.Account.Username, StringComparison.InvariantCultureIgnoreCase));
            AssertTelemetryHeaders(factory, false, labResponse);
            // If test fails with "user needs to consent to the application, do an interactive request" error,
            // Do the following:
            // 1) Add in code to pull the user's password before creating the SecureString, and put a breakpoint there.
            // string password = ((LabUser)user).GetPassword();
            // 2) Using the MSAL Desktop app, make sure the ClientId matches the one used in integration testing.
            // 3) Do the interactive sign-in with the MSAL Desktop app with the username and password from step 1.
            // 4) After successful log-in, remove the password line you added in with step 1, and run the integration test again.
        }

        private void AssertTelemetryHeaders(HttpSnifferClientFactory factory, bool IsFailure, LabResponse labResponse)
        {
            var (req, res) = factory.RequestsAndResponses.Single(x => x.Item1.RequestUri.AbsoluteUri == labResponse.Lab.Authority + "organizations/oauth2/v2.0/token" &&
            x.Item2.StatusCode == HttpStatusCode.OK);

            var telemetryLastValue = req.Headers.Single(h => h.Key == TelemetryConstants.XClientLastTelemetry).Value;
            var telemetryCurrentValue = req.Headers.Single(h => h.Key == TelemetryConstants.XClientCurrentTelemetry).Value;
            HttpTelemetryRecorder httpTelemetryRecorder = new HttpTelemetryRecorder();

            string csvCurrent = telemetryCurrentValue.FirstOrDefault();
            string csvPrevious = telemetryLastValue.FirstOrDefault();

            if (!IsFailure)
            {
                Assert.AreEqual(XClientCurrentTelemetryROPC, csvCurrent);
                Assert.AreEqual(XClientLastTelemetryROPC, csvPrevious);

                httpTelemetryRecorder.SplitCurrentCsv(csvCurrent);
                httpTelemetryRecorder.CheckSchemaVersion(csvCurrent);

                Assert.AreEqual(UPApiId, httpTelemetryRecorder.ApiId.FirstOrDefault(e => e.Contains(UPApiId)));
                Assert.AreEqual(TelemetryConstants.Zero, httpTelemetryRecorder.ForceRefresh);
                Assert.AreEqual(XClientLastTelemetryROPC, csvPrevious);
            }
            else
            {
                Assert.AreEqual(XClientCurrentTelemetryROPCFailure, csvCurrent);
                Assert.AreEqual(XClientLastTelemetryROPCFailure, csvPrevious);
                httpTelemetryRecorder.CheckSchemaVersion(csvCurrent);
                httpTelemetryRecorder.CheckSchemaVersion(csvPrevious);
                httpTelemetryRecorder.SplitCurrentCsv(csvCurrent);
                httpTelemetryRecorder.SplitPreviousCsv(csvPrevious);

                Assert.AreEqual(UPApiId, httpTelemetryRecorder.ApiId.FirstOrDefault(e => e.Contains(UPApiId)));
                Assert.AreEqual(1, httpTelemetryRecorder.ErrorCode.Count());
                Assert.AreEqual(TelemetryConstants.Zero, httpTelemetryRecorder.SilentCallSuccessfulCount);
                Assert.AreEqual(TelemetryConstants.Zero, httpTelemetryRecorder.ForceRefresh);
                Assert.AreEqual(ApiIdAndCorrelationIdSection, httpTelemetryRecorder.ApiIdAndCorrelationIds.FirstOrDefault());
                Assert.AreEqual(InvalidGrantError, httpTelemetryRecorder.ErrorCode.FirstOrDefault());
            }
        }
    }
}
#endif
