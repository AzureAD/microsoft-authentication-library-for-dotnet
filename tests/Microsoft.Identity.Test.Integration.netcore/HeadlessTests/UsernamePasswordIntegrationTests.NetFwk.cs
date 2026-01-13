// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#if !ANDROID && !iOS // U/P not available on Android and iOS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.TelemetryCore;
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
        private static readonly string[] s_scopes = { "User.Read" };

        // HTTP Telemetry Constants
        private static Guid CorrelationId = new Guid("ad8c894a-557f-48c0-b045-c129590c344e");
        private readonly string XClientCurrentTelemetryROPC = $"{TelemetryConstants.HttpTelemetrySchemaVersion}|1003,{CacheRefreshReason.NotApplicable:D},,,|0,1,1,,";
        private readonly string XClientCurrentTelemetryROPCFailure = $"{TelemetryConstants.HttpTelemetrySchemaVersion}|1003,{CacheRefreshReason.NotApplicable:D},,,|0,1,1,,";
        private const string UPApiId = "1003";
        private const string B2CROPCAuthority = "https://msidlabb2c.b2clogin.com/tfp/msidlabb2c.onmicrosoft.com/B2C_1_ROPC_Auth";
        private static readonly string[] s_b2cScopes = { "https://msidlabb2c.onmicrosoft.com/msidlabb2capi/read" };

        [TestInitialize]
        public void TestInitialize()
        {
            ApplicationBase.ResetStateForTest();
        }

        #region Happy Path Tests
        [TestMethod]
        public async Task ROPC_AAD_Async()
        {
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserPublicCloud).ConfigureAwait(false);
            var app = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppPCAClient).ConfigureAwait(false);
            await RunHappyPathTestAsync(user, app).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ROPC_AAD_CCA_Async()
        {
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserPublicCloud).ConfigureAwait(false);
            var app = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppS2S).ConfigureAwait(false);
            await RunHappyPathTestAsync(user, app, isPublicClient: false).ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
        [TestCategory(TestCategories.Arlington)]
        public async Task ARLINGTON_ROPC_AAD_CCA_Async()
        {
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserArlington).ConfigureAwait(false);
            var app = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.MsalAppArlingtonCCA).ConfigureAwait(false);
            user.AzureEnvironment = LabConstants.AzureEnvironmentUsGovernment;
            await RunHappyPathTestAsync(user, app, isPublicClient: false, cloud:Cloud.Arlington).ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
#if IGNORE_FEDERATED
        [Ignore]
#endif
        public async Task ROPC_ADFSv4Federated_Async()
        {
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserFederated).ConfigureAwait(false);
            var app = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppPCAClient).ConfigureAwait(false);
            await RunHappyPathTestAsync(user, app).ConfigureAwait(false);
        }

        [RunOn(TargetFrameworks.NetCore)]
        [TestCategory(TestCategories.ADFS)]
#if IGNORE_FEDERATED
        [Ignore]
