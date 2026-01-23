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
    internal class RegionAndMtlsDiscoveryProvider : IRegionDiscoveryProvider
    {
        private readonly IRegionManager _regionManager;
        public const string PublicEnvForRegional = "login.microsoft.com";
        public const string PublicEnvForRegionalMtlsAuth = "mtlsauth.microsoft.com";

        public RegionAndMtlsDiscoveryProvider(IHttpManager httpManager)
        {
            _regionManager = new RegionManager(httpManager);
        }

        public async Task<InstanceDiscoveryMetadataEntry> GetMetadataAsync(Uri authority, RequestContext requestContext)
        {
            string region = null;
            bool isMtlsEnabled = requestContext.IsMtlsRequested;

            if (requestContext.ApiEvent?.ApiId == TelemetryCore.Internal.Events.ApiEvent.ApiIds.AcquireTokenForClient)
            {
                region = await _regionManager.GetAzureRegionAsync(requestContext).ConfigureAwait(false);
            }

            if (string.IsNullOrEmpty(region))
            {
                if (isMtlsEnabled)
                {
                    requestContext.Logger.Info("[Region discovery] Region discovery failed during mTLS Pop. ");

                    throw new MsalServiceException(
                        MsalError.RegionRequiredForMtlsPop,
                        MsalErrorMessage.RegionRequiredForMtlsPopMessage);
                }

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
                if (requestContext.IsMtlsRequested)
                {
                    requestContext.Logger.Info(() => $"[Region discovery] Using MTLS regional environment: {region}.{PublicEnvForRegionalMtlsAuth}");
                    return $"{region}.{PublicEnvForRegionalMtlsAuth}";
                }
                else
                {
                    requestContext.Logger.Info(() => $"[Region discovery] Regionalized Environment is : {region}.{PublicEnvForRegional}. ");
                    return $"{region}.{PublicEnvForRegional}";
                }
            }

            // Regional business rule - use the PreferredNetwork value for public and sovereign clouds
            // but do not do instance discovery for it - rely on cached values only
            if (KnownMetadataProvider.TryGetKnownEnviromentPreferredNetwork(host, out var preferredNetworkEnv))
            {
                host = preferredNetworkEnv;
            }

            if (requestContext.IsMtlsRequested)
            {
                // Modify the host to replace "login" with "mtlsauth" for mTLS scenarios
                if (host.StartsWith("login"))
                {
                    host = "mtlsauth" + host.Substring("login".Length);
                }
            }

            requestContext.Logger.Info(() => $"[Region discovery] Regionalized Environment is : {region}.{host}. ");
            return $"{region}.{host}";
        }
    }
}
