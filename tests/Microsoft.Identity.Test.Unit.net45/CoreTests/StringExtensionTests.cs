// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Microsoft.Identity.Test.Unit.CoreTests
{
    [TestClass]
    public class StringExtensionTests
    {
        [TestMethod]
        public void StringReplace()
        {
            Assert.AreEqual("hi common !", "hi {tenant} !".Replace("{tenant}", "common", StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual("hi commoncommon !", "hi {tenant}{tenant} !".Replace("{tenant}", "common", StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual("hi common--common !", "hi {tenant}--{tenant} !".Replace("{tenant}", "common", StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual("hi common !", "hi {tenaNt} !".Replace("{tEnant}", "common", StringComparison.OrdinalIgnoreCase));

            Assert.AreEqual("hi common !", "hi {tenant_id} !".Replace("{tenant_ID}", "common", StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual("hi {tenant_id} !", "hi {tenant_id} !".Replace("nothing", "common", StringComparison.OrdinalIgnoreCase));
           
            Assert.AreEqual("", "".Replace("nothing", "common", StringComparison.OrdinalIgnoreCase));
            AssertException.Throws<ArgumentException>(() =>
                "hi {tenant} !".Replace("", "common", StringComparison.OrdinalIgnoreCase));
        }
    }
}
