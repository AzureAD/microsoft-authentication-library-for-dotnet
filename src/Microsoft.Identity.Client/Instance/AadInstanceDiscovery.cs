// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Instance
{
    internal class AadInstanceDiscovery : IAadInstanceDiscovery
    {
        // TODO: The goal of creating this class was to remove statics, but for the time being
        // we don't have a good separation to cache these across ClientApplication instances
        // in the case where a ConfidentialClientApplication is created per-request, for example.
        // So moving this back to static to keep the existing behavior but the rest of the code
        // won't know this is static.
        private static readonly ConcurrentDictionary<string, InstanceDiscoveryMetadataEntry> s_cache =
            new ConcurrentDictionary<string, InstanceDiscoveryMetadataEntry>();

        private readonly ICoreLogger _logger;
        private readonly IHttpManager _httpManager;
        private readonly ITelemetryManager _telemetryManager;

        public AadInstanceDiscovery(ICoreLogger logger, IHttpManager httpManager, ITelemetryManager telemetryManager, bool shouldClearCache = true)
        {
            _logger = logger;
            _httpManager = httpManager;
            _telemetryManager = telemetryManager;
            if (shouldClearCache)
            {
                s_cache.Clear();
            }
        }

        public bool TryGetValue(string host, out InstanceDiscoveryMetadataEntry instanceDiscoveryMetadataEntry)
        {
            return s_cache.TryGetValue(host, out instanceDiscoveryMetadataEntry);
        }

        public async Task<InstanceDiscoveryMetadataEntry> GetMetadataEntryAsync(
            Uri authority,
            RequestContext requestContext)
        {
            bool foundInCache = TryGetValue(authority.Host, out var entry);
            TraceWrapper.WriteLine("GetMetadataEntryAsync - response from cache? " + foundInCache);

            if (!foundInCache)
            {
                await DoInstanceDiscoveryAndCacheAsync(authority, requestContext).ConfigureAwait(false);
                TryGetValue(authority.Host, out entry);
            }

            return entry;
        }

        public async Task<InstanceDiscoveryResponse> DoInstanceDiscoveryAndCacheAsync(
            Uri authority,
            RequestContext requestContext)
        {
            var discoveryResponse = await SendInstanceDiscoveryRequestAsync(authority, requestContext).ConfigureAwait(false);
            CacheInstanceDiscoveryMetadata(authority.Host, discoveryResponse);
            return discoveryResponse;
        }

        public bool TryAddValue(string host, InstanceDiscoveryMetadataEntry instanceDiscoveryMetadataEntry)
        {
            return s_cache.TryAdd(host, instanceDiscoveryMetadataEntry);
        }

        public static string BuildAuthorizeEndpoint(string host, string tenant)
        {
            return string.Format(CultureInfo.InvariantCulture, "https://{0}/{1}/oauth2/v2.0/authorize", host, tenant);
        }

        private static string GetTenant(Uri uri)
        {
            return uri.AbsolutePath.Split('/')[1];
        }

        public static string BuildInstanceDiscoveryEndpoint(string host)
        {
            return string.Format(CultureInfo.InvariantCulture, "https://{0}/common/discovery/instance", host);
        }

        private async Task<InstanceDiscoveryResponse> SendInstanceDiscoveryRequestAsync(
            Uri authority,
            RequestContext requestContext)
        {
            var client = new OAuth2Client(_logger, _httpManager, _telemetryManager);
            client.AddQueryParameter("api-version", "1.1");
            client.AddQueryParameter("authorization_endpoint", BuildAuthorizeEndpoint(authority.Host, GetTenant(authority)));

            string discoveryHost = AadAuthority.IsInTrustedHostList(authority.Host)
                                       ? authority.Host
                                       : AadAuthority.DefaultTrustedHost;

            string instanceDiscoveryEndpoint = BuildInstanceDiscoveryEndpoint(discoveryHost);

            var discoveryResponse = await client.DiscoverAadInstanceAsync(new Uri(instanceDiscoveryEndpoint), requestContext)
                                                .ConfigureAwait(false);

            return discoveryResponse;
        }

        private void CacheInstanceDiscoveryMetadata(string host, InstanceDiscoveryResponse instanceDiscoveryResponse)
        {
            foreach (var entry in instanceDiscoveryResponse.Metadata ?? Enumerable.Empty<InstanceDiscoveryMetadataEntry>())
            {
                foreach (string aliasedAuthority in entry.Aliases ?? Enumerable.Empty<string>())
                {
                    TryAddValue(aliasedAuthority, entry);
                }
            }

            TryAddValue(
                host,
                new InstanceDiscoveryMetadataEntry
                {
                    PreferredNetwork = host,
                    PreferredCache = host,
                    Aliases = null
                });
        }
    }
}
