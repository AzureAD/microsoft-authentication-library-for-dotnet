// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute.Core;

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
            ParallelRequestMockHanler httpManager = new ParallelRequestMockHanler();

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

    /// <summary>
    /// This custom HttpManager does the following: 
    /// - provides a standard response for discovery calls
    /// - responds with valid tokens based on a naming convention (uid = "uid" + rtSecret, upn = "user_" + rtSecret)
    /// </summary>
    internal class ParallelRequestMockHanler : IHttpManager
    {
        public long LastRequestDurationInMs => 50;

        public Task<HttpResponse> SendGetAsync(Uri endpoint, IDictionary<string, string> headers, ILoggerAdapter logger, bool retry = true, CancellationToken cancellationToken = default)
        {
            Assert.Fail("Only instance discovery is supported");
            return Task.FromResult<HttpResponse>(null);
        }

        public async Task<HttpResponse> SendPostAsync(Uri endpoint, IDictionary<string, string> headers, IDictionary<string, string> bodyParameters, ILoggerAdapter logger, CancellationToken cancellationToken = default)
        {
            await Task.Delay(ParallelRequestsTests.NetworkAccessPenaltyMs).ConfigureAwait(false);

            if (endpoint.AbsoluteUri.Equals("https://login.microsoftonline.com/my-utid/oauth2/v2.0/token"))
            {
                bodyParameters.TryGetValue(OAuth2Parameter.RefreshToken, out string rtSecret);

                return new HttpResponse()
                {
                    Body = GetTokenResponseForRt(rtSecret),
                    StatusCode = System.Net.HttpStatusCode.OK
                };
            }

            Assert.Fail("Only refresh flow is supported");
            return null;
        }

        public async Task<HttpResponse> SendRequestAsync(
            Uri endpoint,
            Dictionary<string, string> headers,
            HttpContent body,
            HttpMethod method,
            ILoggerAdapter logger,
            bool doNotThrow,
            bool retry, 
            X509Certificate2 mtlsCertificate,
            CancellationToken cancellationToken)
        {
            // simulate delay and also add complexity due to thread context switch
            await Task.Delay(ParallelRequestsTests.NetworkAccessPenaltyMs).ConfigureAwait(false);

            if (HttpMethod.Get == method &&
                endpoint.AbsoluteUri.StartsWith("https://login.microsoftonline.com/common/discovery/instance?api-version=1.1"))
            {
                return new HttpResponse()
                {
                    Body = TestConstants.DiscoveryJsonResponse,
                    StatusCode = System.Net.HttpStatusCode.OK
                };
            }

            if (HttpMethod.Post == method && 
                endpoint.AbsoluteUri.Equals("https://login.microsoftonline.com/my-utid/oauth2/v2.0/token"))
            {
                var bodyString = (body as FormUrlEncodedContent).ReadAsStringAsync().GetAwaiter().GetResult();
                var bodyDict = bodyString.Replace("?", "").Split('&').ToDictionary(x => x.Split('=')[0], x => x.Split('=')[1]);

                bodyDict.TryGetValue(OAuth2Parameter.RefreshToken, out string rtSecret);

                return new HttpResponse()
                {
                    Body = GetTokenResponseForRt(rtSecret),
                    StatusCode = System.Net.HttpStatusCode.OK
                };
            }

            Assert.Fail("Test issue - this HttpRequest is not mocked");
            return null;
        }

        private string GetTokenResponseForRt(string rtSecret)
        {
            if (int.TryParse(rtSecret, out int i))
            {
                var upn = ParallelRequestsTests.GetUpn(i);
                var uid = ParallelRequestsTests.GetUid(i);
                HttpResponseMessage response = MockHelpers.CreateSuccessTokenResponseMessageWithUid(uid, TestConstants.Utid, upn);
                return response.Content.ReadAsStringAsync().Result;
            }

            Assert.Fail("Expecting the rt secret to be a number, to be able to craft a response");
            return null;
        }
    }
}
