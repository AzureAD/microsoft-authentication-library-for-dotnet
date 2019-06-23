// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    internal class InstanceDiscoveryManager : IInstanceDiscoveryManager
    {
        private readonly IHttpManager _httpManager;
        private readonly ITelemetryManager _telemetryManager;

        private readonly IKnownMetadataProvider _knownMetadataProvider;
        private readonly IStaticMetadataProvider _staticMetadataProvider;
        private readonly INetworkMetadataProvider _networkMetadataProvider;

        public InstanceDiscoveryManager(
            IHttpManager httpManager,
            ITelemetryManager telemetryManager,
            bool shouldClearCaches,
            IKnownMetadataProvider knownMetadataProvider = null,
            IStaticMetadataProvider staticMetadataProvider = null,
            INetworkMetadataProvider networkMetadataProvider = null)
        {
            _httpManager = httpManager ?? throw new ArgumentNullException(nameof(httpManager));
            _telemetryManager = telemetryManager ?? throw new ArgumentNullException(nameof(telemetryManager));

            _knownMetadataProvider = knownMetadataProvider ?? new KnownMetadataProvider();
            _staticMetadataProvider = staticMetadataProvider ?? new StaticMetadataProvider();
            _networkMetadataProvider = networkMetadataProvider ?? new NetworkMetadataProvider(_httpManager, _telemetryManager);

            if (shouldClearCaches)
            {
                _staticMetadataProvider.Clear();
            }
        }

        public async Task<InstanceDiscoveryMetadataEntry> GetMetadataEntryTryAvoidNetworkAsync(
            string authority,
            IEnumerable<string> existingEnviromentsInCache,
            RequestContext requestContext)
        {
            AuthorityType type = Authority.GetAuthorityType(authority);
            Uri authorityUri = new Uri(authority);
            string environment = authorityUri.Host;

            switch (type)
            {
                case AuthorityType.Aad:
                    InstanceDiscoveryMetadataEntry entry = _staticMetadataProvider.GetMetadata(environment);

                    if (entry != null)
                    {
                        return entry;
                    }

                    entry = _knownMetadataProvider.GetMetadata(environment, existingEnviromentsInCache);

                    if (entry != null)
                    {
                        return entry;
                    }

                    return await GetMetadataEntryAsync(authority, requestContext).ConfigureAwait(false);

                case AuthorityType.Adfs:
                case AuthorityType.B2C:

                    return await GetMetadataEntryAsync(authority, requestContext).ConfigureAwait(false);

                default:
                    throw new InvalidOperationException("Unexpected authority type " + type);
            }
        }

        public async Task<InstanceDiscoveryMetadataEntry> GetMetadataEntryAsync(string authority, RequestContext requestContext)
        {
            AuthorityType type = Authority.GetAuthorityType(authority);
            Uri authorityUri = new Uri(authority);
            string environment = authorityUri.Host;

            switch (type)
            {
                case AuthorityType.Aad:
                    InstanceDiscoveryMetadataEntry entry = _staticMetadataProvider.GetMetadata(environment);

                    if (entry != null)
                    {
                        return entry;
                    }

                    InstanceDiscoveryResponse instanceDiscoveryResponse =
                        await _networkMetadataProvider.FetchAllDiscoveryMetadataAsync(authorityUri, requestContext).ConfigureAwait(false);
                    CacheInstanceDiscoveryMetadata(instanceDiscoveryResponse);
                    entry = _staticMetadataProvider.GetMetadata(environment);

                    return entry ?? CreateEntryForSingleAuthority(authorityUri);

                // ADFS and B2C do not support instance discovery 
                case AuthorityType.Adfs:
                case AuthorityType.B2C:

                    return CreateEntryForSingleAuthority(authorityUri);

                default:
                    throw new InvalidOperationException("Unexpected authority type " + type);
            }
        }


        internal void AddTestValueToStaticProvider(string environment, InstanceDiscoveryMetadataEntry entry)
        {
            _staticMetadataProvider.AddMetadata(environment, entry);
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

        private void CacheInstanceDiscoveryMetadata(InstanceDiscoveryResponse instanceDiscoveryResponse)
        {
            foreach (InstanceDiscoveryMetadataEntry entry in instanceDiscoveryResponse.Metadata ?? Enumerable.Empty<InstanceDiscoveryMetadataEntry>())
            {
                foreach (string aliasedEnvironment in entry.Aliases ?? Enumerable.Empty<string>())
                {
                    _staticMetadataProvider.AddMetadata(aliasedEnvironment, entry);
                }
            }
        }

    }
}
