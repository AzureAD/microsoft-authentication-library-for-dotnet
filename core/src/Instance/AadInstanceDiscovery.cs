using Microsoft.Identity.Client;
using Microsoft.Identity.Core.OAuth2;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Identity.Core.Instance
{
    internal class AadInstanceDiscovery
    {
        AadInstanceDiscovery(){}

        public static AadInstanceDiscovery Instance { get; } = new AadInstanceDiscovery();

        internal readonly ConcurrentDictionary<string, InstanceDiscoveryMetadataEntry> InstanceCache =
            new ConcurrentDictionary<string, InstanceDiscoveryMetadataEntry>();

        public async Task<InstanceDiscoveryMetadataEntry> GetMetadataEntryAsync(Uri authority, bool validateAuthority,
            RequestContext requestContext)
        {
            InstanceDiscoveryMetadataEntry entry = null;
            if (!InstanceCache.TryGetValue(authority.Host, out entry))
            {
                await DoInstanceDiscoveryAndCacheAsync(authority, validateAuthority, requestContext).ConfigureAwait(false);
                InstanceCache.TryGetValue(authority.Host, out entry);
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
            DoInstanceDiscoveryAndCacheAsync(Uri authority, bool validateAuthority, RequestContext requestContext)
        {
            InstanceDiscoveryResponse discoveryResponse =
                await SendInstanceDiscoveryRequestAsync(authority, requestContext).ConfigureAwait(false);

            if (validateAuthority)
            {
                Validate(discoveryResponse);
            }

            CacheInstanceDiscoveryMetadata(authority.Host, discoveryResponse);

            return discoveryResponse;
        }
        private static async Task<InstanceDiscoveryResponse> SendInstanceDiscoveryRequestAsync(Uri authority, RequestContext requestContext)
        {
            OAuth2Client client = new OAuth2Client();
            client.AddQueryParameter("api-version", "1.1");
            client.AddQueryParameter("authorization_endpoint", BuildAuthorizeEndpoint(authority.Host, GetTenant(authority)));

            var discoveryHost = AadAuthority.IsInTrustedHostList(authority.Host) ?
                authority.Host :
                AadAuthority.DefaultTrustedAuthority;

            string instanceDiscoveryEndpoint = BuildInstanceDiscoveryEndpoint(discoveryHost);

            InstanceDiscoveryResponse discoveryResponse =
                await
                    client.DiscoverAadInstance(new Uri(instanceDiscoveryEndpoint), requestContext)
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
                    InstanceCache.TryAdd(aliasedAuthority, entry);
                }
            }

            InstanceCache.TryAdd(host, new InstanceDiscoveryMetadataEntry
                {
                    PreferredNetwork = host,
                    PreferredCache = host,
                    Aliases = null
                });
        }
    }
}
