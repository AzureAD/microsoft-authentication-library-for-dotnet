//----------------------------------------------------------------------
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
//------------------------------------------------------------------------------

using Microsoft.Identity.Core.OAuth2;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Identity.Core.Instance
{
    internal class AadInstanceDiscovery
    {
        AadInstanceDiscovery(){}

        public static AadInstanceDiscovery Instance { get; } = new AadInstanceDiscovery();

        internal readonly ConcurrentDictionary<string, InstanceDiscoveryMetadataEntry> Cache =
            new ConcurrentDictionary<string, InstanceDiscoveryMetadataEntry>();

        public async Task<InstanceDiscoveryMetadataEntry> GetMetadataEntryAsync(
            CorePlatformInformationBase platformInformation, Uri authority, bool validateAuthority,
            RequestContext requestContext)
        {
            InstanceDiscoveryMetadataEntry entry = null;
            if (!Cache.TryGetValue(authority.Host, out entry))
            {
                await DoInstanceDiscoveryAndCacheAsync(platformInformation, authority, validateAuthority, requestContext).ConfigureAwait(false);
                Cache.TryGetValue(authority.Host, out entry);
            }

            return entry;
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

        internal async Task<InstanceDiscoveryResponse> 
            DoInstanceDiscoveryAndCacheAsync(CorePlatformInformationBase platformInformation, Uri authority, bool validateAuthority, RequestContext requestContext)
        {
            InstanceDiscoveryResponse discoveryResponse =
                await SendInstanceDiscoveryRequestAsync(platformInformation, authority, requestContext).ConfigureAwait(false);

            if (validateAuthority)
            {
                Validate(discoveryResponse);
            }

            CacheInstanceDiscoveryMetadata(authority.Host, discoveryResponse);

            return discoveryResponse;
        }
        private static async Task<InstanceDiscoveryResponse> SendInstanceDiscoveryRequestAsync(
            CorePlatformInformationBase platformInformation, 
            Uri authority, 
            RequestContext requestContext)
        {
            OAuth2Client client = new OAuth2Client(platformInformation);
            client.AddQueryParameter("api-version", "1.1");
            client.AddQueryParameter("authorization_endpoint", BuildAuthorizeEndpoint(authority.Host, GetTenant(authority)));

            var discoveryHost = AadAuthority.IsInTrustedHostList(authority.Host) ?
                authority.Host :
                AadAuthority.DefaultTrustedHost;

            string instanceDiscoveryEndpoint = BuildInstanceDiscoveryEndpoint(discoveryHost);

            InstanceDiscoveryResponse discoveryResponse =
                await
                    client.DiscoverAadInstanceAsync(new Uri(instanceDiscoveryEndpoint), requestContext)
                        .ConfigureAwait(false);

            return discoveryResponse;
        }

        private static void Validate(InstanceDiscoveryResponse instanceDiscoveryResponse)
        {
            if (instanceDiscoveryResponse.TenantDiscoveryEndpoint == null)
            {
                throw CoreExceptionFactory.Instance.GetClientException(instanceDiscoveryResponse.Error,
                     instanceDiscoveryResponse.ErrorDescription);
            }
        }

        private void CacheInstanceDiscoveryMetadata(string host, InstanceDiscoveryResponse instanceDiscoveryResponse)
        {
            foreach (var entry in instanceDiscoveryResponse?.Metadata ?? Enumerable.Empty<InstanceDiscoveryMetadataEntry>())
            {
                foreach (var aliasedAuthority in entry?.Aliases ?? Enumerable.Empty<string>())
                {
                    Cache.TryAdd(aliasedAuthority, entry);
                }
            }

            Cache.TryAdd(host, new InstanceDiscoveryMetadataEntry
                {
                    PreferredNetwork = host,
                    PreferredCache = host,
                    Aliases = null
                });
        }
    }
}
