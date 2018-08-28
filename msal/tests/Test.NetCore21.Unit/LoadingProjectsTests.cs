using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace NetCoreUTest
{
    [TestClass]
    public class LoadingProjectsTests
    {
        [TestMethod]
        public void CanDeserializeTokenCacheInNetCore()
        {
            var previousLogLevel = Logger.Level;
            // Setting LogLevel.Verbose causes certain static dependencies to load
            Logger.Level = LogLevel.Verbose;
            TokenCache tokenCache = new TokenCache();
            tokenCache.Deserialize(null);
            Assert.IsFalse(tokenCache.HasStateChanged, "State should not have changed when deserializing nothing.");
            Logger.Level = previousLogLevel;
        }
    }
}
