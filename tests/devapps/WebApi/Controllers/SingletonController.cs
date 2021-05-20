using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SingletonController : ControllerBase
    {
        private readonly ILogger<SingletonController> _logger;
        private static Random s_random = new Random();
        private static Lazy<ConfidentialClientApplication> s_ccaCreator = new Lazy<ConfidentialClientApplication>(CreateCCA);

        public SingletonController(ILogger<SingletonController> logger)
        {
            _logger = logger;
        }

        private static ConfidentialClientApplication CreateCCA()
        {
            S2SParallelRequestMockHandler httpManager = new S2SParallelRequestMockHandler();

            return ConfidentialClientApplicationBuilder
                .Create("d3adb33f-c0de-ed0c-c0de-deadb33fc0d3")
                .WithHttpManager(httpManager)
                .WithClientSecret("secret")
                .BuildConcrete();
        }

        [HttpGet]
#pragma warning disable UseAsyncSuffix // Use Async suffix
        public async Task<long> Get()
#pragma warning restore UseAsyncSuffix // Use Async suffix
        {
            var tid = $"tid{s_random.Next(Settings.NumberOfTenants)}";
            bool cacheHit = s_random.NextDouble() <= Settings.CacheHitRatio;


            var cca = s_ccaCreator.Value;

            var res = await cca.AcquireTokenForClient(new[] { "scope" })
                 .WithAuthority($"https://login.microsoftonline.com/{tid}")
                 .WithForceRefresh(!cacheHit)
                 .ExecuteAsync().ConfigureAwait(false);

            return res.AuthenticationResultMetadata.DurationTotalInMs;
        }
    }
}
