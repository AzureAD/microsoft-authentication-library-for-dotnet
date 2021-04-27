// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.RequestsTests
{
    [TestClass]
    public class ParallelRequestsTests : TestBase
    {

        public const int NetworkAccessPenaltyMs = 50;
        private const double CacheHitRatio = 0.8;
        private const int NumberOfTenants = 5000;
        private const int ConcurrencyLevel = 10;

        private const int NumberOfRequets = 50;

        static Random s_random = new Random();


        private async Task<string> S2SAsync()
        {
            var taskId = Guid.NewGuid();
            var tid = $"tid{s_random.Next(NumberOfTenants)}";
            bool cacheHit = s_random.NextDouble() <= CacheHitRatio;

            Trace.WriteLine($"[{taskId}] Starting S2SAsync for tid: {tid} cachehit: {cacheHit}");

            //return cca.AcquireTokenForClient(new[] { "scope" })
            //     .WithAuthority($"https://login.microsoft.com/{tid}")
            //     .WithForceRefresh(cacheHit)
            //     .ExecuteAsync();
            await Task.Delay(100).ConfigureAwait(false);

            Trace.WriteLine($"[{taskId}] Completing S2SAsync for tid: {tid} cachehit: {cacheHit}");

            return taskId.ToString();

        }

        // time spent in lib
        // time spent in cache
        // time spent in HTTP

        private static ConfidentialClientApplication s_cca;

        [TestMethod]
        public async Task AcquireTokenSilent_ValidATs_ParallelRequests_Async()
        {
            // Arrange
          

            ParallelRequestMockHanler httpManager = new ParallelRequestMockHanler();

            s_cca = ConfidentialClientApplicationBuilder
                    .Create(TestConstants.ClientId)
                    .WithHttpManager(httpManager)
                    .WithClientSecret("secret")
                    .BuildConcrete();

            var schedulerPair = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default,
                            maxConcurrencyLevel: ConcurrencyLevel);
            TaskScheduler scheduler = schedulerPair.ConcurrentScheduler;
            TaskFactory taskFactory = new TaskFactory(scheduler);

            List<Task> tasksInProgress = new List<Task>();
            for (int i=0; i< NumberOfRequets; i++)
            {
                Task t = taskFactory.StartNew(() => S2SAsync());
                
                tasksInProgress.Add(t);
            }


            Trace.WriteLine("Ok, time to wait for these to stop");
            await Task.WhenAll(tasksInProgress).ConfigureAwait(false);
        }

     

   
    }

    /// <summary>
    /// This custom HttpManager does the following: 
    /// - provides a standard reponse for discovery calls
    /// - responds with valid tokens based on a naming convention (uid = "uid" + rtSecret, upn = "user_" + rtSecret)
    /// </summary>
    internal class ParallelRequestMockHanler : IHttpManager
    {
        public long LastRequestTime => 0;

        public async Task<HttpResponse> SendGetAsync(Uri endpoint, IDictionary<string, string> headers, ICoreLogger logger, bool retry = true, CancellationToken cancellationToken = default)
        {
            // simulate delay and also add complexity due to thread context switch
            await Task.Delay(ParallelRequestsTests.NetworkAccessPenaltyMs).ConfigureAwait(false);

            if (endpoint.AbsoluteUri.StartsWith("https://login.microsoftonline.com/common/discovery/instance?api-version=1.1"))
            {
                return new HttpResponse()
                {
                    Body = TestConstants.DiscoveryJsonResponse,
                    StatusCode = System.Net.HttpStatusCode.OK
                };
            }           

            Assert.Fail("Only instance discovery is supported");
            return null;
        }

        public async Task<HttpResponse> SendPostAsync(Uri endpoint, IDictionary<string, string> headers, IDictionary<string, string> bodyParameters, ICoreLogger logger, CancellationToken cancellationToken = default)
        {
            await Task.Delay(ParallelRequestsTests.NetworkAccessPenaltyMs).ConfigureAwait(false);

            if (endpoint.AbsoluteUri.Equals("https://login.microsoftonline.com/my-utid/oauth2/v2.0/token"))
            {
                bodyParameters.TryGetValue(OAuth2Parameter.RefreshToken, out string rtSecret);

                return new HttpResponse()
                {
                   // Body = GetTokenResponseForRt(rtSecret),
                    StatusCode = System.Net.HttpStatusCode.OK
                };
            }

            
            return null;
        }

  

        public Task<HttpResponse> SendPostAsync(Uri endpoint, IDictionary<string, string> headers, HttpContent body, ICoreLogger logger, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponse> SendPostForceResponseAsync(Uri uri, Dictionary<string, string> headers, StringContent body, ICoreLogger logger, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
