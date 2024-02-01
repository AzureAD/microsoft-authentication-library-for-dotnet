// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Region;

namespace Microsoft.Identity.Client.Instance.Discovery
{
    /// <summary>
    /// Priority order of metadata providers: 
    /// 
    /// If user provided metadata via <see cref="AbstractApplicationBuilder{T}.WithInstanceDiscoveryMetadata(string)"/> use it exclusively. Otherwise:
    /// 
    /// 1. Static cache (this is populated from the network)
    /// 2. Well-known cache if all environments present in the token cache are known (this is hard-coded into MSAL)
    /// 3. Cache stored in token cache (Not currently implemented)
    /// 5. AAD discovery endpoint 
    /// 6. If going to the network fails with an error different than "invalid_instance" (i.e.authority validation failed), use the well-known instance metadata entry for the given authority
    /// 7. On failure, use the authority itself(i.e.preferred cache = preferred network = aliases = configured_authority)
    /// 
    /// Spec: https://identitydivision.visualstudio.com/DevEx/_git/AuthLibrariesApiReview?path=%2FInstance%20Discovery%20Caching%2Fdesktop_web_caching.md
    /// </summary>
    internal class InstanceDiscoveryManager : IInstanceDiscoveryManager
    {
        private readonly IHttpManager _httpManager;
        private readonly IUserMetadataProvider _userMetadataProvider;
        private readonly IKnownMetadataProvider _knownMetadataProvider;
        private readonly INetworkCacheMetadataProvider _networkCacheMetadataProvider;
        private readonly INetworkMetadataProvider _networkMetadataProvider;
        private readonly IRegionDiscoveryProvider _regionDiscoveryProvider;

        public InstanceDiscoveryManager(
          IHttpManager httpManager,
          bool /* for test */ shouldClearCaches,
          InstanceDiscoveryResponse userProvidedInstanceDiscoveryResponse = null,
          Uri userProvidedInstanceDiscoveryUri = null) :
            this(
                httpManager,                
                shouldClearCaches,
                userProvidedInstanceDiscoveryResponse != null ? new UserMetadataProvider(userProvidedInstanceDiscoveryResponse) : null,
                userProvidedInstanceDiscoveryUri,
                null, null, null, null)
        {
        }

        public /* public for test */ InstanceDiscoveryManager(
            IHttpManager httpManager,
            bool shouldClearCaches,
            IUserMetadataProvider userMetadataProvider = null,
            Uri userProvidedInstanceDiscoveryUri = null,
            IKnownMetadataProvider knownMetadataProvider = null,
            INetworkCacheMetadataProvider networkCacheMetadataProvider = null,
            INetworkMetadataProvider networkMetadataProvider = null,
            IRegionDiscoveryProvider regionDiscoveryProvider = null)
        {
            _httpManager = httpManager ?? throw new ArgumentNullException(nameof(httpManager));

            _userMetadataProvider = userMetadataProvider;
            _knownMetadataProvider = knownMetadataProvider ?? new KnownMetadataProvider();
            _networkCacheMetadataProvider = networkCacheMetadataProvider ?? new NetworkCacheMetadataProvider();

            _networkMetadataProvider = networkMetadataProvider ??
                new NetworkMetadataProvider(
                    _httpManager,
                    _networkCacheMetadataProvider,
                    userProvidedInstanceDiscoveryUri);

            _regionDiscoveryProvider = regionDiscoveryProvider ??
                new RegionDiscoveryProvider(_httpManager, shouldClearCaches);

            if (shouldClearCaches)
            {
                _networkCacheMetadataProvider.Clear();
            }
        }

