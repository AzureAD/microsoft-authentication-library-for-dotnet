// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Region
{
    internal class RegionDiscoveryProvider : IRegionDiscoveryProvider
    {
        private readonly IRegionManager _regionManager;

        public RegionDiscoveryProvider(IHttpManager httpManager, bool clearCache)
        {
            _regionManager = new RegionManager(httpManager, shouldClearStaticCache: clearCache);
        }

        public async Task<InstanceDiscoveryMetadataEntry> GetMetadataAsync(Uri authority, RequestContext requestContext)
        {
            string region = await _regionManager.GetAzureRegionAsync(requestContext).ConfigureAwait(false);
            if (string.IsNullOrEmpty(region))
            {
                requestContext.Logger.Info("Azure region was not configured or could not be discovered. Not using a regional authority.");
                return null;
            }

            

            string regionalEnv = GetRegionalizedEnviroment(authority, region);
            return CreateEntry(authority.Host, regionalEnv);
        }

        private static InstanceDiscoveryMetadataEntry CreateEntry(string originalEnv, string regionalEnv)
        {
            return new InstanceDiscoveryMetadataEntry()
            {
                Aliases = new[] { regionalEnv, originalEnv },
                PreferredCache = originalEnv,
                PreferredNetwork = regionalEnv
            };
        }

        private string GetRegionalizedEnviroment(Uri authority, string region)
        {
            string host = authority.Host;

            // Regional business rule - use the PrefferedNetwork value for public and soverign clouds
            // but do not do instance discovery for it - rely on cached values only
            if (KnownMetadataProvider.TryGetKnownEnviromentPrefferedNetwork(host, out var preferredNetworkEnv))
            {
                host = preferredNetworkEnv;
            }

            return $"{region}.{host}";
        }
    }
}
