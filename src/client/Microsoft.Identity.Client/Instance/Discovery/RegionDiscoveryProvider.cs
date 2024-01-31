// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Region
{
    internal class RegionDiscoveryProvider : IRegionDiscoveryProvider
    {
        private readonly IRegionManager _regionManager;
        public const string PublicEnvForRegional = "login.microsoft.com";

        public RegionDiscoveryProvider(IHttpManager httpManager, bool clearCache)
        {
            _regionManager = new RegionManager(httpManager, shouldClearStaticCache: clearCache);
        }

        public async Task<InstanceDiscoveryMetadataEntry> GetMetadataAsync(Uri authority, RequestContext requestContext)
        {
            string region = null;
            if (requestContext.ApiEvent?.ApiId == TelemetryCore.Internal.Events.ApiEvent.ApiIds.AcquireTokenForClient)
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

            string regionalEnv = GetRegionalizedEnvironment(authority, region, requestContext);
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

        private static string GetRegionalizedEnvironment(Uri authority, string region, RequestContext requestContext)
        {

            string host = authority.Host;

            if (KnownMetadataProvider.IsPublicEnvironment(host))
            {
                requestContext.Logger.Info(() => $"[Region discovery] Regionalized Environment is : {region}.{PublicEnvForRegional}. ");
                return $"{region}.{PublicEnvForRegional}";
            }

            // Regional business rule - use the PreferredNetwork value for public and sovereign clouds
            // but do not do instance discovery for it - rely on cached values only
            if (KnownMetadataProvider.TryGetKnownEnviromentPreferredNetwork(host, out var preferredNetworkEnv))
            {
                host = preferredNetworkEnv;
            }

            requestContext.Logger.Info(() => $"[Region discovery] Regionalized Environment is : {region}.{host}. ");
            return $"{region}.{host}";
        }
    }
}
