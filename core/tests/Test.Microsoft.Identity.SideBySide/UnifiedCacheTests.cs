//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

extern alias msal;
extern alias adal;

using Test.Microsoft.Identity.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Security;
using System.Threading.Tasks;
using System.Linq;

namespace Test.MSAL.NET.Integration
{
    [TestClass]
    public class UnifiedCacheTests
    {
        public const string ClientId = "0615b6ca-88d4-4884-8729-b178178f7c27";

        public const string AuthorityTemplate = "https://login.microsoftonline.com/{0}/";

        public string[] MsalScopes = { "https://graph.microsoft.com/.default" };
        public string[] MsalScopes1 = { "https://graph.windows.net/.default" };

        private const string AdalResource1 = "https://graph.windows.net";
        private const string AdalResource2 = "https://graph.microsoft.com";

        private IUser user;
        private SecureString securePassword;
        private string authority;

        private byte[] AdalV3StateStorage;
        private byte[] UnifiedStateStorage;

        [TestInitialize]
        public void TestInitialize()
        {
            if (user == null)
            {
                user = GetUser(
                  new UserQueryParameters
                  {
                      IsMamUser = false,
                      IsMfaUser = false,
                      IsFederatedUser = false
                  });

                string stringPassword = ((LabUser)user).GetPassword();
                securePassword = new NetworkCredential("", stringPassword).SecurePassword;
                authority = string.Format(AuthorityTemplate, user.CurrentTenantId);
            }

            InitAdal();
            InitMsal();
        }

        private void AdalDoBefore(
            adal::Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCacheNotificationArgs args)
        {
            adal::Microsoft.Identity.Core.Cache.CacheData cacheData;

            cacheData = new adal::Microsoft.Identity.Core.Cache.CacheData()
            {
                AdalV3State = AdalV3StateStorage,
                UnifiedState = UnifiedStateStorage
            };

            args.TokenCache.DeserializeAdalAndUnifiedCache(cacheData);
        }

        private void AdalDoAfter(
            adal::Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCacheNotificationArgs args)
        {
            if (args.TokenCache.HasStateChanged)
            {
                adal::Microsoft.Identity.Core.Cache.CacheData cacheData = args.TokenCache.SerializeAdalAndUnifiedCache();

                AdalV3StateStorage = cacheData.AdalV3State;
                UnifiedStateStorage = cacheData.UnifiedState;

                args.TokenCache.HasStateChanged = false;
            }
        }

        private void MsalDoBefore(msal::Microsoft.Identity.Client.TokenCacheNotificationArgs args)
        {
            msal::Microsoft.Identity.Core.Cache.CacheData cacheData;

            cacheData = new msal::Microsoft.Identity.Core.Cache.CacheData()
            {
                AdalV3State = AdalV3StateStorage,
                UnifiedState = UnifiedStateStorage
            };

            msal::Microsoft.Identity.Client.TokenCacheExtensions.DeserializeUnifiedAndAdalCache(args.TokenCache, cacheData);
        }

        private void MsalDoAfter(msal::Microsoft.Identity.Client.TokenCacheNotificationArgs args)
        {
            if (args.TokenCache.HasStateChanged)
            {
                msal::Microsoft.Identity.Core.Cache.CacheData cacheData =
                    msal::Microsoft.Identity.Client.TokenCacheExtensions.SerializeUnifiedAndAdalCache(args.TokenCache);

                AdalV3StateStorage = cacheData.AdalV3State;
                UnifiedStateStorage = cacheData.UnifiedState;
            }
        }

        private adal::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext adalContext;
        private adal::Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCache adalCache;
        private adal::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationResult adalAuthResult;
        private void InitAdal()
        {
            adalCache = new adal::Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCache()
            {
                BeforeAccess = AdalDoBefore,
                AfterAccess = AdalDoAfter
            };
            adalContext = new adal::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(
                authority, adalCache);
        }

        private msal::Microsoft.Identity.Client.PublicClientApplication msalPublicClient;
        private msal::Microsoft.Identity.Client.TokenCache msalCache;
        private msal::Microsoft.Identity.Client.AuthenticationResult msalAuthResult;
        private void InitMsal()
        {
            msalCache = new msal::Microsoft.Identity.Client.TokenCache();

            msal::Microsoft.Identity.Client.TokenCacheExtensions.SetBeforeAccess(msalCache, MsalDoBefore);
            msal::Microsoft.Identity.Client.TokenCacheExtensions.SetAfterAccess(msalCache, MsalDoAfter);

            msalPublicClient = new msal::Microsoft.Identity.Client.PublicClientApplication(
                ClientId, authority, msalCache);
        }

