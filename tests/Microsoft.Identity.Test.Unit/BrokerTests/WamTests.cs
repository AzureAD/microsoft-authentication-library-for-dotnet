#if SUPPORTS_BROKER
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Platforms.Features.WamBroker;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Shared;

#if !NET5_WIN
using Microsoft.Identity.Client.Desktop;
#endif

namespace Microsoft.Identity.Test.Unit.BrokerTests
{
    [TestClass]
    [TestCategory("Broker")]
    public class WamTests : TestBase
    {
        private CoreUIParent _coreUIParent;
        private ICoreLogger _logger;
        private IWamPlugin _aadPlugin;
        private IWamPlugin _msaPlugin;
        private IWamProxy _wamProxy;
        private IWebAccountProviderFactory _webAccountProviderFactory;
        private IAccountPickerFactory _accountPickerFactory;
        private IMsaPassthroughHandler _msaPassthroughHandler;
        private WamBroker _wamBroker;
        private SynchronizationContext _synchronizationContext;

        private MsalTokenResponse _msalTokenResponse = TokenCacheHelper.CreateMsalTokenResponse();

        [TestInitialize]
        public void Init()
        {
            _synchronizationContext = new DedicatedThreadSynchronizationContext();

            _coreUIParent = new CoreUIParent() { SynchronizationContext = _synchronizationContext };
            ApplicationConfiguration applicationConfiguration = new ApplicationConfiguration();
            _logger = Substitute.For<ICoreLogger>();
            _aadPlugin = Substitute.For<IWamPlugin>();
            _msaPlugin = Substitute.For<IWamPlugin>();
            _wamProxy = Substitute.For<IWamProxy>();
            _webAccountProviderFactory = Substitute.For<IWebAccountProviderFactory>();
            _accountPickerFactory = Substitute.For<IAccountPickerFactory>();
            _msaPassthroughHandler = Substitute.For<IMsaPassthroughHandler>();

            _wamBroker = new WamBroker(
                _coreUIParent,
                applicationConfiguration,
                _logger,
                _aadPlugin,
                _msaPlugin,
                _wamProxy,
                _webAccountProviderFactory,
                _accountPickerFactory,
                _msaPassthroughHandler);
        }

        [TestMethod]
        public void WamOnWin10()
        {
            if (!DesktopOsHelper.IsWin10OrServerEquivalent())
            {
                Assert.Inconclusive("Needs to run on win10 or equivalent");
            }
            var pcaBuilder = PublicClientApplicationBuilder
               .Create("d3adb33f-c0de-ed0c-c0de-deadb33fc0d3")
               .WithAuthority(TestConstants.AuthorityTenant);

#if !NET5_WIN
            pcaBuilder = pcaBuilder.WithWindowsBroker();
#endif

            Assert.IsTrue(pcaBuilder.IsBrokerAvailable());

        }

        [TestMethod]
        public void NoWamOnADFS()
        {
            var pcaBuilder = PublicClientApplicationBuilder
               .Create("d3adb33f-c0de-ed0c-c0de-deadb33fc0d3")
               .WithAdfsAuthority(TestConstants.ADFSAuthority);
#if !NET5_WIN
            pcaBuilder = pcaBuilder.WithWindowsBroker();
#endif

            Assert.IsFalse(pcaBuilder.IsBrokerAvailable());

        }

        [TestMethod]
        public void HandleInstallUrl_Throws()
        {
            AssertException.Throws<NotImplementedException>(() => _wamBroker.HandleInstallUrl("http://app"));
        }

        [TestMethod]
        [Description("For AcquireTokenSilent, plugin selection occurs based on the authority only, as this is always tenanted.")]
        public async Task Ats_PluginSelection_Async()
        {
            // tenanted authority => AAD plugin
            await RunPluginSelectionTestAsync(
                TestConstants.AadAuthorityWithTestTenantId,
                expectMsaPlugin: false).ConfigureAwait(false);

            // consumers => MSA plugin
            await RunPluginSelectionTestAsync(
                TestConstants.AuthorityConsumerTidTenant,
                expectMsaPlugin: true).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task ATS_AccountWithWamId_Async()
        {
            // Arrange
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                _webAccountProviderFactory.ClearReceivedCalls();

                var wamAccountProvider = new WebAccountProvider("id", "user@contoso.com", null);
                var extraQP = new Dictionary<string, string>() { { "extraQp1", "extraVal1" }, { "instance_aware", "true" } };
                var requestParams = harness.CreateAuthenticationRequestParameters(
                    "https://login.microsoftonline.com/organizations",
                    extraQueryParameters: extraQP,
                    validateAuthority: true); // AAD

                requestParams.Account = new Account(
                    $"{TestConstants.Uid}.{TestConstants.Utid}",
                    TestConstants.DisplayableId,
                    null,
                    new Dictionary<string, string>() { { TestConstants.ClientId, "wam_id_1" } }); // account has wam_id!

                var webAccount = new WebAccount(wamAccountProvider, "user@contoso.com", WebAccountState.Connected);
                var webTokenRequest = new WebTokenRequest(wamAccountProvider);
                var webTokenResponseWrapper = Substitute.For<IWebTokenRequestResultWrapper>();
                webTokenResponseWrapper.ResponseStatus.Returns(WebTokenRequestStatus.Success);
                var webTokenResponse = new WebTokenResponse();
                webTokenResponseWrapper.ResponseData.Returns(new List<WebTokenResponse>() { webTokenResponse });

                _webAccountProviderFactory.GetAccountProviderAsync(null).ReturnsForAnyArgs(Task.FromResult(wamAccountProvider));
                _wamProxy.FindAccountAsync(Arg.Any<WebAccountProvider>(), "wam_id_1").Returns(Task.FromResult(webAccount));
                _aadPlugin.CreateWebTokenRequestAsync(
                    wamAccountProvider,
                    requestParams,
                    isForceLoginPrompt: false,
                    isAccountInWam: true,
                    isInteractive: false)
                    .Returns(Task.FromResult(webTokenRequest));

                var atsParams = new AcquireTokenSilentParameters();

                _wamProxy.GetTokenSilentlyAsync(webAccount, webTokenRequest).
                    Returns(Task.FromResult(webTokenResponseWrapper));

                _aadPlugin.ParseSuccessfullWamResponse(webTokenResponse, out _).Returns(_msalTokenResponse);

                // Act
                var result = await _wamBroker.AcquireTokenSilentAsync(requestParams, atsParams).ConfigureAwait(false);

                // Assert 
                Assert.AreSame(_msalTokenResponse, result);
                Assert.AreEqual("yes", webTokenRequest.Properties["validateAuthority"]);
                Assert.AreEqual("extraVal1", webTokenRequest.Properties["extraQp1"]);
                AssertTelemetryHeadersInRequest(webTokenRequest.Properties);

                // Although at the time of writing, MSAL does not support instance aware ...
                // WAM does support it but the param is different - discovery=home              
                Assert.AreEqual("home", webTokenRequest.Properties["discover"]);
                Assert.AreEqual("https://login.microsoftonline.com/organizations/", webTokenRequest.Properties["authority"]);
            }
        }

#region CreateMsalTokenResponse
        [TestMethod]
        public async Task WAMBroker_CreateMsalTokenResponse_AccountSwitch_Async()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var (requestParams, webTokenResponseWrapper) = SetupSilentCall(harness);
                webTokenResponseWrapper.ResponseStatus.Returns(WebTokenRequestStatus.AccountSwitch);
                var webTokenResponse = new WebTokenResponse();
                webTokenResponseWrapper.ResponseData.Returns(new List<WebTokenResponse>() { webTokenResponse });
                _aadPlugin.ParseSuccessfullWamResponse(Arg.Any<WebTokenResponse>(), out _).Returns(_msalTokenResponse);

                // Act
                var result = await _wamBroker.AcquireTokenSilentAsync(requestParams, new AcquireTokenSilentParameters())
                    .ConfigureAwait(false);

