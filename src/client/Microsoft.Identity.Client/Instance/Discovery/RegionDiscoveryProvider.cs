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
        public const string PublicEnvForRegional = "r.login.microsoftonline.com";

        public RegionDiscoveryProvider(IHttpManager httpManager, bool clearCache)
        {
            _regionManager = new RegionManager(httpManager, shouldClearStaticCache: clearCache);
        }

        public async Task<InstanceDiscoveryMetadataEntry> GetMetadataAsync(Uri authority, RequestContext requestContext)
        {
            string region = null;
            if (requestContext.ApiEvent?.ApiId != TelemetryCore.Internal.Events.ApiEvent.ApiIds.AcquireTokenOnBehalfOf)
            {
                region = await _regionManager.GetAzureRegionAsync(requestContext).ConfigureAwait(false);
            }

            if (string.IsNullOrEmpty(region))
            {
                requestContext.Logger.Info("[Region discovery] Not using a regional authority. ");
                return null;
            }

            // already regionalized
            if (authority.Host.StartsWith($"{region}."))
            {
                return CreateEntry(requestContext.ServiceBundle.Config.Authority.AuthorityInfo.Host, authority.Host);
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

            if (KnownMetadataProvider.IsPublicEnvironment(host))
            {
                return $"{region}.{PublicEnvForRegional}";
            }

            // Regional business rule - use the PreferredNetwork value for public and sovereign clouds
            // but do not do instance discovery for it - rely on cached values only
            if (KnownMetadataProvider.TryGetKnownEnviromentPreferredNetwork(host, out var preferredNetworkEnv))
            {
                host = preferredNetworkEnv;
            }

            return $"{region}.{host}";
        }
    }
}
