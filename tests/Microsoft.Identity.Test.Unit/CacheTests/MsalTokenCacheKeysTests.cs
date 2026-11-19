// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class MsalTokenCacheKeysTests : TestBase
    {
        [TestMethod]
        public void LegacyOidcMigration_PreservesUnrelatedCacheEntries()
        {
            // Arrange
            var response = new MsalTokenResponse { Scope = "user.read", AccessToken = "access-token", TokenType = "Bearer" };
            var oidc = new MsalAccessTokenCacheItem("oidc.example", "HighClient", response, "tenant", "user");
            var otherAuthority = new MsalAccessTokenCacheItem("login.microsoftonline.com", "HighClient", response, "tenant", "user");
            var otherClient = new MsalAccessTokenCacheItem("oidc.example", "OtherClient", response, "tenant", "user");
            var cache = new CacheSerializationContract(null);
            foreach (var item in new[] { oidc, otherAuthority, otherClient })
            {
                cache.AccessTokens.Add(item.CacheKey, item);
            }

            // Act
            var restored = CacheSerializationContract.FromJsonString(cache.ToJsonString(), "highclient", "oidc.example");

            // Assert
            Assert.HasCount(3, restored.AccessTokens);
            Assert.IsTrue(restored.AccessTokens.ContainsKey(otherAuthority.CacheKey));
            Assert.IsFalse(restored.AccessTokens[otherAuthority.CacheKey].IsClientIdCaseSensitive);
            Assert.IsTrue(restored.AccessTokens.ContainsKey(otherClient.CacheKey));
            Assert.IsFalse(restored.AccessTokens[otherClient.CacheKey].IsClientIdCaseSensitive);
            Assert.IsFalse(restored.AccessTokens.ContainsKey(oidc.CacheKey));
            Assert.IsTrue(restored.AccessTokens.Values.Single(
                item => item.Environment == "oidc.example" && item.ClientId == "HighClient").IsClientIdCaseSensitive);
        }

        [TestMethod]
        public void CaseSensitiveClientKeys_SurviveSerialization()
        {
            // Arrange
            var response = new MsalTokenResponse { Scope = "user.read", AccessToken = "access-token", TokenType = "Bearer" };
            var upperAccess = new MsalAccessTokenCacheItem("issuer", "HighClient", response, "tenant", "user", isClientIdCaseSensitive: true);
            var lowerAccess = new MsalAccessTokenCacheItem("issuer", "highclient", response, "tenant", "user", isClientIdCaseSensitive: true);
            var upperRefresh = new MsalRefreshTokenCacheItem("issuer", "HighClient", "refresh-token", null, null, "user", isClientIdCaseSensitive: true);
            var lowerRefresh = new MsalRefreshTokenCacheItem("issuer", "highclient", "refresh-token", null, null, "user", isClientIdCaseSensitive: true);
            var upperId = new MsalIdTokenCacheItem("issuer", "HighClient", "id-token", null, "user", "tenant", isClientIdCaseSensitive: true);
            var lowerId = new MsalIdTokenCacheItem("issuer", "highclient", "id-token", null, "user", "tenant", isClientIdCaseSensitive: true);
            var upperMetadata = new MsalAppMetadataCacheItem("HighClient", "issuer", null, isClientIdCaseSensitive: true);
            var lowerMetadata = new MsalAppMetadataCacheItem("highclient", "issuer", null, isClientIdCaseSensitive: true);

            // Act
            var restoredAccess = MsalAccessTokenCacheItem.FromJsonString(upperAccess.ToJsonString());
            var restoredRefresh = MsalRefreshTokenCacheItem.FromJsonString(upperRefresh.ToJsonString());
            var restoredId = MsalIdTokenCacheItem.FromJsonString(upperId.ToJsonString());
            var restoredMetadata = MsalAppMetadataCacheItem.FromJsonString(upperMetadata.ToJsonString());

            // Assert
            AssertKeys(upperAccess.CacheKey, lowerAccess.CacheKey, restoredAccess.CacheKey, lowerAccess.iOSCacheKey, restoredAccess.iOSCacheKey);
            AssertKeys(upperRefresh.CacheKey, lowerRefresh.CacheKey, restoredRefresh.CacheKey, lowerRefresh.iOSCacheKey, restoredRefresh.iOSCacheKey);
            AssertKeys(upperId.CacheKey, lowerId.CacheKey, restoredId.CacheKey, lowerId.iOSCacheKey, restoredId.iOSCacheKey);
            AssertKeys(upperMetadata.CacheKey, lowerMetadata.CacheKey, restoredMetadata.CacheKey, lowerMetadata.iOSCacheKey, restoredMetadata.iOSCacheKey, compareGeneric: false);
            Assert.AreEqual(upperId.CacheKey, restoredAccess.GetIdTokenItem().CacheKey);

            static void AssertKeys(string expectedKey, string otherKey, string restoredKey,
                IiOSKey otherIosKey, IiOSKey restoredIosKey, bool compareGeneric = true)
            {
                Assert.AreEqual(expectedKey, restoredKey);
                Assert.AreNotEqual(otherKey, restoredKey);
                Assert.AreNotEqual(otherIosKey.iOSService, restoredIosKey.iOSService);
                if (compareGeneric)
                {
                    Assert.AreNotEqual(otherIosKey.iOSGeneric, restoredIosKey.iOSGeneric);
                }
            }
        }

        [TestMethod]
        public void MsalAccessTokenCacheKey()
        {
            var tokenResponse = new MsalTokenResponse();
            tokenResponse.Scope = "user.read user.write";
            tokenResponse.TokenType = "bearer";

            var item = new MsalAccessTokenCacheItem("login.microsoftonline.com", "clientId", tokenResponse, "contoso.com", "uid.utid");
            var iOSKey = item.iOSCacheKey;
            Assert.AreEqual("uid.utid-login.microsoftonline.com-accesstoken-clientid-contoso.com-user.read user.write", item.CacheKey);

            Assert.AreEqual("uid.utid-login.microsoftonline.com", iOSKey.iOSAccount);
            Assert.AreEqual("accesstoken-clientid-contoso.com-user.read user.write", iOSKey.iOSService);
            Assert.AreEqual("accesstoken-clientid-contoso.com", iOSKey.iOSGeneric);
            Assert.AreEqual((int)MsalCacheKeys.iOSCredentialAttrType.AccessToken, iOSKey.iOSType);
        }

            [TestMethod]
        public void MsalPOPAccessTokenCacheKey()
        {
            var tokenResponse = new MsalTokenResponse();
            tokenResponse.Scope = "user.read user.write";
            tokenResponse.TokenType = "pop";

            var item = new MsalAccessTokenCacheItem("login.microsoftonline.com", "clientId", tokenResponse, "contoso.com", "uid.utid");
            var iOSKey = item.iOSCacheKey;

            Assert.AreEqual("uid.utid-login.microsoftonline.com-accesstoken_with_authscheme-clientid-contoso.com-user.read user.write-pop", item.CacheKey);

            Assert.AreEqual("uid.utid-login.microsoftonline.com", iOSKey.iOSAccount);
            Assert.AreEqual("accesstoken_with_authscheme-clientid-contoso.com-user.read user.write-pop", iOSKey.iOSService);
            Assert.AreEqual("accesstoken_with_authscheme-clientid-contoso.com", iOSKey.iOSGeneric);
            Assert.AreEqual((int)MsalCacheKeys.iOSCredentialAttrType.AccessToken, iOSKey.iOSType);
        }

        [TestMethod]
        public void MsalRefreshTokenCacheKey()
        {
            var item = new MsalRefreshTokenCacheItem("login.microsoftonline.com", "clientid", "secret", "rawClientInfo", "", "uid.utid");
            IiOSKey iOSKey = item.iOSCacheKey;

            Assert.AreEqual("uid.utid-login.microsoftonline.com-refreshtoken-clientid--", item.CacheKey);

            Assert.AreEqual("uid.utid-login.microsoftonline.com", iOSKey.iOSAccount);
            Assert.AreEqual("refreshtoken-clientid--", iOSKey.iOSService);
            Assert.AreEqual("refreshtoken-clientid-", iOSKey.iOSGeneric);
            Assert.AreEqual((int)MsalCacheKeys.iOSCredentialAttrType.RefreshToken, iOSKey.iOSType);
        }

        [TestMethod]
        public void MsalFamilyRefreshTokenCacheKey()
        {
            var item = new MsalRefreshTokenCacheItem("login.microsoftonline.com", "CLIENT_ID_NOT_USED", "secret", "rawClientInfo", "1", "uid.utid");
            IiOSKey iOSKey = item.iOSCacheKey;

            Assert.AreEqual("uid.utid-login.microsoftonline.com-refreshtoken-1--", item.CacheKey);

            Assert.AreEqual("uid.utid-login.microsoftonline.com", iOSKey.iOSAccount);
            Assert.AreEqual("refreshtoken-1--", iOSKey.iOSService);
            Assert.AreEqual("refreshtoken-1-", iOSKey.iOSGeneric);
            Assert.AreEqual((int)MsalCacheKeys.iOSCredentialAttrType.RefreshToken, iOSKey.iOSType);
        }

        [TestMethod]
        public void MsalIdTokenCacheKey()
        {
            var item = new MsalIdTokenCacheItem("login.microsoftonline.com", "clientid", "secret", "rawClientInfo", "uid.utid", "contoso.com");
            IiOSKey iOSKey = item.iOSCacheKey;

            Assert.AreEqual("uid.utid-login.microsoftonline.com-idtoken-clientid-contoso.com-", item.CacheKey);

            Assert.AreEqual("uid.utid-login.microsoftonline.com", iOSKey.iOSAccount);
            Assert.AreEqual("idtoken-clientid-contoso.com-", iOSKey.iOSService);
            Assert.AreEqual("idtoken-clientid-contoso.com", iOSKey.iOSGeneric);
            Assert.AreEqual((int)MsalCacheKeys.iOSCredentialAttrType.IdToken, iOSKey.iOSType);
        }

        [TestMethod]
        public void MsalAccountCacheKey()
        {
            var item = new MsalAccountCacheItem(
                "login.microsoftonline.com",
                "contoso.com",
                "uid.utid",
                null,
                "localId");

            var iOSKey = item.iOSCacheKey;

            Assert.AreEqual("uid.utid-login.microsoftonline.com-contoso.com", item.CacheKey);

            Assert.AreEqual("uid.utid-login.microsoftonline.com", iOSKey.iOSAccount);
            Assert.AreEqual("contoso.com", iOSKey.iOSService);
            Assert.AreEqual("localid", iOSKey.iOSGeneric);
            Assert.AreEqual(MsalCacheKeys.iOSAuthorityTypeToAttrType["MSSTS"], iOSKey.iOSType);
        }

        [TestMethod]
        public void MsalAppMetadataCacheKey()
        {
            var item = new MsalAppMetadataCacheItem("clientid", "login.microsoftonline.com", null);
            IiOSKey iOSKey = item.iOSCacheKey;

            Assert.AreEqual("appmetadata-login.microsoftonline.com-clientid", item.CacheKey);

            Assert.AreEqual("appmetadata-clientid", iOSKey.iOSService);
            Assert.AreEqual("login.microsoftonline.com", iOSKey.iOSAccount);
            Assert.AreEqual("1", iOSKey.iOSGeneric);
            Assert.AreEqual((int)MsalCacheKeys.iOSCredentialAttrType.AppMetadata, iOSKey.iOSType);
        }
    }
}
