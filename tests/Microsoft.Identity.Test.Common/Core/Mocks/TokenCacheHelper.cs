// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Unit;

namespace Microsoft.Identity.Test.Common.Core.Mocks
{
    internal class TokenCacheHelper
    {
        public static long ValidExpiresIn = 28800;
        public static long ValidExtendedExpiresIn = 57600;

        internal void PopulateCacheForClientCredential(ITokenCacheAccessor accessor, int tokensQuantity = 1)
        {
            for (int i = 1; i <= tokensQuantity; i++)
            {
                var atItem = CreateAccessTokenItem(string.Format(System.Globalization.CultureInfo.InvariantCulture, TestConstants.ScopeStrFormat, i));
                accessor.SaveAccessToken(atItem);
            }
        }

        internal static MsalAccessTokenCacheItem CreateAccessTokenItem(string scopes = "")
        {
            string clientInfo = MockHelpers.CreateClientInfo();
            string homeAccId = ClientInfo.CreateFromJson(clientInfo).ToAccountIdentifier();

            MsalAccessTokenCacheItem atItem = new MsalAccessTokenCacheItem(
               TestConstants.ProductionPrefCacheEnvironment,
               TestConstants.ClientId,
               string.IsNullOrEmpty(scopes) ? TestConstants.s_scope.AsSingleString() : scopes,
               TestConstants.Utid,
               "",
               new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)),
               new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExtendedExpiresIn)),
               MockHelpers.CreateClientInfo(),
               homeAccId);

            return atItem;
        }

        internal void PopulateCache(
            ITokenCacheAccessor accessor,
            string uid = TestConstants.Uid,
            string utid = TestConstants.Utid,
            string clientId = TestConstants.ClientId,
            string environment = TestConstants.ProductionPrefCacheEnvironment,
            string displayableId = TestConstants.DisplayableId,
            string rtSecret = TestConstants.RTSecret,
            string overridenScopes = null, 
            bool expiredAccessTokens = false, 
            bool addSecondAt = true)
        {
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

            // add access token
            accessor.SaveAccessToken(atItem);

            var idTokenCacheItem = new MsalIdTokenCacheItem(
                environment,
                clientId,
                MockHelpers.CreateIdToken(TestConstants.UniqueId + "more", displayableId),
                clientInfo,
                homeAccId,
                tenantId: utid);

            accessor.SaveIdToken(idTokenCacheItem);

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

        internal void PopulateCacheWithOneAccessToken(ITokenCacheAccessor accessor)
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
    }
}
