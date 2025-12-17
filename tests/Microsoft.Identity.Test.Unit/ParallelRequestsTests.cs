// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.Identity.Test.Unit.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class ParallelRequestsTests : TestBase
    {
        private const int CacheAccessPenaltyMs = 50;
        public const int NetworkAccessPenaltyMs = 50;

        private string _inMemoryCache = "{}";
        private int _beforeAccessCalls = 0;
        private int _afterAccessCalls = 0;
        private int _afterAccessWriteCalls = 0;
        private int _beforeWriteCalls = 0;

        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            _inMemoryCache = "{}";
        }

        // regression test for  https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/5108
        [TestMethod]
        public async Task ExtraQP()
        {
            Dictionary<string, (string, bool)> extraQp = new()
              {
                  { "key1", ("1", false) },
                  { "key2", ("2", false) }
              };

            // Arrange
            const int NumberOfRequests = 20;

            ParallelRequestMockHandler httpManager = new ParallelRequestMockHandler();

            var cca = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority(TestConstants.AuthorityUtidTenant, true)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithHttpManager(httpManager)
                .Build();

            var tasks = new List<Task<AuthenticationResult>>();

            for (int i = 0; i < NumberOfRequests; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    return await cca.AcquireTokenForClient(TestConstants.s_scope)
                        .WithExtraQueryParameters(extraQp)
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                }));
            }

            // Wait for all tasks to complete
            AuthenticationResult[] results = await Task.WhenAll(tasks).ConfigureAwait(false);

            // Assert
            Assert.HasCount(NumberOfRequests, results);
        }

        [TestMethod]
        public async Task AcquireTokenForClient_ConcurrentTenantRequests_Test()
        {
            // Arrange
            const int NumberOfRequests = 1000;

            // Custom HTTP manager that counts the number of requests
            ParallelRequestMockHandler httpManager = new();

            var cca = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority("https://login.microsoftonline.com/common")
                .WithClientSecret(TestConstants.ClientSecret)
                .WithHttpManager(httpManager)
                .Build();

            var tasks = new List<Task<AuthenticationResult>>();

            for (int i = 0; i < NumberOfRequests; i++)
            {
                int tempI = i; // Capture the current value of i
                tasks.Add(Task.Run(async () =>
                {
                    string tid = $"tidtid_{tempI}";
                    AuthenticationResult res = await cca.AcquireTokenForClient(TestConstants.s_scope)
                        .WithTenantId(tid)
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.IsFalse(
                        string.IsNullOrEmpty(res.AuthenticationResultMetadata.TokenEndpoint),
                        "TokenEndpoint is null/empty!"
                    );
                    Assert.Contains(
tid,
                        res.AuthenticationResultMetadata.TokenEndpoint, "TokenEndpoint should contain the tenant ID."
                    );
                    Assert.AreEqual($"token_{tid}", res.AccessToken, "Access token did not match the expected value.");

                    return res;
                }));
            }

            // Wait for all tasks to complete
            AuthenticationResult[] results = await Task.WhenAll(tasks).ConfigureAwait(false);

            // Assert the total tasks
            Assert.HasCount(NumberOfRequests, results, "Number of AuthenticationResult objects does not match the number of requests.");
        }

        [TestMethod]
        public async Task AcquireTokenForClient_PerTenantCaching_Test()
        {
            const int NumberOfRequests = 5000;

            var httpManager = new ParallelRequestMockHandler();
            IConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithAuthority("https://login.microsoftonline.com/common")
                .WithClientSecret(TestConstants.ClientSecret)
                .WithHttpManager(httpManager)
                .Build();

            // First pass: tokens should come from the network
            var tasksFirstPass = new List<Task<AuthenticationResult>>();
            for (int i = 0; i < NumberOfRequests; i++)
            {
                int tempI = i; // Capture the current value of i
                string tid = $"tidtid_{tempI}";
                tasksFirstPass.Add(Task.Run(async () =>
                {
                    AuthenticationResult result = await cca
                        .AcquireTokenForClient(TestConstants.s_scope)
                        .WithTenantId(tid)
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.IsNotNull(result, $"First-pass result is null for TID '{tid}'.");
                    Assert.IsFalse(
                        string.IsNullOrEmpty(result.AccessToken),
                        $"First-pass access token is null/empty for TID '{tid}'.");
                    Assert.AreEqual(
                        $"token_{tid}",
                        result.AccessToken,
                        $"First-pass AccessToken mismatch for TID '{tid}'.");
                    Assert.Contains(
tid,
                        result.AuthenticationResultMetadata.TokenEndpoint, $"First-pass TokenEndpoint '{result.AuthenticationResultMetadata.TokenEndpoint}' does not contain TID '{tid}'.");

                    return result;
                }));
            }

            AuthenticationResult[] firstPassResults = await Task.WhenAll(tasksFirstPass).ConfigureAwait(false);
            int firstPassRequestsMade = httpManager.RequestsMade;

            // Second pass: tokens should come from the cache
            var tasksSecondPass = new List<Task<AuthenticationResult>>();
            for (int i = 0; i < NumberOfRequests; i++)
            {
                int tempI = i; // Capture the current value of i
                string tid = $"tidtid_{tempI}";
                tasksSecondPass.Add(Task.Run(async () =>
                {
                    AuthenticationResult result = await cca
                        .AcquireTokenForClient(TestConstants.s_scope)
                        .WithTenantId(tid)
                        .ExecuteAsync()
                        .ConfigureAwait(false);

                    Assert.IsNotNull(result, $"Second-pass result is null for TID '{tid}'.");
                    Assert.IsFalse(
                        string.IsNullOrEmpty(result.AccessToken),
                        $"Second-pass access token is null/empty for TID '{tid}'.");
                    Assert.AreEqual(
                        $"token_{tid}",
                        result.AccessToken,
                        $"Second-pass AccessToken mismatch for TID '{tid}'.");

                    return result;
                }));
            }

            AuthenticationResult[] secondPassResults = await Task.WhenAll(tasksSecondPass).ConfigureAwait(false);
            int totalRequestsMade = httpManager.RequestsMade;
            int secondPassRequestsMade = totalRequestsMade - firstPassRequestsMade;

            // Verifying no new network calls on the second pass if caching is working properly
            Assert.AreEqual(
                0,
                secondPassRequestsMade,
                $"Expected zero new requests in second pass, but found {secondPassRequestsMade}."
            );
        }

        [TestMethod]
        public async Task AcquireTokenSilent_ValidATs_ParallelRequests_Async()
        {
            // Arrange
            const int NumberOfRequests = 10;

            using (MockHttpAndServiceBundle harness = CreateTestHarness())
            {
                PublicClientApplication pca = PublicClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(harness.HttpManager)
                    .BuildConcrete();

                ConfigureCacheSerialization(pca);

                var actualUsers = new List<string>();
                for (int i = 1; i <= NumberOfRequests; i++)
                {
                    var user = GetUpn(i);
                    TokenCacheHelper.PopulateCache(pca.UserTokenCacheInternal.Accessor, GetUid(i), "utid", displayableId: user);
                    actualUsers.Add(user);
                }

                byte[] bytes = (pca.UserTokenCacheInternal as ITokenCacheSerializer).SerializeMsalV3();
                _inMemoryCache = Encoding.UTF8.GetString(bytes);

                pca.UserTokenCacheInternal.Accessor.AssertItemCount(
                   expectedAtCount: NumberOfRequests * 2,
                   expectedRtCount: NumberOfRequests,
                   expectedAccountCount: NumberOfRequests,
                   expectedIdtCount: NumberOfRequests,
                   expectedAppMetadataCount: 1);

                IEnumerable<IAccount> accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
                AuthenticationResult[] results;

                // Act
                using (new PerformanceValidator(
                    NumberOfRequests * (2 * CacheAccessPenaltyMs + 100),
                    "AcquireTokenSilent should take roughly 100ms for its internal logic, " +
                    "plus the time needed to access the cache and all the thread context switches"))
                {
                    // execute won't start here because IEnumerable is lazy
                    IEnumerable<Task<AuthenticationResult>> tasks = accounts.Select(acc =>
                        pca.AcquireTokenSilent(TestConstants.s_scope, acc).ExecuteAsync());

                    // execution starts here 
                    Task<AuthenticationResult>[] taskArray = tasks.ToArray();

                    // wait for all requests to complete
                    results = await Task.WhenAll(taskArray).ConfigureAwait(false);
                }

                // Assert
                CollectionAssert.AreEquivalent(
                    actualUsers,
                    results.Select(r => r.Account.Username).ToArray());

                // Expecting the number of cache accesses to be equal to the number of request plus one for GetAccounts
                Assert.AreEqual(NumberOfRequests + 1, _afterAccessCalls);
                Assert.AreEqual(NumberOfRequests + 1, _beforeAccessCalls);

                // Acquire token silent with valid ATs in the cache -> no data is written
                Assert.AreEqual(0, _beforeWriteCalls);
                Assert.AreEqual(0, _afterAccessWriteCalls);
            }
        }

        [TestMethod]
        public async Task AcquireTokenSilent_ExpiredATs_ParallelRequests_Async()
        {
            // Arrange
            const int NumberOfRequests = 10;

            // The typical HttpMockHandler used by other tests can't deal with parallel request
            ParallelRequestMockHandler httpManager = new ParallelRequestMockHandler();

            PublicClientApplication pca = PublicClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithHttpManager(httpManager)
                .BuildConcrete();

            ConfigureCacheSerialization(pca);

            var actualUsers = new List<string>();
            for (int i = 1; i <= NumberOfRequests; i++)
            {
                TokenCacheHelper.PopulateCache(
                    pca.UserTokenCacheInternal.Accessor,
                    GetUid(i),
                    TestConstants.Utid,
                    displayableId: GetUpn(i),
                    rtSecret: i.ToString(CultureInfo.InvariantCulture), // this will help create a valid response
                    expiredAccessTokens: true);
                actualUsers.Add(GetUpn(i));
            }

            byte[] bytes = (pca.UserTokenCacheInternal as ITokenCacheSerializer).SerializeMsalV3();
            _inMemoryCache = Encoding.UTF8.GetString(bytes);

            pca.UserTokenCacheInternal.Accessor.AssertItemCount(
               expectedAtCount: NumberOfRequests * 2,
               expectedRtCount: NumberOfRequests,
               expectedAccountCount: NumberOfRequests,
               expectedIdtCount: NumberOfRequests,
               expectedAppMetadataCount: 1);

            IEnumerable<IAccount> accounts = await pca.GetAccountsAsync().ConfigureAwait(false);
            AuthenticationResult[] results;

            // Act
            using (new PerformanceValidator(
                NumberOfRequests *
                    (2 * CacheAccessPenaltyMs + NetworkAccessPenaltyMs /* refresh the RT */ + 1000 /* internal logic */) +
                    2 * NetworkAccessPenaltyMs, /* one time discovery calls */
                "AcquireTokenSilent in parallel should take roughly 100ms for its internal logic, " +
                "plus the time needed to access the cache, the network, and all the thread context switches"))
            {
                // execute won't start here because IEnumerable is lazy
                IEnumerable<Task<AuthenticationResult>> tasks = accounts.Select(acc =>
                    pca.AcquireTokenSilent(TestConstants.s_scope, acc).ExecuteAsync());

                // execution starts here 
                Task<AuthenticationResult>[] taskArray = tasks.ToArray();

                // wait for all requests to complete
                results = await Task.WhenAll(taskArray).ConfigureAwait(false);
            }

            // Assert
            CollectionAssert.AreEquivalent(
                actualUsers,
                results.Select(r => r.Account.Username).ToArray());

            // Each task will write tokens
            Assert.AreEqual(NumberOfRequests, _beforeWriteCalls);
            Assert.AreEqual(NumberOfRequests, _afterAccessWriteCalls);

            // Expecting the number of cache accesses to be equal to twice the number of requests 
            // (one for the initial read access, one for when writing the tokens)
            // plus one for GetAccounts
            Assert.AreEqual(NumberOfRequests * 2 + 1, _afterAccessCalls);
            Assert.AreEqual(NumberOfRequests * 2 + 1, _beforeAccessCalls);
        }

        public static string GetUid(int rtSecret)
        {
            return "uid" + rtSecret;
        }

        public static string GetUpn(int rtSecret)
        {
            return "user_" + rtSecret;
        }

        private void ConfigureCacheSerialization(IPublicClientApplication pca)
        {
            pca.UserTokenCache.SetBeforeAccessAsync(async notificationArgs =>
            {
                // Introduce more complexity in the test by adding this context change
                // .Net will suspend the current thread untul
                await Task.Delay(CacheAccessPenaltyMs).ConfigureAwait(false);

                byte[] bytes = Encoding.UTF8.GetBytes(_inMemoryCache);
                notificationArgs.TokenCache.DeserializeMsalV3(bytes);
                _beforeAccessCalls++;
            });

            pca.UserTokenCache.SetAfterAccessAsync(async notificationArgs =>
            {
                await Task.Delay(50).ConfigureAwait(false);

                _afterAccessCalls++;
                if (notificationArgs.HasStateChanged)
                {
                    _afterAccessWriteCalls++;
                    byte[] bytes = notificationArgs.TokenCache.SerializeMsalV3();
                    _inMemoryCache = Encoding.UTF8.GetString(bytes);
                }
            });

            pca.UserTokenCache.SetBeforeWrite(_ =>
            {
                _beforeWriteCalls++;
            });
        }
    }
}
