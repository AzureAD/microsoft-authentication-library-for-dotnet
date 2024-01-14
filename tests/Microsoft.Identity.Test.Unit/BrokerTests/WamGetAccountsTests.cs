// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Identity.Test.Unit.BrokerTests
{
    [TestClass]
    [TestCategory(TestCategories.Broker)]
    public class WamGetAccountsTests : TestBase
    {
#if SUPPORTS_BROKER
        [TestMethod]
        public async Task WAM_AccountIdWriteback_Async()
        {
            // Arrange
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                var mockBroker = Substitute.For<IBroker>();
                mockBroker.IsBrokerInstalledAndInvokable(AuthorityType.Aad).Returns(true);

                var msalTokenResponse = CreateMsalTokenResponseFromWam("wam1");
                mockBroker.AcquireTokenInteractiveAsync(null, null).ReturnsForAnyArgs(Task.FromResult(msalTokenResponse));

                var pca = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithExperimentalFeatures(true)
                    .WithTestBroker(mockBroker)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                pca.ServiceBundle.Config.BrokerCreatorFunc = (_, _, _) => mockBroker;

                // Act
                await pca.AcquireTokenInteractive(TestConstants.s_scope).ExecuteAsync().ConfigureAwait(false);
                var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);

                // Assert
                var wamAccountIds = (accounts.Single() as Account).WamAccountIds;
                Assert.AreEqual(1, wamAccountIds.Count);
                Assert.AreEqual("wam1", wamAccountIds[TestConstants.ClientId]);

                var pca2 = PublicClientApplicationBuilder.Create(TestConstants.ClientId2)
                    .WithExperimentalFeatures(true)
                    .WithTestBroker(mockBroker)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();
                
                pca2.ServiceBundle.Config.BrokerCreatorFunc = (_, _, _) => mockBroker;

                var accounts2 = await pca2.GetAccountsAsync().ConfigureAwait(false);
                Assert.IsFalse(accounts2.Any());
            }
        }
#endif

        [TestMethod]
        public async Task WAM_AccountIds_GetMerged_Async()
        {
            // Arrange
            using (var httpManager = new MockHttpManager())
            {
                var cache = new InMemoryTokenCache();
                httpManager.AddInstanceDiscoveryMockHandler();

                var mockBroker = Substitute.For<IBroker>();
                mockBroker.IsBrokerInstalledAndInvokable(AuthorityType.Aad).Returns(true);

                var msalTokenResponse1 = CreateMsalTokenResponseFromWam("wam1");
                var msalTokenResponse2 = CreateMsalTokenResponseFromWam("wam2");
                var msalTokenResponse3 = CreateMsalTokenResponseFromWam("wam3");

                // 2 apps must share the token cache, like FOCI apps, for this test to be interesting
                var pca1 = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithExperimentalFeatures(true)
                    .WithTestBroker(mockBroker)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                var pca2 = PublicClientApplicationBuilder.Create(TestConstants.ClientId2)
                    .WithExperimentalFeatures(true)
                    .WithTestBroker(mockBroker)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                cache.Bind(pca1.UserTokenCache);
                cache.Bind(pca2.UserTokenCache);

                pca1.ServiceBundle.Config.BrokerCreatorFunc = (_, _, _) => mockBroker;
                pca2.ServiceBundle.Config.BrokerCreatorFunc = (_, _, _) => mockBroker;

                // Act 
                mockBroker.AcquireTokenInteractiveAsync(null, null).ReturnsForAnyArgs(Task.FromResult(msalTokenResponse1));
                await pca1.AcquireTokenInteractive(TestConstants.s_scope).ExecuteAsync().ConfigureAwait(false);

                // this should override wam1 id
                mockBroker.AcquireTokenInteractiveAsync(null, null).ReturnsForAnyArgs(Task.FromResult(msalTokenResponse2));
                await pca1.AcquireTokenInteractive(TestConstants.s_scope).ExecuteAsync().ConfigureAwait(false);

                mockBroker.AcquireTokenInteractiveAsync(null, null).ReturnsForAnyArgs(Task.FromResult(msalTokenResponse3));
                await pca2.AcquireTokenInteractive(TestConstants.s_scope).ExecuteAsync().ConfigureAwait(false);

                var accounts1 = await pca1.GetAccountsAsync().ConfigureAwait(false);
                var accounts2 = await pca2.GetAccountsAsync().ConfigureAwait(false);

                // Assert
#if SUPPORTS_BROKER
                var wamAccountIds = (accounts1.Single() as Account).WamAccountIds;
                Assert.AreEqual(2, wamAccountIds.Count);
                Assert.AreEqual("wam2", wamAccountIds[TestConstants.ClientId]);
                Assert.AreEqual("wam3", wamAccountIds[TestConstants.ClientId2]);
                CoreAssert.AssertDictionariesAreEqual(wamAccountIds, (accounts2.Single() as Account).WamAccountIds, StringComparer.Ordinal);
#endif
            }
        }

        [TestMethod]
        public async Task GetAccounts_Returns_Both_WAM_And_Cache_Accounts_Async()
        {
            // Arrange
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();
                string commonAccId = $"{TestConstants.Uid}.{TestConstants.Utid}";
                Account brokerAccount1 = new Account(commonAccId, "commonAccount", "login.windows.net");
                Account brokerAccount2 = new Account("other.account", "brokerAcc2", "login.windows.net");
                IReadOnlyList<IAccount> brokerAccounts = new List<IAccount>() { brokerAccount1, brokerAccount2 };

                var mockBroker = Substitute.For<IBroker>();
                mockBroker.IsBrokerInstalledAndInvokable(AuthorityType.Aad).Returns(true);

                var msalTokenResponse = CreateMsalTokenResponseFromWam("wam_acc_id");
                mockBroker.AcquireTokenInteractiveAsync(null, null).ReturnsForAnyArgs(Task.FromResult(msalTokenResponse));
                mockBroker.GetAccountsAsync(null, null, null, null, null).ReturnsForAnyArgs(
                    Task.FromResult(brokerAccounts));

                var pca = PublicClientApplicationBuilder.Create(TestConstants.ClientId)
                    .WithExperimentalFeatures(true)
                    .WithTestBroker(mockBroker)
                    .WithHttpManager(httpManager)
                    .BuildConcrete();

                // Act
                await pca.AcquireTokenInteractive(TestConstants.s_scope).ExecuteAsync().ConfigureAwait(false);
                var accounts = await pca.GetAccountsAsync().ConfigureAwait(false);

                // Assert
                Assert.AreEqual(2, accounts.Count());
#if SUPPORTS_BROKER
                var wamAccountIds = (accounts.Single(acc => acc.HomeAccountId.Identifier == commonAccId) as Account).WamAccountIds;
                Assert.AreEqual(1, wamAccountIds.Count);
                Assert.AreEqual("wam_acc_id", wamAccountIds[TestConstants.ClientId]);
#endif
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

    public static class BrokerExt
    {
        internal static PublicClientApplicationBuilder WithTestBroker(this PublicClientApplicationBuilder pcaBuilder, IBroker mockBroker)
        {
            pcaBuilder.Config.BrokerCreatorFunc = (_, _, _) => mockBroker;
            pcaBuilder.Config.IsBrokerEnabled = true;

            return pcaBuilder;
        }
    }
}