        private void ValidateMsalAuthResult()
        {
            Assert.IsNotNull(msalAuthResult);
            Assert.IsNotNull(msalAuthResult.AccessToken);
            Assert.IsNotNull(msalAuthResult.IdToken);
            Assert.AreEqual(user.Upn, msalAuthResult.Account.Username);
        }

        private void ValidateAdalAuthResult()
        {
            Assert.IsNotNull(adalAuthResult);
            Assert.IsNotNull(adalAuthResult.AccessToken);
            Assert.IsNotNull(adalAuthResult.IdToken);
            Assert.AreEqual(user.Upn, adalAuthResult.UserInfo.DisplayableId);
        }

        [TestMethod]
        [TestCategory("UnifiedCache_IntegrationTests")]
        public async Task AdalRt_CreatedByMsalV2_UsedByAdalV4Async()
        {
            await AcquireTokensUsingMsalAsync().ConfigureAwait(false);

            ClearMsalCache();
            AssertMsalCacheIsEmpty();

            // passing empty password to make sure that token returned silenlty - using RT
            adalAuthResult = await adal::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions.
                AcquireTokenAsync(adalContext, AdalResource1, ClientId,
                new adal::Microsoft.IdentityModel.Clients.ActiveDirectory.UserPasswordCredential(user.Upn, "")).ConfigureAwait(false);

            ValidateAdalAuthResult();
        }

        [TestMethod]
        [TestCategory("UnifiedCache_IntegrationTests")]
        public async Task AdalRt_CreatedByMsalV2_UsedByMsalV2Async()
        {
            await AcquireTokensUsingMsalAsync().ConfigureAwait(false);

            ClearMsalCache();
            AssertMsalCacheIsEmpty();

            var msalAccounts = await msalPublicClient.GetAccountsAsync().ConfigureAwait(false);
            Assert.AreEqual(1, msalAccounts.Count());

            msalAuthResult = await msalPublicClient.AcquireTokenSilentAsync(MsalScopes, msalAccounts.First()).ConfigureAwait(false);
            ValidateMsalAuthResult();
        }

        [TestMethod]
        [TestCategory("UnifiedCache_IntegrationTests")]
        public async Task MsalRt_CreatedByMsalV2_UsedByAdalV4Async()
        {
            await AcquireTokensUsingMsalAsync().ConfigureAwait(false);

            ClearAdalCache();
            AssertAdalCacheIsEmpty();

            // passing empty password to make sure that token returned silenlty - using RT
            adalAuthResult = await adal::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions.
                AcquireTokenAsync(adalContext, AdalResource1, ClientId,
                new adal::Microsoft.IdentityModel.Clients.ActiveDirectory.UserPasswordCredential(user.Upn, "")).ConfigureAwait(false);

            ValidateAdalAuthResult();
        }

        [TestMethod]
        [TestCategory("UnifiedCache_IntegrationTests")]
        public async Task MsalRt_CreatedByMsalV2_UsedByMsalV2Async()
        {
            await AcquireTokensUsingMsalAsync().ConfigureAwait(false);

            ClearAdalCache();
            AssertAdalCacheIsEmpty();

            var msalAccounts = await msalPublicClient.GetAccountsAsync().ConfigureAwait(false);
            Assert.AreEqual(1, msalAccounts.Count());

            msalAuthResult = await msalPublicClient.AcquireTokenSilentAsync(MsalScopes, msalAccounts.First()).ConfigureAwait(false);
            ValidateMsalAuthResult();
        }

        private async Task AcquireTokensUsingAdalAsync()
        {
            adalAuthResult = await adal::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions.
                AcquireTokenAsync(adalContext, AdalResource1, ClientId,
                new adal::Microsoft.IdentityModel.Clients.ActiveDirectory.UserPasswordCredential(user.Upn, securePassword)).ConfigureAwait(false);

            ValidateAdalAuthResult();

            adalAuthResult = await adal::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions.
                AcquireTokenAsync(adalContext, AdalResource2, ClientId,
                new adal::Microsoft.IdentityModel.Clients.ActiveDirectory.UserPasswordCredential(user.Upn, securePassword)).ConfigureAwait(false);
            ValidateAdalAuthResult();
        }

