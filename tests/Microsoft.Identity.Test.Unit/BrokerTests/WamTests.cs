#if DESKTOP

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Platforms.Features.WamBroker;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.BrokerTests
{
    [TestClass]
    public class WamTests : TestBase
    {
        CoreUIParent _coreUIParent;
        ICoreLogger _logger;
        IWamPlugin _aadPlugin;
        IWamPlugin _msaPlugin;
        IWamProxy _wamProxy;
        WamBroker _wamBroker;

        [TestInitialize]
        public void Init()
        {
            _coreUIParent = new CoreUIParent();
            _logger = NSubstitute.Substitute.For<ICoreLogger>();
            _aadPlugin = NSubstitute.Substitute.For<IWamPlugin>();
            _msaPlugin = NSubstitute.Substitute.For<IWamPlugin>();
            _wamProxy = NSubstitute.Substitute.For<IWamProxy>();
            _wamBroker = new WamBroker(_coreUIParent, _logger, _aadPlugin, _msaPlugin, _wamProxy);
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
        public async Task GetAccounts_Merges_AAD_and_MSA_Accounts_Async()
        {
            var aadAccount1 = new Account("aad.1", "aad1", "login.windows.net");
            var aadAccount2 = new Account("aad.2", "aad2", "login.mac.net");
            IEnumerable<IAccount> aadAccounts = new List<IAccount>() { aadAccount1, aadAccount2 };

            var msaAccount1 = new Account("msa.2", "msa2", "env1");
            var msaAccount2 = new Account("msa.2", "msa2", "env2");
            IEnumerable<IAccount> msaAccounts = new List<IAccount>() { msaAccount1, msaAccount2 };

            _aadPlugin.GetAccountsAsync(TestConstants.ClientId).Returns(Task.FromResult(aadAccounts));
            _msaPlugin.GetAccountsAsync(TestConstants.ClientId).Returns(Task.FromResult(msaAccounts));

            IEnumerable<IAccount> accounts = await _wamBroker.GetAccountsAsync(TestConstants.ClientId, "http://some.url").ConfigureAwait(false);
            CollectionAssert.AreEquivalent((accounts as List<IAccount>), aadAccounts.Concat(msaAccounts) as List<IAccount>);
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
#endif

