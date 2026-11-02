// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class TokenCacheItemTests
    {
        [TestMethod]
        [DataRow(null, null)]
        [DataRow("0123456789ABCDEF0123456789ABCDEF01234567", null)]
        [DataRow(null, "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF")]
        [DataRow("0123456789ABCDEF0123456789ABCDEF01234567", "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF")]
        public void AccessTokenCertificateThumbprint_PreservedBySerializationAndExpiryChange(string thumbprint, string thumbprint256)
        {
            // Arrange
            var now = DateTimeOffset.FromUnixTimeSeconds(1800000000);
            var item = new MsalAccessTokenCacheItem(
                "environment", "client", "scope", "tenant", "access-token",
                now, now.AddHours(1), now.AddHours(2), null, null)
            {
                CertificateThumbprint = thumbprint,
                CertificateThumbprint256 = thumbprint256
            };

            // Act
            var deserialized = MsalAccessTokenCacheItem.FromJsonString(item.ToJsonString());
            var expired = deserialized.WithExpiresOn(now);

            // Assert
            Assert.AreEqual(thumbprint, deserialized.CertificateThumbprint);
            Assert.AreEqual(thumbprint, expired.CertificateThumbprint);
            Assert.AreEqual(thumbprint256, deserialized.CertificateThumbprint256);
            Assert.AreEqual(thumbprint256, expired.CertificateThumbprint256);
            Assert.AreEqual(item.CacheKey, deserialized.CacheKey);
            Assert.AreEqual(item.CacheKey, expired.CacheKey);
            Assert.AreEqual(thumbprint is not null, item.ToJObject().ContainsKey(StorageJsonKeys.CertificateThumbprint));
            Assert.AreEqual(thumbprint256 is not null, item.ToJObject().ContainsKey(StorageJsonKeys.CertificateThumbprint256));
        }

        [TestMethod]
        public void TestMsalRefreshTokenCacheItemFromJsonStringEmpty()
        {
            var item = MsalRefreshTokenCacheItem.FromJsonString(null);
            Assert.IsNull(item);
        }

        [TestMethod]
        public void TestMsalAccessTokenCacheItemFromJsonStringEmpty()
        {
            var item = MsalAccessTokenCacheItem.FromJsonString(null);
            Assert.IsNull(item);
        }

        [TestMethod]
        public void TestMsalAccountCacheItemFromJsonStringEmpty()
        {
            var item = MsalAccountCacheItem.FromJsonString(null);
            Assert.IsNull(item);
        }

        [TestMethod]
        public void TestMsalIdTokenCacheItemFromJsonStringEmpty()
        {
            var item = MsalIdTokenCacheItem.FromJsonString(null);
            Assert.IsNull(item);
        }

        [TestMethod]
        public void TestMsalAppMetadataCacheItemFromJsonStringEmpty()
        {
            var item = MsalAppMetadataCacheItem.FromJsonString(null);
            Assert.IsNull(item);
        }
    }
}