                // Assert 
                Assert.AreSame(_msalTokenResponse, result);
            }
        }

        [TestMethod]
        public async Task WAMBroker_CreateMsalTokenResponse_UserInteractionRequired_Async()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var (requestParams, webTokenResponseWrapper) = SetupSilentCall(harness);
                webTokenResponseWrapper.ResponseStatus.Returns(WebTokenRequestStatus.UserInteractionRequired);
                webTokenResponseWrapper.ResponseError.Returns(new WebProviderError(42, "more_detailed_error_message"));
                _aadPlugin.MapTokenRequestError(WebTokenRequestStatus.UserInteractionRequired, 42, false)
                    .Returns(Tuple.Create("ui_is_really_needed", "", false));

                // Act
                var ex = await AssertException.TaskThrowsAsync<MsalUiRequiredException>(
                    () => _wamBroker.AcquireTokenSilentAsync(requestParams, new AcquireTokenSilentParameters())).ConfigureAwait(false);

                // Assert 
                Assert.AreEqual("ui_is_really_needed", ex.ErrorCode);
                Assert.IsTrue(ex.Message.Contains("more_detailed_error_message"));
                Assert.IsTrue(ex.Message.Contains("Internal Error Code: 42"));
                Assert.IsFalse(ex.IsRetryable);
            }
        }

        [TestMethod]
        public async Task WAMBroker_CreateMsalTokenResponse_ProviderError_Async()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var (requestParams, webTokenResponseWrapper) = SetupSilentCall(harness);
                webTokenResponseWrapper.ResponseStatus.Returns(WebTokenRequestStatus.ProviderError);
                webTokenResponseWrapper.ResponseError.Returns(new WebProviderError(42, "more_detailed_error_message"));
                _aadPlugin.MapTokenRequestError(WebTokenRequestStatus.ProviderError, 42, false)
                    .Returns(Tuple.Create("ui_is_really_needed", "", true));

                // Act
                var ex = await AssertException.TaskThrowsAsync<MsalServiceException>(
                    () => _wamBroker.AcquireTokenSilentAsync(requestParams, new AcquireTokenSilentParameters())).ConfigureAwait(false);

                // Assert 
                Assert.AreEqual("ui_is_really_needed", ex.ErrorCode);
                Assert.IsTrue(ex.Message.Contains("more_detailed_error_message"));
                Assert.IsTrue(ex.Message.Contains("Internal Error Code: 42"));
                Assert.IsTrue(ex.Message.Contains("Is Retryable: True"));
                Assert.IsTrue(ex.IsRetryable);
            }
        }

        [TestMethod]
        public async Task WAMBroker_CreateMsalTokenResponse_UserCancelled_Async()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var (requestParams, webTokenResponseWrapper) = SetupSilentCall(harness);
                webTokenResponseWrapper.ResponseStatus.Returns(WebTokenRequestStatus.UserCancel);

                // Act
                var ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                    () => _wamBroker.AcquireTokenSilentAsync(requestParams, new AcquireTokenSilentParameters())).ConfigureAwait(false);

                // Assert 
                Assert.AreEqual(MsalError.AuthenticationCanceledError, ex.ErrorCode);
                Assert.AreEqual(MsalErrorMessage.AuthenticationCanceled, ex.Message);
                Assert.IsFalse(ex.IsRetryable);
            }
        }

        private (Client.Internal.Requests.AuthenticationRequestParameters requestParams, IWebTokenRequestResultWrapper webTokenResponseWrapper) SetupSilentCall(MockHttpAndServiceBundle harness)
        {
            var wamAccountProvider = new WebAccountProvider("id", "user@contoso.com", null);
            var requestParams = harness.CreateAuthenticationRequestParameters(
                TestConstants.AuthorityHomeTenant);
            requestParams.Account = new Account(
                $"{TestConstants.Uid}.{TestConstants.Utid}",
                TestConstants.DisplayableId,
                null,
                new Dictionary<string, string>() { { TestConstants.ClientId, "wam_id_1" } }); // account has wam_id!

            var webAccount = new WebAccount(wamAccountProvider, "user@contoso.com", WebAccountState.Connected);
            var webTokenRequest = new WebTokenRequest(wamAccountProvider);
            IWebTokenRequestResultWrapper webTokenResponseWrapper = Substitute.For<IWebTokenRequestResultWrapper>();

            _webAccountProviderFactory.GetAccountProviderAsync(null).ReturnsForAnyArgs(Task.FromResult(wamAccountProvider));
            _wamProxy.FindAccountAsync(Arg.Any<WebAccountProvider>(), "wam_id_1").Returns(Task.FromResult(webAccount));
            _aadPlugin.CreateWebTokenRequestAsync(
                wamAccountProvider,
                requestParams,
                isForceLoginPrompt: false,
                isAccountInWam: true,
                isInteractive: false)
                .Returns(Task.FromResult(webTokenRequest));

            _wamProxy.GetTokenSilentlyAsync(webAccount, webTokenRequest).
                Returns(Task.FromResult(webTokenResponseWrapper));

            return (requestParams, webTokenResponseWrapper);
        }

