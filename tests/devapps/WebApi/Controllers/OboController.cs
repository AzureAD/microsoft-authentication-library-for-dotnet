using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.CacheImpl;
using Microsoft.Identity.Client.Internal.Logger;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OboController : ControllerBase
    {
        public OboController()
        {

        }

        static TraceSource s_traceSource = new TraceSource("OBO.Test", SourceLevels.Verbose);
        static InMemoryPartitionedCacheSerializer s_inMemoryPartitionedCacheSerializer =
                 new InMemoryPartitionedCacheSerializer(new NullLogger(), cacheAccessPenaltyMs: Settings.CacheAccessPenaltyMs);

        Random _random = new Random();

        static OBOParallelRequestMockHandler s_httpManager = new OBOParallelRequestMockHandler(false);
        static OBOParallelRequestMockHandler s_httpManagerRefreshFlow = new OBOParallelRequestMockHandler(true);

        static StringBuilder sb2 = new StringBuilder();

        ConfidentialClientApplication s_cca = ConfidentialClientApplicationBuilder
                  .Create("d3adb33f-c0de-ed0c-c0de-deadb33fc0d3")
                  .WithAuthority($"https://login.microsoftonline.com/tid")
                  .WithHttpManager(s_httpManager)
                  .WithClientSecret("secret")
                  .WithLegacyCacheCompatibility(false)
                  .WithLogging((lvl, msg, pii) => sb2.AppendLine(msg), LogLevel.Verbose, true, false)
                  .BuildConcrete();

        [HttpGet]
#pragma warning disable UseAsyncSuffix // Use Async suffix
        public async Task<long> Get(bool refreshFlow)
#pragma warning restore UseAsyncSuffix // Use Async suffix
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Guid requestId = Guid.NewGuid();
            StringBuilder sb = new StringBuilder();
            
            ConfidentialClientApplication local_cca = ConfidentialClientApplicationBuilder
                    .Create("d3adb33f-c0de-ed0c-c0de-deadb33fc0d3")
                    .WithAuthority($"https://login.microsoftonline.com/tid")
                    .WithHttpManager(refreshFlow ? s_httpManagerRefreshFlow : s_httpManager)
                    .WithClientSecret("secret")
                    .WithLegacyCacheCompatibility(false)
                    .WithLogging((lvl, msg, pii) => sb.AppendLine(msg), LogLevel.Verbose, true, false)
                    .BuildConcrete();

            ConfidentialClientApplication cca = local_cca;

            var user = $"user_{_random.Next(refreshFlow ? Settings.NumberOfUsersRefreshFlow : Settings.NumberOfUsers)}";

            s_inMemoryPartitionedCacheSerializer.Initialize(cca.UserTokenCache as TokenCache);

            string fakeUpstreamToken = $"upstream_token_{user}";

            var res = await cca.AcquireTokenOnBehalfOf(new[] { "scope" }, new UserAssertion(fakeUpstreamToken))
                .WithCorrelationId(requestId)
                .ExecuteAsync()
                .ConfigureAwait(false);

            sw.Stop();

            TraceResult(res, user, sw.ElapsedMilliseconds);

            return res.AuthenticationResultMetadata.DurationTotalInMs;
        }

        private void TraceResult(AuthenticationResult res, string user, long sw)
        {
            string msg = $"MSAL8, " +
                $"{user}, " +
                $"{res.AuthenticationResultMetadata.TokenSource}, " +
                $"{sw}, " +
                $"{res.AuthenticationResultMetadata.DurationTotalInMs}, " +
                $"{res.AuthenticationResultMetadata.DurationInCacheInMs}, " +
                $"{res.AuthenticationResultMetadata.DurationInHttpInMs}, " +
                $"{res.CorrelationId} ";

            s_traceSource.TraceInformation(msg);
        }
    }
}
