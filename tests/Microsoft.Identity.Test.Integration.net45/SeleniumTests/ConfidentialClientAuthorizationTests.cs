// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
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
            await RunTestForUserAsync(labResponse, "https://login.microsoftonline.com/common").ConfigureAwait(false);
            await RunTestForUserAsync(labResponse, $"https://login.microsoftonline.com/{TenantId}").ConfigureAwait(false);
        }

        private async Task<AuthenticationResult> RunTestForUserAsync(LabResponse labResponse, string authority)
        {
            var cert = s_secretProvider.GetCertificateWithPrivateMaterial(CertificateName);

            IConfidentialClientApplication cca;
            string redirectUri = SeleniumWebUI.FindFreeLocalhostRedirectUri();

            cca = ConfidentialClientApplicationBuilder
                .Create(ConfidentialClientID)
                .WithAuthority(authority)
                .WithCertificate(cert)
                .WithRedirectUri(redirectUri)
                .Build();

            Trace.WriteLine("Part 1 - Call GetAuthorizationRequestUrl to figure out where to go ");
            var startUri = await cca
                .GetAuthorizationRequestUrl(s_scopes)
                .ExecuteAsync()
                .ConfigureAwait(false);

            Trace.WriteLine("Part 2 - Use a browser to login and to capture the authorization code ");
            var seleniumUi = new SeleniumWebUI((driver) =>
            {
                Trace.WriteLine("Starting Selenium automation");
                driver.PerformLogin(labResponse.User, Prompt.SelectAccount, false, false);
            }, TestContext);

            CancellationTokenSource cts = new CancellationTokenSource(s_timeout);
            Uri authCodeUri = await seleniumUi.AcquireAuthorizationCodeAsync(
                startUri,
                new Uri(redirectUri),
                cts.Token)
                .ConfigureAwait(false);

            var authorizationResult = AuthorizationResult.FromUri(authCodeUri.AbsoluteUri);
            Assert.AreEqual(AuthorizationStatus.Success, authorizationResult.Status);

            Trace.WriteLine("Part 3 - Get a token using the auth code, just like a website");
            var result = await cca.AcquireTokenByAuthorizationCode(s_scopes, authorizationResult.Code)
                .ExecuteAsync()
                .ConfigureAwait(false);

            return result;
        }
    }
}
