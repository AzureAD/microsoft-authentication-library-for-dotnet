// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    internal class NetworkMetadataProvider : INetworkMetadataProvider
    {
        private readonly IHttpManager _httpManager;
        private readonly ITelemetryManager _telemetryManager;
        private readonly INetworkCacheMetadataProvider _networkCacheMetadataProvider;

        public NetworkMetadataProvider(
            IHttpManager httpManager,
            ITelemetryManager telemetryManager,
            INetworkCacheMetadataProvider networkCacheMetadataProvider)
        {
            _httpManager = httpManager;
            _telemetryManager = telemetryManager;
            _networkCacheMetadataProvider = networkCacheMetadataProvider;
        }

        public async Task<InstanceDiscoveryMetadataEntry> GetMetadataAsync(Uri authority, RequestContext requestContext)
        {
            var logger = requestContext.Logger;

            string environment = authority.Host;
            var cachedEntry = _networkCacheMetadataProvider.GetMetadata(environment, logger);
            if (cachedEntry != null)
            {
                logger.Verbose($"[Instance Discovery] The network provider found an entry for {environment}");
                return cachedEntry;
            }

            var discoveryResponse = await FetchAllDiscoveryMetadataAsync(authority, requestContext).ConfigureAwait(false);
            CacheInstanceDiscoveryMetadata(discoveryResponse);

            cachedEntry = _networkCacheMetadataProvider.GetMetadata(environment, logger);
            logger.Verbose($"[Instance Discovery] After hitting the discovery endpoint, the network provider found an entry for {environment} ? {cachedEntry != null}");

            return cachedEntry;
        }

        private void CacheInstanceDiscoveryMetadata(InstanceDiscoveryResponse instanceDiscoveryResponse)
        {
            foreach (InstanceDiscoveryMetadataEntry entry in instanceDiscoveryResponse.Metadata ?? Enumerable.Empty<InstanceDiscoveryMetadataEntry>())
            {
                foreach (string aliasedEnvironment in entry.Aliases ?? Enumerable.Empty<string>())
                {
                    _networkCacheMetadataProvider.AddMetadata(aliasedEnvironment, entry);
                }
            }
        }

        private async Task<InstanceDiscoveryResponse> FetchAllDiscoveryMetadataAsync(
            Uri authority,
            RequestContext requestContext)
        {
            InstanceDiscoveryResponse discoveryResponse = await SendInstanceDiscoveryRequestAsync(authority, requestContext).ConfigureAwait(false);
            return discoveryResponse;
        }

        private async Task<InstanceDiscoveryResponse> SendInstanceDiscoveryRequestAsync(
          Uri authority,
          RequestContext requestContext)
        {
            var client = new OAuth2Client(requestContext.Logger, _httpManager, _telemetryManager);

            client.AddQueryParameter("api-version", "1.1");
            client.AddQueryParameter("authorization_endpoint", BuildAuthorizeEndpoint(authority));

            string discoveryHost = KnownMetadataProvider.IsKnownEnvironment(authority.Host) ?
                authority.Host :
                AadAuthority.DefaultTrustedHost;
            string instanceDiscoveryEndpoint = BuildInstanceDiscoveryEndpoint(discoveryHost, authority.Port);

            requestContext.Logger.InfoPii(
                $"Fetching instance discovery from the network from host {discoveryHost}. Endpoint {instanceDiscoveryEndpoint}",
                $"Fetching instance discovery from the network from host {discoveryHost}");

            InstanceDiscoveryResponse discoveryResponse = await client
                .DiscoverAadInstanceAsync(new Uri(instanceDiscoveryEndpoint), requestContext)
                .ConfigureAwait(false);

            return discoveryResponse;
        }

        private static string BuildAuthorizeEndpoint(Uri authority)
        {
            return UriBuilderExtensions.GetHttpsUriWithOptionalPort(authority.Host, GetTenant(authority), "oauth2/v2.0/authorize", authority.Port);
        }

        private static string GetTenant(Uri uri)
        {
            // AAD specific
            return uri.AbsolutePath.Split('/')[1];
        }

        private static string BuildInstanceDiscoveryEndpoint(string host, int port)
        {
            return UriBuilderExtensions.GetHttpsUriWithOptionalPort(string.Format(CultureInfo.InvariantCulture, "https://{0}/common/discovery/instance", host), port);
        }
    }
}
