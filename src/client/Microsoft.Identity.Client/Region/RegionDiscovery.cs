using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Region
{
    internal sealed class RegionDiscovery
    {
        private const string RegionName = "REGION_NAME";
        private Uri ImdsUri;
        private IDictionary<string, string> Headers;
        private ICoreLogger Logger;
        private static readonly object _lock = new object();
        private static RegionDiscovery Instance = null;
        private HttpManager HttpManager;

        private RegionDiscovery()
        {
            ImdsUri = new Uri("http://169.254.169.254/metadata/instance/compute");
            Headers = new Dictionary<string, string>();
            Headers.Add("api-version", "2019-06-01");
            Logger = new NullLogger();
            HttpManager = new HttpManager(new SimpleHttpClientFactory());
        }

        // For Unit tests only.
        public void setHttpManager(HttpManager httpManager)
        {
            HttpManager = httpManager;
        }

        internal static RegionDiscovery GetInstance
        {
            get
            {
                if (Instance == null)
                {
                    lock (_lock)
                    {
                        Instance = new RegionDiscovery();
                    }
                }

                return Instance;
            }
        }

        internal async Task<string> getRegionAsync()
        {
            if (!Environment.GetEnvironmentVariable(RegionName).IsNullOrEmpty())
            {
                Logger.Info($"[Region discovery] Region: {Environment.GetEnvironmentVariable(RegionName)}");
                return Environment.GetEnvironmentVariable(RegionName);
            }

            try
            {
                HttpResponse response = await HttpManager.SendGetAsync(ImdsUri, Headers, Logger).ConfigureAwait(false);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new MsalClientException(
                        MsalError.RegionDiscoveryFailed,
                        MsalErrorMessage.RegionDiscoveryFailed);
                }

                LocalImdsResponse localImdsResponse = JsonHelper.DeserializeFromJson<LocalImdsResponse>(response.Body);

                Logger.Info($"[Region discovery] Call to local IMDS returned region: {localImdsResponse.location}");
                return localImdsResponse.location;
            }
            catch (Exception e)
            {
                Logger.Info("[Region discovery] Call to local imds failed." + e.Message);
                throw;
            }
        }

    }
}
