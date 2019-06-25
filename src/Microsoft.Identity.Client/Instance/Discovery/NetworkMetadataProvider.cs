// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    internal class NetworkMetadataProvider : INetworkMetadataProvider
    {
        private readonly IHttpManager _httpManager;
        private readonly ITelemetryManager _telemetryManager;

        public NetworkMetadataProvider(IHttpManager httpManager, ITelemetryManager telemetryManager)
        {
            _httpManager = httpManager;
            _telemetryManager = telemetryManager;
        }

        public async Task<InstanceDiscoveryResponse> FetchAllDiscoveryMetadataAsync(
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
            client.AddQueryParameter("authorization_endpoint", BuildAuthorizeEndpoint(authority.Host, GetTenant(authority)));

            string discoveryHost = KnownMetadataProvider.IsKnownEnvironment(authority.Host)
                                       ? authority.Host
                                       : AadAuthority.DefaultTrustedHost;

            string instanceDiscoveryEndpoint = BuildInstanceDiscoveryEndpoint(discoveryHost);

            InstanceDiscoveryResponse discoveryResponse = await client.DiscoverAadInstanceAsync(new Uri(instanceDiscoveryEndpoint), requestContext)
                                                .ConfigureAwait(false);

            return discoveryResponse;
        }

        private static string BuildAuthorizeEndpoint(string host, string tenant)
        {
            return string.Format(CultureInfo.InvariantCulture, "https://{0}/{1}/oauth2/v2.0/authorize", host, tenant);
        }

        private static string GetTenant(Uri uri)
        {
            // AAD specific
            return uri.AbsolutePath.Split('/')[1];
        }

        private static string BuildInstanceDiscoveryEndpoint(string host)
        {
            return string.Format(CultureInfo.InvariantCulture, "https://{0}/common/discovery/instance", host);
        }
    }
}
