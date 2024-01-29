// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    internal class NetworkMetadataProvider : INetworkMetadataProvider
    {
        private readonly IHttpManager _httpManager;
        private readonly INetworkCacheMetadataProvider _networkCacheMetadataProvider;
        private readonly Uri _userProvidedInstanceDiscoveryUri; // can be null

        public NetworkMetadataProvider(
            IHttpManager httpManager,
            INetworkCacheMetadataProvider networkCacheMetadataProvider, 
            Uri userProvidedInstanceDiscoveryUri = null)
        {
            _httpManager = httpManager ?? throw new ArgumentNullException(nameof(httpManager));
            _networkCacheMetadataProvider = networkCacheMetadataProvider ?? throw new ArgumentNullException(nameof(networkCacheMetadataProvider));
            _userProvidedInstanceDiscoveryUri = userProvidedInstanceDiscoveryUri; // can be null
        }

        public async Task<InstanceDiscoveryMetadataEntry> GetMetadataAsync(Uri authority, RequestContext requestContext)
        {
            var logger = requestContext.Logger;

            string environment = authority.Host;
            var cachedEntry = _networkCacheMetadataProvider.GetMetadata(environment, logger);
            if (cachedEntry != null)
            {
                logger.Verbose(() => $"[Instance Discovery] The network provider found an entry for {environment}. ");
                return cachedEntry;
            }

            var discoveryResponse = await FetchAllDiscoveryMetadataAsync(authority, requestContext).ConfigureAwait(false);
            CacheInstanceDiscoveryMetadata(discoveryResponse);

            cachedEntry = _networkCacheMetadataProvider.GetMetadata(environment, logger);
            logger.Verbose(() => $"[Instance Discovery] After hitting the discovery endpoint, the network provider found an entry for {environment} ? {cachedEntry != null}. ");

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
            var client = new OAuth2Client(requestContext.Logger, _httpManager, mtlsCertificate: null);

            client.AddQueryParameter("api-version", "1.1");
            client.AddQueryParameter("authorization_endpoint", BuildAuthorizeEndpoint(authority));

            Uri instanceDiscoveryEndpoint = ComputeHttpEndpoint(authority, requestContext);

            InstanceDiscoveryResponse discoveryResponse = await client
                .DiscoverAadInstanceAsync(instanceDiscoveryEndpoint, requestContext)
                .ConfigureAwait(false);

            return discoveryResponse;
        }

        private Uri ComputeHttpEndpoint(Uri authority, RequestContext requestContext)
        {
            if (_userProvidedInstanceDiscoveryUri != null)
            {
                return _userProvidedInstanceDiscoveryUri;
            }

            string discoveryHost = KnownMetadataProvider.IsKnownEnvironment(authority.Host) ?
                authority.Host :
                AadAuthority.DefaultTrustedHost;

            string instanceDiscoveryEndpoint =  UriBuilderExtensions.GetHttpsUriWithOptionalPort(
                $"https://{discoveryHost}/common/discovery/instance",
                authority.Port);

            requestContext.Logger.InfoPii(
                () => $"Fetching instance discovery from the network from host {discoveryHost}. Endpoint {instanceDiscoveryEndpoint}. ",
                () => $"Fetching instance discovery from the network from host {discoveryHost}. ");

            return new Uri(instanceDiscoveryEndpoint);
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

    }
}
