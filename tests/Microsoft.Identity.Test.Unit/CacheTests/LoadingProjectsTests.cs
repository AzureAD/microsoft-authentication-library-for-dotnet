// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheTests
{
#if !ANDROID && !iOS  // custom token cache serialization not available 
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
        }
    }
#endif
}
