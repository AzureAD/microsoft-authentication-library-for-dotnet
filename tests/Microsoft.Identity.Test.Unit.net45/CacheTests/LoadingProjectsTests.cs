using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
#if !ANDROID && !iOS && !WINDOWS_APP // custom token cache serialization not available 
    [TestClass]
    public class LoadingProjectsTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [TestMethod]
        public void CanDeserializeTokenCache()
        {
            TokenCache tokenCache = new TokenCache(TestCommon.CreateDefaultServiceBundle(), false)
            {
                AfterAccess = args => { Assert.IsFalse(args.HasStateChanged); }
            };

            ((ITokenCacheSerializer)tokenCache).DeserializeMsalV3(null);
#pragma warning disable CS0618 // Type or member is obsolete
            Assert.IsFalse(tokenCache.HasStateChanged, "State should not have changed when deserializing nothing.");
#pragma warning restore CS0618 // Type or member is obsolete
            
        }
    }
#endif
}
