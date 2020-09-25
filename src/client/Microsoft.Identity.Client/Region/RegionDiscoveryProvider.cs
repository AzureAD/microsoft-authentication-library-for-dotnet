using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Region
{
    internal sealed class RegionDiscoveryProvider : IRegionDiscoveryProvider
    {
        private const string RegionName = "REGION_NAME";
        private readonly Uri _ImdsUri;
        private IDictionary<string, string> Headers;
        private readonly IHttpManager _httpManager;
        private static INetworkCacheMetadataProvider _networkCacheMetadataProvider;

        public RegionDiscoveryProvider(IHttpManager httpManager, INetworkCacheMetadataProvider networkCacheMetadataProvider = null)
        {
            _httpManager = httpManager;

            // For information of the current api-version refer: https://docs.microsoft.com/en-us/azure/virtual-machines/windows/instance-metadata-service#versioning
            _ImdsUri = new Uri("http://169.254.169.254/metadata/instance/compute/api-version=2020-06-01");

            Headers = new Dictionary<string, string>();
            Headers.Add("Metadata", "true");
            _networkCacheMetadataProvider = networkCacheMetadataProvider ?? new NetworkCacheMetadataProvider();
        }

        public async Task<InstanceDiscoveryMetadataEntry> GetMetadataAsync(Uri authority, RequestContext requestContext)
        {
            ICoreLogger logger = requestContext.Logger;
            string environment = authority.Host;
            InstanceDiscoveryMetadataEntry cachedEntry = _networkCacheMetadataProvider.GetMetadata(environment, logger);
            
            if (cachedEntry == null)
            {
                Uri regionalizedAuthority = await BuildAuthorityWithRegionAsync(authority, requestContext.Logger).ConfigureAwait(false);
                CacheInstanceDiscoveryMetadata(CreateEntry(authority, regionalizedAuthority));

                cachedEntry = _networkCacheMetadataProvider.GetMetadata(environment, logger);
                logger.Verbose($"[Region Discovery] Created metadata for the regional environment {environment} ? {cachedEntry != null}");
            } else
            {
                logger.Verbose($"[Region Discovery] The network provider found an entry for {environment}");
            }

            requestContext.ApiEvent.RegionDiscovered = cachedEntry.PreferredNetwork.Split('.')[0];
            return cachedEntry;
        }


        private async Task<string> GetRegionAsync(ICoreLogger logger)
        {
            if (!Environment.GetEnvironmentVariable(RegionName).IsNullOrEmpty())
            {
                logger.Info($"[Region discovery] Region: {Environment.GetEnvironmentVariable(RegionName)}");
                return Environment.GetEnvironmentVariable(RegionName);
            }

            try
            {
                HttpResponse response = await _httpManager.SendGetAsync(_ImdsUri, Headers, logger).ConfigureAwait(false);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new MsalClientException(
                        MsalError.RegionDiscoveryFailed,
                        MsalErrorMessage.RegionDiscoveryFailed);
                }

                LocalImdsResponse localImdsResponse = JsonHelper.DeserializeFromJson<LocalImdsResponse>(response.Body);

                logger.Info($"[Region discovery] Call to local IMDS returned region: {localImdsResponse.location}");
                return localImdsResponse.location;
            }
            catch (MsalClientException)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.Info("[Region discovery] Call to local imds failed." + e.Message);
                throw new MsalClientException(MsalError.RegionDiscoveryFailed, MsalErrorMessage.RegionDiscoveryFailed);
            }
        }

        private static InstanceDiscoveryMetadataEntry CreateEntry(Uri orginalAuthority, Uri regionalizedAuthority)
        {
            return new InstanceDiscoveryMetadataEntry()
            {
                Aliases = new[] { orginalAuthority.Host, regionalizedAuthority.Host },
                PreferredCache = orginalAuthority.Host,
                PreferredNetwork = regionalizedAuthority.Host
            };
        }

        private void CacheInstanceDiscoveryMetadata(InstanceDiscoveryMetadataEntry metadataEntry)
        {
            foreach (string aliasedEnvironment in metadataEntry.Aliases ?? Enumerable.Empty<string>())
            {
                _networkCacheMetadataProvider.AddMetadata(aliasedEnvironment, metadataEntry);
            }
        }

        private async Task<Uri> BuildAuthorityWithRegionAsync(Uri canonicalAuthority, ICoreLogger logger)
        {
            string region = await GetRegionAsync(logger).ConfigureAwait(false);
            var builder = new UriBuilder(canonicalAuthority);

            if (KnownMetadataProvider.IsPublicEnvironment(canonicalAuthority.Host))
            {
                builder.Host = $"{region}.login.microsoft.com";
            } 
            else
            {
                builder.Host = $"{region}.{builder.Host}";
            }
            
            return builder.Uri;
        }
    }
}