        private async Task AcquireTokensUsingMsalAsync()
        {
            msalAuthResult =
                await msalPublicClient.AcquireTokenByUsernamePasswordAsync(MsalScopes, user.Upn, securePassword).ConfigureAwait(false);
            ValidateMsalAuthResult();

            msalAuthResult =
                await msalPublicClient.AcquireTokenByUsernamePasswordAsync(MsalScopes1, user.Upn, securePassword).ConfigureAwait(false);
            ValidateMsalAuthResult();
        }

        [TestMethod]
        [TestCategory("UnifiedCache_IntegrationTests")]
        public async Task MsalRt_CreatedByAdalV4_UsedByMsalV2Async()
        {
            await AcquireTokensUsingAdalAsync().ConfigureAwait(false);

            ClearAdalCache();
            AssertAdalCacheIsEmpty();

            var msalAccounts = await msalPublicClient.GetAccountsAsync().ConfigureAwait(false);
            Assert.AreEqual(1, msalAccounts.Count());

            msalAuthResult = await msalPublicClient.AcquireTokenSilentAsync(MsalScopes, msalAccounts.First()).ConfigureAwait(false);
            ValidateMsalAuthResult();
        }

        [TestMethod]
        [TestCategory("UnifiedCache_IntegrationTests")]
        public async Task MsalRt_CreatedByAdalV4_UsedByAdalV4Async()
        {
            await AcquireTokensUsingAdalAsync().ConfigureAwait(false);

            ClearAdalCache();
            AssertAdalCacheIsEmpty();

            // passing empty password to make sure that token returned silenlty - using RT
            adalAuthResult = await adal::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions.
                AcquireTokenAsync(adalContext, AdalResource1, ClientId,
                new adal::Microsoft.IdentityModel.Clients.ActiveDirectory.UserPasswordCredential(user.Upn, "")).ConfigureAwait(false);

            ValidateAdalAuthResult();
        }

        [TestMethod]
        [TestCategory("UnifiedCache_IntegrationTests")]
        public async Task AdalRt_CreatedByAdalV4_UsedByMsalV2Async()
        {
            await AcquireTokensUsingAdalAsync().ConfigureAwait(false);

            ClearMsalCache();
            AssertMsalCacheIsEmpty();

            var msalAccounts = await msalPublicClient.GetAccountsAsync().ConfigureAwait(false);
            Assert.AreEqual(1, msalAccounts.Count());

            msalAuthResult = await msalPublicClient.AcquireTokenSilentAsync(MsalScopes, msalAccounts.First()).ConfigureAwait(false);
            ValidateMsalAuthResult();
        }

        [TestMethod]
        [TestCategory("UnifiedCache_IntegrationTests")]
        public async Task AdalRt_CreatedByAdalV4_UsedByAdalV4Async()
        {
            await AcquireTokensUsingAdalAsync().ConfigureAwait(false);

            ClearMsalCache();
            AssertMsalCacheIsEmpty();

            // passing empty password to make sure that token returned silenlty - using RT
            adalAuthResult = await adal::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions.
                AcquireTokenAsync(adalContext, AdalResource1, ClientId,
                new adal::Microsoft.IdentityModel.Clients.ActiveDirectory.UserPasswordCredential(user.Upn, "")).ConfigureAwait(false);

            ValidateAdalAuthResult();
        }

        private void ClearAdalCache()
        {
            adalCache.BeforeAccess(new adal::Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCacheNotificationArgs
            {
                TokenCache = adalCache
            });

            adalCache.ClearAdalCache();
            adalCache.HasStateChanged = true;

            adalCache.AfterAccess(new adal::Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCacheNotificationArgs
            {
                TokenCache = adalCache
            });
        }

        private void ClearMsalCache()
        {
            adalCache.BeforeAccess(new adal::Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCacheNotificationArgs
            {
                TokenCache = adalCache
            });

            adalCache.ClearMsalCache();
            adalCache.HasStateChanged = true;

            adalCache.AfterAccess(new adal::Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCacheNotificationArgs
            {
                TokenCache = adalCache
            });
        }

