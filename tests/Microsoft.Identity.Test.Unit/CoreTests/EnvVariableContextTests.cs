// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests
{
    [TestClass]
    public class EnvVariableContextTests
    {
        [TestMethod]
        public void EnvVariableContextTest()
        {
            Environment.SetEnvironmentVariable("var1", "val1");
            Environment.SetEnvironmentVariable("var2", "val2");
            Environment.SetEnvironmentVariable("var3", "val3");
            Environment.SetEnvironmentVariable("var4", null);
            Environment.SetEnvironmentVariable("var5", ""); // this is the same as null

            using (new EnvVariableContext())
            {
                Environment.SetEnvironmentVariable("var1", null);
                Environment.SetEnvironmentVariable("var2", "");

                Environment.SetEnvironmentVariable("var3", "other_value");
                Environment.SetEnvironmentVariable("var4", "val4");
                Environment.SetEnvironmentVariable("var5", "val5");
            }

            Assert.AreEqual("val1", Environment.GetEnvironmentVariable("var1"));
            Assert.AreEqual("val2", Environment.GetEnvironmentVariable("var2"));
            Assert.AreEqual("val3", Environment.GetEnvironmentVariable("var3"));
            Assert.IsNull(Environment.GetEnvironmentVariable("var4"));
            Assert.IsNull(Environment.GetEnvironmentVariable("var5"));
        }
    }
}