        public async Task<InstanceDiscoveryMetadataEntry> GetMetadataEntryTryAvoidNetworkAsync(
            AuthorityInfo authorityInfo,
            IEnumerable<string> existingEnvironmentsInCache,
            RequestContext requestContext)
        {
            string environment = authorityInfo.Host;

            if (authorityInfo.IsInstanceDiscoverySupported)
            {
                InstanceDiscoveryMetadataEntry entry =
                    // if user provided metadata but entry is not found, fail fast
                    _userMetadataProvider?.GetMetadataOrThrow(environment, requestContext.Logger) ??
                    await _regionDiscoveryProvider.GetMetadataAsync(authorityInfo.CanonicalAuthority, requestContext).ConfigureAwait(false);

                //Check if instance discovery endpoint is disabled
                if (entry == null && requestContext.ServiceBundle.Config.IsInstanceDiscoveryEnabled)
                {
                    entry = _networkCacheMetadataProvider.GetMetadata(environment, requestContext.Logger) ??
                    _knownMetadataProvider.GetMetadata(environment, existingEnvironmentsInCache, requestContext.Logger) ??
                    await GetMetadataEntryAsync(authorityInfo, requestContext).ConfigureAwait(false);
                }
                if (entry == null)
                {
                    requestContext.Logger.Info(() => $"Skipping Instance discovery for {authorityInfo.AuthorityType} authority because it is not enabled.");
                    entry = CreateEntryForSingleAuthority(authorityInfo.CanonicalAuthority);
                }

                return entry;
            }
            else
            {
                requestContext.Logger.Info(() => $"Skipping Instance discovery for {authorityInfo.AuthorityType} authority because it is not supported.");
                return await GetMetadataEntryAsync(authorityInfo, requestContext).ConfigureAwait(false);
            }
        }

        public async Task<InstanceDiscoveryMetadataEntry> GetMetadataEntryAsync(
            AuthorityInfo authorityInfo,
            RequestContext requestContext, 
            bool forceValidation = false)
        {
            Uri authorityUri = authorityInfo.CanonicalAuthority;
            string environment = authorityInfo.Host;

            if (authorityInfo.IsInstanceDiscoverySupported)
            {
                var entry = _userMetadataProvider?.GetMetadataOrThrow(environment, requestContext.Logger);

                //Check if instance discovery endpoint is disabled
                if (entry == null && !requestContext.ServiceBundle.Config.IsInstanceDiscoveryEnabled)
                {
                    //Check if regional discovery provider returns an entry. Regional should not be affected by the disabling of instance discovery endpoint.
                    entry = await _regionDiscoveryProvider.GetMetadataAsync(authorityUri, requestContext).ConfigureAwait(false);

                    if (entry == null)
                    {
                        requestContext.Logger.Info("[Instance Discovery] Skipping Instance discovery because it is disabled. ");
                        return CreateEntryForSingleAuthority(authorityUri);
                    }
                }

                if (entry == null && forceValidation)
                {
                    // only the network provider does validation
                    await FetchNetworkMetadataOrFallbackAsync(requestContext, authorityUri).ConfigureAwait(false);
                }

                requestContext.Logger.Info("[Instance Discovery] Instance discovery is enabled and will be performed");
                entry ??= await _regionDiscoveryProvider.GetMetadataAsync(authorityUri, requestContext).ConfigureAwait(false) ??
                          await FetchNetworkMetadataOrFallbackAsync(requestContext, authorityUri).ConfigureAwait(false);

                if (entry == null)
                {
                    string message = "[Instance Discovery] Instance metadata for this authority could neither be fetched nor found. MSAL will continue regardless. SSO might be broken if authority aliases exist. ";
                    requestContext.Logger.WarningPii(message + "Authority: " + authorityInfo.CanonicalAuthority, message);

                    entry = CreateEntryForSingleAuthority(authorityUri);
                    _networkCacheMetadataProvider.AddMetadata(environment, entry);
                }

                return entry;
            }
            else
            {
                requestContext.Logger.Info("[Instance Discovery] Skipping Instance discovery for non-AAD authority. ");
                return CreateEntryForSingleAuthority(authorityUri);
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
                if (!requestContext.ServiceBundle.Config.Authority.AuthorityInfo.ValidateAuthority)
                {
                    requestContext.Logger.Info("[Instance Discovery] Skipping Instance discovery as validate authority is set to false. ");
                    return CreateEntryForSingleAuthority(authorityUri);
                }

                // Validate Authority exception
                if (ex.ErrorCode == MsalError.InvalidInstance)
                {
                    requestContext.Logger.Error("[Instance Discovery] Instance discovery failed - invalid instance!");
                    throw;
                }

                string message =
                    "[Instance Discovery] Instance Discovery failed. Potential cause: no network connection or discovery endpoint is busy. See exception below. MSAL will continue without network instance metadata. ";

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