        private void UpdateAdalCacheSetClientInfoToNull()
        {
            adalCache.BeforeAccess(new adal::Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCacheNotificationArgs
            {
                TokenCache = adalCache
            });

            foreach (var adalResultWrapper in adalCache.tokenCacheDictionary.Values)
            {
                adalResultWrapper.RawClientInfo = null;
            }
            adalCache.HasStateChanged = true;
            adalCache.AfterAccess(new adal::Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCacheNotificationArgs
            {
                TokenCache = adalCache
            });
        }

        [TestMethod]
        [TestCategory("UnifiedCache_IntegrationTests")]
        public async Task UnifiedCache_Adalv3ToMsal2MigrationIntegrationTestAsync()
        {
            // acquire adal tokens using adalV4
            adalAuthResult = await adal::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions.
                AcquireTokenAsync(adalContext, AdalResource1, ClientId,
                new adal::Microsoft.IdentityModel.Clients.ActiveDirectory.UserPasswordCredential(user.Upn, securePassword)).ConfigureAwait(false);
            ValidateAdalAuthResult();

            adalAuthResult = await adal::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions.
                AcquireTokenAsync(adalContext, AdalResource2, ClientId,
                new adal::Microsoft.IdentityModel.Clients.ActiveDirectory.UserPasswordCredential(user.Upn, securePassword)).ConfigureAwait(false);
            ValidateAdalAuthResult();

            // simulate adalV3 token cache state by setting client info in adal cache entities to null 
            // and clearing msal cache
            UpdateAdalCacheSetClientInfoToNull();
            ClearMsalCache();
            AssertMsalCacheIsEmpty();

            // make sure that adal v3 RT is visible for Msal
            var msalAccounts = await msalPublicClient.GetAccountsAsync().ConfigureAwait(false);
            Assert.AreEqual(1, msalAccounts.Count());
            var account = msalAccounts.First();
            Assert.AreEqual(user.Upn, account.Username);
            Assert.IsNull(account.HomeAccountId);
            Assert.IsNotNull(account.Environment);

            // make sure that adal v3 RT is usable by Msal
            msalAuthResult = await msalPublicClient.AcquireTokenSilentAsync(MsalScopes, account).ConfigureAwait(false);
            ValidateMsalAuthResult();

            // make sure Msal remove account api remove corresponding cache entities in all formats  
            msalAccounts = await msalPublicClient.GetAccountsAsync().ConfigureAwait(false);
            Assert.AreEqual(1, msalAccounts.Count());
            account = msalAccounts.First();

            await msalPublicClient.RemoveAsync(account).ConfigureAwait(false);

            AssertAdalCacheIsEmpty();
            AssertMsalCacheIsEmpty();
        }

        [TestMethod]
        [TestCategory("UnifiedCache_IntegrationTests")]
        public async Task UnifiedCache_Adalv3ToAdalV4ToMsal2MigrationIntegrationTestAsync()
        {
            // acquire adal tokens using adalV4
            adalAuthResult = await adal::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions.
                AcquireTokenAsync(adalContext, AdalResource1, ClientId,
                new adal::Microsoft.IdentityModel.Clients.ActiveDirectory.UserPasswordCredential(user.Upn, securePassword)).ConfigureAwait(false);
            ValidateAdalAuthResult();

            // simulate adalV3 token cache state by setting client info in adal cache entities to null 
            // and clearing msal cache
            UpdateAdalCacheSetClientInfoToNull();
            ClearMsalCache();
            AssertMsalCacheIsEmpty();


            // Migration to AdalV4 - acquire adal tokens using adalV4

            // make sure that AT in AdalV3 format is used by AdalV4
            Assert.AreEqual(1, adalCache.ReadItems().Count());
            adalAuthResult = await adalContext.AcquireTokenSilentAsync(AdalResource1, ClientId).ConfigureAwait(false);
            Assert.AreEqual(1, adalCache.ReadItems().Count());

            // acquire token to different resource
            adalAuthResult = await adal::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions.
                AcquireTokenAsync(adalContext, AdalResource2, ClientId,
                new adal::Microsoft.IdentityModel.Clients.ActiveDirectory.UserPasswordCredential(user.Upn, securePassword)).ConfigureAwait(false);
            ValidateAdalAuthResult();

            // At this poing Adal cache contains RTs for the same account in diff format v3 and v4
            Assert.IsTrue(adalCache.ReadItems().Count() == 2);

            var msalAccounts = await msalPublicClient.GetAccountsAsync().ConfigureAwait(false);
            Assert.AreEqual(1, msalAccounts.Count());
            var account = msalAccounts.First();
            Assert.AreEqual(user.Upn, account.Username);
            // make sure for the same account RT in V4 format preffered over V3 format
            Assert.IsNotNull(account.HomeAccountId);
            Assert.IsNotNull(account.Environment);

            // validate that Adal writes only RT and Account cache entities in Msal format
            Assert.AreEqual(0, msalCache.tokenCacheAccessor.GetAllAccessTokensAsString().Count);
            Assert.AreEqual(1, msalCache.tokenCacheAccessor.GetAllRefreshTokensAsString().Count);
            Assert.AreEqual(0, msalCache.tokenCacheAccessor.GetAllIdTokensAsString().Count);
            Assert.AreEqual(1, msalCache.tokenCacheAccessor.GetAllAccountsAsString().Count);

            // make sure that adal v4 RT is usable by Msal
            msalAuthResult = await msalPublicClient.AcquireTokenSilentAsync(MsalScopes, account).ConfigureAwait(false);
            ValidateMsalAuthResult();

            // make sure Msal remove account api remove corresponding cache entities in all formats  
            msalAccounts = await msalPublicClient.GetAccountsAsync().ConfigureAwait(false);
            Assert.AreEqual(1, msalAccounts.Count());
            account = msalAccounts.First();

            await msalPublicClient.RemoveAsync(account).ConfigureAwait(false);

            AssertAdalCacheIsEmpty();
            AssertMsalCacheIsEmpty();
        }

