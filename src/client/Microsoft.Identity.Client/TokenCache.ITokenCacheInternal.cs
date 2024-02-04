// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.AuthScheme.Bearer;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.Instance.Discovery;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;
using Microsoft.Identity.Client.TelemetryCore.TelemetryClient;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// IMPORTANT: this class is performance critical; any changes must be benchmarked using Microsoft.Identity.Test.Performance.
    /// More information about how to test and what data to look for is in https://aka.ms/msal-net-performance-testing.
    /// </summary>
    public sealed partial class TokenCache : ITokenCacheInternal
    {
        #region SaveTokenResponse
        async Task<Tuple<MsalAccessTokenCacheItem, MsalIdTokenCacheItem, Account>> ITokenCacheInternal.SaveTokenResponseAsync(
            AuthenticationRequestParameters requestParams,
            MsalTokenResponse response)
        {
            var logger = requestParams.RequestContext.Logger;
            response.Log(logger, LogLevel.Verbose);

            MsalAccessTokenCacheItem msalAccessTokenCacheItem = null;
            MsalRefreshTokenCacheItem msalRefreshTokenCacheItem = null;
            MsalIdTokenCacheItem msalIdTokenCacheItem = null;
            MsalAccountCacheItem msalAccountCacheItem = null;

            IdToken idToken = IdToken.Parse(response.IdToken);
            if (idToken == null)
            {
                logger.Info("[SaveTokenResponseAsync] ID Token not present in response. ");
            }

            var tenantId = TokenResponseHelper.GetTenantId(idToken, requestParams);
            string username = TokenResponseHelper.GetUsernameFromIdToken(idToken);
            string homeAccountId = TokenResponseHelper.GetHomeAccountId(requestParams, response, idToken);

            string suggestedWebCacheKey = CacheKeyFactory.GetExternalCacheKeyFromResponse(requestParams, homeAccountId);

            // token could be coming from a different cloud than the one configured
            if (requestParams.AppConfig.MultiCloudSupportEnabled && !string.IsNullOrEmpty(response.AuthorityUrl))
            {
                var url = new Uri(response.AuthorityUrl);
                requestParams.AuthorityManager = new AuthorityManager(
                    requestParams.RequestContext,
                    Authority.CreateAuthorityWithEnvironment(requestParams.Authority.AuthorityInfo, url.Host));
            }

            // Do a full instance discovery when saving tokens (if not cached),
            // so that the PreferredNetwork environment is up to date.
            InstanceDiscoveryMetadataEntry instanceDiscoveryMetadata =
                await requestParams.AuthorityManager.GetInstanceDiscoveryEntryAsync().ConfigureAwait(false);

            #region Create Cache Objects
            if (!string.IsNullOrEmpty(response.AccessToken))
            {
                msalAccessTokenCacheItem =
                    new MsalAccessTokenCacheItem(
                        instanceDiscoveryMetadata.PreferredCache,
                        requestParams.AppConfig.ClientId,
                        response,
                        tenantId,
                        homeAccountId,
                        requestParams.AuthenticationScheme.KeyId,
                        CacheKeyFactory.GetOboKey(requestParams.LongRunningOboCacheKey, requestParams.UserAssertion));
            }

            if (!string.IsNullOrEmpty(response.RefreshToken))
            {
                Debug.Assert(
                    requestParams.ApiId != ApiEvent.ApiIds.AcquireTokenForClient,
                    "client_credentials flow should not receive a refresh token");

                Debug.Assert(
                    (requestParams.ApiId != ApiEvent.ApiIds.AcquireTokenForSystemAssignedManagedIdentity || 
                    requestParams.ApiId != ApiEvent.ApiIds.AcquireTokenForUserAssignedManagedIdentity),
                    "Managed identity flow should not receive a refresh token");

                msalRefreshTokenCacheItem = new MsalRefreshTokenCacheItem(
                                    instanceDiscoveryMetadata.PreferredCache,
                                    requestParams.AppConfig.ClientId,
                                    response,
                                    homeAccountId)
                {
                    OboCacheKey = CacheKeyFactory.GetOboKey(requestParams.LongRunningOboCacheKey, requestParams.UserAssertion),
                };

                if (!_featureFlags.IsFociEnabled)
                {
                    msalRefreshTokenCacheItem.FamilyId = null;
                }
            }

            Account account = null;
            if (idToken != null)
            {
                Debug.Assert(
                    requestParams.ApiId != ApiEvent.ApiIds.AcquireTokenForClient,
                    "client_credentials flow should not receive an ID token");

                Debug.Assert(
                    (requestParams.ApiId != ApiEvent.ApiIds.AcquireTokenForSystemAssignedManagedIdentity ||
                    requestParams.ApiId != ApiEvent.ApiIds.AcquireTokenForUserAssignedManagedIdentity),
                    "Managed identity flow should not receive an ID token");

                msalIdTokenCacheItem = new MsalIdTokenCacheItem(
                    instanceDiscoveryMetadata.PreferredCache,
                    requestParams.AppConfig.ClientId,
                    response,
                    tenantId,
                    homeAccountId);

                Dictionary<string, string> wamAccountIds = TokenResponseHelper.GetWamAccountIds(requestParams, response);
                msalAccountCacheItem = new MsalAccountCacheItem(
                             instanceDiscoveryMetadata.PreferredCache,
                             response.ClientInfo,
                             homeAccountId,
                             idToken,
                             username,
                             tenantId,
                             wamAccountIds);

                // Add the newly obtained id token to the list of profiles
                IDictionary<string, TenantProfile> tenantProfiles = null;
                if (msalIdTokenCacheItem.TenantId != null)
                {
                    tenantProfiles = await GetTenantProfilesAsync(requestParams, homeAccountId).ConfigureAwait(false);
                    if (tenantProfiles != null)
                    {
                        TenantProfile tenantProfile = new TenantProfile(msalIdTokenCacheItem);
                        tenantProfiles[msalIdTokenCacheItem.TenantId] = tenantProfile;
                    }
                }

                account = new Account(
                  homeAccountId,
                  username,
                  instanceDiscoveryMetadata.PreferredNetwork,
                  wamAccountIds,
                  tenantProfiles?.Values);
            }

            #endregion

            logger.Verbose(() => $"[SaveTokenResponseAsync] Entering token cache semaphore. Count {_semaphoreSlim.GetCurrentCountLogMessage()}.");
            await _semaphoreSlim.WaitAsync(requestParams.RequestContext.UserCancellationToken).ConfigureAwait(false);
            logger.Verbose(() => "[SaveTokenResponseAsync] Entered token cache semaphore. ");
            ITokenCacheInternal tokenCacheInternal = this;

            try
            {
#pragma warning disable CS0618 // Type or member is obsolete
                HasStateChanged = true;
#pragma warning restore CS0618 // Type or member is obsolete

                try
                {
                    if (tokenCacheInternal.IsAppSubscribedToSerializationEvents())
                    {
                        var args = new TokenCacheNotificationArgs(
                            tokenCache: this,
                            clientId: ClientId,
                            account: account,
                            hasStateChanged: true,
                            tokenCacheInternal.IsApplicationCache,
                            suggestedCacheKey: suggestedWebCacheKey,
                            hasTokens: tokenCacheInternal.HasTokensNoLocks(),
                            suggestedCacheExpiry: null,
                            cancellationToken: requestParams.RequestContext.UserCancellationToken,
                            correlationId: requestParams.RequestContext.CorrelationId,
                            requestScopes: requestParams.Scope,
                            requestTenantId: requestParams.AuthorityManager.OriginalAuthority.TenantId,
                            identityLogger: requestParams.RequestContext.Logger.IdentityLogger,
                            piiLoggingEnabled: requestParams.RequestContext.Logger.PiiLoggingEnabled);

                        Stopwatch sw = Stopwatch.StartNew();

                        await tokenCacheInternal.OnBeforeAccessAsync(args).ConfigureAwait(false);
                        await tokenCacheInternal.OnBeforeWriteAsync(args).ConfigureAwait(false);
                        requestParams.RequestContext.ApiEvent.DurationInCacheInMs += sw.ElapsedMilliseconds;
                    }

                    // Don't cache access tokens from broker
                    if (ShouldCacheAccessToken(msalAccessTokenCacheItem, response.TokenSource))
                    {
                        logger.Info("[SaveTokenResponseAsync] Saving AT in cache and removing overlapping ATs...");
                        DeleteAccessTokensWithIntersectingScopes(
                            requestParams,
                            instanceDiscoveryMetadata.Aliases,
                            tenantId,
                            msalAccessTokenCacheItem.ScopeSet,
                            msalAccessTokenCacheItem.HomeAccountId,
                            msalAccessTokenCacheItem.TokenType);

                        Accessor.SaveAccessToken(msalAccessTokenCacheItem);
                    }

                    if (idToken != null)
                    {
                        logger.Info("[SaveTokenResponseAsync] Saving Id Token and Account in cache ...");
                        Accessor.SaveIdToken(msalIdTokenCacheItem);
                        MergeWamAccountIds(msalAccountCacheItem);
                        Accessor.SaveAccount(msalAccountCacheItem);
                    }

                    // if server returns the refresh token back, save it in the cache.
                    if (msalRefreshTokenCacheItem != null)
                    {
                        logger.Info("[SaveTokenResponseAsync] Saving RT in cache...");
                        Accessor.SaveRefreshToken(msalRefreshTokenCacheItem);
                    }

                    UpdateAppMetadata(
                        requestParams.AppConfig.ClientId,
                        instanceDiscoveryMetadata.PreferredCache,
                        response.FamilyId);

                    SaveToLegacyAdalCache(
                        requestParams,
                        response,
                        msalRefreshTokenCacheItem,
                        msalIdTokenCacheItem,
                        tenantId,
                        instanceDiscoveryMetadata);
                }
                finally
                {
                    if (tokenCacheInternal.IsAppSubscribedToSerializationEvents())
                    {
                        DateTimeOffset? cacheExpiry = CalculateSuggestedCacheExpiry(Accessor, logger);

                        var args = new TokenCacheNotificationArgs(
                            tokenCache: this,
                            clientId: ClientId,
                            account: account,
                            hasStateChanged: true,
                            tokenCacheInternal.IsApplicationCache,
                            suggestedCacheKey: suggestedWebCacheKey,
                            hasTokens: tokenCacheInternal.HasTokensNoLocks(),
                            suggestedCacheExpiry: cacheExpiry,
                            cancellationToken: requestParams.RequestContext.UserCancellationToken,
                            correlationId: requestParams.RequestContext.CorrelationId,
                            requestScopes: requestParams.Scope,
                            requestTenantId: requestParams.AuthorityManager.OriginalAuthority.TenantId,
                            identityLogger: requestParams.RequestContext.Logger.IdentityLogger,
                            piiLoggingEnabled: requestParams.RequestContext.Logger.PiiLoggingEnabled);

                        Stopwatch sw = Stopwatch.StartNew();
                        await tokenCacheInternal.OnAfterAccessAsync(args).ConfigureAwait(false);
                        requestParams.RequestContext.ApiEvent.DurationInCacheInMs += sw.ElapsedMilliseconds;

                        LogCacheContents(requestParams);
                    }

#pragma warning disable CS0618 // Type or member is obsolete
                    HasStateChanged = false;
#pragma warning restore CS0618 // Type or member is obsolete
                }

                return Tuple.Create(msalAccessTokenCacheItem, msalIdTokenCacheItem, account);
            }
            finally
            {
                _semaphoreSlim.Release();
                logger.Verbose(() => "[SaveTokenResponseAsync] Released token cache semaphore. ");
            }
        }

        private static bool ShouldCacheAccessToken(MsalAccessTokenCacheItem msalAccessTokenCacheItem, TokenSource tokenSource)
        {
#if iOS
            return msalAccessTokenCacheItem != null;
#else
            return msalAccessTokenCacheItem != null && tokenSource != TokenSource.Broker;
#endif
        }

        //This method pulls all of the access and refresh tokens from the cache and can therefore be very impactful on performance.
        //This will run on a background thread to mitigate this.
        private void LogCacheContents(AuthenticationRequestParameters requestParameters)
        {

            if (requestParameters.RequestContext.Logger.IsLoggingEnabled(LogLevel.Verbose))
            {
                var accessTokenCacheItems = Accessor.GetAllAccessTokens();
                var refreshTokenCacheItems = Accessor.GetAllRefreshTokens();
                var accessTokenCacheItemsSubset = accessTokenCacheItems.Take(10).ToList();
                var refreshTokenCacheItemsSubset = refreshTokenCacheItems.Take(10).ToList();

                StringBuilder tokenCacheKeyLog = new StringBuilder();

                tokenCacheKeyLog.AppendLine($"Total number of access tokens in the cache: {accessTokenCacheItems.Count}");
                tokenCacheKeyLog.AppendLine($"Total number of refresh tokens in the cache: {refreshTokenCacheItems.Count}");

                tokenCacheKeyLog.AppendLine($"First {accessTokenCacheItemsSubset.Count} access token cache keys:");
                foreach (var cacheItem in accessTokenCacheItemsSubset)
                {
                    tokenCacheKeyLog.AppendLine($"AT Cache Key: {cacheItem.ToLogString(requestParameters.RequestContext.Logger.PiiLoggingEnabled)}");
                }

                tokenCacheKeyLog.AppendLine($"First {refreshTokenCacheItemsSubset.Count} refresh token cache keys:");
                foreach (var cacheItem in refreshTokenCacheItemsSubset)
                {
                    tokenCacheKeyLog.AppendLine($"RT Cache Key: {cacheItem.ToLogString(requestParameters.RequestContext.Logger.PiiLoggingEnabled)}");
                }

                requestParameters.RequestContext.Logger.Verbose(() => tokenCacheKeyLog.ToString());
            }
        }

        private bool IsLegacyAdalCacheEnabled(AuthenticationRequestParameters requestParams)
        {
            if (requestParams.IsClientCredentialRequest)
            {
                // client_credentials request. Only RTs are transferable between ADAL and MSAL
                return false;
            }

            if (ServiceBundle.PlatformProxy.LegacyCacheRequiresSerialization &&
               !(this as ITokenCacheInternal).IsExternalSerializationConfiguredByUser())
            {
                // serialization is not configured but is required
                return false;
            }

            if (!ServiceBundle.Config.LegacyCacheCompatibilityEnabled)
            {
                // disabled by app developer
                return false;
            }

            if (requestParams.AuthorityInfo.AuthorityType == AuthorityType.B2C)
            {
                // ADAL did not support B2C
                return false;
            }

            requestParams.RequestContext.Logger.Info("IsLegacyAdalCacheEnabled: yes");
            return true;
        }

        private void SaveToLegacyAdalCache(
            AuthenticationRequestParameters requestParams,
            MsalTokenResponse response,
            MsalRefreshTokenCacheItem msalRefreshTokenCacheItem,
            MsalIdTokenCacheItem msalIdTokenCacheItem,
            string tenantId,
            InstanceDiscoveryMetadataEntry instanceDiscoveryMetadata)
        {
            if (msalRefreshTokenCacheItem?.RawClientInfo != null &&
                msalIdTokenCacheItem?.IdToken?.GetUniqueId() != null &&
                IsLegacyAdalCacheEnabled(requestParams))
            {

                var tenantedAuthority = Authority.CreateAuthorityWithTenant(requestParams.AuthorityInfo, tenantId);
                var authorityWithPreferredCache = Authority.CreateAuthorityWithEnvironment(
                        tenantedAuthority.AuthorityInfo,
                        instanceDiscoveryMetadata.PreferredCache);

                CacheFallbackOperations.WriteAdalRefreshToken(
                    requestParams.RequestContext.Logger,
                    LegacyCachePersistence,
                    msalRefreshTokenCacheItem,
                    msalIdTokenCacheItem,
                    authorityWithPreferredCache.AuthorityInfo.CanonicalAuthority.ToString(),
                    msalIdTokenCacheItem.IdToken.GetUniqueId(),
                    response.Scope);
            }
            else
            {
                requestParams.RequestContext.Logger.Verbose(() => "Not saving to ADAL legacy cache. ");
            }
        }

        /// <summary>
        /// Important note: we should not be suggesting expiration dates that are in the past, as it breaks some cache implementations.
        /// </summary>
        internal /* for testing */ static DateTimeOffset? CalculateSuggestedCacheExpiry(
            ITokenCacheAccessor accessor,
            ILoggerAdapter logger)
        {
            // If we have refresh tokens in the cache, we cannot suggest expiration
            // because refresh token expiration is not disclosed to SDKs and RTs are long lived anyway (3 months by default)
            if (accessor.GetAllRefreshTokens().Count == 0)
            {
                var tokenCacheItems = accessor.GetAllAccessTokens(optionalPartitionKey: null);
                if (tokenCacheItems.Count == 0)
                {
                    logger.Warning("[CalculateSuggestedCacheExpiry] No access tokens or refresh tokens found in the accessor. Not returning any expiration.");
                    return null;
                }

                DateTimeOffset cacheExpiry = tokenCacheItems.Max(item => item.ExpiresOn);

                // do not suggest an expiration date from the past or within 5 min, as tokens will not be usable anyway
                // and HasTokens will be set to false, letting implementers know to delete the cache node
                if (cacheExpiry < DateTimeOffset.UtcNow + Constants.AccessTokenExpirationBuffer)
                {
                    return null;
                }

                return cacheExpiry;
            }

            return null;
        }

        private void MergeWamAccountIds(MsalAccountCacheItem msalAccountCacheItem)
        {
            var existingAccount = Accessor.GetAccount(msalAccountCacheItem);
            var existingWamAccountIds = existingAccount?.WamAccountIds;
            msalAccountCacheItem.WamAccountIds.MergeDifferentEntries(existingWamAccountIds);
        }
        #endregion

        #region FindAccessToken
        /// <summary>
        /// IMPORTANT: this class is performance critical; any changes must be benchmarked using Microsoft.Identity.Test.Performance.
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
                logger.Warning("[FindAccessTokenAsync] No authority provided. Skipping cache lookup. ");
                return null;
            }

            // take a snapshot of the access tokens to avoid problems where the underlying collection is changed,
            // as this method is NOT locked by the semaphore
            string partitionKey = CacheKeyFactory.GetKeyFromRequest(requestParams);
            Debug.Assert(partitionKey != null || !requestParams.AppConfig.IsConfidentialClient, "On confidential client, cache must be partitioned.");

            var accessTokens = Accessor.GetAllAccessTokens(partitionKey, logger);

            requestParams.RequestContext.Logger.Always($"[FindAccessTokenAsync] Discovered {accessTokens.Count} access tokens in cache using partition key: {partitionKey}");

            if (accessTokens.Count == 0)
            {

                logger.Verbose(() => "[FindAccessTokenAsync] No access tokens found in the cache. Skipping filtering. ");
                requestParams.RequestContext.ApiEvent.CacheInfo = CacheRefreshReason.NoCachedAccessToken;

                return null;
            }

            FilterTokensByHomeAccountTenantOrAssertion(accessTokens, requestParams);
            FilterTokensByTokenType(accessTokens, requestParams);
            FilterTokensByScopes(accessTokens, requestParams);
            accessTokens = await FilterTokensByEnvironmentAsync(accessTokens, requestParams).ConfigureAwait(false);
            FilterTokensByClientId(accessTokens);

            CacheRefreshReason cacheInfoTelemetry = CacheRefreshReason.NotApplicable;

            // no match
            if (accessTokens.Count == 0)
            {
                logger.Verbose(() => "[FindAccessTokenAsync] No tokens found for matching authority, client_id, user and scopes. ");
                return null;
            }

            MsalAccessTokenCacheItem msalAccessTokenCacheItem = GetSingleToken(accessTokens, requestParams);
            msalAccessTokenCacheItem = FilterTokensByPopKeyId(msalAccessTokenCacheItem, requestParams);
            msalAccessTokenCacheItem = FilterTokensByExpiry(msalAccessTokenCacheItem, requestParams);

            if (msalAccessTokenCacheItem == null)
            {
                cacheInfoTelemetry = CacheRefreshReason.Expired;
            }

            requestParams.RequestContext.ApiEvent.CacheInfo = cacheInfoTelemetry;

            return msalAccessTokenCacheItem;
        }

        private static void FilterTokensByScopes(
            List<MsalAccessTokenCacheItem> tokenCacheItems,
            AuthenticationRequestParameters requestParams)
        {
            var logger = requestParams.RequestContext.Logger;
            if (tokenCacheItems.Count == 0)
            {
                logger.Verbose(() => "Not filtering by scopes, because there are no candidates");
                return;
            }

            var requestScopes = requestParams.Scope.Where(s =>
                !OAuth2Value.ReservedScopes.Contains(s));

            tokenCacheItems.FilterWithLogging(
                item =>
                {
                    bool accepted = ScopeHelper.ScopeContains(item.ScopeSet, requestScopes);

                    if (logger.IsLoggingEnabled(LogLevel.Verbose))
                    {
                        logger.Verbose(() => $"Access token with scopes {string.Join(" ", item.ScopeSet)} " + $"passes scope filter? {accepted} ");
                    }
                    return accepted;
                },
                logger,
                "Filtering by scopes");
        }

        private static void FilterTokensByTokenType(
            List<MsalAccessTokenCacheItem> tokenCacheItems,
            AuthenticationRequestParameters requestParams)
        {
            tokenCacheItems.FilterWithLogging(item =>
                            string.Equals(
                                item.TokenType ?? BearerAuthenticationScheme.BearerTokenType,
                                requestParams.AuthenticationScheme.AccessTokenType,
                                StringComparison.OrdinalIgnoreCase),
                            requestParams.RequestContext.Logger,
                            "Filtering by token type");
        }

        private static void FilterTokensByHomeAccountTenantOrAssertion(
            List<MsalAccessTokenCacheItem> tokenCacheItems,
            AuthenticationRequestParameters requestParams)
        {
            string requestTenantId = requestParams.Authority.TenantId;
            bool filterByTenantId = true;

            if (ApiEvent.IsOnBehalfOfRequest(requestParams.ApiId))
            {
                tokenCacheItems.FilterWithLogging(item =>
                        !string.IsNullOrEmpty(item.OboCacheKey) &&
                        item.OboCacheKey.Equals(
                            !string.IsNullOrEmpty(requestParams.LongRunningOboCacheKey) ? requestParams.LongRunningOboCacheKey : requestParams.UserAssertion.AssertionHash,
                            StringComparison.OrdinalIgnoreCase),
                        requestParams.RequestContext.Logger,
                        !string.IsNullOrEmpty(requestParams.LongRunningOboCacheKey) ?
                            $"Filtering AT by user-provided cache key: {requestParams.LongRunningOboCacheKey}" :
                            $"Filtering AT by user assertion: {requestParams.UserAssertion.AssertionHash}");

                // OBO calls FindAccessTokenAsync directly, but we are not able to resolve the authority 
                // unless the developer has configured a tenanted authority. If they have configured /common
                // then we cannot filter by tenant and will use whatever is in the cache.
                filterByTenantId =
                    !string.IsNullOrEmpty(requestTenantId) &&
                    !AadAuthority.IsCommonOrganizationsOrConsumersTenant(requestTenantId);
            }

            if (filterByTenantId)
            {
                tokenCacheItems.FilterWithLogging(item =>
                    string.Equals(item.TenantId ?? string.Empty, requestTenantId ?? string.Empty, StringComparison.OrdinalIgnoreCase),
                    requestParams.RequestContext.Logger,
                    "Filtering AT by tenant id");
            }
            else
            {
                requestParams.RequestContext.Logger.Warning("Have not filtered by tenant ID. " +
                    "This can happen in OBO scenario where authority is /common or /organizations. " +
                    "Please use tenanted authority.");
            }

            // Only AcquireTokenSilent has an IAccount in the request that can be used for filtering
            if (requestParams.ApiId != ApiEvent.ApiIds.AcquireTokenForClient &&
                requestParams.ApiId != ApiEvent.ApiIds.AcquireTokenForSystemAssignedManagedIdentity &&
                requestParams.ApiId != ApiEvent.ApiIds.AcquireTokenForUserAssignedManagedIdentity &&
                !ApiEvent.IsOnBehalfOfRequest(requestParams.ApiId))
            {
                tokenCacheItems.FilterWithLogging(item => item.HomeAccountId.Equals(
                                requestParams.Account.HomeAccountId?.Identifier, StringComparison.OrdinalIgnoreCase),
                                requestParams.RequestContext.Logger,
                                "Filtering AT by home account id");
            }
        }

        private MsalAccessTokenCacheItem FilterTokensByExpiry(
            MsalAccessTokenCacheItem msalAccessTokenCacheItem,
            AuthenticationRequestParameters requestParams)
        {
            var logger = requestParams.RequestContext.Logger;
            if (msalAccessTokenCacheItem != null)
            {

                if (msalAccessTokenCacheItem.ExpiresOn > DateTime.UtcNow + Constants.AccessTokenExpirationBuffer)
                {
                    // due to https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1806
                    if (msalAccessTokenCacheItem.ExpiresOn > DateTime.UtcNow + TimeSpan.FromDays(ExpirationTooLongInDays))
                    {
                        logger.Error(
                           "Access token expiration too large. This can be the result of a bug or corrupt cache. Token will be ignored as it is likely expired." +
                           GetAccessTokenExpireLogMessageContent(msalAccessTokenCacheItem));
                        return null;
                    }

                    logger.Info(
                        () => "Access token is not expired. Returning the found cache entry. " +
                        GetAccessTokenExpireLogMessageContent(msalAccessTokenCacheItem));

                    return msalAccessTokenCacheItem;
                }

                if (ServiceBundle.Config.IsExtendedTokenLifetimeEnabled &&
                    msalAccessTokenCacheItem.ExtendedExpiresOn > DateTime.UtcNow + Constants.AccessTokenExpirationBuffer)
                {

                    logger.Info(() =>
                        "Access token is expired.  IsExtendedLifeTimeEnabled=TRUE and ExtendedExpiresOn is not exceeded.  Returning the found cache entry. " +
                        GetAccessTokenExpireLogMessageContent(msalAccessTokenCacheItem));

                    msalAccessTokenCacheItem.IsExtendedLifeTimeToken = true;
                    return msalAccessTokenCacheItem;
                }

                logger.Info(() =>
                    "Access token has expired or about to expire. " +
                    GetAccessTokenExpireLogMessageContent(msalAccessTokenCacheItem));
            }

            return null;
        }

        private static MsalAccessTokenCacheItem GetSingleToken(
            List<MsalAccessTokenCacheItem> tokenCacheItems,
            AuthenticationRequestParameters requestParams)
        {
            // if only one cached token found
            if (tokenCacheItems.Count == 1)
            {
                return tokenCacheItems[0];
            }

            requestParams.RequestContext.Logger.Error("Multiple access tokens found for matching authority, client_id, user and scopes. ");
            throw new MsalClientException(
                MsalError.MultipleTokensMatchedError,
                MsalErrorMessage.MultipleTokensMatched);
        }

        private async Task<List<MsalAccessTokenCacheItem>> FilterTokensByEnvironmentAsync(
            List<MsalAccessTokenCacheItem> tokenCacheItems,
            AuthenticationRequestParameters requestParams)
        {
            var logger = requestParams.RequestContext.Logger;

            if (tokenCacheItems.Count == 0)
            {
                logger.Verbose(() => "Not filtering AT by environment, because there are no candidates");
                return tokenCacheItems;
            }

            // at this point we need environment aliases, try to get them without a discovery call
            var instanceMetadata = await ServiceBundle.InstanceDiscoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                                     requestParams.AuthorityInfo,
                                     tokenCacheItems.Select(at => at.Environment),  // if all environments are known, a network call can be avoided
                                     requestParams.RequestContext)
                            .ConfigureAwait(false);

            // In case we're sharing the cache with an MSAL that does not implement environment aliasing,
            // it's possible (but unlikely), that we have multiple ATs from the same alias family.
            // To overcome some of these use cases, try to filter just by preferred cache alias
            var itemsFilteredByAlias = tokenCacheItems.FilterWithLogging(
                item => item.Environment.Equals(instanceMetadata.PreferredCache, StringComparison.OrdinalIgnoreCase),
                requestParams.RequestContext.Logger,
                $"Filtering AT by preferred environment {instanceMetadata.PreferredCache}",
                updateOriginalCollection: false);

            if (itemsFilteredByAlias.Count > 0)
            {
                if (logger.IsLoggingEnabled(LogLevel.Verbose))
                {
                    logger.Verbose(() => $"Filtered AT by preferred alias returning {itemsFilteredByAlias.Count} tokens.");
                }

                return itemsFilteredByAlias;
            }

            return tokenCacheItems.FilterWithLogging(
                item => instanceMetadata.Aliases.ContainsOrdinalIgnoreCase(item.Environment),
                requestParams.RequestContext.Logger,
                $"Filtering AT by environment");
        }

        private static MsalAccessTokenCacheItem FilterTokensByPopKeyId(MsalAccessTokenCacheItem item, AuthenticationRequestParameters authenticationRequest)
        {
            if (item == null)
            {
                return null;
            }

            string requestKid = authenticationRequest.AuthenticationScheme.KeyId;
            if (string.IsNullOrEmpty(item.KeyId) && string.IsNullOrEmpty(requestKid))
            {
                authenticationRequest.RequestContext.Logger.Verbose(() => "Bearer token found");
                return item;
            }

            if (string.Equals(item.KeyId, requestKid, StringComparison.OrdinalIgnoreCase))
            {
                authenticationRequest.RequestContext.Logger.Verbose(() => "Keyed token found");
                return item;
            }

            authenticationRequest.RequestContext.Logger.Info(
                    () => $"A token bound to the wrong key was found. Token key id: {item.KeyId} Request key id: {requestKid}");
            return null;
        }
        #endregion

        private void FilterTokensByClientId<T>(List<T> tokenCacheItems) where T : MsalCredentialCacheItemBase
        {
            tokenCacheItems.RemoveAll(x => !x.ClientId.Equals(ClientId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// For testing purposes only. Expires ALL access tokens in memory and fires OnAfterAccessAsync event with no cache key
        /// </summary>
        internal async Task ExpireAllAccessTokensForTestAsync()
        {
            ITokenCacheInternal tokenCacheInternal = this;
            var accessor = tokenCacheInternal.Accessor;

            var allAccessTokens = accessor.GetAllAccessTokens();
            foreach (MsalAccessTokenCacheItem atItem in allAccessTokens)
            {
                accessor.SaveAccessToken(atItem.WithExpiresOn(DateTimeOffset.UtcNow));
            }

            if (tokenCacheInternal.IsAppSubscribedToSerializationEvents())
            {
                var args = new TokenCacheNotificationArgs(
                            tokenCache: this,
                            clientId: ClientId,
                            account: null,
                            hasStateChanged: true,
                            tokenCacheInternal.IsApplicationCache,
                            suggestedCacheKey: null,
                            hasTokens: tokenCacheInternal.HasTokensNoLocks(),
                            suggestedCacheExpiry: null,
                            cancellationToken: default,
                            correlationId: default,
                            requestScopes: null,
                            requestTenantId: null,
                            identityLogger: null,
                            piiLoggingEnabled: false);

                await tokenCacheInternal.OnAfterAccessAsync(args).ConfigureAwait(false);
            }
        }

        async Task<MsalRefreshTokenCacheItem> ITokenCacheInternal.FindRefreshTokenAsync(
            AuthenticationRequestParameters requestParams,
            string familyId)
        {
            if (requestParams.Authority == null)
            {
                return null;
            }

            var requestKey = CacheKeyFactory.GetKeyFromRequest(requestParams);
            var refreshTokens = Accessor.GetAllRefreshTokens(requestKey);
            requestParams.RequestContext.Logger.Always($"[FindRefreshTokenAsync] Discovered {refreshTokens.Count} refresh tokens in cache using key: {requestKey}");

            if (refreshTokens.Count != 0)
            {
                FilterRefreshTokensByHomeAccountIdOrAssertion(refreshTokens, requestParams, familyId);

                if (!requestParams.AppConfig.MultiCloudSupportEnabled)
                {
                    var metadata =
                    await ServiceBundle.InstanceDiscoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                        requestParams.AuthorityInfo,
                        refreshTokens.Select(rt => rt.Environment),  // if all environments are known, a network call can be avoided
                        requestParams.RequestContext)
                    .ConfigureAwait(false);
                    var aliases = metadata.Aliases;

                    refreshTokens.RemoveAll(
                        item => !aliases.ContainsOrdinalIgnoreCase(item.Environment));
                }

                requestParams.RequestContext.Logger.Info(() => "[FindRefreshTokenAsync] Refresh token found in the cache? - " + (refreshTokens.Count != 0));

                if (refreshTokens.Count > 0)
                {
                    return refreshTokens.FirstOrDefault();
                }
            }
            else
            {
                requestParams.RequestContext.Logger.Verbose(() => "[FindRefreshTokenAsync] No RTs found in the MSAL cache ");
            }

            requestParams.RequestContext.Logger.Verbose(() => "[FindRefreshTokenAsync] Checking ADAL cache for matching RT. ");

            if (IsLegacyAdalCacheEnabled(requestParams) &&
                requestParams.Account != null &&
                string.IsNullOrEmpty(familyId)) // ADAL legacy cache does not store FRTs
            {
                var metadata =
                  await ServiceBundle.InstanceDiscoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                      requestParams.AuthorityInfo,
                      refreshTokens.Select(rt => rt.Environment),  // if all environments are known, a network call can be avoided
                      requestParams.RequestContext)
                  .ConfigureAwait(false);
                var aliases = metadata.Aliases;

                return CacheFallbackOperations.GetRefreshToken(
                    requestParams.RequestContext.Logger,
                    LegacyCachePersistence,
                    aliases,
                    requestParams.AppConfig.ClientId,
                    requestParams.Account);
            }

            return null;
        }

        private static void FilterRefreshTokensByHomeAccountIdOrAssertion(
            List<MsalRefreshTokenCacheItem> cacheItems,
            AuthenticationRequestParameters requestParams,
            string familyId)
        {
            if (ApiEvent.IsOnBehalfOfRequest(requestParams.ApiId))
            {
                cacheItems.FilterWithLogging(item =>
                                !string.IsNullOrEmpty(item.OboCacheKey) &&
                                item.OboCacheKey.Equals(
                                !string.IsNullOrEmpty(requestParams.LongRunningOboCacheKey) ? requestParams.LongRunningOboCacheKey : requestParams.UserAssertion.AssertionHash,
                                    StringComparison.OrdinalIgnoreCase),
                                requestParams.RequestContext.Logger,
                                !string.IsNullOrEmpty(requestParams.LongRunningOboCacheKey) ?
                                $"Filtering RT by user-provided cache key: {requestParams.LongRunningOboCacheKey}" :
                                $"Filtering RT by user assertion: {requestParams.UserAssertion.AssertionHash}");
            }
            else
            {
                cacheItems.FilterWithLogging(item => item.HomeAccountId.Equals(
                                requestParams.Account.HomeAccountId?.Identifier, StringComparison.OrdinalIgnoreCase),
                                requestParams.RequestContext.Logger,
                                "Filtering RT by home account id");
            }

            // This will also filter for the case when familyId is null and exclude RTs with familyId in filtered list
            cacheItems.FilterWithLogging(item =>
                    string.Equals(item.FamilyId ?? string.Empty,
                    familyId ?? string.Empty, StringComparison.OrdinalIgnoreCase),
                    requestParams.RequestContext.Logger,
                    "Filtering RT by family id");

            // if there is a value in familyId, we are looking for FRT and hence ignore filter with clientId
            if (string.IsNullOrEmpty(familyId))
            {
                cacheItems.FilterWithLogging(item => item.ClientId.Equals(
                            requestParams.AppConfig.ClientId, StringComparison.OrdinalIgnoreCase),
                            requestParams.RequestContext.Logger,
                            "Filtering RT by client id");
            }
        }

        async Task<bool?> ITokenCacheInternal.IsFociMemberAsync(AuthenticationRequestParameters requestParams, string familyId)
        {
            var logger = requestParams.RequestContext.Logger;
            if (requestParams?.AuthorityInfo?.CanonicalAuthority == null)
            {
                logger.Warning("No authority details, can't check app metadata. Returning unknown. ");
                return null;
            }

            var allAppMetadata = Accessor.GetAllAppMetadata();

            var instanceMetadata = await ServiceBundle.InstanceDiscoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                    requestParams.AuthorityInfo,
                    allAppMetadata.Select(m => m.Environment),
                    requestParams.RequestContext)
                .ConfigureAwait(false);

            var appMetadata =
                instanceMetadata.Aliases
                .Select(env => Accessor.GetAppMetadata(new MsalAppMetadataCacheItem(ClientId, env, null)))
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

        /// <remarks>
        /// Get accounts should not make a network call, if possible. This can be achieved if
        /// all the environments in the token cache are known to MSAL, as MSAL keeps a list of
        /// known environments in <see cref="KnownMetadataProvider"/>
        /// </remarks>
        async Task<IEnumerable<IAccount>> ITokenCacheInternal.GetAccountsAsync(AuthenticationRequestParameters requestParameters)
        {
            var logger = requestParameters.RequestContext.Logger;
            var environment = requestParameters.AuthorityInfo.Host;
            bool filterByClientId = !_featureFlags.IsFociEnabled;

            // this will either be the home account ID or null, it can never be OBO assertion or tenant ID
            string partitionKey = CacheKeyFactory.GetKeyFromRequest(requestParameters);

            var refreshTokenCacheItems = Accessor.GetAllRefreshTokens(partitionKey);
            var accountCacheItems = Accessor.GetAllAccounts(partitionKey);

            if (filterByClientId)
            {
                FilterTokensByClientId(refreshTokenCacheItems);
            }

            if (logger.IsLoggingEnabled(LogLevel.Verbose))
            {
                logger.Verbose(() => $"[GetAccounts] Found {refreshTokenCacheItems.Count} RTs and {accountCacheItems.Count} accounts in MSAL cache. ");
            }

            // Multi-cloud support - must filter by environment.
            ISet<string> allEnvironmentsInCache = new HashSet<string>(
                accountCacheItems.Select(aci => aci.Environment),
                StringComparer.OrdinalIgnoreCase);
            allEnvironmentsInCache.UnionWith(refreshTokenCacheItems.Select(rt => rt.Environment));

            AdalUsersForMsal adalUsersResult = null;

            if (IsLegacyAdalCacheEnabled(requestParameters))
            {
                adalUsersResult = CacheFallbackOperations.GetAllAdalUsersForMsal(
                    logger,
                    LegacyCachePersistence,
                    ClientId);
                allEnvironmentsInCache.UnionWith(adalUsersResult.GetAdalUserEnvironments());
            }

            InstanceDiscoveryMetadataEntry instanceMetadata = await ServiceBundle.InstanceDiscoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                requestParameters.AuthorityInfo,
                allEnvironmentsInCache,
                requestParameters.RequestContext).ConfigureAwait(false);

            // If the client application is instance aware then we skip the filter with environment
            // since the authority in request is different from the authority used to get the token
            if (!requestParameters.AppConfig.MultiCloudSupportEnabled)
            {
                refreshTokenCacheItems.RemoveAll(rt => !instanceMetadata.Aliases.ContainsOrdinalIgnoreCase(rt.Environment));
                accountCacheItems.RemoveAll(acc => !instanceMetadata.Aliases.ContainsOrdinalIgnoreCase(acc.Environment));
            }

            if (logger.IsLoggingEnabled(LogLevel.Verbose))
            {
                logger.Verbose(() => $"[GetAccounts] Found {refreshTokenCacheItems.Count} RTs and {accountCacheItems.Count} accounts in MSAL cache after environment filtering. ");
            }

            IDictionary<string, Account> clientInfoToAccountMap = new Dictionary<string, Account>();
            foreach (MsalRefreshTokenCacheItem rtItem in refreshTokenCacheItems)
            {
                foreach (MsalAccountCacheItem account in accountCacheItems)
                {
                    if (RtMatchesAccount(rtItem, account))
                    {
                        var tenantProfiles = await GetTenantProfilesAsync(requestParameters, account.HomeAccountId).ConfigureAwait(false);

                        clientInfoToAccountMap[rtItem.HomeAccountId] = new Account(
                            account.HomeAccountId,
                            account.PreferredUsername,
                            requestParameters.AppConfig.MultiCloudSupportEnabled ?
                                account.Environment : // If multi cloud support is enabled keep the cached environment
                                environment, // Preserve the environment passed in by the user
                            account.WamAccountIds,
                            tenantProfiles?.Values);

                        break;
                    }
                }
            }

            if (IsLegacyAdalCacheEnabled(requestParameters))
            {
                UpdateMapWithAdalAccountsWithClientInfo(
                    environment,
                    instanceMetadata.Aliases,
                    adalUsersResult,
                    clientInfoToAccountMap);
            }

            // Add WAM accounts stored in MSAL's cache - for which we do not have an RT
            if (requestParameters.AppConfig.IsBrokerEnabled && ServiceBundle.PlatformProxy.BrokerSupportsWamAccounts)
            {
                foreach (MsalAccountCacheItem cachedAccount in accountCacheItems)
                {
                    if (!clientInfoToAccountMap.ContainsKey(cachedAccount.HomeAccountId) &&
                        cachedAccount.WamAccountIds != null &&
                        cachedAccount.WamAccountIds.ContainsKey(requestParameters.AppConfig.ClientId))
                    {
                        var tenantProfiles = await GetTenantProfilesAsync(requestParameters, cachedAccount.HomeAccountId).ConfigureAwait(false);

                        var wamAccount = new Account(
                            cachedAccount.HomeAccountId,
                            cachedAccount.PreferredUsername,
                            cachedAccount.Environment,
                            cachedAccount.WamAccountIds,
                            tenantProfiles?.Values);

                        clientInfoToAccountMap[cachedAccount.HomeAccountId] = wamAccount;
                    }
                }
            }

            var accounts = new List<IAccount>(clientInfoToAccountMap.Values);

            if (IsLegacyAdalCacheEnabled(requestParameters))
            {
                UpdateWithAdalAccountsWithoutClientInfo(environment,
                 instanceMetadata.Aliases,
                 adalUsersResult,
                 accounts);
            }

            if (!string.IsNullOrEmpty(requestParameters.HomeAccountId))
            {
                accounts = accounts.Where(acc => acc.HomeAccountId.Identifier.Equals(
                    requestParameters.HomeAccountId,
                    StringComparison.OrdinalIgnoreCase)).ToList();

                if (logger.IsLoggingEnabled(LogLevel.Verbose))
                {
                    logger.Verbose(() => $"Filtered by home account id. Remaining accounts {accounts.Count} ");
                }
            }

            return accounts;
        }

        private static void UpdateMapWithAdalAccountsWithClientInfo(
          string envFromRequest,
          IEnumerable<string> envAliases,
          AdalUsersForMsal adalUsers,
          IDictionary<string, Account> clientInfoToAccountMap)
        {
            foreach (KeyValuePair<string, AdalUserInfo> pair in adalUsers?.GetUsersWithClientInfo(envAliases))
            {
                var clientInfo = ClientInfo.CreateFromJson(pair.Key);
                string accountIdentifier = clientInfo.ToAccountIdentifier();

                if (!clientInfoToAccountMap.ContainsKey(accountIdentifier))
                {
                    clientInfoToAccountMap[accountIdentifier] = new Account(
                            accountIdentifier, pair.Value.DisplayableId, envFromRequest);
                }
            }
        }

        private static void UpdateWithAdalAccountsWithoutClientInfo(
            string envFromRequest,
            IEnumerable<string> envAliases,
            AdalUsersForMsal adalUsers,
            List<IAccount> accounts)
        {
            var uniqueUserNames = accounts.Select(a => a.Username).Distinct().ToList();

            foreach (AdalUserInfo user in adalUsers?.GetUsersWithoutClientInfo(envAliases))
            {
                if (!string.IsNullOrEmpty(user.DisplayableId) && !uniqueUserNames.Contains(user.DisplayableId))
                {
                    accounts.Add(new Account(null, user.DisplayableId, envFromRequest));
                    uniqueUserNames.Add(user.DisplayableId);
                }
            }
        }

        MsalIdTokenCacheItem ITokenCacheInternal.GetIdTokenCacheItem(MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            var idToken = Accessor.GetIdToken(msalAccessTokenCacheItem);
            return idToken;
        }

        private async Task<IDictionary<string, TenantProfile>> GetTenantProfilesAsync(
            AuthenticationRequestParameters requestParameters,
            string homeAccountId)
        {
            if (!requestParameters.AuthorityInfo.CanBeTenanted)
            {
                return null;
            }

            if (homeAccountId == null)
            {
                requestParameters.RequestContext.Logger.Warning("No homeAccountId, skipping tenant profiles");
                return null;
            }

            var idTokenCacheItems = Accessor.GetAllIdTokens(homeAccountId);
            FilterTokensByClientId(idTokenCacheItems);

            if (!requestParameters.AppConfig.MultiCloudSupportEnabled)
            {
                ISet<string> allEnvironmentsInCache = new HashSet<string>(
                    idTokenCacheItems.Select(aci => aci.Environment),
                    StringComparer.OrdinalIgnoreCase);

                InstanceDiscoveryMetadataEntry instanceMetadata = await ServiceBundle.InstanceDiscoveryManager.GetMetadataEntryTryAvoidNetworkAsync(
                    requestParameters.AuthorityInfo,
                    allEnvironmentsInCache,
                    requestParameters.RequestContext).ConfigureAwait(false);

                idTokenCacheItems.RemoveAll(idToken => !instanceMetadata.Aliases.ContainsOrdinalIgnoreCase(idToken.Environment));
            }

            // some accessors might not support partitioning, so make sure to filter by home account id
            idTokenCacheItems.RemoveAll(idToken => !homeAccountId.Equals(idToken.HomeAccountId));

            Dictionary<string, TenantProfile> tenantProfiles = new Dictionary<string, TenantProfile>();
            foreach (MsalIdTokenCacheItem idTokenCacheItem in idTokenCacheItems)
            {
                tenantProfiles[idTokenCacheItem.TenantId] = new TenantProfile(idTokenCacheItem);
            }

            return tenantProfiles;
        }

        async Task<Account> ITokenCacheInternal.GetAccountAssociatedWithAccessTokenAsync(
            AuthenticationRequestParameters requestParameters,
            MsalAccessTokenCacheItem msalAccessTokenCacheItem)
        {
            Debug.Assert(msalAccessTokenCacheItem.HomeAccountId != null);

            var tenantProfiles = await GetTenantProfilesAsync(requestParameters, msalAccessTokenCacheItem.HomeAccountId).ConfigureAwait(false);

            var accountCacheItem = Accessor.GetAccount(
                new MsalAccountCacheItem(
                        msalAccessTokenCacheItem.Environment,
                        msalAccessTokenCacheItem.TenantId,
                        msalAccessTokenCacheItem.HomeAccountId,
                        requestParameters.Account?.Username));

            return new Account(
                msalAccessTokenCacheItem.HomeAccountId,
                accountCacheItem?.PreferredUsername,
                accountCacheItem?.Environment,
                accountCacheItem?.WamAccountIds,
                tenantProfiles?.Values);
        }

        async Task<bool> ITokenCacheInternal.StopLongRunningOboProcessAsync(string longRunningOboCacheKey, AuthenticationRequestParameters requestParameters)
        {
            bool tokensRemoved;

            requestParameters.RequestContext.Logger.Verbose(() => $"[StopLongRunningOboProcessAsync] Entering token cache semaphore. Count {_semaphoreSlim.GetCurrentCountLogMessage()}");
            await _semaphoreSlim.WaitAsync(requestParameters.RequestContext.UserCancellationToken).ConfigureAwait(false);
            requestParameters.RequestContext.Logger.Verbose(() => "[StopLongRunningOboProcessAsync] Entered token cache semaphore");

            try
            {
                requestParameters.RequestContext.Logger.Info(() => "[StopLongRunningOboProcessAsync] Stopping long running OBO process by removing tokens from cache.");

                ITokenCacheInternal tokenCacheInternal = this;

                try
                {
                    if (tokenCacheInternal.IsAppSubscribedToSerializationEvents())
                    {
                        var args = new TokenCacheNotificationArgs(
                            tokenCache: this,
                            clientId: ClientId,
                            account: null,
                            hasStateChanged: false,
                            tokenCacheInternal.IsApplicationCache,
                            suggestedCacheKey: longRunningOboCacheKey,
                            hasTokens: tokenCacheInternal.HasTokensNoLocks(),
                            suggestedCacheExpiry: null,
                            cancellationToken: requestParameters.RequestContext.UserCancellationToken,
                            correlationId: requestParameters.RequestContext.CorrelationId,
                            requestScopes: requestParameters.Scope,
                            requestTenantId: requestParameters.AuthorityManager.OriginalAuthority.TenantId,
                            identityLogger: requestParameters.RequestContext.Logger.IdentityLogger,
                            piiLoggingEnabled: requestParameters.RequestContext.Logger.PiiLoggingEnabled);

                        await tokenCacheInternal.OnBeforeAccessAsync(args).ConfigureAwait(false);
                        await tokenCacheInternal.OnBeforeWriteAsync(args).ConfigureAwait(false);
                    }

                    tokensRemoved = RemoveOboTokensInternal(longRunningOboCacheKey, requestParameters.RequestContext);
                }
                finally
                {
                    if (tokenCacheInternal.IsAppSubscribedToSerializationEvents())
                    {
                        var args = new TokenCacheNotificationArgs(
                           tokenCache: this,
                           clientId: ClientId,
                           account: null,
                           hasStateChanged: true,
                           tokenCacheInternal.IsApplicationCache,
                           suggestedCacheKey: longRunningOboCacheKey,
                           hasTokens: tokenCacheInternal.HasTokensNoLocks(),
                           suggestedCacheExpiry: null,
                           cancellationToken: requestParameters.RequestContext.UserCancellationToken,
                           correlationId: requestParameters.RequestContext.CorrelationId,
                           requestScopes: requestParameters.Scope,
                           requestTenantId: requestParameters.AuthorityManager.OriginalAuthority.TenantId,
                           identityLogger: requestParameters.RequestContext.Logger.IdentityLogger,
                           piiLoggingEnabled: requestParameters.RequestContext.Logger.PiiLoggingEnabled);

                        await tokenCacheInternal.OnAfterAccessAsync(args).ConfigureAwait(false);
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

            return tokensRemoved;
        }

        async Task ITokenCacheInternal.RemoveAccountAsync(IAccount account, AuthenticationRequestParameters requestParameters)
        {
            requestParameters.RequestContext.Logger.Verbose(() => $"[RemoveAccountAsync] Entering token cache semaphore. Count {_semaphoreSlim.GetCurrentCountLogMessage()}");
            await _semaphoreSlim.WaitAsync(requestParameters.RequestContext.UserCancellationToken).ConfigureAwait(false);
            requestParameters.RequestContext.Logger.Verbose(() => "[RemoveAccountAsync] Entered token cache semaphore");

            var cacheKey = account.HomeAccountId?.Identifier;
            try
            {
                requestParameters.RequestContext.Logger.Info("[RemoveAccountAsync] Removing account from cache.");

                ITokenCacheInternal tokenCacheInternal = this;

                try
                {
                    if (tokenCacheInternal.IsAppSubscribedToSerializationEvents())
                    {
                        var args = new TokenCacheNotificationArgs(
                            tokenCache: this,
                            clientId: ClientId,
                            account: account,
                            hasStateChanged: true,
                            tokenCacheInternal.IsApplicationCache,
                            suggestedCacheKey: cacheKey,
                            hasTokens: tokenCacheInternal.HasTokensNoLocks(),
                            suggestedCacheExpiry: null,
                            cancellationToken: requestParameters.RequestContext.UserCancellationToken,
                            correlationId: requestParameters.RequestContext.CorrelationId,
                            requestScopes: requestParameters.Scope,
                            requestTenantId: requestParameters.AuthorityManager.OriginalAuthority.TenantId,
                            identityLogger: requestParameters.RequestContext.Logger.IdentityLogger,
                            piiLoggingEnabled: requestParameters.RequestContext.Logger.PiiLoggingEnabled);

                        await tokenCacheInternal.OnBeforeAccessAsync(args).ConfigureAwait(false);
                        await tokenCacheInternal.OnBeforeWriteAsync(args).ConfigureAwait(false);
                    }

                    RemoveAccountInternal(account, requestParameters.RequestContext);
                    
                    if (IsLegacyAdalCacheEnabled(requestParameters))
                    {
                        CacheFallbackOperations.RemoveAdalUser(
                           requestParameters.RequestContext.Logger,
                           LegacyCachePersistence,
                           ClientId,
                           account?.Username,
                           cacheKey);
                    }
                }
                finally
                {
                    if (tokenCacheInternal.IsAppSubscribedToSerializationEvents())
                    {
                        var args = new TokenCacheNotificationArgs(
                           tokenCache: this,
                           clientId: ClientId,
                           account: account,
                           hasStateChanged: true,
                           tokenCacheInternal.IsApplicationCache,
                           suggestedCacheKey: cacheKey,
                           hasTokens: tokenCacheInternal.HasTokensNoLocks(),
                           suggestedCacheExpiry: null,
                           cancellationToken: requestParameters.RequestContext.UserCancellationToken,
                           correlationId: requestParameters.RequestContext.CorrelationId,
                           requestScopes: requestParameters.Scope,
                           requestTenantId: requestParameters.AuthorityManager.OriginalAuthority.TenantId,
                           identityLogger: requestParameters.RequestContext.Logger.IdentityLogger,
                           piiLoggingEnabled: requestParameters.RequestContext.Logger.PiiLoggingEnabled);

                        await tokenCacheInternal.OnAfterAccessAsync(args).ConfigureAwait(false);
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
            return Accessor.HasAccessOrRefreshTokens();
        }

        /// <summary>
        /// Removes OBO tokens stored in the cache. Note that the cache is internally and externally partitioned by the oboKey.
        /// </summary>
        private bool RemoveOboTokensInternal(string oboPartitionKey, RequestContext requestContext)
        {
            ILoggerAdapter logger = requestContext.Logger;

            //Filter and remove tokens based on OBO Cache Key
            var refreshTokens = Accessor.GetAllRefreshTokens(oboPartitionKey, logger);
            refreshTokens.RemoveAll(item => !(bool)item?.OboCacheKey.Equals(oboPartitionKey, StringComparison.OrdinalIgnoreCase));
            var rtsRemoved = RemoveRefreshTokens(refreshTokens, logger, out bool filterByClientId);

            var accessTokens = Accessor.GetAllAccessTokens(oboPartitionKey, logger);
            accessTokens.RemoveAll(item => !(bool)item?.OboCacheKey.Equals(oboPartitionKey, StringComparison.OrdinalIgnoreCase));
            var atsRemoved = RemoveAccessTokens(accessTokens, logger, filterByClientId);

            return rtsRemoved > 0 || atsRemoved > 0;
        }

        internal /* internal for test only */ void RemoveAccountInternal(IAccount account, RequestContext requestContext)
        {
            if (account.HomeAccountId == null)
            {
                // adalv3 account
                return;
            }

            //Filter and remove tokens based on account identifier as 
            string partitionKey = account.HomeAccountId.Identifier;

            ILoggerAdapter logger = requestContext.Logger;

            var refreshTokens = Accessor.GetAllRefreshTokens(partitionKey, logger);
            refreshTokens.RemoveAll(item => !item.HomeAccountId.Equals(partitionKey, StringComparison.OrdinalIgnoreCase));
            RemoveRefreshTokens(refreshTokens, logger, out bool filterByClientId);

            var accessTokens = Accessor.GetAllAccessTokens(partitionKey, logger);
            accessTokens.RemoveAll(item => !item.HomeAccountId.Equals(partitionKey, StringComparison.OrdinalIgnoreCase));
            RemoveAccessTokens(accessTokens, logger, filterByClientId);

            RemoveIdTokens(partitionKey, logger, filterByClientId);

            RemoveAccounts(account);
        }

        private int RemoveRefreshTokens(List<MsalRefreshTokenCacheItem> refreshTokens, ILoggerAdapter logger, out bool filterByClientId)
        {
            // To maintain backward compatibility with other MSALs, filter all credentials by clientID if
            // FOCI is disabled or if an FRT is not present
            filterByClientId = !_featureFlags.IsFociEnabled || !FrtExists(refreshTokens);

            // Delete all credentials associated with this IAccount
            if (filterByClientId)
            {
                FilterTokensByClientId(refreshTokens);
            }

            foreach (MsalRefreshTokenCacheItem refreshTokenCacheItem in refreshTokens)
            {
                Accessor.DeleteRefreshToken(refreshTokenCacheItem);
            }

            logger.Info(() => $"[RemoveRefreshTokens] Deleted {refreshTokens.Count} refresh tokens.");

            return refreshTokens.Count;
        }

        private int RemoveAccessTokens(List<MsalAccessTokenCacheItem> accessTokens, ILoggerAdapter logger, bool filterByClientId)
        {
            if (filterByClientId)
            {
                FilterTokensByClientId(accessTokens);
            }

            foreach (MsalAccessTokenCacheItem accessTokenCacheItem in accessTokens)
            {
                Accessor.DeleteAccessToken(accessTokenCacheItem);
            }

            logger.Info(() => $"[RemoveAccessTokens] Deleted {accessTokens.Count} access tokens.");

            return accessTokens.Count;
        }

        private int RemoveIdTokens(string partitionKey, ILoggerAdapter logger, bool filterByClientId)
        {
            var idTokens = Accessor.GetAllIdTokens(partitionKey);
            idTokens.RemoveAll(item => !item.HomeAccountId.Equals(partitionKey, StringComparison.OrdinalIgnoreCase));
            if (filterByClientId)
            {
                FilterTokensByClientId(idTokens);
            }

            foreach (MsalIdTokenCacheItem idTokenCacheItem in idTokens)
            {
                Accessor.DeleteIdToken(idTokenCacheItem);
            }

            logger.Info(() => $"[RemoveIdTokens] Deleted {idTokens.Count} ID tokens.");

            return idTokens.Count;
        }

        private void RemoveAccounts(IAccount account)
        {
            if (account != null)
            {
                var accounts = Accessor.GetAllAccounts(account.HomeAccountId.Identifier);
                accounts.RemoveAll(item => !(item.HomeAccountId.Equals(account.HomeAccountId.Identifier, StringComparison.OrdinalIgnoreCase) &&
                                   item.PreferredUsername.Equals(account.Username, StringComparison.OrdinalIgnoreCase)));

                foreach (MsalAccountCacheItem accountCacheItem in accounts)
                {
                    Accessor.DeleteAccount(accountCacheItem);
                }
            }
        }
    }
}
