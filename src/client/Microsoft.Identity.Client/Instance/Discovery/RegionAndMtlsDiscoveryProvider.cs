// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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

        // Map of unsupported sovereign cloud hosts for mTLS PoP to their error messages
        private static readonly Dictionary<string, string> s_unsupportedMtlsHosts =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "login.usgovcloudapi.net", MsalErrorMessage.MtlsPopNotSupportedForUsGovCloudApiMessage },
                { "login.chinacloudapi.cn", MsalErrorMessage.MtlsPopNotSupportedForChinaCloudApiMessage }
            };

        public RegionAndMtlsDiscoveryProvider(IHttpManager httpManager)
        {
            _regionManager = new RegionManager(httpManager);
        }

        public async Task<InstanceDiscoveryMetadataEntry> GetMetadataAsync(Uri authority, RequestContext requestContext)
        {
            // Fail fast: Check for unsupported mTLS hosts before any region discovery
            if (requestContext.IsMtlsRequested)
            {
                string host = authority.Host;

                // Check if host is in the unsupported list
                if (s_unsupportedMtlsHosts.TryGetValue(host, out string errorMessage))
                {
                    requestContext.Logger.Error($"[Region discovery] mTLS PoP is not supported for host: {host}");
                    throw new MsalClientException(
                        MsalError.MtlsPopNotSupportedForEnvironment,
                        errorMessage);
                }
            }

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
                    // mTLS PoP is supported on global endpoints. Transform the authority host so that
                    // the HTTP request goes to the mTLS endpoint (mtlsauth.*) instead of the regular
                    // login endpoint. This is required for the mutual-TLS handshake to take place.
                    //   Public:     login.microsoftonline.com  → mtlsauth.microsoft.com
                    //   Gov:        login.microsoftonline.us   → mtlsauth.microsoftonline.us
                    //   China:      login.partner.microsoftonline.cn → mtlsauth.partner.microsoftonline.cn
                    //   Sovereign:  login.sovcloud-identity.xx → mtlsauth.sovcloud-identity.xx
                    string originalHost = authority.Host;
                    string mtlsHost = originalHost;

                    if (KnownMetadataProvider.IsPublicEnvironment(originalHost))
                    {
                        // Public cloud has a dedicated global mTLS hostname.
                        mtlsHost = PublicEnvForRegionalMtlsAuth;
                    }
                    else if (originalHost.StartsWith("login.", StringComparison.OrdinalIgnoreCase))
                    {
                        mtlsHost = "mtlsauth." + originalHost.Substring("login.".Length);
                    }

                    requestContext.Logger.Info($"[Region discovery] Region not available for mTLS PoP; using global mTLS endpoint: {mtlsHost}");
                    return CreateEntry(originalHost, mtlsHost);
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
