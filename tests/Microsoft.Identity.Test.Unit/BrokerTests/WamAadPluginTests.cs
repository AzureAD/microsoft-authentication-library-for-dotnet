using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Platforms.Features.WamBroker;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace Microsoft.Identity.Test.Unit.BrokerTests
{
    [TestClass]
    public class WamAadPluginTests : TestBase
    {
        private ICoreLogger _logger;
        private IWamPlugin _aadPlugin;
        private IWamProxy _wamProxy;
        private IWebAccountProviderFactory _webAccountProviderFactory;
        private IAccountPickerFactory _accountPickerFactory;
        private ICacheSessionManager _cacheSessionManager;
        private IInstanceDiscoveryManager _instanceDiscoveryManager;

        private MsalTokenResponse _msalTokenResponse = new MsalTokenResponse
        {
            IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
            AccessToken = "access-token",
            ClientInfo = MockHelpers.CreateClientInfo(),
            ExpiresIn = 3599,
            CorrelationId = "correlation-id",
            RefreshToken = null, // brokers don't return RT
            Scope = TestConstants.s_scope.AsSingleString(),
            TokenType = "Bearer",
            WamAccountId = "wam_account_id",
        };

        [TestInitialize]
        public void Init()
        {
            _logger = Substitute.For<ICoreLogger>();
            _wamProxy = Substitute.For<IWamProxy>();
            _webAccountProviderFactory = Substitute.For<IWebAccountProviderFactory>();
            _accountPickerFactory = Substitute.For<IAccountPickerFactory>();

            _webAccountProviderFactory.ClearReceivedCalls();
            _cacheSessionManager = Substitute.For<ICacheSessionManager>();

            _instanceDiscoveryManager = Substitute.For<IInstanceDiscoveryManager>();

            _aadPlugin = new AadPlugin(_wamProxy, _webAccountProviderFactory, _logger);
        }

        [TestMethod]
        public async Task GetAccounts_NoAccounts_Async()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {

                // Act
                var accounts = await _aadPlugin.GetAccountsAsync(
                    TestConstants.ClientId,
                    AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityCommonTenant, true),
                    _cacheSessionManager,
                    _instanceDiscoveryManager).ConfigureAwait(false);

                // Assert
                Assert.AreEqual(0, accounts.Count());
            }
        }

        [TestMethod]
        public async Task GetAccounts_WamAccounts_NoAuthority_Async()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                var wamAccountProvider = new WebAccountProvider("id", "user1@contoso.com", null);
                _webAccountProviderFactory.GetAccountProviderAsync("organizations").Returns(wamAccountProvider);
                var wamAccount = new WebAccount(wamAccountProvider, "user1@contoso.com", WebAccountState.Connected);
                // no authority ... skip these accounts
                _wamProxy.FindAllWebAccountsAsync(wamAccountProvider, TestConstants.ClientId).Returns(new[] { wamAccount });

                // Act
                var accounts = await _aadPlugin.GetAccountsAsync(
                    TestConstants.ClientId,
                    AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityCommonTenant, true),
                    _cacheSessionManager,
                    _instanceDiscoveryManager).ConfigureAwait(false);

                // Assert
                Assert.AreEqual(0, accounts.Count());

            }
        }

        [TestMethod]
        public async Task GetAccounts_WamAccounts_DifferentCloud_Async()
        {
            const string BogusAuthority = "https://login.bogus.com/common";

            // Arrange
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                var wamAccountProvider = new WebAccountProvider("id", "user1@contoso.com", null);
                _webAccountProviderFactory.GetAccountProviderAsync("organizations").Returns(wamAccountProvider);
                var wamAccount = new WebAccount(wamAccountProvider, "user1@contoso.com", WebAccountState.Connected);
                _wamProxy.FindAllWebAccountsAsync(wamAccountProvider, TestConstants.ClientId).Returns(new[] { wamAccount });
                _wamProxy.TryGetAccountProperty(wamAccount, "Authority", out string _).Returns(x =>
                {
                    x[2] = BogusAuthority;
                    return true;
                });
                var rq = new Client.Internal.RequestContext(harness.ServiceBundle, Guid.NewGuid(), default);
                _cacheSessionManager.RequestContext.Returns(rq);

                _instanceDiscoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                    Arg.Any<AuthorityInfo>(),
                    Arg.Any<IEnumerable<string>>(), rq)
                    .Returns(CreateEntryForSingleAuthority(new Uri(TestConstants.AuthorityCommonTenant))); // user set this authority in config

                // Act
                var accounts = await _aadPlugin.GetAccountsAsync(
                    TestConstants.ClientId,
                    AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityCommonTenant, true),
                    _cacheSessionManager,
                    _instanceDiscoveryManager).ConfigureAwait(false);

                // Assert
                Assert.AreEqual(0, accounts.Count(), "The only WAM account has an authority which is not in the aliases of the input authority, so it is not returned."); 
            }
        }

        [TestMethod]
        public async Task GetAccounts_WamAccount_CacheAccount_SameUpn_Async()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                const string User1Upn = "user1@contoso.com";
                var wamAccountProvider = new WebAccountProvider("id", User1Upn, null);
                _webAccountProviderFactory.GetAccountProviderAsync("organizations").Returns(wamAccountProvider);
                var wamAccount = new WebAccount(wamAccountProvider, "user1@contoso.com", WebAccountState.Connected);
                _wamProxy.FindAllWebAccountsAsync(wamAccountProvider, TestConstants.ClientId).Returns(new[] { wamAccount });
                _wamProxy.TryGetAccountProperty(wamAccount, "Authority", out string _).Returns(x =>
                {
                    x[2] = TestConstants.AuthorityCommonTenant;
                    return true;
                });
                var rq = new Client.Internal.RequestContext(harness.ServiceBundle, Guid.NewGuid(), default);
                _cacheSessionManager.RequestContext.Returns(rq);

                _instanceDiscoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                    Arg.Any<AuthorityInfo>(),
                    Arg.Any<IEnumerable<string>>(), rq)
                    .Returns(CreateEntryForSingleAuthority(new Uri(TestConstants.AuthorityCommonTenant))); // user set this authority in config

                // assume there is a cache account with the same UPN 
                _cacheSessionManager.GetAccountsAsync().Returns(new[] {
                    new Account(TestConstants.HomeAccountId, User1Upn, null, null )});

                // Act
                var accounts = await _aadPlugin.GetAccountsAsync(
                    TestConstants.ClientId,
                    AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityCommonTenant, true),
                    _cacheSessionManager,
                    _instanceDiscoveryManager).ConfigureAwait(false);

                // Assert
                Assert.AreEqual(1, accounts.Count());
                Assert.AreEqual(TestConstants.HomeAccountId, accounts.Single().HomeAccountId.Identifier);
            }
        }

        [TestMethod]
        public async Task GetAccounts_WamAccount_NoCacheAccounts_WebRequest_Async()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                const string User1Upn = "user1@contoso.com";
                var wamAccountProvider = new WebAccountProvider("id", User1Upn, null);
                _webAccountProviderFactory.GetAccountProviderAsync("organizations").Returns(wamAccountProvider);
                var wamAccount = new WebAccount(wamAccountProvider, "user1@contoso.com", WebAccountState.Connected);
                _wamProxy.FindAllWebAccountsAsync(wamAccountProvider, TestConstants.ClientId).Returns(new[] { wamAccount });
                _wamProxy.TryGetAccountProperty(wamAccount, "Authority", out string _).Returns(x =>
                {
                    x[2] = TestConstants.AuthorityCommonTenant;
                    return true;
                });
                var rq = new Client.Internal.RequestContext(harness.ServiceBundle, Guid.NewGuid(), default);
                _cacheSessionManager.RequestContext.Returns(rq);

                _instanceDiscoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                    Arg.Any<AuthorityInfo>(),
                    Arg.Any<IEnumerable<string>>(), rq)
                    .Returns(CreateEntryForSingleAuthority(new Uri(TestConstants.AuthorityCommonTenant))); // user set this authority in config                

                // Setup for the silent token request we're going to do via WAM
                _webAccountProviderFactory.GetAccountProviderAsync(TestConstants.AuthorityCommonTenant).Returns(wamAccountProvider);
                IWebTokenRequestResultWrapper webTokenResponseWrapper = CreateSuccessResponse(wamAccount);

                var newlyAddedAccountToCache = new Account("Id_From_ESTS", "upn", null);
                _wamProxy.GetTokenSilentlyAsync(wamAccount, Arg.Any<WebTokenRequest>()).Returns(webTokenResponseWrapper);
                _cacheSessionManager.SaveTokenResponseAsync(Arg.Any<MsalTokenResponse>()).Returns(Task.FromResult(
                    new Tuple<MsalAccessTokenCacheItem, MsalIdTokenCacheItem, Account>(null, null, newlyAddedAccountToCache)));

                // Act
                var accounts = await _aadPlugin.GetAccountsAsync(
                    TestConstants.ClientId,
                    AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityCommonTenant, true),
                    _cacheSessionManager,
                    _instanceDiscoveryManager).ConfigureAwait(false);

                // Assert
                Assert.AreEqual(1, accounts.Count());
                Assert.AreEqual("Id_From_ESTS", accounts.Single().HomeAccountId.Identifier);
            }
        }

        [TestMethod]
        public async Task GetAccounts_WamAccount_NoCacheAccounts_FailedWebRequest_Async()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                const string User1Upn = "user1@contoso.com";
                var wamAccountProvider = new WebAccountProvider("id", User1Upn, null);
                _webAccountProviderFactory.GetAccountProviderAsync("organizations").Returns(wamAccountProvider);
                var wamAccount = new WebAccount(wamAccountProvider, "user1@contoso.com", WebAccountState.Connected);
                _wamProxy.FindAllWebAccountsAsync(wamAccountProvider, TestConstants.ClientId).Returns(new[] { wamAccount });
                _wamProxy.TryGetAccountProperty(wamAccount, "Authority", out string _).Returns(x =>
                {
                    x[2] = TestConstants.AuthorityCommonTenant;
                    return true;
                });
                var rq = new Client.Internal.RequestContext(harness.ServiceBundle, Guid.NewGuid(), default);
                _cacheSessionManager.RequestContext.Returns(rq);

                _instanceDiscoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                    Arg.Any<AuthorityInfo>(),
                    Arg.Any<IEnumerable<string>>(), rq)
                    .Returns(CreateEntryForSingleAuthority(new Uri(TestConstants.AuthorityCommonTenant))); // user set this authority in config                

                // Setup for the silent token request we're going to do via WAM
                _webAccountProviderFactory.GetAccountProviderAsync(TestConstants.AuthorityCommonTenant).Returns(wamAccountProvider);
                IWebTokenRequestResultWrapper webTokenResponseWrapper = Substitute.For<IWebTokenRequestResultWrapper>();
                webTokenResponseWrapper.ResponseStatus.Returns(WebTokenRequestStatus.UserInteractionRequired); // silent web request fails

                var newlyAddedAccountToCache = new Account("Id_From_ESTS", "upn", null);
                _wamProxy.GetTokenSilentlyAsync(wamAccount, Arg.Any<WebTokenRequest>()).Returns(webTokenResponseWrapper);
                _cacheSessionManager.SaveTokenResponseAsync(Arg.Any<MsalTokenResponse>()).Returns(Task.FromResult(
                    new Tuple<MsalAccessTokenCacheItem, MsalIdTokenCacheItem, Account>(null, null, newlyAddedAccountToCache)));

                // Act
                var accounts = await _aadPlugin.GetAccountsAsync(
                    TestConstants.ClientId,
                    AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityCommonTenant, true),
                    _cacheSessionManager,
                    _instanceDiscoveryManager).ConfigureAwait(false);

                // Assert
                Assert.AreEqual(0, accounts.Count(), "If silent web request fails, do not return WAM accoutn as the home account id cannot be trusted");                
            }
        }

        [TestMethod]
        public async Task GetAccounts_WamAccount_NoCacheAccountMatch_WebRequest_Async()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                const string User1Upn = "user1@contoso.com";
                var wamAccountProvider = new WebAccountProvider("id", User1Upn, null);
                _webAccountProviderFactory.GetAccountProviderAsync("organizations").Returns(wamAccountProvider);
                var wamAccount = new WebAccount(wamAccountProvider, "user1@contoso.com", WebAccountState.Connected);
                _wamProxy.FindAllWebAccountsAsync(wamAccountProvider, TestConstants.ClientId).Returns(new[] { wamAccount });
                _wamProxy.TryGetAccountProperty(wamAccount, "Authority", out string _).Returns(x =>
                {
                    x[2] = TestConstants.AuthorityCommonTenant;
                    return true;
                });
                var rq = new Client.Internal.RequestContext(harness.ServiceBundle, Guid.NewGuid(), default);
                _cacheSessionManager.RequestContext.Returns(rq);
                _cacheSessionManager.GetAccountsAsync().Returns(new[] {
                    new Account(TestConstants.HomeAccountId, "some_other_user", null, null )});

                _instanceDiscoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                    Arg.Any<AuthorityInfo>(),
                    Arg.Any<IEnumerable<string>>(), rq)
                    .Returns(CreateEntryForSingleAuthority(new Uri(TestConstants.AuthorityCommonTenant))); // user set this authority in config                

                // Setup for the silent token request we're going to do via WAM
                _webAccountProviderFactory.GetAccountProviderAsync(TestConstants.AuthorityCommonTenant).Returns(wamAccountProvider);
                IWebTokenRequestResultWrapper webTokenResponseWrapper = CreateSuccessResponse(wamAccount);

                var newlyAddedAccountToCache = new Account("Id_From_ESTS", "upn", null);
                _wamProxy.GetTokenSilentlyAsync(wamAccount, Arg.Any<WebTokenRequest>()).Returns(webTokenResponseWrapper);
                _cacheSessionManager.SaveTokenResponseAsync(Arg.Any<MsalTokenResponse>()).Returns(Task.FromResult(
                    new Tuple<MsalAccessTokenCacheItem, MsalIdTokenCacheItem, Account>(null, null, newlyAddedAccountToCache)));

                // Act
                var accounts = await _aadPlugin.GetAccountsAsync(
                    TestConstants.ClientId,
                    AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityCommonTenant, true),
                    _cacheSessionManager,
                    _instanceDiscoveryManager).ConfigureAwait(false);

                // Assert
                Assert.AreEqual(1, accounts.Count());
                Assert.AreEqual("Id_From_ESTS", accounts.Single().HomeAccountId.Identifier);
            }
        }

        private static IWebTokenRequestResultWrapper CreateSuccessResponse(WebAccount account)
        {
            var webTokenResponseWrapper = Substitute.For<IWebTokenRequestResultWrapper>();
            webTokenResponseWrapper.ResponseStatus.Returns(WebTokenRequestStatus.Success);
            var webTokenResponse = new WebTokenResponse("at", account);
            webTokenResponseWrapper.ResponseData.Returns(new List<WebTokenResponse>() { webTokenResponse });
            
            webTokenResponse.Properties.Add("Authority", TestConstants.AuthorityHomeTenant);
            webTokenResponse.Properties.Add("wamcompat_client_info", MockHelpers.CreateClientInfo());
            webTokenResponse.Properties.Add("wamcompat_id_token", MockHelpers.CreateIdToken("oid", "upn", "tid"));
            webTokenResponse.Properties.Add("wamcompat_scopes", "profile openid");

            return webTokenResponseWrapper;
        }

        private static InstanceDiscoveryMetadataEntry CreateEntryForSingleAuthority(Uri authority)
        {
            return new InstanceDiscoveryMetadataEntry()
            {
                Aliases = new[] { authority.Host },
                PreferredCache = authority.Host,
                PreferredNetwork = authority.Host
            };
        }
    }

   

}
