// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client.Cache
{
    /// <summary>
    /// Responsible for computing:
    /// - external distributed cache key (from request and responses)
    /// - internal cache partition keys (as above, but also from cache items)
    /// 
    /// These are the same string, but MSAL cannot control if the app developer actually uses distributed caching. 
    /// However, MSAL's in-memory cache needs to be partitioned, and this class computes the partition key.
    /// </summary>
    internal static class CacheKeyFactory
    {
        public static string GetKeyFromRequest(AuthenticationRequestParameters requestParameters)
        {
            if (GetOboOrAppKey(requestParameters, out string key))
            {
                return key;
            }

            if (requestParameters.ApiId == ApiEvent.ApiIds.AcquireTokenSilent ||
                requestParameters.ApiId == ApiEvent.ApiIds.RemoveAccount)
            {
                return requestParameters.Account?.HomeAccountId?.Identifier;
            }

            if (requestParameters.ApiId == ApiEvent.ApiIds.GetAccountById)
            {
                return requestParameters.HomeAccountId;
            }

            return null;
        }

        public static string GetExternalCacheKeyFromResponse(
            AuthenticationRequestParameters requestParameters,
            string homeAccountIdFromResponse)
        {
            if (GetOboOrAppKey(requestParameters, out string key))
            {
                return key;
            }

            if (requestParameters.AppConfig.IsConfidentialClient ||
                requestParameters.ApiId == ApiEvent.ApiIds.AcquireTokenSilent)
            {
                return homeAccountIdFromResponse;
            }

            return null;
        }

        public static string GetInternalPartitionKeyFromResponse(
            AuthenticationRequestParameters requestParameters,
            string homeAccountIdFromResponse)
        {
            return GetExternalCacheKeyFromResponse(requestParameters, homeAccountIdFromResponse) ??
                homeAccountIdFromResponse;
        }

        private static bool GetOboOrAppKey(AuthenticationRequestParameters requestParameters, out string key)
        {
            if (ApiEvent.IsOnBehalfOfRequest(requestParameters.ApiId))
            {
                key = GetOboKey(requestParameters.LongRunningOboCacheKey, requestParameters.UserAssertion);
                return true;
            }

            if (requestParameters.ApiId == ApiEvent.ApiIds.AcquireTokenForClient || 
                requestParameters.ApiId == ApiEvent.ApiIds.AcquireTokenForSystemAssignedManagedIdentity ||
                requestParameters.ApiId == ApiEvent.ApiIds.AcquireTokenForUserAssignedManagedIdentity)
            {
                string tenantId = requestParameters.Authority.TenantId ?? "";
                key = GetClientCredentialKey(requestParameters.AppConfig.ClientId, tenantId, requestParameters.AuthenticationScheme?.KeyId);

                return true;
            }

            key = null;
            return false;
        }

        public static string GetClientCredentialKey(string clientId, string tenantId, string popKid)
        {
            return $"{popKid}{clientId}_{tenantId}_AppTokenCache";
        }

        public static string GetOboKey(string oboCacheKey, UserAssertion userAssertion)
        {
            return !string.IsNullOrEmpty(oboCacheKey) ? oboCacheKey : userAssertion?.AssertionHash;
        }

        public static string GetOboKey(string oboCacheKey, string homeAccountId)
        {
            return !string.IsNullOrEmpty(oboCacheKey) ? oboCacheKey : homeAccountId;
        }

        public static string GetKeyFromCachedItem(MsalAccessTokenCacheItem accessTokenCacheItem)
        {
            string partitionKey = GetOboKey(accessTokenCacheItem.OboCacheKey, accessTokenCacheItem.HomeAccountId);
            return partitionKey;
        }

        public static string GetKeyFromCachedItem(MsalRefreshTokenCacheItem refreshTokenCacheItem)
        {
            string partitionKey = GetOboKey(refreshTokenCacheItem.OboCacheKey, refreshTokenCacheItem.HomeAccountId);
            return partitionKey;
        }

        // Id tokens are not indexed by OBO key, only by home account key
        public static string GetIdTokenKeyFromCachedItem(MsalAccessTokenCacheItem accessTokenCacheItem)
        {
            return accessTokenCacheItem.HomeAccountId;
        }

        public static string GetKeyFromAccount(MsalAccountCacheItem accountCacheItem)
        {
            return accountCacheItem.HomeAccountId;
        }

        public static string GetKeyFromCachedItem(MsalIdTokenCacheItem idTokenCacheItem)
        {
            return idTokenCacheItem.HomeAccountId;
        }

        public static string GetKeyFromCachedItem(MsalAccountCacheItem accountCacheItem)
        {
            return accountCacheItem.HomeAccountId;
        }
    }
}
