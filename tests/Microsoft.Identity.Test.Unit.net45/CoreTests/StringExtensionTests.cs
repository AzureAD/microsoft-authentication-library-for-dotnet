// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Microsoft.Identity.Client.Utils;

namespace Microsoft.Identity.Test.Unit.CoreTests
{
    [TestClass]
    public class StringExtensionTests
    {
        [TestMethod]
        public void StringReplace()
        {
            string original1 = "https://login.microsoftonline.com/{tenant}/oauth2";
            string original2 = "https://login.microsoftonline.com/{tenant_id}/oauth2";
            string expected = "https://login.microsoftonline.com/common/oauth2";

            Assert.AreEqual(expected, original1.Replace("{tenant}", "common", StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual(expected, original2.Replace("{tenant_ID}", "common", StringComparison.OrdinalIgnoreCase));

            Assert.AreEqual(original1, original1.Replace("{tenant_id}", "common", StringComparison.OrdinalIgnoreCase));
        }
    }
}
