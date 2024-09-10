// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if !ANDROID && !iOS // U/P not available on Android and iOS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Kerberos;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.NetFx.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.HeadlessTests
{
    [TestClass]
    public class UsernamePasswordIntegrationTests
    {
        private const string Authority = "https://login.microsoftonline.com/organizations/";
        private static readonly string[] s_scopes = { "User.Read" };
        public string CurrentApiId { get; set; }

        // HTTP Telemetry Constants
        private static Guid CorrelationId = new Guid("ad8c894a-557f-48c0-b045-c129590c344e");
        private readonly string XClientCurrentTelemetryROPC = $"{TelemetryConstants.HttpTelemetrySchemaVersion}|1003,{CacheRefreshReason.NotApplicable:D},,,|0,1,1,,";
        private readonly string XClientCurrentTelemetryROPCFailure = $"{TelemetryConstants.HttpTelemetrySchemaVersion}|1003,{CacheRefreshReason.NotApplicable:D},,,|0,1,1,,";
        private const string ApiIdAndCorrelationIdSection =
            "1003,ad8c894a-557f-48c0-b045-c129590c344e";
        private const string InvalidGrantError = "invalid_grant";
        private const string UPApiId = "1003";
        private const string B2CROPCAuthority = "https://msidlabb2c.b2clogin.com/tfp/msidlabb2c.onmicrosoft.com/B2C_1_ROPC_Auth";
        private static readonly string[] s_b2cScopes = { "https://msidlabb2c.onmicrosoft.com/msidlabb2capi/read" };

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
        public async Task ROPC_AAD_CCA_Async()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            await RunHappyPathTestAsync(labResponse: labResponse, isPublicClient: false).ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
        [TestCategory(TestCategories.Arlington)]
        public async Task ARLINGTON_ROPC_AAD_Async()
        {
            var labResponse = await LabUserHelper.GetArlingtonUserAsync().ConfigureAwait(false);
            await RunHappyPathTestAsync(labResponse).ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
        [TestCategory(TestCategories.Arlington)]
        public async Task ARLINGTON_ROPC_AAD_CCA_Async()
        {
            var labResponse = await LabUserHelper.GetArlingtonUserAsync().ConfigureAwait(false);
            await RunHappyPathTestAsync(labResponse, isPublicClient: false, cloud:Cloud.Arlington).ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
        [TestCategory(TestCategories.Arlington)]
        public async Task ARLINGTON_ROPC_ADFS_Async()
        {
            var labResponse = await LabUserHelper.GetArlingtonADFSUserAsync().ConfigureAwait(false);
            await RunHappyPathTestAsync(labResponse).ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
        public async Task ROPC_ADFSv4Federated_Async()
        {
            var labResponse = await LabUserHelper.GetAdfsUserAsync(FederationProvider.AdfsV4, true).ConfigureAwait(false);
            await RunHappyPathTestAsync(labResponse).ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
        public async Task ROPC_ADFSv4Federated_WithMetadata_Async()
        {
            var labResponse = await LabUserHelper.GetAdfsUserAsync(FederationProvider.AdfsV4, true).ConfigureAwait(false);
            string federationMetadata = File.ReadAllText(@"federationMetadata.xml").ToString();
            await RunHappyPathTestAsync(labResponse, federationMetadata).ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
        [TestCategory(TestCategories.ADFS)]
        public async Task AcquireTokenFromAdfsUsernamePasswordAsync()
        {
            LabResponse labResponse = await LabUserHelper.GetAdfsUserAsync(FederationProvider.ADFSv2019, true).ConfigureAwait(false);

            var user = labResponse.User;

            var msalPublicClient = PublicClientApplicationBuilder
                .Create(Adfs2019LabConstants.PublicClientId)
                .WithAdfsAuthority(Adfs2019LabConstants.Authority)
                .WithTestLogging()
                .Build();
            AuthenticationResult authResult = await msalPublicClient
                .AcquireTokenByUsernamePassword(s_scopes, user.Upn, user.GetOrFetchPassword())
                .ExecuteAsync()
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
            TestCommon.ValidateNoKerberosTicketFromAuthenticationResult(authResult);
        }

        #endregion

        /// <summary>
        /// ROPC does not support MSA accounts
        /// </summary>
        /// <returns></returns>
        [RunOn(TargetFrameworks.NetCore)]
        [TestCategory(TestCategories.MSA)]
        public async Task ROPC_MSA_Async()
        {
            var labResponse = await LabUserHelper.GetMsaUserAsync().ConfigureAwait(false);

            var msalPublicClient = PublicClientApplicationBuilder
                .Create(labResponse.App.AppId)
                .WithAuthority(Authority)
                .Build();

            var result = await AssertException.TaskThrowsAsync<MsalClientException>(() =>
                msalPublicClient
                    .AcquireTokenByUsernamePassword(s_scopes, labResponse.User.Upn, labResponse.User.GetOrFetchPassword())
                    .ExecuteAsync(CancellationToken.None))
                    .ConfigureAwait(false);

            Assert.AreEqual(MsalError.RopcDoesNotSupportMsaAccounts, result.ErrorCode);
            Assert.AreEqual(MsalErrorMessage.RopcDoesNotSupportMsaAccounts, result.Message);

        }

        [RunOn(TargetFrameworks.NetCore)]
        public async Task ROPC_B2C_Async()
        {
            var labResponse = await LabUserHelper.GetB2CLocalAccountAsync().ConfigureAwait(false);
            await RunB2CHappyPathTestAsync(labResponse).ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
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

        [RunOn(TargetFrameworks.NetCore)]
        public async Task AcquireTokenWithFederatedUsernameIncorrectPasswordAsync()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            var user = labResponse.User;

            string incorrectPassword = "x";

            var msalPublicClient = PublicClientApplicationBuilder
                .Create(labResponse.App.AppId)
                .WithTestLogging()
                .WithAuthority(Authority).Build();

            var result = await AssertException.TaskThrowsAsync<MsalUiRequiredException>(() =>
                msalPublicClient
                    .AcquireTokenByUsernamePassword(s_scopes, user.Upn, incorrectPassword)
                    .ExecuteAsync(CancellationToken.None)
                    )
                .ConfigureAwait(false);

            Assert.AreEqual(result.ErrorCode, "invalid_grant");
        }

        [RunOn(TargetFrameworks.NetCore)]
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
                    .AcquireTokenByUsernamePassword(s_scopes, labResponse.User.Upn, labResponse.User.GetOrFetchPassword())
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
            try
            {
                var result = await msalPublicClient
                    .AcquireTokenByUsernamePassword(s_scopes, userName, "incorrectPass")

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

        private async Task RunHappyPathTestAsync(LabResponse labResponse, string federationMetadata = "", bool isPublicClient = true, Cloud cloud = Cloud.Public)
        {
            var factory = new HttpSnifferClientFactory();
            IClientApplicationBase clientApp = null;

            if (isPublicClient)
            {
                clientApp = PublicClientApplicationBuilder
                            .Create(labResponse.App.AppId)
                            .WithTestLogging()
                            .WithHttpClientFactory(factory)
                            .WithAuthority(labResponse.Lab.Authority, "organizations")
                            .Build();
            }
            else
            {
                IConfidentialAppSettings settings = ConfidentialAppSettings.GetSettings(cloud);
                var clientAppBuilder = ConfidentialClientApplicationBuilder
                            .Create(settings.ClientId)
                            .WithTestLogging()
                            .WithHttpClientFactory(factory)
                            .WithAuthority(labResponse.Lab.Authority, "organizations");

                if (cloud == Cloud.Arlington)
                {
                    clientAppBuilder.WithClientSecret(settings.GetSecret());
                }
                else
                {
                    clientAppBuilder.WithCertificate(settings.GetCertificate(), true);
                }

                 clientApp = clientAppBuilder.Build();
            }

            AuthenticationResult authResult
                = await GetAuthenticationResultWithAssertAsync(
                    labResponse,
                    factory,
                    clientApp,
                    federationMetadata,
                    CorrelationId).ConfigureAwait(false);

            if (AuthorityInfo.FromAuthorityUri(labResponse.Lab.Authority + "/" + labResponse.Lab.TenantId, false).AuthorityType == AuthorityType.Aad)
            {
                AssertTenantProfiles(authResult.Account.GetTenantProfiles(), authResult.TenantId);
            }
            else
            {
                Assert.IsNull(authResult.Account.GetTenantProfiles());
            }

            TestCommon.ValidateNoKerberosTicketFromAuthenticationResult(authResult);
            // If test fails with "user needs to consent to the application, do an interactive request" error,
            // Do the following:
            // 1) Add in code to pull the user's password, and put a breakpoint there.
            // string password = ((LabUser)user).GetPassword();
            // 2) Using the MSAL Desktop app, make sure the ClientId matches the one used in integration testing.
            // 3) Do the interactive sign-in with the MSAL Desktop app with the username and password from step 1.
            // 4) After successful log-in, remove the password line you added in with step 1, and run the integration test again.
        }

        private async Task RunB2CHappyPathTestAsync(LabResponse labResponse, string federationMetadata = "")
        {
            var factory = new HttpSnifferClientFactory();

            var msalPublicClient = PublicClientApplicationBuilder
                .Create(labResponse.App.AppId)
                .WithB2CAuthority(B2CROPCAuthority)
                .WithTestLogging()
                .WithHttpClientFactory(factory)
                .Build();

            AuthenticationResult authResult = await msalPublicClient
                .AcquireTokenByUsernamePassword(s_b2cScopes, labResponse.User.Upn, labResponse.User.GetOrFetchPassword())
                .WithCorrelationId(CorrelationId)
                .WithFederationMetadata(federationMetadata)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(authResult);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
            AssertCcsRoutingInformationIsNotSent(factory);

            // If test fails with "user needs to consent to the application, do an interactive request" error,
            // Do the following: 
            // 1) Add in code to pull the user's password, and put a breakpoint there.
            // string password = ((LabUser)user).GetPassword();
            // 2) Using the MSAL Desktop app, make sure the ClientId matches the one used in integration testing.
            // 3) Do the interactive sign-in with the MSAL Desktop app with the username and password from step 1.
            // 4) After successful log-in, remove the password line you added in with step 1, and run the integration test again.
        }

        private async Task<AuthenticationResult> GetAuthenticationResultWithAssertAsync(
            LabResponse labResponse,
            HttpSnifferClientFactory factory,
            IClientApplicationBase clientApp,
            string federationMetadata,
            Guid testCorrelationId)
        {
            AuthenticationResult authResult;

            if (clientApp is IPublicClientApplication publicClientApp)
            {
                authResult = await publicClientApp
                .AcquireTokenByUsernamePassword(s_scopes, labResponse.User.Upn, labResponse.User.GetOrFetchPassword())
                .WithCorrelationId(testCorrelationId)
                .WithFederationMetadata(federationMetadata)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
            }
            else
            {
                authResult = await (((IConfidentialClientApplication)clientApp) as IByUsernameAndPassword)
                .AcquireTokenByUsernamePassword(s_scopes, labResponse.User.Upn, labResponse.User.GetOrFetchPassword())
                .WithCorrelationId(testCorrelationId)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
            }


            Assert.IsNotNull(authResult);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
            Assert.IsTrue(string.Equals(labResponse.User.Upn, authResult.Account.Username, StringComparison.InvariantCultureIgnoreCase));
            AssertTelemetryHeaders(factory, false, labResponse);
            AssertCcsRoutingInformationIsSent(factory, labResponse);                        

            return authResult;
        }

        private void AssertCcsRoutingInformationIsSent(HttpSnifferClientFactory factory, LabResponse labResponse)
        {
            var CcsHeader = TestCommon.GetCcsHeaderFromSnifferFactory(factory);
            Assert.AreEqual($"x-anchormailbox:upn:{labResponse.User.Upn}", $"{CcsHeader.Key}:{CcsHeader.Value.FirstOrDefault()}");
        }

        private void AssertCcsRoutingInformationIsNotSent(HttpSnifferClientFactory factory)
        {
            var (req, _) = factory.RequestsAndResponses.Single(x =>
                x.Item1.RequestUri.AbsoluteUri.Contains("oauth2/v2.0/token") &&
                x.Item2.StatusCode == HttpStatusCode.OK);

            Assert.IsTrue(!req.Headers.Any(h => h.Key == Constants.CcsRoutingHintHeader));
        }

        private void AssertTenantProfiles(IEnumerable<TenantProfile> tenantProfiles, string tenantId)
        {
            Assert.IsNotNull(tenantProfiles);
            Assert.IsTrue(tenantProfiles.Count() > 0);

            TenantProfile tenantProfile = tenantProfiles.Single(tp => tp.TenantId == tenantId);
            Assert.IsNotNull(tenantProfile);
            Assert.IsNotNull(tenantProfile.ClaimsPrincipal);
            Assert.IsNotNull(tenantProfile.ClaimsPrincipal.FindFirst(claim => claim.Type == "tid" && claim.Value == tenantId));
        }

        private void AssertTelemetryHeaders(HttpSnifferClientFactory factory, bool IsFailure, LabResponse labResponse)
        {
            var (req, _) = factory.RequestsAndResponses.Single(x =>
                x.Item1.RequestUri.AbsoluteUri == labResponse.Lab.Authority + "organizations/oauth2/v2.0/token" &&
                x.Item2.StatusCode == HttpStatusCode.OK);

            var telemetryCurrentValue = req.Headers.Single(h => h.Key == TelemetryConstants.XClientCurrentTelemetry).Value;
            HttpTelemetryRecorder httpTelemetryRecorder = new HttpTelemetryRecorder();

            string csvCurrent = telemetryCurrentValue.FirstOrDefault();

            if (!IsFailure)
            {
                Assert.AreEqual(XClientCurrentTelemetryROPC, csvCurrent);

                httpTelemetryRecorder.SplitCurrentCsv(csvCurrent);
                httpTelemetryRecorder.CheckSchemaVersion(csvCurrent);

                Assert.AreEqual(UPApiId, httpTelemetryRecorder.ApiId.FirstOrDefault(e => e.Contains(UPApiId)));
                Assert.IsFalse(httpTelemetryRecorder.ForceRefresh);
            }
            else
            {
                Assert.AreEqual(XClientCurrentTelemetryROPCFailure, csvCurrent);
                httpTelemetryRecorder.CheckSchemaVersion(csvCurrent);
                httpTelemetryRecorder.SplitCurrentCsv(csvCurrent);

                Assert.AreEqual(UPApiId, httpTelemetryRecorder.ApiId.FirstOrDefault(e => e.Contains(UPApiId)));
                Assert.IsFalse(httpTelemetryRecorder.ForceRefresh);
            }
        }

        #region Azure AD Kerberos Feature Tests
        [IgnoreOnOneBranch]
        public async Task Kerberos_ROPC_AAD_Async()
        {
            var labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            await KerberosRunHappyPathTestAsync(labResponse).ConfigureAwait(false);
        }

        [IgnoreOnOneBranch]
        public async Task Kerberos_ROPC_ADFSv4Federated_Async()
        {
            var labResponse = await LabUserHelper.GetAdfsUserAsync(FederationProvider.AdfsV4, true).ConfigureAwait(false);
            await KerberosRunHappyPathTestAsync(labResponse).ConfigureAwait(false);
        }

        private async Task KerberosRunHappyPathTestAsync(LabResponse labResponse)
        {
            // Test with Id token
            var factory = new HttpSnifferClientFactory();
            var idTokenPublicClient = PublicClientApplicationBuilder
                .Create(labResponse.App.AppId)
                .WithTestLogging()
                .WithHttpClientFactory(factory)
                .WithAuthority(labResponse.Lab.Authority, "organizations")
                .WithClientId(TestConstants.KerberosTestApplicationId)
                .WithKerberosTicketClaim(TestConstants.KerberosServicePrincipalName, KerberosTicketContainer.IdToken)
                .Build();

            AuthenticationResult authResult = await GetAuthenticationResultWithAssertAsync(
                labResponse,
                factory,
                idTokenPublicClient,
                "",
                Guid.NewGuid()).ConfigureAwait(false);
            KerberosSupplementalTicket ticket = TestCommon.GetValidatedKerberosTicketFromAuthenticationResult(
                authResult,
                KerberosTicketContainer.IdToken,
                labResponse.User.Upn);
            Assert.IsNotNull(ticket);
            TestCommon.ValidateKerberosWindowsTicketCacheOperation(ticket);

            // Test with Access Token
            factory = new HttpSnifferClientFactory();
            var accessTokenPublicClient = PublicClientApplicationBuilder
                .Create(labResponse.App.AppId)
                .WithTestLogging()
                .WithHttpClientFactory(factory)
                .WithAuthority(labResponse.Lab.Authority, "organizations")
                .WithClientId(TestConstants.KerberosTestApplicationId)
                .WithKerberosTicketClaim(TestConstants.KerberosServicePrincipalName, KerberosTicketContainer.AccessToken)
                .Build();

            authResult = await GetAuthenticationResultWithAssertAsync(
                labResponse,
                factory,
                accessTokenPublicClient,
                "",
                Guid.NewGuid()).ConfigureAwait(false);
            ticket = TestCommon.GetValidatedKerberosTicketFromAuthenticationResult(
                authResult,
                KerberosTicketContainer.AccessToken,
                labResponse.User.Upn);
            Assert.IsNotNull(ticket);
            TestCommon.ValidateKerberosWindowsTicketCacheOperation(ticket);
        }

#endregion
    }
}
#endif
