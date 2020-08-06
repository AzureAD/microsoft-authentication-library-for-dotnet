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
    internal sealed class RegionDiscoveryProvider : IRegionDiscoveryProvider
    {
        private const string RegionName = "REGION_NAME";
        private readonly Uri _ImdsUri;
        private IDictionary<string, string> Headers;
        private readonly ICoreLogger _logger;
        private readonly IHttpManager _httpManager;

        public RegionDiscoveryProvider(IHttpManager httpManager, ICoreLogger logger = null)
        {
            _httpManager = httpManager;
            _ImdsUri = new Uri("http://169.254.169.254/metadata/instance/compute/api-version=2019-06-01");
            Headers = new Dictionary<string, string>();
            Headers.Add("Metadata", "true");
            _logger = logger ?? new NullLogger();

        }

        public async Task<string> getRegionAsync()
        {
            if (!Environment.GetEnvironmentVariable(RegionName).IsNullOrEmpty())
            {
                _logger.Info($"[Region discovery] Region: {Environment.GetEnvironmentVariable(RegionName)}");
                return Environment.GetEnvironmentVariable(RegionName);
            }

            try
            {
                HttpResponse response = await _httpManager.SendGetAsync(_ImdsUri, Headers, _logger).ConfigureAwait(false);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new MsalClientException(
                        MsalError.RegionDiscoveryFailed,
                        MsalErrorMessage.RegionDiscoveryFailed);
                }

                LocalImdsResponse localImdsResponse = JsonHelper.DeserializeFromJson<LocalImdsResponse>(response.Body);

                _logger.Info($"[Region discovery] Call to local IMDS returned region: {localImdsResponse.location}");
                return localImdsResponse.location;
            }
            catch (Exception e)
            {
                _logger.Info("[Region discovery] Call to local imds failed." + e.Message);
                throw;
            }
        }

    }
}
