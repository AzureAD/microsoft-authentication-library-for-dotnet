using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
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
        private WamBroker _wamBroker;

        [TestInitialize]
        public void Init()
        {
            _coreUIParent = new CoreUIParent();
            _logger = Substitute.For<ICoreLogger>();
            _aadPlugin = Substitute.For<IWamPlugin>();
            _msaPlugin = Substitute.For<IWamPlugin>();
            _wamProxy = Substitute.For<IWamProxy>();
            _webAccountProviderFactory = Substitute.For<IWebAccountProviderFactory>();

            _wamBroker = new WamBroker(_coreUIParent, _logger, _aadPlugin, _msaPlugin, _wamProxy, _webAccountProviderFactory);
        }

        [TestMethod]
        public async Task WAM_RemoveAccount_DoesNothing_Async()
        {
            await _wamBroker.RemoveAccountAsync(TestConstants.ClientId, new Account("a.b", "user", "login.linux.net"))
                .ConfigureAwait(false);
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
        [Ignore] // ignore until we figure out what to do with mocking WebTokenRequestResponse
        public async Task ATS_AccountWithWamId_Async()
        {
            // Arrange
            using (var harness = CreateTestHarness())
            {
                _webAccountProviderFactory.ClearReceivedCalls();

                var wamAccountProvider = new WebAccountProvider("id", "user@contoso.com", null);
                var requestParams = harness.CreateAuthenticationRequestParameters(TestConstants.AuthorityHomeTenant); // AAD
                var webAccount = new WebAccount(wamAccountProvider, "user@contoso.com", WebAccountState.Connected);
                var webTokenRequest = new WebTokenRequest(wamAccountProvider);
                var webTokenResponse = new WebTokenResponse();

                _wamProxy.FindAccountAsync(Arg.Any<WebAccountProvider>(), "wam_id_1").Returns(Task.FromResult(webAccount));
                _aadPlugin.CreateWebTokenRequestAsync(
                    wamAccountProvider,
                    requestParams,
                    isForceLoginPrompt: false,
                    isAccountInWam: true,
                    isInteractive: false)
                    .Returns(Task.FromResult(webTokenRequest));

                requestParams.Account = new Account(
                    $"{TestConstants.Uid}.{TestConstants.Utid}",
                    TestConstants.DisplayableId,
                    null,
                    new Dictionary<string, string>() { { TestConstants.ClientId, "wam_id_1" } }); // account has wam_id!

                var atsParams = new AcquireTokenSilentParameters();
                _webAccountProviderFactory.GetAccountProviderAsync(null).ReturnsForAnyArgs(Task.FromResult(wamAccountProvider));
                

                // Act
                await _wamBroker.AcquireTokenSilentAsync(requestParams, atsParams).ConfigureAwait(false);

                // Assert 

            }
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



        private static MsalTokenResponse CreateMsalTokenResponseFromWam(string wamAccountId)
        {
            return new MsalTokenResponse
            {
                IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                AccessToken = "access-token",
                ClientInfo = MockHelpers.CreateClientInfo(),
                ExpiresIn = 3599,
                CorrelationId = "correlation-id",
                RefreshToken = null, // brokers don't return RT
                Scope = TestConstants.s_scope.AsSingleString(),
                TokenType = "Bearer",
                WamAccountId = wamAccountId,
            };
        }
    }
}