#endregion

        [TestMethod]
        public async Task ATS_AccountMatchingInWAM_MatchingHomeAccId_Async()
        {
            string homeAccId = $"{TestConstants.Uid}.{TestConstants.Utid}";
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var wamAccountProvider = new WebAccountProvider("id", "user@contoso.com", null);
                var requestParams = harness.CreateAuthenticationRequestParameters(TestConstants.AuthorityConsumerTidTenant); // MSA
                var webAccount = new WebAccount(wamAccountProvider, "user@contoso.com", WebAccountState.Connected);
                IReadOnlyList<WebAccount> webAccounts = new List<WebAccount>() { webAccount };
                var webTokenRequest = new WebTokenRequest(wamAccountProvider);
                var webTokenResponseWrapper = Substitute.For<IWebTokenRequestResultWrapper>();
                webTokenResponseWrapper.ResponseStatus.Returns(WebTokenRequestStatus.Success);
                var webTokenResponse = new WebTokenResponse();
                webTokenResponseWrapper.ResponseData.Returns(new List<WebTokenResponse>() { webTokenResponse });

                _wamProxy.FindAllWebAccountsAsync(wamAccountProvider, TestConstants.ClientId).Returns(Task.FromResult(webAccounts));

                // WAM can give MSAL the home account ID of a Wam account, which MSAL matches to a WAM account
                _msaPlugin.GetHomeAccountIdOrNull(webAccount).Returns(homeAccId);

                _msaPlugin.CreateWebTokenRequestAsync(
                    wamAccountProvider,
                    requestParams,
                    isForceLoginPrompt: false,
                    isAccountInWam: true,
                    isInteractive: false)
                    .Returns(Task.FromResult(webTokenRequest));

                requestParams.Account = new Account(
                    homeAccId, // matching in on home acc id
                    "doesnt_matter@contoso.com", // matching is not on UPN
                    null); // account does not have wam_id, might be coming directly from WAM

                var atsParams = new AcquireTokenSilentParameters();
                _webAccountProviderFactory.GetAccountProviderAsync(null).ReturnsForAnyArgs(Task.FromResult(wamAccountProvider));

                _wamProxy.GetTokenSilentlyAsync(webAccount, webTokenRequest).
                    Returns(Task.FromResult(webTokenResponseWrapper));
                _msaPlugin.ParseSuccessfullWamResponse(webTokenResponse, out _).Returns(_msalTokenResponse);

                // Act
                var result = await _wamBroker.AcquireTokenSilentAsync(requestParams, atsParams).ConfigureAwait(false);

                // Assert 
                Assert.AreSame(_msalTokenResponse, result);
                AssertTelemetryHeadersInRequest(webTokenRequest.Properties);
            }
        }

     
        [TestMethod]
        public async Task ATS_NoAccountMatching_ThrowsUiRequiredException_Async()
        {
            string homeAccId = $"{TestConstants.Uid}.{TestConstants.Utid}";

            // Arrange
            using (var harness = CreateTestHarness())
            {
                _webAccountProviderFactory.ClearReceivedCalls();

                var wamAccountProvider = new WebAccountProvider("id", "user@contoso.com", null);
                var requestParams = harness.CreateAuthenticationRequestParameters(TestConstants.AuthorityHomeTenant); // AAD
                var webAccount = new WebAccount(wamAccountProvider, "user@contoso.com", WebAccountState.Connected);
                IReadOnlyList<WebAccount> webAccounts = new List<WebAccount>() { webAccount };
                var webTokenRequest = new WebTokenRequest(wamAccountProvider);

                _wamProxy.FindAllWebAccountsAsync(wamAccountProvider, TestConstants.ClientId).Returns(Task.FromResult(webAccounts));

                _aadPlugin.GetHomeAccountIdOrNull(webAccount).Returns("other_home_acc_id");

                requestParams.Account = new Account(
                    homeAccId, // matching in on home acc id
                    "doesnt_matter@contoso.com", // matching is not on UPN
                    null); // account does not have wam_id, might be coming directly from WAM

                var atsParams = new AcquireTokenSilentParameters();
                _webAccountProviderFactory.GetAccountProviderAsync(null).ReturnsForAnyArgs(Task.FromResult(wamAccountProvider));

                // Act / Assert
                var ex = await AssertException.TaskThrowsAsync<MsalUiRequiredException>(
                    () => _wamBroker.AcquireTokenSilentAsync(requestParams, atsParams)).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task ATS_DefaultAccount_Async()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {

                var wamAccountProvider = new WebAccountProvider("id", "user@contoso.com", null);

                _webAccountProviderFactory.GetDefaultProviderAsync().ReturnsForAnyArgs(Task.FromResult(wamAccountProvider));
                
                var requestParams = harness.CreateAuthenticationRequestParameters(TestConstants.AuthorityHomeTenant); // AAD authority, no account
                var webTokenRequest = new WebTokenRequest(wamAccountProvider);
                var atsParams = new AcquireTokenSilentParameters();
                _webAccountProviderFactory.GetAccountProviderAsync(null).ReturnsForAnyArgs(Task.FromResult(wamAccountProvider));
                var webTokenResponseWrapper = Substitute.For<IWebTokenRequestResultWrapper>();
                webTokenResponseWrapper.ResponseStatus.Returns(WebTokenRequestStatus.Success);
                var webTokenResponse = new WebTokenResponse();
                webTokenResponseWrapper.ResponseData.Returns(new List<WebTokenResponse>() { webTokenResponse });

                _aadPlugin.CreateWebTokenRequestAsync(
                   wamAccountProvider,
                   requestParams,
                   isForceLoginPrompt: false,
                   isAccountInWam: false,
                   isInteractive: false)
                   .Returns(Task.FromResult(webTokenRequest));

                _wamProxy.GetTokenSilentlyForDefaultAccountAsync(webTokenRequest).
                    Returns(Task.FromResult(webTokenResponseWrapper));

                _aadPlugin.ParseSuccessfullWamResponse(webTokenResponse, out _).Returns(_msalTokenResponse);

                // Act
                var result = await _wamBroker.AcquireTokenSilentDefaultUserAsync(requestParams, atsParams).ConfigureAwait(false);

                // Assert 
                Assert.AreSame(_msalTokenResponse, result);
                await _aadPlugin.Received(1).CreateWebTokenRequestAsync(wamAccountProvider,
                   requestParams,
                   isForceLoginPrompt: false,
                   isAccountInWam: false,
                   isInteractive: false).ConfigureAwait(false);
                AssertTelemetryHeadersInRequest(webTokenRequest.Properties);
            }
        }

        [TestMethod]
        public async Task ATS_DefaultAccount_Passthrough_Async()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var msaAccountProvider = new WebAccountProvider("id", "user@outlook.com", null);
                var aadAccountProvider = new WebAccountProvider("id", "organizations", null);

                var requestParams = harness.CreateAuthenticationRequestParameters(TestConstants.AuthorityOrganizationsTenant);
                requestParams.AppConfig.WindowsBrokerOptions = new WindowsBrokerOptions()
                {
                    MsaPassthrough = true
                };
                _wamBroker = new WamBroker(
                     _coreUIParent,
                      requestParams.AppConfig,
                     _logger,
                     _aadPlugin,
                     _msaPlugin,
                     _wamProxy,
                     _webAccountProviderFactory,
                     _accountPickerFactory,
                     _msaPassthroughHandler);

                _webAccountProviderFactory.GetDefaultProviderAsync().ReturnsForAnyArgs(Task.FromResult(msaAccountProvider));
                _webAccountProviderFactory.IsConsumerProvider(msaAccountProvider).Returns(true);
                _msaPassthroughHandler.TryFetchTransferTokenSilentDefaultAccountAsync(requestParams, msaAccountProvider).Returns("transfer_token");

                _webAccountProviderFactory.GetAccountProviderAsync("organizations").Returns(aadAccountProvider);

                var webTokenRequest = new WebTokenRequest(aadAccountProvider);
                var atsParams = new AcquireTokenSilentParameters();
                var webTokenResponseWrapper = Substitute.For<IWebTokenRequestResultWrapper>();
                webTokenResponseWrapper.ResponseStatus.Returns(WebTokenRequestStatus.Success);
                var webTokenResponse = new WebTokenResponse();
                webTokenResponseWrapper.ResponseData.Returns(new List<WebTokenResponse>() { webTokenResponse });

                _aadPlugin.CreateWebTokenRequestAsync(
                   aadAccountProvider,
                   requestParams,
                   isForceLoginPrompt: false,
                   isAccountInWam: true,
                   isInteractive: false)
                   .Returns(Task.FromResult(webTokenRequest));

                _wamProxy.RequestTokenForWindowAsync(Arg.Any<IntPtr>(), webTokenRequest).Returns(webTokenResponseWrapper);
                _aadPlugin.ParseSuccessfullWamResponse(webTokenResponse, out _).Returns(_msalTokenResponse);

                // Act
                var result = await _wamBroker.AcquireTokenSilentDefaultUserAsync(requestParams, atsParams).ConfigureAwait(false);

                // Assert 
                Assert.AreSame(_msalTokenResponse, result);
                AssertTelemetryHeadersInRequest(webTokenRequest.Properties);
                _msaPassthroughHandler.Received(1).AddTransferTokenToRequest(webTokenRequest, "transfer_token");
            }
        }

        [TestMethod]
        public async Task ATI_WithoutPicker_AccountMatch_Async()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var requestParams = harness.CreateAuthenticationRequestParameters(TestConstants.AuthorityHomeTenant); // AAD
                requestParams.Account = new Account(
                   $"{TestConstants.Uid}.{TestConstants.Utid}",
                   TestConstants.DisplayableId,
                   null,
                   new Dictionary<string, string>() { { TestConstants.ClientId, "wam_id_1" } }); // account has wam_id!

                var wamAccountProvider = new WebAccountProvider("id", "user@contoso.com", null);
                var webAccount = new WebAccount(wamAccountProvider, "user@contoso.com", WebAccountState.Connected);
                var webTokenRequest = new WebTokenRequest(wamAccountProvider);

                // will use the AAD provider because the authority is tenanted (i.e. AAD only)
                _webAccountProviderFactory
                    .GetAccountProviderAsync(TestConstants.AuthorityHomeTenant)
                    .ReturnsForAnyArgs(Task.FromResult(wamAccountProvider));

                // account matching based on wam account ID (logic for matching based on home_account_id is checked in ATS tests)
                _wamProxy.FindAccountAsync(Arg.Any<WebAccountProvider>(), "wam_id_1")
                    .Returns(Task.FromResult(webAccount));

                // WAM can give MSAL the home account ID of a Wam account, which MSAL matches to a WAM account
                _aadPlugin.CreateWebTokenRequestAsync(
                    wamAccountProvider,
                    requestParams,
                    isForceLoginPrompt: false,
                    isAccountInWam: true,
                    isInteractive: true)
                    .Returns(Task.FromResult(webTokenRequest));

                var webTokenResponseWrapper = Substitute.For<IWebTokenRequestResultWrapper>();
                webTokenResponseWrapper.ResponseStatus.Returns(WebTokenRequestStatus.Success);
                var webTokenResponse = new WebTokenResponse();
                webTokenResponseWrapper.ResponseData.Returns(new List<WebTokenResponse>() { webTokenResponse });

                _wamProxy.RequestTokenForWindowAsync(Arg.Any<IntPtr>(), webTokenRequest, webAccount).
                    Returns(Task.FromResult(webTokenResponseWrapper));
                _aadPlugin.ParseSuccessfullWamResponse(webTokenResponse, out _).Returns(_msalTokenResponse);

                // Act
                var result = await _wamBroker.AcquireTokenInteractiveAsync(
                    requestParams,
                    new AcquireTokenInteractiveParameters()).ConfigureAwait(false);

                // Assert 
                Assert.AreSame(_msalTokenResponse, result);
                AssertTelemetryHeadersInRequest(webTokenRequest.Properties);
            }
        }

        [TestMethod]
        public async Task ATI_WithoutPicker_NoAccountMatch_Async()
        {
            _accountPickerFactory.ClearReceivedCalls();

            // Arrange 
            using (var harness = CreateTestHarness())
            {
                var requestParams = harness.CreateAuthenticationRequestParameters(TestConstants.AuthorityOrganizationsTenant); 
                var wamAccountProvider = new WebAccountProvider("id", "user@contoso.com", null);
               
                var webTokenRequest = new WebTokenRequest(wamAccountProvider);

                // will use the AAD provider because the authority is organizations (i.e. AAD only)
                _webAccountProviderFactory
                    .GetAccountProviderAsync("organizations")
                    .ReturnsForAnyArgs(Task.FromResult(wamAccountProvider));
                
                _aadPlugin.CreateWebTokenRequestAsync(
                    wamAccountProvider,
                    requestParams,
                    isForceLoginPrompt: true,
                    isInteractive: true,
                    isAccountInWam: false)
                    .Returns(Task.FromResult(webTokenRequest));

                var webTokenResponseWrapper = Substitute.For<IWebTokenRequestResultWrapper>();
                webTokenResponseWrapper.ResponseStatus.Returns(WebTokenRequestStatus.Success);
                var webTokenResponse = new WebTokenResponse();
                webTokenResponseWrapper.ResponseData.Returns(new List<WebTokenResponse>() { webTokenResponse });

                _wamProxy.RequestTokenForWindowAsync(Arg.Any<IntPtr>(), webTokenRequest).
                    Returns(Task.FromResult(webTokenResponseWrapper));
                _aadPlugin.ParseSuccessfullWamResponse(webTokenResponse, out _).Returns(_msalTokenResponse);

                // Act
                var result = await _wamBroker.AcquireTokenInteractiveAsync(
                    requestParams,
                    new AcquireTokenInteractiveParameters()).ConfigureAwait(false);

                // Assert                 
                _accountPickerFactory.DidNotReceiveWithAnyArgs().Create(IntPtr.Zero, null, null, null, false, null);// Account Picker Is NOT used
                Assert.AreEqual("select_account", webTokenRequest.Properties["prompt"]);
                Assert.AreSame(_msalTokenResponse, result);
                AssertTelemetryHeadersInRequest(webTokenRequest.Properties);
            }
        }

        [TestMethod]
        public async Task ATI_WithoutPicker_AccountMatch_Organizations_Async()
        {
            string homeAccId = $"{TestConstants.Uid}.{TestConstants.Utid}";
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var requestParams = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityOrganizationsTenant); 
                requestParams.Account = new Account(
                   $"{TestConstants.Uid}.{TestConstants.Utid}",
                   TestConstants.DisplayableId,
                   null,
                   new Dictionary<string, string>() { { TestConstants.ClientId, "wam_id_1" } }); // account has wam_id!

                WebAccountProvider wamAccountProvider = await WebAuthenticationCoreManager.FindAccountProviderAsync(
                    "https://login.microsoft.com", "organizations");
                var webAccount = new WebAccount(wamAccountProvider, "user@contoso.com", WebAccountState.Connected);
                var webTokenRequest = new WebTokenRequest(wamAccountProvider);
                _webAccountProviderFactory.IsOrganizationsProvider(wamAccountProvider).Returns(true);
                // will use the AAD provider because the authority is tenanted (i.e. AAD only)
                _webAccountProviderFactory
                    .GetAccountProviderAsync(TestConstants.AuthorityHomeTenant)
                    .ReturnsForAnyArgs(Task.FromResult(wamAccountProvider));

                // account matching based on wam account ID (logic for matching based on home_account_id is checked in ATS tests)
                _wamProxy.FindAccountAsync(Arg.Any<WebAccountProvider>(), "wam_id_1")
                    .Returns(Task.FromResult(webAccount));

                // WAM can give MSAL the home account ID of a Wam account, which MSAL matches to a WAM account
                _aadPlugin.CreateWebTokenRequestAsync(
                    wamAccountProvider,
                    requestParams,
                    isForceLoginPrompt: false,
                    isAccountInWam: true,
                    isInteractive: true)
                    .Returns(Task.FromResult(webTokenRequest));

                var webTokenResponseWrapper = Substitute.For<IWebTokenRequestResultWrapper>();
                webTokenResponseWrapper.ResponseStatus.Returns(WebTokenRequestStatus.Success);
                var webTokenResponse = new WebTokenResponse();
                webTokenResponseWrapper.ResponseData.Returns(new List<WebTokenResponse>() { webTokenResponse });

                _wamProxy.RequestTokenForWindowAsync(Arg.Any<IntPtr>(), webTokenRequest, webAccount).
                    Returns(Task.FromResult(webTokenResponseWrapper));
                _aadPlugin.ParseSuccessfullWamResponse(webTokenResponse, out _).Returns(_msalTokenResponse);

                // Act
                var result = await _wamBroker.AcquireTokenInteractiveAsync(
                    requestParams,
                    new AcquireTokenInteractiveParameters()).ConfigureAwait(false);

                // Assert 
                Assert.AreSame(_msalTokenResponse, result);
                Assert.AreEqual(
                    "https://login.microsoftonline.com/common/",
                    webTokenRequest.Properties["authority"]);
                AssertTelemetryHeadersInRequest(webTokenRequest.Properties);
            }
        }

        [TestMethod]
        public async Task ATI_WithDefaultUser_Async()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var requestParams = harness.CreateAuthenticationRequestParameters(TestConstants.AuthorityHomeTenant); // AAD
                requestParams.Account = PublicClientApplication.OperatingSystemAccount;

                var wamAccountProvider = new WebAccountProvider("id", "user@contoso.com", null);
                var webTokenRequest = new WebTokenRequest(wamAccountProvider);

                // will use the AAD provider because the authority is tenanted (i.e. AAD only)
                _webAccountProviderFactory
                    .GetAccountProviderAsync(TestConstants.AuthorityHomeTenant)
                    .ReturnsForAnyArgs(Task.FromResult(wamAccountProvider));

                _aadPlugin.CreateWebTokenRequestAsync(
                    wamAccountProvider,
                    requestParams,
                    isForceLoginPrompt: false,
                    isAccountInWam: true,
                    isInteractive: true)
                    .Returns(Task.FromResult(webTokenRequest));

                var webTokenResponseWrapper = Substitute.For<IWebTokenRequestResultWrapper>();
                webTokenResponseWrapper.ResponseStatus.Returns(WebTokenRequestStatus.Success);
                var webTokenResponse = new WebTokenResponse();
                webTokenResponseWrapper.ResponseData.Returns(new List<WebTokenResponse>() { webTokenResponse });

                _wamProxy.RequestTokenForWindowAsync(Arg.Any<IntPtr>(), webTokenRequest).
                    Returns(Task.FromResult(webTokenResponseWrapper));
                _aadPlugin.ParseSuccessfullWamResponse(webTokenResponse, out _).Returns(_msalTokenResponse);

                // Act
                var result = await _wamBroker.AcquireTokenInteractiveAsync(
                    requestParams,
                    new AcquireTokenInteractiveParameters()).ConfigureAwait(false);

                // Assert 
                Assert.AreSame(_msalTokenResponse, result);
                AssertTelemetryHeadersInRequest(webTokenRequest.Properties);
            }
        }

        [TestMethod]
        public async Task ATI_WithDefaultUser_OrganizationsWorkaround_Async()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var requestParams = harness.CreateAuthenticationRequestParameters(TestConstants.AuthorityOrganizationsTenant);

                var wamAccountProvider = new WebAccountProvider("id", "user@contoso.com", null);
                var webTokenRequest = new WebTokenRequest(wamAccountProvider);

                // will use the AAD provider because the authority is organizaitons
                _webAccountProviderFactory
                    .GetAccountProviderAsync(TestConstants.AuthorityHomeTenant)
                    .ReturnsForAnyArgs(Task.FromResult(wamAccountProvider));

                _aadPlugin.CreateWebTokenRequestAsync(
                    wamAccountProvider,
                    requestParams,
                    isForceLoginPrompt: false,
                    isAccountInWam: true,
                    isInteractive: true)
                    .Returns(Task.FromResult(webTokenRequest));

                var webTokenResponseWrapper = Substitute.For<IWebTokenRequestResultWrapper>();
                webTokenResponseWrapper.ResponseStatus.Returns(WebTokenRequestStatus.Success);
                var webTokenResponse = new WebTokenResponse();
                webTokenResponseWrapper.ResponseData.Returns(new List<WebTokenResponse>() { webTokenResponse });

                _wamProxy.RequestTokenForWindowAsync(Arg.Any<IntPtr>(), webTokenRequest).
                    Returns(Task.FromResult(webTokenResponseWrapper));
                _aadPlugin.ParseSuccessfullWamResponse(webTokenResponse, out _).Returns(_msalTokenResponse);

                // Act
                requestParams.Account = PublicClientApplication.OperatingSystemAccount;
                var result = await _wamBroker.AcquireTokenInteractiveAsync(
                    requestParams,
                    new AcquireTokenInteractiveParameters()).ConfigureAwait(false);

                // Assert 
                Assert.AreEqual(
                    "https://login.microsoftonline.com/common/",
                    webTokenRequest.Properties["authority"], 
                    "The workaround rewrites the tenant from organizations to common");
                Assert.AreSame(_msalTokenResponse, result);
                AssertTelemetryHeadersInRequest(webTokenRequest.Properties);
            }
        }

        [TestMethod]
        public async Task ATI_WithPicker_Async()
        {
            string homeAccId = $"{TestConstants.Uid}.{TestConstants.Utid}";
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var requestParams = harness.CreateAuthenticationRequestParameters(TestConstants.AuthorityCommonTenant);
                requestParams.Account = new Account(
                   $"{TestConstants.Uid}.{TestConstants.Utid}",
                   TestConstants.DisplayableId,
                   null,
                   new Dictionary<string, string>() { { TestConstants.ClientId, "wam_id_1" } }); // account has wam_id!

                var accountPicker = Substitute.For<IAccountPicker>();

                _accountPickerFactory.Create(Arg.Any<IntPtr>(), null, null, null, false, null).ReturnsForAnyArgs(accountPicker);

                var wamAccountProvider = new WebAccountProvider("id", "user@contoso.com", null);
                accountPicker.DetermineAccountInteractivelyAsync().Returns(Task.FromResult(wamAccountProvider));

                var webTokenRequest = new WebTokenRequest(wamAccountProvider);
                _aadPlugin.CreateWebTokenRequestAsync(
                    wamAccountProvider,
                    requestParams,
                    isForceLoginPrompt: false,
                     isInteractive: true,
                     isAccountInWam: false)
                    .Returns(Task.FromResult(webTokenRequest));

                var webTokenResponseWrapper = Substitute.For<IWebTokenRequestResultWrapper>();
                webTokenResponseWrapper.ResponseStatus.Returns(WebTokenRequestStatus.Success);
                var webTokenResponse = new WebTokenResponse();
                webTokenResponseWrapper.ResponseData.Returns(new List<WebTokenResponse>() { webTokenResponse });

                _wamProxy.RequestTokenForWindowAsync(Arg.Any<IntPtr>(), webTokenRequest).
                    Returns(Task.FromResult(webTokenResponseWrapper));
                _aadPlugin.ParseSuccessfullWamResponse(webTokenResponse, out _).Returns(_msalTokenResponse);

                var atiParams = new AcquireTokenInteractiveParameters();

                // Act
                var result = await _wamBroker.AcquireTokenInteractiveAsync(requestParams, atiParams)
                    .ConfigureAwait(false);

                // Assert 
                Assert.AreSame(_msalTokenResponse, result);
                AssertTelemetryHeadersInRequest(webTokenRequest.Properties);
            }
        }

    

        [TestMethod]
        public async Task ATI_WithAadPlugin_LoginHint_Async()
        {
            string homeAccId = $"{TestConstants.Uid}.{TestConstants.Utid}";
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var requestParams = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityOrganizationsTenant);
                requestParams.LoginHint = "user@contoso.com";

                var wamAccountProvider = new WebAccountProvider("id", "user@contoso.com", null);
                _webAccountProviderFactory
                   .GetAccountProviderAsync(TestConstants.AuthorityOrganizationsTenant)
                   .ReturnsForAnyArgs(Task.FromResult(wamAccountProvider));

                // these will not match the login hint
                var webAccount1 = new WebAccount(wamAccountProvider, "user1@contoso.com", WebAccountState.Connected);
                var webAccount2 = new WebAccount(wamAccountProvider, "user2@contoso.com", WebAccountState.Connected);
                var webAccount3 = new WebAccount(wamAccountProvider, "user3@contoso.com", WebAccountState.Connected);

                IReadOnlyList<WebAccount> webAccounts = new List<WebAccount>() { webAccount1, webAccount2, webAccount3 };
                _wamProxy.FindAllWebAccountsAsync(wamAccountProvider, TestConstants.ClientId).Returns(Task.FromResult(webAccounts));

                var webTokenRequest = new WebTokenRequest(wamAccountProvider);
                _aadPlugin.CreateWebTokenRequestAsync(
                    wamAccountProvider,
                    requestParams,
                    isForceLoginPrompt: true, // force login!
                     isInteractive: true,
                     isAccountInWam: false)
                    .Returns(Task.FromResult(webTokenRequest));

                var webTokenResponseWrapper = Substitute.For<IWebTokenRequestResultWrapper>();
                webTokenResponseWrapper.ResponseStatus.Returns(WebTokenRequestStatus.Success);
                var webTokenResponse = new WebTokenResponse();
                webTokenResponseWrapper.ResponseData.Returns(new List<WebTokenResponse>() { webTokenResponse });

                _wamProxy.RequestTokenForWindowAsync(Arg.Any<IntPtr>(), webTokenRequest).
                    Returns(Task.FromResult(webTokenResponseWrapper));
                _aadPlugin.ParseSuccessfullWamResponse(webTokenResponse, out _).Returns(_msalTokenResponse);

                var atiParams = new AcquireTokenInteractiveParameters();

                // Act
                var result = await _wamBroker.AcquireTokenInteractiveAsync(requestParams, atiParams)
                    .ConfigureAwait(false);

                // Assert 
                Assert.AreSame(_msalTokenResponse, result);
                AssertTelemetryHeadersInRequest(webTokenRequest.Properties);
            }
        }

        [TestMethod] // regrssion for https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/2903
        public async Task ATI_WithAadPlugin_MultipleExistingAccounts_MatchByLoginHint_Async()
        {
            string homeAccId = $"{TestConstants.Uid}.{TestConstants.Utid}";

            // Arrange
            using (var harness = CreateTestHarness())
            {
                var requestParams = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityOrganizationsTenant);
                requestParams.LoginHint = "user2@contoso.com";

                var wamAccountProvider = new WebAccountProvider("id", "user", null);
                _webAccountProviderFactory
                   .GetAccountProviderAsync(TestConstants.AuthorityOrganizationsTenant)
                   .ReturnsForAnyArgs(Task.FromResult(wamAccountProvider));

                var webAccount1 = new WebAccount(wamAccountProvider, "user1@contoso.com", WebAccountState.Connected);
                var webAccount2 = new WebAccount(wamAccountProvider, "user2@contoso.com", WebAccountState.Connected);
                var webAccount3 = new WebAccount(wamAccountProvider, "user3@contoso.com", WebAccountState.Connected);

                IReadOnlyList<WebAccount> webAccounts = new List<WebAccount>() { webAccount1, webAccount2, webAccount3 };
                _wamProxy.FindAllWebAccountsAsync(wamAccountProvider, TestConstants.ClientId).Returns(Task.FromResult(webAccounts));

                var webTokenRequest = new WebTokenRequest(wamAccountProvider);
                _aadPlugin.CreateWebTokenRequestAsync(
                    wamAccountProvider,
                    requestParams,
                    isForceLoginPrompt: false, 
                    isInteractive: true,
                    isAccountInWam: true)
                    .Returns(Task.FromResult(webTokenRequest));

                var webTokenResponseWrapper = Substitute.For<IWebTokenRequestResultWrapper>();
                webTokenResponseWrapper.ResponseStatus.Returns(WebTokenRequestStatus.Success);
                var webTokenResponse = new WebTokenResponse();
                webTokenResponseWrapper.ResponseData.Returns(new List<WebTokenResponse>() { webTokenResponse });

                _wamProxy.RequestTokenForWindowAsync(Arg.Any<IntPtr>(), webTokenRequest, webAccount2).
                    Returns(Task.FromResult(webTokenResponseWrapper));
                _aadPlugin.ParseSuccessfullWamResponse(webTokenResponse, out _).Returns(_msalTokenResponse);

                var atiParams = new AcquireTokenInteractiveParameters();

                // Act
                var result = await _wamBroker.AcquireTokenInteractiveAsync(requestParams, atiParams)
                    .ConfigureAwait(false);

                // Assert 
                Assert.AreSame(_msalTokenResponse, result);
                AssertTelemetryHeadersInRequest(webTokenRequest.Properties);
            }
        }

        [TestMethod]
        public async Task RemoveAADAccountAsync()
        {
            string aadHomeAccId = $"{TestConstants.Uid}.{TestConstants.Utid}";
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var wamAccountProvider = new WebAccountProvider("id", "user@contoso.com", null);
                var requestParams = harness.CreateAuthenticationRequestParameters(TestConstants.AuthorityConsumerTidTenant); // MSA
                requestParams.Account = new Account(
                                    aadHomeAccId, // matching in on home acc id
                                    "doesnt_matter@contoso.com", // matching is not on UPN
                                    null); // account does not have wam_id, might be coming directly from WAM

                var webAccount = new WebAccount(wamAccountProvider, "user@contoso.com", WebAccountState.Connected);
                IReadOnlyList<WebAccount> webAccounts = new List<WebAccount>() { webAccount };

                _wamProxy.FindAllWebAccountsAsync(wamAccountProvider, TestConstants.ClientId).Returns(Task.FromResult(webAccounts));

                // WAM can give MSAL the home account ID of a Wam account, which MSAL matches to a WAM account
                _aadPlugin.GetHomeAccountIdOrNull(webAccount).Returns(aadHomeAccId);

                var atsParams = new AcquireTokenSilentParameters();
                _webAccountProviderFactory.GetAccountProviderAsync("organizations").ReturnsForAnyArgs(Task.FromResult(wamAccountProvider));

                // Act Assert
                await AssertException.TaskThrowsAsync<FileNotFoundException>( // Since WebAccount is a real object, it throws this exception
                    () => _wamBroker.RemoveAccountAsync(harness.ServiceBundle.Config, requestParams.Account))
                    .ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task RemoveGuestAADAccountAsync()
        {
            string aadHomeAccId = $"{TestConstants.Uid}.{TestConstants.MsaTenantId}";

            // Arrange
            using (var harness = CreateTestHarness())
            {
                var wamAccountProvider = new WebAccountProvider("id", "user@contoso.com", null);
                var requestParams = harness.CreateAuthenticationRequestParameters(TestConstants.AuthorityOrganizationsTenant);
                // configure msa-pt
                requestParams.AppConfig.WindowsBrokerOptions = new WindowsBrokerOptions() { MsaPassthrough = true };

                requestParams.Account = new Account(
                                    aadHomeAccId, 
                                    "user@outlook.com", // matching is not on UPN
                                    null); // account does not have wam_id, might be coming directly from WAM

                var webAccount = new WebAccount(wamAccountProvider, "user@outlook.com", WebAccountState.Connected);
                IReadOnlyList<WebAccount> webAccounts = new List<WebAccount>() { webAccount };

                _wamProxy.FindAllWebAccountsAsync(wamAccountProvider, TestConstants.ClientId).Returns(Task.FromResult(webAccounts));

                // WAM can give MSAL the home account ID of a Wam account, which MSAL matches to a WAM account
                _aadPlugin.GetHomeAccountIdOrNull(webAccount).Returns(aadHomeAccId);

                var atsParams = new AcquireTokenSilentParameters();
                _webAccountProviderFactory.GetAccountProviderAsync("organizations").ReturnsForAnyArgs(Task.FromResult(wamAccountProvider));

                
                // This assertion ensures that WebAccount.SignOutAsync() is called. Since the WebAccount is fake, it throws a specific exception.
                // This can be improved with a extra layer of abstraction over WebAccount, but it is sufficient for testing

                await AssertException.TaskThrowsAsync<FileNotFoundException>( // Since WebAccount is a real object, it throws this exception
                    () => _wamBroker.RemoveAccountAsync(harness.ServiceBundle.Config, requestParams.Account))
                    .ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task GetAccounts_DoesNotCallPlugins_Async()
        {
            string aadHomeAccId = $"{TestConstants.Uid}.{TestConstants.Utid}";
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var wamAccountProvider = new WebAccountProvider("id", "user@contoso.com", null);
                var requestParams = harness.CreateAuthenticationRequestParameters(TestConstants.AuthorityHomeTenant); 

                requestParams.AppConfig.WindowsBrokerOptions = new WindowsBrokerOptions() { ListWindowsWorkAndSchoolAccounts = true };
                _wamBroker = new WamBroker(
                   _coreUIParent,
                    requestParams.AppConfig,
                   _logger,
                   _aadPlugin,
                   _msaPlugin,
                   _wamProxy,
                   _webAccountProviderFactory,
                   _accountPickerFactory,
                   _msaPassthroughHandler);              

                var cacheSessionManager = NSubstitute.Substitute.For<ICacheSessionManager>();
                var discoveryManager = NSubstitute.Substitute.For<IInstanceDiscoveryManager>();
                var authoritInfo = AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityHomeTenant, true);

                // Act
                await _wamBroker.GetAccountsAsync(
                    TestConstants.ClientId,
                    TestConstants.RedirectUri,
                    authoritInfo,
                    cacheSessionManager,
                    discoveryManager).ConfigureAwait(false);

                // Assert
                await _aadPlugin.Received().GetAccountsAsync(
                    TestConstants.ClientId,
                    authoritInfo,
                    cacheSessionManager,
                    discoveryManager).ConfigureAwait(false);

                await _msaPlugin.Received().GetAccountsAsync(
                   TestConstants.ClientId,
                   authoritInfo,
                   cacheSessionManager,
                   discoveryManager).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task GetAccounts_CallsPlugins_Async()
        {
            string aadHomeAccId = $"{TestConstants.Uid}.{TestConstants.Utid}";
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var requestParams = harness.CreateAuthenticationRequestParameters(TestConstants.AuthorityHomeTenant);

                requestParams.AppConfig.WindowsBrokerOptions = new WindowsBrokerOptions() { 
                    ListWindowsWorkAndSchoolAccounts = false};
                _wamBroker = new WamBroker(
                   _coreUIParent,
                    requestParams.AppConfig,
                   _logger,
                   _aadPlugin,
                   _msaPlugin,
                   _wamProxy,
                   _webAccountProviderFactory,
                   _accountPickerFactory,
                   _msaPassthroughHandler);

                var cacheSessionManager = NSubstitute.Substitute.For<ICacheSessionManager>();
                var discoveryManager = NSubstitute.Substitute.For<IInstanceDiscoveryManager>();

                // Act
                await _wamBroker.GetAccountsAsync(
                    TestConstants.ClientId,
                    TestConstants.RedirectUri,
                    AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityHomeTenant, true),
                    cacheSessionManager,
                    discoveryManager).ConfigureAwait(false);

                // Assert
                await _aadPlugin.DidNotReceive().GetAccountsAsync(
                    TestConstants.ClientId,
                    AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityHomeTenant, true),
                    cacheSessionManager,
                    discoveryManager).ConfigureAwait(false);

                await _msaPlugin.DidNotReceive().GetAccountsAsync(
                   TestConstants.ClientId,
                    AuthorityInfo.FromAuthorityUri(TestConstants.AuthorityHomeTenant, true),
                   cacheSessionManager,
                   discoveryManager).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task RemoveAccount_DoesNothing_WhenAccountDoesNotMatchWAM_Async()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var requestParams = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityHomeTenant);
                requestParams.Account = new Account("a.b", "c", "env");

                var wamAccountProvider = new WebAccountProvider("id", "user@contoso.com", null);

                _webAccountProviderFactory.GetAccountProviderAsync("organizations").
                    ReturnsForAnyArgs(Task.FromResult(wamAccountProvider));

                // Act Assert
                await _wamBroker.RemoveAccountAsync(harness.ServiceBundle.Config, requestParams.Account)
                    .ConfigureAwait(false);

            }
        }

        [TestMethod]
        public void TestDefaultAccountPluginSelection()
        {
            _webAccountProviderFactory.IsDefaultAccountMsaAsync().Returns(true);
            Assert.IsTrue(
              _wamBroker.IsMsaRequestAsync(Authority.CreateAuthority(TestConstants.AuthorityCommonTenant), null, false).Result,
              "Common authority with no account - use Windows default account");
            _webAccountProviderFactory.Received(1).IsDefaultAccountMsaAsync();

            _webAccountProviderFactory.IsDefaultAccountMsaAsync().Returns(false);
            Assert.IsFalse(
             _wamBroker.IsMsaRequestAsync(Authority.CreateAuthority(TestConstants.AuthorityCommonTenant), null, false).Result,
             "Common authority with no account - use Windows default account");
            _webAccountProviderFactory.Received(2).IsDefaultAccountMsaAsync();

        }

        [TestMethod]
        public void TestPluginSelection()
        {
            var ex = AssertException.Throws<MsalClientException>(
                () => _wamBroker.IsMsaRequestAsync(Authority.CreateAuthority(TestConstants.B2CAuthority), null, false).GetAwaiter().GetResult());
            Assert.AreEqual(MsalError.WamNoB2C, ex.ErrorCode);

            Assert.IsFalse(
                _wamBroker.IsMsaRequestAsync(Authority.CreateAuthority(TestConstants.ADFSAuthority), null, false).Result,
                "ADFS authorities should be handled by AAD plugin");

            Assert.IsTrue(
                _wamBroker.IsMsaRequestAsync(Authority.CreateAuthority(TestConstants.AuthorityCommonTenant), TestConstants.MsaTenantId, false).Result,
                "Common authority - look at account tenant ID to determine plugin");

            Assert.IsFalse(
                _wamBroker.IsMsaRequestAsync(Authority.CreateAuthority(TestConstants.AuthorityCommonTenant), TestConstants.TenantId, false).Result,
                "Common authority - look at account tenant ID to determine plugin");

            Assert.IsFalse(
               _wamBroker.IsMsaRequestAsync(Authority.CreateAuthority(TestConstants.AuthorityOrganizationsTenant), TestConstants.TenantId, false).Result,
               "Organizations authority - AAD plugin unless MSA-pt");

            Assert.IsFalse(
               _wamBroker.IsMsaRequestAsync(Authority.CreateAuthority(TestConstants.AuthorityOrganizationsTenant), TestConstants.TenantId, true).Result,
               "MSA-PT is always served by the AAD plugin");

            Assert.IsFalse(
               _wamBroker.IsMsaRequestAsync(Authority.CreateAuthority(TestConstants.AuthorityOrganizationsTenant), TestConstants.MsaTenantId, true).Result,
               "MSA-PT is always served by the AAD plugin");

            Assert.IsTrue(
                _wamBroker.IsMsaRequestAsync(Authority.CreateAuthority(TestConstants.AuthorityConsumersTenant), TestConstants.TenantId, false).Result,
                "Consumer authority - msa plugin");

            Assert.IsFalse(
               _wamBroker.IsMsaRequestAsync(Authority.CreateAuthority(TestConstants.AuthorityGuidTenant), TestConstants.TenantId, true).Result,
               "MSA-PT is always served by the AAD plugin");
        }

        [TestMethod]
        public void TestPromptMapping()
        {
            Assert.IsFalse(WamBroker.IsForceLoginPrompt(Prompt.NotSpecified));
            Assert.IsFalse(WamBroker.IsForceLoginPrompt(Prompt.NoPrompt));
            Assert.IsTrue(WamBroker.IsForceLoginPrompt(Prompt.SelectAccount));
            Assert.IsTrue(WamBroker.IsForceLoginPrompt(Prompt.ForceLogin));
            Assert.IsTrue(WamBroker.IsForceLoginPrompt(Prompt.Consent));
        }

