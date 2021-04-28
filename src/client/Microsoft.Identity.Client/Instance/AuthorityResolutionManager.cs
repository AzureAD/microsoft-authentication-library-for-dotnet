// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Instance.Validation;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client.Instance
{
    /// <summary>
    /// Responsible for figuring out authority endpoints and for validation
    /// </summary>
    internal class AuthorityResolutionManager
        : IAuthorityResolutionManager
    {
        private static readonly ConcurrentDictionary<string, AuthorityEndpointCacheEntry> s_endpointCacheEntries =
            new ConcurrentDictionary<string, AuthorityEndpointCacheEntry>();

        private static readonly ConcurrentHashSet<string> s_validatedEnvironments =
            new ConcurrentHashSet<string>();

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public AuthorityResolutionManager(bool shouldClearCache = true)
        {
            if (shouldClearCache)
            {
                s_endpointCacheEntries.Clear();
                s_validatedEnvironments.Clear();
            }
        }

        public async Task ValidateAuthorityAsync(Authority authority, RequestContext context)
        {
            if (!s_validatedEnvironments.Contains(authority.AuthorityInfo.Host))
            {
                await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
                {
                    try
                    {
                        if (!s_validatedEnvironments.Contains(authority.AuthorityInfo.CanonicalAuthority))
                        {

                            // validate the original authority, as the resolved authority might be regionalized and we cannot validate regionalized authorities.
                            var validator = AuthorityValidatorFactory.Create(authority.AuthorityInfo, context);
                            await validator.ValidateAuthorityAsync(authority.AuthorityInfo).ConfigureAwait(false);
                            s_validatedEnvironments.Add(authority.AuthorityInfo.Host);
                        }
                    }
                    finally
                    {
                        _semaphoreSlim.Release();
                    }
                }
            }
        }

        public AuthorityEndpoints ResolveEndpoints(
            Authority authority,
            string userPrincipalName,
            RequestContext requestContext)
        {
            if (TryGetCacheValue(authority.AuthorityInfo, userPrincipalName, out var endpoints))
            {
                requestContext.Logger.Info(LogMessages.ResolvingAuthorityEndpointsTrue);
                return endpoints;
            }

            requestContext.Logger.Info(LogMessages.ResolvingAuthorityEndpointsFalse);

            endpoints = authority.GetHardcodedEndpoints();
            Add(authority.AuthorityInfo, userPrincipalName, endpoints);
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
