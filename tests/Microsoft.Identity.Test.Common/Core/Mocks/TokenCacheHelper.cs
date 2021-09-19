// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Factories;
using Microsoft.Identity.Client.PlatformsCommon.Shared;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    internal static class TokenCacheHelper
    {
        public static long ValidExpiresIn = 28800;
        public static long ValidExtendedExpiresIn = 57600;

        internal static MsalAccessTokenCacheItem CreateAccessTokenItem(
            string scopes = TestConstants.ScopeStr,
            string tenant = TestConstants.Utid,
            string homeAccountId = TestConstants.HomeAccountId,
            bool isExpired = false)
        {
            MsalAccessTokenCacheItem atItem = new MsalAccessTokenCacheItem(
               TestConstants.ProductionPrefCacheEnvironment,
               TestConstants.ClientId,
               scopes,
               tenantId: tenant,
               secret: string.Empty,
               accessTokenExpiresOn: isExpired ? new DateTimeOffset(DateTime.UtcNow) : new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
               accessTokenExtendedExpiresOn: isExpired ? new DateTimeOffset(DateTime.UtcNow) : new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExtendedExpiresIn)),
               MockHelpers.CreateClientInfo(),
               homeAccountId);

            return atItem;
        }

        internal static MsalRefreshTokenCacheItem CreateRefreshTokenItem(
            string userAssertionHash = TestConstants.UserAssertion,
            string homeAccountId = TestConstants.HomeAccountId)
        {
            return new MsalRefreshTokenCacheItem()
            {
                ClientId = TestConstants.ClientId,
                Environment = TestConstants.ProductionPrefCacheEnvironment,
                HomeAccountId = homeAccountId,
                UserAssertionHash = userAssertionHash,
                Secret = string.Empty
            };
        }

        internal static MsalIdTokenCacheItem CreateIdTokenCacheItem(
            string tenant = TestConstants.Utid,
            string homeAccountId = TestConstants.HomeAccountId,
            string uid = TestConstants.Uid)
        {
            return new MsalIdTokenCacheItem()
            {
                ClientId = TestConstants.ClientId,
                Environment = TestConstants.ProductionPrefCacheEnvironment,
                HomeAccountId = homeAccountId,
                TenantId = tenant,
                Secret = MockHelpers.CreateIdToken(uid, TestConstants.DisplayableId, tenant)
            };
        }

        internal static MsalAccountCacheItem CreateAccountItem(
            string tenant = TestConstants.Utid,
            string homeAccountId = TestConstants.HomeAccountId)
        {
            return new MsalAccountCacheItem()
            {
                Environment = TestConstants.ProductionPrefCacheEnvironment,
                HomeAccountId = homeAccountId,
                TenantId = tenant,
                PreferredUsername = TestConstants.DisplayableId,
            };
        }

        internal static MsalAppMetadataCacheItem CreateAppMetadataItem(
            string clientId = TestConstants.ClientId)
        {
            return new MsalAppMetadataCacheItem(
                clientId,
                TestConstants.ProductionPrefCacheEnvironment,
                null);
        }

        internal static void PopulateCache(
            ITokenCacheAccessor accessor,
            string uid = TestConstants.Uid,
            string utid = TestConstants.Utid,
            string clientId = TestConstants.ClientId,
            string environment = TestConstants.ProductionPrefCacheEnvironment,
            string displayableId = TestConstants.DisplayableId,
            string rtSecret = TestConstants.RTSecret,
            string overridenScopes = null,
            string userAssertion = null,
            bool expiredAccessTokens = false,
            bool addSecondAt = true)
        {
            bool addAccessTokenOnly = accessor is InMemoryPartitionedAppTokenCacheAccessor;

            string clientInfo = MockHelpers.CreateClientInfo(uid, utid);
            string homeAccId = ClientInfo.CreateFromJson(clientInfo).ToAccountIdentifier();

            var accessTokenExpiresOn = expiredAccessTokens ?
                new DateTimeOffset(DateTime.UtcNow) :
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn));

            var extendedAccessTokenExpiresOn = expiredAccessTokens ?
                new DateTimeOffset(DateTime.UtcNow) :
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExtendedExpiresIn));

            MsalAccessTokenCacheItem atItem = new MsalAccessTokenCacheItem(
                environment,
                clientId,
                overridenScopes ?? TestConstants.s_scope.AsSingleString(),
                utid,
                "",
                accessTokenExpiresOn,
                extendedAccessTokenExpiresOn,
                clientInfo,
                homeAccId);

            if (userAssertion != null)
            {
                var crypto = PlatformProxyFactory.CreatePlatformProxy(null).CryptographyManager;
                atItem.UserAssertionHash = crypto.CreateBase64UrlEncodedSha256Hash(userAssertion);
            }

            // add access token
            accessor.SaveAccessToken(atItem);

            // add another access token
            if (addSecondAt)
            {
                atItem = new MsalAccessTokenCacheItem(
                  environment,
                  clientId,
                  TestConstants.s_scopeForAnotherResource.AsSingleString(),
                  utid,
                  "",
                  accessTokenExpiresOn,
                  extendedAccessTokenExpiresOn,
                  clientInfo,
                  homeAccId);

                accessor.SaveAccessToken(atItem);
            }

            if (!addAccessTokenOnly)
            {
                var idTokenCacheItem = new MsalIdTokenCacheItem(
                environment,
                clientId,
                MockHelpers.CreateIdToken(TestConstants.UniqueId + "more", displayableId),
                clientInfo,
                homeAccId,
                tenantId: utid);

                accessor.SaveIdToken(idTokenCacheItem);

                var accountCacheItem = new MsalAccountCacheItem(
                    environment,
                    null,
                    clientInfo,
                    homeAccId,
                    null,
                    displayableId,
                    utid,
                    null,
                    null,
                    null);

                accessor.SaveAccount(accountCacheItem);

                AddRefreshTokenToCache(accessor, uid, utid, clientId, environment, rtSecret);

                var appMetadataItem = new MsalAppMetadataCacheItem(
                    clientId,
                    environment,
                    null);

                accessor.SaveAppMetadata(appMetadataItem);
            }
        }

        internal static void PopulateCacheWithOneAccessToken(ITokenCacheAccessor accessor)
        {
            string clientInfo = MockHelpers.CreateClientInfo();
            string homeAccountId = ClientInfo.CreateFromJson(clientInfo).ToAccountIdentifier();

            MsalAccessTokenCacheItem atItem = new MsalAccessTokenCacheItem(
               TestConstants.ProductionPrefCacheEnvironment,
               TestConstants.ClientId,
               TestConstants.s_scope.AsSingleString(),
               TestConstants.Utid,
               "",
               new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
               new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExtendedExpiresIn)),
               clientInfo,
               homeAccountId);

            // add access token
            accessor.SaveAccessToken(atItem);

            MsalIdTokenCacheItem idTokenCacheItem = new MsalIdTokenCacheItem(
                TestConstants.ProductionPrefCacheEnvironment,
                TestConstants.ClientId,
                MockHelpers.CreateIdToken(TestConstants.UniqueId + "more", TestConstants.DisplayableId),
                clientInfo,
                homeAccountId,
                TestConstants.Utid);

            accessor.SaveIdToken(idTokenCacheItem);

            MsalAccountCacheItem accountCacheItem = new MsalAccountCacheItem(
                TestConstants.ProductionPrefNetworkEnvironment,
                null,
                clientInfo,
                homeAccountId,
                null,
                null,
                TestConstants.Utid,
                null,
                null,
                null);

            accessor.SaveAccount(accountCacheItem);
            AddRefreshTokenToCache(accessor, TestConstants.Uid, TestConstants.Utid);
        }

        public static void AddRefreshTokenToCache(
            ITokenCacheAccessor accessor,
            string uid,
            string utid,
            string clientId = TestConstants.ClientId,
            string environment = TestConstants.ProductionPrefCacheEnvironment,
            string rtSecret = TestConstants.RTSecret)
        {
            var rtItem = new MsalRefreshTokenCacheItem
                (environment, clientId, rtSecret, MockHelpers.CreateClientInfo(uid, utid), null, $"{uid}.{utid}");

            accessor.SaveRefreshToken(rtItem);
        }

        public static void AddRefreshTokensToCache(ITokenCacheAccessor cacheAccessor, int tokensQuantity = 1)
        {
            for (int i = 1; i <= tokensQuantity; i++)
            {
                AddRefreshTokenToCache(cacheAccessor, Guid.NewGuid().ToString(), TestConstants.Utid);
            }
        }

        public static void AddAccountToCache(
            ITokenCacheAccessor accessor,
            string uid,
            string utid,
            string environment = TestConstants.ProductionPrefCacheEnvironment)
        {
            MsalAccountCacheItem accountCacheItem = new MsalAccountCacheItem(
                environment,
                null,
                MockHelpers.CreateClientInfo(uid, utid),
                $"{uid}.{utid}",
                null,
                null,
                utid,
                null,
                null,
                null);

            accessor.SaveAccount(accountCacheItem);
        }

        public static void ExpireAccessTokens(ITokenCacheInternal tokenCache)
        {
            IReadOnlyList<MsalAccessTokenCacheItem> allAccessTokens;

            // avoid calling GetAllAccessTokens() on the strict accessors, as they will throw
            if (tokenCache.Accessor is AppAccessorWithPartitionAsserts appPartitionedAccessor)
            {
                allAccessTokens = appPartitionedAccessor.AccessTokenCacheDictionary.SelectMany(dict => dict.Value).Select(kv => kv.Value).ToList();
            }
            else if (tokenCache.Accessor is UserAccessorWithPartitionAsserts userPartitionedAccessor)
            {
                allAccessTokens = userPartitionedAccessor.AccessTokenCacheDictionary.SelectMany(dict => dict.Value).Select(kv => kv.Value).ToList();
            }
            else
            {
                allAccessTokens = tokenCache.Accessor.GetAllAccessTokens();
            }

            foreach (MsalAccessTokenCacheItem atItem in allAccessTokens)
            {
                ExpireAndSaveAccessToken(tokenCache, atItem);
            }
        }

        public static void ExpireAndSaveAccessToken(ITokenCacheInternal tokenCache, MsalAccessTokenCacheItem atItem)
        {
            atItem.ExpiresOnUnixTimestamp = CoreHelpers.DateTimeToUnixTimestamp(DateTimeOffset.UtcNow);
            tokenCache.AddAccessTokenCacheItem(atItem);
        }

        public static void UpdateUserAssertions(ConfidentialClientApplication app)
        {
            TokenCacheHelper.UpdateAccessTokenUserAssertions(app.UserTokenCacheInternal);
            TokenCacheHelper.UpdateRefreshTokenUserAssertions(app.UserTokenCacheInternal);
        }

        public static void UpdateAccessTokenUserAssertions(ITokenCacheInternal tokenCache, string assertion = "SomeAssertion")
        {
            var allAccessTokens = tokenCache.Accessor.GetAllAccessTokens();

            foreach (var atItem in allAccessTokens)
            {
                atItem.UserAssertionHash = assertion;
                tokenCache.AddAccessTokenCacheItem(atItem);
            }
        }

        public static void UpdateRefreshTokenUserAssertions(ITokenCacheInternal tokenCache, string assertion = "SomeAssertion")
        {
            var rtItems = tokenCache.Accessor.GetAllRefreshTokens();

            foreach (var rtItem in rtItems)
            {
                rtItem.UserAssertionHash = assertion;
                tokenCache.AddRefreshTokenCacheItem(rtItem);
            }
        }
    }
}
