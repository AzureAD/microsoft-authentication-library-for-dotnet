// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.Identity.Client.Cache.Keys;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class MsalTokenCacheKeysTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
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
