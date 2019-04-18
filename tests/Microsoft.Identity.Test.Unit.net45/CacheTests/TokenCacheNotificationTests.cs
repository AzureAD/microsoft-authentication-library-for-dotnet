// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Common.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class TokenCacheNotificationTests
    {
        [TestMethod]
        public async Task TestSubscribeNonAsync()
        {
            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId).Build();

            bool beforeAccessCalled = false;
            bool afterAccessCalled = false;
            bool beforeWriteCalled = false;

            pca.UserTokenCache.SetBeforeAccess(args => { beforeAccessCalled = true; });
            pca.UserTokenCache.SetAfterAccess(args => { afterAccessCalled = true; });
            pca.UserTokenCache.SetBeforeWrite(args => { beforeWriteCalled = true; });

            await pca.GetAccountsAsync().ConfigureAwait(false);

            Assert.IsTrue(beforeAccessCalled);
            Assert.IsTrue(afterAccessCalled);
            Assert.IsFalse(beforeWriteCalled);
        }

        [TestMethod]
        public async Task TestSubscribeAsync()
        {
            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId).Build();

            bool beforeAccessCalled = false;
            bool afterAccessCalled = false;
            bool beforeWriteCalled = false;

            pca.UserTokenCache.SetAsyncBeforeAccess(async args => { beforeAccessCalled = true; await Task.Delay(10).ConfigureAwait(false); });
            pca.UserTokenCache.SetAsyncAfterAccess(async args => { afterAccessCalled = true; await Task.Delay(10).ConfigureAwait(false); });
            pca.UserTokenCache.SetAsyncBeforeWrite(async args => { beforeWriteCalled = true; await Task.Delay(10).ConfigureAwait(false); });

            await pca.GetAccountsAsync().ConfigureAwait(false);

            Assert.IsTrue(beforeAccessCalled);
            Assert.IsTrue(afterAccessCalled);
            Assert.IsFalse(beforeWriteCalled);
        }

        [TestMethod]
        public async Task TestSubscribeBothAsync()
        {
            var pca = PublicClientApplicationBuilder.Create(MsalTestConstants.ClientId).Build();

            bool beforeAccessCalled = false;
            bool afterAccessCalled = false;
            bool beforeWriteCalled = false;

            bool asyncBeforeAccessCalled = false;
            bool asyncAfterAccessCalled = false;
            bool asyncBeforeWriteCalled = false;

            // Sync method should be called _first_ (just by convention).  But let's validate this.

            pca.UserTokenCache.SetBeforeAccess(args => { beforeAccessCalled = true; });
            pca.UserTokenCache.SetAfterAccess(args => { afterAccessCalled = true; });
            pca.UserTokenCache.SetBeforeWrite(args => { beforeWriteCalled = true; });

            pca.UserTokenCache.SetAsyncBeforeAccess(async args => { asyncBeforeAccessCalled = beforeAccessCalled; await Task.Delay(10).ConfigureAwait(false); });
            pca.UserTokenCache.SetAsyncAfterAccess(async args => { asyncAfterAccessCalled = afterAccessCalled; await Task.Delay(10).ConfigureAwait(false); });
            pca.UserTokenCache.SetAsyncBeforeWrite(async args => { asyncBeforeWriteCalled = beforeWriteCalled; await Task.Delay(10).ConfigureAwait(false); });

            await pca.GetAccountsAsync().ConfigureAwait(false);

            Assert.IsTrue(asyncBeforeAccessCalled);
            Assert.IsTrue(asyncAfterAccessCalled);
            Assert.IsFalse(asyncBeforeWriteCalled);

            Assert.IsTrue(beforeAccessCalled);
            Assert.IsTrue(afterAccessCalled);
            Assert.IsFalse(beforeWriteCalled);
        }

        [TestMethod]
        public async Task TestSerializationViaAsync()
        {
            int numBeforeAccessCalls = 0;
            int numAfterAccessCalls = 0;
            int numBeforeWriteCalls = 0;

            byte[] serializedPayload = null;

            using (var harness = new MockHttpAndServiceBundle())
            {
                harness.HttpManager.AddInstanceDiscoveryMockHandler();

                PublicClientApplication pca = PublicClientApplicationBuilder
                    .Create(MsalTestConstants.ClientId)
                    .WithAuthority(new Uri(ClientApplicationBase.DefaultAuthority), true)
                    .WithHttpManager(harness.HttpManager)
                    .BuildConcrete();

                pca.UserTokenCache.SetAsyncBeforeAccess(async args =>
                {
                    numBeforeAccessCalls++;
                    await Task.Delay(10).ConfigureAwait(false);
                });
                pca.UserTokenCache.SetAsyncAfterAccess(async args =>
                {
                    numAfterAccessCalls++;
                    serializedPayload = args.TokenCache.SerializeMsalV3();
                    await Task.Delay(10).ConfigureAwait(false);
                });
                pca.UserTokenCache.SetAsyncBeforeWrite(async args =>
                {
                    numBeforeWriteCalls++;                    
                    await Task.Delay(10).ConfigureAwait(false);
                });

                MsalMockHelpers.ConfigureMockWebUI(
                    pca.ServiceBundle.PlatformProxy,
                    new AuthorizationResult(AuthorizationStatus.Success, pca.AppConfig.RedirectUri + "?code=some-code"));

                harness.HttpManager.AddMockHandlerForTenantEndpointDiscovery(MsalTestConstants.AuthorityCommonTenant);
                harness.HttpManager.AddSuccessTokenResponseMockHandlerForPost(MsalTestConstants.AuthorityCommonTenant);

                AuthenticationResult result = await pca
                    .AcquireTokenInteractive(MsalTestConstants.Scope)
                    .ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }

            Assert.AreEqual(1, numBeforeAccessCalls);
            Assert.AreEqual(1, numAfterAccessCalls);
            Assert.AreEqual(1, numBeforeWriteCalls);

            Assert.IsNotNull(serializedPayload);
        }
    }
}
