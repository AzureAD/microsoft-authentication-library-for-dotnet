using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
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
        private WamBroker _wamBroker;
        private SynchronizationContext _synchronizationContext;

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
            _synchronizationContext = new DedicatedThreadSynchronisationContext();

            _coreUIParent = new CoreUIParent() { SynchronizationContext = _synchronizationContext };

            _logger = Substitute.For<ICoreLogger>();
            _aadPlugin = Substitute.For<IWamPlugin>();
            _msaPlugin = Substitute.For<IWamPlugin>();
            _wamProxy = Substitute.For<IWamProxy>();
            _webAccountProviderFactory = Substitute.For<IWebAccountProviderFactory>();
            _accountPickerFactory = Substitute.For<IAccountPickerFactory>();

            _wamBroker = new WamBroker(
                _coreUIParent,
                _logger,
                _aadPlugin,
                _msaPlugin,
                _wamProxy,
                _webAccountProviderFactory,
                _accountPickerFactory);
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
                    TestConstants.AuthorityHomeTenant,
                    extraQueryParameters: extraQP,
                    validateAuthority: true); // AAD                
                requestParams.UserConfiguredAuthority = Authority.CreateAuthority("https://login.microsoftonline.com/organizations");

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

                _aadPlugin.ParseSuccesfullWamResponse(webTokenResponse).Returns(_msalTokenResponse);

                // Act
                var result = await _wamBroker.AcquireTokenSilentAsync(requestParams, atsParams).ConfigureAwait(false);

                // Assert 
                Assert.AreSame(_msalTokenResponse, result);
                Assert.AreEqual("yes", webTokenRequest.Properties["validateAuthority"]);
                Assert.AreEqual("extraVal1", webTokenRequest.Properties["extraQp1"]);

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
                _aadPlugin.ParseSuccesfullWamResponse(Arg.Any<WebTokenResponse>()).Returns(_msalTokenResponse);

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
                    .Returns("ui_is_really_needed");

                // Act
                var result = await _wamBroker.AcquireTokenSilentAsync(requestParams, new AcquireTokenSilentParameters()).ConfigureAwait(false);

                // Assert 
                Assert.AreEqual("ui_is_really_needed", result.Error);
                Assert.AreEqual("42", result.ErrorCodes[0]);
                Assert.IsTrue(result.ErrorDescription.Contains("more_detailed_error_message"));
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
                    .Returns("ui_is_really_needed");

                // Act
                var result = await _wamBroker.AcquireTokenSilentAsync(requestParams, new AcquireTokenSilentParameters()).ConfigureAwait(false);

                // Assert 
                Assert.AreEqual("ui_is_really_needed", result.Error);
                Assert.AreEqual("42", result.ErrorCodes[0]);
                Assert.IsTrue(result.ErrorDescription.Contains("more_detailed_error_message"));
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
                var result = await _wamBroker.AcquireTokenSilentAsync(requestParams, new AcquireTokenSilentParameters()).ConfigureAwait(false);

                // Assert 
                Assert.AreEqual(MsalError.AuthenticationCanceledError, result.Error);
                Assert.AreEqual(MsalErrorMessage.AuthenticationCanceled, result.ErrorDescription);
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
                _msaPlugin.ParseSuccesfullWamResponse(webTokenResponse).Returns(_msalTokenResponse);


                // Act
                var result = await _wamBroker.AcquireTokenSilentAsync(requestParams, atsParams).ConfigureAwait(false);

                // Assert 
                Assert.AreSame(_msalTokenResponse, result);
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
        public async Task ATI_RequiresSyncContext_Async()
        {
            var wamBroker = new WamBroker(
            new CoreUIParent(), // no sync context here
                _logger,
                _aadPlugin,
                _msaPlugin,
                _wamProxy,
                _webAccountProviderFactory,
                _accountPickerFactory);
            using (var harness = CreateTestHarness())
            {
                var requestParams = harness.CreateAuthenticationRequestParameters(TestConstants.AuthorityHomeTenant); // AAD
                var atiParams = new AcquireTokenInteractiveParameters();

                // Act
                var ex = await AssertException.TaskThrowsAsync<MsalClientException>(
                    () => wamBroker.AcquireTokenInteractiveAsync(requestParams, atiParams)).ConfigureAwait(false);

                // Assert
                Assert.AreEqual(MsalError.WamUiThread, ex.ErrorCode);
            }
        }

        [TestMethod]
        public async Task ATI_WithoutPicker_Async()
        {
            string homeAccId = $"{TestConstants.Uid}.{TestConstants.Utid}";
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
                _aadPlugin.ParseSuccesfullWamResponse(webTokenResponse).Returns(_msalTokenResponse);

                // Act
                var result = await _wamBroker.AcquireTokenInteractiveAsync(
                    requestParams, 
                    new AcquireTokenInteractiveParameters()).ConfigureAwait(false);

                // Assert 
                Assert.AreSame(_msalTokenResponse, result);
            }
        }

        [TestMethod]
        public async Task ATI_WithPicker_Async()
        {
            string homeAccId = $"{TestConstants.Uid}.{TestConstants.Utid}";
            // Arrange
            using (var harness = CreateTestHarness())
            {
                var requestParams = harness.CreateAuthenticationRequestParameters(TestConstants.AuthorityHomeTenant); // AAD
                requestParams.Account = new Account(
                   $"{TestConstants.Uid}.{TestConstants.Utid}",
                   TestConstants.DisplayableId,
                   null,
                   new Dictionary<string, string>() { { TestConstants.ClientId, "wam_id_1" } }); // account has wam_id!

                var accountPicker = Substitute.For<IAccountPicker>();

                _accountPickerFactory.Create(Arg.Any<IntPtr>(), null, null, null, false).ReturnsForAnyArgs(accountPicker);

                var wamAccountProvider = new WebAccountProvider("id", "user@contoso.com", null);
                accountPicker.DetermineAccountInteractivelyAsync().Returns(Task.FromResult(wamAccountProvider));

                var webTokenRequest = new WebTokenRequest(wamAccountProvider);
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
                _aadPlugin.ParseSuccesfullWamResponse(webTokenResponse).Returns(_msalTokenResponse);

                var atiParams = new AcquireTokenInteractiveParameters();

                // Act
                var result = await _wamBroker.AcquireTokenInteractiveAsync(requestParams, atiParams)
                    .ConfigureAwait(false);

                // Assert 
                Assert.AreSame(_msalTokenResponse, result);
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
        public void TestPluginSelection()
        {

            var ex = AssertException.Throws<MsalClientException>(
                () => _wamBroker.IsMsaRequest(Authority.CreateAuthority(TestConstants.B2CAuthority), null, false));
            Assert.AreEqual(MsalError.WamNoB2C, ex.ErrorCode);

            Assert.IsFalse(
                _wamBroker.IsMsaRequest(Authority.CreateAuthority(TestConstants.ADFSAuthority), null, false),
                "ADFS authorities should be handled by AAD plugin");

            Assert.IsTrue(
                _wamBroker.IsMsaRequest(Authority.CreateAuthority(TestConstants.AuthorityCommonTenant), TestConstants.MsaTenantId, false),
                "Common authority - look at account tenant ID to determine plugin");

            Assert.IsFalse(
                _wamBroker.IsMsaRequest(Authority.CreateAuthority(TestConstants.AuthorityCommonTenant), TestConstants.TenantId, false),
                "Common authority - look at account tenant ID to determine plugin");

            Assert.IsFalse(
               _wamBroker.IsMsaRequest(Authority.CreateAuthority(TestConstants.AuthorityOrganizationsTenant), TestConstants.TenantId, false),
               "Organizations authority - AAD plugin unless MSA-pt");

            Assert.IsFalse(
               _wamBroker.IsMsaRequest(Authority.CreateAuthority(TestConstants.AuthorityOrganizationsTenant), TestConstants.TenantId, true),
               "Organizations authority with MSA-pt - based on home account id");

            Assert.IsTrue(
               _wamBroker.IsMsaRequest(Authority.CreateAuthority(TestConstants.AuthorityOrganizationsTenant), TestConstants.MsaTenantId, true),
               "Organizations authority with MSA-pt - based on home account id");

            Assert.IsTrue(
                _wamBroker.IsMsaRequest(Authority.CreateAuthority(TestConstants.AuthorityConsumersTenant), TestConstants.TenantId, false),
                "Consumer authority - msa plugin");

            Assert.IsFalse(
               _wamBroker.IsMsaRequest(Authority.CreateAuthority(TestConstants.AuthorityGuidTenant), TestConstants.TenantId, true),
               "Tenanted authorities - AAD plugin");
        }

        [TestMethod]
        public void TestPromptMapping()
        {
            Assert.IsFalse(WamBroker.IsForceLoginPrompt(Prompt.NotSpecified));
            Assert.IsFalse(WamBroker.IsForceLoginPrompt(Prompt.NoPrompt));
            Assert.IsTrue(WamBroker.IsForceLoginPrompt(Prompt.SelectAccount));
            Assert.IsTrue(WamBroker.IsForceLoginPrompt(Prompt.ForceLogin));

            AssertException.Throws<MsalClientException>(() => WamBroker.IsForceLoginPrompt(Prompt.Consent));
#if DESKTOP
            AssertException.Throws<MsalClientException>(() => WamBroker.IsForceLoginPrompt(Prompt.Never));
#endif
        }

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

    }

    // A simple SynchronizationContext that encapsulates it's own dedicated task queue and processing
    // thread for servicing Send() & Post() calls.  
    // Based upon http://blogs.msdn.com/b/pfxteam/archive/2012/01/20/10259049.aspx but uses it's own thread
    // rather than running on the thread that it's instanciated on
    public sealed class DedicatedThreadSynchronisationContext : SynchronizationContext, IDisposable
    {
        public DedicatedThreadSynchronisationContext()
        {
            m_thread = new Thread(ThreadWorkerDelegate);
            m_thread.Start(this);
        }

        public void Dispose()
        {
            m_queue.CompleteAdding();
        }

        /// <summary>Dispatches an asynchronous message to the synchronization context.</summary>
        /// <param name="d">The System.Threading.SendOrPostCallback delegate to call.</param>
        /// <param name="state">The object passed to the delegate.</param>
        public override void Post(SendOrPostCallback d, object state)
        {
            if (d == null)
                throw new ArgumentNullException("d");
            m_queue.Add(new KeyValuePair<SendOrPostCallback, object>(d, state));
        }

        /// <summary> As 
        public override void Send(SendOrPostCallback d, object state)
        {
            using (var handledEvent = new ManualResetEvent(false))
            {
                Post(SendOrPostCallback_BlockingWrapper, Tuple.Create(d, state, handledEvent));
                handledEvent.WaitOne();
            }
        }

        public int WorkerThreadId { get { return m_thread.ManagedThreadId; } }
        //=========================================================================================

        private static void SendOrPostCallback_BlockingWrapper(object state)
        {
            var innerCallback = (state as Tuple<SendOrPostCallback, object, ManualResetEvent>);
            try
            {
                innerCallback.Item1(innerCallback.Item2);
            }
            finally
            {
                innerCallback.Item3.Set();
            }
        }

        /// <summary>The queue of work items.</summary>
        private readonly BlockingCollection<KeyValuePair<SendOrPostCallback, object>> m_queue =
            new BlockingCollection<KeyValuePair<SendOrPostCallback, object>>();

        private readonly Thread m_thread = null;

        /// <summary>Runs an loop to process all queued work items.</summary>
        private void ThreadWorkerDelegate(object obj)
        {
            SynchronizationContext.SetSynchronizationContext(obj as SynchronizationContext);

            try
            {
                foreach (var workItem in m_queue.GetConsumingEnumerable())
                    workItem.Key(workItem.Value);
            }
            catch (ObjectDisposedException) { }
        }
    }
}

