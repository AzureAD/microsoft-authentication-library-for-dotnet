// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Instance.Validation;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.Instance
{
    /// <summary>
    /// Responsible for figuring out authority endpoints
    /// </summary>
    internal class AuthorityEndpointResolutionManager : IAuthorityEndpointResolutionManager
    {
        private static readonly ConcurrentDictionary<string, AuthorityEndpointCacheEntry> s_endpointCacheEntries =
            new ConcurrentDictionary<string, AuthorityEndpointCacheEntry>();

        private readonly IServiceBundle _serviceBundle;

        public AuthorityEndpointResolutionManager(IServiceBundle serviceBundle, bool shouldClearCache = true)
        {
            _serviceBundle = serviceBundle;
            if (shouldClearCache)
            {
                s_endpointCacheEntries.Clear();
            }
        }

        public async Task<AuthorityEndpoints> ResolveEndpointsAsync(
            AuthorityInfo authorityInfo,
            string userPrincipalName,
            RequestContext requestContext)
        {
            if (TryGetCacheValue(authorityInfo, userPrincipalName, out var endpoints))
            {
                requestContext.Logger.Info(LogMessages.ResolvingAuthorityEndpointsTrue);
                return endpoints;
            }

            requestContext.Logger.Info(LogMessages.ResolvingAuthorityEndpointsFalse);

            var validator = AuthorityValidatorFactory.Create(authorityInfo, _serviceBundle);

            // TODO: move this away from here?
            await validator.ValidateAuthorityAsync(authorityInfo, requestContext).ConfigureAwait(false);

            //TODO: stop using AuthorityInfo on its own
            var authority = Authority.CreateAuthority(authorityInfo);

            endpoints = authority.GetHardcodedEndpoints();
            Add(authorityInfo, userPrincipalName, endpoints);
            return endpoints;
        }

        private bool TryGetCacheValue(AuthorityInfo authorityInfo, string userPrincipalName, out AuthorityEndpoints endpoints)
        {
            endpoints = null;

            if (!s_endpointCacheEntries.TryGetValue(authorityInfo.CanonicalAuthority, out var cacheEntry))
            {
                return false;
            }

            if (authorityInfo.AuthorityType != AuthorityType.Adfs)
            {
                endpoints = cacheEntry.Endpoints;
                return true;
            }

            if (!string.IsNullOrEmpty(userPrincipalName) &&
                !cacheEntry.ValidForDomainsList.Contains(AdfsUpnHelper.GetDomainFromUpn(userPrincipalName)))
            {
                return false;
            }

            endpoints = cacheEntry.Endpoints;
            return true;
        }

        private void Add(AuthorityInfo authorityInfo, string userPrincipalName, AuthorityEndpoints endpoints)
        {
            var updatedCacheEntry = new AuthorityEndpointCacheEntry(endpoints);

            if (authorityInfo.AuthorityType == AuthorityType.Adfs)
            {
                // Since we're here, we've made a call to the backend.  We want to ensure we're caching
                // the latest values from the server.
                if (s_endpointCacheEntries.TryGetValue(authorityInfo.CanonicalAuthority, out var cacheEntry))
                {
                    foreach (string s in cacheEntry.ValidForDomainsList)
                    {
                        updatedCacheEntry.ValidForDomainsList.Add(s);
                    }
                }

                if (!string.IsNullOrEmpty(userPrincipalName))
                {
                    updatedCacheEntry.ValidForDomainsList.Add(AdfsUpnHelper.GetDomainFromUpn(userPrincipalName));
                }
            }

            s_endpointCacheEntries.TryAdd(authorityInfo.CanonicalAuthority, updatedCacheEntry);
        }

        private class AuthorityEndpointCacheEntry
        {
            public AuthorityEndpointCacheEntry(AuthorityEndpoints endpoints)
            {
                Endpoints = endpoints;
            }

            public AuthorityEndpoints Endpoints { get; }
            public HashSet<string> ValidForDomainsList { get; } = new HashSet<string>();
        }
    }
}
