// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AuthScheme.Bearer;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// IMPORTANT: this class is perf critical; any changes must be benchmarked using Microsoft.Identity.Test.Performace.
    /// More information about how to test and what data to look for is in https://aka.ms/msal-net-performance-testing.
    /// </summary>
    public sealed partial class TokenCache : ITokenCacheInternal
    {
        async Task<Tuple<MsalAccessTokenCacheItem, MsalIdTokenCacheItem>> ITokenCacheInternal.SaveTokenResponseAsync(
            AuthenticationRequestParameters requestParams,
            MsalTokenResponse response)
        {
            MsalAccessTokenCacheItem msalAccessTokenCacheItem = null;
            MsalRefreshTokenCacheItem msalRefreshTokenCacheItem = null;
            MsalIdTokenCacheItem msalIdTokenCacheItem = null;
            MsalAccountCacheItem msalAccountCacheItem = null;

            IdToken idToken = IdToken.Parse(response.IdToken);
            if (idToken == null)
            {
                requestParams.RequestContext.Logger.Info("ID Token not present in response. ");
            }

            var tenantId = Authority
                .CreateAuthority(requestParams.TenantUpdatedCanonicalAuthority.AuthorityInfo.CanonicalAuthority)
                .TenantId;

            bool isAdfsAuthority = requestParams.AuthorityInfo.AuthorityType == AuthorityType.Adfs;
            string preferredUsername = GetPreferredUsernameFromIdToken(isAdfsAuthority, idToken);
            string username = isAdfsAuthority ? idToken?.Upn : preferredUsername;
            string homeAccountId = GetHomeAccountId(requestParams, response, idToken);
            string suggestedWebCacheKey = SuggestedWebCacheKeyFactory.GetKeyFromResponse(requestParams, homeAccountId);

            // Do a full instance discovery when saving tokens (if not cached),
            // so that the PreferredNetwork environment is up to date.
            var instanceDiscoveryMetadata = await ServiceBundle.InstanceDiscoveryManager
                                .GetMetadataEntryAsync(
                                    requestParams.TenantUpdatedCanonicalAuthority.AuthorityInfo.CanonicalAuthority,
                                    requestParams.RequestContext)
                                .ConfigureAwait(false);

            #region Create Cache Objects
            if (!string.IsNullOrEmpty(response.AccessToken))
            {
                msalAccessTokenCacheItem =
                    new MsalAccessTokenCacheItem(
                        instanceDiscoveryMetadata.PreferredCache,
                        requestParams.ClientId,
                        response,
                        tenantId,
                        homeAccountId,
                        requestParams.AuthenticationScheme.KeyId)
                    {
                        UserAssertionHash = requestParams.UserAssertion?.AssertionHash,
                        IsAdfs = isAdfsAuthority
                    };
            }

            if (!string.IsNullOrEmpty(response.RefreshToken))
            {
                msalRefreshTokenCacheItem = new MsalRefreshTokenCacheItem(
                                    instanceDiscoveryMetadata.PreferredCache,
                                    requestParams.ClientId,
                                    response,
                                    homeAccountId);

                if (!_featureFlags.IsFociEnabled)
                {
                    msalRefreshTokenCacheItem.FamilyId = null;
                }
            }

            if (idToken != null)
            {
                msalIdTokenCacheItem = new MsalIdTokenCacheItem(
                    instanceDiscoveryMetadata.PreferredCache,
                    requestParams.ClientId,
                    response,
                    tenantId,
                    homeAccountId)
                {
                    IsAdfs = isAdfsAuthority
                };

                Dictionary<string, string> wamAccountIds = GetWamAccountIds(requestParams, response);

                msalAccountCacheItem = new MsalAccountCacheItem(
                             instanceDiscoveryMetadata.PreferredCache,
                             response.ClientInfo,
                             homeAccountId,
                             idToken,
                             preferredUsername,
                             tenantId,
                             wamAccountIds);
            }

            #endregion

            Account account = new Account(
                    homeAccountId,
                    username,
                    instanceDiscoveryMetadata.PreferredCache);

            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
#pragma warning disable CS0618 // Type or member is obsolete
                HasStateChanged = true;
#pragma warning restore CS0618 // Type or member is obsolete

                try
                {
                    ITokenCacheInternal tokenCacheInternal = this;
                    if (tokenCacheInternal.IsTokenCacheSerialized())
                    {
                        var args = new TokenCacheNotificationArgs(
                            this,
                            ClientId,
                            account,
                            hasStateChanged: true,
                            tokenCacheInternal.IsApplicationCache,
                            hasTokens: tokenCacheInternal.HasTokensNoLocks(),
                            suggestedCacheKey: suggestedWebCacheKey);

                        await tokenCacheInternal.OnBeforeAccessAsync(args).ConfigureAwait(false);
                        await tokenCacheInternal.OnBeforeWriteAsync(args).ConfigureAwait(false);
                    }

                    if (msalAccessTokenCacheItem != null)
                    {
                        requestParams.RequestContext.Logger.Info("Saving AT in cache and removing overlapping ATs...");

                        DeleteAccessTokensWithIntersectingScopes(
                            requestParams,
                            instanceDiscoveryMetadata.Aliases,
                            tenantId,
                            msalAccessTokenCacheItem.ScopeSet,
                            msalAccessTokenCacheItem.HomeAccountId,
                            msalAccessTokenCacheItem.TokenType);

                        _accessor.SaveAccessToken(msalAccessTokenCacheItem);
                    }

                    if (idToken != null)
                    {
                        requestParams.RequestContext.Logger.Info("Saving Id Token and Account in cache ...");
                        _accessor.SaveIdToken(msalIdTokenCacheItem);
                        MergeWamAccountIds(msalAccountCacheItem);
                        _accessor.SaveAccount(msalAccountCacheItem);
                    }

                    // if server returns the refresh token back, save it in the cache.
                    if (msalRefreshTokenCacheItem != null)
                    {
                        requestParams.RequestContext.Logger.Info("Saving RT in cache...");
                        _accessor.SaveRefreshToken(msalRefreshTokenCacheItem);
                    }

                    UpdateAppMetadata(requestParams.ClientId, instanceDiscoveryMetadata.PreferredCache, response.FamilyId);

                    // Do not save RT in ADAL cache for client credentials flow or B2C                        
                    if (ServiceBundle.Config.AdalCacheCompatibilityEnabled &&
                        !requestParams.IsClientCredentialRequest &&
                        requestParams.AuthorityInfo.AuthorityType != AuthorityType.B2C)
                    {
                        var authorityWithPreferredCache = Authority.CreateAuthorityWithEnvironment(
                                requestParams.TenantUpdatedCanonicalAuthority.AuthorityInfo,
                                instanceDiscoveryMetadata.PreferredCache);

                        CacheFallbackOperations.WriteAdalRefreshToken(
                            Logger,
                            LegacyCachePersistence,
                            msalRefreshTokenCacheItem,
                            msalIdTokenCacheItem,
                            authorityWithPreferredCache.AuthorityInfo.CanonicalAuthority,
                            msalIdTokenCacheItem.IdToken.ObjectId,
                            response.Scope);
                    }
                }
                finally
                {
                    ITokenCacheInternal tokenCacheInternal = this;
                    if (tokenCacheInternal.IsTokenCacheSerialized())
                    {
                        var args = new TokenCacheNotificationArgs(
                            this,
                            ClientId,
                            account,
                            hasStateChanged: true,
                            tokenCacheInternal.IsApplicationCache,
                            tokenCacheInternal.HasTokensNoLocks(),
                            suggestedCacheKey: suggestedWebCacheKey);

                        await tokenCacheInternal.OnAfterAccessAsync(args).ConfigureAwait(false);
                    }
#pragma warning disable CS0618 // Type or member is obsolete
                    HasStateChanged = false;
#pragma warning restore CS0618 // Type or member is obsolete
                }

                return Tuple.Create(msalAccessTokenCacheItem, msalIdTokenCacheItem);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private void MergeWamAccountIds(MsalAccountCacheItem msalAccountCacheItem)
        {
            var existingAccount = _accessor.GetAllAccounts()
                .SingleOrDefault(
                    acc => string.Equals(
                        acc.GetKey().ToString(),
                        msalAccountCacheItem.GetKey().ToString(),
                        StringComparison.OrdinalIgnoreCase));
            var existingWamAccountIds = existingAccount?.WamAccountIds;
            msalAccountCacheItem.WamAccountIds.MergeDifferentEntries(existingWamAccountIds);
        }

        private static Dictionary<string, string> GetWamAccountIds(AuthenticationRequestParameters requestParams, MsalTokenResponse response)
        {
            if (!string.IsNullOrEmpty(response.WamAccountId))
            {
                return new Dictionary<string, string>() { { requestParams.ClientId, response.WamAccountId } };
            }

            return new Dictionary<string, string>();
        }

        private static string GetHomeAccountId(AuthenticationRequestParameters requestParams, MsalTokenResponse response, IdToken idToken)
        {
            string subject = idToken?.Subject;
            if (idToken?.Subject != null)
            {
                requestParams.RequestContext.Logger.Info("Subject not present in Id token");
            }

            ClientInfo clientInfo = response.ClientInfo != null ? ClientInfo.CreateFromJson(response.ClientInfo) : null;
            string homeAccountId = clientInfo?.ToAccountIdentifier() ?? subject; // ADFS does not have client info, so we use subject
            return homeAccountId;
        }

        private static string GetPreferredUsernameFromIdToken(bool isAdfsAuthority, IdToken idToken)
        {
            // The preferred_username value cannot be null or empty in order to comply with the ADAL/MSAL Unified cache schema.
            // It will be set to "preferred_username not in id token"
            if (idToken == null)
            {
                return NullPreferredUsernameDisplayLabel;
            }

            if (string.IsNullOrWhiteSpace(idToken.PreferredUsername))
            {
                if (isAdfsAuthority)
                {
                    //The direct to ADFS scenario does not return preferred_username in the id token so it needs to be set to the UPN
                    return !string.IsNullOrEmpty(idToken.Upn)
                        ? idToken.Upn
                        : NullPreferredUsernameDisplayLabel;
                }
                return NullPreferredUsernameDisplayLabel;
            }

            return idToken.PreferredUsername;
        }

        /// <summary>
        /// IMPORTANT: this class is perf critical; any changes must be benchmarked using Microsoft.Identity.Test.Performace.
        /// More information about how to test and what data to look for is in https://aka.ms/msal-net-performance-testing.
        /// 
        /// Scenario: client_creds with default in-memory cache can get to ~500k tokens
        /// </summary>
        async Task<MsalAccessTokenCacheItem> ITokenCacheInternal.FindAccessTokenAsync(
            AuthenticationRequestParameters requestParams)
        {
            var logger = requestParams.RequestContext.Logger;
            // no authority passed
            if (requestParams.AuthorityInfo?.CanonicalAuthority == null)
            {
                logger.Warning("No authority provided. Skipping cache lookup ");
                return null;
            }

            logger.Verbose("Looking up access token in the cache.");
            // take a snapshot of the access tokens to avoid problems where the underlying collection is changed,
            // as this method is NOT locked by the semaphore
            IEnumerable<MsalAccessTokenCacheItem> tokenCacheItems = GetAllAccessTokensWithNoLocks(true).ToList();

            tokenCacheItems = FilterByHomeAccountTenantOrAssertion(requestParams, tokenCacheItems);
            tokenCacheItems = FilterByTokenType(requestParams, tokenCacheItems);
            tokenCacheItems = FilterByScopes(requestParams, tokenCacheItems);
            tokenCacheItems = await FilterByEnvironmentAsync(requestParams, tokenCacheItems).ConfigureAwait(false);

            // perf: take a snapshot as calling Count(), Any() etc. on the IEnumerable evaluates it each time
            IReadOnlyList<MsalAccessTokenCacheItem> finalList = tokenCacheItems.ToList();

            // no match
            if (finalList.Count == 0)
            {
                logger.Verbose("No tokens found for matching authority, client_id, user and scopes.");
                return null;
            }

            MsalAccessTokenCacheItem msalAccessTokenCacheItem = GetSingleResult(requestParams, finalList);
            msalAccessTokenCacheItem = FilterByKeyId(msalAccessTokenCacheItem, requestParams);

            return FilterByExpiry(msalAccessTokenCacheItem, requestParams);
        }

        private static IEnumerable<MsalAccessTokenCacheItem> FilterByScopes(
            AuthenticationRequestParameters requestParams,
            IEnumerable<MsalAccessTokenCacheItem> tokenCacheItems)
        {
            var requestScopes = requestParams.Scope.Where(s =>
                !OAuth2Value.ReservedScopes.Contains(s));

            tokenCacheItems = tokenCacheItems.FilterWithLogging(
                item => ScopeHelper.ScopeContains(item.ScopeSet, requestScopes),
                requestParams.RequestContext.Logger,
                "Filtering by scopes");

            return tokenCacheItems;
        }

        private static IEnumerable<MsalAccessTokenCacheItem> FilterByTokenType(AuthenticationRequestParameters requestParams, IEnumerable<MsalAccessTokenCacheItem> tokenCacheItems)
        {
            tokenCacheItems = tokenCacheItems.FilterWithLogging(item =>
                            string.Equals(
                                item.TokenType ?? BearerAuthenticationScheme.BearerTokenType,
                                requestParams.AuthenticationScheme.AccessTokenType,
                                StringComparison.OrdinalIgnoreCase),
                            requestParams.RequestContext.Logger,
                            "Filtering by token type");
            return tokenCacheItems;
        }

        private static IEnumerable<MsalAccessTokenCacheItem> FilterByHomeAccountTenantOrAssertion(
            AuthenticationRequestParameters requestParams,
            IEnumerable<MsalAccessTokenCacheItem> tokenCacheItems)
        {
            string requestTenantId = requestParams.Authority.TenantId;
            bool filterByTenantId = true;

            if (requestParams.UserAssertion != null) // OBO
            {
                tokenCacheItems = tokenCacheItems.FilterWithLogging(item =>
                                !string.IsNullOrEmpty(item.UserAssertionHash) &&
                                item.UserAssertionHash.Equals(requestParams.UserAssertion.AssertionHash, StringComparison.OrdinalIgnoreCase),
                                requestParams.RequestContext.Logger,
                                "Filtering by user assertion id");

                // OBO calls FindAccessTokenAsync directly, but we are not able to resolve the authority 
                // unless the developer has configured a tenanted authority. If they have configured /common
                // then we cannot filter by tenant and will use whatever is in the cache.
                filterByTenantId =
                    !string.IsNullOrEmpty(requestTenantId) &&
                    !AadAuthority.IsCommonOrganizationsOrConsumersTenant(requestTenantId);
            }

            if (filterByTenantId)
            {
                tokenCacheItems = tokenCacheItems.FilterWithLogging(item =>
                    string.Equals(item.TenantId ?? string.Empty, requestTenantId ?? string.Empty, StringComparison.OrdinalIgnoreCase),
                    requestParams.RequestContext.Logger,
                    "Filtering by tenant id");
            }
            else
            {
                requestParams.RequestContext.Logger.Warning("Have not filtered by tenant ID. " +
                    "This can happen in OBO scenario where authority is /common or /organizations. " +
                    "Please use tenanted authority.");
            }

            // Only AcquireTokenSilent has an IAccount in the request that can be used for filtering
            if (requestParams.ApiId != TelemetryCore.Internal.Events.ApiEvent.ApiIds.AcquireTokenForClient &&
                requestParams.ApiId != TelemetryCore.Internal.Events.ApiEvent.ApiIds.AcquireTokenOnBehalfOf)
            {
                tokenCacheItems = tokenCacheItems.FilterWithLogging(item => item.HomeAccountId.Equals(
                                requestParams.Account.HomeAccountId?.Identifier, StringComparison.OrdinalIgnoreCase),
                                requestParams.RequestContext.Logger,
                                "Filtering by home account id");
            }

            return tokenCacheItems;
        }

        private MsalAccessTokenCacheItem FilterByExpiry(MsalAccessTokenCacheItem msalAccessTokenCacheItem, AuthenticationRequestParameters requestParams)
        {
            var logger = requestParams.RequestContext.Logger;
            if (msalAccessTokenCacheItem != null)
            {

                if (msalAccessTokenCacheItem.ExpiresOn > DateTime.UtcNow + AccessTokenExpirationBuffer)
                {
                    // due to https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1806
                    if (msalAccessTokenCacheItem.ExpiresOn > DateTime.UtcNow + TimeSpan.FromDays(ExpirationTooLongInDays))
                    {
                        logger.Error(
                           "Access token expiration too large. This can be the result of a bug or corrupt cache. Token will be ignored as it is likely expired." +
                           GetAccessTokenExpireLogMessageContent(msalAccessTokenCacheItem));
                        return null;
                    }

                    if (logger.IsLoggingEnabled(LogLevel.Info))
                    {
                        logger.Info(
                            "Access token is not expired. Returning the found cache entry. " +
                            GetAccessTokenExpireLogMessageContent(msalAccessTokenCacheItem));
                    }

                    return msalAccessTokenCacheItem;
                }

                if (ServiceBundle.Config.IsExtendedTokenLifetimeEnabled &&
                    msalAccessTokenCacheItem.ExtendedExpiresOn > DateTime.UtcNow + AccessTokenExpirationBuffer)
                {
                    if (logger.IsLoggingEnabled(LogLevel.Info))
                    {
                        logger.Info(
                            "Access token is expired.  IsExtendedLifeTimeEnabled=TRUE and ExtendedExpiresOn is not exceeded.  Returning the found cache entry. " +
                            GetAccessTokenExpireLogMessageContent(msalAccessTokenCacheItem));
                    }

                    msalAccessTokenCacheItem.IsExtendedLifeTimeToken = true;
                    return msalAccessTokenCacheItem;
                }

                if (logger.IsLoggingEnabled(LogLevel.Info))
                {
                    logger.Info(
                        "Access token has expired or about to expire. " +
                        GetAccessTokenExpireLogMessageContent(msalAccessTokenCacheItem));
                }
            }

            return null;
        }

        private static MsalAccessTokenCacheItem GetSingleResult(
            AuthenticationRequestParameters requestParams,
            IReadOnlyList<MsalAccessTokenCacheItem> filteredItems)
        {
            // if only one cached token found
            if (filteredItems.Count == 1)
            {
                return filteredItems[0];
            }

            requestParams.RequestContext.Logger.Error("Multiple tokens found for matching authority, client_id, user and scopes. ");
            throw new MsalClientException(
                MsalError.MultipleTokensMatchedError,
                MsalErrorMessage.MultipleTokensMatched);
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

            if (string.Equals(item.KeyId, requestKid, StringComparison.OrdinalIgnoreCase))
            {
                authenticationRequest.RequestContext.Logger.Verbose("Keyed token found");
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

            requestParams.RequestContext.Logger.Info("Checking ADAL cache for matching RT. ");

            // ADAL legacy cache does not store FRTs
            if (ServiceBundle.Config.AdalCacheCompatibilityEnabled &&
                requestParams.Account != null &&
                string.IsNullOrEmpty(familyId))
            {
                return CacheFallbackOperations.GetRefreshToken(
                    Logger,
                    LegacyCachePersistence,
                    aliases,
                    requestParams.ClientId,
                    requestParams.Account);
            }

            return null;
        }

        async Task<bool?> ITokenCacheInternal.IsFociMemberAsync(AuthenticationRequestParameters requestParams, string familyId)
        {
            var logger = requestParams.RequestContext.Logger;
            if (requestParams?.AuthorityInfo?.CanonicalAuthority == null)
            {
                logger.Warning("No authority details, can't check app metadata. Returning unknown. ");
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
                logger.Warning("No app metadata found. Returning unknown. ");
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
        async Task<IEnumerable<IAccount>> ITokenCacheInternal.GetAccountsAsync(AuthenticationRequestParameters requestParameters)
        {
            var logger = requestParameters.RequestContext.Logger;
            var environment = Authority.GetEnviroment(requestParameters.AuthorityInfo.CanonicalAuthority);
            bool filterByClientId = !_featureFlags.IsFociEnabled;

            IEnumerable<MsalRefreshTokenCacheItem> rtCacheItems = GetAllRefreshTokensWithNoLocks(filterByClientId);
            IEnumerable<MsalAccountCacheItem> accountCacheItems = _accessor.GetAllAccounts();

            if (logger.IsLoggingEnabled(LogLevel.Verbose))
                logger.Verbose($"GetAccounts found {rtCacheItems.Count()} RTs and {accountCacheItems.Count()} accounts in MSAL cache. ");

            // Multi-cloud support - must filter by env.
            ISet<string> allEnvironmentsInCache = new HashSet<string>(
                accountCacheItems.Select(aci => aci.Environment),
                StringComparer.OrdinalIgnoreCase);
            allEnvironmentsInCache.UnionWith(rtCacheItems.Select(rt => rt.Environment));

            AdalUsersForMsal adalUsersResult = null;

            if (ServiceBundle.Config.AdalCacheCompatibilityEnabled)
            {
                adalUsersResult = CacheFallbackOperations.GetAllAdalUsersForMsal(
                    Logger,
                    LegacyCachePersistence,
                    ClientId);
                allEnvironmentsInCache.UnionWith(adalUsersResult.GetAdalUserEnviroments());
            }

            var instanceMetadata = await ServiceBundle.InstanceDiscoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                requestParameters.AuthorityInfo.CanonicalAuthority,
                allEnvironmentsInCache,
                requestParameters.RequestContext).ConfigureAwait(false);

            rtCacheItems = rtCacheItems.Where(rt => instanceMetadata.Aliases.ContainsOrdinalIgnoreCase(rt.Environment));
            accountCacheItems = accountCacheItems.Where(acc => instanceMetadata.Aliases.ContainsOrdinalIgnoreCase(acc.Environment));

            if (logger.IsLoggingEnabled(LogLevel.Verbose))
                logger.Verbose($"GetAccounts found {rtCacheItems.Count()} RTs and {accountCacheItems.Count()} accounts in MSAL cache after environment filtering. ");

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

            if (ServiceBundle.Config.AdalCacheCompatibilityEnabled)
            {
                UpdateMapWithAdalAccountsWithClientInfo(
                    environment,
                    instanceMetadata.Aliases,
                    adalUsersResult,
                    clientInfoToAccountMap);
            }

            // Add WAM accounts stored in MSAL's cache - for which we do not have an RT
            if (requestParameters.IsBrokerConfigured && ServiceBundle.PlatformProxy.BrokerSupportsWamAccounts)
            {
                foreach (MsalAccountCacheItem wamAccountCache in accountCacheItems.Where(
                    acc => acc.WamAccountIds != null &&
                    acc.WamAccountIds.ContainsKey(requestParameters.ClientId)))
                {
                    var wamAccount = new Account(
                        wamAccountCache.HomeAccountId,
                        wamAccountCache.PreferredUsername,
                        environment,
                        wamAccountCache.WamAccountIds);

                    clientInfoToAccountMap[wamAccountCache.HomeAccountId] = wamAccount;
                }
            }

            IEnumerable<IAccount> accounts = UpdateWithAdalAccountsWithoutClientInfo(environment,
                instanceMetadata.Aliases,
                adalUsersResult,
                clientInfoToAccountMap);

            if (!string.IsNullOrEmpty(requestParameters.HomeAccountId))
            {
                accounts = accounts.Where(acc => acc.HomeAccountId.Identifier.Equals(
                    requestParameters.HomeAccountId,
                    StringComparison.OrdinalIgnoreCase));

                if (logger.IsLoggingEnabled(LogLevel.Verbose))
                    logger.Verbose($"Filtered by home account id. Remaining accounts {accounts.Count()} ");
            }

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

                ITokenCacheInternal tokenCacheInternal = this;

                try
                {
                    if (tokenCacheInternal.IsTokenCacheSerialized())
                    {
                        var args = new TokenCacheNotificationArgs(
                            this,
                            ClientId,
                            account,
                            true,
                            tokenCacheInternal.IsApplicationCache,
                            tokenCacheInternal.HasTokensNoLocks(),
                            account.HomeAccountId.Identifier);

                        await tokenCacheInternal.OnBeforeAccessAsync(args).ConfigureAwait(false);
                        await tokenCacheInternal.OnBeforeWriteAsync(args).ConfigureAwait(false);
                    }

                    tokenCacheInternal.RemoveMsalAccountWithNoLocks(account, requestContext);
                    if (ServiceBundle.Config.AdalCacheCompatibilityEnabled)
                    {
                        RemoveAdalUser(account);
                    }
                }
                finally
                {
                    if (tokenCacheInternal.IsTokenCacheSerialized())
                    {
                        var afterAccessArgs = new TokenCacheNotificationArgs(
                            this,
                            ClientId,
                            account,
                            true,
                            tokenCacheInternal.IsApplicationCache,
                            hasTokens: tokenCacheInternal.HasTokensNoLocks(),
                            account.HomeAccountId.Identifier);

                        await tokenCacheInternal.OnAfterAccessAsync(afterAccessArgs).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
#pragma warning disable CS0618 // Type or member is obsolete
                HasStateChanged = false;
#pragma warning restore CS0618 // Type or member is obsolete

                _semaphoreSlim.Release();
            }
        }

        bool ITokenCacheInternal.HasTokensNoLocks()
        {
            return _accessor.GetAllRefreshTokens().Any() ||
                _accessor.GetAllAccessTokens().Any(at => !IsAtExpired(at));
        }

        private bool IsAtExpired(MsalAccessTokenCacheItem at)
        {
            return at.ExpiresOn < DateTime.UtcNow + AccessTokenExpirationBuffer;
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
    }
}
