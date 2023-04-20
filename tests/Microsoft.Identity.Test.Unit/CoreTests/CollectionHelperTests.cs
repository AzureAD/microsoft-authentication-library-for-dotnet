// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Identity.Client.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests
{
    [TestClass]
    public class CollectionHelperTests
    {
        [TestMethod]
        public void EmptyStringDictionary_Is_Shared()
        {
            var d1 = CollectionHelpers.GetEmptyDictionary<string, string>();
            var d2 = CollectionHelpers.GetEmptyDictionary<string, string>();

            Assert.AreEqual(d1, d2);    
        }

#if NETCOREAPP2_0_OR_GREATER

        [TestMethod]
        public void EmptyStringDictionaryIsImmutable()
        {
            (CollectionHelpers.GetEmptyDictionary<string, string>() as System.Collections.Immutable.ImmutableDictionary<string, string>).Add("k", "v");
            Assert.IsTrue(!CollectionHelpers.GetEmptyDictionary<string, string>().Any());

            Assert.IsNull((CollectionHelpers.GetEmptyDictionary<string, string>() as Dictionary<string, string>));
        }
#endif
    }
}
