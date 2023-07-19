// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebApi.Misc;
using WebApi.MockHttp;

namespace Microsoft.Identity.Test.Unit.ParallelRequests
{
    [TestClass]
    public class SingletonCCATest : TestBase
    {
        [TestMethod]
        public async Task ReproAsync()
        {
            const int MaxUsers = 10;
            const int Requests = 100;

            DynamicHttpClientFactory fakeSTS = new DynamicHttpClientFactory();

            InMemoryPartitionedCacheSerializer inMemoryCache = new InMemoryPartitionedCacheSerializer();

            var singletonCca = ConfidentialClientApplicationBuilder
                .Create(TestConstants.ClientId)
                .WithClientSecret(TestConstants.ClientSecret)
                .WithHttpClientFactory(fakeSTS)
                .Build();

            inMemoryCache.Initialize(singletonCca.UserTokenCache);


            List<Task> tasks = new List<Task>();
            foreach (int i in Enumerable.Range(1, Requests))
            {
                Task<AuthenticationResult> t = Task.Run(
                    async () =>
                    {

                        int userForObo = (new Random()).Next(1, MaxUsers);
                        var user = $"user_{userForObo}"; // will use {user} as the object id 
                        string fakeUpstreamToken = $"upstream_token_{user}";

                        Trace.WriteLine($"Starting request {i} with {user}");

                        var res = await singletonCca.AcquireTokenOnBehalfOf(new[] { "R1" }, new UserAssertion(fakeUpstreamToken))
                            .WithAuthority($"https://login.microsoftonline.com/TID")
                            .ExecuteAsync().ConfigureAwait(false);

                        Trace.WriteLine($"Finished request {i} with {user}");

                        // TODO: some assertions

                        return res;
                    });
                
                    tasks.Add(t);

            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            foreach (var t in tasks)
            {
                Assert.AreEqual(TaskStatus.RanToCompletion, t.Status);
            }

        }
    }
}