#endif
        public async Task AcquireTokenFromAdfsUsernamePasswordAsync()
        {
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserFederated).ConfigureAwait(false);
            var app = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppPCAClient).ConfigureAwait(false);

            // Use the new ADFS authority and disable validation since ADFS infrastructure is not fully available
            Uri authorityUri = new Uri("https://fs.id4slab1.com/adfs");
            
            var msalPublicClient = PublicClientApplicationBuilder
                .Create(app.AppId)
                .WithAuthority(authorityUri, validateAuthority: false)
                .WithTestLogging()
                .Build();

            #pragma warning disable CS0618 // Type or member is obsolete
            AuthenticationResult authResult = await msalPublicClient
                .AcquireTokenByUsernamePassword(s_scopes, user.Upn, user.GetOrFetchPassword())
                .ExecuteAsync()
                .ConfigureAwait(false);
            #pragma warning restore CS0618

            Assert.IsNotNull(authResult);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
        }

        #endregion

        [RunOn(TargetFrameworks.NetCore)]
        public async Task ROPC_B2C_Async()
        {
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserB2C).ConfigureAwait(false);
            var app = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.B2CAppIdLabsAppB2C).ConfigureAwait(false);
            await RunB2CHappyPathTestAsync(user, app).ConfigureAwait(false);
        }

        private async Task RunHappyPathTestAsync(UserConfig user, AppConfig app, string federationMetadata = "", bool isPublicClient = true, Cloud cloud = Cloud.Public)
        {
            var factory = new HttpSnifferClientFactory();
            IClientApplicationBase clientApp = null;

            if (isPublicClient)
            {
                clientApp = PublicClientApplicationBuilder
                            .Create(app.AppId)
                            .WithTestLogging()
                            .WithHttpClientFactory(factory)
                            .WithAuthority(app.Authority, "organizations")
                            .Build();
            }
            else
            {
                var clientAppBuilder = ConfidentialClientApplicationBuilder
                            .Create(app.AppId)
                            .WithTestLogging()
                            .WithHttpClientFactory(factory)
                            .WithAuthority(app.Authority);

                if (cloud == Cloud.Arlington)
                {
                    clientAppBuilder.WithClientSecret(LabResponseHelper.FetchSecretString(TestConstants.MsalArlingtonCCAKeyVaultSecretName, LabResponseHelper.KeyVaultSecretsProviderMsid));
                }
                else
                {
                    clientAppBuilder.WithCertificate(CertificateHelper.FindCertificateByName(TestConstants.AutomationTestCertName), true);
                }

                 clientApp = clientAppBuilder.Build();
            }

            AuthenticationResult authResult
                = await GetAuthenticationResultWithAssertAsync(
                    user,
                    app,
                    factory,
                    clientApp,
                    federationMetadata,
                    CorrelationId).ConfigureAwait(false);

            if (AuthorityInfo.FromAuthorityUri(app.Authority + "/" + user.TenantId, false).AuthorityType == AuthorityType.Aad)
            {
                AssertTenantProfiles(authResult.Account.GetTenantProfiles(), authResult.TenantId);
            }
            else
            {
                Assert.IsNull(authResult.Account.GetTenantProfiles());
            }

            // If test fails with "user needs to consent to the application, do an interactive request" error,
            // Do the following:
            // 1) Add in code to pull the user's password, and put a breakpoint there.
            // string password = ((LabUser)user).GetPassword();
            // 2) Using the MSAL Desktop app, make sure the ClientId matches the one used in integration testing.
            // 3) Do the interactive sign-in with the MSAL Desktop app with the username and password from step 1.
            // 4) After successful log-in, remove the password line you added in with step 1, and run the integration test again.
        }

        private async Task RunB2CHappyPathTestAsync(UserConfig user, AppConfig app, string federationMetadata = "")
        {
            var factory = new HttpSnifferClientFactory();

            var msalPublicClient = PublicClientApplicationBuilder
                .Create(app.AppId)
                .WithB2CAuthority(B2CROPCAuthority)
                .WithTestLogging()
                .WithHttpClientFactory(factory)
                .Build();

            #pragma warning disable CS0618 // Type or member is obsolete
            AuthenticationResult authResult = await msalPublicClient
                .AcquireTokenByUsernamePassword(s_b2cScopes, user.Upn, user.GetOrFetchPassword())
                .WithCorrelationId(CorrelationId)
                .WithFederationMetadata(federationMetadata)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
            #pragma warning restore CS0618

            Assert.IsNotNull(authResult);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
            AssertCcsRoutingInformationIsNotSent(factory);

            var acc = (await msalPublicClient.GetAccountsAsync().ConfigureAwait(false)).Single();
            var claimsPrincipal = acc.GetTenantProfiles().Single().ClaimsPrincipal;

            Assert.AreNotEqual(TokenResponseHelper.NullPreferredUsernameDisplayLabel, acc.Username);
            Assert.IsNotNull(claimsPrincipal.FindFirst("Name"));
            Assert.IsNotNull(claimsPrincipal.FindFirst("nbf"));
            Assert.IsNotNull(claimsPrincipal.FindFirst("exp"));

            // If test fails with "user needs to consent to the application, do an interactive request" error,
            // Do the following: 
            // 1) Add in code to pull the user's password, and put a breakpoint there.
            // string password = ((LabUser)user).GetPassword();
            // 2) Using the MSAL Desktop app, make sure the ClientId matches the one used in integration testing.
            // 3) Do the interactive sign-in with the MSAL Desktop app with the username and password from step 1.
            // 4) After successful log-in, remove the password line you added in with step 1, and run the integration test again.
        }

        private async Task<AuthenticationResult> GetAuthenticationResultWithAssertAsync(
            UserConfig user,
            AppConfig app,
            HttpSnifferClientFactory factory,
            IClientApplicationBase clientApp,
            string federationMetadata,
            Guid testCorrelationId)
        {
            AuthenticationResult authResult;

            if (clientApp is IPublicClientApplication publicClientApp)
            {
                #pragma warning disable CS0618 // Type or member is obsolete
                authResult = await publicClientApp
                .AcquireTokenByUsernamePassword(s_scopes, user.Upn, user.GetOrFetchPassword())
                .WithCorrelationId(testCorrelationId)
                .WithFederationMetadata(federationMetadata)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
                #pragma warning restore CS0618
            }
            else
            {
                authResult = await (((IConfidentialClientApplication)clientApp) as IByUsernameAndPassword)
                .AcquireTokenByUsernamePassword(s_scopes, user.Upn, user.GetOrFetchPassword())
                .WithCorrelationId(testCorrelationId)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
            }

            Console.WriteLine("Access Token: " + authResult.AccessToken);

            Assert.IsNotNull(authResult);
            Assert.AreEqual(TokenSource.IdentityProvider, authResult.AuthenticationResultMetadata.TokenSource);
            Assert.IsNotNull(authResult.AccessToken);
            Assert.IsNotNull(authResult.IdToken);
            Assert.IsTrue(string.Equals(user.Upn, authResult.Account.Username, StringComparison.InvariantCultureIgnoreCase));
            AssertTelemetryHeaders(factory, false, user, app);
            AssertCcsRoutingInformationIsSent(factory, user);                        

            return authResult;
        }

        private void AssertCcsRoutingInformationIsSent(HttpSnifferClientFactory factory, UserConfig user)
        {
            var CcsHeader = TestCommon.GetCcsHeaderFromSnifferFactory(factory);
            Assert.AreEqual($"x-anchormailbox:upn:{user.Upn}", $"{CcsHeader.Key}:{CcsHeader.Value.FirstOrDefault()}");
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

        private void AssertTelemetryHeaders(HttpSnifferClientFactory factory, bool IsFailure, UserConfig user, AppConfig app)
        {
            // Ensure that a request to the token endpoint was made using the expected authority.
            var (req, _) = factory.RequestsAndResponses.Single(x =>
                x.Item1.RequestUri.AbsoluteUri.StartsWith(app.Authority, StringComparison.OrdinalIgnoreCase) &&
                x.Item1.RequestUri.AbsoluteUri.EndsWith("oauth2/v2.0/token", StringComparison.OrdinalIgnoreCase) &&
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
    }
}
#endif
