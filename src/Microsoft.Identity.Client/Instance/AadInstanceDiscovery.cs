// ------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// ------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore;

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
            if (!TryGetValue(authority.Host, out var entry))
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
