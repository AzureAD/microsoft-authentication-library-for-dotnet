using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache.Items;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
    [TestClass]
    public class TokenCacheItemTests
    {
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
