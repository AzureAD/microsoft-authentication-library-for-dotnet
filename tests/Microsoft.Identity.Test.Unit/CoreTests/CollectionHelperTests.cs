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
#if NET6_0_OR_GREATER

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
