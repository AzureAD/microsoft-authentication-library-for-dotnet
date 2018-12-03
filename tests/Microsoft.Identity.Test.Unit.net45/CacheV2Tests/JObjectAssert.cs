// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

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