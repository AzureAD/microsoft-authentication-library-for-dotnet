// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Integration.Infrastructure;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.Identity.Test.Unit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Integration.SeleniumTests
{
    [TestClass]
    public class FociTests
    {
        private static readonly string[] s_scopes = new[] { "https://graph.microsoft.com/.default" };

#region MSTest Hooks
        /// <summary>
        /// Initialized by MSTest (do not make private or readonly)
        /// </summary>
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            ApplicationBase.ResetStateForTest();
        }

        #endregion

        /// <summary>
        /// Tests: 
        /// 1. Global sign-in: sign in from one app in the family, the other app can sign-in silently.
        /// 2. Global sign-out: sign out from one app in the family, the others app are automatically signed out.
        /// </summary>
        /// <remarks>The FOCI flag does not appear in the U/P flow, an interactive flow is required. Interactive flow
        /// cannot be automated because http://localhost cannot currently be added to the family apps</remarks>
        [Ignore]
        [TestMethod]
        public async Task FociSignInSignOutAsync()
        {
            var user = await LabResponseHelper.GetUserConfigAsync(KeyVaultSecrets.UserPublicCloud).ConfigureAwait(false);
            var app = await LabResponseHelper.GetAppConfigAsync(KeyVaultSecrets.AppPCAClient).ConfigureAwait(false);
            string cacheFilePath = null;

            try
            {
                cacheFilePath = Path.GetTempFileName();

                CreateFamilyApps(app.AppId, cacheFilePath, out IPublicClientApplication pca_fam1, out IPublicClientApplication pca_fam2, out IPublicClientApplication pca_nonFam);

                var userCacheAccess1 = pca_fam1.UserTokenCache.RecordAccess();
                var userCacheAccess2 = pca_fam2.UserTokenCache.RecordAccess();
                var userCacheAccess3 = pca_nonFam.UserTokenCache.RecordAccess();

                Trace.WriteLine("Get a token interactively with an app from the family.");
                AuthenticationResult authResult = await pca_fam1.AcquireTokenWithDeviceCode(s_scopes, deviceCodeResult =>
                {
                    SeleniumExtensions.PerformDeviceCodeLogin(
                        deviceCodeResult,
                        user,
                        TestContext,
                        false);

                    return Task.FromResult(0);
                }).ExecuteAsync()
                .ConfigureAwait(false);

                MsalAssert.AssertAuthResult(authResult, user);
                userCacheAccess1.AssertAccessCounts(0, 1);
                userCacheAccess2.AssertAccessCounts(0, 0);
                userCacheAccess3.AssertAccessCounts(0, 0);

                Trace.WriteLine("Get a token silently with another app from the family.");
                authResult = await pca_fam2.AcquireTokenSilent(s_scopes, user.Upn)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                MsalAssert.AssertAuthResult(authResult, user);
                userCacheAccess1.AssertAccessCounts(0, 1);
                userCacheAccess2.AssertAccessCounts(1, 1); // a write occurs because appA does not have an AT, so it needs to refresh the FRT
                userCacheAccess3.AssertAccessCounts(0, 0);

                Trace.WriteLine("Apps that are not part of the family cannot get tokens this way.");
                await AssertException.TaskThrowsAsync<MsalUiRequiredException>(() => pca_nonFam
                        .AcquireTokenSilent(s_scopes, user.Upn)
                        .ExecuteAsync())
                    .ConfigureAwait(false);

                userCacheAccess1.AssertAccessCounts(0, 1);
                userCacheAccess2.AssertAccessCounts(1, 1);
                userCacheAccess3.AssertAccessCounts(1, 0);

                Trace.WriteLine("Sing-out from one app - sign out of all apps in the family");
                System.Collections.Generic.IEnumerable<IAccount> accounts = await pca_fam1.GetAccountsAsync().ConfigureAwait(false);
                await pca_fam1.RemoveAsync(accounts.Single()).ConfigureAwait(false);
                System.Collections.Generic.IEnumerable<IAccount> acc2 = await pca_fam2.GetAccountsAsync().ConfigureAwait(false);

                Assert.IsFalse(acc2.Any());
            }
            finally
            {
                if (cacheFilePath != null && File.Exists(cacheFilePath))
                {
                    File.Delete(cacheFilePath);
                }
            }
        }

        private static void CreateFamilyApps(string nonFamilyAppId, string cacheFilePath, out IPublicClientApplication pca_fam1, out IPublicClientApplication pca_fam2, out IPublicClientApplication pca_nonFam)
        {
            var keyvault = new KeyVaultSecretsProvider(KeyVaultInstance.MsalTeam);
            var clientId1 = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1"; // Visual Studio Known App
            //var clientId2 = "1950a258-227b-4e31-a9cf-717495945fc2"; // PowerShell Known App
            var clientId2 = "d326c1ce-6cc6-4de2-bebc-4591e5e13ef0"; // SharePoint Known App

            pca_fam1 = PublicClientApplicationBuilder
               .Create(clientId1)
               .WithTestLogging()
               .Build();

            pca_fam2 = PublicClientApplicationBuilder
               .Create(clientId2)
               .WithTestLogging()
              .Build();

            pca_nonFam = PublicClientApplicationBuilder
              .Create(nonFamilyAppId)
               .WithTestLogging()
              .Build();

            SetCacheSerializationToFile(pca_fam1, cacheFilePath);
            SetCacheSerializationToFile(pca_fam2, cacheFilePath, true);
            SetCacheSerializationToFile(pca_nonFam, cacheFilePath);
        }

        private static void SetCacheSerializationToFile(IPublicClientApplication pca, string filePath, bool clearCache = false)
        {
            pca.UserTokenCache.SetBeforeAccess(notificationArgs =>
            {
                byte[] content = File.Exists(filePath)
                    ? File.ReadAllBytes(filePath)
                    : null;
                notificationArgs.TokenCache.DeserializeMsalV3(content, clearCache);
            });

            pca.UserTokenCache.SetAfterAccess(notificationArgs =>
            {
                // if the access operation resulted in a cache update
                if (notificationArgs.HasStateChanged)
                {
                    // reflect changes in the persistent store
                    File.WriteAllBytes(filePath, notificationArgs.TokenCache.SerializeMsalV3());
                }
            });
        }
    }
}