#if DESKTOP
        [TestMethod]
        public void NetFwkPlatformNotAvailable()
        {
            AssertException.Throws<PlatformNotSupportedException>(() =>
                PublicClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithExperimentalFeatures(true)
                .WithBroker()
                .Build());
        }
#endif

#region MSA-PT 
        [TestMethod]
        public async Task ATI_WithPicker_MsaPt_Async()
        {
            string homeAccId = $"{TestConstants.Uid}.{TestConstants.Utid}";
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var requestParams = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityOrganizationsTenant); 

                // msa-pt scenario
                requestParams.AppConfig.WindowsBrokerOptions = new WindowsBrokerOptions() { MsaPassthrough = true };
                _wamBroker = new WamBroker(
                   _coreUIParent,
                    requestParams.AppConfig,
                   _logger,
                   _aadPlugin,
                   _msaPlugin,
                   _wamProxy,
                   _webAccountProviderFactory,
                   _accountPickerFactory,
                   _msaPassthroughHandler);

                var accountPicker = Substitute.For<IAccountPicker>();
                _accountPickerFactory.Create(Arg.Any<IntPtr>(), null, null, null, false, null).ReturnsForAnyArgs(accountPicker);
                var msaProvider = new WebAccountProvider("msa", "user@contoso.com", null);
                accountPicker.DetermineAccountInteractivelyAsync().Returns(Task.FromResult(msaProvider));
                // AAD plugin + consumer provider = Guest MSA-PT scenario
                _webAccountProviderFactory.IsConsumerProvider(msaProvider).Returns(true);
                _msaPassthroughHandler.TryFetchTransferTokenInteractiveAsync(requestParams, msaProvider)
                    .Returns(Task.FromResult("transfer_token"));

                var aadProvider = new WebAccountProvider("aad", "user@contoso.com", null);
                _webAccountProviderFactory.GetAccountProviderAsync(TestConstants.AuthorityOrganizationsTenant).Returns(aadProvider);

                // make sure the final request is done with the AAD provider
                var webTokenRequest = new WebTokenRequest(aadProvider);
                _aadPlugin.CreateWebTokenRequestAsync(
                    aadProvider,
                    requestParams,
                    isForceLoginPrompt: false,
                    isInteractive: true,
                    isAccountInWam: false)
                    .Returns(Task.FromResult(webTokenRequest));

                var webTokenResponseWrapper = Substitute.For<IWebTokenRequestResultWrapper>();
                webTokenResponseWrapper.ResponseStatus.Returns(WebTokenRequestStatus.Success);
                var webTokenResponse = new WebTokenResponse();
                webTokenResponseWrapper.ResponseData.Returns(new List<WebTokenResponse>() { webTokenResponse });

                _wamProxy.RequestTokenForWindowAsync(Arg.Any<IntPtr>(), webTokenRequest).
                    Returns(Task.FromResult(webTokenResponseWrapper));
                _aadPlugin.ParseSuccessfullWamResponse(webTokenResponse, out _).Returns(_msalTokenResponse);

                var atiParams = new AcquireTokenInteractiveParameters();

                // Act
                var result = await _wamBroker.AcquireTokenInteractiveAsync(requestParams, atiParams)
                    .ConfigureAwait(false);

                // Assert 
                Assert.AreSame(_msalTokenResponse, result);
                _msaPassthroughHandler.Received(1).AddTransferTokenToRequest(webTokenRequest, "transfer_token");
                AssertTelemetryHeadersInRequest(webTokenRequest.Properties);
                Assert.AreEqual(
                   "https://login.microsoftonline.com/organizations/",
                   webTokenRequest.Properties["authority"]);
            }
        }

        [TestMethod]
        public async Task ATI_WithPicker_MsaPt_WorkAndSchool_Async()
        {
            string homeAccId = $"{TestConstants.Uid}.{TestConstants.Utid}";
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var requestParams = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityOrganizationsTenant); // AAD authorities for whi

                // msa-pt scenario
                requestParams.AppConfig.WindowsBrokerOptions = new WindowsBrokerOptions() { MsaPassthrough = true };
                _wamBroker = new WamBroker(
                   _coreUIParent,
                    requestParams.AppConfig,
                   _logger,
                   _aadPlugin,
                   _msaPlugin,
                   _wamProxy,
                   _webAccountProviderFactory,
                   _accountPickerFactory,
                   _msaPassthroughHandler);

                var accountPicker = Substitute.For<IAccountPicker>();
                _accountPickerFactory.Create(Arg.Any<IntPtr>(), null, null, null, false, null).ReturnsForAnyArgs(accountPicker);
                var aadProvider = new WebAccountProvider("work_and_school", "user@contoso.com", null);
                accountPicker.DetermineAccountInteractivelyAsync().Returns(Task.FromResult(aadProvider));
                _webAccountProviderFactory.IsConsumerProvider(aadProvider).Returns(false);
                _webAccountProviderFactory.IsOrganizationsProvider(aadProvider).Returns(true);
               

                // make sure the final request is done with the AAD provider
                var webTokenRequest = new WebTokenRequest(aadProvider);
                _aadPlugin.CreateWebTokenRequestAsync(
                    aadProvider,
                    requestParams,
                    isForceLoginPrompt: false,
                     isInteractive: true,
                     isAccountInWam: false)
                    .Returns(Task.FromResult(webTokenRequest));

                var webTokenResponseWrapper = Substitute.For<IWebTokenRequestResultWrapper>();
                webTokenResponseWrapper.ResponseStatus.Returns(WebTokenRequestStatus.Success);
                var webTokenResponse = new WebTokenResponse();
                webTokenResponseWrapper.ResponseData.Returns(new List<WebTokenResponse>() { webTokenResponse });

                _wamProxy.RequestTokenForWindowAsync(Arg.Any<IntPtr>(), webTokenRequest).
                    Returns(Task.FromResult(webTokenResponseWrapper));
                _aadPlugin.ParseSuccessfullWamResponse(webTokenResponse, out _).Returns(_msalTokenResponse);

                var atiParams = new AcquireTokenInteractiveParameters();

                // Act
                var result = await _wamBroker.AcquireTokenInteractiveAsync(requestParams, atiParams)
                    .ConfigureAwait(false);

                // Assert 
                Assert.AreSame(_msalTokenResponse, result);
                AssertTelemetryHeadersInRequest(webTokenRequest.Properties);
                webTokenRequest.Properties.TryGetValue("authority", out string authority);
                Assert.AreEqual(
                    "https://login.microsoftonline.com/common/",
                    authority, 
                    "Expecting the authority to have been changed from /organizations to /common, to workaround the PRT update bug");

            }
        }

     

        [TestMethod]
        public async Task ATS_MsaPt_Async()
        {
            string homeAccId = $"{TestConstants.Uid}.{TestConstants.Utid}";

            // Arrange
            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                _webAccountProviderFactory.ClearReceivedCalls();

                
                var requestParams = harness.CreateAuthenticationRequestParameters(
                    $"https://login.microsoftonline.com/{TestConstants.MsaTenantId}", // this is how MSAL displays the tenant id                
                    validateAuthority: true);
                requestParams.AppConfig.WindowsBrokerOptions = new WindowsBrokerOptions() { MsaPassthrough = true };

                requestParams.Account = new Account(
                    $"{TestConstants.Uid}.{TestConstants.Utid}",
                    "user@live.com",
                    null,
                    null); // account does not have wam_id!

                _wamBroker = new WamBroker(
                  _coreUIParent,
                   requestParams.AppConfig,
                  _logger,
                  _aadPlugin,
                  _msaPlugin,
                  _wamProxy,
                  _webAccountProviderFactory,
                  _accountPickerFactory,
                  _msaPassthroughHandler);

                var wamAccountProvider = new WebAccountProvider("id", "AAD", null);

                var webAccount = new WebAccount(wamAccountProvider, "user@live.com", WebAccountState.Connected);
                IReadOnlyList<WebAccount> webAccounts = new List<WebAccount>() { webAccount };

                var webTokenRequest = new WebTokenRequest(wamAccountProvider);
                var webTokenResponseWrapper = Substitute.For<IWebTokenRequestResultWrapper>();
                webTokenResponseWrapper.ResponseStatus.Returns(WebTokenRequestStatus.Success);
                var webTokenResponse = new WebTokenResponse();
                webTokenResponseWrapper.ResponseData.Returns(new List<WebTokenResponse>() { webTokenResponse });

                _webAccountProviderFactory.GetAccountProviderAsync("organizations").ReturnsForAnyArgs(Task.FromResult(wamAccountProvider));
                _wamProxy.FindAllWebAccountsAsync(Arg.Any<WebAccountProvider>(), TestConstants.ClientId).Returns(Task.FromResult(webAccounts));
                _aadPlugin.CreateWebTokenRequestAsync(
                    wamAccountProvider,
                    requestParams,
                    isForceLoginPrompt: false,
                    isAccountInWam: true,
                    isInteractive: false)
                    .Returns(Task.FromResult(webTokenRequest));

                var atsParams = new AcquireTokenSilentParameters();

                _wamProxy.GetTokenSilentlyAsync(webAccount, webTokenRequest).
                    Returns(Task.FromResult(webTokenResponseWrapper));

                _aadPlugin.ParseSuccessfullWamResponse(webTokenResponse, out _).Returns(_msalTokenResponse);

                // Act
                var result = await _wamBroker.AcquireTokenSilentAsync(requestParams, atsParams).ConfigureAwait(false);

                // Assert 
                Assert.AreSame(_msalTokenResponse, result);
                AssertTelemetryHeadersInRequest(webTokenRequest.Properties);
                Assert.AreEqual("https://login.microsoftonline.com/organizations/", webTokenRequest.Properties["authority"]);
            }
        }

        [TestMethod]
        public async Task ATI_WithPicker_MsaPt_NoTransferToken_Async()
        {
            string homeAccId = $"{TestConstants.Uid}.{TestConstants.Utid}";
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var requestParams = harness.CreateAuthenticationRequestParameters(
                    TestConstants.AuthorityHomeTenant);

                // msa-pt scenario
                requestParams.AppConfig.WindowsBrokerOptions = new WindowsBrokerOptions() { MsaPassthrough = true };
                _wamBroker = new WamBroker(
                   _coreUIParent,
                    requestParams.AppConfig,
                   _logger,
                   _aadPlugin,
                   _msaPlugin,
                   _wamProxy,
                   _webAccountProviderFactory,
                   _accountPickerFactory,
                   _msaPassthroughHandler);
                var accountPicker = Substitute.For<IAccountPicker>();
                _accountPickerFactory.Create(Arg.Any<IntPtr>(), null, null, null, false, null).ReturnsForAnyArgs(accountPicker);
                var msaProvider = new WebAccountProvider("msa", "user@contoso.com", null);
                accountPicker.DetermineAccountInteractivelyAsync().Returns(Task.FromResult(msaProvider));
                // AAD plugin + consumer provider = Guest MSA-PT scenario
                _webAccountProviderFactory.IsConsumerProvider(msaProvider).Returns(true);
                _msaPassthroughHandler.TryFetchTransferTokenInteractiveAsync(requestParams, msaProvider)
                    .Returns(Task.FromResult<string>(null));

                var aadProvider = new WebAccountProvider("aad", "user@contoso.com", null);
                _webAccountProviderFactory.GetAccountProviderAsync(TestConstants.AuthorityHomeTenant).Returns(aadProvider);

                // make sure the final request is done with the AAD provider
                var webTokenRequest = new WebTokenRequest(aadProvider);
                _aadPlugin.CreateWebTokenRequestAsync(
                    aadProvider,
                    requestParams,
                    isForceLoginPrompt: true, // it is important to force prompt if a transfer token was not obtained
                     isInteractive: true,
                     isAccountInWam: false)
                    .Returns(Task.FromResult(webTokenRequest));

                var webTokenResponseWrapper = Substitute.For<IWebTokenRequestResultWrapper>();
                webTokenResponseWrapper.ResponseStatus.Returns(WebTokenRequestStatus.Success);
                var webTokenResponse = new WebTokenResponse();
                webTokenResponseWrapper.ResponseData.Returns(new List<WebTokenResponse>() { webTokenResponse });

                _wamProxy.RequestTokenForWindowAsync(Arg.Any<IntPtr>(), webTokenRequest).
                    Returns(Task.FromResult(webTokenResponseWrapper));
                _aadPlugin.ParseSuccessfullWamResponse(webTokenResponse, out _).Returns(_msalTokenResponse);

                var atiParams = new AcquireTokenInteractiveParameters();
                atiParams.Prompt = Prompt.SelectAccount;

                // Act
                var result = await _wamBroker.AcquireTokenInteractiveAsync(requestParams, atiParams)
                    .ConfigureAwait(false);

                // Assert 
                Assert.AreSame(_msalTokenResponse, result);
                Assert.AreEqual("select_account", webTokenRequest.Properties["prompt"]);
                _msaPassthroughHandler.Received(1).AddTransferTokenToRequest(webTokenRequest, null);
                AssertTelemetryHeadersInRequest(webTokenRequest.Properties);
            }
        }
