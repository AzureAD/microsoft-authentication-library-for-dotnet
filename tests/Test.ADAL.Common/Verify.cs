//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;

#if TEST_ADAL_WINRT
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace Test.ADAL.Common
{
    static class Verify
    {
        internal static void IsGreaterThanOrEqual(IComparable expectedGreater, IComparable expectedLess, string message = null)
        {
            Assert.IsTrue(expectedGreater.CompareTo(expectedLess) >= 0, message);
        }

        internal static void IsLessThanOrEqual(IComparable expectedLess, IComparable expectedGreater, string message = null)
        {
            Assert.IsTrue(expectedLess.CompareTo(expectedGreater) <= 0, message);
        }

        internal static void AreEqual(IComparable expected, IComparable actual, string message = null)
        {
            Assert.IsTrue((expected == null && actual == null) || expected.CompareTo(actual) == 0, message);
        }

        internal static void AreEqual(object expected, object actual, string message = null)
        {
            Assert.IsTrue(expected == actual || (expected != null && expected.Equals(actual)), message);
        }

        internal static void AreNotEqual(object expected, object actual, string message = null)
        {
            Assert.IsFalse(expected == actual || (expected != null && expected.Equals(actual)), message);
        }

        internal static void IsTrue(bool condition, string message = null)
        {
            Assert.IsTrue(condition, message);
        }

        internal static void IsFalse(bool condition, string message = null)
        {
            Assert.IsFalse(condition, message);
        }

        internal static void IsNull(object variable, string message = null)
        {
            Assert.IsNull(variable, message);
        }

        internal static void IsNotNull(object variable, string message = null)
        {
            Assert.IsNotNull(variable, message);
        }

        internal static void Fail(string message, params object[] args)
        {
            Assert.IsTrue(false, message, args);
        }
    }
}
