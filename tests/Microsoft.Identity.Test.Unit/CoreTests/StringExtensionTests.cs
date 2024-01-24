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
        public void NullIfEmpty()
        {
            string nullString = null;

            Assert.IsNull("".NullIfEmpty());
            Assert.IsNull(nullString.NullIfEmpty());

            Assert.IsNull("".NullIfWhiteSpace());
            Assert.IsNull(" ".NullIfWhiteSpace());
            Assert.IsNull(nullString.NullIfWhiteSpace());
        }
    }
}
