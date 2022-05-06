// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Advanced;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.Integration.net45.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.UIAutomation.Infrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.SeleniumTests
{
    /// <summary>
    /// Test provisioning: 
    /// 
    /// 1. Create a certificate in KV. Update CertificateName with its name.
    /// 2. Create an AAD app and register this certificate. Also set http://localhost as redirect uri.
    /// </summary>
    [TestClass]
    public class ConfidentialClientAuthorizationTests
    {
        // TODO: 
        // - Extend these tests to B2C and ADFS
        private static readonly TimeSpan s_timeout = TimeSpan.FromMinutes(1);

        private static readonly string[] s_scopes = { "User.Read" };
        private const string ConfidentialClientID = "8b5195c6-3cc2-4e81-ad28-1e07ad219f3e";
        private const string TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
        private const string CertificateName = "for-cca-testing";

        private static KeyVaultSecretsProvider s_secretProvider;

        #region MSTest Hooks
        /// <summary>
        /// Initialized by MSTest (do not make private or readonly)
        /// </summary>
        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            s_secretProvider = new KeyVaultSecretsProvider();
        }

        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        #endregion

        [TestMethod]
        // Regression test for: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/891
        public async Task SeleniumGetAuthCode_RedeemForAt_CommonAuthority_Async()
        {
            // Arrange
            LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            
            await RunTestForUserAsync(ConfidentialClientID, labResponse, "https://login.microsoftonline.com/common", false).ConfigureAwait(false);
            await RunTestForUserAsync(ConfidentialClientID, labResponse, $"https://login.microsoftonline.com/{labResponse.User.TenantId}", false).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task GetTokenByAuthCode_WithPKCE_Async()
        {
            // Arrange
            LabResponse labResponse = await LabUserHelper.GetDefaultUserAsync().ConfigureAwait(false);
            await RunTestForUserAsync(ConfidentialClientID, labResponse, "https://login.microsoftonline.com/common", true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task GetTokenByAuthCode_HybridSPA_Async()
        {
            // Arrange
            LabResponse labResponse = await LabUserHelper.GetHybridSpaAccontAsync().ConfigureAwait(false);

            var result = await RunTestForUserAsync(labResponse.App.AppId, labResponse, 
                "https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca", false, 
                "http://localhost:3000/auth/implicit-redirect").ConfigureAwait(false);

            Assert.IsNotNull(result.SpaAuthCode);

            result = await RunTestForUserAsync(labResponse.App.AppId, labResponse, 
                "https://login.microsoftonline.com/f645ad92-e38d-4d1a-b510-d1b09a74a8ca", false, 
                "http://localhost:3000/auth/implicit-redirect", false).ConfigureAwait(false);

            Assert.IsNull(result.SpaAuthCode);
        }

        private async Task<AuthenticationResult> RunTestForUserAsync(string appId, LabResponse labResponse, 
            string authority, bool usePkce = false, string redirectUri = null, bool spaCode = true)
        {
            var cert = await s_secretProvider.GetCertificateWithPrivateMaterialAsync(
                CertificateName, KeyVaultInstance.MsalTeam).ConfigureAwait(false);

            IConfidentialClientApplication cca;
            redirectUri = redirectUri ?? SeleniumWebUI.FindFreeLocalhostRedirectUri();

            HttpSnifferClientFactory factory;

            cca = ConfidentialClientApplicationBuilder
                .Create(appId)
                .WithAuthority(authority)
                .WithCertificate(cert)
                .WithRedirectUri(redirectUri)
                .WithTestLogging(out factory)
                .Build();

            var cacheAccess = (cca as ConfidentialClientApplication).UserTokenCache.RecordAccess();

            Trace.WriteLine("Part 1 - Call GetAuthorizationRequestUrl to figure out where to go ");
            var authUriBuilder = cca
                .GetAuthorizationRequestUrl(s_scopes);

            string codeVerifier = "";
            if (usePkce)
            {
                authUriBuilder.WithPkce(out codeVerifier);
            }

            Uri authUri = await authUriBuilder.ExecuteAsync()
                .ConfigureAwait(false);

            cacheAccess.AssertAccessCounts(0, 0);

            Trace.WriteLine("Part 2 - Use a browser to login and to capture the authorization code ");
            var seleniumUi = new SeleniumWebUI((driver) =>
            {
                Trace.WriteLine("Starting Selenium automation");
                driver.PerformLogin(labResponse.User, Prompt.SelectAccount, false, false);
            }, TestContext);

            CancellationTokenSource cts = new CancellationTokenSource(s_timeout);
            Uri authCodeUri = await seleniumUi.AcquireAuthorizationCodeAsync(
                authUri,
                new Uri(redirectUri),
                cts.Token)
                .ConfigureAwait(false);

            var authorizationResult = AuthorizationResult.FromUri(authCodeUri.AbsoluteUri);
            Assert.AreEqual(AuthorizationStatus.Success, authorizationResult.Status);

            factory.RequestsAndResponses.Clear();

            Trace.WriteLine("Part 3 - Get a token using the auth code, just like a website");
             var result = await cca.AcquireTokenByAuthorizationCode(s_scopes, authorizationResult.Code)
                .WithPkceCodeVerifier(codeVerifier)
                .WithExtraHttpHeaders(TestConstants.ExtraHttpHeader)
                .WithSpaAuthorizationCode(spaCode)
                .ExecuteAsync()
                .ConfigureAwait(false);

            cacheAccess.AssertAccessCounts(0, 1);
            AssertCacheKey(cacheAccess, result.Account.HomeAccountId.Identifier);

            AssertExtraHTTPHeadersAreSent(factory);

            Trace.WriteLine("Part 4 - Remove Account");

            await cca.RemoveAsync(result.Account).ConfigureAwait(false);
            cacheAccess.AssertAccessCounts(0, 2); 

            AssertCacheKey(cacheAccess, result.Account.HomeAccountId.Identifier);

            return result;
        }

        private static void AssertCacheKey(TokenCacheAccessRecorder cacheAccess, string expectedKey)
        {
            Assert.AreEqual(
                expectedKey,
                cacheAccess.LastAfterAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(
                expectedKey,
                cacheAccess.LastBeforeAccessNotificationArgs.SuggestedCacheKey);
            Assert.AreEqual(
                expectedKey,
                cacheAccess.LastBeforeWriteNotificationArgs.SuggestedCacheKey);
        }

        private void AssertExtraHTTPHeadersAreSent(HttpSnifferClientFactory factory)
        {
            var (req, res) = factory.RequestsAndResponses.Single(x => x.Item1.RequestUri.AbsoluteUri.Contains("oauth2/v2.0/token") &&
            x.Item2.StatusCode == HttpStatusCode.OK);

            var ExtraHttpHeader = req.Headers.Single(h => h.Key == TestConstants.ExtraHttpHeader.Keys.FirstOrDefault());

            Assert.AreEqual(TestConstants.ExtraHttpHeader.Keys.FirstOrDefault(), ExtraHttpHeader.Key);
            Assert.AreEqual(TestConstants.ExtraHttpHeader.Values.FirstOrDefault(), ExtraHttpHeader.Value.FirstOrDefault());
        }
    }
}
