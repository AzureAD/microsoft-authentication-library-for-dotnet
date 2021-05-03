using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.CacheImpl;
using Microsoft.Identity.Client.Internal.Logger;

namespace WebApi.Controllers
{
    /// <summary>
    /// Initialize a CCA per request -> no caching
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class NoCacheController : ControllerBase
    {
        private readonly ILogger<SingletonController> _logger;
        private static Random s_random = new Random();
        

        public NoCacheController(ILogger<SingletonController> logger)
        {
            _logger = logger;
        }
      

        [HttpGet]
#pragma warning disable UseAsyncSuffix // Use Async suffix
        public async Task<long> Get()
#pragma warning restore UseAsyncSuffix // Use Async suffix
        {
            var tid = $"tid{s_random.Next(Settings.NumberOfTenants)}";
            bool cacheHit = s_random.NextDouble() <= Settings.CacheHitRatio;

            ParallelRequestMockHandler httpManager = new ParallelRequestMockHandler();

            var cca = ConfidentialClientApplicationBuilder
                .Create("d3adb33f-c0de-ed0c-c0de-deadb33fc0d3")
                .WithAuthority($"https://login.microsoftonline.com/{tid}")
                .WithHttpManager(httpManager)
                .WithClientSecret("secret")
                .BuildConcrete();


            var res = await cca.AcquireTokenForClient(new[] { "scope" })
                 .WithForceRefresh(!cacheHit)
                 .ExecuteAsync().ConfigureAwait(false);

            return res.AuthenticationResultMetadata.DurationTotalInMs;
        }
    }
}
