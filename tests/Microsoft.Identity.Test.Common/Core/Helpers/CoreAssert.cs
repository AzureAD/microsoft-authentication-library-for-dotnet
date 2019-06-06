// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Common.Core.Helpers
{
    public static class CoreAssert
    {
        public static void AreScopesEqual(string scopesExpected, string scopesActual)
        {
            var expectedScopes = ScopeHelper.ConvertStringToLowercaseSortedSet(scopesExpected);
            var actualScopes = ScopeHelper.ConvertStringToLowercaseSortedSet(scopesActual);

            // can't use Assert.AreEqual on HashSet, so we'll compare by hand.
            Assert.AreEqual(expectedScopes.Count, actualScopes.Count);
            foreach (string expectedScope in expectedScopes)
            {
                Assert.IsTrue(actualScopes.Contains(expectedScope));
            }
        }

        public static void AreEqual<T>(T val1, T val2, T val3)
        {
            Assert.AreEqual(val1, val2, "First and second values differ");
            Assert.AreEqual(val1, val3, "First and third values differ");
        }
    }
}