        [TestMethod]
        [TestCategory("UnifiedCache_IntegrationTests")]
        public async Task UnifiedCache_Adal_ClearCacheAsync()
        {
            adalAuthResult = await adal::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions.
                AcquireTokenAsync(adalContext, AdalResource1, ClientId,
            new adal::Microsoft.IdentityModel.Clients.ActiveDirectory.UserPasswordCredential(user.Upn, securePassword)).ConfigureAwait(false);

            Assert.IsTrue(adalCache.ReadItems().Count() > 0);
            var accounts = await msalPublicClient.GetAccountsAsync().ConfigureAwait(false);
            Assert.IsTrue(accounts.Count() > 0);

            adalCache.Clear();

            AssertAdalCacheIsEmpty();
            AssertMsalCacheIsEmpty();
        }

        [TestMethod]
        [TestCategory("UnifiedCache_IntegrationTests")]
        public async Task UnifiedCache_Msal_ClearCacheAsync()
        {
            adalAuthResult = await adal::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions.
                AcquireTokenAsync(adalContext, AdalResource1, ClientId,
            new adal::Microsoft.IdentityModel.Clients.ActiveDirectory.UserPasswordCredential(user.Upn, securePassword)).ConfigureAwait(false);

            Assert.IsTrue(adalCache.ReadItems().Count() > 0);
            var accounts = await msalPublicClient.GetAccountsAsync().ConfigureAwait(false);
            Assert.IsTrue(accounts.Count() > 0);

            msalCache.Clear();

            AssertAdalCacheIsEmpty();
            AssertMsalCacheIsEmpty();
        }

        private void AssertAdalCacheIsEmpty()
        {
            Assert.IsTrue(adalCache.ReadItems().Count() == 0);
        }

        private void AssertMsalCacheIsEmpty()
        {
            msalCache.BeforeAccess(new msal::Microsoft.Identity.Client.TokenCacheNotificationArgs
            {
                TokenCache = msalCache
            });

            Assert.IsTrue(msalCache.tokenCacheAccessor.GetAllAccessTokensAsString().Count == 0);
            Assert.IsTrue(msalCache.tokenCacheAccessor.GetAllRefreshTokensAsString().Count == 0);
            Assert.IsTrue(msalCache.tokenCacheAccessor.GetAllIdTokensAsString().Count == 0);
            Assert.IsTrue(msalCache.tokenCacheAccessor.GetAllAccountsAsString().Count == 0);
        }

        private static Microsoft.Identity.LabInfrastructure.IUser GetUser(UserQueryParameters query)
        {
            ILabService _labService = new LabServiceApi(new KeyVaultSecretsProvider());
            var user = _labService.GetUser(query);
            Assert.IsTrue(user != null, "Found no users for the given query.");
            return user;
        }
    }
}