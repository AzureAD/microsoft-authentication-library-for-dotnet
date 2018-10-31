using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Test.MSAL.NET.Unit.netcore
{
#if !ANDROID && !iOS && !WINDOWS_APP // custom token cache serialization not available 
    [TestClass]
    public class LoadingProjectsTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetStateAndInitMsal();
        }

        [TestMethod]
        public void CanDeserializeTokenCache()
        {
            TokenCache tokenCache = new TokenCache();
            tokenCache.Deserialize(null);
            Assert.IsFalse(tokenCache.HasStateChanged, "State should not have changed when deserializing nothing.");
        }
    }
#endif
}