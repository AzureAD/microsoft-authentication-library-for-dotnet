using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.CacheImpl;
using Microsoft.Identity.Client.Internal.Logger;
using WebApi.MockHttp;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FlatCacheController : ControllerBase
    {
        private readonly ILogger<FlatCacheController> _logger;
        private static Random s_random = new Random();
        private static InMemoryTokenCache s_flatTokenCache = new InMemoryTokenCache();
        private static MockHttpClientFactory s_mockHttpClientFactory = new MockHttpClientFactory();

        public FlatCacheController(ILogger<FlatCacheController> logger)
        {
            _logger = logger;
        }

        public const double CacheHitRatio = 0.95;


        [HttpGet]
#pragma warning disable UseAsyncSuffix // Use Async suffix
        public async Task<TokenSource> Get()
#pragma warning restore UseAsyncSuffix // Use Async suffix
        {
            var tid = $"tid{s_random.Next(Settings.NumberOfTenants)}";
            bool cacheHit = s_random.NextDouble() <= CacheHitRatio;

            var cca = ConfidentialClientApplicationBuilder
                .Create("d3adb33f-c0de-ed0c-c0de-deadb33fc0d3")
                .WithAuthority($"https://login.microsoftonline.com/{tid}")
                .WithHttpClientFactory(s_mockHttpClientFactory)
                .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
                .WithClientSecret("secret")
                .Build();

            //s_flatTokenCache.Bind(cca.AppTokenCache as TokenCache);

            var res = await cca.AcquireTokenForClient(new[] { "scope" })
                 .WithForceRefresh(!cacheHit)
                 .ExecuteAsync().ConfigureAwait(false);

            return res.AuthenticationResultMetadata.TokenSource;
        }
    }
}
