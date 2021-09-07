using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.CacheImpl;
using Microsoft.Identity.Client.Internal.Logger;
using WebApi.Misc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WilsonLruCacheController : ControllerBase
    {
        private readonly ILogger<SingletonController> _logger;
        private static Random s_random = new Random();


        public WilsonLruCacheController(ILogger<SingletonController> logger)
        {
            _logger = logger;
        }

        // size of cache
        // 1 token -> ~2200 bytes (assuming 1 simple scope + bearer token)
        // 975k tokens -> ~2GB
        private static EventBasedLRUCache<string, byte[]> s_wilsonCache =
            new EventBasedLRUCache<string, byte[]>(500 * 1000);
        private static MsalCacheBasedOnWilson s_msalCache = new MsalCacheBasedOnWilson(s_wilsonCache);



        [HttpGet]
#pragma warning disable UseAsyncSuffix // Use Async suffix
        public async Task<long> Get()
#pragma warning restore UseAsyncSuffix // Use Async suffix
        {
            var tid = $"tid{s_random.Next(Settings.NumberOfTenants)}";
            bool cacheHit = s_random.NextDouble() <= Settings.CacheHitRatio;

            S2SParallelRequestMockHandler httpManager = new S2SParallelRequestMockHandler();

            var cca = ConfidentialClientApplicationBuilder
                .Create("d3adb33f-c0de-ed0c-c0de-deadb33fc0d3")
                .WithAuthority($"https://login.microsoftonline.com/{tid}")
                .WithHttpManager(httpManager)
                .WithClientSecret("secret")
                .BuildConcrete();

            s_msalCache.Initialize(cca.AppTokenCache as TokenCache);


            var res = await cca.AcquireTokenForClient(new[] { "scope" })
                 .WithForceRefresh(!cacheHit)
                 .ExecuteAsync().ConfigureAwait(false);

            if (res.AccessToken != tid)
            {
                throw new InvalidOperationException("failed");
            }

            return res.AuthenticationResultMetadata.DurationTotalInMs;
        }
    }
}
