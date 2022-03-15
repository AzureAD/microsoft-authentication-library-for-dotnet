// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class CacheSyncronizationTests : TestBase
    {

        [TestMethod]
        [Timeout(2000)]
        public async Task DisableInternalSemaphore_Async()
        {
            await RunSemaphoreTestAsync(true).ConfigureAwait(false);
            await RunSemaphoreTestAsync(false).ConfigureAwait(false);
        }

        private async Task RunSemaphoreTestAsync(bool useCacheSyncronization)
        {
            using (var harness = base.CreateTestHarness())
            {
                MockHttpManager httpManager = harness.HttpManager;
                httpManager.AddInstanceDiscoveryMockHandler();

                ConfidentialClientApplication app =
                    ConfidentialClientApplicationBuilder.Create(TestConstants.ClientId)
                                                              .WithClientSecret(TestConstants.ClientSecret)
                                                              .WithAuthority(TestConstants.AuthorityUtidTenant)
                                                              .WithCacheSynchronization(useCacheSyncronization)
                                                              .WithHttpManager(httpManager)
                                                              .BuildConcrete();

                httpManager.AddMockHandlerSuccessfulClientCredentialTokenResponseMessage();

                BlockingCache inMemoryTokenCache = new BlockingCache();
                inMemoryTokenCache.Bind(app.AppTokenCache);

                // Seed the cache with a token
                var result = await app.AcquireTokenForClient(TestConstants.s_scope.ToArray())
                    .ExecuteAsync()
                    .ConfigureAwait(false);
                Assert.IsTrue(result.AuthenticationResultMetadata.TokenSource == TokenSource.IdentityProvider);

                var blockingTask = RunAsync(inMemoryTokenCache, app, true);
                var nonBlockingTask1 = RunAsync(inMemoryTokenCache, app, false);
                var nonBlockingTask2 = RunAsync(inMemoryTokenCache, app, false);

                int res = Task.WaitAny(new[] { blockingTask, nonBlockingTask1, nonBlockingTask2 }, 100);

                if (useCacheSyncronization)
                {
                    Assert.AreEqual(-1, res, "WaitAny should have timed out, all tasks are blocked when the first call is blocking");
                }
                else
                {
                    Assert.AreNotEqual(-1, res, "WaitAny should have NOT timed out, the 2 non-blocking tasks should be allowed to complete");
                    Assert.IsTrue(nonBlockingTask1.IsCompleted);
                    Assert.IsTrue(nonBlockingTask2.IsCompleted);
                    Assert.IsFalse(blockingTask.IsCompleted, "The blocking task should still be blocked");
                    Assert.IsTrue(nonBlockingTask1.Result.AuthenticationResultMetadata.TokenSource == TokenSource.Cache);
                    Assert.IsTrue(nonBlockingTask2.Result.AuthenticationResultMetadata.TokenSource == TokenSource.Cache);
                }
            }
        }

        private static async Task<AuthenticationResult> RunAsync(BlockingCache cache, IConfidentialClientApplication app, bool block = false)
        {
            cache.BlockAccess = block;
            return await app.AcquireTokenForClient(TestConstants.s_scope.ToArray()).ExecuteAsync().ConfigureAwait(false);
        }

        public class BlockingCache
        {
            private byte[] _cacheData;

            public bool BlockAccess { get; set; } = false;

            public async Task BeforeAccessNotificationAsync(TokenCacheNotificationArgs args)
            {
                if (BlockAccess)
                {
                    // completely blocked
                    await Task.Delay(10000).ConfigureAwait(false);
                    Assert.Fail("Test error - waiting too long in a test");
                }

                args.TokenCache.DeserializeMsalV3(_cacheData);
            }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            public async Task AfterAccessNotificationAsync(TokenCacheNotificationArgs args)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {

                // if the access operation resulted in a cache update
                if (args.HasStateChanged)
                {
                    _cacheData = args.TokenCache.SerializeMsalV3();
                }
            }

            public void Bind(ITokenCache tokenCache)
            {
                tokenCache.SetBeforeAccessAsync(BeforeAccessNotificationAsync);
                tokenCache.SetAfterAccessAsync(AfterAccessNotificationAsync);
            }

        }
    }
}
