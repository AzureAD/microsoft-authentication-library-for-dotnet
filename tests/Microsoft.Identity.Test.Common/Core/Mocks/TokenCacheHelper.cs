// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.OAuth2;
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
            bool isExpired = false,
            string oboCacheKey = null,
            TimeSpan? exiresIn = null,
            string accessToken = TestConstants.ATSecret)
        {
            var expiresIn = (exiresIn.HasValue ? exiresIn.Value : TimeSpan.FromSeconds(ValidExpiresIn));
            MsalAccessTokenCacheItem atItem = new MsalAccessTokenCacheItem(
               TestConstants.ProductionPrefCacheEnvironment,
               TestConstants.ClientId,
               scopes,
               tenantId: tenant,
               secret: accessToken,
               cachedAt: DateTimeOffset.UtcNow,
               expiresOn: isExpired ? new DateTimeOffset(DateTime.UtcNow) : new DateTimeOffset(DateTime.UtcNow + expiresIn),
               extendedExpiresOn: isExpired ? new DateTimeOffset(DateTime.UtcNow) : new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExtendedExpiresIn)),
               MockHelpers.CreateClientInfo(),
               homeAccountId,
               oboCacheKey: oboCacheKey);

            return atItem;
        }

        internal static MsalTokenResponse CreateMsalTokenResponse(bool includeRefreshToken = false)
        {
            return new MsalTokenResponse
            {
                IdToken = MockHelpers.CreateIdToken(TestConstants.UniqueId, TestConstants.DisplayableId),
                AccessToken = TestConstants.ATSecret,
                ClientInfo = MockHelpers.CreateClientInfo(),
                ExpiresIn = 3599,
                CorrelationId = "correlation-id",
                RefreshToken = includeRefreshToken ? TestConstants.RTSecret : null, // brokers don't return RT
                Scope = TestConstants.s_scope.AsSingleString(),
                TokenType = "Bearer",
                WamAccountId = "wam_account_id",
            };
        }

        internal static MsalRefreshTokenCacheItem CreateRefreshTokenItem(
            string oboCacheKey = TestConstants.UserAssertion,
            string homeAccountId = TestConstants.HomeAccountId,
            string refreshToken = TestConstants.RTSecret)
        {
            return new MsalRefreshTokenCacheItem()
            {
                ClientId = TestConstants.ClientId,
                Environment = TestConstants.ProductionPrefCacheEnvironment,
                HomeAccountId = homeAccountId,
                OboCacheKey = oboCacheKey,
                Secret = refreshToken,
            };
        }

        internal static MsalIdTokenCacheItem CreateIdTokenCacheItem(
            string tenant = TestConstants.Utid,
            string homeAccountId = TestConstants.HomeAccountId,
            string uid = TestConstants.Uid,
            string idToken = "")
        {
            return new MsalIdTokenCacheItem()
            {
                ClientId = TestConstants.ClientId,
                Environment = TestConstants.ProductionPrefCacheEnvironment,
                HomeAccountId = homeAccountId,
                TenantId = tenant,
                Secret = !string.IsNullOrEmpty(idToken) ? idToken : MockHelpers.CreateIdToken(uid, TestConstants.DisplayableId, tenant),
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
                DateTimeOffset.UtcNow :
                DateTimeOffset.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn);

            var extendedAccessTokenExpiresOn = expiredAccessTokens ?
                DateTimeOffset.UtcNow :
                DateTimeOffset.UtcNow + TimeSpan.FromSeconds(ValidExtendedExpiresIn);

            string userAssertionHash = null;
            if (userAssertion != null)
            {
                var crypto = PlatformProxyFactory.CreatePlatformProxy(null).CryptographyManager;
                userAssertionHash = crypto.CreateBase64UrlEncodedSha256Hash(userAssertion);
            }

            MsalAccessTokenCacheItem atItem = new MsalAccessTokenCacheItem(
                environment,
                clientId,
                overridenScopes ?? TestConstants.s_scope.AsSingleString(),
                utid,
                "",
                DateTimeOffset.UtcNow,
                accessTokenExpiresOn,
                extendedAccessTokenExpiresOn,
                clientInfo,
                homeAccId,
                oboCacheKey: userAssertionHash);

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
                  DateTimeOffset.UtcNow,
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

        internal static IEnumerable<Tuple<MsalAccessTokenCacheItem,
                                          MsalRefreshTokenCacheItem,
                                          MsalIdTokenCacheItem,
                                          MsalAccountCacheItem>> PopulateCacheWithAccessTokens(ITokenCacheAccessor accessor, int tokensQuantity = 1)
        {
            IList<Tuple<MsalAccessTokenCacheItem, MsalRefreshTokenCacheItem, MsalIdTokenCacheItem, MsalAccountCacheItem>> tokens
                                        = new List<Tuple<MsalAccessTokenCacheItem, MsalRefreshTokenCacheItem, MsalIdTokenCacheItem, MsalAccountCacheItem>>();

            bool randomizeClientInfo = tokensQuantity > 1;

            for (int i = 1; i <= tokensQuantity; i++)
            {
                var result = PopulateCacheWithOneAccessToken(accessor, randomizeClientInfo);
                Tuple<MsalAccessTokenCacheItem, MsalRefreshTokenCacheItem, MsalIdTokenCacheItem, MsalAccountCacheItem> token =
                    new Tuple<MsalAccessTokenCacheItem,
                              MsalRefreshTokenCacheItem,
                              MsalIdTokenCacheItem,
                              MsalAccountCacheItem>(result.AT, result.RT, result.ID, result.Account);

                tokens.Add(token);
            }

            return tokens;
        }

        internal static (MsalAccessTokenCacheItem AT, MsalRefreshTokenCacheItem RT, MsalIdTokenCacheItem ID, MsalAccountCacheItem Account) PopulateCacheWithOneAccessToken(ITokenCacheAccessor accessor, bool randomizeClientInfo = false)
        {
            string uid = randomizeClientInfo ? Guid.NewGuid().ToString() : TestConstants.Uid;
            string utid = randomizeClientInfo ? Guid.NewGuid().ToString() : TestConstants.Utid;

            string clientInfo = MockHelpers.CreateClientInfo(uid, utid);
            string homeAccountId = ClientInfo.CreateFromJson(clientInfo).ToAccountIdentifier();

            MsalAccessTokenCacheItem atItem = new MsalAccessTokenCacheItem(
               TestConstants.ProductionPrefCacheEnvironment,
               TestConstants.ClientId,
               TestConstants.s_scope.AsSingleString(),
               TestConstants.Utid,
               "",
               DateTimeOffset.UtcNow,
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
            var rt = AddRefreshTokenToCache(accessor, uid, utid);

            return (atItem, rt, idTokenCacheItem, accountCacheItem);
        }

        public static MsalRefreshTokenCacheItem AddRefreshTokenToCache(
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

            return rtItem;
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

        public static void ExpireAllAccessTokens(ITokenCacheInternal tokenCache)
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
                ExpireAccessToken(tokenCache, atItem);
            }
        }

        public static void ExpireAccessToken(ITokenCacheInternal tokenCache, MsalAccessTokenCacheItem atItem)
        {
            tokenCache.Accessor.SaveAccessToken(atItem.WithExpiresOn(DateTimeOffset.UtcNow));
        }

        public static MsalAccessTokenCacheItem WithRefreshOn(this MsalAccessTokenCacheItem atItem, DateTimeOffset? refreshOn)
        {
            MsalAccessTokenCacheItem newAtItem = new MsalAccessTokenCacheItem(
               atItem.Environment,
               atItem.ClientId,
               atItem.ScopeString,
               atItem.TenantId,
               atItem.Secret,
               atItem.CachedAt,
               atItem.ExpiresOn,
               atItem.ExtendedExpiresOn,
               atItem.RawClientInfo,
               atItem.HomeAccountId,
               atItem.KeyId,
               refreshOn,
               atItem.TokenType,
               atItem.OboCacheKey);

            return newAtItem;
        }

        public static MsalAccessTokenCacheItem WithUserAssertion(this MsalAccessTokenCacheItem atItem, string assertion)
        {
            MsalAccessTokenCacheItem newAtItem = new MsalAccessTokenCacheItem(
               atItem.Environment,
               atItem.ClientId,
               atItem.ScopeString,
               atItem.TenantId,
               atItem.Secret,
               atItem.CachedAt,
               atItem.ExpiresOn,
               atItem.ExtendedExpiresOn,
               atItem.RawClientInfo,
               atItem.HomeAccountId,
               atItem.KeyId,
               atItem.RefreshOn,
               atItem.TokenType,
               assertion);

            return newAtItem;
        }

        public static void UpdateUserAssertions(ConfidentialClientApplication app)
        {
            UpdateAccessTokenUserAssertions(app.UserTokenCacheInternal);
            UpdateRefreshTokenUserAssertions(app.UserTokenCacheInternal);
        }

        public static void UpdateAccessTokenUserAssertions(ITokenCacheInternal tokenCache, string assertion = "SomeAssertion")
        {
            var allAccessTokens = tokenCache.Accessor.GetAllAccessTokens();

            foreach (var atItem in allAccessTokens)
            {
                var newAt = atItem.WithUserAssertion(assertion);
                tokenCache.Accessor.SaveAccessToken(newAt);
                tokenCache.Accessor.DeleteAccessToken(atItem);
            }
        }

        public static void UpdateRefreshTokenUserAssertions(ITokenCacheInternal tokenCache, string assertion = "SomeAssertion")
        {
            var rtItems = tokenCache.Accessor.GetAllRefreshTokens();

            foreach (var rtItem in rtItems)
            {
                rtItem.OboCacheKey = assertion;
                tokenCache.Accessor.SaveRefreshToken(rtItem);
            }
        }
    }
}
