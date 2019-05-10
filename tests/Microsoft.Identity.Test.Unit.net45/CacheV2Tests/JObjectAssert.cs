// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Identity.Json.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CacheV2Tests
{
    internal static class JObjectAssert
    {
        public static void AreEqual(JObject expected, JObject actual)
        {
            if (expected == null && actual == null)
            {
                return;
            }

            if (expected == null)
            {
                Assert.Fail("Expected should not be null");
            }

            if (actual == null)
            {
                Assert.Fail("Actual should not be null");
            }

            if (expected.Count != actual.Count)
            {
                Assert.Fail($"expected.Count ({expected.Count}) != actual.Count ({actual.Count})");
            }

            foreach (KeyValuePair<string, JToken> kvpExpected in expected)
            {
                if (actual.TryGetValue(kvpExpected.Key, out var value))
                {
                    Assert.AreEqual(kvpExpected.Value.Type, value.Type);
                    Assert.AreEqual(kvpExpected.Value, value);
                }
                else
                {
                    Assert.Fail($"actual does not have key: {kvpExpected.Key}");
                }
            }
        }
    }
}
