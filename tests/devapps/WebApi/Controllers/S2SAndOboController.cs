using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache.CacheImpl;
using Microsoft.Identity.Client.Internal.Logger;
using WebApi.Misc;
using WebApi.MockHttp;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class S2SAndOboController : ControllerBase
    {
        private const string FileCachePath = @"c:\temp\cache";
        private readonly IServiceProvider _provider;
        private readonly ILogger<S2SAndOboController> _logger;
        private static MockHttpClientFactory s_mockHttpClientFactory = new MockHttpClientFactory();
        private Random s_random = new Random();
        static S2SAndOboController()
        {
            if (Directory.Exists(FileCachePath))
                Directory.Delete(FileCachePath, true);
            Directory.CreateDirectory(FileCachePath);
        }

        public S2SAndOboController(IServiceProvider provider, ILogger<S2SAndOboController> logger)
        {
            _provider = provider;
            _logger = logger;
        }

        public enum TID
        {
            A,
            B
        }

        public enum Flow
        {
            S2S,
            OBO
        }

        public enum Scope
        {
            S1,
            S2
        }

        // L2 caching solution used
        static FilePartionedCacheSerializer s_l2 =
            new FilePartionedCacheSerializer(FileCachePath);

        [HttpGet]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "<Pending>")]
        public async Task<string> Get(
            bool ccaPerRequest = true,            
            Flow flow = Flow.S2S,
            int userForObo = 1,
            TID tenantId = TID.A,
            Scope scope = Scope.S1,
            bool staticL1 = false,
            bool useL2 = true)
        {
            

            char c = tenantId.ToString().ToCharArray()[0];
            var tid = new string(Enumerable.Repeat(c, 16).ToArray());

            IConfidentialClientApplication cca = GetOrCreateCCA(ccaPerRequest, staticL1, useL2);

            AuthenticationResult res;
            if (flow == Flow.S2S)
            {
                res = await cca.AcquireTokenForClient(new[] { scope.ToString() })
                     .WithAuthority($"https://login.microsoftonline.com/{tid}")
                     .ExecuteAsync().ConfigureAwait(false);
            }
            else
            {
                
                var user = $"user_{userForObo}";

                // must be in this format. MSAL will use {user} as the object id 
                string fakeUpstreamToken = $"upstream_token_{user}";
                
                res = await cca.AcquireTokenOnBehalfOf(new[] { scope.ToString() }, new UserAssertion(fakeUpstreamToken))                    
                    .WithAuthority($"https://login.microsoftonline.com/{tid}")
                    .ExecuteAsync()
                    .ConfigureAwait(false);
            }

           

            return res.AuthenticationResultMetadata.TokenSource.ToString();
        }

        private static IConfidentialClientApplication s_staticCCA = null;
        private static object sync_obj = new object();

        private IConfidentialClientApplication GetOrCreateCCA(bool ccaPerRequest, bool staticL1, bool useL2)
        {
            if (ccaPerRequest)
            {
                var ccaBuilder = ConfidentialClientApplicationBuilder
                    .Create("d3adb33f-c0de-ed0c-c0de-deadb33fc0d3")
                    .WithHttpClientFactory(s_mockHttpClientFactory)
                    .WithClientSecret("secret");

                if (staticL1)
                {
                    ccaBuilder.WithCacheOptions(CacheOptions.EnableSharedCacheOptions);
                }

                var cca = ccaBuilder.Build();

                if (useL2)
                {
                    s_l2.Initialize(cca.AppTokenCache);
                    s_l2.Initialize(cca.UserTokenCache);
                }

                return cca;
            }
            else
            {

                if (s_staticCCA == null)
                {
                    lock (sync_obj)
                    {
                        if (s_staticCCA == null)
                            s_staticCCA = GetOrCreateCCA(true, staticL1, useL2);
                    }
                }
                return s_staticCCA;
            }
        }
    }
}

