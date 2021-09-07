using System;
using System.IO;
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
    public class PrototypeController : ControllerBase
    {
        private const string FileCachePath = @"c:\temp\cache";

        static PrototypeController()
        {
            if (Directory.Exists(FileCachePath))
                Directory.Delete(FileCachePath, true);
            Directory.CreateDirectory(FileCachePath);
        }

        public PrototypeController(ILogger<PrototypeController> logger)
        {
        }


        public enum TID
        {
            A,
            B
        }

        public enum Scope
        {
            S1,
            S2
        }

        static FilePartionedCacheSerializer s_l2 =
            new FilePartionedCacheSerializer(FileCachePath);



        [HttpGet]
#pragma warning disable UseAsyncSuffix // Use Async suffix
        public async Task<string> Get(
            bool ccaPerRequest = true, 
            TID tenantId = TID.A, 
            Scope scope = Scope.S1, 
            bool staticL1 = true, 
            bool useL2 = false)
            
#pragma warning restore UseAsyncSuffix // Use Async suffix
        {

            char c = tenantId.ToString().ToCharArray()[0];
            var tid = new string(Enumerable.Repeat(c, 16).ToArray());

            ConfidentialClientApplication cca = CreateCCA(ccaPerRequest, staticL1, useL2);

          
            var res = await cca.AcquireTokenForClient(new[] { scope.ToString() })                
                 .WithAuthority($"https://login.microsoftonline.com/{tid}")
                 .ExecuteAsync().ConfigureAwait(false);


            if (!string.Equals(res.AccessToken ,tid, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("failed");
            }

            return res.AuthenticationResultMetadata.TokenSource.ToString();
        }

        private static ConfidentialClientApplication s_staticCCA = null;
        private static object sync_obj = new object();

        private static ConfidentialClientApplication CreateCCA(bool ccaPerRequest, bool staticL1, bool useL2)
        {
            if (ccaPerRequest)
            {
                S2SParallelRequestMockHandler httpManager = new S2SParallelRequestMockHandler();

                ConfidentialClientApplication cca = ConfidentialClientApplicationBuilder
                    .Create("d3adb33f-c0de-ed0c-c0de-deadb33fc0d3")
                    .WithHttpManager(httpManager)
                    .WithInternalMemoryTokenCacheOptions(new InternalMemoryTokenCacheOptions() { UseSharedCache = true })
                    .WithClientSecret("secret")
                    .BuildConcrete();

                if (useL2)
                {
                    s_l2.Initialize((cca.AppTokenCache as TokenCache));
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
                            s_staticCCA = CreateCCA(true, staticL1, useL2);
                    }
                }
                return s_staticCCA;
            }
        }
    }
}

// Test
// 1 CCA per app + non-static L1 [OK, but no chaching] -> this is what we want to prevent
// 1 CCA per app + static L1
// 1 CCA per app + no static L1 + L2
// singleton CCA + static L1 
// signelton CCA + non-static L1 [OK]
// singleton CCA + non-static L1 + L2
// singleton CCA + static L1 + L2
