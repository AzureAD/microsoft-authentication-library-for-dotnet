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
    /// <summary>
    /// Priority order of metadata providers: 
    /// 
    /// If user provided metadata via <see cref="AbstractApplicationBuilder{T}.WithInstanceDicoveryMetadata(string)"/> use it exclusively. Otherwise:
    /// 
    /// 1. Static cache (this is populated from the network)
    /// 2. Well-known cache if all environments present in the token cache are known (this is hardcoded into msal)
    /// 3. Cache stored in token cache (Not currently implemented)
    /// 5. AAD discovery endpoint 
    /// 6. If going to the network fails with an error different than "invalid_instance" (i.e.authority validation failed), use the well-known instance metadata entry for the given authority
    /// 7. On failure, use the authority itself(i.e.preferred cache = preferred network = aliases = configured_authority)
    /// 
    /// Spec: https://identitydivision.visualstudio.com/DevEx/_git/AuthLibrariesApiReview?path=%2FInstance%20Discovery%20Caching%2Fdesktop_web_caching.md&version=GBdev
    /// </summary>
    internal class InstanceDiscoveryManager : IInstanceDiscoveryManager
    {
        private readonly IHttpManager _httpManager;
        private readonly ITelemetryManager _telemetryManager;

        private readonly IUserMetadataProvider _userMetadataProvider;
        private readonly IKnownMetadataProvider _knownMetadataProvider;
        private readonly INetworkCacheMetadataProvider _networkCacheMetadataProvider;
        private readonly INetworkMetadataProvider _networkMetadataProvider;

        public InstanceDiscoveryManager(
          IHttpManager httpManager,
          ITelemetryManager telemetryManager,
          bool /* for test */ shouldClearCaches,
          InstanceDiscoveryResponse userProviderInstanceDiscoveryResponse) :
            this(
                httpManager,
                telemetryManager,
                shouldClearCaches,
                userProviderInstanceDiscoveryResponse != null ? new UserMetadataProvider(userProviderInstanceDiscoveryResponse) : null,
                null, null, null)
        {
        }

        public /* public for test */ InstanceDiscoveryManager(
            IHttpManager httpManager,
            ITelemetryManager telemetryManager,
            bool shouldClearCaches,
            IUserMetadataProvider userMetadataProvider = null,
            IKnownMetadataProvider knownMetadataProvider = null,
            INetworkCacheMetadataProvider networkCacheMetadataProvider = null,
            INetworkMetadataProvider networkMetadataProvider = null)
        {
            _httpManager = httpManager ?? throw new ArgumentNullException(nameof(httpManager));
            _telemetryManager = telemetryManager ?? throw new ArgumentNullException(nameof(telemetryManager));

            _userMetadataProvider = userMetadataProvider;
            _knownMetadataProvider = knownMetadataProvider ?? new KnownMetadataProvider();
            _networkCacheMetadataProvider = networkCacheMetadataProvider ?? new NetworkCacheMetadataProvider();
            _networkMetadataProvider = networkMetadataProvider ?? 
                new NetworkMetadataProvider(_httpManager, _telemetryManager, _networkCacheMetadataProvider);

            if (shouldClearCaches)
            {
                _networkCacheMetadataProvider.Clear();
            }
        }

        public async Task<InstanceDiscoveryMetadataEntry> GetMetadataEntryTryAvoidNetworkAsync(
            string authority,
            IEnumerable<string> existingEnvironmentsInCache,
            RequestContext requestContext)
        {
            AuthorityType type = Authority.GetAuthorityType(authority);
            Uri authorityUri = new Uri(authority);
            string environment = authorityUri.Host;

            switch (type)
            {
                case AuthorityType.Aad:

                    return
                        _userMetadataProvider?.GetMetadataOrThrow(environment, requestContext.Logger) ??  // if user provided metadata but entry is not found, fail fast
                        _networkCacheMetadataProvider.GetMetadata(environment, requestContext.Logger) ??
                        _knownMetadataProvider.GetMetadata(environment, existingEnvironmentsInCache, requestContext.Logger) ??
                        await GetMetadataEntryAsync(authority, requestContext).ConfigureAwait(false);

                case AuthorityType.Adfs:
                case AuthorityType.B2C:

                    requestContext.Logger.Info("[Instance Discovery] Skipping Instance discovery for non-AAD authority");
                    return await GetMetadataEntryAsync(authority, requestContext).ConfigureAwait(false);

                default:
                    throw new InvalidOperationException("Unexpected authority type " + type);
            }
        }

        public async Task<InstanceDiscoveryMetadataEntry> GetMetadataEntryAsync(
            string authority, 
            RequestContext requestContext)
        {
            AuthorityType type = Authority.GetAuthorityType(authority);
            Uri authorityUri = new Uri(authority);
            string environment = authorityUri.Host;

            switch (type)
            {
                case AuthorityType.Aad:

                    InstanceDiscoveryMetadataEntry entry =
                        _userMetadataProvider?.GetMetadataOrThrow(environment, requestContext.Logger) ??  // if user provided metadata but entry is not found, fail fast
                        await FetchNetworkMetadataOrFallbackAsync(requestContext, authorityUri).ConfigureAwait(false);

                    if (entry == null)
                    {
                        string message = "[Instance Discovery] Instance metadata for this authority could neither be fetched nor found. MSAL will continue regardless. SSO might be broken if authority aliases exist. ";
                        requestContext.Logger.WarningPii(message + "Authority: " + authority, message);

                        entry = CreateEntryForSingleAuthority(authorityUri);
                    }

                    return entry;

                // ADFS and B2C do not support instance discovery 
                case AuthorityType.Adfs:
                case AuthorityType.B2C:
                    requestContext.Logger.Info("[Instance Discovery] Skipping Instance discovery for non-AAD authority");
                    return CreateEntryForSingleAuthority(authorityUri);

                default:
                    throw new InvalidOperationException("Unexpected authority type " + type);
            }
        }

        private async Task<InstanceDiscoveryMetadataEntry> FetchNetworkMetadataOrFallbackAsync(
            RequestContext requestContext, 
            Uri authorityUri)
        {
            try
            {
                return await _networkMetadataProvider.GetMetadataAsync(authorityUri, requestContext).ConfigureAwait(false);
            }
            catch (MsalServiceException ex)
            {
                // Validate Authority exception
                if (ex.ErrorCode == MsalError.InvalidInstance)
                {
                    throw;
                }

                string message =
                    "[Instance Discovery] Instance Discovery failed. Potential cause: no network connection or discovery endpoint is busy. See exception below. MSAL will continue without network instance metadata.";

                requestContext.Logger.WarningPii(message + " Authority: " + authorityUri, message);
                requestContext.Logger.WarningPii(ex);

                return _knownMetadataProvider.GetMetadata(authorityUri.Host, Enumerable.Empty<string>(), requestContext.Logger);
            }
        }

        internal void AddTestValueToStaticProvider(string environment, InstanceDiscoveryMetadataEntry entry)
        {
            _networkCacheMetadataProvider.AddMetadata(environment, entry);
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
    }
}
