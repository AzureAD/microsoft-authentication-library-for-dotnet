// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    internal class InstanceDiscoveryManager : IInstanceDiscoveryManager
    {
        // TODO: move caching to a different class
        private static readonly ConcurrentDictionary<string, InstanceDiscoveryMetadataEntry> s_cache =
            new ConcurrentDictionary<string, InstanceDiscoveryMetadataEntry>();

        private readonly IHttpManager _httpManager;
        private readonly ITelemetryManager _telemetryManager;

        public InstanceDiscoveryManager(IHttpManager httpManager, ITelemetryManager telemetryManager, bool shouldClearCaches)
        {
            _httpManager = httpManager ?? throw new ArgumentNullException(nameof(httpManager));
            _telemetryManager = telemetryManager ?? throw new ArgumentNullException(nameof(telemetryManager));

            if (shouldClearCaches)
                s_cache.Clear();
        }

        public async Task<InstanceDiscoveryMetadataEntry> GetMetadataEntryAsync(Uri authority, RequestContext requestContext)
        {
            AuthorityType type = Authority.GetAuthorityType(authority.AbsoluteUri);

            switch (type)
            {
                case AuthorityType.Aad:
                    bool foundInCache = s_cache.TryGetValue(authority.Host, out InstanceDiscoveryMetadataEntry entry);

                    if (!foundInCache)
                    {
                        await DoInstanceDiscoveryAndCacheAsync(authority, requestContext).ConfigureAwait(false);
                        s_cache.TryGetValue(authority.Host, out entry);
                    }

                    return entry ?? CreateEntryForSingleAuthority(authority);

                // ADFS and B2C do not support instance discovery 
                case AuthorityType.Adfs:
                case AuthorityType.B2C:

                    return CreateEntryForSingleAuthority(authority);

                default:
                    throw new InvalidOperationException("Unexpected authority type " + type);
            }
        }

        private static InstanceDiscoveryMetadataEntry CreateEntryForSingleAuthority(Uri authority)
        {
            return new InstanceDiscoveryMetadataEntry()
            {
                Aliases = new[] { authority.Host },
                PreferredCache = authority.Host,
                PreferredNetwork = authority.Host
            };
        }

        private async Task<InstanceDiscoveryResponse> DoInstanceDiscoveryAndCacheAsync(
          Uri authority,
          RequestContext requestContext)
        {
            InstanceDiscoveryResponse discoveryResponse = await SendInstanceDiscoveryRequestAsync(authority, requestContext).ConfigureAwait(false);
            CacheInstanceDiscoveryMetadata(discoveryResponse);
            return discoveryResponse;
        }

        private async Task<InstanceDiscoveryResponse> SendInstanceDiscoveryRequestAsync(
          Uri authority,
          RequestContext requestContext)
        {
            var client = new OAuth2Client(requestContext.Logger, _httpManager, _telemetryManager);

            client.AddQueryParameter("api-version", "1.1");
            client.AddQueryParameter("authorization_endpoint", BuildAuthorizeEndpoint(authority.Host, GetTenant(authority)));

            string discoveryHost = AadAuthority.IsInTrustedHostList(authority.Host)
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
            return uri.AbsolutePath.Split('/')[1];
        }

        public static string BuildInstanceDiscoveryEndpoint(string host)
        {
            return string.Format(CultureInfo.InvariantCulture, "https://{0}/common/discovery/instance", host);
        }

        private void CacheInstanceDiscoveryMetadata(InstanceDiscoveryResponse instanceDiscoveryResponse)
        {
            foreach (InstanceDiscoveryMetadataEntry entry in instanceDiscoveryResponse.Metadata ?? Enumerable.Empty<InstanceDiscoveryMetadataEntry>())
            {
                foreach (string aliasedAuthority in entry.Aliases ?? Enumerable.Empty<string>())
                {
                    s_cache.TryAdd(aliasedAuthority, entry);
                }
            }
        }

        // TODO bogavril - refactor this
        public bool AddTestValue(string host, InstanceDiscoveryMetadataEntry instanceDiscoveryMetadataEntry)
        {
            return s_cache.TryAdd(host, instanceDiscoveryMetadataEntry);
        }
    }
}
