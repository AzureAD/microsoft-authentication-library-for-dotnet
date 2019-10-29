// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client
{
    public sealed partial class TokenCache : ITokenCacheInternal
    {
        async Task<Tuple<MsalAccessTokenCacheItem, MsalIdTokenCacheItem>> ITokenCacheInternal.SaveTokenResponseAsync(
            AuthenticationRequestParameters requestParams,
            MsalTokenResponse response)
        {
            var tenantId = Authority
                .CreateAuthority(requestParams.TenantUpdatedCanonicalAuthority.AuthorityInfo.CanonicalAuthority)
                .GetTenantId();

            bool isAdfsAuthority = requestParams.AuthorityInfo.AuthorityType == AuthorityType.Adfs;

            IdToken idToken = IdToken.Parse(response.IdToken);

            string subject = idToken?.Subject;

            if (idToken != null && string.IsNullOrEmpty(subject))
            {
                requestParams.RequestContext.Logger.Warning("Subject not present in Id token");
            }

            string preferredUsername;

            // The preferred_username value cannot be null or empty in order to comply with the ADAL/MSAL Unified cache schema.
            // It will be set to "preferred_username not in idtoken"
            if (idToken == null)
            {
                preferredUsername = NullPreferredUsernameDisplayLabel;
            }
            else if (string.IsNullOrWhiteSpace(idToken.PreferredUsername))
            {
                if (isAdfsAuthority)
                {
                    //The direct to adfs scenario does not return preferred_username in the id token so it needs to be set to the upn
                    preferredUsername = !string.IsNullOrEmpty(idToken.Upn)
                        ? idToken.Upn
                        : NullPreferredUsernameDisplayLabel;
                }
                else
                {
                    preferredUsername = NullPreferredUsernameDisplayLabel;
                }
            }
            else
            {
                preferredUsername = idToken.PreferredUsername;
            }

            // Do a full instance discovery when saving tokens (if not cached),
            // so that the PreferredNetwork environment is up to date.
            var instanceDiscoveryMetadata = await ServiceBundle.InstanceDiscoveryManager
                                .GetMetadataEntryAsync(
                                    requestParams.TenantUpdatedCanonicalAuthority.AuthorityInfo.CanonicalAuthority,
                                    requestParams.RequestContext)
                                .ConfigureAwait(false);

            var msalAccessTokenCacheItem =
                new MsalAccessTokenCacheItem(
                    instanceDiscoveryMetadata.PreferredCache,
                    requestParams.ClientId,
                    response,
                    tenantId,
                    subject,
                    requestParams.AuthenticationScheme.KeyId)
                {
                    UserAssertionHash = requestParams.UserAssertion?.AssertionHash,
                    IsAdfs = isAdfsAuthority
                };

            MsalRefreshTokenCacheItem msalRefreshTokenCacheItem = null;
            MsalIdTokenCacheItem msalIdTokenCacheItem = null;
            if (idToken != null)
            {
                msalIdTokenCacheItem = new MsalIdTokenCacheItem(
                    instanceDiscoveryMetadata.PreferredCache,
                    requestParams.ClientId,
                    response,
                    tenantId,
                    subject)
                {
                    IsAdfs = isAdfsAuthority
                };
            }

            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                try
                {
                    Account account = null;
                    string username = isAdfsAuthority ? idToken?.Upn : preferredUsername;
                    if (msalAccessTokenCacheItem.HomeAccountId != null)
                    {
                        account = new Account(
                                          msalAccessTokenCacheItem.HomeAccountId,
                                          username,
                                          instanceDiscoveryMetadata.PreferredCache);
                    }
                    var args = new TokenCacheNotificationArgs(this, ClientId, account, true, (this as ITokenCacheInternal).IsApplicationCache);

#pragma warning disable CS0618 // Type or member is obsolete
                    HasStateChanged = true;
#pragma warning restore CS0618 // Type or member is obsolete

                    await (this as ITokenCacheInternal).OnBeforeAccessAsync(args).ConfigureAwait(false);
                    try
                    {
                        await (this as ITokenCacheInternal).OnBeforeWriteAsync(args).ConfigureAwait(false);

                        DeleteAccessTokensWithIntersectingScopes(
                            requestParams,
                            instanceDiscoveryMetadata.Aliases,
                            tenantId,
                            msalAccessTokenCacheItem.ScopeSet,
                            msalAccessTokenCacheItem.HomeAccountId);

                        _accessor.SaveAccessToken(msalAccessTokenCacheItem);

                        if (idToken != null)
                        {
                            _accessor.SaveIdToken(msalIdTokenCacheItem);
                            var msalAccountCacheItem = new MsalAccountCacheItem(
                                instanceDiscoveryMetadata.PreferredCache,
                                response,
                                preferredUsername,
                                tenantId);

                            //The ADFS direct scenario does not return client info so the home account id is acquired from the subject
                            if (isAdfsAuthority && String.IsNullOrEmpty(msalAccountCacheItem.HomeAccountId))
                            {
                                msalAccountCacheItem.HomeAccountId = idToken.Subject;
                            }

                            _accessor.SaveAccount(msalAccountCacheItem);
                        }

                        // if server returns the refresh token back, save it in the cache.
                        if (response.RefreshToken != null)
                        {
                            msalRefreshTokenCacheItem = new MsalRefreshTokenCacheItem(
                                instanceDiscoveryMetadata.PreferredCache,
                                requestParams.ClientId,
                                response,
                                subject);

                            if (!_featureFlags.IsFociEnabled)
                            {
                                msalRefreshTokenCacheItem.FamilyId = null;
                            }

                            requestParams.RequestContext.Logger.Info("Saving RT in cache...");
                            _accessor.SaveRefreshToken(msalRefreshTokenCacheItem);
                        }

                        UpdateAppMetadata(requestParams.ClientId, instanceDiscoveryMetadata.PreferredCache, response.FamilyId);

                        // save RT in ADAL cache for public clients
                        // do not save RT in ADAL cache for MSAL B2C scenarios
                        if (!requestParams.IsClientCredentialRequest && !requestParams.AuthorityInfo.AuthorityType.Equals(AuthorityType.B2C))
                        {
                            var authorityWithPrefferedCache = Authority.CreateAuthorityWithEnvironment(
                                    requestParams.TenantUpdatedCanonicalAuthority.AuthorityInfo,
                                    instanceDiscoveryMetadata.PreferredCache);

                            CacheFallbackOperations.WriteAdalRefreshToken(
                                Logger,
                                LegacyCachePersistence,
                                msalRefreshTokenCacheItem,
                                msalIdTokenCacheItem,
                                authorityWithPrefferedCache.AuthorityInfo.CanonicalAuthority,
                                msalIdTokenCacheItem.IdToken.ObjectId, response.Scope);
                        }
                    }
                    finally
                    {
                        await (this as ITokenCacheInternal).OnAfterAccessAsync(args).ConfigureAwait(false);
                    }

                    return Tuple.Create(msalAccessTokenCacheItem, msalIdTokenCacheItem);
                }
                finally
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    HasStateChanged = false;
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        async Task<MsalAccessTokenCacheItem> ITokenCacheInternal.FindAccessTokenAsync(AuthenticationRequestParameters requestParams)
        {
            // no authority passed
            if (requestParams?.AuthorityInfo?.CanonicalAuthority == null)
            {
                requestParams.RequestContext.Logger.Warning("No authority provided. Skipping cache lookup ");
                return null;
            }

            requestParams.RequestContext.Logger.Info("Looking up access token in the cache.");
            IEnumerable<MsalAccessTokenCacheItem> tokenCacheItems = GetAllAccessTokensWithNoLocks(true);

            tokenCacheItems = FilterByHomeAccountTenantOrAssertion(requestParams, tokenCacheItems);

            // no match found after initial filtering
            if (!tokenCacheItems.Any())
            {
                requestParams.RequestContext.Logger.Info("No matching entry found for user or assertion");
                return null;
            }

            requestParams.RequestContext.Logger.Info("Matching entry count -" + tokenCacheItems.Count());

            IEnumerable<MsalAccessTokenCacheItem> filteredItems =
                tokenCacheItems.Where(item => ScopeHelper.ScopeContains(item.ScopeSet, requestParams.Scope));

            requestParams.RequestContext.Logger.Info("Matching entry count after filtering by scopes - " + filteredItems.Count());
            filteredItems = await FilterByEnvironmentAsync(requestParams, filteredItems).ConfigureAwait(false);

            // no match
            if (!filteredItems.Any())
            {
                requestParams.RequestContext.Logger.Info("No tokens found for matching authority, client_id, user and scopes.");
                return null;
            }

            MsalAccessTokenCacheItem msalAccessTokenCacheItem = GetSingleResult(requestParams, filteredItems);
            msalAccessTokenCacheItem = FilterByKeyId(msalAccessTokenCacheItem, requestParams);

            return GetUnexpiredAccessTokenOrNull(requestParams, msalAccessTokenCacheItem);
        }

        private static IEnumerable<MsalAccessTokenCacheItem> FilterByHomeAccountTenantOrAssertion(
            AuthenticationRequestParameters requestParams,
            IEnumerable<MsalAccessTokenCacheItem> tokenCacheItems)
        {
            // this is OBO flow. match the cache entry with assertion hash,
            // Authority, ScopeSet and client Id.
            if (requestParams.UserAssertion != null)
            {
                return tokenCacheItems.FilterWithLogging(item =>
                                !string.IsNullOrEmpty(item.UserAssertionHash) &&
                                item.UserAssertionHash.Equals(requestParams.UserAssertion.AssertionHash, StringComparison.OrdinalIgnoreCase),
                                requestParams.RequestContext.Logger,
                                "Filtering by user assertion id");
            }

            string requestTenantId = requestParams.Authority.GetTenantId();

            tokenCacheItems = tokenCacheItems.FilterWithLogging(item =>
                string.Equals(item.TenantId ?? string.Empty, requestTenantId ?? string.Empty, StringComparison.OrdinalIgnoreCase),
                requestParams.RequestContext.Logger,
                "Filtering by tenant id");

            if (!requestParams.IsClientCredentialRequest)
            {
                tokenCacheItems = tokenCacheItems.FilterWithLogging(item => item.HomeAccountId.Equals(
                                requestParams.Account?.HomeAccountId?.Identifier, StringComparison.OrdinalIgnoreCase),
                                requestParams.RequestContext.Logger,
                                "Filtering by home account id");
            }

            return tokenCacheItems;
        }

        private MsalAccessTokenCacheItem GetUnexpiredAccessTokenOrNull(AuthenticationRequestParameters requestParams, MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            if (msalAccessTokenCacheItem != null)
            {
                if (msalAccessTokenCacheItem.ExpiresOn >
                    DateTime.UtcNow + TimeSpan.FromMinutes(DefaultExpirationBufferInMinutes))
                {
                    requestParams.RequestContext.Logger.Info(
                        "Access token is not expired. Returning the found cache entry. " +
                        GetAccessTokenExpireLogMessageContent(msalAccessTokenCacheItem));
                    return msalAccessTokenCacheItem;
                }

                if (ServiceBundle.Config.IsExtendedTokenLifetimeEnabled && msalAccessTokenCacheItem.ExtendedExpiresOn >
                    DateTime.UtcNow + TimeSpan.FromMinutes(DefaultExpirationBufferInMinutes))
                {
                    requestParams.RequestContext.Logger.Info(
                        "Access token is expired.  IsExtendedLifeTimeEnabled=TRUE and ExtendedExpiresOn is not exceeded.  Returning the found cache entry. " +
                        GetAccessTokenExpireLogMessageContent(msalAccessTokenCacheItem));

                    msalAccessTokenCacheItem.IsExtendedLifeTimeToken = true;
                    return msalAccessTokenCacheItem;
                }

                requestParams.RequestContext.Logger.Info(
                    "Access token has expired or about to expire. " +
                    GetAccessTokenExpireLogMessageContent(msalAccessTokenCacheItem));
            }

            return null;
        }

        private static MsalAccessTokenCacheItem GetSingleResult(AuthenticationRequestParameters requestParams, IEnumerable<MsalAccessTokenCacheItem> filteredItems)
        {
            MsalAccessTokenCacheItem msalAccessTokenCacheItem;

            // if only one cached token found
            if (filteredItems.Count() == 1)
            {
                msalAccessTokenCacheItem = filteredItems.First();
            }
            else
            {
                requestParams.RequestContext.Logger.Error("Multiple tokens found for matching authority, client_id, user and scopes.");

                throw new MsalClientException(
                    MsalError.MultipleTokensMatchedError,
                    MsalErrorMessage.MultipleTokensMatched);
            }

            return msalAccessTokenCacheItem;
        }

        private async Task<IEnumerable<MsalAccessTokenCacheItem>> FilterByEnvironmentAsync(AuthenticationRequestParameters requestParams, IEnumerable<MsalAccessTokenCacheItem> filteredItems)
        {
            // at this point we need env aliases, try to get them without a discovery call
            var instanceMetadata = await ServiceBundle.InstanceDiscoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                                     requestParams.AuthorityInfo.CanonicalAuthority,
                                     filteredItems.Select(at => at.Environment),  // if all environments are known, a network call can be avoided
                                     requestParams.RequestContext)
                            .ConfigureAwait(false);

            // In case we're sharing the cache with an MSAL that does not implement env aliasing,
            // it's possible (but unlikely), that we have multiple ATs from the same alias family.
            // To overcome some of these use cases, try to filter just by preferred cache alias
            var filteredByPreferredAlias = filteredItems.Where(
                at => at.Environment.Equals(instanceMetadata.PreferredCache, StringComparison.OrdinalIgnoreCase));

            if (filteredByPreferredAlias.Any())
            {
                return filteredByPreferredAlias;
            }

            return filteredItems.Where(
                item => instanceMetadata.Aliases.ContainsOrdinalIgnoreCase(item.Environment));
        }

        private MsalAccessTokenCacheItem FilterByKeyId(MsalAccessTokenCacheItem item, AuthenticationRequestParameters authenticationRequest)
        {
            if (item == null)
            {
                return null;
            }

            string requestKid = authenticationRequest.AuthenticationScheme.KeyId;
            if (string.IsNullOrEmpty(item.KeyId) && string.IsNullOrEmpty(requestKid))
            {
                authenticationRequest.RequestContext.Logger.Verbose("Bearer token found");
                return item;
            }

            if (string.Equals(item.KeyId, requestKid))
            {
                authenticationRequest.RequestContext.Logger.Verbose("PoP token found");
                return item;
            }

            authenticationRequest.RequestContext.Logger.Info(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "A token bound to the wrong key was found. Token key id: {0} Request key id: {1}",
                        item.KeyId,
                        requestKid));
            return null;
        }

        async Task<MsalRefreshTokenCacheItem> ITokenCacheInternal.FindRefreshTokenAsync(
            AuthenticationRequestParameters requestParams,
            string familyId)
        {
            if (requestParams.Authority == null)
                return null;

            IEnumerable<MsalRefreshTokenCacheItem> allRts = _accessor.GetAllRefreshTokens();

            var metadata =
                await ServiceBundle.InstanceDiscoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                    requestParams.AuthorityInfo.CanonicalAuthority,
                    allRts.Select(rt => rt.Environment),  // if all environments are known, a network call can be avoided
                    requestParams.RequestContext)
                .ConfigureAwait(false);
            var aliases = metadata.Aliases;

            IEnumerable<MsalRefreshTokenCacheKey> candidateRtKeys = aliases.Select(
                    al => new MsalRefreshTokenCacheKey(
                        al,
                        requestParams.ClientId,
                        requestParams.Account?.HomeAccountId?.Identifier,
                        familyId));

            MsalRefreshTokenCacheItem candidateRt = allRts.FirstOrDefault(
                rt => candidateRtKeys.Any(
                    candidateKey => object.Equals(rt.GetKey(), candidateKey)));

            requestParams.RequestContext.Logger.Info("Refresh token found in the cache? - " + (candidateRt != null));

            if (candidateRt != null)
                return candidateRt;

            requestParams.RequestContext.Logger.Info("Checking ADAL cache for matching RT");

            string upn = string.IsNullOrWhiteSpace(requestParams.LoginHint)
                ? requestParams.Account?.Username
                : requestParams.LoginHint;

            // ADAL legacy cache does not store FRTs
            if (requestParams.Account != null && string.IsNullOrEmpty(familyId))
            {
                return CacheFallbackOperations.GetAdalEntryForMsal(
                    Logger,
                    LegacyCachePersistence,
                    metadata.PreferredCache,
                    aliases,
                    requestParams.ClientId,
                    upn,
                    requestParams.Account.HomeAccountId?.Identifier);
            }

            return null;
        }

        async Task<bool?> ITokenCacheInternal.IsFociMemberAsync(AuthenticationRequestParameters requestParams, string familyId)
        {
            var logger = requestParams.RequestContext.Logger;
            if (requestParams?.AuthorityInfo?.CanonicalAuthority == null)
            {
                logger.Warning("No authority details, can't check app metadata. Returning unknown");
                return null;
            }

            IEnumerable<MsalAppMetadataCacheItem> allAppMetadata = _accessor.GetAllAppMetadata();

            var instanceMetadata = await ServiceBundle.InstanceDiscoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                    requestParams.AuthorityInfo.CanonicalAuthority,
                    allAppMetadata.Select(m => m.Environment),
                    requestParams.RequestContext)
                .ConfigureAwait(false);

            var appMetadata =
                instanceMetadata.Aliases
                .Select(env => _accessor.GetAppMetadata(new MsalAppMetadataCacheKey(ClientId, env)))
                .FirstOrDefault(item => item != null);

            // From a FOCI perspective, an app has 3 states - in the family, not in the family or unknown
            // Unknown is a valid state, where we never fetched tokens for that app or when we used an older
            // version of MSAL which did not record app metadata.
            if (appMetadata == null)
            {
                logger.Warning("No app metadata found. Returning unknown");
                return null;
            }

            return appMetadata.FamilyId == familyId;
        }

        MsalIdTokenCacheItem ITokenCacheInternal.GetIdTokenCacheItem(MsalIdTokenCacheKey msalIdTokenCacheKey)
        {
            var idToken = _accessor.GetIdToken(msalIdTokenCacheKey);
            return idToken;
        }

        /// <remarks>
        /// Get accounts should not make a network call, if possible. This can be achieved if
        /// all the environments in the token cache are known to MSAL, as MSAL keeps a list of
        /// known environments in <see cref="KnownMetadataProvider"/>
        /// </remarks>
        async Task<IEnumerable<IAccount>> ITokenCacheInternal.GetAccountsAsync(string authority, RequestContext requestContext)
        {
            var environment = Authority.GetEnviroment(authority);
            bool filterByClientId = !_featureFlags.IsFociEnabled;

            IEnumerable<MsalRefreshTokenCacheItem> rtCacheItems = GetAllRefreshTokensWithNoLocks(filterByClientId);
            IEnumerable<MsalAccountCacheItem> accountCacheItems = _accessor.GetAllAccounts();

            AdalUsersForMsal adalUsersResult = CacheFallbackOperations.GetAllAdalUsersForMsal(
                Logger,
                LegacyCachePersistence,
                ClientId);

            // Multi-cloud support - must filter by env.
            ISet<string> allEnvironmentsInCache = new HashSet<string>(
                accountCacheItems.Select(aci => aci.Environment),
                StringComparer.OrdinalIgnoreCase);
            allEnvironmentsInCache.UnionWith(rtCacheItems.Select(rt => rt.Environment));
            allEnvironmentsInCache.UnionWith(adalUsersResult.GetAdalUserEnviroments());

            var instanceMetadata = await ServiceBundle.InstanceDiscoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                authority,
                allEnvironmentsInCache,
                requestContext).ConfigureAwait(false);

            rtCacheItems = rtCacheItems.Where(rt => instanceMetadata.Aliases.ContainsOrdinalIgnoreCase(rt.Environment));
            accountCacheItems = accountCacheItems.Where(acc => instanceMetadata.Aliases.ContainsOrdinalIgnoreCase(acc.Environment));

            IDictionary<string, Account> clientInfoToAccountMap = new Dictionary<string, Account>();
            foreach (MsalRefreshTokenCacheItem rtItem in rtCacheItems)
            {
                foreach (MsalAccountCacheItem account in accountCacheItems)
                {
                    if (RtMatchesAccount(rtItem, account))
                    {
                        clientInfoToAccountMap[rtItem.HomeAccountId] = new Account(
                            account.HomeAccountId,
                            account.PreferredUsername,
                            environment);  // Preserve the env passed in by the user

                        break;
                    }
                }
            }

            IEnumerable<IAccount> accounts = UpdateWithAdalAccounts(
                environment,
                instanceMetadata.Aliases,
                adalUsersResult,
                clientInfoToAccountMap);

            return accounts;
        }

        async Task<IEnumerable<MsalRefreshTokenCacheItem>> ITokenCacheInternal.GetAllRefreshTokensAsync(bool filterByClientId)
        {
            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                return GetAllRefreshTokensWithNoLocks(filterByClientId);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        async Task<IEnumerable<MsalAccessTokenCacheItem>> ITokenCacheInternal.GetAllAccessTokensAsync(bool filterByClientId)
        {
            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                return GetAllAccessTokensWithNoLocks(filterByClientId);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        async Task<IEnumerable<MsalIdTokenCacheItem>> ITokenCacheInternal.GetAllIdTokensAsync(bool filterByClientId)
        {
            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                return GetAllIdTokensWithNoLocks(filterByClientId);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        async Task<IEnumerable<MsalAccountCacheItem>> ITokenCacheInternal.GetAllAccountsAsync()
        {
            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                return _accessor.GetAllAccounts();
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        async Task ITokenCacheInternal.RemoveAccountAsync(IAccount account, RequestContext requestContext)
        {
            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                requestContext.Logger.Info("Removing user from cache..");

                try
                {
                    var args = new TokenCacheNotificationArgs(this, ClientId, account, true, (this as ITokenCacheInternal).IsApplicationCache);

                    await (this as ITokenCacheInternal).OnBeforeAccessAsync(args).ConfigureAwait(false);
                    try
                    {
                        await (this as ITokenCacheInternal).OnBeforeWriteAsync(args).ConfigureAwait(false);

                        ((ITokenCacheInternal)this).RemoveMsalAccountWithNoLocks(account, requestContext);
                        RemoveAdalUser(account);
                    }
                    finally
                    {
                        await (this as ITokenCacheInternal).OnAfterAccessAsync(args).ConfigureAwait(false);
                    }
                }
                finally
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    HasStateChanged = false;
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        void ITokenCacheInternal.RemoveMsalAccountWithNoLocks(IAccount account, RequestContext requestContext)
        {
            if (account.HomeAccountId == null)
            {
                // adalv3 account
                return;
            }

            var allRefreshTokens = GetAllRefreshTokensWithNoLocks(false)
                .Where(item => item.HomeAccountId.Equals(account.HomeAccountId.Identifier, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // To maintain backward compatiblity with other MSALs, filter all credentials by clientID if
            // Foci is disabled or if an FRT is not present
            bool filterByClientId = !_featureFlags.IsFociEnabled || !FrtExists(allRefreshTokens);

            // Delete all credentials associated with this IAccount
            var refreshTokensToDelete = filterByClientId ?
                allRefreshTokens.Where(x => x.ClientId.Equals(ClientId, StringComparison.OrdinalIgnoreCase)) :
                allRefreshTokens;

            foreach (MsalRefreshTokenCacheItem refreshTokenCacheItem in refreshTokensToDelete)
            {
                _accessor.DeleteRefreshToken(refreshTokenCacheItem.GetKey());
            }

            requestContext.Logger.Info("Deleted refresh token count - " + allRefreshTokens.Count);
            IList<MsalAccessTokenCacheItem> allAccessTokens = GetAllAccessTokensWithNoLocks(filterByClientId)
                .Where(item => item.HomeAccountId.Equals(account.HomeAccountId.Identifier, StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (MsalAccessTokenCacheItem accessTokenCacheItem in allAccessTokens)
            {
                _accessor.DeleteAccessToken(accessTokenCacheItem.GetKey());
            }

            requestContext.Logger.Info("Deleted access token count - " + allAccessTokens.Count);

            var allIdTokens = GetAllIdTokensWithNoLocks(filterByClientId)
                .Where(item => item.HomeAccountId.Equals(account.HomeAccountId.Identifier, StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (MsalIdTokenCacheItem idTokenCacheItem in allIdTokens)
            {
                _accessor.DeleteIdToken(idTokenCacheItem.GetKey());
            }

            requestContext.Logger.Info("Deleted Id token count - " + allIdTokens.Count);

            _accessor.GetAllAccounts()
                .Where(item => item.HomeAccountId.Equals(account.HomeAccountId.Identifier, StringComparison.OrdinalIgnoreCase) &&
                               item.PreferredUsername.Equals(account.Username, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .ForEach(accItem => _accessor.DeleteAccount(accItem.GetKey()));
        }

        async Task ITokenCacheInternal.ClearAsync()
        {
            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                TokenCacheNotificationArgs args = new TokenCacheNotificationArgs(this, ClientId, null, true, (this as ITokenCacheInternal).IsApplicationCache);

                await (this as ITokenCacheInternal).OnBeforeAccessAsync(args).ConfigureAwait(false);
                try
                {
                    await (this as ITokenCacheInternal).OnBeforeWriteAsync(args).ConfigureAwait(false);

                    ((ITokenCacheInternal)this).ClearMsalCache();
                    ((ITokenCacheInternal)this).ClearAdalCache();
                }
                finally
                {
                    await (this as ITokenCacheInternal).OnAfterAccessAsync(args).ConfigureAwait(false);
#pragma warning disable CS0618 // Type or member is obsolete
                    HasStateChanged = false;
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        void ITokenCacheInternal.ClearAdalCache()
        {
            IDictionary<AdalTokenCacheKey, AdalResultWrapper> dictionary = AdalCacheOperations.Deserialize(Logger, LegacyCachePersistence.LoadCache());
            dictionary.Clear();
            LegacyCachePersistence.WriteCache(AdalCacheOperations.Serialize(Logger, dictionary));
        }

        void ITokenCacheInternal.ClearMsalCache()
        {
            _accessor.Clear();
        }
    }
}
