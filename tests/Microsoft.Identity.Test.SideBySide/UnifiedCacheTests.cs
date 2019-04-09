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

using System.Globalization;
using System.Linq;
using System.Net;
using System.Security;
using System.Threading.Tasks;
using msal::Microsoft.Identity.Client;
using Microsoft.Identity.Test.LabInfrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace Microsoft.Identity.Test.SideBySide
{
    [TestClass]
    public class UnifiedCacheTests
    {
        public const string ClientId = "0615b6ca-88d4-4884-8729-b178178f7c27";

        public const string AuthorityTemplate = "https://login.microsoftonline.com/{0}/";

        public string[] _msalScopes = { "https://graph.microsoft.com/.default" };
        public string[] _msalScopes1 = { "https://graph.windows.net/.default" };

        private const string AdalResource1 = "https://graph.windows.net";
        private const string AdalResource2 = "https://graph.microsoft.com";

        private LabUser _user;
        private SecureString _securePassword;
        private string _authority;

        private byte[] _adalV3StateStorage;
        private byte[] _unifiedStateStorage;

        [TestInitialize]
        public void TestInitialize()
        {
            _adalV3StateStorage = null;
            _unifiedStateStorage = null;
            if (_user == null)
            {
                _user = LabUserHelper.GetDefaultUser().User;

                _securePassword = new NetworkCredential("", _user.GetOrFetchPassword()).SecurePassword;
                _authority = string.Format(
                    CultureInfo.InvariantCulture, 
                    AuthorityTemplate, 
                    _user.CurrentTenantId);
            }

            InitAdal();
            InitMsal();
        }

        private void AdalDoBefore(
            global::Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCacheNotificationArgs args)
        {
            global::Microsoft.Identity.Core.Cache.CacheData cacheData = new global::Microsoft.Identity.Core.Cache.CacheData()
            {
                AdalV3State = _adalV3StateStorage,
                UnifiedState = _unifiedStateStorage
            };

            args.TokenCache.DeserializeAdalAndUnifiedCache(cacheData);
        }

        private void AdalDoAfter(
            global::Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCacheNotificationArgs args)
        {
            if (args.TokenCache.HasStateChanged)
            {
                global::Microsoft.Identity.Core.Cache.CacheData cacheData = args.TokenCache.SerializeAdalAndUnifiedCache();

                _adalV3StateStorage = cacheData.AdalV3State;
                _unifiedStateStorage = cacheData.UnifiedState;

                args.TokenCache.HasStateChanged = false;
            }
        }

        private void MsalDoBefore(msal::Microsoft.Identity.Client.TokenCacheNotificationArgs args)
        {
            args.TokenCache.DeserializeAdalV3(_adalV3StateStorage);
            args.TokenCache.DeserializeMsalV2(_unifiedStateStorage);
        }

        private void MsalDoAfter(msal::Microsoft.Identity.Client.TokenCacheNotificationArgs args)
        {
            if (args.HasStateChanged)
            {
                _adalV3StateStorage = args.TokenCache.SerializeAdalV3();
                _unifiedStateStorage = args.TokenCache.SerializeMsalV2();
            }
        }

        private global::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext _adalContext;
        private global::Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCache _adalCache;
        private global::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationResult _adalAuthResult;

        private void InitAdal()
        {
            _adalCache = new global::Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCache()
            {
                BeforeAccess = AdalDoBefore,
                AfterAccess = AdalDoAfter
            };
            _adalContext = new global::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(
                _authority, _adalCache);
        }

        private msal::Microsoft.Identity.Client.PublicClientApplication _msalPublicClient;
        private msal::Microsoft.Identity.Client.AuthenticationResult _msalAuthResult;

        private void InitMsal()
        {

            _msalPublicClient = PublicClientApplicationBuilder
                .Create(ClientId)
                .WithAuthority(_authority)
                .BuildConcrete();

            _msalPublicClient.UserTokenCache.SetBeforeAccess(MsalDoBefore);
            _msalPublicClient.UserTokenCache.SetAfterAccess(MsalDoAfter);
        }

        private void ValidateMsalAuthResult()
        {
            Assert.IsNotNull(_msalAuthResult);
            Assert.IsNotNull(_msalAuthResult.AccessToken);
            Assert.IsNotNull(_msalAuthResult.IdToken);
            Assert.AreEqual(_user.Upn, _msalAuthResult.Account.Username);
        }

        private void ValidateAdalAuthResult()
        {
            Assert.IsNotNull(_adalAuthResult);
            Assert.IsNotNull(_adalAuthResult.AccessToken);
            Assert.IsNotNull(_adalAuthResult.IdToken);
            Assert.AreEqual(_user.Upn, _adalAuthResult.UserInfo.DisplayableId);
        }

        [TestMethod]
        [TestCategory("UnifiedCache_IntegrationTests")]
        public async Task AdalRt_CreatedByMsalV2_UsedByAdalV4Async()
        {
            await AcquireTokensUsingMsalAsync().ConfigureAwait(false);

            ClearMsalCache();
            AssertMsalCacheIsEmpty();

            // passing empty password to make sure that token returned silenlty - using RT
            _adalAuthResult = await global::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions.
                AcquireTokenAsync(_adalContext, AdalResource1, ClientId,
                new global::Microsoft.IdentityModel.Clients.ActiveDirectory.UserPasswordCredential(_user.Upn, "")).ConfigureAwait(false);

            ValidateAdalAuthResult();
        }

        [TestMethod]
        [TestCategory("UnifiedCache_IntegrationTests")]
        public async Task AdalRt_CreatedByMsalV2_UsedByMsalV2Async()
        {
            await AcquireTokensUsingMsalAsync().ConfigureAwait(false);

            ClearMsalCache();
            AssertMsalCacheIsEmpty();

            var msalAccounts = await _msalPublicClient.GetAccountsAsync().ConfigureAwait(false);
            Assert.AreEqual(1, msalAccounts.Count());

            _msalAuthResult = await _msalPublicClient
                .AcquireTokenSilent(_msalScopes, msalAccounts.First())
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

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
            _adalAuthResult = await global::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions.
                AcquireTokenAsync(_adalContext, AdalResource1, ClientId,
                new global::Microsoft.IdentityModel.Clients.ActiveDirectory.UserPasswordCredential(_user.Upn, "")).ConfigureAwait(false);

            ValidateAdalAuthResult();
        }

        [TestMethod]
        [TestCategory("UnifiedCache_IntegrationTests")]
        public async Task MsalRt_CreatedByMsalV2_UsedByMsalV2Async()
        {
            await AcquireTokensUsingMsalAsync().ConfigureAwait(false);

            ClearAdalCache();
            AssertAdalCacheIsEmpty();

            var msalAccounts = await _msalPublicClient.GetAccountsAsync().ConfigureAwait(false);
            Assert.AreEqual(1, msalAccounts.Count());

            _msalAuthResult = await _msalPublicClient
                .AcquireTokenSilent(_msalScopes, msalAccounts.First())
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            ValidateMsalAuthResult();
        }

        private async Task AcquireTokensUsingAdalAsync()
        {
            _adalAuthResult = await global::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions.
                AcquireTokenAsync(_adalContext, AdalResource1, ClientId,
                new global::Microsoft.IdentityModel.Clients.ActiveDirectory.UserPasswordCredential(_user.Upn, _securePassword)).ConfigureAwait(false);

            ValidateAdalAuthResult();

            _adalAuthResult = await global::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions.
                AcquireTokenAsync(_adalContext, AdalResource2, ClientId,
                new global::Microsoft.IdentityModel.Clients.ActiveDirectory.UserPasswordCredential(_user.Upn, _securePassword)).ConfigureAwait(false);
            ValidateAdalAuthResult();
        }

        private async Task AcquireTokensUsingMsalAsync()
        {
            _msalAuthResult = await _msalPublicClient
                .AcquireTokenByUsernamePassword(_msalScopes, _user.Upn, _securePassword)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            ValidateMsalAuthResult();

            _msalAuthResult = await _msalPublicClient
                .AcquireTokenByUsernamePassword(_msalScopes1, _user.Upn, _securePassword)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            ValidateMsalAuthResult();
        }

        [TestMethod]
        [TestCategory("UnifiedCache_IntegrationTests")]
        public async Task MsalRt_CreatedByAdalV4_UsedByMsalV2Async()
        {
            await AcquireTokensUsingAdalAsync().ConfigureAwait(false);

            ClearAdalCache();
            AssertAdalCacheIsEmpty();

            var msalAccounts = await _msalPublicClient.GetAccountsAsync().ConfigureAwait(false);
            Assert.AreEqual(1, msalAccounts.Count());

            _msalAuthResult = await _msalPublicClient
                .AcquireTokenSilent(_msalScopes, msalAccounts.First())
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

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
            _adalAuthResult = await global::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions.
                AcquireTokenAsync(_adalContext, AdalResource1, ClientId,
                new global::Microsoft.IdentityModel.Clients.ActiveDirectory.UserPasswordCredential(_user.Upn, "")).ConfigureAwait(false);

            ValidateAdalAuthResult();
        }

        [TestMethod]
        [TestCategory("UnifiedCache_IntegrationTests")]
        public async Task AdalRt_CreatedByAdalV4_UsedByMsalV2Async()
        {
            await AcquireTokensUsingAdalAsync().ConfigureAwait(false);

            ClearMsalCache();
            AssertMsalCacheIsEmpty();

            var msalAccounts = await _msalPublicClient.GetAccountsAsync().ConfigureAwait(false);
            Assert.AreEqual(1, msalAccounts.Count());

            _msalAuthResult = await _msalPublicClient
                .AcquireTokenSilent(_msalScopes, msalAccounts.First())
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

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
            _adalAuthResult = await global::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions.
                AcquireTokenAsync(_adalContext, AdalResource1, ClientId,
                new global::Microsoft.IdentityModel.Clients.ActiveDirectory.UserPasswordCredential(_user.Upn, "")).ConfigureAwait(false);

            ValidateAdalAuthResult();
        }

        private void ClearAdalCache()
        {
            _adalCache.BeforeAccess(new global::Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCacheNotificationArgs
            {
                TokenCache = _adalCache
            });

            _adalCache.ClearAdalCache();
            _adalCache.HasStateChanged = true;

            _adalCache.AfterAccess(new global::Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCacheNotificationArgs
            {
                TokenCache = _adalCache
            });
        }

        private void ClearMsalCache()
        {
            _adalCache.BeforeAccess(new global::Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCacheNotificationArgs
            {
                TokenCache = _adalCache
            });

            _msalPublicClient.UserTokenCacheInternal.Clear();

            _adalCache.ClearMsalCache();
            _adalCache.HasStateChanged = true;

            _adalCache.AfterAccess(new global::Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCacheNotificationArgs
            {
                TokenCache = _adalCache
            });
        }

        private void UpdateAdalCacheSetClientInfoToNull()
        {
            _adalCache.BeforeAccess(new global::Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCacheNotificationArgs
            {
                TokenCache = _adalCache
            });

            foreach (var adalResultWrapper in _adalCache.tokenCacheDictionary.Values)
            {
                adalResultWrapper.RawClientInfo = null;
            }
            _adalCache.HasStateChanged = true;
            _adalCache.AfterAccess(new global::Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCacheNotificationArgs
            {
                TokenCache = _adalCache
            });
        }

        [TestMethod]
        [TestCategory("UnifiedCache_IntegrationTests")]
        public async Task UnifiedCache_Adalv3ToMsal2MigrationIntegrationTestAsync()
        {
            // acquire adal tokens using adalV4
            _adalAuthResult = await global::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions.
                AcquireTokenAsync(_adalContext, AdalResource1, ClientId,
                new global::Microsoft.IdentityModel.Clients.ActiveDirectory.UserPasswordCredential(_user.Upn, _securePassword)).ConfigureAwait(false);
            ValidateAdalAuthResult();

            _adalAuthResult = await global::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions.
                AcquireTokenAsync(_adalContext, AdalResource2, ClientId,
                new global::Microsoft.IdentityModel.Clients.ActiveDirectory.UserPasswordCredential(_user.Upn, _securePassword)).ConfigureAwait(false);
            ValidateAdalAuthResult();

            // simulate adalV3 token cache state by setting client info in adal cache entities to null 
            // and clearing msal cache
            UpdateAdalCacheSetClientInfoToNull();
            ClearMsalCache();
            AssertMsalCacheIsEmpty();

            // make sure that adal v3 RT is visible for Msal
            var msalAccounts = await _msalPublicClient.GetAccountsAsync().ConfigureAwait(false);
            Assert.AreEqual(1, msalAccounts.Count());
            var account = msalAccounts.First();
            Assert.AreEqual(_user.Upn, account.Username);
            Assert.IsNull(account.HomeAccountId);
            Assert.IsNotNull(account.Environment);

            // make sure that adal v3 RT is usable by Msal
            _msalAuthResult = await _msalPublicClient
                .AcquireTokenSilent(_msalScopes, account)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            ValidateMsalAuthResult();

            // make sure Msal remove account api remove corresponding cache entities in all formats  
            msalAccounts = await _msalPublicClient.GetAccountsAsync().ConfigureAwait(false);
            Assert.AreEqual(1, msalAccounts.Count());
            account = msalAccounts.First();

            await _msalPublicClient.RemoveAsync(account).ConfigureAwait(false);

            AssertAdalCacheIsEmpty();
            AssertNoCredentialsInMsalCache();
        }

        [TestMethod]
        [TestCategory("UnifiedCache_IntegrationTests")]
        public async Task UnifiedCache_Adalv3ToAdalV4ToMsal2MigrationIntegrationTestAsync()
        {
            // acquire adal tokens using adalV4
            _adalAuthResult = await global::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions.
                AcquireTokenAsync(_adalContext, AdalResource1, ClientId,
                new global::Microsoft.IdentityModel.Clients.ActiveDirectory.UserPasswordCredential(_user.Upn, _securePassword)).ConfigureAwait(false);
            ValidateAdalAuthResult();

            // simulate adalV3 token cache state by setting client info in adal cache entities to null 
            // and clearing msal cache
            UpdateAdalCacheSetClientInfoToNull();
            ClearMsalCache();
            AssertMsalCacheIsEmpty();


            // Migration to AdalV4 - acquire adal tokens using adalV4

            // make sure that AT in AdalV3 format is used by AdalV4
            Assert.AreEqual(1, _adalCache.ReadItems().Count());
            _adalAuthResult = await _adalContext.AcquireTokenSilentAsync(AdalResource1, ClientId).ConfigureAwait(false);
            Assert.AreEqual(1, _adalCache.ReadItems().Count());

            // acquire token to different resource
            _adalAuthResult = await global::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions.
                AcquireTokenAsync(_adalContext, AdalResource2, ClientId,
                new global::Microsoft.IdentityModel.Clients.ActiveDirectory.UserPasswordCredential(_user.Upn, _securePassword)).ConfigureAwait(false);
            ValidateAdalAuthResult();

            // At this poing Adal cache contains RTs for the same account in diff format v3 and v4
            Assert.IsTrue(_adalCache.ReadItems().Count() == 2);

            var msalAccounts = await _msalPublicClient.GetAccountsAsync().ConfigureAwait(false);
            Assert.AreEqual(1, msalAccounts.Count());
            var account = msalAccounts.First();
            Assert.AreEqual(_user.Upn, account.Username);
            // make sure for the same account RT in V4 format preffered over V3 format
            Assert.IsNotNull(account.HomeAccountId);
            Assert.IsNotNull(account.Environment);

            // validate that Adal writes only RT and Account cache entities in Msal format
            Assert.AreEqual(0, _msalPublicClient.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            Assert.AreEqual(1, _msalPublicClient.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());
            Assert.AreEqual(0, _msalPublicClient.UserTokenCacheInternal.Accessor.GetAllIdTokens().Count());
            Assert.AreEqual(1, _msalPublicClient.UserTokenCacheInternal.Accessor.GetAllAccounts().Count());

            // make sure that adal v4 RT is usable by Msal
            _msalAuthResult = await _msalPublicClient
                .AcquireTokenSilent(_msalScopes, account)
                .ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            ValidateMsalAuthResult();

            // make sure Msal remove account api remove corresponding cache entities in all formats  
            msalAccounts = await _msalPublicClient.GetAccountsAsync().ConfigureAwait(false);
            Assert.AreEqual(1, msalAccounts.Count());
            account = msalAccounts.First();

            await _msalPublicClient.RemoveAsync(account).ConfigureAwait(false);

            AssertAdalCacheIsEmpty();
            AssertNoCredentialsInMsalCache();
        }

        [TestMethod]
        [TestCategory("UnifiedCache_IntegrationTests")]
        public async Task UnifiedCache_Adal_ClearCacheAsync()
        {
            _adalAuthResult = await global::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions.
                AcquireTokenAsync(_adalContext, AdalResource1, ClientId,
            new global::Microsoft.IdentityModel.Clients.ActiveDirectory.UserPasswordCredential(_user.Upn, _securePassword)).ConfigureAwait(false);

            Assert.IsTrue(_adalCache.ReadItems().Any());
            var accounts = await _msalPublicClient.GetAccountsAsync().ConfigureAwait(false);
            Assert.IsTrue(accounts.Any());

            _adalCache.Clear();
            _msalPublicClient.UserTokenCacheInternal.Clear();

            AssertAdalCacheIsEmpty();
            AssertMsalCacheIsEmpty();
        }

        [TestMethod]
        [TestCategory("UnifiedCache_IntegrationTests")]
        public async Task UnifiedCache_Msal_ClearCacheAsync()
        {
            _adalAuthResult = await global::Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions.
                AcquireTokenAsync(_adalContext, AdalResource1, ClientId,
            new global::Microsoft.IdentityModel.Clients.ActiveDirectory.UserPasswordCredential(_user.Upn, _securePassword)).ConfigureAwait(false);

            Assert.IsTrue(_adalCache.ReadItems().Any());
            var accounts = await _msalPublicClient.GetAccountsAsync().ConfigureAwait(false);
            Assert.IsTrue(accounts.Any());

            _msalPublicClient.UserTokenCacheInternal.Clear();

            AssertAdalCacheIsEmpty();
            AssertMsalCacheIsEmpty();
        }

        private void AssertAdalCacheIsEmpty()
        {
            Assert.IsFalse(_adalCache.ReadItems().Any());
        }

        private void AssertMsalCacheIsEmpty()
        {
            Assert.AreEqual(0, _msalPublicClient.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            Assert.AreEqual(0, _msalPublicClient.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());
            Assert.AreEqual(0, _msalPublicClient.UserTokenCacheInternal.Accessor.GetAllIdTokens().Count());
            Assert.AreEqual(0, _msalPublicClient.UserTokenCacheInternal.Accessor.GetAllAccounts().Count());
        }

        private void AssertNoCredentialsInMsalCache()
        {
            Assert.AreEqual(0, _msalPublicClient.UserTokenCacheInternal.Accessor.GetAllAccessTokens().Count());
            Assert.AreEqual(0, _msalPublicClient.UserTokenCacheInternal.Accessor.GetAllRefreshTokens().Count());
            Assert.AreEqual(0, _msalPublicClient.UserTokenCacheInternal.Accessor.GetAllIdTokens().Count());
            Assert.AreEqual(0, _msalPublicClient.UserTokenCacheInternal.Accessor.GetAllAccounts().Count());
        }
    }
}
