// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class InternalCacheOptionsTests : TestBase
    {
        [TestMethod]
        public void OptionsAndExternalCacheAreExclusive()
        {
            var app =
                    ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                                                              .Build();

            AssertExclusivity(app.UserTokenCache);
            AssertExclusivity(app.AppTokenCache);

            var app2 =
                     ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                               .WithClientSecret(TestConstants.ClientSecret)                                                               
                                                               .Build();
            app2.AppTokenCache.SetCacheOptions(CacheOptions.EnableSharedCacheOptions);
            app2.UserTokenCache.SetCacheOptions(CacheOptions.EnableSharedCacheOptions);

            AssertExclusivity(app2.AppTokenCache);
            AssertExclusivity(app2.UserTokenCache);

            var app3 =
                     ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                               .WithClientSecret(TestConstants.ClientSecret)
                                                               .Build();
            app3.UserTokenCache.SetAfterAccess((_) => { });
            app3.AppTokenCache.SetBeforeAccess((_) => { });
            var ex = AssertException.Throws<MsalClientException>(() => app3.UserTokenCache.SetCacheOptions(CacheOptions.EnableSharedCacheOptions));
            Assert.AreEqual(MsalError.StaticCacheWithExternalSerialization, ex.ErrorCode);
            ex = AssertException.Throws<MsalClientException>(() => app3.AppTokenCache.SetCacheOptions(CacheOptions.EnableSharedCacheOptions));
            Assert.AreEqual(MsalError.StaticCacheWithExternalSerialization, ex.ErrorCode);

            void AssertExclusivity(ITokenCache tokenCache)
            {
                var ex = AssertException.Throws<MsalClientException>(() => tokenCache.SetAfterAccess((_) => { }));
                Assert.AreEqual(MsalError.StaticCacheWithExternalSerialization, ex.ErrorCode);
                ex = AssertException.Throws<MsalClientException>(() => tokenCache.SetBeforeAccess((_) => { }));
                Assert.AreEqual(MsalError.StaticCacheWithExternalSerialization, ex.ErrorCode);
                ex = AssertException.Throws<MsalClientException>(() => tokenCache.SetBeforeWrite((_) => { }));
                Assert.AreEqual(MsalError.StaticCacheWithExternalSerialization, ex.ErrorCode);

                ex = AssertException.Throws<MsalClientException>(() => tokenCache.SetBeforeAccessAsync((_) => Task.CompletedTask));
                Assert.AreEqual(MsalError.StaticCacheWithExternalSerialization, ex.ErrorCode);
                ex = AssertException.Throws<MsalClientException>(() => tokenCache.SetAfterAccessAsync((_) => Task.CompletedTask));
                Assert.AreEqual(MsalError.StaticCacheWithExternalSerialization, ex.ErrorCode);
                ex = AssertException.Throws<MsalClientException>(() => tokenCache.SetBeforeWriteAsync((_) => Task.CompletedTask));
                Assert.AreEqual(MsalError.StaticCacheWithExternalSerialization, ex.ErrorCode);

            }
        }

        [TestMethod]
        public async Task ClientCreds_StaticCache_Async()
        {
            using (var httpManager = new MockHttpManager())
            {
                httpManager.AddInstanceDiscoveryMockHandler();

                ConfidentialClientApplication app1 =
                    ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                        .WithClientSecret(TestConstants.ClientSecret)
                                                        .WithHttpManager(httpManager)
                                                        .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                                                        .BuildConcrete();

                ConfidentialClientApplication app2 =
                   ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                       .WithClientSecret(TestConstants.ClientSecret)
                                                       .WithHttpManager(httpManager)
                                                       .BuildConcrete();

                app2.AppTokenCache.SetCacheOptions(new CacheOptions(true));

                ConfidentialClientApplication app_withoutStaticCache =
                  ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                      .WithClientSecret(TestConstants.ClientSecret)
                                                      .WithHttpManager(httpManager)
                                                      .BuildConcrete();

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                await ClientCredsAcquireAndAssertTokenSourceAsync(app1, "S1", TokenSource.IdentityProvider).ConfigureAwait(false);

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                await ClientCredsAcquireAndAssertTokenSourceAsync(app1, "S2", TokenSource.IdentityProvider).ConfigureAwait(false);

                await ClientCredsAcquireAndAssertTokenSourceAsync(app2, "S1", TokenSource.Cache).ConfigureAwait(false);
                await ClientCredsAcquireAndAssertTokenSourceAsync(app2, "S2", TokenSource.Cache).ConfigureAwait(false);

                ConfidentialClientApplication app3 =
                     ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                         .WithClientSecret(TestConstants.ClientSecret)
                                                         .WithHttpManager(httpManager)
                                                         .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                                                         .BuildConcrete();

                await ClientCredsAcquireAndAssertTokenSourceAsync(app3, "S1", TokenSource.Cache).ConfigureAwait(false);
                await ClientCredsAcquireAndAssertTokenSourceAsync(app3, "S2", TokenSource.Cache).ConfigureAwait(false);

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                await ClientCredsAcquireAndAssertTokenSourceAsync(app_withoutStaticCache, "S1", TokenSource.IdentityProvider).ConfigureAwait(false);

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();
                await ClientCredsAcquireAndAssertTokenSourceAsync(app_withoutStaticCache, "S2", TokenSource.IdentityProvider).ConfigureAwait(false);

            }
        }

        [TestMethod]
        public async Task PublicClient_StaticCache_Async()
        {
            using (var harness = CreateTestHarness())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                var app1 = PublicClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithHttpManager(harness.HttpManager)
                    .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                    .BuildConcrete();

                app1.ServiceBundle.ConfigureMockWebUI();

                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(TestConstants.AuthorityCommonTenant);

                AuthenticationResult result = await app1
                    .AcquireTokenInteractive(TestConstants.s_scope)
                    .ExecuteAsync().ConfigureAwait(false);

                var accounts = await app1.GetAccountsAsync().ConfigureAwait(false);
                Assert.AreEqual(1, accounts.Count());
                result = await app1.AcquireTokenSilent(TestConstants.s_scope, accounts.Single()).ExecuteAsync().ConfigureAwait(false);

                var app2 = PublicClientApplicationBuilder
                   .Create(TestConstants.ClientId)
                   .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                   .WithHttpManager(harness.HttpManager)
                   .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                   .BuildConcrete();

                accounts = await app2.GetAccountsAsync().ConfigureAwait(false);
                Assert.AreEqual(1, accounts.Count());
                result = await app2.AcquireTokenSilent(TestConstants.s_scope, accounts.Single()).ExecuteAsync().ConfigureAwait(false);
            }
        }

        private async Task ClientCredsAcquireAndAssertTokenSourceAsync(IConfidentialClientApplication app, string scope, TokenSource expectedSource)
        {
            var result = await app.AcquireTokenForClient(new[] { scope })
                 .WithTenantId(TestConstants.Utid)
                 .ExecuteAsync().ConfigureAwait(false);
            Assert.AreEqual(
               expectedSource,
               result.AuthenticationResultMetadata.TokenSource);
        }
    }
}