#endregion

        private async Task RunPluginSelectionTestAsync(string inputAuthority, bool expectMsaPlugin)
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                _webAccountProviderFactory.ClearReceivedCalls();

                var acc = new WebAccountProvider("id", "user@contoso.com", null);

                var requestParams = harness.CreateAuthenticationRequestParameters(inputAuthority);
                requestParams.Account = new Account(
                    $"{TestConstants.Uid}.{TestConstants.Utid}",
                    TestConstants.DisplayableId,
                    null);

                var atsParams = new AcquireTokenSilentParameters();
                _webAccountProviderFactory.GetAccountProviderAsync(null).ReturnsForAnyArgs(Task.FromResult(acc));

                // Act
                var ex = await AssertException.TaskThrowsAsync<MsalUiRequiredException>(
                    () => _wamBroker.AcquireTokenSilentAsync(requestParams, atsParams)).ConfigureAwait(false);

                // Assert
                Assert.AreEqual(MsalError.InteractionRequired, ex.ErrorCode);

                if (expectMsaPlugin)
                {
                    await _webAccountProviderFactory.Received(1).GetAccountProviderAsync("consumers").ConfigureAwait(false);
                }
                else
                {
                    await _webAccountProviderFactory.Received(1).GetAccountProviderAsync(inputAuthority).ConfigureAwait(false);
                }
            }
        }

        private void AssertTelemetryHeadersInRequest(IDictionary<string, string> webTokenRequestProperties)
        {
            Assert.IsTrue(webTokenRequestProperties.ContainsKey(MsalIdParameter.Product));
            Assert.IsTrue(webTokenRequestProperties.ContainsKey(MsalIdParameter.Version));
            Assert.IsTrue(webTokenRequestProperties.ContainsKey(MsalIdParameter.CpuPlatform));
            Assert.IsTrue(webTokenRequestProperties.ContainsKey(MsalIdParameter.OS));
        }
    }
}

#endif
